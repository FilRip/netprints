﻿using System;
using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    public delegate void InputDataPinIncomingPinChangedDelegate(
        NodeInputDataPin pin, NodeOutputDataPin oldPin, NodeOutputDataPin newPin);

    /// <summary>
    /// Input data pin which can be connected to up to one output data pin to receive a value.
    /// </summary>
    [DataContract()]
    public class NodeInputDataPin(Node node, string name, ObservableValue<BaseType> pinType) : NodeDataPin(node, name, pinType)
    {
        /// <summary>
        /// Called when the node's incoming pin changed.
        /// </summary>
        public event InputDataPinIncomingPinChangedDelegate IncomingPinChanged;

        /// <summary>
        /// Incoming data pin for this pin. Null when not connected.
        /// Can trigger IncomingPinChanged when set.
        /// </summary>
        [DataMember()]
        public NodeOutputDataPin IncomingPin
        {
            get => incomingPin;
            set
            {
                if (incomingPin != value)
                {
                    NodeOutputDataPin oldPin = incomingPin;

                    SetProperty(ref incomingPin, value);

                    IncomingPinChanged?.Invoke(this, oldPin, incomingPin);
                }
            }
        }

        /// <summary>
        /// Whether this pin uses its unconnected value to output a value
        /// when no pin is connected to it.
        /// </summary>
        public bool UsesUnconnectedValue
        {
            get => PinType.Value is TypeSpecifier t && t.IsPrimitive;
        }

        private NodeOutputDataPin incomingPin;

        /// <summary>
        /// Unconnected value of this pin when no pin is connected to it.
        /// Setting this for types that don't support unconnected values will throw
        /// an exception.
        /// </summary>
        [DataMember()]
        public object UnconnectedValue
        {
            get => unconnectedValue;
            set
            {
                // Check that:
                // this pin uses the unconnected value
                // the value is of the same type or string if enum

                if (value != null && (!UsesUnconnectedValue
                    || (PinType.Value is TypeSpecifier t && (
                        (!t.IsEnum && TypeSpecifier.FromType(value.GetType()) != t)
                        || (t.IsEnum && value is not string)))))
                {
                    throw new ArgumentException("Pin unconnected");
                }

                SetProperty(ref unconnectedValue, value);
            }
        }

        private object unconnectedValue;

        [DataMember()]
        public object ExplicitDefaultValue
        {
            get;
            set;
        }

        [DataMember()]
        public bool UsesExplicitDefaultValue
        {
            get;
            set;
        }
    }
}
