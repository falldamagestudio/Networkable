using System;

/// <summary>
/// Represents an object, which can be sent over the network by ID.
/// None of the objects' internal data will be sent.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = true)]
public class NetworkableById : System.Attribute
{
}
