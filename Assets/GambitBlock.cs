using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GambitBlock : MonoBehaviour
{
    private GambitLocation Location = new GambitLocation(0, 0, 0);
    public CardinalDirectionEnum Direction = CardinalDirectionEnum.North;
    public GambitDimension dimension;
    public string blockName;
    public string sharedTexturePath = null;

    public int LocationX;
    public int LocationY;
    public int LocationZ;

    public event Action<Dictionary<int,GambitSlot>> OnObserveInventory;

    public struct BlockDetails
    {
        public string name;
        public string state;
        public Dictionary<string, bool> tags;
    }

    public dynamic Details;
    public Dictionary<int, GambitSlot> inventory;

    public void TryProcessTexture(string blockName, string texturePath)
    {
        //update texture ?
        if(texturePath == null || texturePath == "")
        {
            Debug.Log("Find texture for me [" + blockName + "]");
            Texture2D tex;
            texturePath = MissionControl.FindBlockTexture(blockName);
            if(texturePath != null)
            {
                Debug.Log("Assign texture : [" + texturePath + "]");
                SetTexture(texturePath);
            }
        } else
        {
            Debug.Log("Reuse texture for me [" + blockName + "] [" + sharedTexturePath + "]");

            Texture2D tex;
            texturePath = MissionControl.FindBlockTexture(blockName);
            if (texturePath != null)
            {
                Debug.Log("Assign texture : [" + texturePath + "]");
                SetTexture(texturePath);
            }
        }
    }

    public void SetTexture(string texturePath)
    {
        Texture2D tex;
        if (texturePath != null && texturePath != "")
        {
            Debug.Log("Assign texture : [" + texturePath + "]");
            tex = MissionControl.LoadTexture(texturePath);
            sharedTexturePath = texturePath;
            GetComponent<Renderer>().material.mainTexture = tex;
        }
    }

    public struct Int3
    {
        public int x, y, z;

        public Int3(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Int3(JObject jo)
        {
            this.x = jo.Value<int>("x");
            this.y = jo.Value<int>("y");
            this.z = jo.Value<int>("z");
        }

        public Int3(Vector3Int vec3int)
        {
            this.x = vec3int.x;
            this.y = vec3int.y;
            this.z = vec3int.z;
        }

        public Vector3Int asVec3Int()
        {
            return new Vector3Int(x, y, z);
        }

        public Vector3 asVec3()
        {
            return new Vector3(x, y, z);
        }
    }

    /// <summary>
    /// Prepare the object for save to disk
    /// </summary>
    /// <returns></returns>
    public object PrepareSaveData()
    {
        Dictionary<string, object> toSave = new Dictionary<string, object>();
        Vector3Int pos = CoordinateConverter.UnityToMinecraft(this.transform.position);
        toSave.Add("position", new Int3(pos));
        toSave.Add("texture", sharedTexturePath);
        toSave.Add("dimension", dimension.name);

        if(inventory != null)
        {
            List<object> inventoryData = new List<object>();
            foreach (var slot in inventory)
            {
                inventoryData.Add(slot.Value.PrepareSaveData());
            }
            toSave.Add("inventory", inventoryData);
        }
        else
        {
            toSave.Add("inventory", null);
        }

        //toSave.Add("m")
        return toSave;
    }

    public static string PositionToName(Vector3Int pos)
    {
        return "Block ( " + pos.x + " , " + pos.y + " , " + pos.z + " )";
    }


    public void SetDimension(string dimension)
    {
        transform.SetParent(DimensionManager.Find(dimension).transform);
        this.dimension = DimensionManager.Find(dimension);
    }

    public void SetLocation(int x, int y, int z)
    {
        Debug.LogWarning("__OBSERVE__ SET MY POSITION BLOCK AS " + (new Vector3(x, y, z)) + " OR UNITY : " + CoordinateConverter.MinecraftToUnity(new Vector3Int(x, y, z)));
        this.transform.position = CoordinateConverter.MinecraftToUnity(new Vector3Int(x, y, z));
    }

    public void SetLocation(Vector3Int vec3)
    {
        Debug.LogWarning("__OBSERVE__ SET MY POSITION BLOCK AS " + (vec3) + " OR UNITY : " + CoordinateConverter.MinecraftToUnity(vec3));
        this.transform.position = CoordinateConverter.MinecraftToUnity(vec3);
    }

    public void Observe(string details) {
        BlockDetails Details = JsonUtility.FromJson<BlockDetails>(details);
        this.Details = Details;
    }

    public void ObserveInventory(Dictionary<int,GambitSlot> inventory)
    {
        this.inventory = inventory;
        if(this.OnObserveInventory != null)
        {
            Debug.LogError("inform those listening that I block {" + this.gameObject.name +"} has had my inventory observed.");
            this.OnObserveInventory.Invoke(inventory);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        LocationX = Location.x; LocationY = Location.y; LocationZ = Location.z;
        this.transform.rotation = Quaternion.Euler(0, CardinalDirectionUtility.ComputeUnityAngle(this.Direction), 0);
    }
}
