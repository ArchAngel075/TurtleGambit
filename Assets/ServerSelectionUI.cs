using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;

public class ServerSelectionUI : MonoBehaviour
{
    public GameObject serverSelect;
    public GameObject ServerInput;
    public GameObject GameContainer;
    public GameObject MainMenuContainer;

    public WebsocketServer WebsocketServer;
    // Start is called before the first frame update
    void Start()
    {
        TMP_Dropdown serverSelectDropDown = serverSelect.GetComponent<TMP_Dropdown>();
        serverSelectDropDown.ClearOptions();
        string ServerSavePath = Path.Combine(Application.persistentDataPath, "Save");
        List<string> options = new List<string>();
        foreach (var dir in Directory.GetDirectories(ServerSavePath).ToList())
        {
            options.Add(Path.GetFileName(dir));
        }
        serverSelectDropDown.AddOptions(new List<TMP_Dropdown.OptionData>() { new TMP_Dropdown.OptionData("Select Server") });
        serverSelectDropDown.AddOptions(options);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSelectServer()
    {
        TMP_Dropdown serverSelectDropDown = serverSelect.GetComponent<TMP_Dropdown>();
        string input = serverSelectDropDown.options[serverSelectDropDown.value].text;

        MainMenuContainer.SetActive(false);
        WebsocketServer.serverName = input;
        GameContainer.SetActive(true);
    }

    public void OnMakeServer()
    {
        string input = ServerInput.GetComponent<TMP_InputField>().text;
        if(input.Length > 3)
        {
            string ServerSavePath = Path.Combine(Application.persistentDataPath, "Save",input);
            Directory.CreateDirectory(ServerSavePath);
            MainMenuContainer.SetActive(false);
            WebsocketServer.serverName = input;
            GameContainer.SetActive(true);
        }
    }
}
