using System;

/// <summary>
// Represents an object, which can be sent over the network by-value.
// The object will be constructed with 'new' on the receiving side -- therefore, it must be a native C# object and cannot derive from UnityEngine.Object.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class NetworkableByValue : System.Attribute
{
    // If you are using Photon, then any concrete class that is tagged with this should also implement the two following methods:
    //
    // Implementation of ExitGames.Client.Photon.SerializeStreamMethod:
    // public static short Serialize(StreamBuffer outStream, object customObject);
    //
    // Implementation of ExitGames.Client.Photon.DeserializeStreamMethod:
    // public static object Deserialize(StreamBuffer inStream, short length);
    //
    // Abstract base classes do not need to have any serialization methods implemented.
}