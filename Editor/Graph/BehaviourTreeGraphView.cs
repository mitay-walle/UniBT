using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBT.Editor
{
	public partial class BehaviourTreeGraphView : GraphView
	{
		private readonly struct EdgePair
		{
			public readonly NodeBehavior NodeBehavior;
			public readonly Port ParentOutputPort;

			public EdgePair(NodeBehavior nodeBehavior, Port parentOutputPort)
			{
				NodeBehavior = nodeBehavior;
				ParentOutputPort = parentOutputPort;
			}
		}

		private readonly BehaviorTree behaviorTree;
		public BehaviorTree BehaviorTree => behaviorTree;
		public SerializedObject SerializedObject { get; private set; }

		private RootNode rootNode;
		private float lastZoom = 1f;

		private readonly NodeResolver nodeResolver = new NodeResolver();

		public BehaviourTreeGraphView(BehaviorTree bt, EditorWindow editor)
		{
			behaviorTree = bt;
			SerializedObject = new SerializedObject(behaviorTree);
			style.flexGrow = 1;

			SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);

			var grid = new GridBackground();

			grid.style.backgroundColor = new Color(0.08f, 0.08f, 0.08f, 1f);
			grid.style.unityBackgroundImageTintColor = new Color(0.16f, 0.16f, 0.16f, 1f);
			grid.MarkDirtyRepaint();
			this.style.backgroundColor = new Color(0.05f, 0.05f, 0.05f, 1f);
			Insert(0, grid);
			grid.StretchToParentSize();

			var contentDragger = new ContentDragger();
			contentDragger.activators.Add(new ManipulatorActivationFilter()
			{
				button = MouseButton.MiddleMouse,
			});

			this.AddManipulator(new SelectionDragger());
			this.AddManipulator(contentDragger);
			this.AddManipulator(new RectangleSelector());

			var provider = ScriptableObject.CreateInstance<NodeSearchWindowProvider>();
			provider.Initialize(this, editor);

			nodeCreationRequest += context =>
			{
				SearchWindow.Open(new SearchWindowContext(context.screenMousePosition), provider);
			};

			this.RegisterCallback<AttachToPanelEvent>(evt =>
			{
				schedule.Execute(() =>
				{
					float currentZoom = viewTransform.scale.y;

					if (!Mathf.Approximately(currentZoom, lastZoom))
					{
						lastZoom = currentZoom;
						UpdateZoomableLabels(currentZoom);
					}
				}).Every(100); // 10 fps
			});
		}

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			base.BuildContextualMenu(evt);
			var remainTargets = evt.menu.MenuItems().FindAll(e =>
			{
				switch (e)
				{
					case BehaviorTreeDropdownMenuAction _: return true;
					case DropdownMenuAction a: return a.name == "Create Node" || a.name == "Delete";
					default: return false;
				}
			});

			//Remove needless default actions .
			evt.menu.MenuItems().Clear();
			remainTargets.ForEach(evt.menu.MenuItems().Add);
		}

		public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
		{
			var compatiblePorts = new List<Port>();
			foreach (var port in ports.ToList())
			{
				if (startAnchor.node == port.node || startAnchor.direction == port.direction || startAnchor.portType != port.portType)
				{
					continue;
				}

				compatiblePorts.Add(port);
			}

			return compatiblePorts;
		}

		public void Load()
		{
			var stack = new Stack<EdgePair>();
			stack.Push(new EdgePair(behaviorTree.Root, null));
			while (stack.Count > 0)
			{
				// create node
				var edgePair = stack.Pop();
				if (edgePair.NodeBehavior == null)
				{
					continue;
				}

				var node = nodeResolver.CreateNodeInstance(edgePair.NodeBehavior.GetType());
				node.Load(edgePair.NodeBehavior);
				AddElement(node);
				node.SetPosition(edgePair.NodeBehavior.graphPosition);

				// connect parent
				if (edgePair.ParentOutputPort != null)
				{
					var edge = ConnectPorts(edgePair.ParentOutputPort, node.Parent);
					AddElement(edge);
				}

				// seek child
				switch (edgePair.NodeBehavior)
				{
					case Composite nb:
						{
							var compositeNode = node as CompositeNode;
							var addible = nb.Children.Count - compositeNode.ChildPorts.Count;
							for (var i = 0; i < addible; i++)
							{
								compositeNode.AddChild();
							}

							for (var i = 0; i < nb.Children.Count; i++)
							{
								stack.Push(new EdgePair(nb.Children[i], compositeNode.ChildPorts[i]));
							}

							break;
						}

					case Conditional nb:
						{
							var decoratorNode = node as ConditionalNode;
							stack.Push(new EdgePair(nb.Child, decoratorNode.Child));
							break;
						}

					case Root nb:
						{
							rootNode = node as RootNode;
							if (nb.Child != null)
							{
								stack.Push(new EdgePair(nb.Child, rootNode.Child));
							}

							break;
						}
				}
			}
		}

		public bool ValidateThenSave()
		{
			if (Validate())
			{
				Save();
				return true;
			}

			return false;
		}

		private bool Validate()
		{
			//validate nodes by DFS.
			var stack = new Stack<BehaviorTreeNode>();
			stack.Push(rootNode);
			while (stack.Count > 0)
			{
				var node = stack.Pop();
				if (!node.Validate(stack))
				{
					return false;
				}
			}

			return true;
		}

		private void Save()
		{
			var stack = new Stack<BehaviorTreeNode>();

			stack.Push(rootNode);

			// save new components
			while (stack.Count > 0)
			{
				var node = stack.Pop();
				node.Save(stack);
			}

			behaviorTree.Root = JsonUtility.FromJson<Root>(JsonUtility.ToJson(rootNode.NodeBehavior as Root));
			EditorUtility.SetDirty(behaviorTree);
		}

		private static Edge ConnectPorts(Port output, Port input)
		{
			var tempEdge = new Edge
			{
				output = output,
				input = input
			};

			tempEdge.input.Connect(tempEdge);
			tempEdge.output.Connect(tempEdge);
			return tempEdge;
		}

		private void UpdateZoomableLabels(float zoom)
		{
			float inverseScale = 1f / zoom;

			foreach (var textField in this.Query<TextField>().ToList())
			{
				if (textField.ClassListContains("zoomable-text-field"))
				{
					if (string.IsNullOrEmpty(textField.value))
					{
						textField.transform.scale = Vector3.one;
					}
					else
					{
						textField.transform.scale = Vector3.one * inverseScale;
					}

					textField.MarkDirtyRepaint();
				}
			}
		}
	}
}