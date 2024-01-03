using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public struct MyStruct
{
    public int inty;
    public float floaty;
    public string stringy;
}

public class WebSocketConnection : WebSocketBehavior
{
    private string identity;
    private GambitTurtle turtle;
    public MissionControl MissionControl;
    public string GUID;
    public float timeSinceLastPing;
    public bool awaitingPong = false;
    public static int MAX_TIME_TILL_STALE = 3;
    public static int TIME_BETWEEN_PINGS = 6;

    public bool isStale;

    //public ConcurrentQueue<MessageEventArgs> in_queue;
    //public ConcurrentQueue<MessageEventArgs> out_queue;

    public GambitStream Stream;

    public WebSocketConnection() {
    
        this.EmitOnPing = true;
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        /*
        // Handle incoming messages
        Debug.Log("Received message: " + e.Data);
        
        */
        if(e.IsPing)
        {
            turtle.Status = GambitTurtleStatus.Alive;
            return;
        }
        if (Stream.connection.turtle != null)
        {
            Stream.connection.turtle.Status = GambitTurtleStatus.Alive;
        }
        if (e.Data == "__PONG__")
        {
            //Debug.LogError("PONG");
            awaitingPong = false;
            isStale = false;
            return;
        }
        Stream.from_turtle_messages.Enqueue(e);
        
    }

    public void Update()
    {
        timeSinceLastPing += Time.deltaTime;
        if(awaitingPong)
        {
            if(timeSinceLastPing >= MAX_TIME_TILL_STALE)
            {
                isStale = true;
            }
        }
        if(!awaitingPong && timeSinceLastPing > TIME_BETWEEN_PINGS) 
        {
            doPing();
        }
        timeSinceLastPing = Mathf.Min(TIME_BETWEEN_PINGS, timeSinceLastPing);
        //Debug.Log("Time Since Last Ping Pong :" + timeSinceLastPing);
    }

    public static Stream GenerateStreamFromString(string s)
    {
        var stream = new MemoryStream();
        var writer = new StreamWriter(stream);
        writer.Write(s);
        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    public void Dispatch(string s)
    {
        //Debug.Log("DISPATCH [" + s + "]");
        if(IsAlive)
        {
            byte[] compressedData = new byte[0];
            byte[] b = Encoding.UTF8.GetBytes(s);
            using (var outStream = new MemoryStream())
            {
                using (var compress = new DeflateStream(outStream, CompressionMode.Compress, true))
                {
                    compress.Write(b, 0, b.Length);
                    compress.Flush();
                    compress.Close();
                }
                compressedData = outStream.ToArray();
            }
            // Debug.LogError("compressed data from " + (b.Length.ToString()) + " to " + compressedData.Length.ToString());
            Send(compressedData);
            //Send(Encoding.UTF8.GetBytes(s));
        } else
        {
            //turtle.Status = GambitTurtleStatus.Unloaded;
        }
        //Debug.Log("DISPATCHED");
    }

    protected override void OnOpen()
    {
        Debug.Log("Send handshake...");
        Send("HELLO GAMBIT");
        Debug.Log("Handshake Sent.");
    }

    public void doPing()
    {
        timeSinceLastPing = -Time.deltaTime;
        awaitingPong = true;
        this.Dispatch("__PING__");
        //Debug.LogError("PING");
    }

    protected override void OnClose(CloseEventArgs e)
    {
        base.OnClose(e);
        if(Stream.connection.turtle != null)
        {
            Stream.connection.turtle.Status = GambitTurtleStatus.Unloaded;
        }
    }

    protected override void OnError(WebSocketSharp.ErrorEventArgs e)
    {
        //base.OnError(e);
        Debug.LogError(e.Message);
        if (Stream.connection.turtle != null)
        {
            Stream.connection.turtle.Status = GambitTurtleStatus.Unloaded;
        }
    }

    // Accessor for the turtle member
    public GambitTurtle GetTurtle()
    {
        return turtle;
    }
}