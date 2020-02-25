using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Map {
    public Texture2D mapTex { get; protected set; }
    public Color32[] mapPixels { get; protected set; }
    public byte[] mapBrightnesses { get; protected set; }
    public int colourForBrightness = 0; //r, g, b, a
    protected int width = 0;
    protected int height = 0;
    public bool writeCacheToTexNextRefresh = false;
    public bool refreshTexNextFrame = false;
    
    //Utility
    public bool IsPosOnMap(Vector2Int pos)
    {
        return pos.x >= 0 && pos.x < mapTex.width && pos.y >= 0 && pos.y < mapTex.height;
    }
    public Vector2Int ClampPos(Vector2Int pos)
    {
        return new Vector2Int
        (
            Mathf.Clamp(pos.x, 0, mapTex.width - 1), Mathf.Clamp(pos.y, 0, mapTex.height - 1)
        );
    }
    //Get information
    public Color32 GetColourAtPos(Vector2Int pos)
    {
        //try
        //{
            return mapPixels[WorldUtils.TexPosToIndex(pos, width, height)];
        //}
        //catch (System.Exception e)
        //{
            //Debug.LogError("Could not Get Pixel " + pos + " - position out of bounds.");
            //return new Color32(0, 0, 0, 0);
        //}
    }
    public byte GetColourAtPos(Vector2Int pos, int colour)
    {
        //try
        //{
        if(colour == 0)
            return mapPixels[WorldUtils.TexPosToIndex(pos, width, height)].r;
        if (colour == 1)
            return mapPixels[WorldUtils.TexPosToIndex(pos, width, height)].g;
        if (colour == 2)
            return mapPixels[WorldUtils.TexPosToIndex(pos, width, height)].b;
        if (colour == 3)
            return mapPixels[WorldUtils.TexPosToIndex(pos, width, height)].a;
        return 0;
        //}
        //catch (System.Exception e)
        //{
        //Debug.LogError("Could not Get Pixel " + pos + " - position out of bounds.");
        //return new Color32(0, 0, 0, 0);
        //}
    }

    public byte GetBrightnessAtPos(Vector2Int pos)
    {
        return mapBrightnesses[WorldUtils.TexPosToIndex(pos, width, height)];
    }
    //Set information
    public void SetColourAtPos(Vector2Int pos, Color32 pixel, WorldRenderer rend = null)
    {
        SetColourAtIndex(WorldUtils.TexPosToIndex(pos, width, height), pixel, rend);
    }
    public void SetColourAtIndex(int index, Color32 pixel, WorldRenderer rend = null)
    {
        mapPixels[index] = pixel;
        RefreshPixelCacheSingle(index, true);
        writeCacheToTexNextRefresh = true;
        refreshTexNextFrame = true;
        if(rend != null)
        {
            rend.DrawWorldPixel(WorldUtils.TexIndexToPos(index, width, height), rend.drawType);
        }
    }

    //Texture-wide
    public void SetTexture(Texture2D tex)
    {
        writeCacheToTexNextRefresh = false;
        if (!mapTex) mapTex = new Texture2D(tex.width, tex.height);
        mapTex.Resize(tex.width, tex.height, TextureFormat.ARGB32, false);
        mapTex.SetPixels32(tex.GetPixels32());
        RefreshTexture();
        RefreshPixelCache();
    }
    public void RefreshPixelCache()
    {
        width = mapTex.width;
        height = mapTex.height;
        if (!writeCacheToTexNextRefresh) //Write to Cache, read from Tex
        {
            mapPixels = mapTex.GetPixels32();
            int length = mapPixels.Length;
            mapBrightnesses = new byte[length];
            for (int i = 0; i < length; i++)
            {
                if (colourForBrightness == 0)
                    mapBrightnesses[i] = mapPixels[i].r;
                else if (colourForBrightness == 1)
                    mapBrightnesses[i] = mapPixels[i].g;
                else if (colourForBrightness == 2)
                    mapBrightnesses[i] = mapPixels[i].b;
                else if (colourForBrightness == 3)
                    mapBrightnesses[i] = mapPixels[i].a;
            }
        }
        else //Write to Tex, read from Cache
        {
            mapTex.SetPixels32(mapPixels);
            writeCacheToTexNextRefresh = false;
        }
    }
    /// <param name="refreshFromCache">Use this if you need to re-sync values such as brightness.</param>
    public void RefreshPixelCacheSingle(Vector2Int pos, bool refreshFromCache = false)
    {
        int index = WorldUtils.TexPosToIndex(pos, width, height);
        RefreshPixelCacheSingle(index, refreshFromCache);
    }
    /// <param name="refreshFromCache">Use this if you need to re-sync values such as brightness.</param>
    public void RefreshPixelCacheSingle(int index, bool refreshFromCache = false)
    {
        if (!refreshFromCache)
        {
            Vector2Int pos = WorldUtils.TexIndexToPos(index, width, height);
            mapPixels[index] = mapTex.GetPixel(pos.x, pos.y);
            if (colourForBrightness == 0)
                mapBrightnesses[index] = mapPixels[index].r;
            else if (colourForBrightness == 1)
                mapBrightnesses[index] = mapPixels[index].g;
            else if (colourForBrightness == 2)
                mapBrightnesses[index] = mapPixels[index].b;
            else if (colourForBrightness == 3)
                mapBrightnesses[index] = mapPixels[index].a;
        }
        else
        {
            if (colourForBrightness == 0)
                mapBrightnesses[index] = mapPixels[index].r;
            if (colourForBrightness == 1)
                mapBrightnesses[index] = mapPixels[index].g;
            if (colourForBrightness == 2)
                mapBrightnesses[index] = mapPixels[index].b;
            if (colourForBrightness == 3)
                mapBrightnesses[index] = mapPixels[index].a;
        }
    }
    public void RefreshTexture()
    {
        refreshTexNextFrame = false;
        mapTex.Apply();
    }

    //World Interaction
    public virtual void OnRandomTick(Vector2Int pos, WorldManager mgr)
    {
    
    }
    public virtual void OnTick(WorldManager mgr)
    {

    }
    public virtual void OnWorldLoaded()
    {

    }
}

[System.Serializable]
public class Map_Elevation : Map
{
    public float altitudeScale = 10f; //1 unit (0-255) equals this many metres.
    public float seaLevel = 400f;

    //Get information
    public float GetAltitudeAtPos(Vector2Int pos, bool AboveSeaLevel = true)
    {
        return GetAltitudeAtBrightness(GetBrightnessAtPos(pos), AboveSeaLevel);
    }
    public float GetAltitudeAtBrightness(byte brightness, bool AboveSeaLevel = true)
    {
        return (brightness * altitudeScale) - (AboveSeaLevel ? seaLevel : 0);
    }

    public float[] GetAllAltitudes(bool AboveSeaLevel = true)
    {
        int length = mapBrightnesses.Length;
        float[] altitudes = new float[length];
        float seaLevelSubtraction = AboveSeaLevel ? seaLevel : 0;
        for(int i = 0; i < length; i++)
        {
            altitudes[i] = (mapBrightnesses[i] * altitudeScale) - seaLevelSubtraction;
        }
        return altitudes;
    }

    //Constructor
    public Map_Elevation()
    {
        altitudeScale = 10f;
        seaLevel = 400f;
    }
}

[System.Serializable]
public class Map_Fertility : Map
{
    public short fert0FloraChange = -255; //How much lifetime (of max 255) does the flora on a fertility 0 tile lose/gain after a random tick?
    public short fert255FloraChange = 255; //How much lifetime (of max 255) does the flora on a fertility 255 tile lose/gain after a random tick?

    //Get Information
    public byte GetRawFertilityAtPos(Vector2Int pos) //Raw fertility doesn't take into account things such as climate or if the tile is under water.
    {
        return GetRawFertilityAtBrightness(GetBrightnessAtPos(pos));
    }
    public byte GetRawFertilityAtBrightness(byte brightness)
    {
        return brightness;
    }

    /// <summary>
    /// This is intended to be used with Effective Fertility.
    /// </summary>
    public short GetFloraChangeFromFertility(byte fertility)
    {
        return (short)Mathf.Clamp(Mathf.Lerp(fert0FloraChange, fert255FloraChange, fertility / 255f), -255, 255);
    }

    public byte[] GetAllRawFertilities()
    {
        return mapBrightnesses;
    }
    //Constructor
    public Map_Fertility()
    {
        fert0FloraChange = -255;
        fert255FloraChange = 255;
    }
}

[System.Serializable]
public class Map_Flora : Map //a = density, g = lifetime
{
    //Lifetime
    public byte startLifetime = 64; //Lifetime of a newly spawned tile.
    public byte startLifetimeRandom = 12; //Maximum random deviation from start lifetime.
    //Spreading
    public float spreadChance = 0.1f; //Chance per random tick to spread to an adjacent tile.
    public byte minSpreadDensity = 32; //Minimum density to spread to an adjacent tile.
    public float childDensityMult = 0.1f; //When flora spreads to another tile, how much density does it have compared to the parent?

    //Get Information
    public Color32 GetFloraInfoAtPos(Vector2Int pos)
    {
        return mapPixels[WorldUtils.TexPosToIndex(pos, width, height)];
    }
    public Color32 GetFloraInfoAtIndex(int index)
    {
        return mapPixels[index];
    }

    public Color32[] GetAllFloraInfos()
    {
        return mapPixels;
    }

    //Random Tick
    public override void OnRandomTick(Vector2Int pos, WorldManager mgr)
    {
        Color32 floraInfo = GetFloraInfoAtPos(pos);

        float lifetime = floraInfo.g;
        float density = floraInfo.a;
        if (lifetime == 0)
        {
            if (density != 0)
            {
                density = 0;
                floraInfo.a = 0;
            }
        }
        else if (density == 0)
        {
            if (lifetime != 0)
            {
                lifetime = 0;
                floraInfo.g = 0;
            }
        }
        else //If density/lifetime are not 0, do Fertility effects calculations
        {
            byte fertility = mgr.GetEffectiveFertilityAtPos(pos);
            if (fertility == 0) lifetime = 0;
            else
            {
                lifetime += mgr.worldData.mapData.fertility.GetFloraChangeFromFertility(fertility);
            }

            //Spread calc:
            if (lifetime > 0 && density >= minSpreadDensity && spreadChance > 0) //Can it spread?
            {
                float spreadChanceTemp = spreadChance;
                while (spreadChanceTemp > 0)
                {
                    if (Random.Range(0f, 1f) <= spreadChanceTemp) //Will it spread?
                    {
                        TrySpread(pos, mgr);
                    }
                    spreadChanceTemp--;
                }
            }
            if (lifetime == 0) density = 0; //If the lifetime dropped to 0 due to infertility, this tile will be cleared.
            else if (density > lifetime) density = lifetime; //density can not be higher than lifetime.
            floraInfo.g = (byte)Mathf.Clamp(lifetime, 0, 255);
            floraInfo.a = (byte)Mathf.Clamp(density, 0, 255);
        }
        SetColourAtPos(pos, floraInfo, mgr.worldRend);
    }

    public void TrySpread(Vector2Int pos, WorldManager mgr)
    {
        Vector2Int spreadPos = RandAdjacentPos(pos);
        if(PosIsValid(spreadPos, mgr))
        {
            Color32 floraInfo = GetFloraInfoAtPos(pos);
            FloraAddLifetime(spreadPos, (byte)Mathf.Clamp(startLifetime + Random.Range(-minSpreadDensity, minSpreadDensity + 1), 0, 255), mgr.worldRend);
            FloraAddDensity(spreadPos, (byte)Mathf.Clamp(Mathf.Max(floraInfo.a * childDensityMult, 1), 0, 255), mgr.worldRend);
        }
    }
    public Vector2Int RandAdjacentPos(Vector2Int originPos)
    {
        int minx = originPos.x != 0 ? -1 : 0;
        int maxx = originPos.x != width-1 ? 1 : 0;
        int miny = originPos.y != 0 ? -1 : 0;
        int maxy = originPos.y != height-1 ? 1 : 0;

        return originPos + new Vector2Int(Random.Range(minx, maxx + 1), Random.Range(miny, maxy + 1));
    }
    public bool PosIsValid(Vector2Int pos, WorldManager mgr)
    {
        if(!IsPosOnMap(pos))
        {
            return false;
        }
        if(mgr.worldData.mapData.elevation.GetAltitudeAtPos(pos) <= 0) //Underwater? Invalid Pos.
        {
            return false;
        }
        return true;
    }

    public void FloraAddDensity(Vector2Int pos, byte amount, WorldRenderer rend = null)
    {
        Color32 floraInfo = GetFloraInfoAtPos(pos);
        floraInfo.a = (byte)Mathf.Clamp(floraInfo.a + amount, 0, 255);
        SetColourAtPos(pos, floraInfo, rend);
    }
    public void FloraAddLifetime(Vector2Int pos, byte amount, WorldRenderer rend = null)
    {
        Color32 floraInfo = GetFloraInfoAtPos(pos);
        floraInfo.g = (byte)Mathf.Clamp(floraInfo.g + amount, 0, 255);
        SetColourAtPos(pos, floraInfo, rend);
    }

    public void RecalcAllFloraAreValid(WorldManager mgr, bool forceRandTick = false)
    {
        WorldUtils.StartTrackingTime("RecalcAllFloraAreValid");
        Color32 colClear = new Color32(0, 0, 0, 0);
        for(int y = 0; y < height; y++)
        {
            for(int x = 0; x < width; x++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                Color32 flora = GetFloraInfoAtPos(pos);
                if (flora.a == 0 && flora.g == 0) continue;
                if (!PosIsValid(pos, mgr))
                {
                    SetColourAtPos(pos, colClear, mgr.worldRend);
                }
                else if(forceRandTick)
                {
                    OnRandomTick(pos, mgr);
                }
            }
        }
        Debug.Log("---Map_Flora.RecalcAllFloraAreValid: " + WorldUtils.GetTimeTracked("RecalcAllFloraAreValid").TotalMilliseconds + "ms---");
    }

    public override void OnWorldLoaded()
    {
        RecalcAllFloraAreValid(WorldManager.instance, true);
    }

    //Constructor
    public Map_Flora()
    {
        startLifetime = 64;
        startLifetimeRandom = 12;
        spreadChance = 0.1f;
        minSpreadDensity = 32;
        childDensityMult = 0.25f;
        colourForBrightness = 1;
    }
}

[System.Serializable]
public class Map_Climate : Map //r = raw temperature (degrees C), b = raw humidity (0-255)
{
    public float rawTempScale = 0.2f;
    /// <summary>
    /// This is added to the red component of the map to calculate the raw temperature before applying rawTempScale.
    /// </summary>
    public short rawTempUnscaledOffset = -64;

    //Get information
    public float GetRawTempAtPos(Vector2Int pos)
    {
        return GetRawTempAtRedness(GetColourAtPos(pos, 0));
    }
    public float GetRawTempAtIndex(int index)
    {
        return GetRawTempAtRedness(mapPixels[index].r);
    }
    public float GetRawTempAtRedness(byte red)
    {
        return (red + rawTempUnscaledOffset) * rawTempScale;
    }
    public float[] GetAllRawTemperatures()
    {
        int length = mapPixels.Length;
        float[] temps = new float[length];
        for (int i = 0; i < length; i++)
        {
            temps[i] = GetRawTempAtRedness(mapPixels[i].r);
        }
        return temps;
    }

    public byte GetRawHumidityAtPos(Vector2Int pos)
    {
        return GetRawHumidityAtBlueness(GetColourAtPos(pos, 2));
    }
    public byte GetRawHumidityAtBlueness(byte blue)
    {
        return blue;
    }
    public byte[] GetAllRawHumidities()
    {
        int length = mapPixels.Length;
        byte[] hums = new byte[length];
        for(int i = 0; i < length; i++)
        {
            hums[i] = GetRawHumidityAtBlueness(mapPixels[i].b);
        }
        return hums;
    }

    //Constructor
    public Map_Climate()
    {
        rawTempScale = 0.2f;
        rawTempUnscaledOffset = -64;
    }
}

[System.Serializable]
public class Map_Infrastructure : Map
{
    //Get information
    public byte GetInfraAtPos(Vector2Int pos)
    {
        return GetInfraAtAlpha(GetColourAtPos(pos, 3));
    }
    public byte GetInfraAtAlpha(byte alpha)
    {
        return alpha;
    }
    public byte[] GetAllInfras()
    {
        int length = mapPixels.Length;
        byte[] infras = new byte[length];
        for (int i = 0; i < length; i++)
        {
            infras[i] = mapPixels[i].a;
        }
        return infras;
    }

    public byte GetLushnessAtPos(Vector2Int pos)
    {
        return GetLushnessAtGreen(GetColourAtPos(pos, 1));
    }
    public byte GetLushnessAtGreen(byte green)
    {
        return green;
    }
    public byte[] GetAllLushnesses()
    {
        int length = mapPixels.Length;
        byte[] lushnesses = new byte[length];
        for (int i = 0; i < length; i++)
        {
            lushnesses[i] = mapPixels[i].g;
        }
        return lushnesses;
    }

    public enum InfrastructureLevel
    {
        //Values equal to maximum infrastructure elegible for that level. e.g. Tile is village at 44 infra but town at 45.
        WILDERNESS = 0,
        VILLAGE = 44,
        TOWN = 119,
        INDUSTRIAL = 164,
        MODERN = 194,
        ADVANCED = 224,
        METROPOLIS = 254,
        REVOLUTIONARY = 255
    }
    public InfrastructureLevel GetLevelFromInfra(byte infra)
    {
        if (infra <= (byte)InfrastructureLevel.WILDERNESS) return InfrastructureLevel.WILDERNESS;
        if (infra <= (byte)InfrastructureLevel.VILLAGE) return InfrastructureLevel.VILLAGE;
        if (infra <= (byte)InfrastructureLevel.TOWN) return InfrastructureLevel.TOWN;
        if (infra <= (byte)InfrastructureLevel.INDUSTRIAL) return InfrastructureLevel.INDUSTRIAL;
        if (infra <= (byte)InfrastructureLevel.MODERN) return InfrastructureLevel.MODERN;
        if (infra <= (byte)InfrastructureLevel.ADVANCED) return InfrastructureLevel.ADVANCED;
        if (infra <= (byte)InfrastructureLevel.METROPOLIS) return InfrastructureLevel.METROPOLIS;
        if (infra <= (byte)InfrastructureLevel.REVOLUTIONARY) return InfrastructureLevel.REVOLUTIONARY;
        return InfrastructureLevel.WILDERNESS;
    }

    //Modify
    public void AddInfra(short amt, Vector2Int pos, WorldRenderer rend = null)
    {
        Color32 newCol = GetColourAtPos(pos);
        newCol.a = (byte)Mathf.Clamp(newCol.a + amt, 0, 255);
        SetColourAtPos(pos, newCol, rend);
    }
    public void AddLushness(short amt, Vector2Int pos, WorldRenderer rend = null)
    {
        Color32 newCol = GetColourAtPos(pos);
        newCol.g = (byte)Mathf.Clamp(newCol.g + amt, 0, 255);
        SetColourAtPos(pos, newCol, rend);
    }

    //World Tick (Deprecated)
    /*int posCounter = 0;
    public override void OnTick(WorldManager mgr)
    {
        for (int i = 0; i < 1500; i++)
        {
            Vector2Int pos = WorldUtils.TexIndexToPos(posCounter % (width * height), width, height);
            posCounter++;
            byte infra = GetInfraAtPos(pos);
            if (infra == 0) continue; //Skip wilderness

            if (GetLevelFromInfra(infra) <= InfrastructureLevel.TOWN)
            {
                float foodProduction = infra * 100;
                WorldPlayer.instance.ResourceInventory.SetResource("Food", foodProduction, true, 0, WorldPlayer.instance.ResourceInventory.GetResource("Population") * 1800);
            }
        }
    }*/

    //Constructor
    public Map_Infrastructure()
    {

    }
}