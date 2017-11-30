/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPInteractManager.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	This allows interaction with vp_Interactable components in
//					the world via input. Check for the method InputInteract()
//					in the vp_FPInput class.
//
//					NOTE: this script must be run AFTER the default execution time
//					and AFTER the script 'vp_FPBodyAnimator'. to make sure this is
//					the case, in the editor go to:
//					'Edit -> Project Settings -> Script Execution order'
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class vp_AIInteract : MonoBehaviour
{

	public float InteractDistance = 2; // sets the distance for interaction from the player
	public float InteractDistance3rdPerson = 3; // sets the distance for interaction from the player in 3rd person
	public float MaxInteractDistance = 25; 	// sets the max distance any interaction can occur. If any interactables interactDistance
	
	protected vp_PlayerEventHandler m_Player = null; // for caching our player event handler to send in the 'TryInteract' method
	protected vp_Interactable m_CurrentInteractable = null; // for caching what the player is currently interacting with
	protected Dictionary<Collider,vp_Interactable> m_Interactables = new Dictionary<Collider, vp_Interactable>(); // for testing interactable components
	protected vp_Interactable m_LastInteractable = null; // for caching the last interactable
	protected vp_Timer.Handle m_ShowTextTimer = new vp_Timer.Handle(); // to delay interactables text from showing
	protected bool m_CanInteract = false;
	

	/// <summary>
	/// 
	/// </summary>
	protected virtual void Awake()
	{
		
		m_Player = GetComponent<vp_PlayerEventHandler>(); // cache the player event handler
		
	}
	

	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected virtual void OnEnable()
	{

		// allow this monobehaviour to talk to the player event handler
		if (m_Player != null)
			m_Player.Register(this);

	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected virtual void OnDisable()
	{

		// unregister this monobehaviour from the player event handler
		if (m_Player != null)
			m_Player.Unregister(this);

	}
	
	
	/// <summary>
	/// if the player dies, stop interacting
	/// </summary>
	public virtual void OnStart_Dead()
	{
		
		ShouldFinishInteraction();
		
	}
	
	
	/// <summary>
	/// 
	/// </summary>
	public virtual void LateUpdate()
	{
		
		if(m_Player.Dead.Active)
			return;
		
	}
	

	/// <summary>
	/// adds a condition (a rule set) that must be met for the
	/// event handler 'Interact' activity to successfully activate.
	/// NOTE: other scripts may have added conditions to this
	/// activity aswell
	/// </summary>
	protected virtual bool CanStart_Interact()
	{
		
		// if we are already interacting, we need to stop interacting so a new interaction can begin.
		if(ShouldFinishInteraction())
			return false;
		
		// if the weapon is being set, don't allow interaction
		if(m_Player.SetWeapon.Active)
			return false;

		if (m_LastInteractable != null)
		{
			// if the interactable is of type vp_InteractType.Normal, carry on
			if (m_LastInteractable.InteractType != vp_Interactable.vp_InteractType.Normal)
				return false;
			
			// check if we can interact with the interactable, if so, carry on
			if (!m_LastInteractable.TryInteract(m_Player))
				return false;
			
			
			return true; // allow interaction
		}

		return false; // if all else fails, don't allow interaction
		
	}
	
	
	/// <summary>
	/// finishes interaction with the current interactable
	/// </summary>
	protected virtual bool ShouldFinishInteraction()
	{
		
		if(m_Player.Interactable.Get() != null)
		{
			m_LastInteractable = null; // set this to null to allow the crosshair to change again
			m_Player.Interactable.Get().FinishInteraction(); // end interaction with the active interactable
			m_Player.Interactable.Set(null); // set this to null to allow new interaction
			return true; // don't allow new interaction this time through
		}
			
		return false;
		
	}
	
	
	
	/// <summary>
	/// does a raycast to see if an interactable is in range
	/// and returns that interactable in the out as well as returns
	/// true if an interactable was found
	/// </summary>
	protected virtual bool FindInteractable( out vp_Interactable interactable )
	{

		interactable = null;
		
		LayerMask layerMask = ~((1 << vp_Layer.LocalPlayer) | (1 << vp_Layer.Debris) |
								(1 << vp_Layer.IgnoreRaycast) | (1 << vp_Layer.IgnoreBullets) | (1 << vp_Layer.Trigger) | (1 << vp_Layer.Water));
		Collider[] results = Physics.OverlapSphere(transform.position, MaxInteractDistance, layerMask);
		if (results != null && results.Length > 0)
		{
			Collider collider = results[0];

			if (Vector3.Dot(transform.forward, collider.transform.position - transform.position) < 0)
				return false;
			
			if(!m_Interactables.TryGetValue(collider, out interactable))
				m_Interactables.Add(collider, interactable = collider.GetComponent<vp_Interactable>());
			
			// return if no interactable
			if(interactable == null)
				return false;
		}
		else
			return false;
		
		return true;
		
	}
	
	
	
	/// <summary>
	/// this allows the player to interact with collidable objects
	/// </summary>
	protected virtual void OnControllerColliderHit(ControllerColliderHit hit)
	{

		Rigidbody body = hit.collider.attachedRigidbody;

		if (body == null || body.isKinematic)
			return;
		
		vp_Interactable interactable = null;
		if(!m_Interactables.TryGetValue(hit.collider, out interactable))
			m_Interactables.Add(hit.collider, interactable = hit.collider.GetComponent<vp_Interactable>());
		
		if(interactable == null)
			return;
		
		if(interactable.InteractType != vp_Interactable.vp_InteractType.CollisionTrigger)
			return;
			
		hit.gameObject.SendMessage("TryInteract", m_Player, SendMessageOptions.DontRequireReceiver);

	}
	
	
	/// <summary>
	/// sets and returns the current object being interacted with
	/// </summary>
	protected virtual vp_Interactable OnValue_Interactable
	{
		get { return m_CurrentInteractable; }
		set { m_CurrentInteractable = value; }
	}


	/// <summary>
	/// enables or disables the player's ability to interact
	/// </summary>
	protected virtual bool OnValue_CanInteract
	{
	
		get{ return m_CanInteract; }
		set{ m_CanInteract = value; }
	
	}
	
	
}