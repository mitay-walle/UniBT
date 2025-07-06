using UnityEngine;

namespace UniBT
{
	public enum UpdateType
	{
		Auto,
		Manual
	}

	public class BehaviorTree : MonoBehaviour
	{
		//[HideInInspector]
		[SerializeReference]
		protected Root root = new Root();

		[SerializeField]
		protected UpdateType updateType;

		public Root Root
		{
			get => root;
#if UNITY_EDITOR
			set => root = value;
#endif
		}

		protected void Awake()
		{
			root.Run(gameObject);
			root.Awake();
		}

		protected void Start()
		{
			root.Start();
		}

		protected virtual void FixedUpdate()
		{
			if (updateType == UpdateType.Auto) Tick();
		}

		protected void Tick()
		{
			root.PreUpdate();
			root.Update();
			root.PostUpdate();
		}
	}
}