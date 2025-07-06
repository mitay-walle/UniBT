using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UniBT.Editor
{
    public sealed class RootNode : BehaviorTreeNode
    {
        public readonly Port Child;

        private BehaviorTreeNode cache;

        public RootNode() 
        {
            SetNodeType(typeof(Root));
            title = "Root";
            Child = CreateChildPort();
            outputContainer.Add(Child);
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.MenuItems().Clear();
        }

        protected override void AddParent()
        {
        }

        protected override void OnLoad()
        {
            (NodeBehavior as Root).UpdateEditor = ClearStyle;
        }

        protected override bool OnValidate(Stack<BehaviorTreeNode> stack)
        {
            if (!Child.connected)
            {
                return false;
            }
            stack.Push(Child.connections.First().input.node as BehaviorTreeNode);
            return true;
        }
        protected override void OnSave(Stack<BehaviorTreeNode> stack)
        {
            var child = Child.connections.First().input.node as BehaviorTreeNode;
            var newRoot = new Root();
            newRoot.Child = child.NodeBehavior;
            newRoot.UpdateEditor = ClearStyle;
            SetNode(newRoot);
            stack.Push(child);
            cache = child;
        }

        protected override void OnClearStyle()
        {
            cache?.ClearStyle();
        }
    }
}