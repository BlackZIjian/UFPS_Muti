/////////////////////////////////////////////////////////////////////////////////
//
//	vp_MPLocalPlayer.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	this class represents the UFPS local player in multiplayer.
//					it extends vp_MPNetworkPlayer with all functionality specific
//					to the one-and-only local player on this machine. this includes
//					issuing fire events and all sorts of events triggered by the
//					vp_FPPlayerEventHandler (such as crouching, aiming, running and
//					reloading). it also refreshes body model materials, forwards text
//					messages to the hud and prevents input during certain multiplayer
//					game phases. 
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class vp_LocalAIPlayer : vp_MPNetworkPlayer
{
	
	// local player
	private static vp_MPLocalPlayer m_Instance = null;
	public static vp_MPLocalPlayer Instance
	{
		get
		{
			if (m_Instance == null)
			{
				m_Instance = Component.FindObjectOfType(typeof(vp_MPLocalPlayer)) as vp_MPLocalPlayer;
			}
			return m_Instance;
		}
	}


	// platform id
	int PlatformID
	{
		get
		{
			return vp_MPMaster.GetViewIDOfTransform(Player.Platform.Get());
		}
	}


	// timer handles
	static vp_Timer.Handle m_UnFreezeTimer = new vp_Timer.Handle();	// used to protect the unfreeze timer from being canceled on level load


	/// <summary>
	/// 
	/// </summary>
	public override void Awake()
	{

		base.Awake();
			
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnEnable()
	{
		if (Player != null)
			Player.Register(this);

	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual void OnDisable()
	{

		if (Player != null)
			Player.Unregister(this);

	}

	
	/// <summary>
	/// 
	/// </summary>
	public override void Start()
	{

		base.Start();

		// allow player to move by default
		UnFreeze();

	}


	/// <summary>
	/// 
	/// </summary>
	public override void Update()
	{

		base.Update();

	}


	/// <summary>
	/// updates position, rotation and velocity over the network
	/// </summary>
	public override void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {

		if (!stream.isWriting)
			return;

		if (Player == null)
			return;

		stream.SendNext((int)PlatformID);
		if (PlatformID == 0)
			stream.SendNext((Vector3)Transform.position);				// position of player
		else
			stream.SendNext((Vector3)Controller.PositionOnPlatform);	// position of player on current platform
		stream.SendNext((Vector2)new Vector2(Player.Rotation.Get().x, Transform.eulerAngles.y));	// send camera pitch + root-transform yaw
		stream.SendNext((Vector3)Player.Velocity.Get());			// direction player is moving
		stream.SendNext((Vector2)Player.InputMoveVector.Get());	// direction player is trying to move

		// DEBUG: uncomment the below line in BOTH vp_MPRemotePlayer AND vp_MPLocalPlayer ->
		// OnPhotonSerializeView to make the game detect when the weapon index reported
		// by this machine goes out of sync with the one wielded on the corresponding
		// remote player on a remote machine + fix it
		ForceSyncWeapon(stream);

	}


	/// <summary>
	/// updates weapon over the network
	/// </summary>
	protected override void ForceSyncWeapon(PhotonStream stream)
	{

		stream.SendNext((int)Player.CurrentWeaponIndex.Get());	// current weapon of player

	}

	
	/// <summary>
	/// scans this player hierarchy for shooter components, hooks up
	/// their fire seed with the 'Shots' variable, and their firing
	/// delegate with a network (RPC) call. finally, puts them in a
	/// dictionary for validation later
	/// </summary>
	public override void InitShooters()
	{
		
		vp_WeaponShooter[] shooters = gameObject.GetComponentsInChildren<vp_WeaponShooter>(true) as vp_WeaponShooter[];
		m_Shooters.Clear();
		foreach (vp_WeaponShooter f in shooters)
		{

			vp_WeaponShooter shooter = f;	// need to cache shooter or delegate will only be set with values of last weapon

			if (m_Shooters.ContainsKey(WeaponHandler.GetWeaponIndex(shooter.Weapon)))
				continue;
			
			shooter.GetFireSeed = delegate
			{
				return Shots;
			};

			shooter.m_SendFireEventToNetworkFunc = delegate()
			{
				if (!PhotonNetwork.offlineMode)
					photonView.RPC("FireWeapon", PhotonTargets.All, WeaponHandler.GetWeaponIndex(shooter.Weapon), shooter.GetFirePosition(), shooter.GetFireRotation());
				//vp_MPDebug.Log("sending RPC: " + sho.gameObject.name + ", " + sho.GetFirePosition() + ", " + sho.GetFireRotation());
			};

			m_Shooters.Add(WeaponHandler.GetWeaponIndex(shooter.Weapon), shooter);
						
		}

		//Debug.Log("Stored " + m_Shooters.Count + " local weapons.");

	}


	/// <summary>
	/// disarms, stops and locks player so that it cannot move
	/// </summary>
	public static void Freeze()
	{

		if (Instance == null)
			return;

		if (Instance.FPPlayer == null)
			return;

		Instance.FPPlayer.SetWeapon.Start(0);
		Instance.FPPlayer.Stop.Send();
		vp_Timer.In(0.1f, delegate()
		{
			Instance.FPPlayer.SetState("Freeze", true);
		});
		Instance.FPPlayer.InputAllowGameplay.Set(false);

	}


	/// <summary>
	/// allows player to move again and tries to wield the start weapon
	/// </summary>
	public static void UnFreeze()
	{

		if (Instance == null)
			return;

		if (Instance.FPPlayer == null)
			return;

		Instance.FPPlayer.SetState("Freeze", false);
		Instance.FPPlayer.InputAllowGameplay.Set(true);
		Instance.FPPlayer.SetWeapon.Stop();

		vp_Timer.In(1.0f, ()=>
		{
			if((Instance != null) && (Instance.FPPlayer != null) && (Instance.FPPlayer.SetWeapon != null))
			Instance.FPPlayer.SetWeapon.TryStart(Instance.WeaponHandler.StartWeapon);
		}, m_UnFreezeTimer);
		m_UnFreezeTimer.CancelOnLoad = false;

	}


	/// <summary>
	/// iterates the 'Shots' variable by one (in the base class).
	/// for more info, see summary of 'FireWeapon' in vp_MPNetworkPlayer
	/// </summary>
	[PunRPC]
	public override void FireWeapon(int weaponIndex, Vector3 position, Quaternion rotation, PhotonMessageInfo info)
	{

		base.FireWeapon(weaponIndex, position, rotation, info);
		
	}

	// --- UFPS player events, forwarded to corresponding remote players as RPCs ---

	/// <summary>
	/// 
	/// </summary>
	void OnStart_Crouch()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_Crouch", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Crouch()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Stop_Crouch", PhotonTargets.Others);
	}

	
	/// <summary>
	/// 
	/// </summary>
	void OnStart_Run()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_Run", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Run()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Stop_Run", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_SetWeapon()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_SetWeapon", PhotonTargets.Others, Player.SetWeapon.Argument);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_Attack()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_Attack", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Attack()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Stop_Attack", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_Zoom()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_Zoom", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Zoom()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Stop_Zoom", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_Reload()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_Reload", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Reload()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Stop_Reload", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_Climb()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_Climb", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_Climb()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Stop_Climb", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStart_OutOfControl()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Start_OutOfControl", PhotonTargets.Others);
	}


	/// <summary>
	/// 
	/// </summary>
	void OnStop_OutOfControl()
	{
		if (PhotonNetwork.offlineMode)
			return;
		photonView.RPC("Stop_OutOfControl", PhotonTargets.Others);
	}
		

	/// <summary>
	/// prevents weapon switching unless the game is on, or during first logon
	/// </summary>
	bool CanStart_SetWeapon()
	{

		return ((vp_MPMaster.Phase == vp_MPMaster.GamePhase.Playing)
			|| (vp_MPMaster.Phase == vp_MPMaster.GamePhase.NotStarted)
			);

	}


}


