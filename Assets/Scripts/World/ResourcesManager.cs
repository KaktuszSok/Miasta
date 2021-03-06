﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using B83.ExpressionParser;

public static class ResourcesManager {

    public static Dictionary<string, float> ResourceList { get; private set; }
    public static List<string> PopulationSubcategories = new List<string>();
    public static Dictionary<string, string> ResourceExpressions { get; private set; }
    public static UnityEvent OnResourcesInitialised = new UnityEvent();
    public static UnityEvent OnResourcesAllAdded = new UnityEvent();

    public static ExpressionParser resourceExpressionParser = new ExpressionParser();

    public static void Initialise()
    {
        if (ResourceList != null)
        {
            ResourceList.Clear();
            PopulationSubcategories.Clear();
            ResourceExpressions.Clear();
        }
        else
        {
            ResourceList = new Dictionary<string, float>();
            PopulationSubcategories = new List<string>();
            ResourceExpressions = new Dictionary<string, string>();
        }
        OnResourcesInitialised.Invoke();
        //All resource addition should be done when OnResourcesInitialised is called. i.e. immediately after the line above and before the line below.
        OnResourcesAllAdded.Invoke();
    }

    public static void AddResource(string name, float defaultValue)
    {
        if (!ResourceList.ContainsKey(name))
        {
            ResourceList.Add(name, defaultValue);
        }
        else
        {
            Debug.Log("ResourceManager - Resource List already contains key '" + name + "'");
        }
    }
    public static void MarkResourceAsPopulationSubcategory(string resourceName)
    {
        if(!PopulationSubcategories.Contains(resourceName))
        {
            PopulationSubcategories.Add(resourceName);
        }
    }
    public static void MarkResourceAsExpression(string resourceName, string expression)
    {
        ResourceExpressions.Add(resourceName, expression);
    }
}

public class ResourcesHolder
{
    public Dictionary<string, float> ResourceList { get; private set; }

    public void UpdateExpressionResources()
    {
        string[] keys = new string[ResourceList.Keys.Count];
        ResourceList.Keys.CopyTo(keys, 0);
        foreach(string resource in keys) //Go through all resources in inventory
        {
            if (ResourcesManager.ResourceExpressions.ContainsKey(resource)) //If it is an expression resource:
            {
                Expression exp = ResourcesManager.resourceExpressionParser.EvaluateExpression(ResourcesManager.ResourceExpressions[resource]); //Create expression based on resource's assigned expression
                foreach(string r in exp.Parameters.Keys) //Check through all parameters generated by the expression parser
                {
                    exp.Parameters[r].Value = GetResource(r); //Substitute the parameter for the resource value e.g. "Population" in expression becomes 1000
                }
                ResourceList[resource] = exp.Value; //Assign the effective value of the resource based on its expression, to said resource in our list.
            }
        }
    }
    public float GetResource(string name)
    {
        return ResourceList[name];
    }

    public void SetResource(string name, float value, bool additive = false, float clampMin = -float.MaxValue, float clampMax = float.MaxValue)
    {
        if (additive) value += ResourceList[name];
        value = Mathf.Clamp(value, clampMin, clampMax);
        ResourceList[name] = value;
        UpdateExpressionResources(); //Recalculate expression resources because some may have changed as a result of this one changing.
    }

    public bool CanPayCost(ResourceCost cost)
    {
        return ResourceList[cost.resource] >= cost.amount;
    }
    /// <param name="collapse">Set to false when checking reqs</param>
    public bool CanPayCosts(ResourceCost[] costs, bool collapse = true)
    {
        ResourceCost[] totalCosts = ResourceCost.CollapseList(costs);

        foreach (ResourceCost c in totalCosts)
        {
            if (!CanPayCost(c))
            {
                return false;
            }
        }
        return true;
    }

    public void PayCost(ResourceCost cost)
    {
        ResourceList[cost.resource] -= cost.amount;
    }
    public void PayCosts(ResourceCost[] costs)
    {
        foreach (ResourceCost c in costs)
        {
            PayCost(c);
        }
    }

    public ResourcesHolder()
    {
        ResourceList = new Dictionary<string, float>();
        foreach (string s in ResourcesManager.ResourceList.Keys)
        {
            ResourceList.Add(s, ResourcesManager.ResourceList[s]);
        }
        UpdateExpressionResources();
    }
}

public static class ResourceCalculator
{
    public static void KillFromPop(int amount, ResourcesHolder resources)
    {
        int totalPop = (int)resources.GetResource("Population"); //total population before killing
        if (totalPop <= 0) { totalPop = 0; return; }

        //Remove a proportional amount of people from each Subcategory.
        float fractionDied = amount / (float)totalPop; //fraction of total population killed.
        foreach (string s in ResourcesManager.PopulationSubcategories)
        {
            //remove proportional amount from the subcategory to reflect individuals in it who have died.
            int subcategoryPop = (int)resources.GetResource(s);
            int amountToRemove = (int)(subcategoryPop * fractionDied);
            resources.SetResource(s, -amountToRemove, true, 0);
        }

        resources.SetResource("Population", -amount, true, 0); //kill from total pop
        EnsurePopulationSubcategoriesNotExceedingTotalPop(resources);
    }
    public static void KillFromSubcategory(int amount, string subcat, ResourcesHolder resources)
    {
        //If subcat is passed as a blank name, this function will kill from the uncategorised first and once a subcategory exceeds the total pop, it will be capped to the total pop.
        //This is useful for things such as hunger deaths seeing as the rare, highly-educated citizens would not be likely to die of hunger unless shit really hit the fan.
        if (subcat != "") resources.SetResource(subcat, -amount, true, 0); //kill from subcat.
        resources.SetResource("Population", -amount, true, 0); //kill from total pop
        EnsurePopulationSubcategoriesNotExceedingTotalPop(resources);
    }

    public static void AdmitToSubcategory(int amount, string subcat, ResourcesHolder resources) //Add to subcategory and make sure it does not exceed the limit.
    {
        resources.SetResource(subcat, amount, true);
        EnsurePopulationSubcategoriesNotExceedingTotalPop(resources);
    }

    public static void EnsurePopulationSubcategoriesNotExceedingTotalPop(ResourcesHolder resources)
    {
        int totalPop = (int)resources.GetResource("Population"); //total population
        string maxSubcategoryPopResource = GetMaxSubcategoryPopResource(resources); //population subcategory resource with the highest count
        //If one or more of the subcategories exceeds the total population, clip the subcategory's count to the total population.
        while (maxSubcategoryPopResource != "" && resources.GetResource(maxSubcategoryPopResource) > totalPop)
        {
            resources.SetResource(maxSubcategoryPopResource, totalPop);
            maxSubcategoryPopResource = GetMaxSubcategoryPopResource(resources);
        }
    }
    public static void EnsureResourceDoesNotExceedOtherResource(string resource, string limitingResource, float factor, ResourcesHolder resources)
    {
        if(resources.GetResource(resource) > resources.GetResource(limitingResource)*factor)
        {
            resources.SetResource(resource, resources.GetResource(limitingResource)*factor);
        }
    }

    public static int GetTotalSubcategoryPopCount(ResourcesHolder resources)
    {
        int total = 0;
        foreach (string s in ResourcesManager.PopulationSubcategories)
        {
            total += (int)resources.GetResource(s);
        }
        return total;
    }
    public static string GetMaxSubcategoryPopResource(ResourcesHolder resources)
    {
        int max = 0;
        string resource = "";
        foreach(string s in ResourcesManager.PopulationSubcategories)
        {
            if(resources.GetResource(s) > max)
            {
                max = (int)resources.GetResource(s);
                resource = s;
            }
        }
        return resource;
    }
}

public struct ResourceCost
{
    public string resource;
    public float amount;
    /// <summary>
    /// Makes new list from old. Where there was repetition, all repeated instances of a resource are now totalled and put under a single instance.
    /// e.g. List with "Wood" 10, "Wood" 35 would be shortened to "Wood" 45.
    /// </summary>
    public static ResourceCost[] CollapseList(ResourceCost[] costs)
    {
        List<ResourceCost> totalCosts = new List<ResourceCost>(); //new list, if in the original costs for the same resource are repeated, here there will only be one instance for each resource, being the total.
        List<string> resourceNames = new List<string>();
        foreach (ResourceCost c in costs)
        {
            if (resourceNames.Contains(c.resource))
            {
                int index = resourceNames.IndexOf(c.resource);
                totalCosts[index] = new ResourceCost(c.resource, totalCosts[index].amount + c.amount); //Add on the repeated amount to the current amount.
            }
            else
            {
                totalCosts.Add(c);
                resourceNames.Add(c.resource);
            }
        }

        return totalCosts.ToArray();
    }

    /// <summary>
    /// Makes new list from old. Where there was repetition, all repeated instances of a resource are put under a single instance and the value is the maximum found.
    /// e.g. List with "Wood" 10, "Wood" 35 would be shortened to "Wood" 35.
    /// </summary>
    public static ResourceCost[] CollapseListDontTotal(ResourceCost[] costs)
    {
        List<ResourceCost> totalCosts = new List<ResourceCost>(); //new list, if in the original costs for the same resource are repeated, here there will only be one instance for each resource, being the total.
        List<string> resourceNames = new List<string>();
        foreach (ResourceCost c in costs)
        {
            if (resourceNames.Contains(c.resource))
            {
                int index = resourceNames.IndexOf(c.resource);
                if(totalCosts[index].amount < c.amount) totalCosts[index] = new ResourceCost(c.resource, c.amount); //Overwrite amount if greater
            }
            else
            {
                totalCosts.Add(c);
                resourceNames.Add(c.resource);
            }
        }

        return totalCosts.ToArray();
    }

    public ResourceCost(string resourceName, float costAmount)
    {
        resource = resourceName;
        amount = costAmount;
    }
}