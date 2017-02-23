
# Networkable

## Purpose

Enable simple transfer of object references between Unity game clients in the same game session. Also, support for class hierarchies. Integration with Photon Unity Networking.

Pull requests welcome!

## Contents

- The library contains two attributes, [NetworkableById] and [NetworkableByValue]. You add these tags to classes which you want to send over the network.
  You can add them to base classes as well; the attributes will then apply to all child classes.
- The library contains a ScriptableObject, NetworkableSettings. It represents the database of class IDs and asset IDs that are known at compile/build time. You need to trigger refresh of this database manually.
  It is used to ensure that classes and static assets/prefabs are assigned the same IDs in all game clients.
- The library has a component, NetworkableInitializerComponent. It contains initialization logic for the Networkable system. You need to place this component in your start scene.

## Examples

### Pass a reference to a ScriptableObject asset over the network

* Create a subclass from ScriptableObject (in this example called PlayerIcon).
* Tag your PlayerIcon class with the [NetworkableById] attribute.
* Create a couple of instances of the PlayerIcon class in your Assets folder.
* Update NetworkableSettings's Type IDs and Asset IDs.

You can now pass a PlayerIcon reference over the network without any extra logic:

```
	[NetworkableById]
	public class PlayerIcon : ScriptableObject
	{
		Texture2D Image;
		string Name;
	}

	public class PlayerIconVisualizer : MonoBehaviour
	{
		public void PlayerIconChosen(int playerId, PlayerIcon icon)
		{
			// Photon will convert 'obj' from a PlayerIcon reference to a PlayerIcon ID before sending it over the network
			photonView.RPC("Rpc_PlayerIconChosen", PhotonTargets.All, playerId, obj);
		}
		
		[PunRPC]
		public void Rpc_PlayerIconChosen(int playerId, PlayerIcon icon)
		{
			// Photon will receive the PlayerIcon ID and convert it to a PlayerIcon reference before Rpc_PlayerIconChosen is invoked
			Debug.Log("Player icon name: " + icon.Name;
		}
	}
```

### Pass a reference to a prefab over the network

Networkable does not support assigning IDs to prefabs per se. However, you can put a dummy component on your prefabs,
and pass that component by ID.

* Create a subclass from MonoBehaviour (in this example called DynamicallySpawnableObject).
* Tag your DynamicallySpawnableObject class with the [NetworkableById] attribute.
* Add your DynamicallySpawnableObject component to the prefabs which you want to be able to reference in network traffic.
* Update NetworkableSettings's Type IDs and Asset IDs.

You can now pass a prefab reference over the network with a little extra logic:

```
	[NetworkableById]
	public class DynamicallySpawnableObject : MonoBehaviour
	{
	}

	public class PlayerLogic : MonoBehaviour
	{
		public void RequestInstantiatePrefab(GameObject prefab, Vector3 position, Quaternion orientation)
		{
			DynamicallySpawnableObject obj = prefab.GetComponent<DynamicallySpawnableObject>();
			Assert.IsNotNull(obj);

			// Photon will convert 'obj' from a DynamicallySpawnableObject reference
			//   to a DynamicallySpawnableObject ID before sending it over the network
			photonView.RPC("Rpc_InstantiatePrefab", PhotonTargets.All, obj, position, orientation);
		}
		
		[PunRPC]
		public void Rpc_InstantiatePrefab(DynamicallySpawnableObject obj, Vector3 position, Quaternion orientation)
		{
			// Photon will receive the DynamicallySpawnableObject ID and convert it to a
			//   DynamicallySpawnableObject reference before Rpc_InstantiatePrefab is invoked
			
			GameObject prefab = obj.gameObject;
			GameObject.Instantiate(prefab, position, orientation);
		}
		
	}
```

### Pass an object from a C# class hierarchy by value over the network

This can be useful if you want to pass different kinds of structures. As an example, perhaps you want to be able to describe different kinds of actions,
but send them as a single base class type over the network.

* Create a base class for your actions (in this example called Action).
* Tag your base class with the [NetworkableByValue] attribute.
* Create subclasses from Action. Add serialization/deserialization logic to your subclasses.
* Update NetworkableSettings's Type IDs and Asset IDs.

You can now pass different actions between clients with a single RPC:

```
[NetworkableByValue]
public abstract class Action
{
	public void TriggerAction();
}

public class MeleeAttack : Action
{
	int AnimationId;

	public MeleeAttack(animationId)
	{
		AnimationId = animationId;
	}

	public override void TriggerAction()
	{
		Debug.Log("Start melee attack with animation ID " + AnimationId);
	}

	// Typical implementation of serialization of an object using Photon
	
    static byte[] serializationBuffer = new byte[4];

    public static new short Serialize(StreamBuffer outStream, object customObject)
	{
		short length = 0;
		Protocol.Serialize(AnimationId, serializationBuffer, ref length);
		outStream.write(serializationBuffer, 0, length);
		return length; 
	}

	// Typical implementation of deserialization of an object using Photon
	
    static byte[] deserializationBuffer = new byte[4];

    public static new object Deserialize(StreamBuffer inStream, short length)
	{
        inStream.Read(deserializationBuffer, 0, length);
		short offset = 0;
		int animationId;
		Protocol.Deserialize(out animationId, deserializationBuffer, ref offset);

		return new MeleeAttack(animationId);
	}
}

public class Jump : Action
{
	float Height;

	public MeleeAttack(float height)
	{
		Height = height;
	}

	public override void TriggerAction()
	{
		Debug.Log("Start jump action with height " + Height);
	}

	// Typical implementation of serialization of an object using Photon
	
    static byte[] serializationBuffer = new byte[4];

    public static new short Serialize(StreamBuffer outStream, object customObject)
	{
		short length = 0;
		Protocol.Serialize(Height, serializationBuffer, ref length);
		outStream.write(serializationBuffer, 0, length);
		return length; 
	}

	// Typical implementation of deserialization of an object using Photon
	
    static byte[] deserializationBuffer = new byte[4];

    public static new object Deserialize(StreamBuffer inStream, short length)
	{
        inStream.Read(deserializationBuffer, 0, length);
		short offset = 0;
		float height;
		Protocol.Deserialize(out height, deserializationBuffer, ref offset);

		return new JumpAction(height);
	}
}

public class MyPlayerComponent : MonoBehaviour
{
	public void TriggerMeleeAttack(int animationId)
	{
		MeleeAttack meleeAttack = new MeleeAttack(animationId);
		// meleeAttack will be serialized by-value and sent as a MeleeAttack to all clients
		photonView.RPC("Rpc_TriggerAction", PhotonTargets.All, meleeAttack);
	}

	public void TriggerMeleeAttack(float height)
	{
		JumpAction jump = new JumpAction(height);
		// jump will be serialized by-value and sent as a JumpAction to all clients
		photonView.RPC("Rpc_TriggerAction", PhotonTargets.All, jumpp);
	}
	
	[PunRPC]
	public void Rpc_TriggerAction(Action action)
	{
		// Photon will receive an object type Action or any subtype, create the corresponding object with its contents,
		//   and provide the object up-casted to the Action base class
		
		action.TriggerAction();
	}

}
```

### Pass scene objects by reference over the network

This is tricky, but can be done. The basic principle is to have some initialization logic that runs when the scene begins playing. It has to be designed so that every client will register the objects in the exact same order. Example:

NOTE: I am not 100% certain whether the order of siblings in the same GameObject hierarchy level is deterministic. Perhaps Networkable should assign IDs to these objects offline instead?

```
[NetworkableById]
public class SpawnPoint : MonoBehaviour
{
}

public class InitializeSpawnPointsInScene : MonoBehaviour
{
	void Start()
	{
		// Assign IDs to all SpawnPoint components beneath this gameObject; since all clients run the same logic, the IDs will match between clients
		foreach (Transform child in transform)
		{
			SpawnPoint spawnPoint = child.GetComponent<SpawnPoint>();
			if (spawnPoint != null)
				NetworkableId<SpawnPoint>.Add(spawnPoint);
		}
	}
}

```


### Pass dynamically-created objects by reference over the network

This is also tricky, but can be done. The basic principle is to design your code so that the MasterClient creates the object and assigns the ID, and then tells all clients to do the same.

```
public class MyPlayerComponent : MonoBehaviour
{
	public void RequestInstantiatePrefab(GameObject prefab, Vector3 position, Quaternion orientation)
	{
		DynamicallySpawnableObject obj = prefab.GetComponent<DynamicallySpawnableObject>();
		Assert.IsNotNull(obj);

		// Photon will convert 'obj' from a DynamicallySpawnableObject reference
		//   to a DynamicallySpawnableObject ID before sending it over the network
		photonView.RPC("RpcMaster_InstantiatePrefab", PhotonTargets.MasterClient, obj, position, orientation);
	}

	[PunRPC]
	public void RpcMaster_InstantiatePrefab(DynamicallySpawnableObject componentOnPrefab, Vector3 position, Quaternion orientation)
	{
		// Photon will receive the DynamicallySpawnableObject ID and convert it to a
		//   DynamicallySpawnableObject reference before RpcMaster_InstantiatePrefab is invoked

		// Instantiate prefab on MasterClient
		GameObject instance = InstantiatePrefab(componentOnPrefab.gameObject, position, orientation);
		DynamicallySpawnableObject componentOnInstance = instance.GetComponent(DynamicallySpawnableObject);
		
		// Assign ID to the newly created instance
		NetworkableId<DynamicallySpawnableObject>.Add(componentOnInstance);
		int newInstanceId = NetworkableId<DynamicallySpawnableObject>.ToId(componentOnInstance);

		// Instruct all other clients to create the same prefab, using the assigned ID
		photonView.RPC("RpcClient_InstantiatePrefab", PhotonTargets.Others, componentOnPrefab, newInstanceId, position, orientation);
	}


	DynamicallySpawnableObject InstantiatePrefab(GameObject prefab, Vector3 position, Quaternion orientation)
	{
		return GameObject.Instantiate(prefab, position, orientation);
	}

}
```

## Limitations

### No support for Resources

Do not place any networkable assets in Resources. Networkable does not support this.

### No support for client-authoritative object ID assignment

The only documented, known-good approach requires that only one client assigns all IDs to dynamically-created objects. You can let any client initiate creation of new objects, but
you will still need to pass the task of ID assignment to the MasterClient. If you let the requesting client assign the ID you will run into race conditions where different clients
assign the same ID to different objects.

### Limited support for scene changes

Networkable handles static assets well. For any IDs that are assigned as part of scene initialization and dynamic scene creation, you must manually ensure that you remove the
ID<->item mappings before objects are deleted. Any mistakes will result in resource leaks or null pointer exceptions.

### All networkable assets are automatically referenced in the build

One of the purposes of the Networkable system is to enable a Unity game client to receive an ID and instantly be able to translate that to a asset reference. A side effect of that
is that the NetworkableSettings asset will contain references to all assets of networkable types in the entire project.
All these assets will be included in your builds, regardless of whether they are actually used at runtime.
