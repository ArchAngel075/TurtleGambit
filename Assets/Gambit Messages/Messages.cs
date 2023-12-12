using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class EvalMessage : GambitMessage
{
    public new string Type = "EVALUATE";
   

    public EvalMessage(string payload, string turtleId)
    {
        this.Data = LuaUtilities.Build() + " " + payload;
        this.turtleId = turtleId;
    }

    public new string Prepare()
    {
        return JsonUtility.ToJson(this);
    }

    public void Send()
    {
        this.turtle.Send(JsonUtility.ToJson(this));
    }
}

public class MultiPartMessage : GambitMessage
{
    public new string Type = "MultiPartMessage";

    public string GUID;
    public List<MessagePart> Parts;
    public int CurrentPart;

    public static int MESSAGE_PART_SIZE = 1024;


    public MultiPartMessage(string data, string turtle_identity)
    {
        turtleId = turtle_identity;
        Data = data;
        Parts = new List<MessagePart>();
        GUID = Guid.NewGuid().ToString();
        CurrentPart = 0;
        int partNum = 0;
        // Break Data into 8192 byte chunks and create MessagePart for each chunk
        for (int i = 0; i < Data.Length; i += MESSAGE_PART_SIZE)
        {
            string chunk = Data.Substring(i, Math.Min(MESSAGE_PART_SIZE, Data.Length - i));
            Parts.Add(new MessagePart(GUID, chunk, partNum, turtleId));
            partNum++;
        }
    }

    public void SendParts(int[] parts = null) 
    {
        if (parts == null || parts.Length == 0)
        {
            //send all
            foreach (MessagePart part in Parts)
            {
                part.Send();
            }
        } else
        {
            //Debug.Log("Parts total possible : " + Parts.Count().ToString());
            //send those asked
            foreach ( int part in parts )
            {
                //Debug.Log("send part " + (part-1).ToString());
                Parts[part-1].Send();
            }
        }
    }

    public string Prepare()
    {
        return UnityEngine.JsonUtility.ToJson(Parts[CurrentPart]);
    }

    public void SelectPart(int i)
    {
        CurrentPart = i;
    }

    private class MultiPartMessageHeader
    {
        public string Type = "MultiPartMessageHeader";
        public int Expected;
        public string GUID;
    }

    public string PrepareHeader()
    {
        MultiPartMessageHeader header = new MultiPartMessageHeader();
        header.Expected = Enumerable.Range(0, Parts.Count).Count();
        header.GUID = GUID;
        return UnityEngine.JsonUtility.ToJson(header);
    }

    private class MultiPartMessageConfirmationRequest
    {
        public string Type = "MultiPartMessageConfirmationRequest";
        public int Expected;
        public string GUID;
    }

    public string PrepareConfirmation()
    {
        MultiPartMessageConfirmationRequest request = new MultiPartMessageConfirmationRequest();
        request.Expected = Enumerable.Range(0, Parts.Count).Count();
        request.GUID = GUID;
        return UnityEngine.JsonUtility.ToJson(request);
    }

    private class MultiPartMessageExecute
    {
        public string Type = "MultiPartMessageExecute";
    }

    public string PrepareExecuteRequest()
    {
        MultiPartMessageExecute request = new MultiPartMessageExecute();
        return UnityEngine.JsonUtility.ToJson(request);
    }

    public void Execute()
    {
        turtle.Stream.connection.Dispatch(PrepareExecuteRequest());
    }
}

public class MultiPartMessagePleaseSend
{
    private struct Content {
        public string GUID; public int[] parts;
    }

    public new string Type = "MultiPartMessagePleaseSend";

    public GambitMessage baseMessage;
    public string GUID
    {
        get
        {
            return JsonUtility.FromJson<Content>(baseMessage.Data).GUID;
        }
    }

    public int[] parts
    {
        get
        {
            return JsonUtility.FromJson<Content>(baseMessage.Data).parts;
        }
    }

    public static MultiPartMessagePleaseSend From(GambitMessage message)
    {
        Debug.Log("make MPM:PS!");
        return new MultiPartMessagePleaseSend()
        {
            baseMessage = message
        };
    }

    public void ResendRequestedParts()
    {
        //find turtle :
        GameObject go = GameObject.Find("GambitTurtle (" + baseMessage.turtleId + ")");
        if (go)
        {
            GambitTurtle turtle = go.GetComponent<GambitTurtle>();
            turtle.MultiPartMessageStatus = "PREPARE_RESEND";
            turtle.multiPartMessage.SendParts(this.parts);
            turtle.MultiPartMessageStatus = "SEND_TURTLE_CONFIRMATION";
            Debug.LogWarning("Dispatch to turtle ResendRequestedParts");
            turtle.Stream.connection.Dispatch(turtle.multiPartMessage.PrepareConfirmation());
        }
        else
        {
            Debug.LogError("Unable to find Turtle of identity " + baseMessage.turtleId);
        }
    }
}


public class MessagePart : GambitMessage
{
    public new string Type = "MultiPartMessagePart";
    public string MultiPartGUID;
    public int PartNum;

    public MessagePart(string multiPartGUID, string data, int partNum, string turtleId)
    {
        MultiPartGUID = multiPartGUID;
        Data = data;
        PartNum = partNum;
        this.turtleId = turtleId;
    }

    private class MessageContent
    {
        public string GUID;
        public string Data;
    }

    public new string Prepare()
    {
        return JsonUtility.ToJson(this);
    }

    public void Send()
    {
        //find turtle :
        GameObject go = GameObject.Find("GambitTurtle (" + turtleId + ")");
        if (go)
        {
            GambitTurtle turtle = go.GetComponent<GambitTurtle>();
            turtle.Stream.connection.Dispatch(Prepare());
        } else
        {
            Debug.LogError("Unable to Find turtle object :" + turtleId);
        }
    }
}

public class GambitLocation
{
    public int x;
    public int y;
    public int z;
    
    public GambitLocation(int x, int y, int z)
    {
        this.x = x; 
        this.y = y;
        this.z = z;
    }

}

/// <summary>
/// Base Observation class, outlines useful methods and properties.
/// </summary>
public class ObservationMessage
{
    public string ObservationType;
    string Data;
    public string turtleId;

    public static void Handle(GambitMessage gambitMessage)
    {
        //Debug.LogError("On Observation Static from " + gambitMessage.Data);

        GambitTurtle sourceTurtle = gambitMessage.turtle;
        ObservationMessage obs = JsonUtility.FromJson<ObservationMessage>(gambitMessage.Data);
        obs.Data = gambitMessage.Data; 

        if (obs.ObservationType == "SELF")
        {
            ObservationMessageSelf alt = JsonUtility.FromJson<ObservationMessageSelf>(gambitMessage.Data);
            alt.turtleId = gambitMessage.turtleId;
            alt.Data = gambitMessage.Data;
            //N Z subtracts
            //W X subtracts
            //
            //\
            Debug.LogWarning("Observed a location! [" + alt.x.ToString() + "]");
            sourceTurtle.SetLocation(alt.x,alt.y,alt.z);

            return;
        }


        if (obs.ObservationType == "SELF_ASSUME")
        {
            ObservationMessageSelfAssumed alt = JsonUtility.FromJson<ObservationMessageSelfAssumed>(gambitMessage.Data);
            alt.turtleId = gambitMessage.turtleId;
            alt.Data = gambitMessage.Data;
            //N Z subtracts
            //W X subtracts
            //
            Vector3Int p;
            if (alt.change == "forward")
            {
                p = CoordinateConverter.MinecraftToUnity(sourceTurtle.transform.position+sourceTurtle.transform.forward);
                sourceTurtle.SetLocation(p.x, p.y, p.z);
            }
            if (alt.change == "up")
            {
                p = CoordinateConverter.MinecraftToUnity(sourceTurtle.transform.position + sourceTurtle.transform.up);
                sourceTurtle.SetLocation(p.x, p.y, p.z);
            }
            if (alt.change == "down")
            {
                p = CoordinateConverter.MinecraftToUnity(sourceTurtle.transform.position + (-sourceTurtle.transform.up));
                sourceTurtle.SetLocation(p.x, p.y, p.z);
            }
            if (alt.change == "back")
            {
                p = CoordinateConverter.MinecraftToUnity(sourceTurtle.transform.position + (-sourceTurtle.transform.forward));
                sourceTurtle.SetLocation(p.x, p.y, p.z);
            }

            return;
        }

        if (obs.ObservationType == "ROTATION")
        {
            ObservationMessageRotation alt = JsonUtility.FromJson<ObservationMessageRotation>(obs.Data);
            alt.turtleId = gambitMessage.turtleId;
            alt.Data = gambitMessage.Data;
            //N Z subtracts
            //W X subtracts
            //
            //
            Debug.LogWarning("Observed a rotation!");
            if(alt.change == "LEFT")
            {
                
                sourceTurtle.RotateLeft();
            } else
            {
                sourceTurtle.RotateRight();
            }
            return;
        }

        if (obs.ObservationType == "BLOCK_FORWARD")
        {
            Debug.Log("Observed BLOCK FORWARD [" + obs.Data + "]");
            dynamic jsonObject = JsonConvert.DeserializeObject(obs.Data);
            jsonObject.turtleId = gambitMessage.turtleId;
            jsonObject.Data = gambitMessage.Data;
            
            //N Z subtracts
            //W X subtracts
            //
            //
            Debug.LogWarning("Observed a block! Details : [" + jsonObject.details.name + "]");
            sourceTurtle.ObserveBlock(jsonObject, "forward", sourceTurtle.Dimension.name);


            //return jsonObject;
            return;
        }

        if (obs.ObservationType == "BLOCK_UP")
        {
            Debug.Log("Observed BLOCK UP [" + obs.Data + "]");
            dynamic jsonObject = JsonConvert.DeserializeObject(obs.Data);
            jsonObject.turtleId = gambitMessage.turtleId;
            jsonObject.Data = gambitMessage.Data;

            //N Z subtracts
            //W X subtracts
            //
            //
            Debug.LogWarning("Observed a block! Details : [" + jsonObject.details.name + "]");
            sourceTurtle.ObserveBlock(jsonObject, "up", sourceTurtle.Dimension.name);


            //return jsonObject;
            return;
        }

        if (obs.ObservationType == "BLOCK_DOWN")
        {
            Debug.Log("Observed BLOCK DOWN [" + obs.Data + "]");
            dynamic jsonObject = JsonConvert.DeserializeObject(obs.Data);
            jsonObject.turtleId = gambitMessage.turtleId;
            jsonObject.Data = gambitMessage.Data;

            //N Z subtracts
            //W X subtracts
            //
            //
            Debug.LogWarning("Observed a block! Details : [" + jsonObject.details.name + "]");
            sourceTurtle.ObserveBlock(jsonObject, "down", sourceTurtle.Dimension.name);


            //return jsonObject;
            return;
        }

        if (obs.ObservationType == "INVENTORY")
        {
            Debug.LogError("INVT");
            JObject jsonObject = JsonConvert.DeserializeObject<JObject>(obs.Data);
            foreach (var slot in jsonObject.Value<JArray>("slots"))
            {
                int index = slot.Value<int>("index");
                JObject slotData = slot.Value<JObject>("data");
                string name = slotData.Value<string>("name");
                int count = slotData.Value<int>("count");

                Debug.LogError("Observe slot #" + index + " contains x" + count + " of " + name);

                sourceTurtle.ObserveSlot(index, name, count);
            }
            //jsonObject.turtleId = gambitMessage.turtleId;
            // jsonObject.Data = gambitMessage.Data;
            //foreach (JObject slots in jsonObject.Data)
            //{
            //    Debug.LogError("observed slot index " + slots.Value<int>("index"));
            //}


            return;
        }

        if (obs.ObservationType == "SLOT_DETAIL")
        {
            dynamic jsonObject = JsonConvert.DeserializeObject(obs.Data);
            jsonObject.turtleId = gambitMessage.turtleId;
            jsonObject.Data = gambitMessage.Data;

            sourceTurtle.ObserveSlot((int)jsonObject.slot, (string)jsonObject.name, (int)jsonObject.count);

            return;
        }

        if(obs.ObservationType == "SLOT_SELECTED")
        {
            dynamic jsonObject = JsonConvert.DeserializeObject(obs.Data);
            jsonObject.turtleId = gambitMessage.turtleId;
            jsonObject.Data = gambitMessage.Data;
            Debug.LogWarning("SELECTED SLOT '" + jsonObject.slot+ "'");

            sourceTurtle.SetSelectedSlot((int)jsonObject.slot);

            return;
        }

        if (obs.ObservationType == "EXTERNAL_INVENTORY")
        {
            dynamic jsonObject = JsonConvert.DeserializeObject(obs.Data);
            jsonObject.turtleId = gambitMessage.turtleId;
            jsonObject.Data = gambitMessage.Data;
            JArray items = jsonObject.items;
            int size = jsonObject.size;
            string side = jsonObject.side;

            Dictionary<int, GambitSlot> slots = new Dictionary<int, GambitSlot>();

            Debug.LogError("Observed Inventory at side " + side.ToString());
            for (int i = 1; i <= size; i++)
            {
                JToken item = items.FirstOrDefault(e => { return e.Value<int>("slot") == i; });
                if (item != null)
                {
                    Debug.Log("Slot[" + i.ToString() + "]\t" + item.Value<string>("name") + "\t" + "x" + item.Value<string>("count") + ".");
                    GambitSlot slot = new GambitSlot(item.Value<string>("name"), item.Value<int>("count"));
                    slot.side = side;
                    slots.Add(i, slot);
                } else
                {
                    Debug.Log("Slot[" + i.ToString() + "]\t" + "empty" + "\tn/a.");
                    GambitSlot slot = new GambitSlot("GAMBIT:EMPTY", -1);
                    slot.side = side;
                    slots.Add(i, slot);
                }
            }

            Debug.LogError("start trying to inform the block -- " + side.ToString());
            //let us find, and then inform a block at that position on side of the turtle about the contents of its damn inventory :
            Vector3Int blockPosInMC = sourceTurtle.PositionOffsetBySide(side);
            Debug.LogError("pos based on side -- " + blockPosInMC.ToString());
            bool blockExists = MissionControl.Instance.FindBlock(blockPosInMC, sourceTurtle.Dimension.name);
            Debug.LogError("block found based on side -- " + blockExists.ToString());
            if (blockExists)
            {
                GambitBlock blockObserved = MissionControl.Instance.GetBlock(blockPosInMC, sourceTurtle.Dimension.name);
                Debug.LogError("inform that block it has had its inventory observed." + blockObserved);
                blockObserved.ObserveInventory(slots);
                sourceTurtle.ObserveInventory(blockObserved, side);
            } else
            {
                throw new Exception("Unable to find block by side offset @ " + side);
            }
            //below we shall move to the desire to OPEN the ui not observe an block inventory.
            //UIReference.instance.ExternalInventory.GetComponent<ExternalInventoryUI>().Prepare(slots, side);
            //UIReference.instance.ExternalInventory.SetActive(true);


            return;
        }

        if (obs.ObservationType == "FUEL")
        {
            dynamic jsonObject = JsonConvert.DeserializeObject(obs.Data);
            jsonObject.turtleId = gambitMessage.turtleId;
            jsonObject.Data = gambitMessage.Data;
            sourceTurtle.FuelLevel = ((JObject)jsonObject).Value<int>("level");
            sourceTurtle.FuelLimit = ((JObject)jsonObject).Value<int>("limit");

        }


        Debug.LogWarning("Unknown Observation Type '" + obs.ObservationType + "'");
        return;
    }
}
public class ObservationMessageSelf : ObservationMessage
{
    public int x;
    public int y;
    public int z;

}

public class ObservationMessageSelfAssumed : ObservationMessage
{
    public string change;

}

public class ObservationMessageRotation : ObservationMessage
{
    public string change;
}

public class ObservationMessageBlock : ObservationMessage
{
    public dynamic details;
}
