using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TurtleManagerUIMaster : MonoBehaviour
{
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
                        Debug.LogError("from trutle , to inventory");
                        selectedTurtle.Put(sourceDragUISlot.slot.side, sourceDragUISlot.index, countMoved, currentlyHoveredSlot.index);
                    } else if(!fromTurtle && !toTurtle)
                    {
                        Debug.LogError("from inventory , to inventory");
                        selectedTurtle.moveExternal(sourceDragUISlot.slot.side, sourceDragUISlot.index, countMoved, currentlyHoveredSlot.index);
                    } else if(!fromTurtle && toTurtle)
                    {
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
        Camera cam = GameObject.Find("Main Camera").GetComponent<Camera>();
        cam.transform.position = selectedTurtle.transform.position - (selectedTurtle.transform.forward * 6) + (Vector3.up*3);
        cam.transform.LookAt(selectedTurtle.transform.position);

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
        dpRot.SetValueWithoutNotify(index);
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
        this.selectedTurtle.Direction = (CardinalDirectionEnum)selected;
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
                GameObject.Find("ItemHoverBar").GetComponentInChildren<TextMeshProUGUI>().text = "";
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

}
