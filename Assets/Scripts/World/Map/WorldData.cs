using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[System.Serializable]
public class WorldInfo //This will be serialised.
{
    public string name = "Unnamed Map"; //also used for path. StreamingAssets/Worlds/name/name.minfo

    public float tickRate = 20f; //How many ticks per second?
    public int RandomTickRate = 250; //How many random ticks per tick?

    //Variables which are primarily used outside of their maps, e.g. WorldManager calculations.
    public byte fertOptimalTemperature = 25;
    public byte fertDropPerDegree = 7;
    public float fertDropPerDegree_BonusStartAlt = 600f;
    public byte fertDropPerDegree_BonusPenaltyPer100m = 20;
    public byte fertOptimalHumidity = 180;
    public byte fertOptimalHumidityLeniency = 64;
    public byte fertDropPerHumidity = 1;

    public float tempDropPerMetre = 0.01f;
    public float humDropPerMetre = 0.15f;
    public byte maxHumAltLoss = 64;

    //following fields are here just for saving map-specific variables. To access them, refer to their respective classes e.g. mapData.elevation.seaLevel
    public float elv_seaLevel = 64;
    public float elv_altitudeScale = 1f;

    public short fert_FloraChange0 = -255;
    public short fert_FloraChange255 = 255;

    public byte flora0_startLifetime = 64;
    public byte flora0_startLifetimeRandom = 12;
    public float flora0_spreadChance = 0.1f;
    public byte flora0_minSpreadDensity = 32;
    public float flora0_childDensityMult = 0.1f;

    public float climate_rawTempScale = 0.2f;
    public short climate_rawTempUnscaledOffset = -64;

    public WorldInfo()
    {
        name = "Unnamed Map";
    }

    /// <param name="layer">For single map type with multiple layers e.g. Flora trees, flora crops, etc.</param>
    public void SetValuesFromMap(Map m, int layer = 0)
    {
        Type type = m.GetType();
        if (type == typeof(Map_Elevation))
        {
            Map_Elevation elv = ((Map_Elevation)m);
            elv_seaLevel = elv.seaLevel;
            elv_altitudeScale = elv.altitudeScale;
        }
        else if (type == typeof(Map_Fertility))
        {
            Map_Fertility fert = ((Map_Fertility)m);
            fert_FloraChange0 = fert.fert0FloraChange;
            fert_FloraChange255 = fert.fert255FloraChange;
        }
        else if (type == typeof(Map_Flora))
        {
            if (layer == 0)
            {
                Map_Flora flora = ((Map_Flora)m);
                flora0_startLifetime = flora.startLifetime;
                flora0_startLifetimeRandom = flora.startLifetimeRandom;
                flora0_spreadChance = flora.spreadChance;
                flora0_minSpreadDensity = flora.minSpreadDensity;
                flora0_childDensityMult = flora.childDensityMult;
            }
        }
        else if (type == typeof(Map_Climate))
        {
            Map_Climate clim = ((Map_Climate)m);
            climate_rawTempScale = clim.rawTempScale;
            climate_rawTempUnscaledOffset = clim.rawTempUnscaledOffset;
        }
    }
    /// <param name="layer">For single map type with multiple layers e.g. Flora trees, flora crops, etc.</param>
    public void ApplyValuesToMap(Map m, int layer = 0)
    {
        Type type = m.GetType();
        if(type == typeof(Map_Elevation))
        {
            Map_Elevation elv = ((Map_Elevation)m);
            elv.seaLevel = elv_seaLevel;
            elv.altitudeScale = elv_altitudeScale;
        }
        else if (type == typeof(Map_Fertility))
        {
            Map_Fertility fert = ((Map_Fertility)m);
            fert.fert0FloraChange = fert_FloraChange0;
            fert.fert255FloraChange = fert_FloraChange255;
        }
        else if (type == typeof(Map_Flora))
        {
            if (layer == 0)
            {
                Map_Flora flora = ((Map_Flora)m);
                flora.startLifetime = flora0_startLifetime;
                flora.startLifetimeRandom = flora0_startLifetimeRandom;
                flora.spreadChance = flora0_spreadChance;
                flora.minSpreadDensity = flora0_minSpreadDensity;
                flora.childDensityMult = flora0_childDensityMult;
            }
        }
        else if (type == typeof(Map_Climate))
        {
            Map_Climate clim = ((Map_Climate)m);
            clim.rawTempScale = climate_rawTempScale;
            clim.rawTempUnscaledOffset = climate_rawTempUnscaledOffset;
        }
        else if (type == typeof(Map_Infrastructure))
        {

        }
    }
}

[System.Serializable]
public class WorldData { //Loaded world. Deserialise name.minfo -> apply values to WorldData and MapData and each Map -> load in PNGs.

    public WorldInfo info;
    public MapData mapData;

    public string worldName
    {
        get { return info.name; }
        set
        {
            string n = value;
            n = n.Replace('/', '-');
            n = n.Replace('\\', '-');
            n.Replace(':', '-');
            foreach (char i in System.IO.Path.GetInvalidFileNameChars())
            {
                if (n.Contains(i.ToString()))
                {
                    n = "Unnamed Map";
                    break;
                }
            }
            info.name = n;
        }
    }
    
    public WorldData()
    {
        info = new WorldInfo();
        mapData = new MapData();
    }
}

[System.Serializable]
public class MapData
{
    public Vector2Int mapSize = new Vector2Int(1000, 1000);
    public Map_Elevation elevation;
    public Map_Fertility fertility;
    public Map_Flora flora_trees;
    public Map_Climate climate;
    public Map_Infrastructure infrastructure;

    public MapData()
    {
        mapSize = Vector2Int.one * 1000;
        elevation = new Map_Elevation();
        fertility = new Map_Fertility();
        flora_trees = new Map_Flora();
        climate = new Map_Climate();
        infrastructure = new Map_Infrastructure();
    }
    public MapData(Vector2Int size)
    {
        mapSize = size;
        elevation = new Map_Elevation();
        fertility = new Map_Fertility();
        flora_trees = new Map_Flora();
        climate = new Map_Climate();
        infrastructure = new Map_Infrastructure();
    }
}
