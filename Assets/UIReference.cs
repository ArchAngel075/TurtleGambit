using UnityEngine;

public class UIReference : MonoBehaviour
{

    public  static UIReference instance;

    public GameObject PositionX;
    public GameObject PositionY;
    public GameObject PositionZ;

    public GameObject Rotation;
    public GameObject Dimension;

    public GameObject ExternalInventory;



    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if(TurtleManagerUIMaster.Instance.selectedTurtle == null)
        {
            return;
        }
        GambitTurtle turtle = TurtleManagerUIMaster.Instance.selectedTurtle;
        
    }
}
