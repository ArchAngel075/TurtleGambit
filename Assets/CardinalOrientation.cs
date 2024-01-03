public enum CardinalDirectionEnum
{
    North,
    East,
    South,
    West
}

public class CardinalDirectionUtility
{
    // Function to compute the next cardinal direction when rotating right
    public static CardinalDirectionEnum RotateLeft(CardinalDirectionEnum currentDirection)
    {
        int nextIndex = ((int)currentDirection + 1) % 4;
        return (CardinalDirectionEnum)nextIndex;
    }

    // Function to compute the next cardinal direction when rotating left
    public static CardinalDirectionEnum RotateRight(CardinalDirectionEnum currentDirection)
    {
        int nextIndex = ((int)currentDirection - 1 + 4) % 4;
        return (CardinalDirectionEnum)nextIndex;
    }

    public static CardinalDirectionEnum FromString(string str)
    {
        str = str.ToLower();
        switch (str) 
        {
            case "north":
                return CardinalDirectionEnum.North;
            case "east":
                return CardinalDirectionEnum.East;
            case "south":
                return CardinalDirectionEnum.South;
            case "west":
                return CardinalDirectionEnum.West;
            
        }
        return CardinalDirectionEnum.North;
    }

    // Function to compute the Unity angle based on the cardinal direction
    public static float ComputeUnityAngle(CardinalDirectionEnum facingDirection)
    {
        float unityAngle = 0f;

        switch (facingDirection)
        {
            case CardinalDirectionEnum.North:
                unityAngle = 0f;
                break;
            case CardinalDirectionEnum.East:
                unityAngle = -90f;
                break;
            case CardinalDirectionEnum.South:
                unityAngle = -180f;
                break;
            case CardinalDirectionEnum.West:
                unityAngle = -270f;
                break;
        }

        return unityAngle;
    }
}