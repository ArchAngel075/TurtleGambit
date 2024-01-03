using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static GambitBlock;

public enum GambitTurtleStatus
{
    Alive, 
    Unloaded,
    Stale,
    // Add more statuses as needed
}

public class GambitTurtle : MonoBehaviour
{
    public string Identity { get; private set; }
    public string Label;
    public GambitTurtleStatus Status;
    public GambitStream Stream;
    public bool DebugSend = false;
    public string _eval = "";
    private GambitLocation Location = new GambitLocation(0,0,0);
    public CardinalDirectionEnum Direction = CardinalDirectionEnum.North;
    public GambitDimension Dimension;
    public int FuelLevel = 0;
    public int FuelLimit = 0;


    public Dictionary<int, GambitSlot> Slots = new Dictionary<int, GambitSlot>();
    [Range(1,16)]
    public int selectedSlot = 1;

    [Range(1, 16)]
    public int selectSlot = 1;
    public bool selectNewSlot = false;

    public string selectedItemName;
    public int selectedItemCount;

    public bool action_forward = false;
    public bool action_left = false;
    public bool action_right = false;
    public bool action_backward = false;

    public bool action_observe = false;
    public bool pingTurtle = false;

    public string MultiPartMessageStatus = "NONE";
    public MultiPartMessage multiPartMessage = null;


    public event Action<Dictionary<int, GambitSlot>> OnSlotsChanged;
    public event Action<int> OnSelectedSlotChanged;
    public event Action<int> OnFuelStateUpdate;

    public event Action<GambitBlock,string> OnObserveExternalInventory;


    public static GambitTurtle Find(string turtleId)
    {
        return GameObject.Find("GambitTurtle (" + turtleId + ")").GetComponent<GambitTurtle>();
    }

    public static GambitTurtle FindExact(string turtleId)
    {
        return GameObject.Find(turtleId).GetComponent<GambitTurtle>();
    }

    public static GambitTurtle FindByLabel(string Label)
    {
        return GameObject.FindObjectsOfType<GambitTurtle>().ToList().FirstOrDefault(t => t.Label == Label);
    }

    public void ClearMultiPartMessageStatus()
    {
        this.multiPartMessage = null;
        this.MultiPartMessageStatus = "NONE";
    }

    public void FetchInventory()
    {
        /*
         * for (int i = 1; i < Slots.Count + 1; i++)
        {
            FetchSlotDetails(i);
        }
        */
        Send("helpers.observeInventory()");
    }

    public void FetchSelectedSlotIndex()
    {
        Send("helpers.getSelectedSlot();");
    }

    public void FetchSelectedSlotDetails()
    {
        FetchSlotDetails(selectedSlot);
    }

    public void FetchSlotDetails(int slot)
    {
        Send("helpers.getSlotDetails( " + slot + ");");
    }

    private void Start()
    {
        for (int i = 1;  i <= 16; i++) 
        {
            Slots[i] = new GambitSlot("GAMBIT:EMPTY", 0);
            FetchSlotDetails(i);
        }
    }

    public void SetLabel(string label)
    {
        this.Label = label;
    }

    public void SelectSlot(int slot)
    {
        Send("helpers.select( " + slot + ");");
    }

    public void SetDimension(string name)
    {
        this.Dimension = DimensionManager.Find(name);
        this.transform.SetParent(this.Dimension.transform);
        //TMPro.TMP_Dropdown dp = UIReference.instance.Rotation.GetComponent<TMPro.TMP_Dropdown>();
        //int option = dp.options.FindIndex(e => { return e.text == name; });
        //if (option == -1)
        //{
        //    dp.options.Add(new TMPro.TMP_Dropdown.OptionData(name));
        //    option = dp.options.Count - 1;
        //}
        //dp.SetValueWithoutNotify(option);
    }

    public void SetLocation(int x, int y, int z)
    {
        //this.Location = new GambitLocation(x,y,z);
        Debug.LogError("FROM INPUTS OF " + "(" + x + "," + y + "," + z + ")");
        this.transform.position = CoordinateConverter.MinecraftToUnity(new Vector3Int(x, y, z));
        TMPro.TMP_InputField dpx = UIReference.instance.PositionX.GetComponent<TMPro.TMP_InputField>();
        TMPro.TMP_InputField dpy = UIReference.instance.PositionY.GetComponent<TMPro.TMP_InputField>();
        TMPro.TMP_InputField dpz = UIReference.instance.PositionZ.GetComponent<TMPro.TMP_InputField>();
        dpx.SetTextWithoutNotify(x.ToString());
        dpy.SetTextWithoutNotify(y.ToString());
        dpz.SetTextWithoutNotify(z.ToString());
        //update rendering of nearby blocks so that they reflect :
        MissionControl.Instance.UpdateBlockRenderRespectTo(this.transform.position, 12);

    }

    public void RotateLeft()
    {
        this.Direction = CardinalDirectionUtility.RotateLeft(this.Direction);
        this.transform.rotation = Quaternion.Euler(0, CardinalDirectionUtility.ComputeUnityAngle(this.Direction), 0);
    }

    public void RotateRight()
    {
        this.Direction = CardinalDirectionUtility.RotateRight(this.Direction);
        this.transform.rotation = Quaternion.Euler(0, CardinalDirectionUtility.ComputeUnityAngle(this.Direction), 0);
    }


    public void SetIdentity(string identity)
    {
        this.Identity = identity;
        this.gameObject.name = "GambitTurtle (" + Identity + ")";
    }

    public void Initialize(string identity)
    {
        Identity = identity;
        Status = GambitTurtleStatus.Unloaded; // Default status when initialized
    }

    public void ObserveSlot(int slotIndex, string slotContentName, int slotContentCount)
    {
        Slots[slotIndex] = new GambitSlot(slotContentName, slotContentCount);
        if(OnSlotsChanged != null)
        {
            OnSlotsChanged.DynamicInvoke(Slots);
        }
    }

    public void ObserveInventory(GambitBlock block, string side)
    {
        if(OnObserveExternalInventory != null)
        {
            OnObserveExternalInventory.Invoke(block, side);
        }
    }

    public void SetSelectedSlot(int to)
    {
        this.selectedSlot = to;
        if(OnSelectedSlotChanged != null)
        {
            OnSelectedSlotChanged.DynamicInvoke(to);
        }
    }

    /// <summary>
    /// Prepare the object for save to disk
    /// </summary>
    /// <returns></returns>
    public object PrepareSaveData()
    {
        Debug.LogError("PREPARING TURTLE SAVE DATA");
        Dictionary<string, object> toSave = new Dictionary<string, object>();
        Vector3Int pos = CoordinateConverter.UnityToMinecraft(this.transform.position);
        Debug.LogError("Z POS AS " + "(" + pos.x + "," + pos.y + "," + pos.z + ")");
        toSave.Add("position", pos);
        toSave.Add("direction", this.Direction);
        toSave.Add("dimension", this.Dimension.name);
        toSave.Add("identity", this.Identity);
        toSave.Add("label", this.Label);

        Dictionary<int, object> slots = new Dictionary<int, object>();
        foreach (var item in Slots)
        {
            object data = item.Value.PrepareSaveData();
            slots[item.Key] = data;
        }
        toSave.Add("inventory", slots);
        Debug.LogError("TURTLE SAVE DATA PREPARED");
        //toSave.Add("m")
        return toSave;
    }

    public void ObserveBlock(dynamic observation, string relativeDir, string dimension)
    {
        Debug.LogWarning("__OBSERVE__ block [" + observation.details.name + " in direction [" + relativeDir + "]");
        //pos = my position in minecraft;
        //offset = my position in minecraft forward?
        Vector3Int pos = new Vector3Int((int)this.transform.position.x, (int)this.transform.position.y, (int)this.transform.position.z);
        if(relativeDir == "forward")
        {

            //Debug.LogWarning("__OBSERVE__ I am at location " + this.transform.position + " and forwards is " + this.transform.forward + " and the sum of those is " + (this.transform.position + this.transform.forward).ToString());
            //Debug.LogWarning("__OBSERVE__ IN MINECRAFT I am at location " + pos + " and forwards is " + CoordinateConverter.UnityToMinecraft(this.transform.forward) + " and the sum of those is " + (pos + CoordinateConverter.UnityToMinecraft(this.transform.forward)).ToString());
            //Debug.LogWarning("__OBSERVE__ ALTERNATIVELY I am at location " + this.transform.position + " and forwards is " + this.transform.forward + " and the sum of those is " + (this.transform.position + this.transform.forward).ToString().ToString() + " and in MINECRAFT that would be " + CoordinateConverter.UnityToMinecraft(this.transform.position + this.transform.forward).ToString());
            //for some goddamn reason converting pos and forwards and then summing causes wierd things to happen. so this is what i have to do instead.
            pos = CoordinateConverter.UnityToMinecraft(this.transform.forward+this.transform.position);

        }
        else if (relativeDir == "down")
        {
            pos = CoordinateConverter.UnityToMinecraft(this.transform.position - this.transform.up);
        }
        else if (relativeDir == "up")
        {
            pos = CoordinateConverter.UnityToMinecraft(this.transform.position + this.transform.up);
        }
        Debug.LogWarning("__OBSERVE__ the position i will be set as is :" + pos);
        MissionControl.Instance.ObserveBlock(observation, pos, dimension);
        MissionControl.Instance.UpdateBlockRenderRespectTo(this.transform.position, 12);
    }

    public void ObserveBlock(dynamic observation, Vector3Int offset, string dimension)
    {
        Debug.LogWarning("__OBSERVE__ block [" + observation.details.name + " with offset [" + offset + "] / " + CoordinateConverter.MinecraftToUnity(offset));
        //pos = my position in minecraft;
        //offset = my position in minecraft forward?
        Vector3Int pos = new Vector3Int((int)this.transform.position.x, (int)this.transform.position.y, (int)this.transform.position.z);
        pos = offset + CoordinateConverter.UnityToMinecraft(pos);
        Debug.LogWarning("__OBSERVE__ the position i will be set as is :" + pos);
        MissionControl.Instance.ObserveBlock(observation, pos, dimension);
    }

    /// <summary>
    /// Returns the Minecraft position of a unity location offset by some side, returns the current position IF side is invalid.
    /// This should probably toss a exception or use a typed side enum in future
    /// </summary>
    /// <param name="side">the side, in lowercase form and variance agnostic</param>
    /// <returns>
    /// vector3 offset side to the turtle
    /// </returns>
    public Vector3Int PositionOffsetBySide(string side)
    {
        Debug.LogError("offset by side " + side);
        Vector3Int pos = CoordinateConverter.UnityToMinecraft(this.transform.position);
        if (side == "forward" || side == "front")
        {
            pos = CoordinateConverter.UnityToMinecraft(this.transform.position + this.transform.forward);
        }
        else if (side == "down" || side == "bottom")
        {
            pos = CoordinateConverter.UnityToMinecraft(this.transform.position - this.transform.up);
        }
        else if (side == "up" || side == "top")
        {
            pos = CoordinateConverter.UnityToMinecraft(this.transform.position+this.transform.up);
        }
        else if (side == "right")
        {
            pos = CoordinateConverter.UnityToMinecraft(this.transform.position+this.transform.right);
        }
        else if (side == "left")
        {
            pos = CoordinateConverter.UnityToMinecraft(this.transform.position - this.transform.right);
        }

        return pos;
    }

    public void SetStatus(GambitTurtleStatus newStatus)
    {
        Status = newStatus;
        // You can add more logic here based on the new status if needed
    }

    /// <summary>
    /// Send to the turtle some payload
    /// </summary>
    /// <param name="message">The payload to send</param>
    public void Send(string message)
    {
        if (this.Stream.connection == null)
        {
            return;
        }
        message = message.Replace(";;", ";");
        string to_send = new EvalMessage(message, this.Identity).Prepare();
        this.Stream.to_turtle_messages.Enqueue(to_send);  
    }

    public void FetchWorldPosition()
    {
        Send("helpers.observeMyLocation();");
    }

    private void Update()
    {
        if(pingTurtle)
        {
            this.Stream.connection.doPing();
            pingTurtle = false;
        }
        this.selectedItemCount = Slots[selectedSlot].count;
        this.selectedItemName = Slots[selectedSlot].name;

        if (selectNewSlot)
        {
            selectNewSlot = false;
            Send("helpers.select(" + selectSlot + ");");
        }

        if (action_forward) { Send("helpers.forward();"); }
        if(action_backward) { Send("helpers.backward();"); }
        if(action_left) { Send("helpers.turnLeft();"); }
        if(action_right) { Send("helpers.turnRight();"); }
        if(action_observe) { Send("helpers.observeFront();"); }

        action_forward = false;
        action_backward = false;
        action_left = false;
        action_right = false;
        action_observe = false;


        this.transform.rotation = Quaternion.Euler(0, CardinalDirectionUtility.ComputeUnityAngle(this.Direction), 0);
        if (DebugSend)
        {
            Send(_eval);
            DebugSend = false;
        }

        processDispatchQueue();
    }

    public bool isStale()
    {
        if(this.Stream.connection != null)
        {
            return this.Stream.connection.isStale;
        }
        return true;
    }

    public void processDispatchQueue()
    {

        //1. if we dont have a current message then process a new message
        //2. if we have a current message then dispatch those marked MISSING
        if (Stream.connection == null)
        {
            return;
        }
        if (this.multiPartMessage == null)
        {
            if (Stream.to_turtle_messages.TryDequeue(out var message))
            {
                //Debug.Log("pulled outgoing message : " + message);
                if (message != "")
                {
                    Debug.Log(message.ToString() + message.Length.ToString() );

                    if (message.Length < MultiPartMessage.MESSAGE_PART_SIZE)
                    {
                        Debug.LogWarning("Message is short. send as is! " + message.Length.ToString());
                        Stream.connection.Dispatch(message);
                    }
                    else
                    {
                        Debug.LogWarning("Message is long. using multipart system!");
                        MultiPartMessageStatus = "CONSTRUCTING";
                        //Debug.LogWarning("please send the message : '" + to_send + "'"); 
                        MultiPartMessage m = new MultiPartMessage(message, this.Identity);
                        //sets a lock on the dispatcher --
                        this.multiPartMessage = m;
                        //no other message will be allowed to send with this lock until the "EXECUTE" message is sent.
                        //send a header...
                        MultiPartMessageStatus = "HEADER";
                        Stream.connection.Dispatch(this.multiPartMessage.PrepareHeader());
                        //later we will receive a OK or RESEND message...
                        MultiPartMessageStatus = "SEND_CONFIRMATION";
                        Stream.connection.Dispatch(this.multiPartMessage.PrepareConfirmation());
                    }
                }
            }
        } else
        {
            MultiPartMessageStatus = "WAIT_TURTLE_CONFIRMATION";
            //there should be event driven effect of confirmation, resend, confirmation, execute.
        }

    }



    public void MoveForward()
    {
        Send("helpers.forward();");
    }

    public void MoveBackward()
    {
        Send("helpers.back();");
    }

    public void MoveUp()
    {
        Send("helpers.up();");
    }

    public void MoveDown()
    {
        Send("helpers.down();");
    }

    public void TurnLeft()
    {
        Send("helpers.turnLeft();");
    }
    public void TurnRight()
    {
        Send("helpers.turnRight();");
    }

    //observe
    public void ObserveFront()
    {
        Send("helpers.observeFront();");
    }

    public void ObserveDown()
    {
        Send("helpers.observeDown();");
    }

    public void ObserveUp()
    {
        Send("helpers.observeUp();");
    }

    //place
    public void PlaceFront()
    {
        Send("helpers.place();");
    }

    public void PlaceDown()
    {
        Send("helpers.placeDown();");
    }

    public void PlaceUp()
    {
        Send("helpers.placeUp();");
    }

    //dig
    public void DigFront()
    {
        Send("helpers.dig();");
    }

    public void DigDown()
    {
        Send("helpers.digDown();");
    }

    public void DigUp()
    {
        Send("helpers.digUp();");
    }

    //attack
    public void AttackFront()
    {
        Send("helpers.attack();");
    }

    public void AttackDown()
    {
        Send("helpers.attackDown();");
    }

    public void AttackUp()
    {
        Send("helpers.attackUp();");
    }

    //drop
    public void DropFront()
    {
        Send("helpers.drop();");
    }

    public void DropDown()
    {
        Send("helpers.dropDown();");
    }

    public void DropUp()
    {
        Send("helpers.dropUp();");
    }

    //Use
    public void UseFront()
    {
        Send("helpers.use();");
    }

    public void UseDown()
    {
        Send("helpers.useDown();");
    }

    public void UseUp()
    {
        Send("helpers.useUp();");
    }

    public void TransferFromTo(int from, int to, int count=1)
    {
        Send("helpers.transferTo(" + from +","+ to + ", " + count +");");
    }

    public void Craft()
    {
        Send("helpers.craft();");
    }

    public void Put(string side, int slotFrom, int count, int slotTo)
    {
        //Send("put(\"" + side + "\"," + slotFrom + "," + count + "," + slotTo + ");");
        /////
        Debug.LogError("Intent to: Put `" + side + "`");
        bool found = false;
        GambitBlock b = null;
        Vector3Int blockPos;

        if (side == "front" || side == "")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.forward + this.transform.position);
        }
        else if (side == "down")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position - this.transform.up);
        }
        else if (side == "up")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position + this.transform.up);
        }
        else
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position);
        }
        found = MissionControl.Instance.TryGetBlock(blockPos, this.Dimension.name, out b);

        if (found)
        {
            Debug.LogError("Put Intent: block Found --" + b.blockName);

            Driver driver = DriverManager.Instance.FindDriverForEvent(DriverManager.DrivenEvents.OnMoveFromTurtleToBlock, b.blockName);
            Debug.LogError("Found driver for event `OnMoveFromTurtleToBlock` => " + driver.GetNamespace());
            string function = driver.GetFunctionForEvent(DriverManager.DrivenEvents.OnMoveFromTurtleToBlock);
            string call = Driver.AttachParams(function, new string[] {
                Driver.StringyParam(side),
                slotFrom.ToString(),
                count.ToString(),
                slotTo.ToString()
            });
            Debug.LogError("Try and call the following on turtle : `" + call + "`");
            Send(call);
        }
        else
        {
            Debug.LogError("Put Intent, block NOT FOUND");
        }
    }
    public void moveExternal(string side, int slotFrom, int count, int slotTo)
    {
        Debug.LogError("Move external(");
        Debug.LogError("side " + side);
        Debug.LogError("slotFrom " + slotFrom);
        Debug.LogError("count " + count);
        Debug.LogError("slotTo " + slotTo);
        Debug.LogError(")");
        //Send("moveExternal(\"" + side + "\"," + slotFrom + "," + count + "," + slotTo + ");");
        /////////====++++

        Debug.LogError("Intent to: Open " + side);
        bool found = false;
        GambitBlock b = null;
        Vector3Int blockPos;

        if(side == "front")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.forward + this.transform.position);
        } else if (side == "down")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position - this.transform.up);
        } else if (side == "up")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position + this.transform.up);
        } else
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position);
        }
        found = MissionControl.Instance.TryGetBlock(blockPos, this.Dimension.name, out b);

        if (found)
        {
            Debug.LogError("Open Intent: block Found --" + b.blockName);

            Driver driver = DriverManager.Instance.FindDriverForEvent(DriverManager.DrivenEvents.OnMoveItemInsideBlock, b.blockName);
            Debug.LogError("Found driver for event `OnMoveItemInsideBlock` => " + driver.GetNamespace());
            string function = driver.GetFunctionForEvent(DriverManager.DrivenEvents.OnMoveItemInsideBlock);
            string call = Driver.AttachParams(function, new string[] { 
                Driver.StringyParam(side), 
                slotFrom.ToString(), 
                count.ToString(),
                slotTo.ToString()
            });
            Debug.LogError("Try and call the following on turtle : `" + call + "`");
            Send(call);
        }
        else
        {
            Debug.LogError("Open Intent, block NOT FOUND");
        }

    }
    public void Take(string side, int slotFrom, int count, int slotTo)
    {
        //Send("take(\"" + side + "\"," + slotFrom + "," + count + "," + slotTo + ");");
        Debug.LogError("Intent to: Take " + side);
        bool found = false;
        GambitBlock b = null;
        Vector3Int blockPos;

        if (side == "front")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.forward + this.transform.position);
        }
        else if (side == "down")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position - this.transform.up);
        }
        else if (side == "up")
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position + this.transform.up);
        }
        else
        {
            blockPos = CoordinateConverter.UnityToMinecraft(this.transform.position);
        }
        found = MissionControl.Instance.TryGetBlock(blockPos, this.Dimension.name, out b);

        if (found)
        {
            Debug.LogError("Take Intent: block Found --" + b.blockName);

            Driver driver = DriverManager.Instance.FindDriverForEvent(DriverManager.DrivenEvents.OnMoveFromBlockToTurtle, b.blockName);
            Debug.LogError("Found driver for event `OnMoveFromBlockToTurtle` => " + driver.GetNamespace());
            string function = driver.GetFunctionForEvent(DriverManager.DrivenEvents.OnMoveFromBlockToTurtle);
            string call = Driver.AttachParams(function, new string[] {
                Driver.StringyParam(side),
                slotFrom.ToString(),
                count.ToString(),
                slotTo.ToString()
            });
            Debug.LogError("Try and call the following on turtle : `" + call + "`");
            Send(call);
        }
        else
        {
            Debug.LogError("Take Intent, block NOT FOUND");
        }
    }

    public void OpenVanillaFront()
    {
        Debug.LogError("Open Front @ " + CoordinateConverter.UnityToMinecraft(this.transform.forward + this.transform.position));
        GambitBlock b;
        bool found = MissionControl.Instance.TryGetBlock(CoordinateConverter.UnityToMinecraft(this.transform.forward+this.transform.position), this.Dimension.name, out b);
        
        if(found)
        {
            Debug.LogError("Open Front, block Found --" + b.blockName);

            Driver driver = DriverManager.Instance.FindDriverForEvent(DriverManager.DrivenEvents.OnGetBlockInventory, b.blockName);
            Debug.LogError("Found driver for event `OnGetBlockInventory` => " + driver.GetNamespace());
            string function = driver.GetFunctionForEvent(DriverManager.DrivenEvents.OnGetBlockInventory);
            string call = Driver.AttachParams(function, new string[] { "\"front\"" } );
            Debug.LogError("Try and call the following on turtle : `" + call + "`");
            Send(call);
        } else
        {
            Debug.LogError("Open Front, block NOT FOUND");
        }

    }

    public void OpenVanillaDown()
    {
        Debug.LogError("Open Down @ " + CoordinateConverter.UnityToMinecraft(this.transform.position-this.transform.forward));
        GambitBlock b;
        bool found = MissionControl.Instance.TryGetBlock(CoordinateConverter.UnityToMinecraft(this.transform.position-this.transform.up), this.Dimension.name, out b);

        if (found)
        {
            Debug.LogError("Open Front, block Found --" + b.blockName);

            Driver driver = DriverManager.Instance.FindDriverForEvent(DriverManager.DrivenEvents.OnGetBlockInventory, b.blockName);
            Debug.LogError("Found driver for event `OnGetBlockInventory` => " + driver.GetNamespace());
            string function = driver.GetFunctionForEvent(DriverManager.DrivenEvents.OnGetBlockInventory);
            string call = Driver.AttachParams(function, new string[] { "\"down\"" });
            Debug.LogError("Try and call the following on turtle : `" + call + "`");
            Send(call);
        }
        else
        {
            Debug.LogError("Open Front, block NOT FOUND");
        }
    }

    public void OpenVanillaUp()
    {
        Debug.LogError("Open Up @ " + CoordinateConverter.UnityToMinecraft(this.transform.up + this.transform.position));
        GambitBlock b;
        bool found = MissionControl.Instance.TryGetBlock(CoordinateConverter.UnityToMinecraft(this.transform.up + this.transform.position), this.Dimension.name, out b);

        if (found)
        {
            Debug.LogError("Open Front, block Found --" + b.blockName);

            Driver driver = DriverManager.Instance.FindDriverForEvent(DriverManager.DrivenEvents.OnGetBlockInventory, b.blockName);
            Debug.LogError("Found driver for event `OnGetBlockInventory` => " + driver.GetNamespace());
            string function = driver.GetFunctionForEvent(DriverManager.DrivenEvents.OnGetBlockInventory);
            string call = Driver.AttachParams(function, new string[] { "\"up\"" });
            Debug.LogError("Try and call the following on turtle : `" + call + "`");
            Send(call);
        }
        else
        {
            Debug.LogError("Open Front, block NOT FOUND");
        }
    }

    public void RefuelFromSlot()
    {
        Send("helpers.refuel()");
    }

    public void Reboot()
    {
        Send("helpers.reboot()");
    }

    public void EquipLeft()
    {
        Send("helpers.equipLeft()");
    }

    public void EquipRight()
    {
        Send("helpers.equipRight()");
    }
}

public class CoordinateConverter
{
    // Convert Minecraft coordinates to Unity coordinates
    public static Vector3Int MinecraftToUnity(Vector3 minecraftCoords)
    {
        return new Vector3Int(Mathf.CeilToInt(minecraftCoords.x), Mathf.CeilToInt(minecraftCoords.y), -Mathf.CeilToInt(minecraftCoords.z));
    }

    public static Vector3Int MinecraftToUnity(Vector3Int minecraftCoords)
    {
        return new Vector3Int(Mathf.CeilToInt(minecraftCoords.x), Mathf.CeilToInt(minecraftCoords.y), -Mathf.CeilToInt(minecraftCoords.z));
    }

    // Convert Unity coordinates to Minecraft coordinates
    public static Vector3Int UnityToMinecraft(Vector3 unityCoords)
    {
        return new Vector3Int(Mathf.CeilToInt(unityCoords.x), Mathf.CeilToInt(unityCoords.y), -Mathf.CeilToInt(unityCoords.z));
    }

    public static Vector3Int UnityToMinecraft(Vector3Int unityCoords)
    {
        return new Vector3Int(Mathf.CeilToInt(unityCoords.x), Mathf.CeilToInt(unityCoords.y), -Mathf.CeilToInt(unityCoords.z));
    }
}