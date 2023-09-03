using NetPrints.Core;

namespace NetPrintsEditor.Messages
{
    /// <summary>
    /// Message for opening a graph.
    /// </summary>
    public class OpenGraphMessage(NodeGraph graph)
    {
        /// <summary>
        /// Graph to open.
        /// </summary>
        public NodeGraph Graph { get; } = graph;
    }
}
