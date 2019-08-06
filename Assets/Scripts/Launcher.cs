# **Photon Unity Network - Tutorial**

## **1 - Lobby**

The PUN Basic Tutorial is a Unity based tutorial. It will show you how to develop your own multiplayer enabled application powered by Photon Cloud and how to use Characters using Animator for their animations. We'll learn along the way many important features, tips and tricks to get a good overview of the approach to network based development with PUN.

## **Contents**

* [Connection to Server, Room Access and Creation](#Connection-to-Server,-Room-Access-and-Creation)
  * [Launcher.cs Summary](#Launcher.cs-Summary)
* [PUN Callbacks](#PUN-Callbacks)
  * [Implementing Callback Interfaces](#Implementing-Callback-Interfaces)
* [Extending MonoBehaviourPunCallbacks](#Extending-MonoBehaviourPunCallbacks)
* [Expose Fields in Unity Inspector](#Expose-Fields-in-Unity-Inspector)
* [Full Source Code](#Full-Source-Code)
  * [Launcher.cs](#Launcher.cs)

## **Connection to Server, Room Access and Creation**

Let's first tackle the core of this tutorial, being able to connect to Photon Cloud server and join a Room or create one if necessary.

1. Create a new scene, and save it as `Launcher`.
2. Create a new C# script `Launcher`.
3. Create an empty GameObject in the Hierarchy named `Launcher`.
4. Attach the C# script `Launcher` to the GameObject `Launcher`.
5. Edit the C# script `Launcher` to have its content as below:

```c#
using UnityEngine;
using Photon.Pun;

public class Launcher : MonoBehaviour
{
    #region Private Serializable Fields

    // empty for now

    #endregion

    #region Private Fields

    /// <summary>
    /// This client's version number.
    /// Users are separated from each other by gameVersion (which allows you to make breaking changes).
    /// </summary>
    string gameVersion = "1";

    #endregion

    #region MonoBehaviour CallBacks

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
    /// </summary>
    void Awake()
    {
        // #Critical
        // this makes sure we can use PhotonNetwork.LoadLevel() on the master client
        // and all clients in the same room sync their level automatically
        PhotonNetwork.AutomaticallySyncScene = true;
    }

    /// <summary>
    /// MonoBehaviour method called on GameObject by Unity during initialization phase.
    /// </summary>
    void Start()
    {
        Connect();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Start the connection process.
    /// - If already connected, we attempt joining a random room
    /// - if not yet connected, Connect this application instance to Photon Cloud Network
    /// </summary>
    public void Connect()
    {
        // we check if we are connected or not,
        // we join if we are,
        // else we initiate the connection to the server.
        if (PhotonNetwork.IsConnected)
        {
            // #Critical
            // we need at this point to attempt joining a Random Room.
            // If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
            PhotonNetwork.JoinRandomRoom();
        }
        else
        {
            // #Critical
            // we must first and foremost connect to Photon Online Server.
            PhotonNetwork.GameVersion = gameVersion;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

#endregion

}
```

6. Save the C# script `Launcher`.

### **Launcher.cs Summary**

Let's review what's in this script so far, first from a general Unity point-of-view and then looking at the PUN specific calls we make.

* **Namespace**

  While not mandatory, giving a proper namespace to your script prevents clashes with other assets and developers. What if another developer create a class Launcher, too? Unity will complain, and you or that developer will have to rename the class for Unity to allow the project to be executed. This can be tricky if the conflict comes from an asset you downloaded from the Asset Store. Now, Launcher class is really Com.MyCompany.MyGame.Launcher actually under the hood it's very unlikely that someone else will have this exact same namespace because you own this domain and so using reversed domain convention as namespace makes your work safe and well organized. Com.MyCompany.MyGame is supposed to be replaced by your own reversed domain name and game name which is good convention to follow.
* **MonoBehaviour Class**

  Notice that we are deriving our class from MonoBehaviour which essentially turns our class into an Unity Component that we can then drop onto a GameObject or Prefab. A class extending a MonoBehaviour has access to many very important methods and properties. In your case we'll use two callback methods, Awake() and Start().
* **PhotonNetwork.GameVersion**

  Notice the gameVersion variable representing your gameversion. You should leave it to "1" until you need to make breaking changes on a project that is already Live.
* **PhotonNetwork.ConnectUsingSettings()**

  During Start() we call our public function Connect() which calls this method. The important information to remember here is that this method is the starting point to connect to Photon Cloud.
* **PhotonNetwork.AutomaticallySyncScene**

  Our game will have a resizable arena based on the number of players, and to make sure that the loaded scene is the same for every connected player, we'll make use of the very convenient feature provided by Photon: PhotonNetwork.AutomaticallySyncScene When this is true, the MasterClient can call PhotonNetwork.LoadLevel() and all connected players will automatically load that same level.

  At this point, you can save the Launcher Scene and open the PhotonServerSettings (select it from the Unity menu Window/Photon Unity Networking/Highlight Photon Server Settings), we need to set the PUN Logging to "Full":
  
  ![alt text](./img/01_01.png "PhotonSettings debug level Setup")
  
  A good habit to have when coding is to always test potential failure. Here we assume the computer is connected to the Internet, but what would happen if the computer isn't connected to the Internet? Let's find out. Turn off Internet on your computer and play the Scene. You should see this error in the Unity console:
  
  ```terminal
  Connect() to 'ns.exitgames.com' failed: System.Net.Sockets.SocketException: No such host is known.
  ```
  
  Ideally, our script should be made aware of this issue and react elegantly to these situation and propose a responsive experience no matter what situation or problem could arise.
  
  Let's now handle both of these cases and be informed within our Launcher script that we are indeed connected or not to Photon Cloud. This will be the perfect introduction to PUN Callbacks.

## **PUN Callbacks**
PUN is very flexible with callbacks and offers two different implementations. Let's cover all approaches for the sake of learning and we'll pick the one that fits best depending on the situation.

### **Implementing Callback Interfaces**

PUN provides C# interfaces that you can implement in your classes:

* `IConnectionCallbacks`: connection related callbacks.
* `IInRoomCallbacks`: callbacks that happen inside the room.
* `ILobbyCallbacks`: lobby related callbacks.
* `IMatchmakingCallbacks`: matchmaking related callbacks.
* `IOnEventCallback`: a single callback for any received event. This is equivalent to the C# event OnEventReceived.
* `IWebRpcCallback`: a single callback for receiving WebRPC operation response.
* `IPunInstantiateMagicCallback`: a single callback for instantiated PUN prefabs.
* `IPunObservable`: PhotonView serialization callbacks.
* `IPunOwnershipCallbacks`: PUN ownership transfer callbacks.

Callback interfaces must be registered and unregistered. Call `PhotonNetwork.AddCallbackTarget(this)` and `PhotonNetwork.RemoveCallbackTarget(this)` (likely within `OnEnable()` and `OnDisable()` respectivly)

This is a very secure way to make sure a class complies to all the interface, but forces the developer to implement all the interface declarations. Most good IDEs will make this task very easy. However, the script could end up with a lot of methods that may do nothing, yet all methods must be implemented for Unity compiler to be happy. So this is really when your script is going to make heavy use of all or most PUN Features.

We are indeed going to use `IPunObservable`, further down this tutorial for data serialization.

## **Extending MonoBehaviourPunCallbacks**

The other technique, which is the one we'll be using often, is the most convenient one. Instead of creating a class that derives from MonoBehaviour, we'll derive the class from MonoBehaviourPunCallbacks, as it exposes specific properties and virtual methods for us to use and override at our convenience. It's very practical, because we can be sure that we don't have any typos, and we don't need to implement all methods.

Note: when overriding, most IDEs will by default implement a base call and fill that up for you automatically. In our case we don't need to, so as a general rule for MonoBehaviourPunCallbacks, never call the base method unless you override `OnEnable()` or `OnDisable()`. Always call the base class methods if you override `OnEnable()` and `OnDisable()`.

So, let's put this in practice with `OnConnectedToMaster()` and `OnDisconnected()` PUN callbacks.

1. Edit the C# script `Launcher`
2. Add `using Photon.Realtime;` on top of the file before the class definition.
3. Modify the base class from `MonoBehaviour` to `MonoBehaviourPunCallbacks`
4. Add the following two methods at the end of the class, within a region `MonoBehaviourPunCallbacks Callbacks` for clarity.

    ```c#
    #region MonoBehaviourPunCallbacks Callbacks

    public override void OnConnectedToMaster()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
    }

    #endregion
    ```
5. Save `Launcher` script.





6. Now if we play this scene with or without Internet, we can take the appropriate steps to inform the Player and/or proceed further into the logic. We'll deal with this in the next section when we'll start building the UI. Right now we'll deal with the successful connections:

    So, we append to the `OnConnectedToMaster()` method the following call:

    ```c#
    // #Critical
    // The first thing we try to do is to join a potential existing room.
    // If there is, good, else, we'll be called back with OnJoinRandomFailed()
    PhotonNetwork.JoinRandomRoom();
    ```
7. As the comment says, we need to be informed if the attempt to join a random room failed, in which case we need to actually create a room, so we implement `OnJoinRandomFailed()` PUN callback in our script and create a room using `PhotonNetwork.CreateRoom()` and, you guessed already, the related PUN callback `OnJoinedRoom()` which will inform your script when we effectively join a room:

    ```c#
    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

        // #Critical
        // We failed to join a random room.
        // Maybe none exists or they are all full.
        // No worries, we create a new room.
        PhotonNetwork.CreateRoom(null, new RoomOptions());
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
    }
    ```
8. Now if you run the scene, and you should end up following the logical successions of connecting to PUN, attempting to join an existing room, else creating a room and join that newly created room.

    At this point of the tutorial, since we have now covered the critical aspects of connecting and joining a room, there are a few things not very convenient, and they need to be addressed, sooner than later. These are not really related to learning PUN, however important from an overall perspective.

## **Expose Fields in Unity Inspector**

You may already know this, but in case you don't, MonoBehaviours can automatically expose fields to the Unity Inspector. By default all Public Fields are exposed unless they are marked with `[HideInInspector]`. And if we want to expose non public fields we can make use of the attribute `[SerializeField]`. This is a very important concept within Unity, and in our case we'll modify the maximum number of players per room and expose it in the inspector so that we can set that up without touching the code itself.

We'll do the same for the maximum number of players per room. Hardcoding this within the code isn't the best practice, instead, let's make it as a public variable, so that we can later decide and toy around with that number without the need for recompiling.

At the beginning of the class declaration, within the `Private Serializable Fields` region let's add:

```c#
/// <summary>
/// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
/// </summary>
[Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
[SerializeField]
private byte maxPlayersPerRoom = 4;
```

and then we modify the PhotonNetwork.CreateRoom() call and use this new field instead of the harcoded number we were using before.

```c#
// #Critical: we failed to join a random room, maybe none exists or they are all full. No worries, we create a new room.
PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
```

![alt text](./img/01_02.png "Launcher Script Inspector")

So, now we don't force the script to use a static MaxPlayers value, we simply need to set it in the Unity inspector, and hit run, no need to open the script, edit it, save it, wait for Unity to recompile and finally run. It's a lot more productive and flexible this way.

## **Full Source Code**

### **Launcher.cs**

<details>
    <summary>
        Click to see 'Launcher.cs'
    </summary>
    <p>

```c#
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

namespace com.unity.photon
{
    public class Launcher : MonoBehaviourPunCallbacks
    {
        #region Private Serializable Fields

        /// <summary>
        /// The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created.
        /// </summary>
        [Tooltip("The maximum number of players per room. When a room is full, it can't be joined by new players, and so new room will be created")]
        [SerializeField]
        private byte maxPlayersPerRoom = 4;

        #endregion


        #region Private Fields

        /// <summary>
        /// This client's version number.
        /// Users are separated from each other by gameVersion (which allows you to make breaking changes).
        /// </summary>
        string gameVersion = "1";

        #endregion


        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            // #Critical
            // this makes sure we can use PhotonNetwork.LoadLevel() on the master client
            // and all clients in the same room sync their level automatically
            PhotonNetwork.AutomaticallySyncScene = true;
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            Connect();
        }

        #endregion


        #region MonoBehaviourPunCallbbacks CallBacks

        public override void OnConnectedToMaster()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: `OnConnectedToMaster()` was called by PUN");

            // #Critical
            // The first thing we try to do is to join a potential existing room.
            // If there is, good, else, we'll be called back with OnJoinRandomFailed()
            PhotonNetwork.JoinRandomRoom();
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: `OnDisconnected()` was called by PUN with reason: {0}.", cause);
        }

        public override void OnJoinRandomFailed(short returnCode, string message)
        {
            Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");

            // #Critical
            // We failed to join a random room.
            // Maybe none exists or they are all full.
            // No worries, we create a new room.
            PhotonNetwork.CreateRoom(null, new RoomOptions { MaxPlayers = maxPlayersPerRoom });
        }

        public override void OnJoinedRoom()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Start the connection process.
        /// - If already connected, we attempt joining a random room
        /// - if not yet connected, Connect this application instance to Photon Cloud Network
        /// </summary>
        public void Connect()
        {
            // we check if we are connected or not,
            // we join if we are,
            // else we initiate the connection to the server.
            if (PhotonNetwork.IsConnected)
            {
                // #Critical
                // we need at this point to attempt joining a Random Room.
                // If it fails, we'll get notified in OnJoinRandomFailed() and we'll create one.
                PhotonNetwork.JoinRandomRoom();
            }
            else
            {
                // #Critical
                // we must first and foremost connect to Photon Online Server.
                PhotonNetwork.GameVersion = gameVersion;
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        #endregion

    }
}

```

</p>
</details>


<div style="text-align: right">

[Back to Top](#Contents)

</div>
