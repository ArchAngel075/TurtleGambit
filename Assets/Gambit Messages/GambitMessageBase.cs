using UnityEngine;

public enum Directionality
{
    FROM_TURTLE,
    TO_TURTLE,
}

public class GambitMessage
{
    public string turtleId;
    public string Type;
    public string Data;
    public virtual string Prepare() => JsonUtility.ToJson(null);

    public virtual Directionality GetDirection() { return Directionality.FROM_TURTLE; }

    public GambitTurtle turtle => GameObject.Find("GambitTurtle (" + turtleId + ")").GetComponent<GambitTurtle>();

}

public class IdentityMessage : GambitMessage
{
    private struct Content { public string Identity; public string Label; }
    public string Identity
    {
        get
        {
            return JsonUtility.FromJson<Content>(Data).Identity;
        }
    }

    public string Label
    {
        get
        {
            return JsonUtility.FromJson<Content>(Data).Label;
        }
    }

    public static IdentityMessage From(string message)
    {
        return JsonUtility.FromJson<IdentityMessage>(message);
    }
}

public class ReportMessage
{
    private struct Content { public string Report; }
    public GambitMessage baseMessage;
    public string Report
    {
        get
        {
            return JsonUtility.FromJson<Content>(baseMessage.Data).Report;
        }
    }

    public static ReportMessage From(GambitMessage message)
    {
        //Debug.Log("from message " + message.Data);
        return new ReportMessage()
        {
            baseMessage = message
        };
    }

    public void Log()
    {
        GameObject turtleGO = GameObject.Find("GambitTurtle (" + baseMessage.turtleId + ")");
        if (turtleGO != null)
        {
            Debug.Log(Report, turtleGO);
        }
    }
}

