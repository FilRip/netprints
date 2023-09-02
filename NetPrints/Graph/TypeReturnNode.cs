using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    [DataContract()]
    public class TypeReturnNode : Node
    {
        public NodeInputTypePin TypePin => InputTypePins[0];

        public TypeReturnNode(TypeGraph graph)
            : base(graph)
        {
            AddInputTypePin("Type");
        }
    }
}
