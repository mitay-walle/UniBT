using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBT.Editor
{
	public class BehaviourTreeGraphWindow : EditorWindow
	{
		// GraphView window per GameObject
		private static readonly Dictionary<GameObject, BehaviourTreeGraphWindow> windows = new Dictionary<GameObject, BehaviourTreeGraphWindow>();

		private GameObject gameObject { get; set; }

		[SerializeField] Vector3 savedView;

		public static void Show(BehaviorTree bt)
		{
			var window = Create(bt);
			window.Show();
			window.Focus();
		}

		private static BehaviourTreeGraphWindow Create(BehaviorTree bt)
		{
			var go = bt.gameObject;
			if (windows.TryGetValue(go, out BehaviourTreeGraphWindow window1))
			{
				return window1;
			}

			var window = CreateInstance<BehaviourTreeGraphWindow>();
			StructGraphView(window, bt);
			window.titleContent = new GUIContent($"UniBT ({bt.gameObject.name})", EditorGUIUtility.IconContent("d_BlendTree Icon")?.image);
			window.gameObject = go;
			windows[go] = window;
			return window;
		}

		private static void StructGraphView(BehaviourTreeGraphWindow window, BehaviorTree behaviorTree)
		{
			window.rootVisualElement.Clear();
			var graphView = new BehaviourTreeGraphView(behaviorTree, window);
			graphView.Load();
			window.rootVisualElement.Add(window.CreateToolBar(graphView));
			window.rootVisualElement.Add(graphView);
		}

		private void OnDestroy()
		{
			if (gameObject != null && windows.ContainsKey(gameObject))
			{
				windows.Remove(gameObject);
			}
		}

		void Update()
		{
			var graphView = rootVisualElement.Q<BehaviourTreeGraphView>();

			if (graphView is { AutoLayout: true } && !IsMouseInteractionActive())
			{
				graphView.ArrangeNodesAsTree();
			}

			if (graphView != null)
			{
				savedView = new Vector3(graphView.viewTransform.position.x, graphView.viewTransform.position.y, graphView.viewTransform.scale.x);
			}

			return;

			bool IsMouseInteractionActive()
			{
				return graphView.panel?.GetCapturingElement(PointerId.mousePointerId) != null;
			}
		}

		private void OnPlayModeStateChanged(PlayModeStateChange playModeStateChange)
		{
			switch (playModeStateChange)
			{
				case PlayModeStateChange.EnteredEditMode:
					Reload();
					break;

				case PlayModeStateChange.ExitingEditMode:
					break;

				case PlayModeStateChange.EnteredPlayMode:
					Reload();
					break;

				case PlayModeStateChange.ExitingPlayMode:
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(playModeStateChange), playModeStateChange, null);
			}
		}

		private void OnEnable()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			Reload();
		}

		private void OnDisable()
		{
			EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
		}

		private void Reload()
		{
			if (gameObject != null)
			{
				StructGraphView(this, gameObject.GetComponent<BehaviorTree>());
				rootVisualElement.Q<BehaviourTreeGraphView>().UpdateViewTransform(new Vector3(savedView.x, savedView.y), Vector3.one * savedView.z);
				Repaint();
			}
		}

		private VisualElement CreateToolBar(BehaviourTreeGraphView graphView)
		{
			return new IMGUIContainer(() =>
			{
				GUILayout.BeginHorizontal(EditorStyles.toolbar);

				GUI.enabled = !Application.isPlaying;

				if (GUILayout.Button("Save", EditorStyles.toolbarButton))
				{
					var guiContent = new GUIContent();
					if (graphView.ValidateThenSave())
					{
						guiContent.text = "Successfully updated.";
						this.ShowNotification(guiContent);
					}
					else
					{
						guiContent.text = "Invalid tree. one or mode nodes have error.";
						this.ShowNotification(guiContent);
					}
				}

				GUI.enabled = true;

				// if (GUILayout.Button("Layout", EditorStyles.toolbarButton))
				// {
				// 	graphView.ArrangeNodesAsTree();
				// }

				graphView.AutoLayout = GUILayout.Toggle(graphView.AutoLayout, "Auto Layout", EditorStyles.toolbarButton);

				// GUILayout.Label("Label");
				// BehaviourTreeGraphView.LABEL_WIDTH = GUILayout.HorizontalSlider(BehaviourTreeGraphView.LABEL_WIDTH, .001f, .5f, GUILayout.Width(100));
				// GUILayout.Label("Node");
				// BehaviourTreeGraphView.NODE_WIDTH = GUILayout.HorizontalSlider(BehaviourTreeGraphView.NODE_WIDTH, 300, 600, GUILayout.Width(100));
				GUILayout.FlexibleSpace();
				if (GUILayout.Button(EditorGUIUtility.IconContent("d__Help@2x"), EditorStyles.toolbarButton))
				{
					Application.OpenURL("https://github.com/yoshidan/UniBT");
				}

				GUILayout.EndHorizontal();
			});
		}
	}
}