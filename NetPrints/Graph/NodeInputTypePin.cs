﻿using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    public delegate void InputTypePinIncomingPinChangedDelegate(
        NodeInputTypePin pin, NodeOutputTypePin oldPin, NodeOutputTypePin newPin);

    /// <summary>
    /// Pin which can receive types.
    /// </summary>
    [DataContract()]
    public class NodeInputTypePin(Node node, string name) : NodeTypePin(node, name)
    {
        /// <summary>
        /// Called when the node's incoming pin changed.
        /// </summary>
        public event InputTypePinIncomingPinChangedDelegate IncomingPinChanged;

        /// <summary>
        /// Incoming type pin for this pin. Null when not connected.
        /// Can trigger IncomingPinChanged when set.
        /// </summary>
        [DataMember()]
        public NodeOutputTypePin IncomingPin
        {
            get => incomingPin;
            set
            {
                if (incomingPin != value)
                {
                    NodeOutputTypePin oldPin = incomingPin;

                    SetProperty(ref incomingPin, value);

                    IncomingPinChanged?.Invoke(this, oldPin, incomingPin);
                }
            }
        }

        public override ObservableValue<BaseType> InferredType
        {
            get => IncomingPin?.InferredType;
        }

        private NodeOutputTypePin incomingPin;
    }
}
