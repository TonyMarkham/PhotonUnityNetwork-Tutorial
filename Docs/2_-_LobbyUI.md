# PhotonUnityNetwork Tutorial

## 2. Lobby UI

This Part will focus on creating the User Interface (UI) for the lobby. It's going to stay very basic as it's not really related to Networking per se.

## **Contents**

* [The Play Button](#The-Play-Button)
* [The Player Name](#The-Player-Name)
    * [Creating The PlayerNameInputField](#Creating-The-PlayerNameInputField)
    * [Creating The UI for the Player's Name](#Creating-The-UI-for-the-Player's-Name)
* [The Connection Progress](#The-Connection-Progress)
* [Full Source Code](#Full-Source-Code)
  * [Launcher.cs](#Launcher.cs)
  * [PlayerNameInputField.cs](#PlayerNameInputField.cs)

## **The Play Button**

Currently our lobby is automatically connecting us to a room, that was good for the early testing, but really we want to let the user choose if and when to start playing. So we'll simply provide a Button for that.

1. Open the scene Launcher.
2. Create a UI Button using the Unity menu `GameObject/UI/Button`, name that button `Play Button`.
    * Notice it created a `Canvas` and an `EventSystem` GameObject in the Scene Hierarchy, so we don't have to, nice :)
3. Edit the child Text value of the `Play Button` to `Play`.
4. Select `Play Button` and locate the `On Click ()` section inside the `Button Component`.
5. Click in the small `+` to add a entry.
6. Drag the `Launcher` GameObject from the Hierarchy into the Field.
7. Select in the drop down menu `Launcher.Connect()`.  We have now connected the `Button` with our `Launcher` Script, so that when the user presses that Button, it will call the method `Connect()` from our `Launcher` Script.
8. Open the `Launcher` script.
9. In the `Launcher` script, go to the Start() method and remove the `Connect()` line.
10. Save the Script Launcher and save the Scene as well.

If you hit play now, notice you won't be connecting until you hit the Button.

## **The Player Name**

One other important minimal requirement for typical games is to let users input their names, so that other players know who they are playing against. We'll add a twist to this simple task, by using `PlayerPrefs` to remember the value of the Name so that when the user open the game again, we can recover what was the name. It's a very handy and quite important feature to implement in many areas of your game for a great User Experience.

Let's first create the script that will manage and remember the Player's name and then create the related UI.

### **Creating The PlayerNameInputField**

1. Create a new `C# Script`, call it `PlayerNameInputField`.
2. Here is the full content of it. Edit and save `PlayerNameInputField` script accordingly:

<details>
    <summary>
        Click to see 'PlayerNameInputField.cs'
    </summary>
    <p>

```c#
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

namespace com.unity.photon
{
    /// <summary>
    /// Player name input field. Let the user input his name, will appear above the player in the game.
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class PlayerNameInputField : MonoBehaviour
    {
        #region Private Constants

        // Store the PlayerPref Key to avoid typos
        const string playerNamePrefKey = "PlayerName";

        #endregion


        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            string defaultName = string.Empty;
            InputField _inputField = this.GetComponent<InputField>();
            if (_inputField != null)
            {
                if (PlayerPrefs.HasKey(playerNamePrefKey))
                {
                    defaultName = PlayerPrefs.GetString(playerNamePrefKey);
                    _inputField.text = defaultName;
                }
            }
            PhotonNetwork.NickName = defaultName;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Sets the name of the player, and save it in the PlayerPrefs for future sessions.
        /// </summary>
        /// <param name="value">The name of the Player</param>
        public void SetPlayerName(string value)
        {
            // #Important
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogError("Player Name is null or empty");
                return;
            }
            PhotonNetwork.NickName = value;
            PlayerPrefs.SetString(playerNamePrefKey, value);
        }

        #endregion
    }
}
```

</p>
</details>

#### **Let's analyze this script:**

* **RequireComponent(typeof(TMP_InputField))**
  * We first make sure that this script enforce the `TMP_InputField` because we need this, it's a very convenient and fast way to guarantee trouble free usage of this script.
* **`PlayerPrefs.HasKey()`, `PlayerPrefs.GetString()` and `PlayerPrefs.SetString()`**
  * `PlayerPrefs` is a simple lookup list of paired entries (like an excel sheet with two columns), one is the `key`, one is the `Value`. The `Key` is a string, and is totally arbitrary, you decide how to name and you will need to stick to it throughout the development. Because of that, it make sense to always store your `PlayerPrefs` Keys in one place only, a convenient way is to use a `[Static|` variable declaration, because it won't change over time during the game and is the same everytime. One could go all the way and declare it `const`, but this is something you'll get into as you gain more and more experience with C#, this is just teasing with C# scope of possibilities here.
  
    So, the logic is very straight forward. If the PlayerPrefs has a given key, we can get it and inject that value directly when we start the feature, in our case we fill up the InputField with this when we start up, and during editing, we set the PlayerPref Key with the current value of the InputField, and we are then sure it's been stored locally on the user device for later retrieval (the next time the user will open this game).

* **`PhotonNetwork.NickName`**
  * This is main point of this script, setting up the name of the player over the network. The script uses this in two places, once during `Start()` after having check if the name was stored in the `PlayerPrefs`, and inside the public method `SetPlayerName()`. Right now, nothing is calling this method, we need to bind the `InputField` `OnValueChange()` to call `SetPlayerName()` so that every time the user is editing the `InputField`, we record it. We could do this only when the user is pressing play, this is up to you, however this is a bit more involving script wise, so let's keep it simple for the sake of clarity. It also means that no matter what the user will do, the input will be remembered, which is often the desired behavior.

### **Creating The UI for the Player's Name**

1. Make sure you are still in the `Launcher` scene.
2. Create a `UI` `InputField` using the Unity menu `GameObject > UI > InputField - TextMeshPro`', name that GameObject `NameInputField`.
3. Set the `PosY` value within the `RectTransform` to `35` so it sits above the Play Button.
4. Locate the `PlaceHolder` child of Name `InputField` and set it's `Text` Value to `Enter your Name...`.
5. Select the `NameInputField` GameObject.
6. Add the `PlayerNameInputField` Script we've just created to it.
7. Locate the `On Value Change (String)` section inside the `InputField` Component.
8. Click in the small `+` to add a entry.
9. Drag the `PlayerNameInputField` component into the field.
10. Select in the dropdown menu the `PlayerNameInputField.SetPlayerName` under `Dynamic String` section
11. Save the Scene

Now you can hit play, input your name, and stop playing, hit play again, the input that you've entered just showed up.

We are getting there, however in terms of User Experience we're missing feedback on the connection progress as well as when something goes wrong during connection and when joining rooms.

## **The Connection Progress**

We are going to keep it simple here and hide away the name field and play button and replace it with a simple text "Connecting..." during connection, and switch it back when needed.

To do so, we are going to group the `Play Button` and `NameIUnputField` so that we simply activate and deactivate that group. Later on more feature can be added to the group and it will not affect our logic.

1. Make sure you are still in the `Launcher` scene.
2. Create a `UI` `Panel` using the Unity menu `GameObject > UI > Panel`, name that GameObject `ControlPanel`.
3. Delete the `Image` and `Canvas Renderer` component from the `ControlPanel`, we don't need any visuals for this panel, we only care about its content.
4. drag and drop the `PlayButton` and `NameInputField` onto the Control Panel.
5. Create a `UI` `Text` using the Unity menu `GameObject > UI > Text - TextMeshPro`, name that GameObject `ProgressLabel` don't worry about it interfering visually, we'll activate/deactivate them accordingly at runtime.
6. Select the `Text` Component of `ProgressLabel`.
7. Set Alignment to be center align and middle align.
8. Set Text value to `Connecting...`.
9. Set Color to white or whatever stands out from the background.
10. Save the Scene

At this point, for testing, you can simply enable/disable `ControlPanel` and `ProgressLabel` to see how things will look during the various connection phases. Let's now edit the scripts to control these two GameObjects activation.

1. Edit the `Launcher` script.
2. Add the two following properties within the `Public Fields` region.

    ```c#
    [Tooltip("The UI Panel to let the user enter name, connect and play")]
    [SerializeField]
    private GameObject controlPanel;

    [Tooltip("The UI Label to inform the user that the connection is in progress")]
    [SerializeField]
    private GameObject progressLabel;
    ```

3. Add the following to the `Start()` method:

    ```c#
    progressLabel.SetActive(false);
    controlPanel.SetActive(true);
    ```

4. Add the following to the beginning of `Connect()`:

    ```c#
    progressLabel.SetActive(true);
    controlPanel.SetActive(false);
    ```

5. Add the following to the beginning of `OnDisconnected()`:

    ```c#
    progressLabel.SetActive(false);
    controlPanel.SetActive(true);
    ```

6. Add the following to the beginning of `OnJoinedRoom()`:

    ```c#
    progressLabel.SetActive(false);
    controlPanel.SetActive(false);
    ```

7. Save the `Launcher` script and wait for Unity to finish compiling.
8. Make sure you are still in the `Launcher` scene.
9. Select the `Launcher` GameObject in the Hierarchy.
10. Drag and Drop from the Hierarchy Control Panel and Progress Label to their respective Field within the Launcher Component.
11. Save the Scene.

Now, if you play the scene, you'll be presented with just the Control Panel, visible and as soon as you click Play, the Progres Label will be shown.

For now, we're good for the lobby part. In order to further add features to the lobby, we need to switch to the Game itself, and create the various scenes so that we can finally load the right level when we join a room. We'll do that in the next sections and after that, we'll finally finish off the lobby system.

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

            // #Critical
            // The first thing we try to do is to join a potential existing room.
            // If there is, good, else, we'll be called back with OnJoinRandomFailed()
            PhotonNetwork.JoinRandomRoom();
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

### **PlayerNameInputField.cs**

<details>
    <summary>
        Click to see 'PlayerNameInputField.cs'
    </summary>
    <p>

```c#
using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using TMPro;

namespace com.unity.photon
{
    /// <summary>
    /// Player name input field. Let the user input his name, will appear above the player in the game.
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public class PlayerNameInputField : MonoBehaviour
    {
        #region Private Constants

        // Store the PlayerPref Key to avoid typos
        const string playerNamePrefKey = "PlayerName";

        #endregion


        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            string defaultName = string.Empty;
            InputField _inputField = this.GetComponent<InputField>();
            if (_inputField != null)
            {
                if (PlayerPrefs.HasKey(playerNamePrefKey))
                {
                    defaultName = PlayerPrefs.GetString(playerNamePrefKey);
                    _inputField.text = defaultName;
                }
            }
            PhotonNetwork.NickName = defaultName;
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Sets the name of the player, and save it in the PlayerPrefs for future sessions.
        /// </summary>
        /// <param name="value">The name of the Player</param>
        public void SetPlayerName(string value)
        {
            // #Important
            if (string.IsNullOrEmpty(value))
            {
                Debug.LogError("Player Name is null or empty");
                return;
            }
            PhotonNetwork.NickName = value;
            PlayerPrefs.SetString(playerNamePrefKey, value);
        }

        #endregion
    }
}
```

</p>
</details>
