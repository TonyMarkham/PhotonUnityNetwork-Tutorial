# Photon Unity Network Tutorial

## 6. Player Camera Work

This section will guide you to create the CameraWork script to follow along your player as you play this game.

This section has nothing to do with networking, so it will be kept short.

## **Contents**

* [Creating the CameraWork Script](#Creating-the-CameraWork-Script)

* [Full Source Code](#Full-Source-Code)
  * [GameManager.cs](#GameManager.cs)

## **Creating the CameraWork Script**

1. Create a new C# script called `CameraWork`.
2. Replace the the content of `CameraWork` with the following:

    ```c#
    using UnityEngine;

    namespace com.unity.photon
    {
        /// <summary>
        /// Camera work. Follow a target
        /// </summary>
        public class CameraWork : MonoBehaviour
        {
            #region Private Fields

            [Tooltip("The distance in the local x-z plane to the target")]
            [SerializeField]
            private float distance = 7.0f;

            [Tooltip("The height we want the camera to be above the target")]
            [SerializeField]
            private float height = 3.0f;

            [Tooltip("The Smooth time lag for the height of the camera.")]
            [SerializeField]
            private float heightSmoothLag = 0.3f;

            [Tooltip("Allow the camera to be offseted vertically from the target, for example giving more view of the sceneray and less ground.")]
            [SerializeField]
            private Vector3 centerOffset = Vector3.zero;

            [Tooltip("Set this as false if a component of a prefab being instanciated by Photon Network, and manually call OnStartFollowing() when and if needed.")]
            [SerializeField]
            private bool followOnStart = false;

            // cached transform of the target
            Transform cameraTransform;

            // maintain a flag internally to reconnect if target is lost or camera is switched
            bool isFollowing;

            // Represents the current velocity, this value is modified by SmoothDamp() every time you call it.
            private float heightVelocity;

            // Represents the position we are trying to reach using SmoothDamp()
            private float targetHeight = 100000.0f;

            #endregion


            #region MonoBehaviour Callbacks

            /// <summary>
            /// MonoBehaviour method called on GameObject by Unity during initialization phase
            /// </summary>
            void Start()
            {
                // Start following the target if wanted.
                if (followOnStart)
                {
                    OnStartFollowing();
                }
            }

            /// <summary>
            /// MonoBehaviour method called after all Update functions have been called. This is useful to order script execution. For example a follow camera should always be implemented in LateUpdate because it tracks objects that might have moved inside Update.
            /// </summary>
            void LateUpdate()
            {
                // The transform target may not destroy on level load,
                // so we need to cover corner cases where the Main Camera is different everytime we load a new scene, and reconnect when that happens
                if (cameraTransform == null && isFollowing)
                {
                    OnStartFollowing();
                }
                // only follow is explicitly declared
                if (isFollowing)
                {
                    Apply();
                }
            }

            #endregion


            #region Public Methods

            /// <summary>
            /// Raises the start following event.
            /// Use this when you don't know at the time of editing what to follow, typically instances managed by the photon network.
            /// </summary>
            public void OnStartFollowing()
            {
                cameraTransform = Camera.main.transform;
                isFollowing = true;
                // we don't smooth anything, we go straight to the right camera shot
                Cut();
            }

            #endregion


            #region Private Methods

            /// <summary>
            /// Follow the target smoothly
            /// </summary>
            void Apply()
            {
                Vector3 targetCenter = transform.position + centerOffset;
                // Calculate the current & target rotation angles
                float originalTargetAngle = transform.eulerAngles.y;
                float currentAngle = cameraTransform.eulerAngles.y;
                // Adjust real target angle when camera is locked
                float targetAngle = originalTargetAngle;
                currentAngle = targetAngle;
                targetHeight = targetCenter.y + height;

                // Damp the height
                float currentHeight = cameraTransform.position.y;
                currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, heightSmoothLag);
                // Convert the angle into a rotation, by which we then reposition the camera
                Quaternion currentRotation = Quaternion.Euler(0, currentAngle, 0);
                // Set the position of the camera on the x-z plane to:
                // distance meters behind the target
                cameraTransform.position = targetCenter;
                cameraTransform.position += currentRotation * Vector3.back * distance;
                // Set the height of the camera
                cameraTransform.position = new Vector3(cameraTransform.position.x, currentHeight, cameraTransform.position.z);
                // Always look at the target
                SetUpRotation(targetCenter);
            }

            /// <summary>
            /// Directly position the camera to a the specified Target and center.
            /// </summary>
            void Cut()
            {
                float oldHeightSmooth = heightSmoothLag;
                heightSmoothLag = 0.001f;
                Apply();
                heightSmoothLag = oldHeightSmooth;
            }

            /// <summary>
            /// Sets up the rotation of the camera to always be behind the target
            /// </summary>
            /// <param name="centerPos">Center position.</param>
            void SetUpRotation(Vector3 centerPos)
            {
                Vector3 cameraPos = cameraTransform.position;
                Vector3 offsetToCenter = centerPos - cameraPos;
                // Generate base rotation only around y-axis
                Quaternion yRotation = Quaternion.LookRotation(new Vector3(offsetToCenter.x, 0, offsetToCenter.z));
                Vector3 relativeOffset = Vector3.forward * distance + Vector3.down * height;
                cameraTransform.rotation = yRotation * Quaternion.LookRotation(relativeOffset);
            }

            #endregion
        }
    }
    ```

3. Save the Script `CameraWork`.

The maths behind following the player is tricky if you've just started with real time 3d, Vectors and Quaternion based mathematics. So I'll spare you with trying to explains this within this tutorial. However if you are curious and want to learn, do not hesitate to contact us, We'll do our best to explain it.

However, this script is not just about crazy mathematics, something important is also set up; the ability to control when the behaviour should actively follow the player. And this is important to understand: why would we want to control when to follow a player.

Typically, let's imagine what would happen if it was always following the player. As you connect to a room full of players, each CameraWork script on the other players' instances would fight to control the "Main Camera" so that it looks at its player... Well we don't want that, we want to only follow the local player that represents the user behind the computer.

Once we've defined the problem that we have only one camera but several player instances, we can easily find several ways to go about it.

1. Only attach the `CameraWork` script on the local player.
2. Control the `CameraWork` behaviour by turning it off and on depending on whether the player it has to follow is the local player or not.
3. Have the `CameraWork` attached to the `Camera` and watch out for when there is a local player instance in the scene and follow that one only.

These 3 options are not exhaustive, many more ways can be found, but out of these 3, we'll arbitrary pick the second one. None of the above options are better or worse, however this is the one that possibly requires the least amount of coding and is the most flexible... "interesting..." I hear you say :)

* We've exposed a field `followOnStart` which we can set to true if we want to use this on a non networked environment, for example in our test scene, or in completely different scenarios.
* When running in our networked based game, we'll call the public method `OnStartFollowing()` when we detect that the player is a local player. This will be done in the script `PlayerManager` that is created and explained in the chapter Player Prefab Networking.

## **Full Source Code**

### **CameraWork.cs**

<details>
    <summary>
        Click to see 'CameraWork.cs'
    </summary>
    <p>

```c#
using UnityEngine;

namespace com.unity.photon
{
    /// <summary>
    /// Camera work. Follow a target
    /// </summary>
    public class CameraWork : MonoBehaviour
    {
        #region Private Fields

        [Tooltip("The distance in the local x-z plane to the target")]
        [SerializeField]
        private float distance = 7.0f;

        [Tooltip("The height we want the camera to be above the target")]
        [SerializeField]
        private float height = 3.0f;

        [Tooltip("The Smooth time lag for the height of the camera.")]
        [SerializeField]
        private float heightSmoothLag = 0.3f;

        [Tooltip("Allow the camera to be offseted vertically from the target, for example giving more view of the sceneray and less ground.")]
        [SerializeField]
        private Vector3 centerOffset = Vector3.zero;

        [Tooltip("Set this as false if a component of a prefab being instanciated by Photon Network, and manually call OnStartFollowing() when and if needed.")]
        [SerializeField]
        private bool followOnStart = false;

        // cached transform of the target
        Transform cameraTransform;

        // maintain a flag internally to reconnect if target is lost or camera is switched
        bool isFollowing;

        // Represents the current velocity, this value is modified by SmoothDamp() every time you call it.
        private float heightVelocity;

        // Represents the position we are trying to reach using SmoothDamp()
        private float targetHeight = 100000.0f;

        #endregion


        #region MonoBehaviour Callbacks

        /// <summary>
        /// MonoBehaviour method called on GameObject by Unity during initialization phase
        /// </summary>
        void Start()
        {
            // Start following the target if wanted.
            if (followOnStart)
            {
                OnStartFollowing();
            }
        }

        /// <summary>
        /// MonoBehaviour method called after all Update functions have been called. This is useful to order script execution. For example a follow camera should always be implemented in LateUpdate because it tracks objects that might have moved inside Update.
        /// </summary>
        void LateUpdate()
        {
            // The transform target may not destroy on level load,
            // so we need to cover corner cases where the Main Camera is different everytime we load a new scene, and reconnect when that happens
            if (cameraTransform == null && isFollowing)
            {
                OnStartFollowing();
            }
            // only follow is explicitly declared
            if (isFollowing)
            {
                Apply();
            }
        }

        #endregion


        #region Public Methods

        /// <summary>
        /// Raises the start following event.
        /// Use this when you don't know at the time of editing what to follow, typically instances managed by the photon network.
        /// </summary>
        public void OnStartFollowing()
        {
            cameraTransform = Camera.main.transform;
            isFollowing = true;
            // we don't smooth anything, we go straight to the right camera shot
            Cut();
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Follow the target smoothly
        /// </summary>
        void Apply()
        {
            Vector3 targetCenter = transform.position + centerOffset;
            // Calculate the current & target rotation angles
            float originalTargetAngle = transform.eulerAngles.y;
            float currentAngle = cameraTransform.eulerAngles.y;
            // Adjust real target angle when camera is locked
            float targetAngle = originalTargetAngle;
            currentAngle = targetAngle;
            targetHeight = targetCenter.y + height;

            // Damp the height
            float currentHeight = cameraTransform.position.y;
            currentHeight = Mathf.SmoothDamp(currentHeight, targetHeight, ref heightVelocity, heightSmoothLag);
            // Convert the angle into a rotation, by which we then reposition the camera
            Quaternion currentRotation = Quaternion.Euler(0, currentAngle, 0);
            // Set the position of the camera on the x-z plane to:
            // distance meters behind the target
            cameraTransform.position = targetCenter;
            cameraTransform.position += currentRotation * Vector3.back * distance;
            // Set the height of the camera
            cameraTransform.position = new Vector3(cameraTransform.position.x, currentHeight, cameraTransform.position.z);
            // Always look at the target
            SetUpRotation(targetCenter);
        }

        /// <summary>
        /// Directly position the camera to a the specified Target and center.
        /// </summary>
        void Cut()
        {
            float oldHeightSmooth = heightSmoothLag;
            heightSmoothLag = 0.001f;
            Apply();
            heightSmoothLag = oldHeightSmooth;
        }

        /// <summary>
        /// Sets up the rotation of the camera to always be behind the target
        /// </summary>
        /// <param name="centerPos">Center position.</param>
        void SetUpRotation(Vector3 centerPos)
        {
            Vector3 cameraPos = cameraTransform.position;
            Vector3 offsetToCenter = centerPos - cameraPos;
            // Generate base rotation only around y-axis
            Quaternion yRotation = Quaternion.LookRotation(new Vector3(offsetToCenter.x, 0, offsetToCenter.z));
            Vector3 relativeOffset = Vector3.forward * distance + Vector3.down * height;
            cameraTransform.rotation = yRotation * Quaternion.LookRotation(relativeOffset);
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