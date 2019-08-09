# Photon Unity Network Tutorial

## 9. Player UI Prefab

This section will guide you to create the `Player UI` system. We'll need to show the name of the player and its current health. We'll also need to manage the UI position to follow the players around.

This section has nothing to do with networking per se. However, it raises some very important design patterns, to provide some advanced features revolving around networking and the constraints it introduces in development.

So, the UI is not going to be networked, simply because we don't need to, plenty of other way to go about this and avoid taking up traffic. It's always something to strive for, if you can get away with a feature not to be networked, it's good.

The legitimate question now would be: how can we have an UI for each networked player?

We'll have an UI Prefab with a dedicated `PlayerUI` script. Our `PlayerManager` script will hold a reference of this UI Prefab, and will simply instantiate this UI Prefab when the `PlayerManager` starts, and tells the prefab to follow that very player.

## **Contents**

* [Creating the UI Prefab](#Creating-the-UI-Prefab)
* [The PlayerUI Script Basics](#The-PlayerUI-Script-Basics)
* [Instantiating and Binding with the Player](#Instantiating-and-Binding-with-the-Player)
  * [Binding PlayerUI with the Player](#Binding-PlayerUI-with-the-Player)
  * [Instantiating](#Instantiating)
  * [Parenting to UI Canvas](#Parenting-to-UI-Canvas)
* [Following the Target Player](#Following-the-Target-Player)

## **Creating the UI Prefab**

1. Open any Scene, where you have an `UI Canvas`.
2. Add a `Slider UI` GameObject to the `canvas`, name it `PlayerUI`.
3. Set the `Rect Transform` vertical anchor to Middle, and the Horizontal anchor to center.
4. Set the `Rect Transform` width to `80`, and the height to `15`.
5. Select the `Background` child, set it's `Image` component `color` to `Red`.
6. Select the child `Fill Area > Fill`, set it's `Image` `color` to `green`.
7. Add a `Text UI` GameObject as a child of `PlayerUI`, name it `PlayerNameText`.
8. Add a `CanvasGroup` Component to `Player UI`.
9. Set the `Interactable` and `Blocks Raycast` property to `false` on that `CanvasGroup` Component.
10. Drag `PlayerUI` from the hierarchy into your `Prefab` Folder in your Assets, you know have a prefab.
11. Delete the instance in the scene, we don't need it anymore.

## **The PlayerUI Script Basics**

1. Create a new C# script, and call it `PlayerUI`.
2. Here's the basic script structure, edit and save `PlayerUI` script accordingly:

    ```c#
    using UnityEngine;
    using UnityEngine.UI;

    namespace com.unity.photon
    {
        public class PlayerUI : MonoBehaviour
        {
            #region Private Fields

            [Tooltip("UI Text to display Player's Name")]
            [SerializeField]
            private Text playerNameText;

            [Tooltip("UI Slider to display Player's Health")]
            [SerializeField]
            private Slider playerHealthSlider;

            #endregion


            #region MonoBehaviour Callbacks



            #endregion


            #region Public Methods



            #endregion
        }
    }
    ```

3. Save the `PlayerUI` Script.

Now let's create the Prefab itself.

1. Add `PlayerUI` script to the Prefab `PlayerUI`.
2. Drag and drop the child GameObject `PlayerNameText` into the public field `PlayerNameText`.
3. Drag and drop the `Slider` Component into the public field `PlayerHealthSlider`.

## **Instantiating and Binding with the Player**

### **Binding PlayerUI with the Player**

The `PlayerUI` script will need to know which player it represents for one reason amongst others: being able to show its health and name, let's create a public method for this binding to be possible.

1. Open the script `PlayerUI`.
2. Add a private property in the `Private Fields` Region:

    ```c#
    private PlayerManager target;
    ```

    We need to think ahead here, we'll be looking up for the health regularly, so it make sense to cache a reference of the PlayerManager for efficiency.

3. Add this public method in the `Public Methods` region:

    ```c#
    public void SetTarget(PlayerManager _target)
    {
        if (_target == null)
        {
            Debug.LogError("<Color=Red><a>Missing</a></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
            return;
        }
        // Cache references for efficiency
        target = _target;
        if (playerNameText != null)
        {
            playerNameText.text = target.photonView.Owner.NickName;
        }
    }
    ```

4. Add this method in the `MonoBehaviour Callbacks` Region:

    ```c#
    void Update()
    {
        // Reflect the Player Health
        if (playerHealthSlider != null)
        {
            playerHealthSlider.value = target.Health;
        }
    }
    ```

5. Save the PlayerUI script

With this, we have the UI to show the targeted player's name and health.

### **Instantiating**

OK, so we know already how we want to instantiate this prefab, every time we instantiate a player prefab. The best way to do this is inside the PlayerManager during its initialization.

1. Open the script `PlayerManager`.
2. Add a public field to hold a reference to the `PlayerUI` prefab as follows:

    ```c#
    [Tooltip("The Player's UI GameObject Prefab")]
    [SerializeField]
    public GameObject PlayerUiPrefab;
    ```

3. Add this code inside the `Start()` method:

    ```c#
    if (PlayerUiPrefab != null)
    {
        GameObject _uiGo =  Instantiate(PlayerUiPrefab);
        _uiGo.SendMessage ("SetTarget", this, SendMessageOptions.RequireReceiver);
    }
    else
    {
        Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
    }
    ```

4. Save the `PlayerManager` script.

All of this is standard Unity coding. However notice that we are sending a message to the instance we've just created. We require a receiver, which means we will be alerted if the `SetTarget` did not find a component to respond to it. Another way would have been to get the `PlayerUI` component from the instance, and then call `SetTarget` directly. It's generally recommended to use components directly, but it's also good to know you can achieve the same thing in various ways.

However this is far from being sufficient, we need to deal with the deletion of the player, we certainly don't want to have orphan UI instances all over the scene, so we need to destroy the UI instance when it finds out that the target it's been assigned is gone.

1. Open `PlayerUI` script.
2. Add this to the `Update()` function:

    ```c#
    // Destroy itself if the target is null, It's a fail safe when Photon is destroying Instances of a Player over the network
    if (target == null)
    {
        Destroy(this.gameObject);
        return;
    }
    ```

3. Save `PlayerUI` script.

    This code, while easy, is actually quite handy. Because of the way Photon deletes Instances that are networked, it's easier for the UI instance to simply destroy itself if the target reference is null. This avoids a lot of potential problems, and is very secure, no matter the reason why a target is missing, the related UI will automatically destroy itself too, very handy and quick. But wait... when a new level is loaded, the UI is being destroyed yet our player remains... so we need to instantiate it as well when we know a level was loaded, let's do this:

4. Open the script `PlayerManager`.
5. Add this code inside the `CalledOnLevelWasLoaded()` method:

    ```c#
    void CalledOnLevelWasLoaded(int level)
    {
        // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
        if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
        {
            transform.position = new Vector3(0f, 5f, 0f);
        }

        GameObject _uiGo = Instantiate(this.playerUiPrefab);
        _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
    }
    ```

6. Save the `PlayerManager` Script.

Note that there are more complex/powerful ways to deal with this and the UI could be made out with a singleton, but it would quickly become complex, because other players joining and leaving the room would need to deal with their UI as well. In our implementation, this is straight forward, at the cost of a duplication of where we instantiate our UI prefab. As a simple exercise, you can create a private method that would instantiate and send the `SetTarget` message, and from the various places, call that method instead of duplicating the code.

### **Parenting to UI Canvas**

One very important constraint with the Unity UI system is that any UI element must be placed within a `Canvas` GameObject, and so we need to handle this when this `PlayerUI` Prefab will be instantiated, we'll do this during the initialization of the `PlayerUI` script.

1. Open the script `PlayerUI`.
2. Add this method inside the `MonoBehaviour Callbacks` region:

    ```c#
    void Awake()
    {
        this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
    }
    ```

3. Save the `PlayerUI` Script.

    Why going brute force and find the `Canvas` this way?

    Because when scenes are going to be loaded and unloaded, so is our Prefab, and the `Canvas` will be everytime different. To avoid more complex code structure, we'll go for the quickest way. However it's really not recommended to use `Find`, because this is a slow operation. This is out of scope for this tutorial to implement a more complex handling of such case, but a good exercise when you'll feel comfortable with Unity and scripting to find ways into coding a better management of the reference of the Canvas element that takes loading and unloading into account.

## **Following the Target Player**

That's an interesting part, we need to have the `PlayerUI` following on screen the player target. This means several small issues to solve:

* The UI is a 2D element, and the player is a 3D object. How can we match positions in this case?
* We don't want the UI to be slight above the player, how can we offset on screen from the Player position?

1. Open `PlayerUI` script.
2. Add this public property inside the `Public Fields` region:

    ```c#
    [Tooltip("Pixel offset from the player target")]
    [SerializeField]
    private Vector3 screenOffset = new Vector3(0f,30f,0f);
    ```

3. Add these four fields to the `Private Fields` region:

    ```c#
    float characterControllerHeight = 0f;
    Transform targetTransform;
    Renderer targetRenderer;
    CanvasGroup _canvasGroup;
    Vector3 targetPosition;
    ```

4. Add this inside the `Awake()` Method region:

    ```c#
    _canvasGroup = this.GetComponent<CanvasGroup>();
    ```

5. Append the following code to the `SetTarget()` method after `_target` was set:

    ```c#
    targetTransform = this.target.GetComponent<Transform>();
    targetRenderer = this.target.GetComponent<Renderer>();

    CharacterController _characterController = this.target.GetComponent<CharacterController>();

    // Get data from the Player that won't change during the lifetime of this Component
    if (_characterController != null)
    {
        characterControllerHeight = _characterController.height;
    }
    ```

    We know our player to be based off a `CharacterController`, which features a `Height` property, we'll need this to do a proper offset of the UI element above the Player.

6. Add this public method in the `MonoBehaviour Callbacks` region:

    ```c#
    void LateUpdate()
    {
        // Do not show the UI if we are not visible to the camera, thus avoid potential bugs with seeing the UI, but not the player itself.
        if (targetRenderer != null)
        {
            this._canvasGroup.alpha = targetRenderer.isVisible ? 1f : 0f;
        }

        // #Critical
        // Follow the Target GameObject on screen.
        if (targetTransform != null)
        {
            targetPosition = targetTransform.position;
            targetPosition.y += characterControllerHeight;
            this.transform.position = Camera.main.WorldToScreenPoint(targetPosition) + screenOffset;
        }
    }
    ```

7. Save the `PlayerUI` Script.

So, the trick to match a 2D position with a 3D position is to use the `WorldToScreenPoint` function of a camera and since we only have one in our game, we can rely on accessing the `Main Camera` which is the default setup for a Unity Scene.

Notice how we setup the offset in several steps: first we get the actual position of the target, then we add the `characterControllerHeight`, and finally, after we've deduced the screen position of the top of the Player, we add the screen offset.

## **Full Source Code**

### **PlayerUI.cs**

<details>
    <summary>
        Click to see 'PlayerUI.cs'
    </summary>
    <p>

```c#
using UnityEngine;
using UnityEngine.UI;

namespace com.unity.photon
{
    public class PlayerUI : MonoBehaviour
    {
        #region Private Fields

        [Tooltip("UI Text to display Player's Name")]
        [SerializeField]
        private Text playerNameText;

        [Tooltip("UI Slider to display Player's Health")]
        [SerializeField]
        private Slider playerHealthSlider;

        private PlayerManager target;

        float characterControllerHeight = 0f;
        Transform targetTransform;
        Renderer targetRenderer;
        CanvasGroup _canvasGroup;
        Vector3 targetPosition;

        #endregion


        #region Public Fields

        [Tooltip("Pixel offset from the player target")]
        [SerializeField]
        private Vector3 screenOffset = new Vector3(0f, 30f, 0f);

        #endregion


        #region MonoBehaviour Callbacks

        void Awake()
        {
            this.transform.SetParent(GameObject.Find("Canvas").GetComponent<Transform>(), false);
            _canvasGroup = this.GetComponent<CanvasGroup>();
        }

        void Update()
        {
            // Destroy itself if the target is null, It's a fail safe when Photon is destroying Instances of a Player over the network
            if (target == null)
            {
                Destroy(this.gameObject);
                return;
            }

            // Reflect the Player Health
            if (playerHealthSlider != null)
            {
                playerHealthSlider.value = target.Health;
            }
        }

        void LateUpdate()
        {
            // Do not show the UI if we are not visible to the camera, thus avoid potential bugs with seeing the UI, but not the player itself.
            if (targetRenderer != null)
            {
                this._canvasGroup.alpha = targetRenderer.isVisible ? 1f : 0f;
            }

            // #Critical
            // Follow the Target GameObject on screen.
            if (targetTransform != null)
            {
                targetPosition = targetTransform.position;
                targetPosition.y += characterControllerHeight;
                this.transform.position = Camera.main.WorldToScreenPoint(targetPosition) + screenOffset;
            }
        }

        #endregion


        #region Public Methods

        public void SetTarget(PlayerManager _target)
        {
            if (_target == null)
            {
                Debug.LogError("<Color=Red><a>Missing</a></Color> PlayMakerManager target for PlayerUI.SetTarget.", this);
                return;
            }
            // Cache references for efficiency
            target = _target;

            targetTransform = this.target.GetComponent<Transform>();
            targetRenderer = this.target.GetComponent<Renderer>();

            CharacterController _characterController = this.target.GetComponent<CharacterController>();

            // Get data from the Player that won't change during the lifetime of this Component
            if (_characterController != null)
            {
                characterControllerHeight = _characterController.height;
            }

            if (playerNameText != null)
            {
                playerNameText.text = target.photonView.Owner.NickName;
            }
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

        [Tooltip("The Beams GameObject to control")]
        [SerializeField]
        private GameObject beams;

        [Tooltip("The Player's UI GameObject Prefab")]
        [SerializeField]
        private GameObject playerUiPrefab;

        //True, when the user is firing
        bool IsFiring;

        #endregion


        #region Public Fields

        [Tooltip("The current Health of our player")]
        public float Health = 1f;

        [Tooltip("The local player instance. Use this to know if the local player is represented in the Scene")]
        public static GameObject LocalPlayerInstance;

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

            if (playerUiPrefab != null)
            {
                GameObject _uiGo = Instantiate(playerUiPrefab);
                _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
            }
            else
            {
                Debug.LogWarning("<Color=Red><a>Missing</a></Color> PlayerUiPrefab reference on player Prefab.", this);
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

        /// <summary>
        /// MonoBehaviour method called after a new level of index 'level' was loaded.
        /// We recreate the Player UI because it was destroy when we switched level.
        /// Also reposition the player if outside the current arena.
        /// </summary>
        /// <param name="level">Level index loaded</param>
        void CalledOnLevelWasLoaded(int level)
        {
            // check if we are outside the Arena and if it's the case, spawn around the center of the arena in a safe zone
            if (!Physics.Raycast(transform.position, -Vector3.up, 5f))
            {
                transform.position = new Vector3(0f, 5f, 0f);
            }

            GameObject _uiGo = Instantiate(this.playerUiPrefab);
            _uiGo.SendMessage("SetTarget", this, SendMessageOptions.RequireReceiver);
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