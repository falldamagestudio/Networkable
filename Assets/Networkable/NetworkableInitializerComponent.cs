using UnityEngine;
using UnityEngine.Assertions;

/// <summary>
/// Initialization trigger for Networkable.
/// Place this component on a GameObject in the first scene that your game loads.
/// </summary>
public class NetworkableInitializerComponent : MonoBehaviour
{
    static bool initialized = false;

    public NetworkableSettings NetworkableSettings;

    void Awake()
    {
        Assert.IsNotNull(NetworkableSettings);

        if (!initialized)
        {
            NetworkableInitializer.Initialize(NetworkableSettings, new PhotonRegisterSerializers());
            initialized = true;
        }
    }
}
