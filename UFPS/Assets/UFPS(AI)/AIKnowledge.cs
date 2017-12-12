using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIKnowledge : Object
{

    public SortedList<PlayerKey,PlayerTransform> Targets;

    public AIKnowledge()
    {
        Targets = new SortedList<PlayerKey,PlayerTransform>(new PlayerKey.PlayerKeyCompare());
    }
}
