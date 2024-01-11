using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    /// <summary>
    /// Pin that can be connected to output execution pins to receive execution.
    /// </summary>
    [DataContract()]
    public class NodeInputExecPin(Node node, string name) : NodeExecPin(node, name)
    {
        /// <summary>
        /// Output execution pins connected to this pin.
        /// </summary>
        [DataMember()]
        public ObservableRangeCollection<NodeOutputExecPin> IncomingPins { get; private set; } =
            [];
    }
}
