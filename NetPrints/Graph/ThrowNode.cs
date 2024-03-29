﻿using System;
using System.Runtime.Serialization;

using NetPrints.Core;

namespace NetPrints.Graph
{
    /// <summary>
    /// Node representing an exception throw.
    /// </summary>
    [DataContract()]
    public class ThrowNode : Node
    {
        /// <summary>
        /// Pin for the exception to throw.
        /// </summary>
        public NodeInputDataPin ExceptionPin
        {
            get { return InputDataPins[0]; }
        }

        public ThrowNode(NodeGraph graph)
            : base(graph)
        {
            AddInputExecPin("Exec");
            AddInputDataPin("Exception", TypeSpecifier.FromType<Exception>());
        }

        public override string ToString()
        {
            return $"Throw Exception";
        }
    }
}
