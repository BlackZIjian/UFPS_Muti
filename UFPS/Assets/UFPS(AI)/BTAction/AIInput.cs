using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;


public enum InputType
{
    Interact,
    Move,
    Run,
    Jump,
    Crouch,
    Attack,
    Reload,
    SetWeapon,
    Camera,
    MouseLook,
    MouseRawLook,
    Button,
    ButtonUp,
    ButtonDown
}
public class AIInput : Action
{

    public InputType MInputType;

    /// <summary>
    /// Move:Horizontal,SetWeapons:-1:Prev,-2:Next,0:Clear,-3:nothing
    /// </summary>
    public SharedFloat value1;

    /// <summary>
    /// Move:Vertical
    /// </summary>
    public SharedFloat value2;

    /// <summary>
    /// Button:button
    /// </summary>
    public SharedString value3;

    private vp_AIInput mAIInput;

    public override void OnAwake()
    {
        base.OnAwake();
        mAIInput = Owner.GetComponent<vp_AIInput>();
    }

    public override TaskStatus OnUpdate()
    {
        if (mAIInput == null)
            return TaskStatus.Failure;
        if (MInputType == InputType.Interact)
        {
            mAIInput.DownInteract = true;
            mAIInput.GetButtonDown["Interact"] = true;
            return TaskStatus.Success;
        }

        if (MInputType == InputType.Move)
        {
            mAIInput.HorizontalAxis = value1.Value;
            mAIInput.VerticalAxis = value2.Value;
            return TaskStatus.Success;
        }

        if (MInputType == InputType.Run)
        {
            mAIInput.IsRun = true;
            mAIInput.GetButton["Run"] = true;
            return TaskStatus.Success;
        }

        if (MInputType == InputType.Jump)
        {
            mAIInput.IsJump = true;
            mAIInput.GetButton["Jump"] = true;
            return TaskStatus.Success;
        }
        
        if (MInputType == InputType.Crouch)
        {
            mAIInput.IsCrouch = true;
            mAIInput.GetButton["Crouch"] = true;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.Camera)
        {
            mAIInput.IsZoom = true;
            mAIInput.GetButton["Zoom"] = true;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.Attack)
        {
            mAIInput.IsAttack = true;
            mAIInput.GetButton["Attack"] = true;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.Reload)
        {
            mAIInput.IsReload = true;
            mAIInput.GetButtonDown["Reload"] = true;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.SetWeapon)
        {
            mAIInput.SetWeapon = (int) value1.Value;
            if ((int) value1.Value == -1)
            {
                mAIInput.GetButtonDown["SetPrevWeapon"] = true;
            }
            else if((int) value1.Value == -2)
            {
                mAIInput.GetButtonDown["SetNextWeapon"] = true;
            }
            else if((int) value1.Value == 0)
            {
                mAIInput.GetButtonDown["ClearWeapon"] = true;
            }
            else if((int) value1.Value <= 10 && (int)value1.Value > 0)
            {
                mAIInput.GetButtonDown["SetWeapon" + (int) value1.Value] = true;
            }
            return TaskStatus.Success;
        }
        if (MInputType == InputType.MouseLook)
        {
            mAIInput.MouseX = value1.Value;
            mAIInput.MouseY = value2.Value;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.MouseRawLook)
        {
            mAIInput.MouseXRaw = value1.Value;
            mAIInput.MouseYRaw = value2.Value;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.Button)
        {
            mAIInput.GetButton[value3.Value] = true;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.ButtonUp)
        {
            mAIInput.GetButtonUp[value3.Value] = true;
            return TaskStatus.Success;
        }
        if (MInputType == InputType.ButtonDown)
        {
            mAIInput.GetButtonDown[value3.Value] = true;
            return TaskStatus.Success;
        }
        return TaskStatus.Failure;
    }
}
