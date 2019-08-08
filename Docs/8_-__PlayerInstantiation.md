# Photon Unity Network Tutorial

## 8. Player Instantiation

This section will cover "Player" prefab instantiation over the network and implement the various features needed to accommodate automatic scenes switches while playing.

## **Contents**

* [Instantiating the Player](#Instantiating-the-Player)
* [Keeping Track of the Player Instance](#Keeping-Track-of-the-Player-Instance)
* [Manage Player Position when Outside the Arena](#Manage-Player-Position-when-Outside-the-Arena)

## **Instantiating the Player**

It's actually very easy to instantiate our `Player` prefab. We need to instantiate it when we've just entered the room, and we can rely on the `GameManager` Script `Start()` Message which will indicated we've loaded the Arena, which means by our design that we are in a room.

1. Open `GameManager` Script.
2. In the `Public Fields` region add the following variable:

    ```c#
    [Tooltip("The prefab to use for representing the player")]
    public GameObject playerPrefab;
    ```

3. In the `Start()` method, add the following:

    ```c#
    if (playerPrefab == null)
    {
        Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'",this);
    }
    else
    {
        Debug.LogFormat("We are Instantiating LocalPlayer from {0}", Application.loadedLevelName);
        // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
        PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f,5f,0f), Quaternion.identity, 0);
    }
    ```

4. Save `GameManager` Script.

This exposes a public field for you to reference the `Player` prefab. It's convenient, because in this particular case we can drag and drop directly in the `GameManager` prefab, instead of in each scene, because the `Player` prefab is an asset, and so the reference will be kept intact (as opposed to referencing a GameObject in the hierarchy, which a prefab can only do when instantiated in the same scene).

***WARNING:*** Always make sure prefabs that are supposed to be instantiated over the network are within a Resources folder, this is a Photon requirement.

Then, in `Start()`, we instantiate it (after having checked that we have a `Player` prefab referenced properly).

Notice that we instantiate well above the floor (5 units above while the player is only 2 units high). This is one way amongst many other to prevent collisions when new players join the room, players could be already moving around the center of the arena, and so it avoids abrupt collisions. A "falling" player is also a nice and clean indication and introduction of a new entity in the game.

However, this is not enough for our case, we have a twist :) When other players will join in, different scenes will be loaded, and we want to keep consistency and not destroy existing players just because one of them left. So we need to tell Unity to not destroy the instance we created, which in turn implies we need to now check if instantiation is required when a scene is loaded.

## **Keeping Track of the Player Instance**

1. Open `PlayerManager` script.
2. In the `Public Fields` Region, add the following:

    ```c#
    [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
    public static GameObject LocalPlayerInstance;
    ```

3. In the `Awake()` method, add the following:

    ```c#
    // #Important
    // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
    if (photonView.IsMine)
    {
        PlayerManager.LocalPlayerInstance = this.gameObject;
    }
    // #Critical
    // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
    DontDestroyOnLoad(this.gameObject);
    ```
4. Save `PlayerManager` script.

With these modifications, we can then implement the check to only instantiate if necessary inside the GameManager script.

1. Open `GameManager` Script.
2. Surround the instantiation call with an if condition:

    ```c#
    if (PlayerManager.LocalPlayerInstance == null)
    {
        Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
        // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
        PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
    }
    else
    {
        Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
    }
    ```

3. Save `GameManager` Script.

With this, we now only instantiate if the `PlayerManager` doesn't have a reference to an existing instance of `localPlayer`.

## **Manage Player Position when Outside the Arena**

We have one more thing to watch out for. The size of the Arena is changing based on the number of players, which means that there is a case where if one player leaves and the other players are near the border of the current arena size, they will simply find themselves outside the smaller arena when it will load, we need to take this into account, and simply reposition the player back to the center of the arena in this case. It's something that is an issue in your gameplay and level design specifically.

There is currently an added complexity because Unity has revamped `Scene Management` and Unity 5.4 has deprecated some callbacks, which makes it slightly more complex to create a code that works across all Unity versions (from Unity 5.3.7 to the latest). So we'll need different code based on the Unity version. It's unrelated to Photon Unity Networking, but important to master for your projects to survive updates.

1. Open `PlayerManager` script.
2. Add a new method inside `Private Methods` region:

    ```c#
    #if UNITY_5_4_OR_NEWER
    void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode loadingMode)
    {
        this.CalledOnLevelWasLoaded(scene.buildIndex);
    }
    #endif
    ```

3. At the end of the `Start()` method, add the following code:

    ```c#
    #if UNITY_5_4_OR_NEWER
    // Unity 5.4 has a new scene management. register a method to call CalledOnLevelWasLoaded.
    UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    #endif
    ```

4. Add the following two methods inside the `MonoBehaviour Callbacks` region:

    ```c#
    #if !UNITY_5_4_OR_NEWER
    /// <summary>See CalledOnLevelWasLoaded. Outdated in Unity 5.4.</summary>
    void OnLevelWasLoaded(int level)
    {
        this.CalledOnLevelWasLoaded(level);
    }
    #endif

    void CalledOnLevelWasLoaded(int level)
    {
        // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
        if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
        {
            transform.position = new Vector3(0f, 5f, 0f);
        }
    }
    ```

5. Override `OnDisable` method as follows:

    ```c#
    #if UNITY_5_4_OR_NEWER
    public override void OnDisable()
    {
        // Always call the base to remove callbacks
        base.OnDisable ();
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    #endif
    ```

6. Save `PlayerManager` script.

What this new code does is watching for a level being loaded, and raycast downwards the current player's position to see if we hit anything. If we don't, this is means we are not above the arena's ground and we need to be repositioned back to the center, exactly like when we are entering the room for the first time.

If you are on a Unity version lower than Unity 5.4, we'll use Unity's callback `OnLevelWasLoaded`. If you are on Unity 5.4 or up, `OnLevelWasLoaded` is not available anymore, instead you have to use the new `SceneManagement` system. Finally, to avoid duplicating code, we simply have a `CalledOnLevelWasLoaded` method that will be called either from `OnLevelWasLoaded` or from the `SceneManager.sceneLoaded` callback.

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
        #region Public Fields

        public static GameManager Instance;

        [Tooltip("The prefab to use for representing the player")]
        public GameObject playerPrefab;

        #endregion


        #region MonoBehaviour CallBacks

        void Start()
        {
            Instance = this;

            if (playerPrefab == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> playerPrefab Reference. Please set it up in GameObject 'Game Manager'", this);
            }
            else
            {
                if (PlayerManager.LocalPlayerInstance == null)
                {
                    Debug.LogFormat("We are Instantiating LocalPlayer from {0}", SceneManagerHelper.ActiveSceneName);
                    // we're in a room. spawn a character for the local player. it gets synced by using PhotonNetwork.Instantiate
                    PhotonNetwork.Instantiate(this.playerPrefab.name, new Vector3(0f, 5f, 0f), Quaternion.identity, 0);
                }
                else
                {
                    Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
                }
            }
        }

        #endregion


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

### **PlayerManager.cs**

<details>
    <summary>
        Click to see 'PlayerManager.cs'
    </summary>
    <p>

```c#
using Photon.Pun;
using UnityEngine;

namespace com.unity.photon
{
    /// <summary>
    /// Player manager.
    /// Handles fire Input and Beams.
    /// </summary>
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {
        #region Private Fields

        [Tooltip("The current Health of our player")]
        public float Health = 1f;

        [Tooltip("The Beams GameObject to control")]
        [SerializeField]
        private GameObject beams;

        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;

        //True, when the user is firing
        bool IsFiring;

        #endregion


        #region MonoBehaviour CallBacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during early initialization phase.
        /// </summary>
        void Awake()
        {
            if (beams == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> Beams Reference.", this);
            }
            else
            {
                beams.SetActive(false);
            }

            // #Important
            // used in GameManager.cs: we keep track of the localPlayer instance to prevent instantiation when levels are synchronized
            if (photonView.IsMine)
            {
                PlayerManager.LocalPlayerInstance = this.gameObject;
            }
            // #Critical
            // we flag as don't destroy on load so that instance survives level synchronization, thus giving a seamless experience when levels load.
            DontDestroyOnLoad(this.gameObject);
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase.
        /// </summary>
        void Start()
        {
            CameraWork _cameraWork = this.gameObject.GetComponent<CameraWork>();

            if (_cameraWork != null)
            {
                if (photonView.IsMine)
                {
                    _cameraWork.OnStartFollowing();
                }
            }
            else
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> CameraWork Component on playerPrefab.", this);
            }
        }

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity on every frame.
        /// </summary>
        void Update()
        {
            if (photonView.IsMine)
            {
                this.ProcessInputs();

                if (this.Health <= 0f)
                {
                    GameManager.Instance.LeaveRoom();
                }
            }

            // trigger Beams active state
            if (beams != null && IsFiring != beams.activeSelf)
            {
                beams.SetActive(IsFiring);
            }
        }

        /// <summary>
        /// MonoBehaviour method called when the Collider 'other' enters the trigger.
        /// Affect Health of the Player if the collider is a beam
        /// Note: when jumping and firing at the same, you'll find that the player's own beam intersects with itself
        /// One could move the collider further away to prevent this or check if the beam belongs to the player.
        /// </summary>
        void OnTriggerEnter(Collider other)
        {
            if (!photonView.IsMine)
            {
                return;
            }
            // We are only interested in Beamers
            // we should be using tags but for the sake of distribution, let's simply check by name.
            if (!other.name.Contains("Beam"))
            {
                return;
            }
            Health -= 0.1f;
        }
        /// <summary>
        /// MonoBehaviour method called once per frame for every Collider 'other' that is touching the trigger.
        /// We're going to affect health while the beams are touching the player
        /// </summary>
        /// <param name="other">Other.</param>
        void OnTriggerStay(Collider other)
        {
            // we dont' do anything if we are not the local player.
            if (!photonView.IsMine)
            {
                return;
            }
            // We are only interested in Beamers
            // we should be using tags but for the sake of distribution, let's simply check by name.
            if (!other.name.Contains("Beam"))
            {
                return;
            }
            // we slowly affect health when beam is constantly hitting us, so player has to move to prevent death.
            Health -= 0.1f * Time.deltaTime;
        }

        #endregion


        #region IPunObservable implementation


        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                // We own this player: send the others our data
                stream.SendNext(IsFiring);
                stream.SendNext(Health);
            }
            else
            {
                // Network player, receive data
                this.IsFiring = (bool)stream.ReceiveNext();
                this.Health = (float)stream.ReceiveNext();
            }
        }


        #endregion


        #region Custom

        /// <summary>
        /// Processes the inputs. Maintain a flag representing when the user is pressing Fire.
        /// </summary>
        void ProcessInputs()
        {
            if (Input.GetButtonDown("Fire1"))
            {
                if (!IsFiring)
                {
                    IsFiring = true;
                }
            }
            if (Input.GetButtonUp("Fire1"))
            {
                if (IsFiring)
                {
                    IsFiring = false;
                }
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