# Photon Unity Network Tutorial

## 7. Player Networking

This section will guide you to create the CameraWork script to follow along your player as you play this game.

This section has nothing to do with networking, so it will be kept short.

## **Contents**

* [PhotonView Component](#PhotonView-Component)
* [Transform Syncronization](#Transform-Syncronization)
* [Animation Syncronization](#Animation-Syncronization)
* [User Input Management](#User-Input-Management)
* [Camera Control](#Camera-Control)
* [Beam Fire Control](#Beam-Fire-Control)
* [Health Syncronization](#Health-Syncronization)

* [Full Source Code](#Full-Source-Code)
  * [PlayerAnimatorManager.cs](#PlayerAnimatorManager.cs)
  * [PlayerManager.cs](#PlayerManager.cs)

## **PhotonView Component**

First and foremost, we need to have a `PhotonView` component attached to our prefab. A `PhotonView` is what connects together the various instances on each computers, and define what components to observe and how to observe these components.

1. Add a `PhotonView` Component to `MyRobotKyle`.
2. Set the `Observe Option` to `Unreliable On Change`.
3. Notice `PhotonView` warns you that you need to observe something for this to have any effects.

Let's set up what we are going to observe, and then we'll get back to this `PhotonView` component and finish it's setup.

## **Transform Syncronization**

The obvious feature we want to synchronize is the `Position` and `Rotation` of the character so that when a player is moving around, the character behave in a similar way on other players' instances of the game.

You could manually observe the `Transform` component in your own Script, but you would run into a lot of trouble, due to network latency, and effectiveness of data being synchronized. Luckily, and to make this common task easier, we are going to use a `PhotonTransformView` component. Basically, all the hard work has been done for you with this Component.

1. Add a `PhotonTransformView` to 'My Robot Kyle' Prefab.
2. Drag the `PhotonTransformView` from its header title onto the first observable component entry on the `PhotonView` component.
3. Now, check `Synchronize Position` in `PhotonTransformView`.
4. Check `Synchronize Rotation`.

## **Animation Syncronization**

The `PhotonAnimatorView` is also making networking setup a breeze and will save you a lot of time and trouble. It allows you to define which layer weights and which parameters you want to synchronize. Layer weights only need to be synchronized if they change during the game and it might be possible to get away with not synchronizing them at all. The same is true for parameters. Sometimes it is possible to derive animator values from other factors. A speed value is a good example for this, you don't necessarily need to have this value synchronized exactly but you can use the synchronized position updates to estimate its value. If possible, try to synchronize as little parameters as you can get away with.

1. Add a `PhotonAnimatorView` to `MyRobotKyle` Prefab.
2. Drag the `PhotonAnimatorView` from its header title onto a new observable component entry in the `PhotonView` component.
3. Now, in the `Synchronized Parameters`, set `Speed` to `Discrete`.
4. Set `Direction` to `Discrete`.
5. Set `Jump` to `Discrete`.
6. Set `Hi` to `Disabled`.

Each value can be `disabled`, or `synchronized` either `discretely` or `continuously`. In our case since we are not using the `Hi` parameter, we'll disable it and save traffic.

`Discrete` synchronization means that a value gets sent 10 times a second (in `OnPhotonSerializeView`) by default. The receiving clients pass the value on to their local `Animator`.

`Continuous` synchronization means that the `PhotonAnimatorView` runs every frame. When `OnPhotonSerializeView` is called (10 times per second by default), the values recorded since the last call are sent together. The receiving client then applies the values in sequence to retain smooth transitions. While this mode is smoother, it also sends more data to achieve this effect.

## **User Input Management**

A critical aspect of user control over the network is that the same prefab will be instantiated for all players, but only one of them represents the user actually playing in front of the computer, all other instances represents other users, playing on other computers. So the first hurdle with this in mind is `Input Management`. How can we enable input on one instance and not on others and how to know which one is the right one? Enter the `IsMine` concept.

Let's edit the `PlayerAnimatorManager` script we created earlier. In its current form, this script doesn't know about this distinction, let's implement this.

1. Open the script `PlayerAnimatorManager`.
2. Turn the `PlayerAnimatorManager` class from a `MonoBehaviour` to a `MonoBehaviourPun` which conveniently exposes the `PhotonView` component.
3. In the `Update()` call, insert at the very beginning:

    ```c#
    if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
    {
        return;
    }
    ```

4. Save the Script `PlayerAnimatorManager` Ok, `photonView.IsMine` will be true if the instance is controlled by the 'client' application, meaning this instance represents the physical person playing on this computer within this application. So if it is false, we don't want to do anything and solely rely on the `PhotonView` component to synchronize the transform and animator components we've setup earlier. But, why having then to enforce `PhotonNetwork.IsConnected == true` in our if statement? eh eh :) because during development, we may want to test this prefab without being connected. In a dummy scene for example, just to create and validate code that is not related to networking features per se. And so with this additional expression, we will allow input to be used if we are not connected. It's a very simple trick and will greatly improve your workflow during development.

## **Camera Control**

It's the same as input, the player only has one view of the game, and so we need the `CameraWork` script to only follow the local player, not the other players. That's why the `CameraWork` script has this ability to define when to follow.

Let's modify the `PlayerManager` script to control the `CameraWork` component.

1. Open the `PlayerManager` script.
2. Insert the code below in between `Awake()` and `Update()` methods.

    ```c#
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
    ```

3. Save the Script `PlayerManager`.

First, it gets the `CameraWork` component, we expect this, so if we don't find it, we log an error. Then, if `photonView.IsMine` is `true`, it means we need to follow this instance, and so we call `_cameraWork.OnStartFollowing()` which effectivly makes the camera follow that very instance in the scene.

All other player instances will have their `photonView.IsMine` set as `false`, and so their respective `_cameraWork` will do nothing.

One last change to make this work:

1. Disable `Follow on Start` on the `CameraWork` component on the prefab `MyRobotKyle`.

This effectively now hands over the logic to follow the player to the script `PlayerManager` that will call `_cameraWork.OnStartFollowing()` as described above.

## **Beam Fire Control**

Firing also follow the input principle exposed above, it needs to only be working if `photonView.IsMine` is `true`.

1. Open the Script `PlayerManager`.
2. Surround the input processing call with an if statement.

    ```c#
    if (photonView.IsMine)
    {
        ProcessInputs ();
    }
    ```

3. Save the Script `PlayerManager`.

However, when testing this, we only see the local player firing. We need to see when another instance fires, too. We need a mechanism for synchronizing the firing across the network. To do this, we are going to manually synchronize the `IsFiring` boolean value, until now, we got away with `PhotonTransformView` and `PhotonAnimatorView` to do all the internal synchronization of variables for us, we only had to tweak what was conveniently exposed to us via the Unity Inspector, but here what we need is very specific to your game, and so we'll need to do this manually.

1. Open the Script `PlayerManager`.
2. Implement `IPunObservable`.

    ```c#
    public class PlayerManager : MonoBehaviourPunCallbacks, IPunObservable
    {
        #region IPunObservable implementation


        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
        }


        #endregion
    ```

3. Inside `IPunObservable.OnPhotonSerializeView` add the following code:

    ```c#
    if (stream.IsWriting)
    {
        // We own this player: send the others our data
        stream.SendNext(IsFiring);
    }
    else
    {
        // Network player, receive data
        this.IsFiring = (bool)stream.ReceiveNext();
    }
    ```

4. Save the Script `PlayerManager`.
5. Back to Unity editor, Select the `MyRobotKyle` prefab in your assets, and add an observe entry on the `PhotonView` component and drag the `PlayerManager` component to it.

Without this last step, `IPunObservable.OnPhotonSerializeView` is never called because it is not observed by a `PhotonView`.

In this `IPunObservable.OnPhotonSerializeView` method, we get a variable stream, this is what is going to be send over the network, and this call is our chance to read and write data. We can only write when we are the local player (`photonView.IsMine == true`), else we read.

Since the stream class has helpers to know what to do, we simply rely on `stream.isWriting` to know what is expected in the current instance case.

If we are expected to write data, we append our `IsFiring` value to the stream of data, using `stream.SendNext()`, a very convenient method that hides away all the hard work of data serialization. If we are expected to read, we use `stream.ReceiveNext()`.

## **Health Syncronization**

Ok, to finish with updating player features for networking, we'll synchronize the health value so that each instance of the player will have the right health value. This is exactly the same principle as the `IsFiring` value we just covered above.

1. Open the Script `PlayerManager`.
2. Inside `IPunObservable.OnPhotonSerializeView` after you `SendNext` and `ReceiveNext` the `IsFiring` variable, do the same for `Health`:

    ```c#
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
    ```

3. Save the Script `PlayerManager`.

That's all it takes in this scenario for synchronizing the Health variable.

## **Full Source Code**

### **PlayerAnimatorManager.cs**

<details>
    <summary>
        Click to see 'PlayerAnimatorManager.cs'
    </summary>
    <p>

```c#
using Photon.Pun;
using UnityEngine;

namespace com.unity.photon
{
    public class PlayerAnimatorManager : MonoBehaviourPun
    {
        #region Private Fields

        [SerializeField]
        private float directionDampTime = .25f;
        private Animator animator;

        #endregion


        #region MonoBehaviour CallBacks

        // Use this for initialization
        void Start()
        {
            animator = GetComponent<Animator>();
            if (!animator)
            {
                Debug.LogError("PlayerAnimatorManager is Missing Animator Component", this);
            }

        }

        // Update is called once per frame
        void Update()
        {
            if (photonView.IsMine == false && PhotonNetwork.IsConnected == true)
            {
                return;
            }
            if (!animator)
            {
                return;
            }
            // deal with Jumping
            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            // only allow jumping if we are running.
            if (stateInfo.IsName("Base Layer.Run"))
            {
                // When using trigger parameter
                if (Input.GetButtonDown("Fire2"))
                {
                    animator.SetTrigger("Jump");
                }
            }
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            if (v < 0)
            {
                v = 0;
            }
            animator.SetFloat("Speed", h * h + v * v);
            animator.SetFloat("Direction", h, directionDampTime, Time.deltaTime);
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