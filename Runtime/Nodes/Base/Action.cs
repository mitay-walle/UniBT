using System;
using UnityEngine;

namespace UniBT
{
	[Serializable]
	public abstract class Action : NodeBehavior
	{
		#if UNITY_EDITOR
		public override Color NodeColor => new(.3f,.5f,.3f,1);
		#endif
		protected override sealed void OnRun() { }

		public override sealed void PreUpdate() { }

		public override sealed void PostUpdate() { }
	}
}