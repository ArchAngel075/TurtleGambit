using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExternalInventoryUI : MonoBehaviour
{
    public GameObject ESlotPrefab;
    public GameObject Grid;

    public Dictionary<int, GambitSlot> Slots;
    public GambitBlock Block;

    public void UpdateUISlotDetails()
    {
        Debug.LogError("Update UI Slot Detals : ............/");
        Dictionary<int, GambitSlot> Slots = this.Block.inventory;
        if(Slots == null)
        {
            Debug.LogError("Tried preparing External Inventory UI for a block without an inventory " + this.Block.gameObject.name);
            return;
        }
        List<Transform> children = Grid.transform.GetComponentsInChildren<Transform>().ToList();
        foreach (Transform child in children)
        {
            if(child.GetComponent<Transform>() != null && child != Grid.GetComponent<Transform>())
            {
                Destroy(child.gameObject);
            }
        }
        
        this.Slots = Slots;

        //TurtleManagerUIMaster.Instance.selectedTurtle.PositionOffsetBySide()

        foreach (var slot in Slots)
        {
            slot.Value.owner = this.gameObject;
            GameObject slotsGo = Instantiate(ESlotPrefab, Grid.transform);
            slotsGo.name = "ESlot (" + slot.Key.ToString() + ")";
            //o.transform.SetParent(Grid.transform);
            string name = slot.Value.name;
            if (name == "GAMBIT:EMPTY")
            {
                name = "";
            }
            slotsGo.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = name;
            UIInventorySlot uislot = slotsGo.gameObject.GetComponent<UIInventorySlot>();
            uislot.slot = slot.Value;
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
        }
    }

    public void SetBlock(GambitBlock block, bool refreshUI = false)
    {
        this.Block = block;
        if(refreshUI)
        {
            UpdateUISlotDetails();
        }
    }

    public void OnBlockInventoryWasObserved(Dictionary<int, GambitSlot> slots) {
        Debug.LogError("HEY the block has its inventory observed, lets propagate this :");
        UpdateUISlotDetails();
    }

    public void OpenUI() 
    {
        if(this.gameObject.activeSelf)
        {
            return;
        }
        this.gameObject.SetActive(true);
        Debug.LogError("Bind to the event the block has its inventory observed...");
        this.Block.OnObserveInventory += OnBlockInventoryWasObserved;
    }

    public void CloseUI ()
    {
        if(this.gameObject.activeSelf)
        {
            this.Block.OnObserveInventory -= OnBlockInventoryWasObserved;
            this.gameObject.SetActive(false);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
