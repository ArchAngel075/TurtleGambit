using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using static GambitBlock;

public class MissionControl : MonoBehaviour
{
    public static MissionControl Instance => GameObject.Find("MissionControl").GetComponent<MissionControl>();

    public GameObject GambitTurtleGameObject;
    public GameObject GambitBlockGameObject;
    public GameObject Overworld;
    Dictionary<string,GambitTurtle> Turtles;
    public Dictionary<string,List<string>> AirBlocks = new Dictionary<string,List<string>>();
    public bool SaveAllData;
    public bool LoadAllData;

    public event Action<GambitTurtle> OnTurtleCreated;


    // Start is called before the first frame update
    void Start()
    {
        Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Textures"));
        LoadData();
    }

    public static List<T> ConvertJArrayToList<T>(JArray jArray)
    {
        List<T> resultList = new List<T>();

        foreach (var item in jArray)
        {
            // Use the ToObject method to convert the JToken to the desired type
            T convertedItem = item.ToObject<T>();
            resultList.Add(convertedItem);
        }

        return resultList;
    }

    private void OnApplicationQuit()
    {
        SaveData();
    }

    // Update is called once per frame
    void Update()
    {
        if(SaveAllData)
        {
            SaveAllData = false;
            Debug.Log("Saving all data...");
            SaveData();
        }
        if (LoadAllData)
        {
            LoadAllData = false;
            Debug.Log("Loading all data...");
            LoadData();
        }
    }

    public GambitTurtle NewTurtle(string identity, string label, Vector3Int position, long direction, string dimension, JObject inventory)
    {
        Debug.Log("Making a new turtle instance from known details");
        GameObject go = Instantiate(GambitTurtleGameObject, position, Quaternion.identity);
        go.SetActive(true);
        Debug.Log("Assign relevant details...");
        GambitTurtle turtle = go.GetComponent<GambitTurtle>();
        turtle.Stream = new GambitStream();
        turtle.SetIdentity(identity);
        turtle.SetLabel(label);
        turtle.SetStatus(GambitTurtleStatus.Unloaded);
        GambitDimension dim = DimensionManager.Find(dimension);
        if (dim == null)
        {
            dim = DimensionManager.Find("overworld");
        }
        turtle.SetDimension(dim.name);
        //
        turtle.Slots = new Dictionary<int, GambitSlot>();
        for (int i = 1; i <= 16; i++)
        {
            turtle.Slots.Add(i, new GambitSlot("GAMBIT:EMPTY", 0));
        }
        foreach (var slot in inventory)
        {
            Debug.LogWarning("here is slot #" + (int.Parse(slot.Key)).ToString() + " -- " + (slot.Value).Value<string>("name"));
            turtle.Slots[int.Parse(slot.Key)] = new GambitSlot( (slot.Value).Value<string>("name"), (slot.Value).Value<int>("count"));
        }
        foreach (var slot in turtle.Slots)
        {
            Debug.LogWarning("read back is slot #" + (slot.Key).ToString() + " -- " + (slot.Value.name));
        }
        turtle.Direction = (CardinalDirectionEnum)direction;
        turtle.SetLocation(position.x, position.y, position.z);
        OnTurtleCreated.DynamicInvoke(turtle);
        return turtle;
    }

    public GambitTurtle NewTurtle(string identity, GambitStream from_stream, Vector3 position)
    {
        Debug.Log("Making a new turtle instance");
        GameObject go = Instantiate(GambitTurtleGameObject, position, Quaternion.identity);
        go.SetActive(true);
        Debug.Log("Assign relevant details...");
        GambitTurtle turtle = go.GetComponent<GambitTurtle>();
        turtle.Stream = from_stream;
        turtle.SetIdentity(identity);
        turtle.SetStatus(GambitTurtleStatus.Alive);
        //go.GetComponent<GambitTurtle>().setConnection(c);
        //this.Turtles.Add(identity, turtle);
        //go.transform.SetParent(Overworld.transform);
        GambitDimension dim = DimensionManager.Find("overworld");
        if(dim == null)
        {
            dim = DimensionManager.MakeDimension("overworld");
        }
        turtle.SetDimension(dim.name);
        Debug.Log("New turtle created in overworld!...");
        OnTurtleCreated.DynamicInvoke(turtle);
        return turtle;
    }

    public bool FindBlock(Vector3Int pos, GambitTurtle turtle)
    {
        return FindBlock(pos, turtle.Dimension);
    }

    public bool FindBlock(Vector3Int pos, GambitDimension dimension)
    {
        if (dimension == null)
        {
            return false;
        }
        return FindBlock(pos, dimension.name);
    }

    public bool FindBlock(Vector3Int pos, string dimension)
    {
        Debug.Log("find block?");
        return DimensionManager.Find(dimension).
            GetComponentsInChildren<GambitBlock>().
            FirstOrDefault(e => { return e.name == PositionToName(pos); })
            != null;
            
        //return GameObject.Find(GambitBlock.PositionToName(pos)) != null;
    }


    public GambitBlock GetBlock(Vector3Int pos, GambitTurtle turtle)
    {
        return GetBlock(pos, turtle.Dimension);
    }

    public GambitBlock GetBlock(Vector3Int pos, GambitDimension dimension)
    {
        if(dimension == null)
        {
            return null;
        }
        return GetBlock(pos, dimension.name);
    }

    public GambitBlock GetBlock(Vector3Int pos, string dimension)
    {
        Debug.Log("get block");
        return DimensionManager.Find(dimension).
            GetComponentsInChildren<GambitBlock>().
            FirstOrDefault(e => { return e.name == PositionToName(pos); }).
            GetComponent<GambitBlock>();
        //return GameObject.Find(GambitBlock.PositionToName(pos)).GetComponent<GambitBlock>();
    }

    public bool isPositionAir(Vector3Int pos, string dimension)
    {
        Debug.Log("is position air?");

        return GetOrMakeAirblockDimensionEntry(dimension).Contains("(" + pos.x + "_" + pos.y + "_" + pos.z + ")");
    }

    public bool TryGetBlock(Vector3Int pos, string dimension, out GambitBlock blockFound)
    {
        if (isPositionAir(pos,dimension))
        {
            blockFound = null;
            return true;
        }
        else if(FindBlock(pos, dimension))
        {
            blockFound = GetBlock(pos, dimension);
            return true;
        }
        blockFound = null;
        return false;
    }

    private List<string> GetOrMakeAirblockDimensionEntry(string dimension)
    {
        if(!AirBlocks.ContainsKey(dimension))
        {
            AirBlocks.Add(dimension, new List<string>());
        }
        return AirBlocks[dimension];
    }

    public void CreateAirBlock(Vector3Int pos, string dimension)
    {
        GetOrMakeAirblockDimensionEntry(dimension).Add("(" + pos.x + "_" + pos.y + "_" + pos.z + ")");
    }

    public void RemoveAirBlock(Vector3Int pos, string dimension)
    {
        GetOrMakeAirblockDimensionEntry(dimension).Remove("(" + pos.x + "_" + pos.y + "_" + pos.z + ")");
    }

    public GambitBlock ObserveBlock(dynamic observation, Vector3Int pos, string dimension)
    {
        return this.ObserveBlock((string)observation.details.name, pos, dimension, null);
    }

    public GambitBlock ObserveBlock(dynamic observation, Vector3Int pos, string dimension, string texPath)
    {
        return this.ObserveBlock((string)observation.details.name, pos, dimension, texPath);
    }

    public GambitBlock ObserveBlock(string blockName, Vector3Int pos, string dimension, string texPath)
    {
        Debug.Log("Observing block [" + blockName + "] at location " + pos.ToString());
        bool makeblock = false;
        //find block at position
        GambitBlock block;
        bool occupied = TryGetBlock(pos, dimension, out block);
        //either block or air?
        if (occupied)
        {
            Debug.Log("position is occupied:");
            //is it air ?
            if (block != null)
            {
                Debug.Log("position is occupied with a block");
                if( block.name != blockName)
                {
                    Debug.Log("position is occupied with a different block. destroy old and make new.");
                    Destroy(block.gameObject);
                    if(blockName == "GAMBIT:AIR" )
                    {
                        Debug.Log("position is currently occupied with a non air block. the new block is air. making air...");
                        CreateAirBlock(pos, dimension);
                    } else
                    {
                        Debug.Log("position is currently occupied with a non air block. the new block is not air. make block...");
                        if(block.name == blockName)
                        {
                            Debug.Log("position is currently occupied with a non air block. the new block is not air. new and old are same!");

                            makeblock = false;

                        } else
                        {
                            Debug.Log("position is currently occupied with a non air block. the new block is not air. new and old are different! make block...");
                            makeblock = true;

                        }
                    }
                }
            } else
            {
                Debug.Log("position is occupied with air");
                if (blockName != "GAMBIT:AIR")
                {
                    Debug.Log("the new position is not occupied with air, but the current position is. Remove the air and make block...");
                    RemoveAirBlock(pos, dimension);
                    makeblock = true;
                }
                else
                {
                    Debug.Log("the current position is occupied with air. and the new details describe air. no change!");
                }
            }
        } else
        {
            Debug.Log("the new position is not occupied, but the current position is.");
            if (blockName == "GAMBIT:AIR")
            {
                Debug.Log("the new position is not occupied, but the current position is occupied with air");
                CreateAirBlock(pos, dimension);
            } else
            {
                Debug.Log("the new position is not occupied, but the current position is occupied with something. make block...");
                makeblock = true;
            }

        }
        if (makeblock)
        {
            Debug.Log("<RUNNING> make block...");
            GameObject go = Instantiate(GambitBlockGameObject);
            go.name = GambitBlock.PositionToName(pos);
            GambitBlock newBlock = go.GetComponent<GambitBlock>();
            newBlock.blockName = blockName;
            newBlock.SetDimension(dimension);
            newBlock.SetLocation(pos);
            newBlock.TryProcessTexture(blockName, texPath);
            go.SetActive(true);
            return newBlock;
        } else {
            return null;
        }
    }

    public void SaveData()
    {
        string serverName = WebsocketServer.Instance.serverName;
        Debug.Log("Saving Blocks...");
        //Save Blocks :
        List<GambitBlock> blocks = FindObjectsByType<GambitBlock>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).ToList();
        //sort by type :
        Dictionary<string, List<object>> organised = new Dictionary<string, List<object>>();
        foreach (GambitBlock block in blocks)
        {
            if (!organised.ContainsKey(block.blockName))
            {
                organised.Add(block.blockName, new List<object>(new object[] { block.PrepareSaveData() }));
            }
            else
            {
                organised[block.blockName].Add(block.PrepareSaveData());
            }
        }
        string blockDataSerialized = JsonConvert.SerializeObject(organised);
        //now that we organised blocks lets serialize;
        Debug.Log("SERIALIAZED BLOCKS BOSS - " + blockDataSerialized);
        string blockSavePath = Path.Combine(Application.persistentDataPath, "Save", serverName, "blocks.sav");
        File.WriteAllText(blockSavePath, blockDataSerialized);
        //save turtles :


        Debug.Log("Saving Turtles...");
        //Save Blocks :
        List<GambitTurtle> turtles = FindObjectsByType<GambitTurtle>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID).ToList();
        //sort by type :
        Dictionary<string, object>  turtles_organised = new Dictionary<string, object>();
        foreach (GambitTurtle turtle in turtles)
        {
            turtles_organised.Add(turtle.name, turtle.PrepareSaveData());
        }
        string turtleDataSerialized = JsonConvert.SerializeObject(turtles_organised);
        //now that we organised blocks lets serialize;
        Debug.Log("SERIALIAZED TURTLES BOSS - " + turtleDataSerialized);
        string turtleSavePath = Path.Combine(Application.persistentDataPath, "Save", serverName, "turtles.sav");
        File.WriteAllText(turtleSavePath, turtleDataSerialized);

    }

    public void LoadData()
    {
        string serverName = WebsocketServer.Instance.serverName;
        //load blocks :
        string blockSavePath = Path.Combine(Application.persistentDataPath, "Save", serverName, "blocks.sav");
        Debug.LogError("Load from " + blockSavePath);
        string blockLoadData = File.ReadAllText(blockSavePath);
        Dictionary<string, List<Dictionary<string, object>>> blockNames = JsonConvert.DeserializeObject<Dictionary<string, List<Dictionary<string, object>>>>(blockLoadData);
        foreach (var blockNameData in blockNames)
        {
            string blockName = blockNameData.Key;
            List<Dictionary<string,object>> blocks = blockNameData.Value;
            foreach (dynamic block in blocks)
            {
                Debug.Log("load block instance : " + block["position"] + " -- " + (block["position"]).GetType() );
                Vector3Int vec3Int = (new Int3(block["position"])).asVec3Int();
                string dimension = "overworld";
                if (block.ContainsKey("dimension"))
                {
                    dimension = block["dimension"];
                }
                ObserveBlock(blockName, vec3Int, dimension, (string)block["texture"]);
            }

        }

        //load turtles :
        string turtleLoadPath = Path.Combine(Application.persistentDataPath, "Save", serverName, "turtles.sav");
        string turtleLoadData = File.ReadAllText(turtleLoadPath);
        Dictionary<string, Dictionary<string, object>> turtles = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, object>>>(turtleLoadData);
        foreach (var turtle in turtles)
        {
            string turtleName = turtle.Key;
            Dictionary<string, dynamic> turtleProperties = turtle.Value;
            Debug.Log("load turtle instance : " + turtleProperties["position"] + " -- " + (turtleProperties["position"]).GetType());
            Vector3Int vec3int = new Vector3Int(
                ((JObject)turtleProperties["position"]).Value<int>("x"),
                ((JObject)turtleProperties["position"]).Value<int>("y"),
                ((JObject)turtleProperties["position"]).Value<int>("z")
            );
            long dir = turtleProperties["direction"];
            Debug.Log("dir " + dir.ToString());
            string dim = (string)turtleProperties["dimension"];
            Debug.Log("dim " + dim.ToString());
            string id = turtleProperties["identity"];
            string label = turtleProperties["label"];
            Debug.Log("identity " + id);
            Debug.Log("make");

            GambitTurtle turtleco = NewTurtle(
                id,
                label,
                vec3int,
                dir,
                dim,
                turtleProperties["inventory"]
            );
            

        }

    }

    public static Texture2D LoadTexture( string path )
    {
        if (File.Exists(path))
        {
            byte[] fileData = File.ReadAllBytes(path);

            // Step 2: Create texture from bytes
            Texture2D texture = new Texture2D(16, 16);
            texture.LoadImage(fileData); // This assumes the fileData is a PNG byte array
            return texture;
        } else
        {
            throw new FileNotFoundException("Texture not found.", path);
        }
    }

    public static string FindItemTexture(string itemName)
    {
        Debug.Log("Try Find texture for item : [" + itemName + "]");
        if (itemName.StartsWith("minecraft:"))
        {
            string fileName = itemName.Replace("minecraft:", "") + ".png";
            string filePath = Path.Combine(Application.persistentDataPath, "Textures", "item", fileName);
            string filePath2 = Path.Combine(Application.persistentDataPath, "Textures", "block", fileName);
            Debug.Log("Try Find file '" + filePath + "'");
            if (File.Exists(filePath))
            {
                return filePath;
            }
            if(File.Exists(filePath2))
            {
                return filePath2;
            }
        }
        return null;
    }

    public static string FindBlockTexture(string blockName)
    {
        Debug.Log("Try Find texture for block : [" + blockName + "]");
        if(blockName.StartsWith("minecraft:"))
        {
            string fileName = blockName.Replace("minecraft:", "") +".png";
            string filePath = Path.Combine(Application.persistentDataPath,"Textures","block", fileName);
            Debug.Log("Try Find file '" + filePath + "'");
            if(File.Exists(filePath))
            {
                return filePath;
            }
        }
        return null;
    }
}
