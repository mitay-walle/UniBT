using UnityEditor;
using UnityEngine;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace UniBT.Editor
{
	[CustomEditor(typeof(BehaviorTree), true)]
#if ODIN_INSPECTOR
	public class BehaviorTreeEditor : OdinEditor
#else
	public class BehaviorTreeEditor : UnityEditor.Editor
#endif
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Open Behavior Tree"))
			{
				var bt = target as BehaviorTree;
				BehaviourTreeGraphWindow.Show(bt);
			}
		}
	}
}