using UnityEngine;

namespace UniBT
{
	public abstract class Conditional : NodeBehavior
	{
#if UNITY_EDITOR
		public override Color NodeColor => new(.5f, .5f, .3f, 1);
#endif
		/// <summary>
		/// true: don't re evaluate condition when the previous status is Running.
		/// </summary>
		[SerializeField]
		private bool dontReEvaluateOnRunning = false;

		[HideInInspector]
		[SerializeReference]
		private NodeBehavior child;

		public NodeBehavior Child
		{
			get => child;
#if UNITY_EDITOR
			set => child = value;
#endif
		}

		private bool? frameScope = null;

		private bool isRunning = false;

		protected override sealed void OnRun()
		{
			child?.Run(gameObject);
		}

		public override sealed void Awake()
		{
			OnAwake();
			child?.Awake();
		}

		protected virtual void OnAwake() { }

		public override sealed void Start()
		{
			OnStart();
			child?.Start();
		}

		protected virtual void OnStart() { }

		protected override Status OnUpdate()
		{
			// no child means leaf node
			if (child == null)
			{
				return CanUpdate() ? Status.Success : Status.Failure;
			}

			if (CanUpdate())
			{
				var status = child.Update();
				isRunning = status == Status.Running;
				return status;
			}

			return Status.Failure;
		}

		public override sealed void PreUpdate()
		{
			frameScope = null;
			child?.PreUpdate();
		}

		public override sealed void PostUpdate()
		{
			frameScope = null;
			child?.PostUpdate();
		}

		public override bool CanUpdate()
		{
			if (frameScope != null)
			{
				return frameScope.Value;
			}

			frameScope = isRunning && dontReEvaluateOnRunning || IsUpdatable();
			return frameScope.Value;
		}

		public override void Abort()
		{
			if (isRunning)
			{
				isRunning = false;
				child?.Abort();
			}
		}

		protected abstract bool IsUpdatable();
	}
}