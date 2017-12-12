using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class TurnToTarget : Action
{

    public SharedFloat MouseX;
    public SharedFloat MouseY;

    public SharedObject Knowledge;

    public SharedGameObject AICamera;

    private Camera _camera;

    private AIKnowledge mAiKnowledge;

    private float moveRate = 2.5f;

    public override void OnAwake()
    {
        base.OnAwake();
        _camera = AICamera.Value.GetComponent<Camera>();
    }

    public override void OnStart()
    {
        base.OnStart();
        if (Knowledge != null)
        {
            mAiKnowledge = Knowledge.Value as AIKnowledge;
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (mAiKnowledge.Targets == null || mAiKnowledge.Targets.Count <= 0)
            return TaskStatus.Success;

        Transform targetTansform = mAiKnowledge.Targets.Values[0].Head.transform;
        Vector3 viewportPos = _camera.WorldToViewportPoint(targetTansform.transform.position + new Vector3(0,0.5f,0));
        float mouseX = (int) ((viewportPos.x - 0.5f) / 0.001f) * moveRate * 0.015f;
        float mouseY = (int) ((viewportPos.y - 0.5f) / 0.001f) * moveRate * 0.015f;
        if (Vector3.Dot(targetTansform.position - _camera.transform.position, _camera.transform.forward) < 0)
        {
            mouseX = 1;
            mouseY = 0;
        }
        mouseX = Mathf.Clamp(mouseX, -1, 1);
        mouseY = Mathf.Clamp(mouseY, -1, 1);
        MouseX.SetValue(mouseX);
        MouseY.SetValue(mouseY);
        return TaskStatus.Success;
    }
}
