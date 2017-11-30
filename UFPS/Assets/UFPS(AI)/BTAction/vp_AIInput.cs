/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPInput.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	a script to collect all mouse and keyboard input for first person
//					controls in one place
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using System.Collections.Generic;

public class vp_AIInput : vp_Component
{

	// mouse look
	public Vector2 MouseLookSensitivity = new Vector2(5.0f, 5.0f);
	public bool MouseLookMutePitch = false;             // use this to make the 'InputSmoothLook' and 'InputRawLook' events always return zero pitch / yaw , regardless of sensitivity
	public bool MouseLookMuteYaw = false;				// -		"	-
	public int MouseLookSmoothSteps = 10;				// allowed range: 1-20
	public float MouseLookSmoothWeight = 0.5f;			// allowed range: 0.0f - 1.0f
	public bool MouseLookAcceleration = false;
	public float MouseLookAccelerationThreshold = 0.4f;
	public bool MouseLookInvert = false;
	protected Vector2 m_MouseLookSmoothMove = Vector2.zero;		// distance moved since last frame (smoothed and accelerated)
	protected Vector2 m_MouseLookRawMove = Vector2.zero;		// distance moved since last frame (raw unity input)
	protected List<Vector2> m_MouseLookSmoothBuffer = new List<Vector2>();
	protected int m_LastMouseLookFrame = -1;
	protected Vector2 m_CurrentMouseLook = Vector2.zero;

	// mouse cursor
	public Rect[] MouseCursorZones = null;			// screen regions where mouse arrow remains visible when clicking. may be set up in the Inspector
	// NOTE: these do not currently get saved to presets (!)
	public bool MouseCursorForced = false;			// when true, the mouse arrow is enabled all over the screen and firing is disabled
	public bool MouseCursorBlocksMouseLook = true;	// if true, mouselook will be disabled while the mouse arrow is visible
	public Vector2 MousePos { get { return m_MousePos; } }
	protected Vector2 m_MousePos = Vector2.zero;	// current mouse position in GUI coordinates (Y flipped)

	// move vector
	protected Vector2 m_MoveVector = Vector2.zero;

	protected vp_PlayerEventHandler m_Player = null;
	public vp_PlayerEventHandler Player
	{
		get
		{
			if (m_Player == null)
				m_Player = transform.root.GetComponentInChildren<vp_PlayerEventHandler>();
			return m_Player;
		}
	}


	/// <summary>
	/// registers this component with the event handler (if any)
	/// </summary>
	protected override void OnEnable()
	{

		if (Player != null)
			Player.Register(this);

		SetWeapon = 1;
	}


	/// <summary>
	/// unregisters this component from the event handler (if any)
	/// </summary>
	protected override void OnDisable()
	{

		if (Player != null)
			Player.Unregister(this);

	}

	/// <summary>
	/// 
	/// </summary>
	protected override void Update()
	{
		// interaction
		InputInteract();

		// manage input for moving
		InputMove();
		InputRun();
		InputJump();
		InputCrouch();

		// manage input for weapons
		InputAttack();
		InputReload();
		InputSetWeapon();

		// manage camera related input
		InputCamera();

	}


	public bool DownInteract = false;
	
	/// <summary>
	/// handles interaction with the game world
	/// </summary>
	protected virtual void InputInteract()
	{

		if (DownInteract)
			Player.Interact.TryStart();
		else
			Player.Interact.TryStop();

		DownInteract = false;
	}

	public float HorizontalAxis = 0;
	
	
	public float VerticalAxis = 0;


	/// <summary>
	/// move the player forward, backward, left and right
	/// </summary>
	protected virtual void InputMove()
	{

		// NOTE: you could also use 'GetAxis', but that would add smoothing
		// to the input from both UFPS and from Unity, and might require some
		// tweaking in order not to feel laggy

		Player.InputMoveVector.Set(new Vector2(HorizontalAxis, VerticalAxis));

		HorizontalAxis = 0;
		VerticalAxis = 0;
	}


	public bool IsRun = false;

	/// <summary>
	/// tell the player to enable or disable the 'Run' state.
	/// NOTE: since running is a state, it's not sent to the
	/// controller code (which doesn't know the state names).
	/// instead, the player class is responsible for feeding the
	/// 'Run' state to every affected component.
	/// </summary>
	protected virtual void InputRun()
	{

		if (IsRun)
			Player.Run.TryStart();
		else
			Player.Run.TryStop();

		IsRun = false;
	}


	public bool IsJump;

	/// <summary>
	/// ask controller to jump when button is pressed (the current
	/// controller preset determines jump force).
	/// NOTE: if its 'MotorJumpForceHold' is non-zero, this
	/// also makes the controller accumulate jump force until
	/// button release.
	/// </summary>
	protected virtual void InputJump()
	{

		// TIP: to find out what determines if 'Jump.TryStart'
		// succeeds and where it is hooked up, search the project
		// for 'CanStart_Jump'

		if (IsJump)
			Player.Jump.TryStart();
		else
			Player.Jump.Stop();

		IsJump = false;

	}


	public bool IsCrouch;

	/// <summary>
	/// asks the controller to halve the height of its Unity
	/// CharacterController collision capsule while player is
	/// holding the crouch modifier key. this activity will also
	/// typically trigger states on the camera and weapons. note
	/// that getting up again won't always succeed (for example
	/// the player might be crawling through a ventilation shaft
	/// or hiding under a table).
	/// </summary>
	protected virtual void InputCrouch()
	{

		// IMPORTANT: using the 'Crouch' activity for crouching
		// ensures that CharacterController (collision) height is only
		// updated when needed. this is important because changing its
		// height every frame will make trigger detection break!

		if (IsCrouch)
			Player.Crouch.TryStart();
		else
			Player.Crouch.TryStop();

		IsCrouch = false;
		// TIP: to find out what determines if 'Crouch.TryStop'
		// succeeds and where it is hooked up, search the project
		// for 'CanStop_Crouch'

	}


	public bool IsZoom;

	/// <summary>
	/// camera related input
	/// </summary>
	protected virtual void InputCamera()
	{

		// zoom / ADS
		if (IsZoom)
			Player.Zoom.TryStart();
		else
			Player.Zoom.TryStop();

		IsZoom = false;
	}


	public bool IsAttack;
	
	/// <summary>
	/// broadcasts a message to any listening components telling
	/// them to go into 'attack' mode. vp_FPWeaponShooter uses this
	/// to repeatedly fire the current weapon while the fire button
	/// is being pressed, but it could also be used by, for example,
	/// an animation script to make the player model loop an 'attack
	/// stance' animation.
	/// </summary>
	protected virtual void InputAttack()
	{

		// TIP: you could do this to prevent player from attacking while running
		//if (Player.Run.Active)
		//	return;

		if (IsAttack)
			Player.Attack.TryStart();
		else
			Player.Attack.TryStop();

		IsAttack = false;
	}

	public bool IsReload;

	/// <summary>
	/// when the reload button is pressed, broadcasts a message
	/// to any listening components asking them to reload
	/// NOTE: reload may not succeed due to ammo status etc.
	/// </summary>
	protected virtual void InputReload()
	{

		if (IsReload)
			Player.Reload.TryStart();

		IsReload = false;
	}


	/// <summary>
	/// -1:Prev,-2:Next,0:Clear,-3:nothing
	/// </summary>
	public int SetWeapon = 1;
	
	/// <summary>
	/// handles cycling through carried weapons, wielding specific
	/// ones and clearing the current one
	/// </summary>
	protected virtual void InputSetWeapon()
	{
		// --- cycle to the next or previous weapon ---
		
		if (SetWeapon == -1)
			Player.SetPrevWeapon.Try();

		if (SetWeapon == -2)
			Player.SetNextWeapon.Try();

		// --- switch to weapon 1-10 by direct button press ---

		if (SetWeapon == 1) Player.SetWeapon.TryStart(1);
		if (SetWeapon == 2) Player.SetWeapon.TryStart(2);
		if (SetWeapon == 3) Player.SetWeapon.TryStart(3);
		if (SetWeapon == 4) Player.SetWeapon.TryStart(4);
		if (SetWeapon == 5) Player.SetWeapon.TryStart(5);
		if (SetWeapon == 6) Player.SetWeapon.TryStart(6);
		if (SetWeapon == 7) Player.SetWeapon.TryStart(7);
		if (SetWeapon == 8) Player.SetWeapon.TryStart(8);
		if (SetWeapon == 9) Player.SetWeapon.TryStart(9);
		if (SetWeapon == 10) Player.SetWeapon.TryStart(10);

		// --- unwield current weapon by direct button press ---

		if (SetWeapon == 0) Player.SetWeapon.TryStart(0);

	}

	public float MouseX;

	public float MouseY;

	/// <summary>
	/// mouselook implementation with smooth filtering and acceleration
	/// </summary>
	protected virtual Vector2 GetMouseLook()
	{
		// only recalculate mouselook once per frame or smoothing will break
		if (m_LastMouseLookFrame == Time.frameCount)
			return m_CurrentMouseLook;

// NOTE: this directive addresses an issue with bluetooth gamepads
// when developing for GearVR. please report if it causes any trouble
#if (!UNITY_ANDROID || (UNITY_ANDROID && UNITY_EDITOR))


		// only recalculate mouselook once per frame or smoothing will break
		if (m_LastMouseLookFrame == Time.frameCount)
			return m_CurrentMouseLook;

#endif


		m_LastMouseLookFrame = Time.frameCount;

		// --- fetch mouse input ---

		m_MouseLookSmoothMove.x = MouseX * Time.timeScale;
		m_MouseLookSmoothMove.y = MouseY * Time.timeScale;

		// --- mouse smoothing ---

		// make sure the defined smoothing vars are within range
		MouseLookSmoothSteps = Mathf.Clamp(MouseLookSmoothSteps, 1, 20);
		MouseLookSmoothWeight = Mathf.Clamp01(MouseLookSmoothWeight);

		// keep mousebuffer at a maximum of (MouseSmoothSteps + 1) values
		while (m_MouseLookSmoothBuffer.Count > MouseLookSmoothSteps)
			m_MouseLookSmoothBuffer.RemoveAt(0);

		// add current input to mouse input buffer
		m_MouseLookSmoothBuffer.Add(m_MouseLookSmoothMove);

		// calculate mouse smoothing
		float weight = 1;
		Vector2 average = Vector2.zero;
		float averageTotal = 0.0f;
		for (int i = m_MouseLookSmoothBuffer.Count - 1; i > 0; i--)
		{
			average += m_MouseLookSmoothBuffer[i] * weight;
			averageTotal += (1.0f * weight);
			weight *= (MouseLookSmoothWeight / Delta);
		}

		// store the averaged input value
		averageTotal = Mathf.Max(1, averageTotal);
		m_CurrentMouseLook = vp_MathUtility.NaNSafeVector2(average / averageTotal);

		// --- mouse acceleration ---

		float mouseAcceleration = 0.0f;

		float accX = Mathf.Abs(m_CurrentMouseLook.x);
		float accY = Mathf.Abs(m_CurrentMouseLook.y);

		if (MouseLookAcceleration)
		{
			mouseAcceleration = Mathf.Sqrt((accX * accX) + (accY * accY)) / Delta;
			mouseAcceleration = (mouseAcceleration <= MouseLookAccelerationThreshold) ? 0.0f : mouseAcceleration;
		}

		m_CurrentMouseLook.x *= (MouseLookSensitivity.x + mouseAcceleration);
		m_CurrentMouseLook.y *= (MouseLookSensitivity.y + mouseAcceleration);

		m_CurrentMouseLook.y = (MouseLookInvert ? m_CurrentMouseLook.y : -m_CurrentMouseLook.y);

		return m_CurrentMouseLook;

	}


	public float MouseXRaw;

	public float MouseYRaw;
	
	/// <summary>
	/// returns the difference in raw (un-smoothed) mouse input
	/// since last frame
	/// </summary>
	protected virtual Vector2 GetMouseLookRaw()
	{

// TEST: this directive addresses an issue with bluetooth gamepads.
// please report if it causes any trouble
#if ((!UNITY_ANDROID && !UNITY_IOS) || (UNITY_ANDROID && UNITY_EDITOR) || (UNITY_IOS && UNITY_EDITOR))


#endif

		m_MouseLookRawMove.x = MouseXRaw;
		m_MouseLookRawMove.y = MouseYRaw;

		return m_MouseLookRawMove;

	}


	/// <summary>
	/// returns the current horizontal and vertical input vector depending
	/// on the current platform and / or input control type
	/// </summary>
	protected virtual Vector2 OnValue_InputMoveVector
	{
		get { return m_MoveVector; }
		// these platforms always use analog movement
#if UNITY_IOS || UNITY_ANDROID || UNITY_PS3 || UNITY_PS4 || UNITY_XBOXONE
		set	{	m_MoveVector = ((value.sqrMagnitude > 1) ? value.normalized : value);	}
#else
		// platform supports either analog or digital movement
		set
		{
			switch (vp_Input.Instance.ControlType)
			{
				case 0:	// digital (keyboard)
					m_MoveVector = ((value != Vector2.zero) ? value.normalized : value);
					break;
				case 1:	// analog (joystick)
					m_MoveVector = ((value.sqrMagnitude > 1) ? value.normalized : value);
					break;
			}
		}
#endif
	}


	/// <summary>
	/// move vector for climbing. this event callback always returns
	/// a value (unlike 'InputMoveVector' which gets disabled during
	/// climbing)
	/// </summary>
	protected virtual float OnValue_InputClimbVector
	{
		get
		{
			return VerticalAxis;
		}
	}

	public Dictionary<string, bool> GetButton = new Dictionary<string, bool>();
	public Dictionary<string, bool> GetButtonUp = new Dictionary<string, bool>();
	public Dictionary<string, bool> GetButtonDown = new Dictionary<string, bool>();

	/// <summary>
	/// 
	/// </summary>
	protected virtual bool OnMessage_InputGetButton(string button)
	{
		bool result = GetButton.ContainsKey(button) && GetButton[button];
		if (GetButton.ContainsKey(button))
			GetButton[button] = false;
		return result;
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual bool OnMessage_InputGetButtonUp(string button)
	{
		bool result = GetButtonUp.ContainsKey(button) && GetButtonUp[button];
		if (GetButtonUp.ContainsKey(button))
			GetButtonUp[button] = false;
		return result;
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual bool OnMessage_InputGetButtonDown(string button)
	{
		bool result = GetButtonDown.ContainsKey(button) && GetButtonDown[button];
		if (GetButtonDown.ContainsKey(button))
			GetButtonDown[button] = false;
		return result;
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual Vector2 OnValue_InputSmoothLook
	{
		get
		{
			Vector2 ml = GetMouseLook();
			ml.x *= (MouseLookMuteYaw ? 0 : 1);
			ml.y *= (MouseLookMutePitch ? 0 : 1);
			return ml;
		}
	}


	/// <summary>
	/// 
	/// </summary>
	protected virtual Vector2 OnValue_InputRawLook
	{
		get
		{
			Vector2 ml = GetMouseLookRaw();
			ml.x *= (MouseLookMuteYaw ? 0 : 1);
			ml.y *= (MouseLookMutePitch ? 0 : 1);
			return ml;
		}
	}


}

