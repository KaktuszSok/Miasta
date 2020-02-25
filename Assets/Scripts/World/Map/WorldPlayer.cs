using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldPlayer : MonoBehaviour {

    public static WorldPlayer instance;

    Camera cam;
    public UnityEngine.EventSystems.EventSystem events;
    public GameObject toolButtonPrefab;
    public Transform toolButtonsParent;

    Vector2Int cursorWorldPos;

    public bool canBuild = true;

    public ResourcesHolder ResourceInventory;

    public WorldTool[] tools = new WorldTool[2]
    {
        new Tool_Infrastructure_Density(),
        new Tool_Infrastructure_Lushness()
    };
    public WorldTool currTool;

	void Awake() {
        instance = this;
        tools[0].toolName = "Infra. Density";
        tools[0].defaultStrength = 15;
        tools[1].toolName = "Infra. Lushness";
        tools[1].defaultStrength = 15;
        ResourcesManager.OnResourcesAllAdded.AddListener(OnAllResourcesAdded);
	}

    private void Start()
    {
        WorldManager.OnWorldTick.AddListener(OnTick);
        cam = Camera.main;
        GenerateToolButtons();
    }

    void OnAllResourcesAdded()
    {
        ResourceInventory = new ResourcesHolder();
    }

    uint tickCounter = 0;
    void OnTick()
    {
        if (tickCounter % 20 == 0)
        {
            float foodConsumptionMult = 1f;
            ResourceCost foodConsumed = new ResourceCost("Food", ResourceInventory.GetResource("Population") * foodConsumptionMult);
            if (ResourceInventory.CanPayCost(foodConsumed))
            {
                ResourceInventory.SetResource(foodConsumed.resource, -foodConsumed.amount, true, 0);
            }
            else
            {
                ResourceInventory.SetResource(foodConsumed.resource, 0, false);
                float deltaFood = foodConsumed.amount - ResourceInventory.GetResource(foodConsumed.resource);
                ResourceCalculator.KillFromSubcategory((int)(deltaFood * 0.05f), "", ResourceInventory); //Kill 5% of unfed.
            }
        }
        tickCounter = (uint)Mathf.Repeat(tickCounter + 1, uint.MaxValue);
    }

    void GenerateToolButtons()
    {
        while(toolButtonsParent.childCount > 0)
        {
            Destroy(toolButtonsParent.GetChild(0).gameObject);
        }
        foreach(WorldTool t in tools)
        {
            Transform toolButton = Instantiate(toolButtonPrefab, toolButtonsParent).transform;
            toolButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(delegate { SetTool(t); });
            toolButton.name = t.toolName;
            toolButton.GetComponentInChildren<UnityEngine.UI.Text>().text = t.toolName;
        }
    }

    public void SetTool(WorldTool tool)
    {
        currTool = tool;
    }

    void Update () {
        if (canBuild && Input.GetKey(KeyCode.LeftShift) && !events.IsPointerOverGameObject() && currTool != null)
        {
            if (Input.GetMouseButtonDown(0))
            {
                UpdateCursorWorldPos();
                currTool.UseTool(cursorWorldPos, currTool.defaultStrength);
                Tooltip.instance.UpdateText();
            }
            if (Input.GetMouseButtonDown(1))
            {
                UpdateCursorWorldPos();
                currTool.UseTool(cursorWorldPos, -currTool.defaultStrength);
                Tooltip.instance.UpdateText();
            }
        }
        if(canBuild && currTool != null && Input.GetMouseButtonDown(0) && Input.GetKey(KeyCode.Tab)) //Tab + click = display reqs and costs
        {
            TooltipDisplayToolUseInfo(currTool.defaultStrength);
        }
        else if (canBuild && currTool != null && Input.GetMouseButtonDown(1) && Input.GetKey(KeyCode.Tab)) //Tab + click = display reqs and costs to deconstruct
        {
            TooltipDisplayToolUseInfo(-currTool.defaultStrength);
        }
    }

    void TooltipDisplayToolUseInfo(float strength)
    {
        UpdateCursorWorldPos();
        ResourceCost[] toolReqs = ResourceCost.CollapseListDontTotal(currTool.GetUseReqs(cursorWorldPos, strength));
        ResourceCost[] toolCosts = ResourceCost.CollapseList(currTool.GetUseCosts(cursorWorldPos, strength));
        string tooltip = "building reqs:";
        foreach (ResourceCost c in toolReqs)
        {
            string colour = ResourceInventory.CanPayCost(c) ? "green" : "red";
            tooltip += "\n    <color=" + colour + ">" + c.resource + ": " + c.amount + " (" + (ResourceInventory.GetResource(c.resource) - c.amount) + ")</color>";
        }
        tooltip += "\nbuilding costs:";
        foreach (ResourceCost c in toolCosts)
        {
            string colour = ResourceInventory.CanPayCost(c) ? "green" : "red";
            tooltip += "\n    <color=" + colour + ">" + c.resource + ": " + c.amount + " (" + (ResourceInventory.GetResource(c.resource) - c.amount) + ")</color>";
        }
        Tooltip.instance.UpdateText(tooltip);
    }

    public void UpdateCursorWorldPos()
    {
        Vector3 cursorWorldPos3D = cam.ScreenToWorldPoint(Input.mousePosition);
        int mapx = WorldManager.instance.worldData.mapData.mapSize.x;
        int mapy = WorldManager.instance.worldData.mapData.mapSize.y;
        cursorWorldPos = new Vector2Int((int)Mathf.Clamp(cursorWorldPos3D.x + mapx / 2f, 0, mapx - 1), (int)Mathf.Clamp(cursorWorldPos3D.y + mapy / 2f, 0, mapy - 1));
    }
}
