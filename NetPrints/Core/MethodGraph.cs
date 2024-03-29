﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

using NetPrints.Graph;

namespace NetPrints.Core
{
    /// <summary>
    /// Modifiers a method can have. Can be combined.
    /// </summary>
    [Flags()]
    public enum MethodModifiers
    {
        None = 0,
        Sealed = 8,
        Abstract = 16,
        Static = 32,
        Virtual = 64,
        Override = 128,
        Async = 256,
    }

    /// <summary>
    /// Method type. Contains common things usually associated with methods such as its arguments and its name.
    /// </summary>
    [DataContract()]
    public partial class MethodGraph : ExecutionGraph
    {
        /// <summary>
        /// Return node of this method that when executed will return from the method.
        /// </summary>
        public IEnumerable<ReturnNode> ReturnNodes
        {
            get => Nodes.OfType<ReturnNode>();
        }

        /// <summary>
        /// Main return node that determines the return types of all other return nodes.
        /// </summary>
        public ReturnNode MainReturnNode
        {
            get => Nodes?.OfType<ReturnNode>().FirstOrDefault();
        }

        /// <summary>
        /// Ordered return types this method returns.
        /// </summary>
        public IEnumerable<BaseType> ReturnTypes()
        {
            return MainReturnNode?.InputTypePins?.Select(pin => pin.InferredType?.Value ?? TypeSpecifier.FromType<object>()).ToList() ?? [];
        }

        /// <summary>
        /// Generic type arguments of the method.
        /// </summary>
        public IEnumerable<GenericType> GenericArgumentTypes()
        {
            return EntryNode != null ? EntryNode.OutputTypePins.Select(pin => pin.InferredType.Value).Cast<GenericType>().ToList() : [];
        }

        /// <summary>
        /// Name of the method without any prefixes.
        /// </summary>
        [DataMember()]
        public string Name
        {
            get;
            set;
        }

        /// <summary>
        /// Modifiers this method has.
        /// </summary>
        [DataMember()]
        public MethodModifiers Modifiers
        {
            get;
            set;
        } = MethodModifiers.None;

        /// <summary>
        /// Method entry node where this method graph's execution starts.
        /// </summary>
        public MethodEntryNode MethodEntryNode
        {
            get => (MethodEntryNode)EntryNode;
        }

        /// <summary>
        /// Creates a method given its name.
        /// </summary>
        /// <param name="name">Name for the method.</param>
        public MethodGraph(string name)
        {
            Name = name;
            EntryNode = new MethodEntryNode(this);
            _ = new ReturnNode(this);
        }

        [OnDeserialized()]
        private void OnDeserialized(StreamingContext context)
        {
            // Call Node.OnMethodDeserialized until the types don't change anymore
            // or a max iteration was reached.
            // TODO: Sort nodes by depth and propagate in order instead of
            //       doing this inefficient relaxation process.

            int iterations = 0;
            bool anyTypeChanged = true;
            Dictionary<NodeTypePin, BaseType> pinTypes = [];

            while (anyTypeChanged && iterations < 20)
            {
                anyTypeChanged = false;
                pinTypes.Clear();

                foreach (Node node in Nodes)
                {
                    node.InputTypePins.ToList().ForEach(p => pinTypes.Add(p, p.InferredType?.Value));

                    node.OnMethodDeserialized();

                    if (node.InputTypePins.Any(p => pinTypes[p] != p.InferredType?.Value))
                    {
                        anyTypeChanged = true;
                    }
                }

                iterations++;
            }
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
