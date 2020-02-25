using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldTool {

    public string toolName = "Unnamed Tool";
    public float defaultStrength = 15;

    public virtual void UseTool(Vector2Int pos, float strength)
    {

    }

    public virtual ResourceCost[] GetUseCosts(Vector2Int pos, float strength)
    {
        ResourceCost[] resourceCosts = new ResourceCost[0];
        return resourceCosts;
    }
    public virtual ResourceCost[] GetUseReqs(Vector2Int pos, float strength)
    {
        ResourceCost[] resourceCosts = new ResourceCost[0];
        return resourceCosts;
    }
}

public class Tool_Infrastructure_Density : WorldTool
{
    public override void UseTool(Vector2Int pos, float strength)
    {
        if (WorldPlayer.instance.ResourceInventory.CanPayCosts(GetUseReqs(pos, strength))) //Does player meet the requirements?
        {
            ResourceCost[] costs = GetUseCosts(pos, strength);
            if (WorldPlayer.instance.ResourceInventory.CanPayCosts(costs)) //Does player have enough resources to pay the costs?
            {
                WorldManager.instance.worldData.mapData.infrastructure.AddInfra((short)strength, pos, WorldManager.instance.worldRend); //Add infrastructure.
                WorldPlayer.instance.ResourceInventory.PayCosts(costs); //Pay the costs.
            }
        }
    }

    public override ResourceCost[] GetUseCosts(Vector2Int pos, float strength)
    {
        byte currInfra = WorldManager.instance.worldData.mapData.infrastructure.GetInfraAtPos(pos); //Infrastructure the tile currently has.
        List<ResourceCost> costs = new List<ResourceCost>(); //List of the costs, length is equal to amount of different resources that are needed.
        for(
            short i = (short)(Mathf.Sign(strength) > 0 ? 1 : 1 + strength); //If building, start at next level. If removing, start at the last removed, including current level. 
            i <= (Mathf.Sign(strength) > 0 ? (short)strength : 0); //If building, end at last level built. If removing, end at current level.
            i++)
        {
            if (currInfra + i > 255 || currInfra + i < 0) continue;
            byte newInfra = (byte)(currInfra + i);
            Map_Infrastructure.InfrastructureLevel InfraLevel = WorldManager.instance.worldData.mapData.infrastructure.GetLevelFromInfra(newInfra);

            switch(InfraLevel) //Cost to construct one infra at its level. Also compensated when deconstructing.
            {
                case Map_Infrastructure.InfrastructureLevel.WILDERNESS:
                    break;
                case Map_Infrastructure.InfrastructureLevel.VILLAGE:
                    costs.Add(new ResourceCost("Wood", 50));
                    break;
                case Map_Infrastructure.InfrastructureLevel.TOWN:
                    costs.Add(new ResourceCost("Wood", 75));
                    costs.Add(new ResourceCost("Stone", 200));
                    costs.Add(new ResourceCost("Bricks", 100));
                    break;
                case Map_Infrastructure.InfrastructureLevel.INDUSTRIAL:
                    costs.Add(new ResourceCost("Wood", 100));
                    costs.Add(new ResourceCost("Stone", 250));
                    costs.Add(new ResourceCost("Bricks", 400));
                    costs.Add(new ResourceCost("Concrete", 300));
                    break;
                case Map_Infrastructure.InfrastructureLevel.MODERN:
                    costs.Add(new ResourceCost("Wood", 100));
                    costs.Add(new ResourceCost("Stone", 250));
                    costs.Add(new ResourceCost("Bricks", 400));
                    costs.Add(new ResourceCost("Concrete", 300));
                    break;
                case Map_Infrastructure.InfrastructureLevel.ADVANCED:
                    costs.Add(new ResourceCost("Wood", 100));
                    costs.Add(new ResourceCost("Stone", 250));
                    costs.Add(new ResourceCost("Bricks", 400));
                    costs.Add(new ResourceCost("Concrete", 300));
                    break;
                case Map_Infrastructure.InfrastructureLevel.METROPOLIS:
                    costs.Add(new ResourceCost("Wood", 100));
                    costs.Add(new ResourceCost("Stone", 250));
                    costs.Add(new ResourceCost("Bricks", 400));
                    costs.Add(new ResourceCost("Concrete", 300));
                    break;
                case Map_Infrastructure.InfrastructureLevel.REVOLUTIONARY:
                    costs.Add(new ResourceCost("Wood", 100));
                    costs.Add(new ResourceCost("Stone", 250));
                    costs.Add(new ResourceCost("Bricks", 400));
                    costs.Add(new ResourceCost("Concrete", 300));
                    break;
                default:
                    break;
            }
        }
        if (WorldManager.instance.worldData.mapData.elevation.GetAltitudeAtPos(pos) <= 0) //Add cost for building on water
        {
            for (int i = 0; i < costs.Count; i++)
            {
                costs[i] = new ResourceCost(costs[i].resource, costs[i].amount * 1.5f);
            }
        }
        if (strength < 0) //compensate compensatable costs
        {
            for(int i = 0; i < costs.Count; i++)
            {
                if(!costs[i].resource.Contains("Labour"))
                {
                    costs[i] = new ResourceCost(costs[i].resource, -costs[i].amount);
                }
            }
        }

        return costs.ToArray();
    }

    public override ResourceCost[] GetUseReqs(Vector2Int pos, float strength) //in addition to costs, the reqs must also be fulfilled but will not be consumed.
    {
        byte currInfra = WorldManager.instance.worldData.mapData.infrastructure.GetInfraAtPos(pos); //Infrastructure the tile currently has.
        List<ResourceCost> reqs = new List<ResourceCost>(); //List of the reqs, length is equal to amount of different resources that are needed.
        for (
            short i = (short)(Mathf.Sign(strength) > 0 ? 1 : 1 - strength); //If building, start at next level. If removing, start at the last removed, including current level. 
            i <= (Mathf.Sign(strength) > 0 ? (short)strength : 0); //If building, end at last level built. If removing, end at current level.
            i++)
        {
            if (currInfra + i > 255 || currInfra + 1 < 0) continue;
            byte newInfra = (byte)(currInfra + i);
            Map_Infrastructure.InfrastructureLevel InfraLevel = WorldManager.instance.worldData.mapData.infrastructure.GetLevelFromInfra(newInfra);

            switch (InfraLevel) //Requirements, in addition to cost, to construct each level.
            {
                case Map_Infrastructure.InfrastructureLevel.WILDERNESS:
                    break;
                case Map_Infrastructure.InfrastructureLevel.VILLAGE:
                    break;
                case Map_Infrastructure.InfrastructureLevel.TOWN:
                    break;
                case Map_Infrastructure.InfrastructureLevel.INDUSTRIAL:
                    break;
                case Map_Infrastructure.InfrastructureLevel.MODERN:
                    break;
                case Map_Infrastructure.InfrastructureLevel.ADVANCED:
                    break;
                case Map_Infrastructure.InfrastructureLevel.METROPOLIS:
                    break;
                case Map_Infrastructure.InfrastructureLevel.REVOLUTIONARY:
                    break;
                default:
                    break;
            }
            reqs.Add(new ResourceCost("Labour_Basic", 10));
        }
        if (WorldManager.instance.worldData.mapData.elevation.GetAltitudeAtPos(pos) <= 0) //Add reqs for building on water
        {
            for (int i = 0; i < reqs.Count; i++)
            {
                reqs[i] = new ResourceCost(reqs[i].resource, reqs[i].amount * 1.5f);
            }
        }

        if (strength < 0) //if deconstructing, adjust values.
        {
            for (int i = 0; i < reqs.Count; i++)
            {
                if (reqs[i].resource != "Labour")
                {
                    reqs.RemoveAt(i);
                    i--;
                }
                else
                {
                    reqs[i] = new ResourceCost(reqs[i].resource, reqs[i].amount * 0.25f);
                }
            }
        }

        return reqs.ToArray();
    }
}

public class Tool_Infrastructure_Lushness : WorldTool
{
    public override void UseTool(Vector2Int pos, float strength)
    {
        WorldManager.instance.worldData.mapData.infrastructure.AddLushness((short)strength, pos, WorldManager.instance.worldRend);
    }
}
