using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
#endif

namespace UniBT.Editor
{
#if ODIN_INSPECTOR
	[HideMonoScript]
#endif
	public sealed class BehaviorTreeNodeScriptableObject : ScriptableObject
	{
#if ODIN_INSPECTOR
		[OnInspectorGUI] void OnInspectorGUI() => EditorGUIUtility.labelWidth = BehaviourTreeGraphView.LABEL_WIDTH * BehaviourTreeGraphView.NODE_WIDTH;

		[HideLabel]
		[InlineProperty]
		[HideReferenceObjectPicker]
#endif
		[SerializeReference]
		public NodeBehavior Node;
	}

	public abstract class BehaviorTreeNode : Node, IDisposable
	{
		public NodeBehavior NodeBehavior { private set; get; }

		public Port Parent { private set; get; }

		private Type dirtyNodeBehaviorType;
		private readonly TextField description;
		private BehaviorTreeNodeScriptableObject scriptableObject;
		UnityEditor.Editor editor;

		protected BehaviorTreeNode()
		{
			style.minWidth = 250;
			description = new TextField();
			description.multiline = true;
			description.style.alignSelf = Align.Center;
			description.style.marginBottom = 4;
			description.style.unityFontStyleAndWeight = FontStyle.Bold;
			description.style.color = new Color(1f, 1f, 1f, 0.5f);
			description.RegisterCallback<FocusInEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.On; });
			description.RegisterCallback<FocusOutEvent>(evt => { Input.imeCompositionMode = IMECompositionMode.Auto; });
			description.AddToClassList("zoomable-text-field");
			mainContainer.parent.Add(description);

			CallVirtualInitialize();
			RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
			scriptableObject = ScriptableObject.CreateInstance<BehaviorTreeNodeScriptableObject>();
#if ODIN_INSPECTOR
			UnityEditor.Editor.CreateCachedEditorWithContext(scriptableObject, scriptableObject, typeof(OdinEditor), ref editor);
			VisualElement inspector = new IMGUIContainer(() =>
			{
				try
				{
					editor.OnInspectorGUI();
				}
				catch (Exception e)
				{
					// TODO: ScriptableObject destroyed wrong way 'SerializedObject target has been destroyed' throws
					//Debug.LogWarning(e);
					// ignored
				}
			}); 
#else
			UnityEditor.Editor.CreateCachedEditor(scriptableObject, null, ref editor);
			VisualElement inspector = new IMGUIContainer(editor.OnInspectorGUI);
#endif
			float side = 8;
			float height = 16;
			inspector.style.marginLeft = side;
			inspector.style.marginRight = side;
			inspector.style.marginTop = height;
			inspector.style.marginBottom = height;
			mainContainer.Add(inspector);
		}

		~BehaviorTreeNode()
		{
			ReleaseUnmanagedResources();
		}

		protected void SetNode(NodeBehavior nodeBehavior)
		{
			NodeBehavior = nodeBehavior;
			scriptableObject.Node = nodeBehavior;
			titleContainer.style.backgroundColor = NodeBehavior.NodeColor;
			description.value = NodeBehavior.description;
		}

		public NodeBehavior CreateNodeFromType()
		{
			var node = Activator.CreateInstance(dirtyNodeBehaviorType) as NodeBehavior;
			SetNode(node);
			return NodeBehavior;
		}

		public void SetNodeType(Type nodeType)
		{
			dirtyNodeBehaviorType = nodeType;
			title = ObjectNames.NicifyVariableName(nodeType.Name);
		}

		public void Load(NodeBehavior node)
		{
			SetNode(node);
			NodeBehavior.NotifyEditor = MarkAsExecuted;
			OnLoad();
		}

		protected virtual void OnLoad() { }

		private void OnGeometryChanged(GeometryChangedEvent evt)
		{
			style.width = BehaviourTreeGraphView.NODE_WIDTH;
			if (evt.oldRect.position != evt.newRect.position)
			{
				Vector2 offset = evt.newRect.position - evt.oldRect.position;
				MoveConnectedNodes(offset);
			}
		}
		private void MoveConnectedNodes(Vector2 offset)
		{
			var connectedNodes = GetConnectedNodesOnRight();

			foreach (var node in connectedNodes)
			{
				var newPosition = node.layout.position + offset;
				node.SetPosition(new Rect(newPosition, node.layout.size));
			}
		}

		private List<Node> GetConnectedNodesOnRight()
		{
			var result = new List<Node>();

			foreach (var port in outputContainer.Query<Port>().ToList())
			{
				foreach (var connection in port.connections)
				{
					if (connection.input.node != this)
					{
						result.Add(connection.input.node);
					}
				}
			}

			return result;
		}

		private void CallVirtualInitialize()
		{
			AddParent();
		}

		protected virtual void AddParent()
		{
			Parent = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(Port));
			Parent.portName = "Parent";
			inputContainer.Add(Parent);
		}

		protected Port CreateChildPort()
		{
			var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(Port));
			port.portName = "Child";
			return port;
		}

		public void Save(Stack<BehaviorTreeNode> stack)
		{
			OnSave(stack);

			NodeBehavior.description = this.description.value;
			NodeBehavior.graphPosition = GetPosition();
			NodeBehavior.NotifyEditor = MarkAsExecuted;
		}
		protected abstract void OnSave(Stack<BehaviorTreeNode> stack);

		public bool Validate(Stack<BehaviorTreeNode> stack)
		{
			var valid = dirtyNodeBehaviorType != null && OnValidate(stack);
			if (valid)
			{
				style.backgroundColor = new StyleColor(StyleKeyword.Null);
			}
			else
			{
				style.backgroundColor = Color.red;
			}

			return valid;
		}

		protected abstract bool OnValidate(Stack<BehaviorTreeNode> stack);

		private void MarkAsExecuted(Status status)
		{
			switch (status)
			{
				case Status.Failure:
					{
						AnimateBackgroundColor(Color.red / 1.5f);
						break;
					}

				case Status.Running:
					{
						AnimateBackgroundColor(Color.yellow / 1.5f);
						break;
					}

				case Status.Success:
					{
						AnimateBackgroundColor(Color.green / 1.5f);
						break;
					}
			}
		}

		private void AnimateBackgroundColor(Color to)
		{
			float duration = .5f;
			var startTime = Time.time;
			schedule.Execute(() =>
			{
				var elapsed = Time.time - startTime;
				float t = Mathf.Clamp01((float)(elapsed / duration));
				if (t < .5f)
				{
					t = 0;
				}
				else
				{
					t = Mathf.Lerp(.5f, 1, t);
				}

				t *= t;
				style.backgroundColor = Color.Lerp(to, Color.clear, t);

				if (t >= .99f)
				{
					style.backgroundColor = new StyleColor(StyleKeyword.Null);
				}
			}).Every(1).ForDuration((int)(duration * 1000f)); // примерно 60 FPS
		}

		public void ClearStyle()
		{
			style.backgroundColor = new StyleColor(StyleKeyword.Null);
			OnClearStyle();
		}

		protected abstract void OnClearStyle();
		void ReleaseUnmanagedResources()
		{
			if (editor)
			{
				Object.DestroyImmediate(editor);
			}
		}
		public void Dispose()
		{
			ReleaseUnmanagedResources();
			GC.SuppressFinalize(this);
		}
	}
}