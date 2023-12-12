using UnityEngine;
using UnityEngine.UI;

public class UIInventorySlot : MonoBehaviour
{
    public GameObject image;
    public GambitSlot slot;
    public int index;

    public Sprite normal;
    public Sprite selected;

    public string ownerName;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(slot != null)
        {
            ownerName = slot.owner.ToString();
        }
    }

    public void SetSelected(bool state)
    {
        Sprite s;
        if (state)
        {
            s = selected;
        } else
        {
            s = normal;
        }
        this.GetComponent<Image>().sprite = s;
    }
}
