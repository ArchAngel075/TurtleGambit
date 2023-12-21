using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using static UnityEngine.InputSystem.InputAction;

public class UIReference : MonoBehaviour
{

    public  static UIReference instance;

    public GameObject PositionX;
    public GameObject PositionY;
    public GameObject PositionZ;

    public GameObject Rotation;
    public GameObject Dimension;

    public GameObject ExternalInventory;

    public GameObject EvaluateInput;
    public GameObject DoEvaluate;

    public GameObject BlockContextMenu;
    public GameObject BlockContextMenuPanel;



    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        if (TurtleManagerUIMaster.Instance.selectedTurtle == null)
        {
            return;
        }
        GambitTurtle turtle = TurtleManagerUIMaster.Instance.selectedTurtle;



        

    }

    public void OnClick(CallbackContext cc)
    {
        if (!EventSystem.current.IsPointerOverGameObject() && !BlockContextMenu.activeInHierarchy)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit = Physics.RaycastAll(ray, 100).ToList().FirstOrDefault(e => { return e.transform.gameObject.GetComponent<GambitBlock>() != null; });
            if (!EqualityComparer<RaycastHit>.Default.Equals(hit, default(RaycastHit)))
            {
                GambitBlock block = hit.transform.gameObject.GetComponent<GambitBlock>();
                //BlockContextMenu.SetActive(true);
                //WIP - im not feeling like workign on this feature right now..
            }
        }
    }
}
