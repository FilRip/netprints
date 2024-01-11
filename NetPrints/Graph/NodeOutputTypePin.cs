using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    /// <summary>
    /// Pin which outputs a type. Can be connected to input type pins.
    /// </summary>
    [DataContract()]
    public class NodeOutputTypePin(Node node, string name, ObservableValue<BaseType> outputType) : NodeTypePin(node, name)
    {
        /// <summary>
        /// Connected input data pins.
        /// </summary>
        [DataMember()]
        public ObservableRangeCollection<NodeInputTypePin> OutgoingPins { get; private set; }
            = [];

        public override ObservableValue<BaseType> InferredType
        {
            get => outputType;
        }

        [DataMember()]
        private readonly ObservableValue<BaseType> outputType = outputType;

        public override string ToString()
        {
            return outputType.Value?.ShortName ?? "None";
        }
    }
}
