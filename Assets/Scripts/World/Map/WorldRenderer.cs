using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldRenderer : MonoBehaviour {

    public WorldManager worldMgr;
    public WorldData data
    {
        get { return worldMgr.worldData; }
    }

    public MeshRenderer rend_World;
    public Texture2D tex_World;

    bool texChanged = false;
    float maximumAlt = 255f;
    float minimumTemperature = 0f;
    float maximumTemperature = 0f;

    [Header("Render Settings")]
    public WorldDrawType drawType = WorldDrawType.NORMAL;
    public Color32 elv_SeaColour = new Color32(0, 50, 220, 255);
    public Gradient elv_AltColourGradientMaxFert = new Gradient(); //0.00...01m Above Sea Level - Gradient at t=0. Max Altitude - Gradient at t=1
    public Gradient elv_AltColourGradientMinFert = new Gradient(); //These two are interpolated based on fertility.
    public Color32 trees_ColourLife0 = new Color32(220, 240, 230, 255);
    public Color32 trees_ColourLife255 = new Color32(0, 175, 25, 255);
    public float clim_FullSnowTemp = -4f;
    public Gradient clim_SeaTintByTemp = new Gradient();
    public float clim_TempSeaColourBlendWeight = 0.5f;
    public Gradient infra_ColourByInfraMinLush = new Gradient();
    public Gradient infra_ColourByInfraMaxLush = new Gradient();

    public void StartManual() {
        WorldManager.OnWorldLoaded.AddListener(OnWorldLoaded);
        tex_World = new Texture2D(1,1);
        tex_World.filterMode = FilterMode.Point;
        rend_World.material.mainTexture = tex_World;
	}
    void OnWorldLoaded()
    {
        RecalcMaxValues();
        SyncTexAspects();
        DrawWorld();
    }
    /// <summary>
    /// Synchronise the Texture used for Rendering with the MapData e.g. resolution
    /// </summary>
    public void SyncTexAspects()
    {
        rend_World.transform.localScale = new Vector3(data.mapData.mapSize.x, data.mapData.mapSize.y, 1f);
        tex_World.Resize(data.mapData.mapSize.x , data.mapData.mapSize.y, TextureFormat.ARGB32, false);
        tex_World.Apply();
    }

    private void Update()
    {
        if(texChanged)
        {
            tex_World.Apply();
            texChanged = false;
        }
        /*for(int i = 0; i < 1000; i++) //Testing
        {
            DrawWorldPixel(new Vector2Int(Random.Range(0, data.mapData.mapSize.x), Random.Range(0, data.mapData.mapSize.y)), WorldDrawType.FERTILITY_EFFECTIVE);
        }*/
    }

    public enum WorldDrawType
    {
        NORMAL,
        ELEVATION,
        FERTILITY_RAW,
        FERTILITY_EFFECTIVE,
        FLORA_TREES,
        CLIMATE_RAW,
        CLIMATE_EFFECTIVE
    }
    public void DrawWorld(WorldDrawType drawType = WorldDrawType.NORMAL, bool refreshWorld = true)
    {
        if (refreshWorld) worldMgr.RefreshWorld();
        Color32[] colours = tex_World.GetPixels32();

        Map_Elevation elv = data.mapData.elevation;
        Map_Fertility fert = data.mapData.fertility;
        Map_Flora trees = data.mapData.flora_trees;
        Map_Climate clim = data.mapData.climate;
        Map_Infrastructure infrastructure = data.mapData.infrastructure;
        byte[] allBrightnesses = new byte[0];
        float[] allAlts = new float[0];
        Color32[] allColours = new Color32[0];
        float[] allTemps = new float[0];
        byte[] allHums = new byte[0];
        byte[] allInfras = new byte[0];
        byte[] allLushes = new byte[0];
        int w = tex_World.width;
        int h = tex_World.height;
        switch (drawType)
        {
            case WorldDrawType.NORMAL: //Normal
                allAlts = elv.GetAllAltitudes(true);
                allBrightnesses = worldMgr.GetAllEffectiveFertilities(allAlts);
                allColours = trees.GetAllFloraInfos();
                allTemps = worldMgr.GetAllTemperatures();
                allInfras = infrastructure.GetAllInfras();
                allLushes = infrastructure.GetAllLushnesses();
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = CalcColour_NORMAL(allAlts[i], allBrightnesses[i], allColours[i], allTemps[i], allInfras[i], allLushes[i]);
                }
                break;
            case WorldDrawType.ELEVATION: //Elevation
                allBrightnesses = elv.mapBrightnesses;
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = CalcColour_ELEVATION(allBrightnesses[i], elv);
                }
                break;
            case WorldDrawType.FERTILITY_RAW: //Raw Fertility
                allBrightnesses = fert.mapBrightnesses;
                allAlts = elv.GetAllAltitudes(true);
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = CalcColour_FERTILITY_RAW(allBrightnesses[i], allAlts[i] > 0);
                }
                break;
            case WorldDrawType.FERTILITY_EFFECTIVE: //Effective Fertility
                allAlts = elv.GetAllAltitudes(true);
                allBrightnesses = worldMgr.GetAllEffectiveFertilities(allAlts);
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = CalcColour_FERTILITY_EFFECTIVE(allBrightnesses[i], allAlts[i] > 0);
                }
                break;
            case WorldDrawType.FLORA_TREES: //Trees
                allColours = trees.GetAllFloraInfos();
                allAlts = elv.GetAllAltitudes();
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = CalcColour_FLORA_TREES(allColours[i], allAlts[i] > 0);
                }
                break;
            case WorldDrawType.CLIMATE_RAW: //Raw Climate
                allColours = clim.mapPixels;
                allAlts = elv.GetAllAltitudes();
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = CalcColour_CLIMATE_RAW(allColours[i], allAlts[i] > 0);
                }
                break;
            case WorldDrawType.CLIMATE_EFFECTIVE: //Effective Climate
                allAlts = elv.GetAllAltitudes();
                allTemps = worldMgr.GetAllTemperatures(allAlts);
                allHums = worldMgr.GetAllHumidities(allAlts);
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = CalcColour_CLIMATE_EFFECTIVE(allTemps[i], allHums[i], allAlts[i] > 0);
                }
                break;
            default:
                for (int i = 0; i < colours.Length; i++)
                {
                    colours[i] = colourTransparent;
                }
                break;
        }

        tex_World.SetPixels32(colours);
        texChanged = true;
    }

    public void DrawWorldPixel(Vector2Int pos, WorldDrawType drawType = WorldDrawType.NORMAL, bool refreshWorld = false)
    {
        if (refreshWorld) worldMgr.RefreshWorld();

        Map_Elevation elv = data.mapData.elevation;
        Map_Fertility fert = data.mapData.fertility;
        Map_Flora trees = data.mapData.flora_trees;
        Map_Climate clim = data.mapData.climate;
        Map_Infrastructure infrastructure = data.mapData.infrastructure;
        int w = tex_World.width;
        int h = tex_World.height;
        switch (drawType)
        {
            case WorldDrawType.NORMAL: //Normal
                tex_World.SetPixel(pos.x, pos.y, CalcColour_NORMAL(
                    elv.GetAltitudeAtPos(pos),
                    worldMgr.GetEffectiveFertilityAtPos(pos),
                    trees.GetFloraInfoAtPos(pos),
                    worldMgr.GetEffectiveTempAtPos(pos),
                    infrastructure.GetInfraAtPos(pos),
                    infrastructure.GetLushnessAtPos(pos)
                    ));
                break;
            case WorldDrawType.ELEVATION: //Elevation
                tex_World.SetPixel(pos.x, pos.y, CalcColour_ELEVATION(
                    elv.GetBrightnessAtPos(pos),
                    elv
                    ));
                break;
            case WorldDrawType.FERTILITY_RAW: //Raw Fertility
                tex_World.SetPixel(pos.x, pos.y, CalcColour_FERTILITY_RAW(
                    fert.GetBrightnessAtPos(pos),
                    elv.GetAltitudeAtPos(pos, true) > 0
                    ));
                break;
            case WorldDrawType.FERTILITY_EFFECTIVE: //Effective Fertility
                tex_World.SetPixel(pos.x, pos.y, CalcColour_FERTILITY_EFFECTIVE(
                    worldMgr.GetEffectiveFertilityAtPos(pos),
                    elv.GetAltitudeAtPos(pos) > 0
                    ));
                break;
            case WorldDrawType.FLORA_TREES: //Trees
                tex_World.SetPixel(pos.x, pos.y, CalcColour_FLORA_TREES(
                    trees.GetFloraInfoAtPos(pos),
                    elv.GetAltitudeAtPos(pos) > 0
                    ));
                break;
            case WorldDrawType.CLIMATE_RAW: //Raw Climate
                tex_World.SetPixel(pos.x, pos.y, CalcColour_CLIMATE_RAW(
                    clim.GetColourAtPos(pos),
                    elv.GetAltitudeAtPos(pos) > 0
                    ));
                break;
            case WorldDrawType.CLIMATE_EFFECTIVE: //Effective Climate
                tex_World.SetPixel(pos.x, pos.y, CalcColour_CLIMATE_EFFECTIVE(
                    worldMgr.GetEffectiveTempAtPos(pos),
                    worldMgr.GetEffectiveHumidityAtPos(pos),
                    elv.GetAltitudeAtPos(pos) > 0
                    ));
                break;
            default:
                tex_World.SetPixel(pos.x, pos.y, colourTransparent);
                break;
        }
        texChanged = true;
    }

    public Color32 CalcColour_NORMAL(float altitude, byte fertility, Color32 tree, float temp, byte infra, byte infra_lush)
    {
        //Draw terrain based on altitude ASL and tint based on fertility.
        Color32 newCol = new Color32();
        float normalisedAlt = altitude / maximumAlt;
        float normalisedTemp = ((temp - minimumTemperature) / (maximumTemperature - minimumTemperature));
        if(infra > 0)
        {
            newCol = Color32.Lerp(infra_ColourByInfraMinLush.Evaluate(infra / 255f), infra_ColourByInfraMaxLush.Evaluate(infra / 255f), infra_lush / 255f);
        }
        else if (normalisedAlt <= 0) //Underwater
        {
            newCol = Color32.Lerp(elv_SeaColour, clim_SeaTintByTemp.Evaluate(normalisedTemp), clim_TempSeaColourBlendWeight); //Get water colour tinted by the temperature.
        }
        else
        {
            Color32 maxFertCol = elv_AltColourGradientMaxFert.Evaluate(normalisedAlt);
            Color32 minFertCol = elv_AltColourGradientMinFert.Evaluate(normalisedAlt);
            newCol = Color32.Lerp(minFertCol, maxFertCol, fertility / 164f); //max fertility colour used at and past fertility 164.

            //Apply Temperature Tint (Snow)
            if (temp <= 0)
            {
                newCol = Color32.Lerp(newCol, colourWhite, temp/clim_FullSnowTemp);
            }

            //Draw Trees
            if (tree.a != 0) //only draw tree if density (also therefore lifetime) is not zero, aka this tile has trees.
            {
                Color32 treeCol = Color32.Lerp(trees_ColourLife0, trees_ColourLife255, tree.g / 255f); //treeCol depends on lifetime (green) aka healthiness
                newCol = Color32.Lerp(newCol, treeCol, tree.a / 255f); //drawn colour depends on density (alpha)
            }
        }

        return newCol;
    }
    public Color32 CalcColour_ELEVATION(byte altitudeRaw, Map_Elevation elv)
    {
        Color32 newCol = Color32.Lerp(colourBlack, colourWhite, altitudeRaw / 255f);
        if (altitudeRaw * elv.altitudeScale <= elv.seaLevel)
        {
            newCol.r = newCol.g = 0;
        }

        return newCol;
    }
    public Color32 CalcColour_FERTILITY_RAW(byte fertilityRaw, bool aboveSea)
    {
        Color32 newCol = Color32.Lerp(colourBrown, colourGreen, fertilityRaw / 255f);
        if(!aboveSea)
        {
            newCol.a = 96;
        }

        return newCol;
    }
    public Color32 CalcColour_FERTILITY_EFFECTIVE(byte fertilityEffective, bool aboveSea)
    {
        Color32 newCol = Color32.Lerp(colourBrown, colourGreen, fertilityEffective / 255f);
        if (!aboveSea)
        {
            newCol.a = 96;
        }
        else if (fertilityEffective == 0)
        {
            newCol = colourSilver;
        }

        return newCol;
    }
    public Color32 CalcColour_FLORA_TREES(Color32 floraInfo, bool aboveSea)
    {
        Color32 newCol = aboveSea ? colourBrown : elv_SeaColour;
        if (floraInfo.a != 0) //only draw tree if density (also therefore lifetime) is not zero, aka this tile has trees.
        {
            Color32 treeCol = Color32.Lerp(trees_ColourLife0, trees_ColourLife255, floraInfo.g / 255f); //treeCol depends on lifetime (green) aka healthiness
            newCol = Color32.Lerp(newCol, treeCol, floraInfo.a / 255f); //At density (alpha) = 0, colour is of ground. At density = 255, colour is of tree.
        }

        return newCol;
    }
    public Color32 CalcColour_CLIMATE_RAW(Color32 climateInfo, bool aboveSea)
    {
        return new Color32(climateInfo.r, 0, climateInfo.b, (byte)(aboveSea ? 255 : 192));
    }
    public Color32 CalcColour_CLIMATE_EFFECTIVE(float temperature, byte humidity, bool aboveSea)
    {
        return new Color32(
        (byte)(((temperature - minimumTemperature) / (maximumTemperature - minimumTemperature))*255),
        0,
        humidity,
        (byte)(aboveSea ? 255 : 96)
        );
    }

    public void RecalcMaxValues()
    {
        maximumAlt = data.mapData.elevation.GetAltitudeAtBrightness(255, true);
        float farthestAltFromSeaLevel = maximumAlt;
        if (data.mapData.elevation.seaLevel > maximumAlt) farthestAltFromSeaLevel = -data.mapData.elevation.seaLevel;
        minimumTemperature = worldMgr.GetEffectiveTempAtAlt(data.mapData.climate.GetRawTempAtRedness(0), farthestAltFromSeaLevel);
        maximumTemperature = worldMgr.GetEffectiveTempAtAlt(data.mapData.climate.GetRawTempAtRedness(255), 0);
    }

    Color32 colourWhite = new Color32(255, 255, 255, 255);
    Color32 colourBlack = new Color32(0, 0, 0, 255);
    Color32 colourTransparent = new Color32(0, 0, 0, 0);
    Color32 colourBrown = new Color32(222, 184, 135, 255);
    Color32 colourGreen = new Color32(50, 205, 50, 255);
    Color32 colourSilver = new Color32(180, 180, 180, 255);
}
