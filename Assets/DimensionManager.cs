using System.Linq;
using UnityEngine;

public class DimensionManager : MonoBehaviour
{
    public static DimensionManager Instance;
    public GameObject DimensionPrefab;
    // Start is called before the first frame update
    void Awake()
    {
        Instance = this;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public static GambitDimension MakeDimension(string name)
    {
        GameObject go = Instantiate(DimensionManager.Instance.DimensionPrefab,Instance.transform);
        go.name = name;

        //add to list of options for turtle ui :
        TMPro.TMP_Dropdown dp = UIReference.instance.Dimension.GetComponentInChildren<TMPro.TMP_Dropdown>();
        dp.options.Add(new TMPro.TMP_Dropdown.OptionData(name));
        if(TurtleManagerUIMaster.Instance.selectedTurtle != null)
        {
            dp.value = dp.options.FindIndex(e => { return e.text == TurtleManagerUIMaster.Instance.selectedTurtle.Dimension.name; });
        }
        return go.GetComponent<GambitDimension>();
    }

    public static GambitDimension Find(string name)
    {
        GambitDimension dim = Instance.GetComponentsInChildren<GambitDimension>(true).FirstOrDefault(e => { return e.name == name; }); ;
        if(dim == null)
        {
            return MakeDimension(name);
        } else
        {
            return dim;
        }
    }
}
