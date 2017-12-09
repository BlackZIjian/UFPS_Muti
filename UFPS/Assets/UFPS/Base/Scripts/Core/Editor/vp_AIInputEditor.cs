﻿/////////////////////////////////////////////////////////////////////////////////
//
//	vp_FPInputEditor.cs
//	© Opsive. All Rights Reserved.
//	https://twitter.com/Opsive
//	http://www.opsive.com
//
//	description:	custom inspector for the vp_FPInput class
//
/////////////////////////////////////////////////////////////////////////////////

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(vp_AIInput))]

public class vp_AIInputEditor : Editor
{

	// target component
	public vp_AIInput m_Component = null;

	// input foldouts
	// NOTE: these are static so they remain open when toggling
	// between different components. this simplifies copying
	// content (prefabs / sounds) between components
	public static bool m_StateFoldout;
	public static bool m_PresetFoldout = true;

	private static vp_ComponentPersister m_Persister = null;

	
	/// <summary>
	/// hooks up the FPSCamera object to the inspector target
	/// </summary>
	public void OnEnable()
	{

		m_Component = (vp_AIInput)target;

		if (m_Persister == null)
			m_Persister = new vp_ComponentPersister();
		m_Persister.Component = m_Component;
		m_Persister.IsActive = true;

		if (m_Component.DefaultState == null)
			m_Component.RefreshDefaultState();


	}


	/// <summary>
	/// disables the persister and removes its reference
	/// </summary>
	void OnDestroy()
	{

		if (m_Persister != null)
			m_Persister.IsActive = false;

	}

	
	/// <summary>
	/// 
	/// </summary>
	public override void OnInspectorGUI()
	{

		if (Application.isPlaying || m_Component.DefaultState.TextAsset == null)
		{

		}
		else
			vp_PresetEditorGUIUtility.DefaultStateOverrideMessage();

		// state foldout
		m_StateFoldout = vp_PresetEditorGUIUtility.StateFoldout(m_StateFoldout, m_Component, m_Component.States, m_Persister);

		// preset foldout
		m_PresetFoldout = vp_PresetEditorGUIUtility.PresetFoldout(m_PresetFoldout, m_Component);

		// update default state and persist in order not to loose inspector tweaks
		// due to state switches during runtime - UNLESS a runtime state button has
		// been pressed (in which case user wants to toggle states as opposed to
		// reset / alter them)
		if (GUI.changed &&
			(!vp_PresetEditorGUIUtility.RunTimeStateButtonTarget == m_Component))
		{

			EditorUtility.SetDirty(target);

			if (Application.isPlaying)
				m_Component.RefreshDefaultState();

			if (m_Component.Persist)
				m_Persister.Persist();
	
			m_Component.Refresh();

		}

		m_Component.SetWeapon = EditorGUILayout.IntSlider("StartWeapon", m_Component.SetWeapon, 1, 10);

	}



	



}

