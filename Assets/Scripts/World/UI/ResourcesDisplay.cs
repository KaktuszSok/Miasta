using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ResourcesDisplay : MonoBehaviour {

    public Text text;
    public ResourcesHolder holder;

    float timer = 0.2f;

	// Use this for initialization
	void Awake () {
        text = GetComponent<Text>();
        WorldManager.OnWorldLoaded.AddListener(OnWorldLoaded);
	}

    void OnWorldLoaded()
    {
        holder = WorldPlayer.instance.ResourceInventory;
    }

    // Update is called once per frame
    void Update () {
		if(timer > 0)
        {
            timer -= Time.deltaTime;
        }
        else
        {
            timer = 0.2f;
            UpdateText();    
        }
	}

    public void UpdateText()
    {
        string s = "<b>Resources:</b>";
        foreach(string key in holder.ResourceList.Keys)
        {
            s += "\n" + key + ": " + (Mathf.Round(holder.GetResource(key)*1000)/1000f).ToString(); //Display to 3 decimal places
        }
        text.text = s;
    }
}
