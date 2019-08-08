# Photon Unity Network Tutorial

## 4. Game Manager and Levels

This section covers the creation of the various scenes where players will be playing.

Each scene will be dedicated for a specific number of players, getting bigger and bigger to fit all players and give them enough space to move around.

Further down this tutorial, we'll implement the logic to load the right level based on the number of players, and for this we'll use a convention that each level will be name with the following format: "Room for X" where X will represent the number of players.

## **Contents**

* [Loading Arena Routine](#Loading-Arena-Routine)
* [Watching Player Connection](#Watching-Player-Connection)
* [Loading Arena from the Lobby](#Loading-Arena-from-the-Lobby)

* [Full Source Code](#Full-Source-Code)
  * [GameManager.cs](#GameManager.cs)
  * [Launcher.cs](#Launcher.cs)

## **Loading Arena Routine**

We've created 4 different rooms, and we named them all with the convention that the last character is the number of players, so it's now very easy to bind the current number of players in the room and the related scene. It's a very effective technique known as "convention over configuration". A "configuration" based approach would have been, for example, maintaining a lookup table list of the scene name for a given number of players in a room. Our scripts would have then looked in that list and be returned a scene in which the name doesn't matter at all. "configuration" requires more scripting in general, that's why we'll go for "convention" here which gets us to working code faster without polluting our code with unrelated features.

1. Open `GameManager` script.
2. Let's add a new method within a new region dedicated to `Private Methods` we'll create for the occasion. Don't forget to save `GameManager` script.

    ```c#
    #region Private Methods

    void LoadArena()
    {
        if (!PhotonNetwork.IsMasterClient)
        {
            Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
        }
        Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
        PhotonNetwork.LoadLevel("Room for " + PhotonNetwork.CurrentRoom.PlayerCount);
    }

    #endregion
    ```

3. Save GameManager script

When we'll call this method, we are going to load the appropriate room, based on the `PlayerCount` property of the room we are in.

There are two things to watch out for here, it's very important.

* `PhotonNetwork.LoadLevel()` should only be called if we are the `MasterClient`. So we check first that we are the `MasterClient` using `PhotonNetwork.IsMasterClient`. It will be the responsibility of the caller to also check for this, we'll cover that in the next part of this section.
* We use `PhotonNetwork.LoadLevel()` to load the level we want, we don't use Unity directly, because we want to rely on Photon to load this level on all connected clients in the room, since we've enabled `PhotonNetwork.AutomaticallySyncScene` for this Game.

Now that we have our function to load the right level, let's bind this with players connecting and disconnecting.

## **Watching Player Connection**

We've studied the various ways to get Photon Callbacks in previous part of the tutorial and now the `GameManager` needs to listen to players connecting and disconnecting. Let's implement this.

1. Open `GameManager` script.
2. Add the following to the `MonoBehaviourPunCallbacks` Region and save `GameManager` script.

    ```c#
    public override void OnPlayerEnteredRoom(Player other)
    {
        Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting


        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            LoadArena();
        }
    }


    public override void OnPlayerLeftRoom(Player other)
    {
        Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects


        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


            LoadArena();
        }
    }
    ```

3. Save `GameManager` script.

Now, we have a complete setup. Every time a player joins or leaves the room, we'll be informed, and we'll call the `LoadArena()` method we've implemented before. However, we'll call `LoadArena()` **ONLY** if we are the `MasterClient` using `PhotonNetwork.IsMasterClient`.

Let's now come back to the `Lobby` to finally be able to load the right scene when joining a room.

## **Loading Arena from the Lobby**

1. Edit the `Launcher` script.
2. Append the following to the `OnJoinedRoom()` method:

    ```c#
    // #Critical:
    // We only load if we are the first player,
    // else we rely on `PhotonNetwork.AutomaticallySyncScene` to sync our instance scene.
    if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
    {
        Debug.Log("We load the 'Room for 1' ");

        // #Critical
        // Load the Room Level.
        PhotonNetwork.LoadLevel("Room for 1");
    }
    ```

3. Save the `Launcher` script.

Let's test this, open the `Launcher` scene, and run it.

Click on *"Play"*, and let the system connect and `join a room`. 

That's it, we now have our lobby working. But if you leave the room, you'll notice that when coming back to the lobby, it automatically rejoins... oops, let's address this.

If you don't know why this happens yet, "simply" analyze the logs. I put simply in quote, because it takes practice and experience to acquire the automatism to overview an issue and know where to look and how to debug it.

Try yourself now and if you are still unable to find the source of the problem, let's do this together.

1. Run the `Launcher` Scene.
2. Hit the "Play" button, and wait until you've joined the room and that `Room for 1` is loaded.
3. Clear the Unity Console.
4. Hit the `Leave Room` Button.
5. Study the Unity Console, notice that *"PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN"* is logged.
6. Stop the `Launcher` Scene.
7. Double Click on the log entry *"PUN Basics Tutoria/Launcher: OnConnectedToMaster() was called by PUN"* the script will be loaded and point to the line of the debug call.
8. Uhmmm... so, every time we get informed that we are connected, we automatically join a random room which is not what we want.

To fix this, we need to be aware of the context. When the User clicks on the "Play" button, we should raise a flag to be aware that the connection procedure originated from the user. Then we can check for this flag to act accordingly within the various Photon Callbacks.

1. Edit the `Launcher` script.
2. Create a new property within the `Private Fields` regions:

    ```c#
    /// <summary>
    /// Keep track of the current process.
    /// Since connection is asynchronous and is based on several callbacks from Photon,
    /// we need to keep track of this to properly and adjust the behavior when we receive call back by Photon.
    /// Typically this is used for the OnConnectedToMaster() callback.
    /// </summary>
    bool isConnecting;
    ```

3. At the beginning of the `Connect()` method add the following:

```c#
// keep track of the will to join a room,
// because when we come back from the game we will get a callback that we are connected,
// so we need to know what to do then
isConnecting = true;
```

4. In `OnConnectedToMaster()` method, surround the `PhotonNetwork.JoinRandomRoom()` with an if statement as follow:

```c#
// we don't want to do anything if we are not attempting to join a room.
// this case where isConnecting is false is typically when you lost or quit the game,
// when this level is loaded, OnConnectedToMaster will be called, in that case
// we don't want to do anything.
if (isConnecting)
{
    // #Critical:
    // The first we try to do is to join a potential existing room.
    // If there is, good, else, we'll be called back with OnJoinRandomFailed()
    PhotonNetwork.JoinRandomRoom();
}
```

5. Save the `Launcher` script.

Now if we test again and run the `Launcher` Scene, and go back and forth between the Lobby and the Game, all is well :) In order to test the automatic syncing of scenes, you'll need to publish the application (publish for desktop, it's the quickest for running tests), and run it alongside Unity, so you have effectively two players that will connected and join a room. If the Unity Editor creates the Room first, it will be the MasterClient and you'll be able to verify in the Unity Console that you get "PhotonNetwork : Loading Level : 1" and later "PhotonNetwork : Loading Level : 2" as you connect with the published instance.

Good! We have covered a lot, but this is only half of the job... :) We need to tackle the Player himself, so let's do that in the next section. Don't forget to take breaks away from the computer from time to time, to be more effective in absorbing the various concepts explained.

Don't hesitate to ask questions on the forum if you are in doubt of a particular feature or if you have trouble following this tutorial and hit an error or an issue that is not covered here, we'll be happy to help :)

## **Full Source Code**

### **GameManager.cs**

<details>
    <summary>
        Click to see 'GameManager.cs'
    </summary>
    <p>

```c#
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace com.unity.photon
{
    public class GameManager : MonoBehaviourPunCallbacks
    {
        #region MonoBehaviourPunCallbacks CallBacks

        /// <summary>
        /// Called when the local player left the room. We need to load the launcher scene.
        /// </summary>
        public override void OnLeftRoom()
        {
            SceneManager.LoadScene(0);
        }

        public override void OnPlayerEnteredRoom(Player other)
        {
            Debug.LogFormat("OnPlayerEnteredRoom() {0}", other.NickName); // not seen if you're the player connecting


            if (PhotonNetwork.IsMasterClient)
            {
                Debug.LogFormat("OnPlayerEnteredRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


                LoadArena();
            }
        }


        public override void OnPlayerLeftRoom(Player other)
        {
            Debug.LogFormat("OnPlayerLeftRoom() {0}", other.NickName); // seen when other disconnects


            if (PhotonNetwork.IsMasterClient)
            {
                Debug.LogFormat("OnPlayerLeftRoom IsMasterClient {0}", PhotonNetwork.IsMasterClient); // called before OnPlayerLeftRoom


                LoadArena();
            }
        }

        #endregion


        #region Private Methods

        void LoadArena()
        {
            if (!PhotonNetwork.IsMasterClient)
            {
                Debug.LogError("PhotonNetwork : Trying to Load a level but we are not the master Client");
            }
            Debug.LogFormat("PhotonNetwork : Loading Level : {0}", PhotonNetwork.CurrentRoom.PlayerCount);
            PhotonNetwork.LoadLevel("Room for " + PhotonNetwork.CurrentRoom.PlayerCount);
        }

        #endregion


        #region Public Methods

        public void LeaveRoom()
        {
            PhotonNetwork.LeaveRoom();
        }

        #endregion
    }
}

```

</p>
</details>

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

        /// <summary>
        /// Keep track of the current process.
        /// Since connection is asynchronous and is based on several callbacks from Photon,
        /// we need to keep track of this to properly and adjust the behavior when we receive call back by Photon.
        /// Typically this is used for the OnConnectedToMaster() callback.
        /// </summary>
        bool isConnecting;

        #endregion


        #region Public Fields

        [Tooltip("The UI Panel to let the user enter name, connect and play")]
        [SerializeField]
        private GameObject controlPanel;

        [Tooltip("The UI Label to inform the user that the connection is in progress")]
        [SerializeField]
        private GameObject progressLabel;

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
            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
        }

        #endregion


        #region MonoBehaviourPunCallbbacks CallBacks

        public override void OnConnectedToMaster()
        {
            Debug.Log("PUN Basics Tutorial/Launcher: `OnConnectedToMaster()` was called by PUN");

            // we don't want to do anything if we are not attempting to join a room.
            // this case where isConnecting is false is typically when you lost or quit the game,
            // when this level is loaded, OnConnectedToMaster will be called, in that case
            // we don't want to do anything.
            if (isConnecting)
            {
                // #Critical:
                // The first we try to do is to join a potential existing room.
                // If there is, good, else, we'll be called back with OnJoinRandomFailed()
                PhotonNetwork.JoinRandomRoom();
            }
        }

        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: `OnDisconnected()` was called by PUN with reason: {0}.", cause);

            progressLabel.SetActive(false);
            controlPanel.SetActive(true);
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
            progressLabel.SetActive(false);
            controlPanel.SetActive(false);

            Debug.Log("PUN Basics Tutorial/Launcher: OnJoinedRoom() called by PUN. Now this client is in a room.");

            // #Critical:
            // We only load if we are the first player,
            // else we rely on `PhotonNetwork.AutomaticallySyncScene` to sync our instance scene.
            if (PhotonNetwork.CurrentRoom.PlayerCount == 1)
            {
                Debug.Log("We load the 'Room for 1' ");

                // #Critical
                // Load the Room Level.
                PhotonNetwork.LoadLevel("Room for 1");
            }
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
            // keep track of the will to join a room,
            // because when we come back from the game we will get a callback that we are connected,
            // so we need to know what to do then
            isConnecting = true;

            progressLabel.SetActive(true);
            controlPanel.SetActive(false);

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

[Back to Top](#Photon-Unity-Network-Tutorial)

</div>