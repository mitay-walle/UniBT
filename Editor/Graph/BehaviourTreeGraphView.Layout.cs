using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UniBT.Editor
{
	public partial class BehaviourTreeGraphView
	{
		public static float LABEL_WIDTH = .5f;
		public static float NODE_WIDTH = 300f;
		private const float NODE_HEIGHT = 150f;
		private const float HORIZONTAL_SPACING = 100f;
		private const float VERTICAL_SPACING = 100f;
		private const float ROOT_X_OFFSET = 50f;
		private const float ROOT_Y_OFFSET = 50f;
		public bool AutoLayout = true;

		public void ArrangeNodesAsTree()
		{
			if (rootNode == null)
			{
				Debug.LogWarning("Root node not found. Cannot arrange tree.");
				return;
			}

			var nodeHierarchy = BuildNodeHierarchy();
			var positions = new Dictionary<BehaviorTreeNode, Vector2>();
			var nodeHeights = new Dictionary<BehaviorTreeNode, float>();

			CalculateSubtreeHeightsRecursive(rootNode, nodeHierarchy, nodeHeights);
			PlaceNodeWithBranchingRecursive(rootNode, ROOT_X_OFFSET, ROOT_Y_OFFSET, nodeHierarchy, positions, nodeHeights);
			ApplyPositionsToNodes(positions);
		}

		private Dictionary<BehaviorTreeNode, List<BehaviorTreeNode>> BuildNodeHierarchy()
		{
			var hierarchy = new Dictionary<BehaviorTreeNode, List<BehaviorTreeNode>>();
			var visited = new HashSet<BehaviorTreeNode>();
			var queue = new Queue<BehaviorTreeNode>();

			queue.Enqueue(rootNode);
			visited.Add(rootNode);

			while (queue.Count > 0)
			{
				var currentNode = queue.Dequeue();
				var children = GetChildNodes(currentNode);

				hierarchy[currentNode] = children;

				foreach (var child in children)
				{
					if (!visited.Contains(child))
					{
						visited.Add(child);
						queue.Enqueue(child);
					}
				}
			}

			return hierarchy;
		}

		private List<BehaviorTreeNode> GetChildNodes(BehaviorTreeNode node)
		{
			var children = new List<BehaviorTreeNode>();

			var outgoingEdges = edges.ToList().Where(e => e.output?.node == node).ToList();
			
			outgoingEdges.Sort((edge, edge1) => edge1.output.GetPosition().y.CompareTo(edge.output.GetPosition().y));
			
			foreach (var edge in outgoingEdges)
			{
				if (edge.input?.node is BehaviorTreeNode childNode)
				{
					children.Add(childNode);
				}
			}

			return children;
		}

		private float CalculateSubtreeHeightsRecursive(BehaviorTreeNode node, Dictionary<BehaviorTreeNode, List<BehaviorTreeNode>> hierarchy, Dictionary<BehaviorTreeNode, float> nodeHeights)
		{
			if (!hierarchy.TryGetValue(node, out List<BehaviorTreeNode> children))
			{
				nodeHeights[node] = NODE_HEIGHT;
				return NODE_HEIGHT;
			}

			if (children.Count == 0)
			{
				nodeHeights[node] = NODE_HEIGHT;
				return NODE_HEIGHT;
			}

			float totalChildrenHeight = 0;
			foreach (var child in children)
			{
				totalChildrenHeight += CalculateSubtreeHeightsRecursive(child, hierarchy, nodeHeights);
			}

			float totalHeight = totalChildrenHeight + (children.Count - 1) * VERTICAL_SPACING;
			nodeHeights[node] = Mathf.Max(NODE_HEIGHT, totalHeight);

			return nodeHeights[node];
		}

		private void PlaceNodeWithBranchingRecursive(BehaviorTreeNode node, float x, float y, Dictionary<BehaviorTreeNode, List<BehaviorTreeNode>> hierarchy, Dictionary<BehaviorTreeNode, Vector2> positions, Dictionary<BehaviorTreeNode, float> nodeHeights)
		{
			positions[node] = new Vector2(x, y);

			if (!hierarchy.TryGetValue(node, out List<BehaviorTreeNode> children))
				return;

			if (children.Count == 0)
				return;

			float totalChildrenHeight = 0;
			foreach (var child in children)
			{
				totalChildrenHeight += nodeHeights[child];
			}

			float totalHeightWithSpacing = totalChildrenHeight + (children.Count - 1) * VERTICAL_SPACING;
			float startY = y + totalHeightWithSpacing / 2f;
			float currentY = startY;
			float childX = x + NODE_WIDTH + HORIZONTAL_SPACING;

			foreach (var child in children)
			{
				float childHeight = nodeHeights[child];
				float childCenterY = currentY - childHeight / 2f;

				PlaceNodeWithBranchingRecursive(child, childX, childCenterY, hierarchy, positions, nodeHeights);

				currentY -= childHeight + VERTICAL_SPACING;
			}
		}

		private void ApplyPositionsToNodes(Dictionary<BehaviorTreeNode, Vector2> positions)
		{
			foreach (var kvp in positions)
			{
				var node = kvp.Key;
				var position = kvp.Value;

				var rect = new Rect(position.x, position.y - NODE_HEIGHT / 2f, NODE_WIDTH, NODE_HEIGHT);
				node.SetPosition(rect);

				if (node.NodeBehavior != null)
				{
					node.NodeBehavior.graphPosition = rect;
				}
			}
		}
	}
}