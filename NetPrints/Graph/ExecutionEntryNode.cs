using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    [DataContract()]
    public abstract class ExecutionEntryNode(ExecutionGraph graph) : Node(graph)
    {
        /// <summary>
        /// Output execution pin that initially executes when a method gets called.
        /// </summary>
        public NodeOutputExecPin InitialExecutionPin
        {
            get { return OutputExecPins[0]; }
        }
    }
}
