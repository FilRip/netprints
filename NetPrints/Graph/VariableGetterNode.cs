using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    /// <summary>
    /// Node that gets the value of a variable.
    /// </summary>
    [DataContract()]
    public class VariableGetterNode(NodeGraph graph, VariableSpecifier variable) : VariableNode(graph, variable)
    {
        public override string ToString()
        {
            string staticText = IsStatic ? $"{TargetType.ShortName}." : "";
            return $"Get {staticText}{VariableName}";
        }
    }
}
