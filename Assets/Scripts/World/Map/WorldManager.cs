using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class WorldManager : MonoBehaviour {

    public static WorldManager instance;
    public static UnityEvent OnWorldLoaded;
    public static UnityEvent OnWorldTick;
    float tickDeltaTime = 0.05f;
    float tickTimer = 0f;

    public string WorldToLoad = "";

    public WorldData worldData;
    public WorldRenderer worldRend;

    public AnimationCurve InfraBaseCapacityCurve;

    public bool SimuationPaused = true;

	void Awake () {
        instance = this;
        if (OnWorldLoaded == null) OnWorldLoaded = new UnityEvent();
        if(OnWorldTick == null) OnWorldTick = new UnityEvent();
	}
    private void Start()
    {
        OnWorldTick.AddListener(OnTick);
        ResourcesManager.OnResourcesInitialised.AddListener(OnResourceManagerInitialised);

        worldRend.worldMgr = this;
        worldRend.StartManual();
        LoadWorld(WorldToLoad);
    }
    public void LoadWorld(string worldName)
    {
        foreach(WorldUtils.MapInfo mi in WorldUtils.mapInfos)
        {
            OnWorldLoaded.RemoveListener(mi.mapReference.OnWorldLoaded); //Remove references to old maps.
        }
        //Load world data and log time taken
        WorldUtils.StartTrackingTime("worldData");
        worldData = WorldUtils.LoadWorld(worldName);
        Debug.Log("Load World Data: " + WorldUtils.GetTimeTracked("worldData").TotalMilliseconds + "ms");
        //Refresh the World and log time taken
        WorldUtils.StartTrackingTime("worldRefresh");
        RefreshWorld();
        Debug.Log("Refresh World: " + WorldUtils.GetTimeTracked("worldRefresh").TotalMilliseconds + "ms");
        //Set up values for game logic
        tickDeltaTime = 1 / worldData.info.tickRate;
        tickTimer = 0;
        //Initialise Resources
        ResourcesManager.Initialise();
        //Call OnWorldLoaded wherever needed and log time taken
        WorldUtils.StartTrackingTime("OnWorldLoadedTotal");
        foreach (WorldUtils.MapInfo mi in WorldUtils.mapInfos)
        {
            if (mi.receiveOnWorldLoaded)
            {
                OnWorldLoaded.AddListener(mi.mapReference.OnWorldLoaded);
            }
        }
        OnWorldLoaded.Invoke();
        Debug.Log("OnWorldLoaded Calls: " + WorldUtils.GetTimeTracked("OnWorldLoadedTotal").TotalMilliseconds + "ms");
    }

    public void OnResourceManagerInitialised()
    {
        AddBasicResourcesToManager();
    }

    public void AddBasicResourcesToManager()
    {
        ResourcesManager.AddResource("Population", 10); //Total population. Please make sure to always adjust this when adjusting sub-categories such as Labour, otherwise inconsistencies may arise.
        //Population Subcategories - One person may be in none, all, or anything in-between, the following subcategories:
        ResourcesManager.AddResource("Population_Educated_Basic", 10); //People who have received basic education. This pool is the upper limit to how many people can be in any higher education-required category e.g. Educated or Master Labour categories.
        ResourcesManager.MarkResourceAsPopulationSubcategory("Population_Educated_Basic");
        //Labour is the amount of workers available.
        ResourcesManager.AddResource("Param_Labour_Fraction", 0.4f); //Population multiplied by this equals the amount of people able to do Basic Labour. Mostly for policies like working age etc.
        ResourcesManager.AddResource("Labour_Basic", 0); //Basic Labour - Building, Production, etc.
        ResourcesManager.MarkResourceAsExpression("Labour_Basic", "Population*Param_Labour_Fraction"); //Declare that Basic Labour is equal to Population*Labour_Fraction.
        ResourcesManager.AddResource("Labour_Master", 0); //Labourers who have mastered their craft. Needed for top-level infrastructure etc.
        ResourcesManager.MarkResourceAsPopulationSubcategory("Labour_Master");
        ResourcesManager.AddResource("Labour_Educated_Planning", 0); //Labourers who have education and skills needed for more advanced tasks e.g. medium-level and higher infrastructure etc.
        ResourcesManager.MarkResourceAsPopulationSubcategory("Labour_Educated_Planning");
        //Raw Materials
        ResourcesManager.AddResource("Food", 1000); //Each person consumes a base amount of 1 unit of food per 20 ticks. This amount can vary with policies. If on a given tick there is less food than population, 5% of the unfed population will die.
        ResourcesManager.AddResource("Wood", 0);
        ResourcesManager.AddResource("Stone", 0);
        ResourcesManager.AddResource("Bricks", 0);
        ResourcesManager.AddResource("Concrete", 0);
        ResourcesManager.AddResource("Metals_Basic_Raw", 0); //Iron, copper, etc. Ores
        ResourcesManager.AddResource("Metals_Basic_Processed", 0); //Iron, copper, etc. Processed
        ResourcesManager.AddResource("Steel", 0);
        ResourcesManager.AddResource("Metals_Advanced_Raw", 0); //Aluminium, etc. Ore
        ResourcesManager.AddResource("Metals_Advanced_Processed", 0); //Aluminium, etc. Processed
        ResourcesManager.AddResource("Power_Requested", 0); //Power needed this tick. Resets/Recalculates every tick.
        ResourcesManager.AddResource("Coal", 0);
        ResourcesManager.AddResource("Oil", 0);
    }

    void FixedUpdate()
    {
        //Ticks
        if (!SimuationPaused)
        {
            tickTimer += Time.fixedDeltaTime;
            while(tickTimer > tickDeltaTime)
            {
                tickTimer -= tickDeltaTime;
                OnWorldTick.Invoke();
            }
        }
    }

    void Update()
    {
        /*if (worldData.mapData.elevation.refreshTexNextFrame) RefreshMap("elevation");
        if (worldData.mapData.fertility.refreshTexNextFrame) RefreshMap("fertility");
        if (worldData.mapData.flora_trees.refreshTexNextFrame) RefreshMap("trees");
        if (worldData.mapData.climate.refreshTexNextFrame) RefreshMap("climate");*/
        foreach (WorldUtils.MapInfo mi in WorldUtils.mapInfos) {
            if (mi.mapReference.refreshTexNextFrame) RefreshMap(mi);
        }

        //No Shift, No Ctrl
        if (!Input.GetKey(KeyCode.LeftShift) && !(Input.GetKey(KeyCode.LeftControl) || Application.isEditor && Input.GetKey(KeyCode.BackQuote)))
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                worldRend.DrawWorld(worldRend.drawType);
            }
            if(Input.GetKeyDown(KeyCode.Space))
            {
                PauseSimulation(!SimuationPaused);
            }
        }
        //Shift, No Ctrl
        if (Input.GetKey(KeyCode.LeftShift) && !(Input.GetKey(KeyCode.LeftControl) || Application.isEditor && Input.GetKey(KeyCode.BackQuote)))
        {
        }
        //Ctrl, No Shift
        if (!Input.GetKey(KeyCode.LeftShift) && (Input.GetKey(KeyCode.LeftControl) || Application.isEditor && Input.GetKey(KeyCode.BackQuote)))
        {
            if(Input.GetKeyDown(KeyCode.S))
            {
                WorldUtils.SaveWorld(worldData);
            }
            if (Input.GetKeyDown(KeyCode.A))
            {
                foreach(string s in ResourcesManager.ResourceList.Keys)
                {
                    WorldPlayer.instance.ResourceInventory.SetResource(s, 100000, true);
                }
            }
        }
        //Shift, Ctrl
        if (Input.GetKey(KeyCode.LeftShift) && (Input.GetKey(KeyCode.LeftControl) || Application.isEditor && Input.GetKey(KeyCode.BackQuote)))
        {
            if(Input.GetKeyDown(KeyCode.R))
            {
                WorldUtils.StartTrackingTime("ReloadWorld");
                LoadWorld(WorldToLoad);
                Debug.Log(">>Reload World Total: " + WorldUtils.GetTimeTracked("ReloadWorld").TotalMilliseconds + "ms<<");
            }
        }
    }

    public void OnTick()
    {
        for(int i = 0; i < worldData.info.RandomTickRate; i++)
        {
            RandomTick();
        }
        foreach (WorldUtils.MapInfo mi in WorldUtils.mapInfos)
        {
            if (mi.receiveNormalTicks)
            {
                mi.mapReference.OnTick(this);
            }
        }
    }

    public void RandomTick()
    {
        RandomTickPos(new Vector2Int(Random.Range(0, worldData.mapData.mapSize.x), Random.Range(0, worldData.mapData.mapSize.y)));
    }
    public void RandomTickPos(Vector2Int pos)
    {
        foreach (WorldUtils.MapInfo mi in WorldUtils.mapInfos)
        {
            if (mi.receiveRandomTicks)
            {
                mi.mapReference.OnRandomTick(pos, this);
            }
        }
    }

    public void PauseSimulation(bool pause)
    {
        SimuationPaused = pause;
        tickDeltaTime = 1 / worldData.info.tickRate; //recalculate tickDeltaTime for convenience when testing.
    }

    //Tile Value Calculations:
    public byte GetEffectiveFertilityAtPos(Vector2Int pos)
    {
        Map_Fertility fertilityMap = worldData.mapData.fertility;
        byte rawFertility = fertilityMap.GetRawFertilityAtPos(pos);
        float alt = worldData.mapData.elevation.GetAltitudeAtPos(pos, true);
        float temp = GetEffectiveTempAtPos(pos);
        byte hum = GetEffectiveHumidityAtPos(pos);
        return GetEffectiveFertilityAtAltitude(rawFertility, alt, temp, hum);
    }
    public byte GetEffectiveFertilityAtAltitude(byte rawFertility, float altASL, float temp, byte hum)
    {
        byte effectiveFertility = rawFertility;
        if (altASL <= 0) effectiveFertility = 0; //0 if under sea level.
        else
        {
            //Drop fertility by difference between temp and optimal temp, multiplied by the rate at which it should drop per degree:
            effectiveFertility = (byte)Mathf.Clamp(effectiveFertility - Mathf.Abs(worldData.info.fertOptimalTemperature - temp)*worldData.info.fertDropPerDegree, 0, 255);
            if(altASL > worldData.info.fertDropPerDegree_BonusStartAlt)
            {
                float penaltyDist = altASL - worldData.info.fertDropPerDegree_BonusStartAlt;
                effectiveFertility = (byte)Mathf.Clamp(effectiveFertility - (penaltyDist/100f)*worldData.info.fertDropPerDegree_BonusPenaltyPer100m, 0, 255);
            }
            //Apply humidity calculations
            byte humDifference = (byte)Mathf.Abs(worldData.info.fertOptimalHumidity - hum);
            if(humDifference > worldData.info.fertOptimalHumidityLeniency)
            {
                //Drop fertility if too arid or humid
                effectiveFertility = (byte)Mathf.Clamp(effectiveFertility - (humDifference - worldData.info.fertOptimalHumidityLeniency)*worldData.info.fertDropPerHumidity, 0, 255);
            }
        }
        return effectiveFertility;
    }
    public byte[] GetAllEffectiveFertilities(float[] allAlts = null)
    {
        if (allAlts == null) allAlts = worldData.mapData.elevation.GetAllAltitudes(true);
        byte[] fertilities = (byte[])worldData.mapData.fertility.mapBrightnesses.Clone();
        int length = fertilities.Length;
        float[] allTemps = GetAllTemperatures(allAlts);
        byte[] allHums = GetAllHumidities(allAlts);
        //Calc effective fertilities
        for(int i = 0; i < length; i++)
        {
            fertilities[i] = GetEffectiveFertilityAtAltitude(fertilities[i], allAlts[i], allTemps[i], allHums[i]); //convert from raw to effective
        }
        return fertilities;
    }

    public float GetEffectiveTempAtPos(Vector2Int pos)
    {
        return GetEffectiveTempAtAlt(worldData.mapData.climate.GetRawTempAtPos(pos), worldData.mapData.elevation.GetAltitudeAtPos(pos));
    }
    public float GetEffectiveTempAtAlt(float rawTemp, float altASL)
    {
        float tempDueToAltitude = -worldData.info.tempDropPerMetre * Mathf.Abs(altASL);
        return rawTemp + tempDueToAltitude;
    }
    public float[] GetAllTemperatures(float[] altitudes = null)
    {
        if(altitudes == null) altitudes = worldData.mapData.elevation.GetAllAltitudes(true);
        int length = altitudes.Length;
        float[] temps = worldData.mapData.climate.GetAllRawTemperatures();
        for(int i = 0; i < length; i++)
        {
            temps[i] = GetEffectiveTempAtAlt(temps[i], altitudes[i]);
        }
        return temps;
    }

    public byte GetEffectiveHumidityAtPos(Vector2Int pos)
    {
        return GetEffectiveHumidityAtAlt(worldData.mapData.climate.GetRawHumidityAtPos(pos), worldData.mapData.elevation.GetAltitudeAtPos(pos));
    }
    public byte GetEffectiveHumidityAtAlt(byte rawHum, float altASL)
    {
        if (altASL <= 0) return 255;
        else
        {
            float lossDueToAlt = Mathf.Min(worldData.info.humDropPerMetre * altASL, worldData.info.maxHumAltLoss);
            return (byte)Mathf.Clamp(rawHum - lossDueToAlt, 0, 255);
        } 
    }
    public byte[] GetAllHumidities(float[] altitudes = null)
    {
        if (altitudes == null) altitudes = worldData.mapData.elevation.GetAllAltitudes(true);
        int length = altitudes.Length;
        byte[] hums = worldData.mapData.climate.GetAllRawHumidities();
        for (int i = 0; i < length; i++)
        {
            hums[i] = GetEffectiveHumidityAtAlt(hums[i], altitudes[i]);
        }
        return hums;
    }

    public void RefreshWorld()
    {
        /*RefreshMap("elevation");
        RefreshMap("fertility");
        RefreshMap("trees");
        RefreshMap("climate");*/
        foreach (WorldUtils.MapInfo mi in WorldUtils.mapInfos) {
            RefreshMap(mi);
        }
        worldRend.RecalcMaxValues();
        worldData.mapData.flora_trees.RecalcAllFloraAreValid(this, false);
    }
    public void RefreshMap(WorldUtils.MapInfo mapInfo)
    {
        /*switch (mapType)
        {
            case "elevation":
                if(worldData.mapData.elevation.writeCacheToTexNextRefresh) worldData.mapData.elevation.RefreshPixelCache();
                worldData.mapData.elevation.RefreshTexture();
                break;
            case "fertility":
                if (worldData.mapData.fertility.writeCacheToTexNextRefresh) worldData.mapData.fertility.RefreshPixelCache();
                worldData.mapData.fertility.RefreshTexture();
                break;
            case "trees":
                if (worldData.mapData.flora_trees.writeCacheToTexNextRefresh) worldData.mapData.flora_trees.RefreshPixelCache();
                worldData.mapData.flora_trees.RefreshTexture();
                break;
            case "climate":
                if (worldData.mapData.climate.writeCacheToTexNextRefresh) worldData.mapData.climate.RefreshPixelCache();
                worldData.mapData.climate.RefreshTexture();
                break;
            default:
                break;
        }*/
        if (mapInfo.mapReference.writeCacheToTexNextRefresh) mapInfo.mapReference.RefreshPixelCache();
        mapInfo.mapReference.RefreshTexture();
    }
}
