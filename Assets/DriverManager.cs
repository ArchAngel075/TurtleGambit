using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using static DriverManager;

/// <summary>
/// Driver manager acts as a container and utility for those interactions between the app and various in game mods.
/// 
/// all files in app/Drivers will be loaded.
/// a driver.json is required.
/// it must specify this schema :
/// ---
/// id : the string mod identity that acts as a namespace in the lua environment aswell.
/// bindings : a key value list of those events and those methods the driver can bind to.
///     methods are lua side called using driver.{mod.id}.{method}
///     keys are the exact match string name event fired inside the app.
///     
///     example :
///         OnGetBlockInventory : observeExternalInventory
///         OnMoveFromTurtleToBlock : transferFromTo
/// blocks : a list of regex string matchings that the driver will want to take the wheel on
///     example :
///         blocks : { "^(minecraft:)" }
/// ---
/// 
/// </summary>
public class DriverManager : MonoBehaviour
{
    public static DriverManager Instance;
    List<Driver> Drivers = new List<Driver>();

    /// <summary>
    /// Dumbly pushes the drivers registered to the given turtle. Will allways override current drivers and will allways push all drivers.
    /// TODO : Use MD5 to match what a turtle has to what we have and update discrepencies.
    /// </summary>
    /// <param name="turtle"></param>
    public void PushDriversToTurtle(GambitTurtle turtle)
    {
        string turtleSideDriversRootPath = "Drivers";
        TurtleManagerUIMaster UIMaster = TurtleManagerUIMaster.Instance;
        foreach (Driver driver in Drivers)
        {
            byte[] content = File.ReadAllBytes(driver.PathToDriverLua);
            string pathToDriverInTurtle = turtleSideDriversRootPath + "/" + driver.GetNamespace() + "/driver.lua";
            string command = "";
            UIMaster.pushFile(pathToDriverInTurtle, content, out command);
            //append a driver rebuild so the turtle is aware it received drivers.
            command += "; helpers.RebuildDrivers()";
            Debug.LogError("Push driver [" + driver.GetNamespace() + "] to location [" + pathToDriverInTurtle + "] using local [" + driver.PathToDriverLua +"]");
            turtle.Send(command);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        DriverManager.Instance = this;

        string path = Path.Combine(Application.persistentDataPath, "Drivers");
        Debug.LogError("Search for drivers in path [" + path + "]");
        //load those drivers :
        foreach(var driverJson in Directory.EnumerateFiles(path, "*.json",SearchOption.AllDirectories))
        {
            if(Path.GetFileNameWithoutExtension(driverJson) == "driver")
            {
                Debug.LogError("[DriverManager] found driver [" + driverJson + "]");
                string jsonContent = File.ReadAllText(driverJson);
                string pathToDriverDir = Path.GetDirectoryName(driverJson);
                string pathToDriverLogic = Path.Combine(pathToDriverDir, "driver.lua");
                Debug.LogError("[DriverManager] register logic @ [" + pathToDriverLogic + "]");
                Drivers.Add(new Driver(jsonContent, pathToDriverLogic));
            }
        }
    }

    public Driver FindDriverForEvent(DriverManager.DrivenEvents drivenEvent, string blockName)
    {
        string eventName = Enum.GetName(typeof(DrivenEvents), drivenEvent);
        return FindDriverForEvent(eventName, blockName);
    }

    public Driver FindDriverForEvent(string eventName, string blockName)
    {
        foreach (var driver in Drivers)
        {
            if(driver.CanDriveBlock(blockName) && driver.CanDriveEvent(eventName))
            {
                return driver;
            }
        }
        return null;
    }

    public static TEnum GetEnumValue<TEnum>(string input) where TEnum : struct, Enum
    {
        if (Enum.TryParse<TEnum>(input, out TEnum result))
        {
            return result;
        }
        else
        {
            return default(TEnum); // Return default value for the enum if no match is found
        }
    }

    public enum DrivenEvents
    {
        NONE,
        OnGetBlockInventory,
        OnMoveItemInsideBlock,
        OnMoveFromBlockToTurtle,

        OnMoveFromTurtleToBlock
    }
}

public class Driver
{
    Dictionary<string,string> EventToFunctionBinding = new Dictionary<string,string>();
    List<string> BlockMatching = new List<string>();
    string ID;
    public string PathToDriverLua;

    public string GetNamespace()
    {
        return ID;
    }

    public bool CanDriveEvent(string eventName) {
        return EventToFunctionBinding.ContainsKey(eventName);
    }

    public bool CanDriveBlock(string blockName)
    {
        foreach (string pattern in BlockMatching)
        {
            if(Regex.Matches(blockName, pattern).Count > 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// returns the binded lua function (prepended by namespace) for the given event
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public string GetFunctionForEvent(string eventName)
    {
        string function;
        if (!EventToFunctionBinding.TryGetValue(eventName, out function))
        {
            throw new System.Exception("Unable to drive event " + eventName);
        }
        return "Drivers." + GetNamespace() + "." + function;
    }

    /// <summary>
    /// returns the binded lua function (prepended by namespace) for the given event
    /// </summary>
    /// <param name="eventName"></param>
    /// <returns></returns>
    /// <exception cref="System.Exception"></exception>
    public string GetFunctionForEvent(DriverManager.DrivenEvents drivenEvent)
    {
        string eventName = Enum.GetName(typeof(DrivenEvents), drivenEvent);
        return GetFunctionForEvent(eventName);
    }

    /// <summary>
    /// Attach to the given function call those params (assumed with string or type safe encoding)
    /// </summary>
    /// <param name="functionCall">the namespace and function call</param>
    /// <param name="args">those params to attach, in order.</param>
    /// <returns></returns>
    public static string AttachParams(string functionCall,  params string[] args)
    {
        string final = functionCall + "(";
        bool first = true;
        foreach (var arg in args)
        {
            if (!first)
            {
                final += ",";
            }
            final += arg;
            first = false;
        }
        final += ");";
        return final;
    }

    public static string StringyParam(string param)
    {
        return "\"" + param.ToString() + "\"";
    }

    public Driver(string json, string pathToDriverLua)
    {
        dynamic definitions = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
        ID = definitions.id;
        JObject bindings = definitions.bindings;
        JArray patterns = definitions.blocks;

        foreach (var item in bindings)
        {
            Debug.LogError("found binding [" + item.Key + "] => " + item.Value);
            EventToFunctionBinding.Add(item.Key, item.Value.ToString());
        }

        foreach (var item in patterns)
        {
            Debug.LogError("found pattern [" + item + "] => `" + item + "`");
            BlockMatching.Add(item.ToString());
        }

        PathToDriverLua = pathToDriverLua;
    }
}
