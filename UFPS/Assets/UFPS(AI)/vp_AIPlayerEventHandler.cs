using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_5_4_OR_NEWER
using UnityEngine.SceneManagement;
#endif

public class vp_AIPlayerEventHandler : vp_PlayerEventHandler {
// input
    public vp_Value<Vector2> InputSmoothLook;
    public vp_Value<Vector2> InputRawLook;
    public vp_Message<string, bool> InputGetButton;
    public vp_Message<string, bool> InputGetButtonUp;
    public vp_Message<string, bool> InputGetButtonDown;
    public vp_Value<bool> InputAllowGameplay;
    public vp_Value<bool> Pause;
    
    // old inventory system
    // TIP: these events can be removed along with the old inventory system
    public vp_Value<string> CurrentWeaponClipType;
    public vp_Attempt<object> AddAmmo;
    public vp_Attempt RemoveClip;
}
