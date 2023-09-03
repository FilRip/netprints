using System;
using System.Linq;
using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    /// <summary>
    /// Node representing the initial execution node of a method.
    /// </summary>
    [DataContract()]
    public class MethodEntryNode : ExecutionEntryNode
    {
        public MethodEntryNode(MethodGraph graph)
            : base(graph)
        {
            AddOutputExecPin("Exec");
        }

        protected override void OnInputTypeChanged(object sender, EventArgs eventArgs)
        {
            base.OnInputTypeChanged(sender, eventArgs);

            for (int i = 0; i < InputTypePins.Count; i++)
            {
                OutputDataPins[i].PinType.Value = InputTypePins[i].InferredType?.Value ?? TypeSpecifier.FromType<object>();
            }
        }

        public override string ToString()
        {
            return $"{MethodGraph.Name} Entry";
        }

        public void AddArgument()
        {
            int argIndex = OutputDataPins.Count;
            AddOutputDataPin($"Input{argIndex}", new ObservableValue<BaseType>(TypeSpecifier.FromType<object>()));
            AddInputTypePin($"Input{argIndex}Type");
        }

        public void RemoveArgument()
        {
            if (OutputDataPins.Count > 0)
            {
                NodeOutputDataPin odpToRemove = OutputDataPins[^1];
                NodeInputTypePin itpToRemove = InputTypePins[^1];

                GraphUtil.DisconnectOutputDataPin(odpToRemove);
                GraphUtil.DisconnectInputTypePin(itpToRemove);

                OutputDataPins.Remove(odpToRemove);
                InputTypePins.Remove(itpToRemove);
            }
        }

        public void AddGenericArgument()
        {
            string name = $"T{OutputTypePins.Count}";
            AddOutputTypePin(name, new GenericType(name));
        }

        public void RemoveGenericArgument()
        {
            if (OutputTypePins.Count > 0)
            {
                NodeOutputTypePin otpToRemove = OutputTypePins[^1];

                GraphUtil.DisconnectOutputTypePin(otpToRemove);

                OutputTypePins.Remove(otpToRemove);
            }
        }
    }
}
