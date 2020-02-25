using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class WorldUtils {
    //World Stuff
    public static WorldData LoadWorld(string worldName)
    {
        WorldData world = new WorldData();
        string path = Application.streamingAssetsPath + "/Worlds/" + worldName + "/" + worldName + ".minfo";
        if(!File.Exists(path)) //Error if world save doesn't exist.
        {
            Debug.LogError("Could not find world " + worldName + " at " + path);
            return world;
        }
        //Load worldInfo from name.minfo
        string worldJson = File.ReadAllText(path);
        world.info = JsonUtility.FromJson<WorldInfo>(worldJson);
        //Create MapInfos
        ClearMapInfos();
        //elevation
        MapInfo miElevation = new MapInfo();
        miElevation.mapReference = world.mapData.elevation;
        miElevation.mapName = "MapElevation";
        miElevation.shortName = "elevation";
        miElevation.mapLayer = 0;
        RegisterMapInfo(miElevation);
        //fertility
        MapInfo miFertility = new MapInfo();
        miFertility.mapReference = world.mapData.fertility;
        miFertility.mapName = "MapFertility";
        miFertility.shortName = "fertility";
        miFertility.mapLayer = 0;
        RegisterMapInfo(miFertility);
        //flora_trees
        MapInfo miFlora_Trees = new MapInfo();
        miFlora_Trees.mapReference = world.mapData.flora_trees;
        miFlora_Trees.mapName = "MapFlora_Trees";
        miFlora_Trees.shortName = "flora_trees";
        miFlora_Trees.mapLayer = 0;
        miFlora_Trees.receiveOnWorldLoaded = true;
        miFlora_Trees.receiveRandomTicks = true;
        RegisterMapInfo(miFlora_Trees);
        //climate
        MapInfo miClimate = new MapInfo();
        miClimate.mapReference = world.mapData.climate;
        miClimate.mapName = "MapClimate";
        miClimate.shortName = "climate";
        miClimate.mapLayer = 0;
        RegisterMapInfo(miClimate);
        //infrastructure
        MapInfo miInfrastructure = new MapInfo();
        miInfrastructure.mapReference = world.mapData.infrastructure;
        miInfrastructure.mapName = "MapInfrastructure";
        miInfrastructure.shortName = "infrastructure";
        miInfrastructure.mapLayer = 0;
        //miInfrastructure.receiveNormalTicks = true;
        RegisterMapInfo(miInfrastructure);

        //Apply worldInfo to maps
        /*world.info.ApplyValuesToMap(world.mapData.elevation);
        world.info.ApplyValuesToMap(world.mapData.fertility);
        world.info.ApplyValuesToMap(world.mapData.flora_trees, 0);
        world.info.ApplyValuesToMap(world.mapData.climate);*/
        //Load Maps:
        /*world.mapData.elevation.SetTexture(LoadMapTex(worldName, "elevation"));
        world.mapData.fertility.SetTexture(LoadMapTex(worldName, "fertility"));
        world.mapData.flora_trees.SetTexture(LoadMapTex(worldName, "trees"));
        world.mapData.climate.SetTexture(LoadMapTex(worldName, "climate"));*/
        foreach (MapInfo mi in mapInfos)
        {
            world.info.ApplyValuesToMap(mi.mapReference, mi.mapLayer);
            mi.mapReference.SetTexture(LoadMapTex(worldName, mi.mapName));
        }

        return world;
    }

    public static void SaveWorld(WorldData world)
    {
        //save data to WorldInfo
        WorldInfo info = world.info;
        /*info.SetValuesFromMap(world.mapData.elevation);
        info.SetValuesFromMap(world.mapData.fertility);
        info.SetValuesFromMap(world.mapData.flora_trees, 0);
        info.SetValuesFromMap(world.mapData.climate);*/
        foreach(MapInfo mi in mapInfos)
        {
            info.SetValuesFromMap(mi.mapReference, mi.mapLayer);
        }

        //Save files
        string path = Application.streamingAssetsPath + "/Worlds/" + info.name + "/";
        if(!File.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }
        string infoJson = JsonUtility.ToJson(info);
        File.WriteAllText(path + info.name + ".minfo", infoJson); //writes to StreamingAssets/Worlds/name/name.minfo
        /*File.WriteAllBytes(path + "MapElevation.png", world.mapData.elevation.mapTex.EncodeToPNG());
        File.WriteAllBytes(path + "MapFertility.png", world.mapData.fertility.mapTex.EncodeToPNG());
        File.WriteAllBytes(path + "MapFlora_Trees.png", world.mapData.flora_trees.mapTex.EncodeToPNG());
        File.WriteAllBytes(path + "MapClimate.png", world.mapData.climate.mapTex.EncodeToPNG());*/
        foreach(MapInfo mi in mapInfos)
        {
            File.WriteAllBytes(path + mi.mapName + ".png", mi.mapReference.mapTex.EncodeToPNG());

        }

    }

    //Map Stuff
    public class MapInfo
    {
        public Map mapReference;
        public string mapName; //file name e.g. "MapFertility"
        public string shortName; //e.g. "fertility"
        public int mapLayer; //e.g. 0 for "flora_trees" to distinguish from "flora_crops"
        public bool receiveOnWorldLoaded = false;
        public bool receiveRandomTicks = false;
        public bool receiveNormalTicks = false;
    }
    public static List<MapInfo> mapInfos = new List<MapInfo>();
    
    public static void RegisterMapInfo(MapInfo mi)
    {
        mapInfos.Add(mi);
    }
    /// <param name="index">-1 for all</param>
    public static void ClearMapInfos(int index = -1)
    {
        if(index == -1)
        {
            mapInfos.Clear();
        }
        else
        {
            mapInfos.RemoveAt(index);
        }
    }
    public static void ClearMapInfos(string mapShortName)
    {
        int foundIndex = -1;
        for(int i = 0; i < mapInfos.Count; i++)
        {
            if(mapInfos[i].shortName == mapShortName)
            {
                foundIndex = i;
                break;
            }
        }
        if(foundIndex != -1)
        {
            ClearMapInfos(foundIndex);
        }
    }

    public static Texture2D LoadMapTex(string worldName, string mapName)
    {
        Texture2D mapTex = new Texture2D(1, 1);
        string path = Application.streamingAssetsPath + "/Worlds/" + worldName + "/" + mapName + ".png";
        if (File.Exists(path))
        {
            mapTex.LoadImage(File.ReadAllBytes(path));
        }
        else
        {
            Debug.LogError("Could not find " + mapName + " at path " + path);
        }
        return mapTex;
    }

    public static bool isMapValid(Map map, MapData data)
    {
        if (data == null) //If data is null, return false.
        {
            Debug.LogError("isMapValid [X] - mapData null");
            return false;
        }
        if (map == null) //If map is null, return false.
        {
            Debug.LogError("isMapValid [X] - map null");
            return false;
        }
        if(map != null && map.mapTex == null) //If map texture is null, return false.
        {
            Debug.LogError("isMapValid [X] - map texture null");
            return false;
        }
        if(map.mapTex.width != data.mapSize.x || map.mapTex.height != data.mapSize.y) //If map texture is wrong size, return false.
        {
            Debug.LogError("isMapValid [X] - map size not equal to mapData size");
            return false;
        }
        return true;
    }

    public static Vector2Int TexIndexToPos(int index, int width, int height)
    {
        return new Vector2Int(index % width, index / width);
    }
    public static int TexPosToIndex(Vector2Int pos, int width, int height)
    {
        return (pos.y * width) + pos.x;
    }

    //Utility
    public static void ChanceLog(object log, int chanceDivisor)
    {
        if(Random.Range(0, chanceDivisor) == 0)
        {
            Debug.Log(log);
        }
    }

    static Dictionary<string, System.DateTime> timeTrackingStarts = new Dictionary<string, System.DateTime>(); //Dictionary allows for multiple independent trackings at the same time.
    public static void StartTrackingTime(string trackerIdentifier)
    {
        timeTrackingStarts.Add(trackerIdentifier, System.DateTime.Now);
    }
    public static System.TimeSpan GetTimeTracked(string trackerIdentifier, bool remove = true)
    {
        System.TimeSpan timeSpan = System.DateTime.Now.Subtract(timeTrackingStarts[trackerIdentifier]);
        if(remove) timeTrackingStarts.Remove(trackerIdentifier); //Optionally, don't remove. Handy if you want to report time taken at certain intervals of a function while still with respect to the original start time.
        return timeSpan;
    }
}
