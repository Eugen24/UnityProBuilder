using System;
using UnityEditor;
using UnityEngine;

[Serializable]
public class NestedClass : ScriptableObject
{
	[SerializeField]
	private float m_StructFloat;

	public void OnEnable() { hideFlags = HideFlags.HideAndDontSave; }

	public void OnGUI()
	{
		m_StructFloat = EditorGUILayout.FloatField("Float", m_StructFloat);
	}
}

[Serializable]
public class SerializeMe : EditorWindow
{
	[SerializeField]
	private NestedClass m_Class1;

	[SerializeField]
	private NestedClass m_Class2;

	public SerializeMe()
	{
		m_Class1 = (NestedClass)ScriptableObject.CreateInstance("NestedClass");
		m_Class2 = m_Class1;
	}

	// Method to open the window
	[MenuItem("Window/MyEditorWindow")]
	static void OpenWindow()
	{
		GetWindow<SerializeMe>();
	}

	public void OnGUI()
	{
		m_Class1.OnGUI();
		m_Class2.OnGUI();
	}
}