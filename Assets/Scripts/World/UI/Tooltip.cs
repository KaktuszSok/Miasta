using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tooltip : MonoBehaviour {

    public static Tooltip instance;
    Image panel;
    public Text text;
    public LayoutElement textLayout;
    Camera cam;
    Canvas canvas;

    public bool shown = false;
    float textUpdateTimer = 0.1f;
    public Vector2Int cursorWorldPos;
    Vector2Int prevCursorWorldPos;

    public enum TooltipMode
    {
        ALL,
        ELEVATION,
        FERTILITY,
        TREES,
        CLIMATE,
        INFRASTRUCTURE,
        NumberOfTypes
    }
    int currTooltipMode = 0;

    private void Awake()
    {
        instance = this;
    }

    void Start () {
        panel = GetComponent<Image>();
        text = GetComponentInChildren<Text>();
        textLayout = GetComponentInChildren<LayoutElement>();
        cam = Camera.main;
        canvas = GetComponentInParent<Canvas>();
        ShowTooltip(false);
    }

    private void Update()
    {

    }

    void LateUpdate () {
		if(Input.GetKeyDown(KeyCode.Tab))
        {
            ShowTooltip(true);
        }
        if(Input.GetKeyUp(KeyCode.Tab))
        {
            ShowTooltip(false);
        }
        if (shown)
        {
            UpdateCursorWorldPos();
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, Input.mousePosition, canvas.worldCamera, out pos);
            transform.position = canvas.transform.TransformPoint(pos);
            if (Input.mouseScrollDelta.y != 0f)
            {
                currTooltipMode = (int)Mathf.Repeat(currTooltipMode + (int)Mathf.Sign(Input.mouseScrollDelta.y), (int)TooltipMode.NumberOfTypes);
                UpdateText(GetTooltipTextAtPos(cursorWorldPos, (TooltipMode)currTooltipMode));
            }
            /*if (textUpdateTimer <= 0)
            {
                textUpdateTimer = 0.1f;
                UpdateText(GetTooltipTextAtPos(cursorWorldPos, (TooltipMode)currTooltipMode));
            }
            else if (textUpdateTimer > 0)
            {
                textUpdateTimer -= Time.deltaTime;
            }*/

            if (prevCursorWorldPos != cursorWorldPos)
            {
                UpdateText(GetTooltipTextAtPos(cursorWorldPos, (TooltipMode)currTooltipMode));
            }
            prevCursorWorldPos = cursorWorldPos;
        }
	}

    public void UpdateCursorWorldPos()
    {
        Vector3 cursorWorldPos3D = cam.ScreenToWorldPoint(Input.mousePosition);
        int mapx = WorldManager.instance.worldData.mapData.mapSize.x;
        int mapy = WorldManager.instance.worldData.mapData.mapSize.y;
        cursorWorldPos = new Vector2Int((int)Mathf.Clamp(cursorWorldPos3D.x + mapx / 2f, 0, mapx - 1), (int)Mathf.Clamp(cursorWorldPos3D.y + mapy / 2f, 0, mapy - 1));
    }

    public void ShowTooltip(bool show)
    {
        shown = show;
        panel.enabled = show;
        text.enabled = show;
        if (show) UpdateText();
    }

    public string GetTooltipTextAtPos(Vector2Int pos, TooltipMode infoType)
    {
        string s = "Pos: " + pos.x + "," + pos.y;
        if(infoType == TooltipMode.ELEVATION || infoType == TooltipMode.ALL)
        {
            s += "\n";
            s += "Elevation: " + WorldManager.instance.worldData.mapData.elevation.GetAltitudeAtPos(pos)
            + "m ASL (Raw: " + WorldManager.instance.worldData.mapData.elevation.GetAltitudeAtPos(pos, false) + "m)";
        }
        if (infoType == TooltipMode.CLIMATE || infoType == TooltipMode.ALL)
        {
            s += "\n";
            s += "Climate:"
                + "\n    Temperature: " + WorldManager.instance.GetEffectiveTempAtPos(pos).ToString("F1") + " Effective (Raw: " + WorldManager.instance.worldData.mapData.climate.GetRawTempAtPos(pos).ToString("F1") + ")"
                + "\n    Humidity: " + WorldManager.instance.GetEffectiveHumidityAtPos(pos) + " Effective (Raw: " + WorldManager.instance.worldData.mapData.climate.GetRawHumidityAtPos(pos) + ")";
        }
        if (infoType == TooltipMode.FERTILITY || infoType == TooltipMode.ALL)
        {
            float effectiveFertility = WorldManager.instance.GetEffectiveFertilityAtPos(pos);
            WorldData worldData = WorldManager.instance.worldData;
            float dropDueToTemp = -Mathf.Abs(worldData.info.fertOptimalTemperature - WorldManager.instance.GetEffectiveTempAtPos(pos))*worldData.info.fertDropPerDegree;
            float dropDueToHum = 0f;
            byte humDifference = (byte)Mathf.Abs(worldData.info.fertOptimalHumidity - WorldManager.instance.GetEffectiveHumidityAtPos(pos));
            if (humDifference > worldData.info.fertOptimalHumidityLeniency)
            {
                //Drop fertility if too arid or humid
                dropDueToHum = -(humDifference - worldData.info.fertOptimalHumidityLeniency)*worldData.info.fertDropPerHumidity;
            }
            float dropDueToPenalty = 0f;
            float altASL = worldData.mapData.elevation.GetAltitudeAtPos(pos, true);
            if (altASL > worldData.info.fertDropPerDegree_BonusStartAlt)
            {
                float penaltyDist = altASL - worldData.info.fertDropPerDegree_BonusStartAlt;
                dropDueToPenalty = -(penaltyDist / 100f)*worldData.info.fertDropPerDegree_BonusPenaltyPer100m;
            }
            s += "\n";
            s += "Fertility: " + effectiveFertility
            + " Effective (Raw: " + WorldManager.instance.worldData.mapData.fertility.GetRawFertilityAtPos(pos) + ")"
            + "\n    Due to Temp.: " + dropDueToTemp.ToString("F0")
            + "\n    Due to Hum.: " + dropDueToHum.ToString("F0")
            + "\n    Due to Altitude: " + dropDueToPenalty.ToString("F0");
        }
        if (infoType == TooltipMode.TREES || infoType == TooltipMode.ALL)
        {
            s += "\n";
            Color32 floraInfo = WorldManager.instance.worldData.mapData.flora_trees.GetColourAtPos(pos);
            s += "Flora (Trees):"
                + "\n    Density = " + floraInfo.a
                + "\n    Life = " + floraInfo.g;
        }
        if (infoType == TooltipMode.INFRASTRUCTURE || infoType == TooltipMode.ALL)
        {
            s += "\n";
            Color32 infraInfo = WorldManager.instance.worldData.mapData.infrastructure.GetColourAtPos(pos);
            s += "Infrastructure:"
                + "\n    Density = " + infraInfo.a
                + "\n    Lushness = " + infraInfo.g;
        }
        return s;
    }

    public void UpdateText()
    {
        UpdateText(GetTooltipTextAtPos(cursorWorldPos, (TooltipMode)currTooltipMode));
    }
    public void UpdateText(string t)
    {
        text.text = t;
        textLayout.minHeight = 0;
        textLayout.minHeight = LayoutUtility.GetPreferredHeight(text.rectTransform);
    }
}
