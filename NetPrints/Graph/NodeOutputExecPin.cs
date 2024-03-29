﻿using System.Runtime.Serialization;

namespace NetPrints.Graph
{
    public delegate void DelegateOutputExecPinOutgoingPinChanged(
        NodeOutputExecPin pin, NodeInputExecPin oldPin, NodeInputExecPin newPin);

    /// <summary>
    /// Pin which can be connected to an input execution pin to pass along execution.
    /// </summary>
    [DataContract()]
    public class NodeOutputExecPin(Node node, string name) : NodeExecPin(node, name)
    {
        /// <summary>
        /// Called when the connected outgoing pin changed.
        /// </summary>
        public event DelegateOutputExecPinOutgoingPinChanged OutgoingPinChanged;

        /// <summary>
        /// Connected input execution pin. Null if not connected.
        /// Can trigger OutgoingPinChanged when set.
        /// </summary>
        [DataMember()]
        public NodeInputExecPin OutgoingPin
        {
            get => outgoingPin;
            set
            {
                if (outgoingPin != value)
                {
                    NodeInputExecPin oldPin = outgoingPin;

                    SetProperty(ref outgoingPin, value);

                    OutgoingPinChanged?.Invoke(this, oldPin, outgoingPin);
                }
            }
        }

        private NodeInputExecPin outgoingPin;
    }
}
