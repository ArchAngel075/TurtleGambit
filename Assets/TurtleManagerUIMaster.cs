using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

public class TurtleManagerUIMaster : MonoBehaviour
{
    public int chunkSize = 256;
    public int MaxRecordLength = 10;

    public bool isRecording = false;
    public AudioClip clip;
    float startRecordingTime;

    public GambitTurtle selectedTurtle;

    public GameObject offlineOverlay;
    public GameObject noTurtleOverlay;

    UIInventorySlot lastHovered;
    UIInventorySlot lastSelected;

    bool isDragging = false;
    bool isPressing = false;

    public GameObject DraggingItem;
    public UIInventorySlot sourceDragUISlot = null;

    Vector2 pointerPosition;
    Vector2 pointerPositionAtPress;

    public static TurtleManagerUIMaster Instance;
    public string speachCommandParamString;

    public void SetNoTurtlePanel(bool state)
    {
        noTurtleOverlay.SetActive(state);
    }

    public void SetTurtleOfflineOverlay(bool state)
    {
        offlineOverlay.SetActive(state);
    }

    public static UIInventorySlot GetSlotUnderMouse()
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Input.mousePosition;

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        UIInventorySlot slot = null;
        foreach (var result in results)
        {
            UIInventorySlot find = result.gameObject.GetComponent<UIInventorySlot>();
            if (find != null)
            {
                slot = find;
                break;
            }
        }
        return slot;
    }

    public static GameObject GetOwnerOfSlot(UIInventorySlot slot)
    {
        return slot.slot.owner;
    }

    public void OnPointerMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        pointerPosition = context.ReadValue<Vector2>();
        UIInventorySlot slotUnderMouse = GetSlotUnderMouse();
        if(isPressing && !isDragging && slotUnderMouse!= null)
        {
            if((pointerPositionAtPress - pointerPosition).magnitude > 0.1f)
            {
                sourceDragUISlot = slotUnderMouse;
                isDragging = true;
                DraggingItem.SetActive(true);
                DraggingItem.GetComponent<Image>().sprite = sourceDragUISlot.slot.GetSprite();
                DraggingItem.GetComponent<Image>().color = Color.white * 0.63f;
            }
        }
        if(isDragging)
        {
            //move the drag object :
            DraggingItem.GetComponent<RectTransform>().position = pointerPosition;
            DraggingItem.GetComponent<RectTransform>().localScale = Vector3.one*0.25f;
        }
    }

    public void OnPointerClick(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Debug.Log("CLICK!");
        isPressing = context.ReadValue<float>() == 1f;
        if(isPressing)
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            pointerPositionAtPress = new Vector2(pointerPosition.x, pointerPosition.y);

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            UIInventorySlot slot = null;
            foreach (var result in results)
            {
                UIInventorySlot find = result.gameObject.GetComponent<UIInventorySlot>();
                if (find != null)
                {
                    slot = find;
                }
            }

            if (slot != null && slot.slot != null)
            {
                //only send selection if the slot involved turtle :
                if(selectedTurtle != null && slot.slot.owner == selectedTurtle.gameObject)
                {
                    selectedTurtle.SelectSlot(slot.index);
                }
                //in another case we can rather do a intentional refresh contents of specific slot. when i rewrite the open observe logic...
            }
        }

        if(!isPressing && isDragging)
        {
            UIInventorySlot currentlyHoveredSlot = GetSlotUnderMouse();
            isDragging = false;
            DraggingItem.SetActive(false);
            DraggingItem.GetComponent<Image>().sprite = null;
            DraggingItem.GetComponent<Image>().color = Color.clear;
            if(currentlyHoveredSlot != null)
            {
                //ask turtle to move item from slot (selected) to slot (lastHovered)
                int from = sourceDragUISlot.index;
                int to = currentlyHoveredSlot.index;
                if(selectedTurtle != null)
                {
                    /* Cases :
                        * 1. turtle to turtle  transferFromTo
                        * 2. turtle to inv     put
                        * 3. inv to turtle     take
                        * 4. inv to inv        transferItemExternal
                    */

                    Debug.LogError("sourceDragUISlot slot owner :" + sourceDragUISlot.slot.owner.gameObject.ToString() + " [" + from + "]");
                    Debug.LogError("currentlyHoveredSlot slot owner : " + currentlyHoveredSlot.slot.owner.gameObject.ToString() + " [" + to + "]");

                    bool fromTurtle = sourceDragUISlot.slot.owner.gameObject == selectedTurtle.gameObject;
                    bool toTurtle = currentlyHoveredSlot.slot.owner.gameObject == selectedTurtle.gameObject;

                    int countMoved = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? sourceDragUISlot.slot.count : 1;

                    if (fromTurtle && toTurtle)
                    {
                        Debug.LogError("from and to turtle");
                        selectedTurtle.TransferFromTo(from, to, countMoved);
                    } 
                    else if(fromTurtle && !toTurtle)
                    {
                        Debug.LogError("from trutle , to inventory, side, index, count, index: " + string.Join(',',new string[] { sourceDragUISlot.slot.side, sourceDragUISlot.index.ToString(), countMoved.ToString(), currentlyHoveredSlot.index.ToString() }));
                        selectedTurtle.Put(currentlyHoveredSlot.slot.side, sourceDragUISlot.index, countMoved, currentlyHoveredSlot.index);
                    } else if(!fromTurtle && !toTurtle)
                    {
                        Debug.LogError("from inventory , to inventory");
                        selectedTurtle.moveExternal(sourceDragUISlot.slot.side, sourceDragUISlot.index, countMoved, currentlyHoveredSlot.index);
                    } else if(!fromTurtle && toTurtle)
                    {
                        Debug.LogError("from inventory , to turtle");
                        selectedTurtle.Take(sourceDragUISlot.slot.side, sourceDragUISlot.index, countMoved, currentlyHoveredSlot.index);
                    }
                }
            } else
            {
                DraggingItem.SetActive(false);
                DraggingItem.GetComponent<Image>().sprite = null;
                DraggingItem.GetComponent<Image>().color = Color.clear;
                isDragging = false;
            }
        }
    }

    public void CameraMoveToTurtle()
    {
        if (selectedTurtle != null)
        {
            Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            cam.transform.position = selectedTurtle.transform.position - (selectedTurtle.transform.forward * 6) + (Vector3.up * 3);
            cam.transform.LookAt(selectedTurtle.transform.position);
        } else
        {
            Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
            cam.transform.position = Vector3.zero;
            cam.transform.rotation = Quaternion.identity;
        }
    }

    public void OnTurtleSelected()
    {
        TMPro.TMP_Dropdown drop = GameObject.Find("TurtleSelection").transform.GetComponentInChildren<TMPro.TMP_Dropdown>();
        string name = drop.options[drop.value].text;
        if (selectedTurtle != null)
        {
            selectedTurtle.OnSlotsChanged -= RefreshSlots;
            selectedTurtle.OnSelectedSlotChanged -= UpdateSelectedSlot;
        }
        if (name == "None")
        {
            SetTurtleOfflineOverlay(false);
            SetNoTurtlePanel(true);
            return;
        }
        
        GambitTurtle turtle = GambitTurtle.FindExact(name);
        SetNoTurtlePanel(false);
        selectedTurtle = turtle;

        selectedTurtle.OnSlotsChanged += RefreshSlots;
        selectedTurtle.OnSelectedSlotChanged += UpdateSelectedSlot;
        RefreshTurtleDetails();
        CameraMoveToTurtle();

        if (selectedTurtle.isStale()) 
        {
            //SetTurtleOfflineOverlay(true);
        } else
        {
            SetTurtleOfflineOverlay(false);
        }

        //Debug.LogError("selectedTurtle.Dimension.name " + selectedTurtle.Dimension.name);

        TMPro.TMP_Dropdown dpd = UIReference.instance.Dimension.GetComponentInChildren<TMPro.TMP_Dropdown>();
        int index = dpd.options.FindIndex(e => { return e.text == selectedTurtle.Dimension.name; });
        //Debug.LogError("selectedTurtle.Dimension index in options " + index);
        dpd.SetValueWithoutNotify(index);

        TMPro.TMP_Dropdown dpRot = UIReference.instance.Rotation.GetComponentInChildren<TMPro.TMP_Dropdown>();
        int indexRot = dpRot.options.FindIndex(e => { return CardinalDirectionUtility.FromString(e.text) == selectedTurtle.Direction; });
        //Debug.LogError("selectedTurtle.Rotation index in options " + indexRot);
        dpRot.SetValueWithoutNotify(indexRot);
    }

    public void SetTurtleDimension()
    {
        TMP_Dropdown dp = UIReference.instance.Dimension.GetComponentInChildren<TMPro.TMP_Dropdown>();
        int selected = dp.value;
        Debug.LogError("selectedTurtle.Dimension index change to " + selected);
        if (selected != 0) 
        {
            this.selectedTurtle.SetDimension(dp.options[selected].text);
        }
    }
    public void SetTurtleRotation()
    {
        TMP_Dropdown dp = UIReference.instance.Rotation.GetComponentInChildren<TMPro.TMP_Dropdown>();
        int selected = dp.value;
        Debug.LogError("selectedTurtle.Dimension index change to " + selected);
        this.selectedTurtle.Direction = CardinalDirectionUtility.FromString(dp.options[selected].text);
    }


    public void OnTurtleCreated(GambitTurtle turtle)
    {
        TMPro.TMP_Dropdown drop = GameObject.Find("TurtleSelection").transform.GetComponentInChildren<TMPro.TMP_Dropdown>();
        drop.options.Add(new TMPro.TMP_Dropdown.OptionData(turtle.name));
    }

    private void UpdateSelectedSlot(int index)
    {
        if(lastSelected != null && lastSelected.slot.owner == selectedTurtle.gameObject)
        {
            lastSelected.SetSelected(false);
        }
        GameObject slotsGo = GameObject.Find("Slot (" + (index-1) + ")");
        slotsGo.GetComponent<UIInventorySlot>().SetSelected(true);
        lastSelected = slotsGo.GetComponent<UIInventorySlot>();
    }

    public void ScaryForceRedownload()
    {
        if( selectedTurtle != null )
        {
            selectedTurtle.Send("fs.delete('brain.lua'); os.reboot();");
        }
    }


    private void RefreshSlots(Dictionary<int, GambitSlot> slots)
    {
        foreach (var slot in slots)
        {
            GameObject slotsGo = GameObject.Find("Slot (" + (slot.Key-1) + ")");
            string name = slot.Value.name;
            if(name == "GAMBIT:EMPTY")
            {
                name = "";
            }
            slotsGo.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = name;
            UIInventorySlot uislot = slotsGo.gameObject.GetComponent<UIInventorySlot>();
            uislot.slot = slot.Value;
            slot.Value.owner = this.selectedTurtle.gameObject;
            uislot.index = slot.Key;
            Image imgComponent = uislot.image.GetComponent<Image>();
            imgComponent.sprite = slot.Value.GetSprite();
            if (imgComponent.sprite != null)
            {
                imgComponent.color = Color.white;
            }
            else
            {
                imgComponent.color = Color.clear;
            }
            //slotsGo.gameObject.GetComponentInChildren<TMPro.>().text = slot.Value.name;
            //GetTexture
        }
    }

    public void RefreshTurtleDetails()
    {
        selectedTurtle.FetchInventory();
        selectedTurtle.FetchWorldPosition();
        selectedTurtle.FetchSelectedSlotIndex();
        DriverManager.Instance.PushDriversToTurtle(selectedTurtle);
    }

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;
        MissionControl.Instance.OnTurtleCreated += OnTurtleCreated;
    }

    // Update is called once per frame
    void Update()
    {
        if(isRecording)
        {
            float t = Time.time;
            if(t-startRecordingTime > 10)
            {
                //OnFinishRecording();
            }
        }
        if(selectedTurtle)
        {
            if(selectedTurtle.isStale())
            {
                //SetTurtleOfflineOverlay(true);
            } else
            {
                SetTurtleOfflineOverlay(false);
            }
            GameObject.Find("TurtleBatteryDisplayValue").GetComponent<TextMeshProUGUI>().text = selectedTurtle.FuelLevel.ToString();

            TMPro.TMP_Dropdown dpd = UIReference.instance.Dimension.GetComponentInChildren<TMPro.TMP_Dropdown>();
            int index = dpd.options.FindIndex(e => { return e.text == selectedTurtle.Dimension.name; });
            dpd.SetValueWithoutNotify(index);

            TMPro.TMP_Dropdown dpRot = UIReference.instance.Rotation.GetComponentInChildren<TMPro.TMP_Dropdown>();
            dpRot.SetValueWithoutNotify((int)selectedTurtle.Direction);
        }


        if(EventSystem.current.IsPointerOverGameObject())
        {
            PointerEventData eventData = new PointerEventData(EventSystem.current);
            eventData.position = Input.mousePosition;
            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(eventData, results);
            UIInventorySlot slot = null;
            foreach (var result in results)
            {
                UIInventorySlot find = result.gameObject.GetComponent<UIInventorySlot>();
                if(find != null)
                {
                    slot = find;
                }
            }

            if (slot != null)
            {
                if (lastHovered != null)
                {
                    lastHovered.GetComponent<Image>().color = Color.white;
                }
                slot.GetComponent<Image>().color = Color.white * 0.5f;
                lastHovered = slot;
            } else
            {
                if (lastHovered != null)
                {
                    lastHovered.GetComponent<Image>().color = Color.white;
                    lastHovered = null;
                }
            }
            //Debug.LogWarningFormat("results:{0}", results.Count);
        } 

        if (selectedTurtle)
        {
            if (lastHovered != null)
            {
                if (lastHovered.slot != null)
                {
                    if (lastHovered.slot.name == "GAMBIT:EMPTY")
                    {
                        GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = "";
                    }
                    else
                    {
                        GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = lastHovered.slot.name + "\tx" + lastHovered.slot.count;
                    }
                }
                else
                {
                    GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = "...loading...";
                }
            }
            else
            {
                if(! EventSystem.current.IsPointerOverGameObject())
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    RaycastHit hit = Physics.RaycastAll(ray, 100).ToList().FirstOrDefault(e => { return e.transform.gameObject.GetComponent<GambitBlock>() != null; });
                    if (!EqualityComparer<RaycastHit>.Default.Equals(hit, default(RaycastHit)))
                    {
                        GambitBlock block = hit.transform.gameObject.GetComponent<GambitBlock>();
                        GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = block.blockName;
                    }
                    else
                    {
                        GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = "";
                    }
                }
                else
                {
                    GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = "";
                }
            }
        }
        else
        {
            GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = "";
        }
    }

    public void MoveForward()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.MoveForward();
        }
    }

    public void MoveBackward()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.MoveBackward();
        }
    }

    public void MoveUp()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.MoveUp();
        }
    }

    public void MoveDown()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.MoveDown();
        }
    }

    public void TurnLeft()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.TurnLeft();
        }
    }
    public void TurnRight()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.TurnRight();
        }
    }

    //observe
    public void ObserveFront()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.ObserveFront();
        }
    }

    public void ObserveDown()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.ObserveDown();
        }
    }

    public void ObserveUp()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.ObserveUp();
        }
    }

    //place
    public void PlaceFront()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.PlaceFront();
        }
    }

    public void PlaceDown()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.PlaceDown();
        }
    }

    public void PlaceUp()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.PlaceUp();
        }
    }

    //dig
    public void DigFront()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.DigFront();
        }
    }

    public void DigDown()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.DigDown();
        }
    }

    public void DigUp()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.DigUp();
        }
    }

    //attack
    public void AttackFront()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.AttackFront();
        }
    }

    public void AttackDown()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.AttackDown();
        }
    }

    public void AttackUp()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.AttackUp();
        }
    }

    //drop
    public void DropFront()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.DropFront();
        }
    }

    public void DropDown()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.DropDown();
        }
    }

    public void DropUp()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.DropUp();
        }
    }

    public void Craft()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.Craft();
        }
    }

    public void HookOpenInventoryOnObservation()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.OnObserveExternalInventory += OnObservationOpenInventoryAction;
        }
    }

    private void OnObservationOpenInventoryAction(GambitBlock block, string side)
    {
        selectedTurtle.OnObserveExternalInventory -= OnObservationOpenInventoryAction;
        //MissionControl.Instance.GetBlock(selectedTurtle.PositionOffsetBySide(side), selectedTurtle.Dimension);
        UIReference.instance.ExternalInventory.GetComponent<ExternalInventoryUI>().SetBlock(block);
        UIReference.instance.ExternalInventory.GetComponent<ExternalInventoryUI>().UpdateUISlotDetails();
        UIReference.instance.ExternalInventory.GetComponent<ExternalInventoryUI>().OpenUI();
    }

    public bool OpenBlock(GambitBlock block)
    {
        if (selectedTurtle != null)
        {
            MissionControl mc = MissionControl.Instance;
            Vector3Int frontPos = selectedTurtle.PositionOffsetBySide("front");
            Vector3Int downPos = selectedTurtle.PositionOffsetBySide("down");
            Vector3Int upPos = selectedTurtle.PositionOffsetBySide("up");

            Dictionary<Action, GambitBlock> blocks = new Dictionary<Action,GambitBlock>();
            if(mc.FindBlock(frontPos, selectedTurtle))
            {
                blocks.Add(OpenVanillaFront, mc.GetBlock(frontPos, selectedTurtle));
            }
            if (mc.FindBlock(downPos, selectedTurtle))
            {
                blocks.Add(OpenVanillaDown, mc.GetBlock(downPos, selectedTurtle));
            }
            if (mc.FindBlock(upPos, selectedTurtle))
            {
                blocks.Add(OpenVanillaUp, mc.GetBlock(upPos, selectedTurtle));
            }
            //now confirm which block if any, the first, that block we want to open ->
            Action? found = null;
            foreach (var item in blocks)
            {
                if(item.Value == block)
                {
                    found = item.Key; break;
                }
            }
            if (found != null)
            {
                found.Invoke();
            } else
            {
                Debug.LogWarning("Unable to open block as it is not adjacent to the turtle on a valid interactable side " + block.name);
            }
        }
        return false;
    }

    public void OnSetPositionX(string xAsString)
    {
        Vector3Int current = CoordinateConverter.UnityToMinecraft(selectedTurtle.transform.position);
        selectedTurtle.SetLocation(Mathf.FloorToInt(int.Parse(xAsString)), current.y, current.z);
        CameraMoveToTurtle();
    }

    public void OnSetPositionY(string yAsString)
    {
        Vector3Int current = CoordinateConverter.UnityToMinecraft(selectedTurtle.transform.position);
        selectedTurtle.SetLocation(current.x, Mathf.FloorToInt(int.Parse(yAsString)), current.z);
        CameraMoveToTurtle();
    }

    public void OnSetPositionZ(string zAsString)
    {
        Vector3Int current = CoordinateConverter.UnityToMinecraft(selectedTurtle.transform.position);
        selectedTurtle.SetLocation(current.x, current.y, Mathf.FloorToInt(int.Parse(zAsString)));
        CameraMoveToTurtle();
    }

    public void OpenVanillaFront()
    {
        if (selectedTurtle != null)
        {
            HookOpenInventoryOnObservation();
            selectedTurtle.OpenVanillaFront();
        }
    }

    public void OpenVanillaDown()
    {
        if (selectedTurtle != null)
        {
            HookOpenInventoryOnObservation();
            selectedTurtle.OpenVanillaDown();
        }
    }

    public void OpenVanillaUp()
    {
        if (selectedTurtle != null)
        {
            HookOpenInventoryOnObservation();
            selectedTurtle.OpenVanillaUp();
        }
    }

    public void ConsumeSelectedSlotAsFuel()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.RefuelFromSlot();
        }
    }

    public void EquipLeft()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.EquipLeft();
        }
    }

    public void EquipRight()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.EquipRight();
        }
    }

    public void rebootTurtle()
    {
        if (selectedTurtle != null)
        {
            selectedTurtle.Reboot();
        }
    }

    public void DoEvaluate()
    {
        if (selectedTurtle != null)
        {
            string command = UIReference.instance.EvaluateInput.GetComponentInChildren<TMP_InputField>().text;
            selectedTurtle.Send(command);
            UIReference.instance.EvaluateInput.GetComponentInChildren<TMP_InputField>().text = "";
        }
    }

    public void OnPushToTalkChanged(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        Debug.LogError("PTT?");
        bool isPushToTalk = context.ReadValue<float>() == 1f;
        if (isPushToTalk)
        {
            OnBeginRecording();
        } else
        {
            OnFinishRecording();
        }
    }

    public void OnBeginRecording()
    {
        Debug.LogError("PUSH TO TALK TRUE");
        MicrophoneSend();
    }

    public void MicrophoneSend()
    {
        //OnFinishRecording();

        string device = Microphone.devices[0];
        Microphone.GetDeviceCaps(device, out int minFreq, out int maxFreq);
        Debug.LogError("Speach Start! @ " + device + " capabilities of " + minFreq + " to " + maxFreq);
        clip = Microphone.Start(device, false, MaxRecordLength, 48000 ); //6 minutes for now?
        startRecordingTime = Time.time;
        isRecording = true;
    }

    public void TrimAudioFile(string fileName, int t)
    {
        string command = "-y -ss 0 -t {t} -i {fileName} _{fileName}".Replace("{fileName}",fileName).Replace("{t}",t.ToString());
        Debug.LogError("trim using command `" + command + "`");
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        startInfo.FileName = Path.Combine(Application.persistentDataPath, "ffmpeg.exe");
        startInfo.Arguments = command;
        startInfo.WorkingDirectory = Application.persistentDataPath;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;

        process.StartInfo = startInfo;
        process.Start();
        process.WaitForExit(1000 * (1 + MaxRecordLength));
    }

    public void OnFinishRecording()
    {
        Debug.LogError("PUSH TO TALK FALSE");

        isRecording = false;
        int recordingLength = Mathf.CeilToInt((Time.time - startRecordingTime)) + 1;
        Debug.LogError("Speach Done!" + "\tTook " + recordingLength.ToString());
        //TimeSpan t = TimeSpan.FromSeconds(recordingLength);

        string device = Microphone.devices[0];
        Microphone.End(device);

        //time of recording is based on the T slice of time

        string baseFileName = "recording";
        Debug.LogError("Using file name " + baseFileName);
        string recordingFileName = baseFileName+".wav";
        string dfpwmfileOnDisk = baseFileName+".dfpwm";

        //-y - i _recording.wav - ac 1 - c:a dfpwm recording.dfpwm -ar 48k
        //-y -i _recording.wav -ac 1 -c:a dfpwm recording.dfpwm -ar 48k
        string command = speachCommandParamString;

        SavWav.Save(recordingFileName, clip);
        //saved as recording.wav ->
        Debug.LogError("Speach Stored!");
        Debug.LogError("trim audio file :");
        TrimAudioFile(recordingFileName, recordingLength);
        Debug.LogError("audio file trimmed,");
        recordingFileName = "_" + recordingFileName; //set as output of trimmings file name

        Debug.LogError("Conversion command deduced as `" + command + "`");

        //convert file,
        System.Diagnostics.Process process = new System.Diagnostics.Process();
        System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
        startInfo.FileName = Path.Combine(Application.persistentDataPath, "ffmpeg.exe") ;
        startInfo.Arguments = command;
        startInfo.WorkingDirectory = Application.persistentDataPath;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardError = true;
        startInfo.RedirectStandardOutput = true;

        Debug.LogError("Working from " + startInfo);
        Debug.LogError("Working dir " + Application.persistentDataPath);


        process.StartInfo = startInfo;
        process.Start();
        //will create file recording.dfpwm

        // start our event pumps
        //process.BeginOutputReadLine();
        //process.BeginErrorReadLine();

        // until we are done
        process.WaitForExit(1000 * (1+MaxRecordLength));
        Debug.LogError("Speach Converted! Will now send base64 bytes of " + dfpwmfileOnDisk);
        //grab bytes of the dfpwm file
        byte[] bytes = File.ReadAllBytes( Path.Combine(Application.persistentDataPath, dfpwmfileOnDisk) );
        string commands;
        
        pushFile(dfpwmfileOnDisk, bytes, out commands);

        commands += "helpers.playFile('" + dfpwmfileOnDisk + "');";
        commands += "fs.delete('" + dfpwmfileOnDisk + "');";

        selectedTurtle.Send(commands);
    }

    public void pushFile(string fileName, byte[] content, out string commands)
    {
        //input file content as bytes
        //convert bytes to base64
        string base64String = Convert.ToBase64String(content);
        //push to turtle
        List<string> chunks = new List<string>();
        int cSize = chunkSize;

        for (int i = 0; i < base64String.Length; i += cSize)
        {
            int remainingLength = Math.Min(cSize, base64String.Length - i);
            string chunk = base64String.Substring(i, remainingLength);
            chunks.Add(chunk);
        }
        commands = "";
        //this approach is used over multiparting, seems multipart files have something wrong with excessively large data?
        bool firstItem = true;
        foreach (var chunk in chunks)
        {
            if (firstItem)
            {
                commands += "helpers.proxyOnfile('" + fileName + "','w','write','" + chunk + "');";
            }
            else
            {
                commands += "helpers.proxyOnfile('" + fileName + "','a','write','" + chunk + "');";
            }
            firstItem = false;
        }
        //decode.
        commands += "helpers.decodeFile('" + fileName + "');";
    }

}
