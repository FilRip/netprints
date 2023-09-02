﻿using System;
using System.Linq;

using NetPrints.Core;
using NetPrints.Graph;

namespace NetPrintsEditor.Messages
{
    /// <summary>
    /// Message for adding a node to a graph.
    /// </summary>
    public class AddNodeMessage
    {
        public bool Handled;

        public Type NodeType;
        public NodeGraph Graph;
        public double PositionX;
        public double PositionY;
        public NodePin SuggestionPin;
        public object[] ConstructorParameters;

        public AddNodeMessage(Type nodeType, NodeGraph graph, double posX, double posY, NodePin suggestionPin, params object[] constructorParameters)
        {
            if (!nodeType.IsSubclassOf(typeof(Node)) || nodeType.IsAbstract)
            {
                throw new ArgumentException("Invalid type for node");
            }

            Type[] constructorParamTypes = (new Type[] { typeof(NodeGraph) }).Concat
                (constructorParameters.Select(p => p.GetType()))
                .ToArray();

            if (nodeType.GetConstructor(constructorParamTypes) == null)
            {
                constructorParamTypes = (new Type[] { typeof(MethodGraph) }).Concat
                    (constructorParameters.Select(p => p.GetType()))
                    .ToArray();

                if (nodeType.GetConstructor(constructorParamTypes) == null)
                {
                    constructorParamTypes = (new Type[] { typeof(ConstructorGraph) }).Concat
                        (constructorParameters.Select(p => p.GetType()))
                        .ToArray();

                    if (nodeType.GetConstructor(constructorParamTypes) == null)
                        throw new ArgumentException($"Invalid parameters for constructor of {nodeType.FullName}");
                }
            }

            NodeType = nodeType;
            Graph = graph;
            PositionX = posX;
            PositionY = posY;
            SuggestionPin = suggestionPin;
            ConstructorParameters = constructorParameters;
        }
    }
}
