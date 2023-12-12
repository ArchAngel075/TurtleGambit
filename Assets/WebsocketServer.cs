using System.Collections.Generic;
using UnityEngine;
using WebSocketSharp.Server;
using WebSocketSharp;
using System.Collections.Concurrent;
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public struct GambitStream
{
    public ConcurrentQueue<MessageEventArgs> from_turtle_messages;
    public ConcurrentQueue<string> to_turtle_messages;
    public WebSocketConnection connection;
    public bool locked;
}

public class WebsocketServer : MonoBehaviour
{
    public static WebsocketServer Instance { get; private set; }
    private WebSocketServer wssv;
    public MissionControl MissionControl;
    public string serverName;

    Dictionary<string, GambitStream> Streams = new Dictionary<string, GambitStream>();

    // Start is called before the first frame update
    void Start()
    {
        Instance = this;

        wssv = new WebSocketServer(System.Net.IPAddress.Any , 5757);

        // Add the WebSocket service
        wssv.AddWebSocketService<WebSocketConnection>("/" + serverName, s => {
            s.GUID = Guid.NewGuid().ToString();
            
            var outt = new ConcurrentQueue<string>();
            var inn = new ConcurrentQueue<MessageEventArgs>();
            Debug.Log("Create GambitStream...");

            GambitStream stream = new GambitStream {
                from_turtle_messages = inn,
                to_turtle_messages = outt,
                connection = s,
                locked = false,
            };
            Debug.Log("Register GambitStream");
            Debug.Log("GUID :  [" + s.GUID + "]");

            Streams.Add(s.GUID, stream);

            Debug.Log("Assign GambitStream...");

            s.Stream = stream;
            
            Debug.Log("GambitStream assigned...");

            //s.out_queue = outt;
            //s.in_queue = inn;
        });
        // Start the server
        wssv.Start();

        Debug.Log("WebSocket server started on port 8080");
    }

    void OnDestroy()
    {
        // Stop the WebSocket server when the script is destroyed
        if (wssv != null && wssv.IsListening)
        {
            wssv.Stop();
            Debug.Log("WebSocket server stopped");
        }
    }

    void Update()
    {
        foreach (KeyValuePair<string,GambitStream> stream in Streams)
        {
            stream.Value.connection.Update();
            //stream.Value.currentMessage;
            ConcurrentQueue<MessageEventArgs> from_turtle = stream.Value.from_turtle_messages;
            if (from_turtle.TryDequeue(out var message))
            {
                //Debug.Log("DEQUEUE! from " + stream.Key);
                Handle(message, stream.Value);
            }
        }
    }

    struct DebugMessage
    {
        public string type;
        public string message;
    }

    public void Handle(MessageEventArgs e, GambitStream stream)
    {
        Debug.Log("Handling RAW DATA [" + e.Data + "]");
        GambitMessage message = JsonUtility.FromJson<GambitMessage>(e.Data);

        Debug.Log("HANDLE MESSAGE OF TYPE [" + message.Type + "]");

        if (message.Type == "Identity")
        {
            //find turtle, else make?
            IdentityMessage IdentityMessage = IdentityMessage.From(e.Data);
            //Debug.Log("Stream ID : " + stream.connection.GUID);
            string Identity = IdentityMessage.Identity;
            


            GameObject existing = GameObject.Find("GambitTurtle (" + Identity + ")");
            if (existing == null)
            {
                GambitTurtle turtle = MissionControl.NewTurtle(Identity, stream, Vector3.zero);
                turtle.SetLabel(IdentityMessage.Label);
                turtle.FetchWorldPosition();
                turtle.SetStatus(GambitTurtleStatus.Alive);
            }
            else
            {
                GambitTurtle turtle = existing.GetComponent<GambitTurtle>();
                turtle.SetIdentity(Identity);
                turtle.SetLabel(IdentityMessage.Label);
                turtle.ClearMultiPartMessageStatus();

                turtle.Stream = stream;
                turtle.SetStatus(GambitTurtleStatus.Alive);
                turtle.gameObject.SetActive(true);
                turtle.FetchWorldPosition();
                turtle.FetchInventory();
                turtle.FetchSelectedSlotIndex();
            }
        } 
        else if (message.Type == "MultiPartMessagePleaseSend")
        {
            //get the turtle. get the MPM, resend :
            Debug.LogWarning("handling [MultiPartMessagePleaseSend]");
            MultiPartMessagePleaseSend mpm = MultiPartMessagePleaseSend.From(message);
            mpm.ResendRequestedParts();

        }
        else if (message.Type == "MultiPartMessageOK")
        {
            message.turtle.MultiPartMessageStatus = "EXECUTE";
            //get the turtle. get the MPM, resend :
            message.turtle.multiPartMessage.Execute();
            message.turtle.MultiPartMessageStatus = "NONE";
            //we await a EXECUTED from turtle to ensure we do not attempt to send while turtle is "busy"
        }
        else if (message.Type == "MultiPartMessageExecuted")
        {
            Debug.LogWarning("Set MPM on turtle to null. allow next message!");
            message.turtle.ClearMultiPartMessageStatus();
        }
        else if (message.Type == "Observation")
        {
            Debug.Log("OBSERVATION from turtle received. processing...");
            ObservationMessage.Handle(message);
        }
        else if (message.Type == "LOG")
        {
            Debug.Log("LOG from turtle received. processing...");
            dynamic jsonObject = JsonConvert.DeserializeObject(e.Data);
            Debug.LogWarning(((JValue)(jsonObject.Data)).Value);
            //ReportMessage ReportMessage = ReportMessage.From(message);
            //ReportMessage.Log();
        }
        else if (message.Type == "SAY")
        {
            //GambitMessages.Generic.Report str = message.As<GambitMessages.Generic.Report>();
            //Debug.Log(str.message);
        }
        else if (message.Type == "REPORT")
        {
            //GambitMessages.Generic.Report str = message.As<GambitMessages.Generic.Report>();
            //Debug.Log(str.message);
        } else
        {
            Debug.LogWarning("UNKNOWN MESSAEG TYPE [" + message.Type + "]");
        }
    }

    public void Send(string message, GambitStream stream)
    {
        //Debug.Log("Send to turtle the message " + message + " on the stream #" + stream.connection.GUID);
        stream.to_turtle_messages.Enqueue(message);
    }
}
