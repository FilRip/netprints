﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

using NetPrints.Core;
using NetPrints.Graph;

namespace NetPrints.Translator
{
    public static class TranslatorUtil
    {
        public const string VariablePrefix = "var";
        public const string TemporaryVariablePrefix = "temp";

        public readonly static Dictionary<MemberVisibility, string> VisibilityTokens = new()
        {
            [MemberVisibility.Private] = "private",
            [MemberVisibility.Protected] = "protected",
            [MemberVisibility.Public] = "public",
            [MemberVisibility.Internal] = "internal",
        };

        private const int TemporaryVariableNameLength = 16;

        /// <summary>
        /// Gets a temporary variable name.
        /// </summary>
        /// <returns>Temporary variable name.</returns>
        public static string GetTemporaryVariableName(Random random)
        {
            const string chars = "abcdefghijklmnopqrstuvwxyz";

            string name = TemporaryVariablePrefix;
            int additionalLength = TemporaryVariableNameLength - name.Length;
            if (additionalLength > 0)
            {
                string addition = new(Enumerable.Repeat(chars, additionalLength).Select(s => s[random.Next(s.Length)]).ToArray());
                addition = addition[0].ToString().ToUpper() + string.Join("", addition.Skip(1));
                name += addition;
            }

            return name;
        }

        /// <summary>
        /// Translates an object into a literal value (eg. a float 32.32 -> "32.32f")
        /// </summary>
        /// <param name="obj">Object value to translate.</param>
        /// <param name="type">Specifier for the type of the literal.</param>
        /// <returns></returns>
        public static string ObjectToLiteral(object obj, TypeSpecifier type)
        {
            // Interpret object string as enum field
            if (type.IsEnum)
            {
                return $"{type}.{obj}";
            }

            // Null
            if (obj is null)
            {
                return "null";
            }

            // Put quotes around string literals
            if (type == TypeSpecifier.FromType<string>())
            {
                return $"\"{obj}\"";
            }
            else if (type == TypeSpecifier.FromType<float>())
            {
                return $"{obj}F";
            }
            else if (type == TypeSpecifier.FromType<double>())
            {
                return $"{obj}D";
            }
            else if (type == TypeSpecifier.FromType<uint>())
            {
                return $"{obj}U";
            }
            // Put single quotes around char literals
            else if (type == TypeSpecifier.FromType<char>())
            {
                return $"'{obj}'";
            }
            else if (type == TypeSpecifier.FromType<long>())
            {
                return $"{obj}L";
            }
            else if (type == TypeSpecifier.FromType<ulong>())
            {
                return $"{obj}UL";
            }
            else if (type == TypeSpecifier.FromType<decimal>())
            {
                return $"{obj}M";
            }
            else if (type == TypeSpecifier.FromType<bool>())
            {
                return obj.ToString().ToLower();  //Bool false is converted to False, causing the issue in compilation
            }
            else
            {
                return obj.ToString();
            }
        }

        /// <summary>
        /// Returns the first name not already contained in a list of names by
        /// trying to generate a unique name based on the given name.
        /// Includes a prefix in front of the name.
        /// </summary>
        /// <param name="name">Name to make unique.</param>
        /// <param name="names">List of names already existing.</param>
        /// <returns>Name based on name but not contained in names.</returns>
        public static string GetUniqueVariableName(string name, IList<string> names)
        {
            // Don't allow illegal characters in the name
            // TODO: Make this more general
            name = name.Replace("+", "_").Replace("[", "").Replace("]", "Array").Replace(",", "");

            int i = 1;

            while (true)
            {
                string uniqueName = i == 1 ? $"{VariablePrefix}{name}" : $"{VariablePrefix}{name}{i}";

                if (!names.Contains(uniqueName))
                {
                    return uniqueName;
                }

                i++;
            }
        }

        private static void AddAllNodes(Node node, ref HashSet<Node> nodes)
        {
            nodes.Add(node);

            foreach (NodeInputExecPin pin in node.OutputExecPins.Select(pin => pin.OutgoingPin))
            {
                if (pin != null && !nodes.Contains(pin.Node))
                {
                    AddAllNodes(pin.Node, ref nodes);
                }
            }

            foreach (NodeOutputDataPin pin in node.InputDataPins.Select(pin => pin.IncomingPin))
            {
                if (pin != null && !nodes.Contains(pin.Node))
                {
                    AddAllNodes(pin.Node, ref nodes);
                }
            }

            foreach (NodeOutputTypePin pin in node.InputTypePins.Select(pin => pin.IncomingPin))
            {
                if (pin != null && !nodes.Contains(pin.Node))
                {
                    AddAllNodes(pin.Node, ref nodes);
                }
            }
        }

        /// <summary>
        /// Gets all nodes contained in a graph.
        /// </summary>
        /// <param name="graph">Graph containing the nodes.</param>
        /// <returns>Nodes contained in the graph.</returns>
        public static IEnumerable<Node> GetAllNodesInExecGraph(ExecutionGraph graph)
        {
            HashSet<Node> nodes = [];

            AddAllNodes(graph.EntryNode, ref nodes);

            return nodes;
        }

        private static void AddExecNodes(Node node, ref HashSet<Node> nodes)
        {
            nodes.Add(node);

            foreach (NodeInputExecPin pin in node.OutputExecPins.Select(pin => pin.OutgoingPin))
            {
                if (pin != null && !nodes.Contains(pin.Node))
                {
                    AddExecNodes(pin.Node, ref nodes);
                }
            }
        }

        /// <summary>
        /// Gets all execution contained nodes in a graph
        /// </summary>
        /// <param name="graph">Graph containing the execution nodes.</param>
        /// <returns>Execution nodes contained in the graph.</returns>
        public static IEnumerable<Node> GetExecNodesInExecGraph(ExecutionGraph graph)
        {
            HashSet<Node> nodes = [];

            AddExecNodes(graph.EntryNode, ref nodes);

            return nodes;
        }

        private static void AddDependentPureNodes(Node node, ref HashSet<Node> nodes)
        {
            // Only add pure nodes (the initial node might not be pure)
            if (node.IsPure)
            {
                nodes.Add(node);
            }

            foreach (NodeOutputDataPin pin in node.InputDataPins.Select(pin => pin.IncomingPin))
            {
                if (pin?.Node?.IsPure == true && !nodes.Contains(pin.Node))
                {
                    AddDependentPureNodes(pin.Node, ref nodes);
                }
            }
        }

        /// <summary>
        /// Gets all pure nodes a node depends on.
        /// </summary>
        /// <param name="node">Node whose dependent pure nodes to get.</param>
        /// <returns>Pure nodes the node depends on.</returns>
        public static IEnumerable<Node> GetDependentPureNodes(Node node)
        {
            HashSet<Node> nodes = [];

            AddDependentPureNodes(node, ref nodes);

            return nodes;
        }

        /// <summary>
        /// Gets all pure nodes a node depends on sorted by depth.
        /// </summary>
        /// <param name="node">Node whose dependent pure nodes to get.</param>
        /// <returns>Pure nodes the node depends on sorted by depth.</returns>
        public static IEnumerable<Node> GetSortedPureNodes(Node node)
        {
            List<Node> dependentNodes = GetDependentPureNodes(node).ToList();
            List<Node> remainingNodes = new(dependentNodes);
            List<Node> sortedNodes = [];

            List<Node> newNodes = [];

            do
            {
                newNodes.Clear();

                foreach (Node evalNode in remainingNodes.Where(evalNode => evalNode.InputDataPins.All(inNode => inNode.IncomingPin?.Node.IsPure != true || sortedNodes.Contains(inNode.IncomingPin.Node))))
                {
                    // Check whether all of this node's dependencies have been evaluated
                    newNodes.Add(evalNode);
                }

                // Add newly found nodes
                foreach (Node newNode in newNodes)
                {
                    remainingNodes.Remove(newNode);
                }

                sortedNodes.AddRange(newNodes);
            }
            while (newNodes.Count > 0 && remainingNodes.Count > 0);

            Debug.Assert(remainingNodes.Count == 0, "Impossible to evaluate all nodes (cyclic dependencies?)");

            return sortedNodes;
        }

        public static string FormatCode(string code)
        {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            try
            {
                SyntaxNode formatted = Formatter.Format(syntaxTree.GetCompilationUnitRoot(), new AdhocWorkspace()).NormalizeWhitespace();
                return formatted.ToFullString();
            }
            catch (Exception) { /* Nothing to do */ }
            return "";
        }
    }
}
