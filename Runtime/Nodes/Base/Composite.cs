using System.Collections.Generic;
using UnityEngine;

namespace UniBT
{
	public abstract class Composite : NodeBehavior
	{
#if UNITY_EDITOR
		public override Color NodeColor => new(.3f, .3f, .5f, 1);
#endif
		[HideInInspector]
		[SerializeReference] protected List<NodeBehavior> children = new List<NodeBehavior>();
		public virtual bool IsDynamicSized => true;

		public virtual List<NodeBehavior> Children => children;

		protected override sealed void OnRun()
		{
			children.ForEach(e => e.Run(gameObject));
		}

		public override sealed void Awake()
		{
			OnAwake();
			children.ForEach(e => e.Awake());
		}

		protected virtual void OnAwake() { }

		public override sealed void Start()
		{
			OnStart();
			children.ForEach(c => c.Start());
		}

		protected virtual void OnStart() { }

		public override sealed void PreUpdate()
		{
			children.ForEach(c => c.PreUpdate());
		}

		public override sealed void PostUpdate()
		{
			children.ForEach(c => c.PostUpdate());
		}

#if UNITY_EDITOR
		public void AddChild(NodeBehavior child)
		{
			children.Add(child);
		}
#endif
	}
}