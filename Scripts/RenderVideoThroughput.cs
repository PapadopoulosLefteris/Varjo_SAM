// using UnityEngine;
// using Varjo;

// public class VarjoVideoPassThroughController : MonoBehaviour
// {
//     private bool isRendering = false;

//     void Start()
//     {
//         // Check if Varjo Mixed Reality is available
//         if (VarjoMixedReality.IsAvailable())
//         {
//             Debug.Log("Varjo Mixed Reality is available.");
//         }
//         else
//         {
//             Debug.LogError("Varjo Mixed Reality is not available on this device.");
//         }
//     }

//     void Update()
//     {
//         // Toggle rendering with the "R" key
//         if (Input.GetKeyDown(KeyCode.R))
//         {
//             ToggleVideoPassThrough();
//         }
//     }

//     public void ToggleVideoPassThrough()
//     {
//         if (VarjoMixedReality.IsAvailable())
//         {
//             if (isRendering)
//             {
//                 VarjoMixedReality.StopRender();
//                 Debug.Log("Stopped rendering video pass-through.");
//             }
//             else
//             {
//                 VarjoMixedReality.StartRender();
//                 Debug.Log("Started rendering video pass-through.");
//             }
            
//             isRendering = !isRendering;
//         }
//         else
//         {
//             Debug.LogError("Varjo Mixed Reality is not available. Cannot toggle rendering.");
//         }
//     }
// }
