using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class AIView : Action {
    
    
    public SharedObject Knowledge;
    
    public SharedGameObject AICamera;

    private AIKnowledge mAiKnowledge;

    private Camera _camera;

    private string _id;

    public override void OnAwake()
    {
        base.OnAwake();
        _camera = AICamera.Value.GetComponent<Camera>();
        _id = GetComponent<PlayerTransform>().Id;
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
        mAiKnowledge.Targets.Clear();
        
        foreach (var pair in PlayerTransform.AllPlayer)
        {
            if (pair.Key.Id == _id)
            {
                continue;
            }
            pair.Key.distance = Vector3.Distance(transform.position, pair.Value.Body.transform.position);
            Vector3 pos = _camera.WorldToViewportPoint(pair.Value.transform.position);

            float dot = Vector3.Dot(pair.Value.Body.transform.position - _camera.transform.position, _camera.transform.forward);
            if (pos.x >= 0 && pos.y >= 0 && pos.x <= 1 && pos.y <= 1 && dot >= 0 && pair.Key.distance < _camera.farClipPlane)
            {
                pair.Key.layer = 1;
            }
            else
            {
                pair.Key.layer = 0;
            }
            mAiKnowledge.Targets[pair.Key] = pair.Value;
        }

        Knowledge.SetValue(mAiKnowledge);
        
        return TaskStatus.Success;
    }
}
