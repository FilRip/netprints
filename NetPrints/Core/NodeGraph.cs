﻿using System.Runtime.Serialization;

using NetPrints.Graph;

namespace NetPrints.Core
{
    [DataContract()]
    [KnownType(typeof(MethodGraph))]
    [KnownType(typeof(ConstructorGraph))]
    [KnownType(typeof(ClassGraph))]
    [KnownType(typeof(TypeGraph))]
    public abstract class NodeGraph
    {
        /// <summary>
        /// Collection of nodes in this graph.
        /// </summary>
        [DataMember()]
        public ObservableRangeCollection<Node> Nodes
        {
            get;
            private set;
        } = [];

        /// <summary>
        /// Class this graph is contained in.
        /// </summary>
        [DataMember()]
        public ClassGraph Class
        {
            get;
            set;
        }

        /// <summary>
        /// Project the graph is part of.
        /// </summary>
        public Project Project
        {
            get;
            set;
        }
    }
}
