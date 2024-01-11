using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    /// <summary>
    /// Pin which outputs a value. Can be connected to input data pins.
    /// </summary>
    [DataContract()]
    public class NodeOutputDataPin(Node node, string name, ObservableValue<BaseType> pinType) : NodeDataPin(node, name, pinType)
    {
        /// <summary>
        /// Connected input data pins.
        /// </summary>
        [DataMember()]
        public ObservableRangeCollection<NodeInputDataPin> OutgoingPins { get; private set; }
            = [];
    }
}
