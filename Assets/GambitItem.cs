using System.Collections.Generic;
using UnityEngine;

public class GambitSlot
{
    public bool EMPTY = true;
    public int count;
    public string name;
    public string sharedTexturePath = null;
    public GameObject owner;
    public string side;

    public GambitSlot(string name, int count)
    {
        this.EMPTY = (name == "GAMBIT:EMPTY");
        this.count = count;
        this.name = name;
        this.owner = null;
        this.side = null;
    }

    public Sprite GetSprite()
    {
        string texturePath = MissionControl.FindItemTexture(name);
        if (texturePath != null && texturePath != "")
        {
            Texture2D tex;
            Debug.Log("Assign texture : [" + texturePath + "]");
            tex = MissionControl.LoadTexture(texturePath);
            sharedTexturePath = texturePath;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero); ;
        }
        return null;
    }

    /// <summary>
    /// Prepare the object for save to disk
    /// </summary>
    /// <returns></returns>
    public object PrepareSaveData()
    {
        Dictionary<string, object> toSave = new Dictionary<string, object>();
        toSave.Add("EMPTY", EMPTY);
        toSave.Add("count", this.count);
        toSave.Add("name", this.name);
        return toSave;
    }
}
