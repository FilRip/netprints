using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;

using NetPrints.Core;
using NetPrints.Graph;

using NetPrintsEditor.Controls;
using NetPrintsEditor.Dialogs;
using NetPrintsEditor.Messages;
using NetPrintsEditor.Reflection;

namespace NetPrintsEditor.ViewModels
{
    public class NodeGraphVM : ObservableObjectSerializable
    {
        public IEnumerable<SearchableComboBoxItem> Suggestions
        {
            get => SuggestionViewModel.Items;
            private set
            {
                SuggestionViewModel.Items = value;
                OnPropertyChanged(nameof(Suggestions));
            }
        }

        private NodePin m_nodePin;
        /// <summary>
        /// Pin that was dragged to generate suggestions.
        /// Null if that suggestions were not created for a pin.
        /// </summary>
        public NodePin SuggestionPin
        {
            get { return m_nodePin; }
            set
            {
                SetProperty(ref m_nodePin, value);
            }
        }

        private readonly Dictionary<Type, List<object>> builtInNodes = new()
        {
            [typeof(MethodGraph)] = new List<object>()
            {
                TypeSpecifier.FromType<ForLoopNode>(),
                TypeSpecifier.FromType<IfElseNode>(),
                TypeSpecifier.FromType<ConstructorNode>(),
                TypeSpecifier.FromType<TypeOfNode>(),
                TypeSpecifier.FromType<ExplicitCastNode>(),
                TypeSpecifier.FromType<ReturnNode>(),
                TypeSpecifier.FromType<MakeArrayNode>(),
                TypeSpecifier.FromType<LiteralNode>(),
                TypeSpecifier.FromType<TypeNode>(),
                TypeSpecifier.FromType<MakeArrayTypeNode>(),
                TypeSpecifier.FromType<ThrowNode>(),
                TypeSpecifier.FromType<AwaitNode>(),
                TypeSpecifier.FromType<TernaryNode>(),
                TypeSpecifier.FromType<DefaultNode>(),
            },
            [typeof(ConstructorGraph)] = new List<object>()
            {
                TypeSpecifier.FromType<ForLoopNode>(),
                TypeSpecifier.FromType<IfElseNode>(),
                TypeSpecifier.FromType<ConstructorNode>(),
                TypeSpecifier.FromType<TypeOfNode>(),
                TypeSpecifier.FromType<ExplicitCastNode>(),
                TypeSpecifier.FromType<MakeArrayNode>(),
                TypeSpecifier.FromType<LiteralNode>(),
                TypeSpecifier.FromType<TypeNode>(),
                TypeSpecifier.FromType<MakeArrayTypeNode>(),
                TypeSpecifier.FromType<ThrowNode>(),
                TypeSpecifier.FromType<TernaryNode>(),
                TypeSpecifier.FromType<DefaultNode>(),
            },
            [typeof(ClassGraph)] = new List<object>()
            {
                TypeSpecifier.FromType<TypeNode>(),
                TypeSpecifier.FromType<MakeArrayTypeNode>(),
            },
        };

        public SuggestionListVM SuggestionViewModel { get; } = new SuggestionListVM();

        public event EventHandler OnHideContextMenu;

        private List<object> GetBuiltInNodes(NodeGraph graph)
        {
            if (builtInNodes.TryGetValue(graph.GetType(), out List<object> nodes))
            {
                return nodes;
            }

            return new List<object>();
        }

        public void UpdateSuggestions(double mouseX, double mouseY)
        {
            SuggestionViewModel.Graph = Graph;
            SuggestionViewModel.PositionX = mouseX;
            SuggestionViewModel.PositionY = mouseY;
            SuggestionViewModel.SuggestionPin = SuggestionPin;
            SuggestionViewModel.HideContextMenu = () => OnHideContextMenu?.Invoke(this, EventArgs.Empty);

            // Show all relevant methods for the type of the pin
            IEnumerable<(string, object)> suggestions = Array.Empty<(string, object)>();

            void AddSuggestionsWithCategory(string category, IEnumerable<object> newSuggestions)
            {
                suggestions = suggestions.Concat(newSuggestions.Select(suggestion => (category, suggestion)));
            }

            if (SuggestionPin != null)
            {
                if (SuggestionPin is NodeOutputDataPin odp)
                {
                    if (odp.PinType.Value is TypeSpecifier pinTypeSpec)
                    {
                        // Add make delegate
                        AddSuggestionsWithCategory("NetPrints", new[] { new MakeDelegateTypeInfo(pinTypeSpec, Graph.Class.Type()) });

                        // Add variables and methods of the pin type
                        AddSuggestionsWithCategory("Pin Variables",
                            App.ReflectionProvider.GetVariables(
                                new ReflectionProviderVariableQuery()
                                    .WithType(pinTypeSpec)
                                    .WithVisibleFrom(Graph.Class.Type())
                                    .WithStatic(false)));

                        AddSuggestionsWithCategory("Pin Methods", App.ReflectionProvider.GetMethods(
                            new ReflectionProviderMethodQuery()
                                .WithVisibleFrom(Graph.Class.Type())
                                .WithStatic(false)
                                .WithType(pinTypeSpec)));

                        // Add methods of the base types that can accept the pin type as argument
                        foreach (TypeSpecifier baseType in Graph.Class.AllBaseTypes)
                        {
                            AddSuggestionsWithCategory("This Methods", App.ReflectionProvider.GetMethods(
                                new ReflectionProviderMethodQuery()
                                    .WithVisibleFrom(Graph.Class.Type())
                                    .WithStatic(false)
                                    .WithArgumentType(pinTypeSpec)
                                    .WithType(baseType)));
                        }

                        // Add static functions taking the type of the pin
                        AddSuggestionsWithCategory("Static Methods", App.ReflectionProvider.GetMethods(
                            new ReflectionProviderMethodQuery()
                                .WithArgumentType(pinTypeSpec)
                                .WithVisibleFrom(Graph.Class.Type())
                                .WithStatic(true)));
                    }
                }
                else if (SuggestionPin is NodeInputDataPin idp)
                {
                    if (idp.PinType.Value is TypeSpecifier pinTypeSpec)
                    {
                        // Variables of base classes that inherit from needed type
                        foreach (TypeSpecifier baseType in Graph.Class.AllBaseTypes)
                        {
                            AddSuggestionsWithCategory("This Variables", App.ReflectionProvider.GetVariables(
                                new ReflectionProviderVariableQuery()
                                    .WithType(baseType)
                                    .WithVisibleFrom(Graph.Class.Type())
                                    .WithVariableType(pinTypeSpec, true)));
                        }

                        // Add static functions returning the type of the pin
                        AddSuggestionsWithCategory("Static Methods", App.ReflectionProvider.GetMethods(
                            new ReflectionProviderMethodQuery()
                                .WithStatic(true)
                                .WithVisibleFrom(Graph.Class.Type())
                                .WithReturnType(pinTypeSpec)));
                    }
                }
                else if (SuggestionPin is NodeOutputExecPin oxp)
                {
                    GraphUtil.DisconnectOutputExecPin(oxp);

                    AddSuggestionsWithCategory("NetPrints", GetBuiltInNodes(Graph));

                    foreach (TypeSpecifier baseType in Graph.Class.AllBaseTypes)
                    {
                        AddSuggestionsWithCategory("This Methods", App.ReflectionProvider.GetMethods(
                            new ReflectionProviderMethodQuery()
                                .WithType(baseType)
                                .WithStatic(false)
                                .WithVisibleFrom(Graph.Class.Type())));
                    }

                    AddSuggestionsWithCategory("Static Methods", App.ReflectionProvider.GetMethods(
                        new ReflectionProviderMethodQuery()
                            .WithStatic(true)
                            .WithVisibleFrom(Graph.Class.Type())));
                }
                else if (SuggestionPin is NodeInputExecPin)
                {
                    AddSuggestionsWithCategory("NetPrints", GetBuiltInNodes(Graph));

                    foreach (TypeSpecifier baseType in Graph.Class.AllBaseTypes)
                    {
                        AddSuggestionsWithCategory("This Methods", App.ReflectionProvider.GetMethods(
                        new ReflectionProviderMethodQuery()
                            .WithType(baseType)
                            .WithStatic(false)
                            .WithVisibleFrom(Graph.Class.Type())));
                    }

                    AddSuggestionsWithCategory("Static Methods", App.ReflectionProvider.GetMethods(
                        new ReflectionProviderMethodQuery()
                            .WithStatic(true)
                            .WithVisibleFrom(Graph.Class.Type())));
                }
                else if (SuggestionPin is NodeInputTypePin)
                {
                    // TODO: Consider static types
                    AddSuggestionsWithCategory("Types", App.ReflectionProvider.GetNonStaticTypes());
                }
                else if (SuggestionPin is NodeOutputTypePin otp)
                {
                    if (Graph is ExecutionGraph && otp.InferredType.Value is TypeSpecifier typeSpecifier)
                    {
                        AddSuggestionsWithCategory("Pin Static Methods", App.ReflectionProvider
                            .GetMethods(new ReflectionProviderMethodQuery()
                                .WithType(typeSpecifier)
                                .WithStatic(true)
                                .WithVisibleFrom(Graph.Class.Type())));
                    }

                    // Types with type parameters
                    AddSuggestionsWithCategory("Generic Types", App.ReflectionProvider.GetNonStaticTypes()
                        .Where(t => t.GenericArguments.Any()));

                    if (Graph is ExecutionGraph)
                    {
                        // Public static methods that have type parameters
                        AddSuggestionsWithCategory("Generic Static Methods", App.ReflectionProvider
                            .GetMethods(new ReflectionProviderMethodQuery()
                                .WithStatic(true)
                                .WithHasGenericArguments(true)
                                .WithVisibleFrom(Graph.Class.Type())));
                    }
                }

                Suggestions = suggestions.Distinct().Select(x => new SearchableComboBoxItem(x.Item1, x.Item2));
            }
            else
            {
                AddSuggestionsWithCategory("NetPrints", GetBuiltInNodes(Graph));
                
                if (Graph is ExecutionGraph)
                {
                    // Get properties and methods of base class.
                    foreach (TypeSpecifier baseType in Graph.Class.AllBaseTypes)
                    {
                        AddSuggestionsWithCategory("This Variables", App.ReflectionProvider.GetVariables(
                        new ReflectionProviderVariableQuery()
                            .WithVisibleFrom(Graph.Class.Type())
                            .WithType(baseType)
                            .WithStatic(false)));

                        AddSuggestionsWithCategory("This Methods", App.ReflectionProvider.GetMethods(
                            new ReflectionProviderMethodQuery()
                                .WithType(baseType)
                                .WithVisibleFrom(Graph.Class.Type())
                                .WithStatic(false)));
                    }
                    
                    AddSuggestionsWithCategory("Static Methods", App.ReflectionProvider.GetMethods(
                        new ReflectionProviderMethodQuery()
                            .WithStatic(true)
                            .WithVisibleFrom(Graph.Class.Type())));
                    
                    AddSuggestionsWithCategory("Static Variables", App.ReflectionProvider.GetVariables(
                        new ReflectionProviderVariableQuery()
                            .WithStatic(true)
                            .WithVisibleFrom(Graph.Class.Type())));
                }
                else if (Graph is ClassGraph)
                {
                    AddSuggestionsWithCategory("Types", App.ReflectionProvider.GetNonStaticTypes());
                }
            }

            Suggestions = suggestions.Distinct().Select(x => new SearchableComboBoxItem(x.Item1, x.Item2));
        }

        public IEnumerable<NodeVM> SelectedNodes
        {
            get => selectedNodes;
            private set
            {
                if (selectedNodes != value)
                {
                    // Deselect old nodes
                    if (selectedNodes != null)
                    {
                        foreach (NodeVM node in selectedNodes)
                        {
                            node.IsSelected = false;
                        }
                    }

                    SetProperty(ref selectedNodes, value);

                    // Select new nodes
                    if (selectedNodes != null)
                    {
                        foreach (NodeVM node in selectedNodes)
                        {
                            node.IsSelected = true;
                        }
                    }
                }
            }
        }

        private IEnumerable<NodeVM> selectedNodes = Array.Empty<NodeVM>();

        public string Name
        {
            get => graph is MethodGraph methodGraph ? methodGraph.Name : graph.ToString();
            set
            {
                if (graph is MethodGraph methodGraph && methodGraph.Name != value)
                {
                    methodGraph.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public bool IsConstructor => graph is ConstructorGraph;

        private ObservableViewModelCollection<NodeVM, Node> m_nodes;
        public ObservableViewModelCollection<NodeVM, Node> Nodes
        {
            get
            {
                return m_nodes;
            }
            set
            {
                SetProperty(ref m_nodes, value);
            }
        }

        public IEnumerable<BaseType> ArgumentTypes =>
            graph is ExecutionGraph execGraph ? execGraph.ArgumentTypes() : null;

        public IEnumerable<Named<BaseType>> NamedArgumentTypes =>
            graph is ExecutionGraph execGraph ? execGraph.NamedArgumentTypes() : null;

        public IEnumerable<BaseType> ReturnTypes =>
            graph is MethodGraph methodGraph ? methodGraph.ReturnTypes() : null;

        private ClassEditorVM m_class;
        public ClassEditorVM Class
        {
            get
            {
                return m_class;
            }
            set
            {
                SetProperty(ref m_class, value);
            }
        }

        public MethodModifiers Modifiers
        {
            get => graph is MethodGraph methodGraph ? methodGraph.Modifiers : MethodModifiers.None;
            set
            {
                if (graph is MethodGraph methodGraph && methodGraph.Modifiers != value)
                {
                    methodGraph.Modifiers = value;
                    OnPropertyChanged(nameof(Modifiers));
                }
            }
        }

        public MemberVisibility Visibility
        {
            get => graph is ExecutionGraph execGraph ? execGraph.Visibility : MemberVisibility.Invalid;
            set
            {
                if (graph is ExecutionGraph execGraph && execGraph.Visibility != value)
                {
                    execGraph.Visibility = value;
                    OnPropertyChanged(nameof(Visibility));
                }
            }
        }

        public static IEnumerable<MemberVisibility> PossibleVisibilities => new[]
        {
            MemberVisibility.Internal,
            MemberVisibility.Private,
            MemberVisibility.Protected,
            MemberVisibility.Public,
        };

        /*
        Scenarios:
        
        Method changed [0]:
            (Un)assign (old) nodes changed events [1]
            (Un)assign (old)new pins changed events [2]
            (Un)assign (old)new pin connection changed events [3]
            (Un)setup (old)new method pins' connections in VM

        Node changed [1]:
            (Un)assign (old)new pins changed events [2]
            (Un)assign (old)new pin connection changed events [3]
            (Un)setup (old)new node pins' connections in VM

        Pin changed [2]:
            (Un)assign (old)new pin connection changed events [3]
            (Un)setup (old)new pin connection in VM

        Pin connection changed [3]:
            (Un)setup (old)new connection in VM
        */

        private void SetupNodeEvents(NodeVM node, bool add)
        {
            // (Un)assign (old)new pins changed events [2]
            if (add)
            {
                node.InputDataPins.CollectionChanged += OnPinCollectionChanged;
                node.OutputDataPins.CollectionChanged += OnPinCollectionChanged;
                node.InputExecPins.CollectionChanged += OnPinCollectionChanged;
                node.OutputExecPins.CollectionChanged += OnPinCollectionChanged;
                node.InputTypePins.CollectionChanged += OnPinCollectionChanged;
                node.OutputTypePins.CollectionChanged += OnPinCollectionChanged;

                node.OnDragStart += OnNodeDragStart;
                node.OnDragEnd += OnNodeDragEnd;
                node.OnDragMove += OnNodeDragMove;
            }
            else
            {
                node.InputDataPins.CollectionChanged -= OnPinCollectionChanged;
                node.OutputDataPins.CollectionChanged -= OnPinCollectionChanged;
                node.InputExecPins.CollectionChanged -= OnPinCollectionChanged;
                node.OutputExecPins.CollectionChanged -= OnPinCollectionChanged;
                node.InputTypePins.CollectionChanged -= OnPinCollectionChanged;
                node.OutputTypePins.CollectionChanged -= OnPinCollectionChanged;

                node.OnDragStart -= OnNodeDragStart;
                node.OnDragEnd -= OnNodeDragEnd;
                node.OnDragMove -= OnNodeDragMove;
            }

            // (Un)assign (old)new pin connection changed events [3]
            node.InputDataPins.ToList().ForEach(p => SetupPinEvents(p, add));
            node.OutputExecPins.ToList().ForEach(p => SetupPinEvents(p, add));
            node.InputTypePins.ToList().ForEach(p => SetupPinEvents(p, add));
        }

        #region Node dragging
        private double m_nodeDragScale = 1;
        public double NodeDragScale
        {
            get { return m_nodeDragScale; }
            set { SetProperty(ref m_nodeDragScale, value); }
        }

        private double nodeDragAccumX;
        private double nodeDragAccumY;

        private readonly Dictionary<NodeVM, (double X, double Y)> nodeStartPositions = new();

        /// <summary>
        /// Called when a node starts dragging.
        /// </summary>
        private void OnNodeDragStart(NodeVM node)
        {
            nodeDragAccumX = 0;
            nodeDragAccumY = 0;
            nodeStartPositions.Clear();

            // Remember the initial positions of the selected nodes
            if (SelectedNodes != null)
            {
                foreach (NodeVM selectedNode in SelectedNodes)
                {
                    nodeStartPositions.Add(selectedNode, (selectedNode.Node.PositionX, selectedNode.Node.PositionY));
                }
            }
        }

        /// <summary>
        /// Called when a node ends dragging.
        /// </summary>
        private void OnNodeDragEnd(NodeVM node)
        {
            // Snap final position to grid
            if (SelectedNodes != null)
            {
                foreach (Node selectedNode in SelectedNodes.Select(selectedNode => selectedNode.Node))
                {
                    selectedNode.PositionX -= selectedNode.PositionX % GraphEditorView.GridCellSize;
                    selectedNode.PositionY -= selectedNode.PositionY % GraphEditorView.GridCellSize;
                }
            }
        }

        /// <summary>
        /// Called when a node is dragging.
        /// </summary>
        private void OnNodeDragMove(NodeVM node, double dx, double dy)
        {
            nodeDragAccumX += dx * NodeDragScale;
            nodeDragAccumY += dy * NodeDragScale;

            // Move all selected nodes
            if (SelectedNodes != null)
            {
                foreach (NodeVM selectedNode in SelectedNodes)
                {
                    // Set position by taking total delta and adding it to the initial position
                    selectedNode.Node.PositionX = nodeStartPositions[selectedNode].X + nodeDragAccumX - nodeDragAccumX % GraphEditorView.GridCellSize;
                    selectedNode.Node.PositionY = nodeStartPositions[selectedNode].Y + nodeDragAccumY - nodeDragAccumY % GraphEditorView.GridCellSize;
                }
            }
        }
        #endregion

        private void SetupPinEvents(NodePinVM pin, bool add)
        {
            // (Un)assign (old)new pin connection changed events [3]

            if (pin.Pin is NodeInputDataPin idp)
            {
                if (add)
                {
                    idp.IncomingPinChanged += OnInputDataPinIncomingPinChanged;
                }
                else
                {
                    idp.IncomingPinChanged -= OnInputDataPinIncomingPinChanged;
                }
            }
            else if (pin.Pin is NodeOutputExecPin oxp)
            {
                if (add)
                {
                    oxp.OutgoingPinChanged += OnOutputExecPinIncomingPinChanged;
                }
                else
                {
                    oxp.OutgoingPinChanged -= OnOutputExecPinIncomingPinChanged;
                }
            }
            else if (pin.Pin is NodeInputTypePin itp)
            {
                if (add)
                {
                    itp.IncomingPinChanged += OnInputTypePinIncomingPinChanged;
                }
                else
                {
                    itp.IncomingPinChanged -= OnInputTypePinIncomingPinChanged;
                }
            }
        }

        private void SetupPinConnection(NodePinVM pin, bool add)
        {
            if (pin.Pin is NodeInputDataPin idp)
            {
                if (idp.IncomingPin != null)
                {
                    if (add)
                    {
                        NodeOutputDataPin connPin = idp.IncomingPin;
                        pin.ConnectedPin = Nodes
                            .SingleOrDefault(n => n.Node == connPin.Node)
                            ?.OutputDataPins
                            ?.Single(x => x.Pin == connPin);
                    }
                    else
                    {
                        pin.ConnectedPin = null;
                    }
                }
            }
            else if (pin.Pin is NodeOutputExecPin oxp)
            {
                if (oxp.OutgoingPin != null)
                {
                    if (add)
                    {
                        NodeInputExecPin connPin = oxp.OutgoingPin;
                        pin.ConnectedPin = Nodes
                            .Single(n => n.Node == connPin.Node)
                            .InputExecPins
                            .Single(x => x.Pin == connPin);
                    }
                    else
                    {
                        pin.ConnectedPin = null;
                    }
                }
            }
            else if (pin.Pin is NodeInputTypePin itp && itp.IncomingPin != null)
            {
                if (add)
                {
                    NodeOutputTypePin connPin = itp.IncomingPin;
                    pin.ConnectedPin = Nodes
                        .Single(n => n.Node == connPin.Node)
                        .OutputTypePins
                        .Single(x => x.Pin == connPin);
                }
                else
                {
                    pin.ConnectedPin = null;
                }
            }
        }

        private void SetupNodeConnections(NodeVM node, bool add)
        {
            node.OutputExecPins.ToList().ForEach(p => SetupPinConnection(p, add));
            node.InputDataPins.ToList().ForEach(p => SetupPinConnection(p, add));
            node.InputTypePins.ToList().ForEach(p => SetupPinConnection(p, add));

            // Make sure to remove the IXP and ODP connections 
            // any other nodes are connecting to too

            if (!add)
            {
                AllPins.Where(p =>
                    node.InputExecPins.Contains(p.ConnectedPin)
                    || node.OutputDataPins.Contains(p.ConnectedPin)
                    || node.OutputTypePins.Contains(p.ConnectedPin)
                ).ToList().ForEach(p => p.ConnectedPin = null);
            }
        }

        public NodeGraph Graph
        {
            get => graph;
            set
            {
                if (graph != value)
                {
                    if (graph != null)
                    {
                        Nodes.CollectionChanged -= OnNodeCollectionChanged;
                        Nodes.ToList().ForEach(n => SetupNodeEvents(n, false));

                        Nodes.ToList().ForEach(n => SetupNodeConnections(n, false));
                    }

                    SetProperty(ref graph, value);
                    OnPropertyChanged(nameof(AllPins));
                    OnPropertyChanged(nameof(Visibility));

                    Nodes = new ObservableViewModelCollection<NodeVM, Node>(Graph.Nodes, n => new NodeVM(n));

                    if (graph != null)
                    {
                        Nodes.CollectionChanged += OnNodeCollectionChanged;
                        Nodes.ToList().ForEach(n => SetupNodeEvents(n, true));

                        // Connections on normal pins already exist,
                        // Set them up for the viewmodels of the pins
                        Nodes.ToList().ForEach(n => SetupNodeConnections(n, true));
                    }
                }
            }
        }

        private NodeGraph graph;

        private void OnNodeCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                List<NodeVM> removedNodes = e.OldItems.Cast<NodeVM>().ToList();
                removedNodes.ToList().ForEach(n => SetupNodeEvents(n, false));

                removedNodes.ToList().ForEach(n => SetupNodeConnections(n, false));
            }

            if (e.NewItems != null)
            {
                List<NodeVM> addedNodes = e.NewItems.Cast<NodeVM>().ToList();
                addedNodes.ToList().ForEach(n => SetupNodeEvents(n, true));

                addedNodes.ToList().ForEach(n => SetupNodeConnections(n, true));
            }

            OnPropertyChanged(nameof(AllPins));
        }

        private void OnPinCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                List<NodePinVM> removedPins = e.OldItems.Cast<NodePinVM>().ToList();

                // Unsetup initial connections of added pins
                removedPins.ToList().ForEach(p => SetupPinConnection(p, false));

                // Remove events for removed pins
                removedPins.ToList().ForEach(p => SetupPinEvents(p, false));
            }

            if (e.NewItems != null)
            {
                List<NodePinVM> addedPins = e.NewItems.Cast<NodePinVM>().ToList();

                // Setup initial connections of added pins
                addedPins.ToList().ForEach(p => SetupPinConnection(p, true));

                // Add events for added pins
                addedPins.ToList().ForEach(p => SetupPinEvents(p, true));
            }

            OnPropertyChanged(nameof(AllPins));
        }

        private void OnInputDataPinIncomingPinChanged(NodeInputDataPin pin, NodeOutputDataPin oldPin, NodeOutputDataPin newPin)
        {
            // Connect pinVM newPinVM (or null if newPin is null)

            NodePinVM pinVM = FindPinVMFromPin(pin);
            pinVM.ConnectedPin = newPin == null ? null : FindPinVMFromPin(newPin);
        }

        private void OnOutputExecPinIncomingPinChanged(NodeOutputExecPin pin, NodeInputExecPin oldPin, NodeInputExecPin newPin)
        {
            // Connect pinVM newPinVM (or null if newPin is null)

            NodePinVM pinVM = FindPinVMFromPin(pin);
            pinVM.ConnectedPin = newPin == null ? null : FindPinVMFromPin(newPin);
        }

        private void OnInputTypePinIncomingPinChanged(NodeInputTypePin pin, NodeOutputTypePin oldPin, NodeOutputTypePin newPin)
        {
            // Connect pinVM newPinVM (or null if newPin is null)

            NodePinVM pinVM = FindPinVMFromPin(pin);
            if (pinVM != null)
            {
                pinVM.ConnectedPin = newPin == null ? null : FindPinVMFromPin(newPin);
            }
        }

        private NodePinVM FindPinVMFromPin(NodePin pin)
        {
            return AllPins.SingleOrDefault(p => p.Pin == pin);
        }

        public IEnumerable<NodePinVM> AllPins
        {
            get
            {
                List<NodePinVM> pins = new();

                if (Graph != null)
                {
                    foreach (NodeVM node in Nodes)
                    {
                        pins.AddRange(node.InputDataPins);
                        pins.AddRange(node.OutputDataPins);
                        pins.AddRange(node.InputExecPins);
                        pins.AddRange(node.OutputExecPins);
                        pins.AddRange(node.InputTypePins);
                        pins.AddRange(node.OutputTypePins);
                    }
                }

                return pins;
            }
        }

        /// <summary>
        /// Sends a message to deselect all nodes and select the given nodes.
        /// </summary>
        /// <param name="nodes">Nodes to be selected.</param>
        public static void SelectNodes(IEnumerable<NodeVM> nodes)
        {
            WeakReferenceMessenger.Default.Send(new NodeSelectionMessage(nodes, true));
        }

        /// <summary>
        /// Sends a message to deselect all nodes.
        /// </summary>
        public static void DeselectNodes()
        {
            WeakReferenceMessenger.Default.Send(NodeSelectionMessage.DeselectAll);
        }

        public NodeGraphVM(NodeGraph graph)
        {
            Graph = graph;

            WeakReferenceMessenger.Default.Register(this, new MessageHandler<object, NodeSelectionMessage>((obj, message) => OnNodeSelectionReceived(message)));
            WeakReferenceMessenger.Default.Register(this, new MessageHandler<object, AddNodeMessage>((obj, message) => OnAddNodeReceived(message)));
        }

        private void OnNodeSelectionReceived(NodeSelectionMessage msg)
        {
            if (msg.DeselectPrevious)
            {
                SelectedNodes = Array.Empty<NodeVM>();
            }

            SelectedNodes = SelectedNodes.Concat(msg.Nodes).Distinct();
        }

        private void OnAddNodeReceived(AddNodeMessage msg)
        {
            if (!msg.Handled && msg.Graph == Graph)
            {
                msg.Handled = true;

                // Make sure the node will be on the canvas
                if (msg.PositionX < 0)
                    msg.PositionX = 0;

                if (msg.PositionY < 0)
                    msg.PositionY = 0;

                object[] parameters = new object[] { msg.Graph }.Concat(msg.ConstructorParameters).ToArray();
                Node node = Activator.CreateInstance(msg.NodeType, parameters) as Node;
                node.PositionX = msg.PositionX;
                node.PositionY = msg.PositionY;

                // If the node was created as part of a suggestion, connect it
                // to the relevant node pin.
                if (msg.SuggestionPin != null)
                {
                    GraphUtil.ConnectRelevantPins(msg.SuggestionPin,
                        node, App.ReflectionProvider.TypeSpecifierIsSubclassOf,
                        App.ReflectionProvider.HasImplicitCast);
                }
            }
        }

        public void AddNode(AddNodeMessage msg) => OnAddNodeReceived(msg);
    }
}
