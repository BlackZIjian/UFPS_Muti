using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerKey
{
	public string Id;

	public PlayerKey(string id)
	{
		Id = id;
	}

	public int layer;

	public float distance;
	
	public class PlayerKeyCompare : IComparer<PlayerKey>
	{
		public int Compare(PlayerKey x, PlayerKey y)
		{
			if (x.layer > y.layer)
				return -1;
			else if (x.layer < y.layer)
				return 1;
			else
			{
				if (x.distance > y.distance)
					return 1;
				else if (x.distance < y.distance)
					return -1;
				else
					return 0;
			}

		}
	}
}
public class PlayerTransform : MonoBehaviour
{

	private static Dictionary<PlayerKey, PlayerTransform> _allPlayer;

	public static Dictionary<PlayerKey, PlayerTransform> AllPlayer
	{
		get
		{
			if(_allPlayer == null)
				_allPlayer = new Dictionary<PlayerKey, PlayerTransform>();
			return _allPlayer;
		}
	}

	private PlayerKey _key;
	
	private string _id;
	
	public string Id
	{
		get { return _id; }
	}

	public Transform Head;

	private void OnEnable()
	{
		_id = Guid.NewGuid().ToString();
		_key = new PlayerKey(_id);
		AllPlayer.Add(_key, this);
	}

	private void OnDisable()
	{
		if (AllPlayer.ContainsKey(_key))
		{
			AllPlayer.Remove(_key);
		}
	}

	// Update is called once per frame
	void Update () {
		
	}
	
	
}
