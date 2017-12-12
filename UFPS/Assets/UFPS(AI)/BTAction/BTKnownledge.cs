using System.Collections;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

public class BTKnownledge : Action
{

    public SharedObject Knowledge;
    
    public override void OnAwake()
    {
        base.OnAwake();
        Knowledge.SetValue(new AIKnowledge());
    }
}
