using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace UniBT.Editor
{
	public class NodeSearchWindowProvider : ScriptableObject, ISearchWindowProvider
	{
		private GraphView graphView;
		private EditorWindow graphEditor;
		private readonly NodeResolver nodeResolver = new NodeResolver();

		public void Initialize(GraphView graphView, EditorWindow graphEditor)
		{
			this.graphView = graphView;
			this.graphEditor = graphEditor;
		}

		List<SearchTreeEntry> ISearchWindowProvider.CreateSearchTree(SearchWindowContext context)
		{
			var entries = new List<SearchTreeEntry>();
			entries.Add(new SearchTreeGroupEntry(new GUIContent("Create Node")));

			Dictionary<Type, SearchTreeGroupEntry> groups = new Dictionary<Type, SearchTreeGroupEntry>();

			var types = TypeCache.GetTypesDerivedFrom<NodeBehavior>().Where(type => type != typeof(Root) && !type.IsAbstract).ToList();

			types.Sort((t1, t2) => t1.BaseType.FullName.CompareTo(t2.BaseType.FullName));

			foreach (var type in types)
			{
				if (!groups.ContainsKey(type.BaseType))
				{
					groups.Add(type.BaseType, new SearchTreeGroupEntry(new GUIContent(type.BaseType.Name))
					{
						level = 1,
					});

					entries.Add(groups[type.BaseType]);
				}

				string name = type.Name;

				if (name.Contains(type.BaseType.Name))
				{
					name = name.Replace(type.BaseType.Name, "");
				}

				name = ObjectNames.NicifyVariableName(name);

				//Debug.Log($"{type.BaseType.FullName} {name}");
				entries.Add(new SearchTreeEntry(new GUIContent(name))
				{
					level = 2,
					userData = type
				});
			}

			return entries;
		}

		bool ISearchWindowProvider.OnSelectEntry(SearchTreeEntry searchTreeEntry, SearchWindowContext context)
		{
			var type = searchTreeEntry.userData as Type;
			BehaviorTreeNode node = nodeResolver.CreateNodeInstance(type);
			node.SetNodeType(type);
			node.CreateNodeFromType();
			
			var worldMousePosition = graphEditor.rootVisualElement.ChangeCoordinatesTo(graphEditor.rootVisualElement.parent, context.screenMousePosition - graphEditor.position.position);
			var localMousePosition = graphView.contentViewContainer.WorldToLocal(worldMousePosition);
			node.SetPosition(new Rect(localMousePosition, new Vector2(100, 100)));

			graphView.AddElement(node);
			return true;
		}
	}
}