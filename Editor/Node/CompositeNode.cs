using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBT.Editor
{
	public class CompositeNode : BehaviorTreeNode
	{
		public readonly List<Port> ChildPorts = new List<Port>();

		private List<BehaviorTreeNode> cache = new List<BehaviorTreeNode>();

		public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
		{
			evt.menu.MenuItems().Add(new BehaviorTreeDropdownMenuAction("Change Behavior", (a) =>
			{
				var provider = new CompositeSearchWindowProvider(this);
				SearchWindow.Open(new SearchWindowContext(evt.localMousePosition), provider);
			}));

			if (NodeBehavior != null && (NodeBehavior as Composite).IsDynamicSized)
			{
				evt.menu.MenuItems().Add(new BehaviorTreeDropdownMenuAction("Add Child", (a) => AddChild()));
				evt.menu.MenuItems().Add(new BehaviorTreeDropdownMenuAction("Remove Unnecessary Children", (a) => RemoveUnnecessaryChildren()));
			}
		}

		public CompositeNode()
		{
			AddChild();
			AddChild();
		}

		public void AddChild()
		{
			var child = CreateChildPort();
			ChildPorts.Add(child);
			outputContainer.Add(child);
		}

		private void RemoveUnnecessaryChildren()
		{
			var unnecessary = ChildPorts.Where(p => !p.connected).ToList();
			unnecessary.ForEach(e =>
			{
				ChildPorts.Remove(e);
				outputContainer.Remove(e);
			});
		}

		protected override bool OnValidate(Stack<BehaviorTreeNode> stack)
		{
			if (ChildPorts.Count <= 0) return false;

			foreach (var port in ChildPorts)
			{
				if (!port.connected)
				{
					style.backgroundColor = Color.red;
					return false;
				}

				stack.Push(port.connections.First().input.node as BehaviorTreeNode);
			}

			style.backgroundColor = new StyleColor(StyleKeyword.Null);
			return true;
		}

		protected override void OnSave(Stack<BehaviorTreeNode> stack)
		{
			cache.Clear();
			(NodeBehavior as Composite).Children.Clear();
			foreach (var port in ChildPorts)
			{
				var child = port.connections.First().input.node as BehaviorTreeNode;
				//(NodeBehavior as Composite).AddChild(child.ReplaceBehavior());
				(NodeBehavior as Composite).AddChild(child.NodeBehavior);
				stack.Push(child);
				cache.Add(child);
			}
		}

		protected override void OnClearStyle()
		{
			foreach (var node in cache)
			{
				node.ClearStyle();
			}
		}
	}
}