using System.Collections.Generic;
using System;

namespace UniBT
{
    [Serializable]
    public class All : Composite
    {

        private List<NodeBehavior> runningNodes;

        protected override void OnAwake()
        {
            runningNodes = new List<NodeBehavior>();
        }

        /// <summary>
        /// Update all nodes.
        /// - any running -> Running
        /// - any failed -> Failure
        /// - else -> Success
        /// </summary>
        protected override Status OnUpdate()
        {
            runningNodes.Clear();
            var anyFailed = false;
            foreach (var c in Children)
            {
                var result = c.Update();
                if (result == Status.Running)
                {
                    runningNodes.Add(c);
                }else if (result == Status.Failure)
                {
                    anyFailed = true;
                }
            }
            if (runningNodes.Count > 0)
            {
                return Status.Running;
            }

            if (anyFailed)
            {
                runningNodes.ForEach(e => e.Abort());
                return Status.Failure;
            }

            return Status.Success;
        }

        public override void Abort()
        {
            runningNodes.ForEach( e => e.Abort() );
            runningNodes.Clear();
        }

    }
}