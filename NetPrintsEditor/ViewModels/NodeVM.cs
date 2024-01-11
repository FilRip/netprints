using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Media;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;

using NetPrints.Core;
using NetPrints.Graph;

using NetPrintsEditor.Messages;

namespace NetPrintsEditor.ViewModels
{
    public class NodeVM : ObservableObjectSerializable
    {
        private static readonly SolidColorBrush DefaultNodeBrush =
            new(Color.FromArgb(0xFF, 0x30, 0x30, 0x30));

        private static readonly SolidColorBrush EntryNodeBrush =
            new(Color.FromArgb(0xFF, 0x20, 0x20, 0x50));

        private static readonly SolidColorBrush ReturnNodeBrush =
            new(Color.FromArgb(0xFF, 0x50, 0x20, 0x20));

        private static readonly SolidColorBrush CallMethodBrush =
            new(Color.FromArgb(0xFF, 0x20, 0x3A, 0x50));

        private static readonly SolidColorBrush CallStaticFunctionBrush =
            new(Color.FromArgb(0xFF, 0x50, 0x20, 0x3A));

        private static readonly SolidColorBrush ConstructorNodeBrush =
            new(Color.FromArgb(0xFF, 0x3A, 0x50, 0x20));

        private static readonly SolidColorBrush MakeDelegateNodeBrush =
            new(Color.FromArgb(0xFF, 0x7A, 0x7A, 0x20));

        private static readonly SolidColorBrush TypeNodeBrush =
            new(Color.FromArgb(0xFF, 0x7A, 0x30, 0x20));

        private static readonly SolidColorBrush VariableGetterBrush =
            new(Color.FromArgb(0xFF, 0x30, 0x5A, 0x5A));

        private static readonly SolidColorBrush VariableSetterBrush =
            new(Color.FromArgb(0xFF, 0x5A, 0x5A, 0x7A));

        private static readonly SolidColorBrush MakeArrayBrush =
            new(Color.FromArgb(0xFF, 0x1A, 0x5A, 0x30));

        private static readonly SolidColorBrush ThrowBrush =
            new(Color.FromArgb(0xFF, 0xBB, 0x20, 0x20));

        private static readonly SolidColorBrush TernaryBrush =
            new(Color.FromArgb(0xFF, 0x40, 0x3A, 0x3A));

        /// <summary>
        /// Brush for the header of the node.
        /// </summary>
        public SolidColorBrush Brush
        {
            get
            {
                if (Node is ExecutionEntryNode)
                {
                    return EntryNodeBrush;
                }
                else if (Node is ReturnNode)
                {
                    return ReturnNodeBrush;
                }
                else if (Node is CallMethodNode callMethodNode)
                {
                    if (callMethodNode.IsStatic)
                    {
                        return CallStaticFunctionBrush;
                    }
                    else
                    {
                        return CallMethodBrush;
                    }
                }
                else if (Node is ConstructorNode)
                {
                    return ConstructorNodeBrush;
                }
                else if (Node is MakeDelegateNode)
                {
                    return MakeDelegateNodeBrush;
                }
                else if (Node is TypeNode || Node is MakeArrayTypeNode)
                {
                    return TypeNodeBrush;
                }
                else if (Node is VariableGetterNode)
                {
                    return VariableGetterBrush;
                }
                else if (Node is VariableSetterNode)
                {
                    return VariableSetterBrush;
                }
                else if (Node is MakeArrayNode)
                {
                    return MakeArrayBrush;
                }
                else if (Node is ThrowNode)
                {
                    return ThrowBrush;
                }
                else if (Node is TernaryNode)
                {
                    return TernaryBrush;
                }

                return DefaultNodeBrush;
            }
        }

        private static readonly SolidColorBrush DeselectedBorderBrush =
            new(Color.FromArgb(0xCC, 0x30, 0x30, 0x30));

        private static readonly SolidColorBrush SelectedBorderBrush =
            new(Color.FromArgb(0xFF, 0x00, 0x99, 0x00));

        /// <summary>
        /// Brush for the border of the node.
        /// </summary>
        public SolidColorBrush BorderBrush
        {
            get => IsSelected ? SelectedBorderBrush : DeselectedBorderBrush;
        }

        public int ZIndex
        {
            get => IsSelected ? 1 : 0;
        }

        public string LeftPlusToolTip
        {
            get
            {
                if (node is MakeArrayNode)
                {
                    return "Add array element";
                }
                else if (node is MethodEntryNode)
                {
                    return "Add method parameter";
                }
                else if (node is ReturnNode)
                {
                    return "Add method return value";
                }
                else if (node is ClassReturnNode)
                {
                    return "Add interface";
                }

                return "";
            }
        }

        public string LeftMinusToolTip
        {
            get
            {
                if (node is MakeArrayNode)
                {
                    return "Remove array element";
                }
                else if (node is MethodEntryNode)
                {
                    return "Remove method parameter";
                }
                else if (node is ReturnNode)
                {
                    return "Remove method return value";
                }
                else if (node is ClassReturnNode)
                {
                    return "Remove interface";
                }

                return "";
            }
        }

        public string RightPlusToolTip
        {
            get
            {
                if (node is MethodEntryNode)
                {
                    return "Add method generic type parameter";
                }

                return "";
            }
        }

        public string RightMinusToolTip
        {
            get
            {
                if (node is MethodEntryNode)
                {
                    return "Remove method generic type parameter";
                }

                return "";
            }
        }

        /// <summary>
        /// Whether the node is currently selected.
        /// </summary>
        public bool IsSelected
        {
            get => isSelected;
            set
            {
                if (isSelected != value)
                {
                    SetProperty(ref isSelected, value);
                    OnPropertyChanged(nameof(BorderBrush));
                    OnPropertyChanged(nameof(ZIndex));
                }
            }
        }

        private bool isSelected;

        /// <summary>
        /// Whether this node is a reroute node.
        /// </summary>
        public bool IsRerouteNode
        {
            get => node is RerouteNode;
        }

        /// <summary>
        /// Tool tip of the node shown when hovering over it.
        /// </summary>
        public string ToolTip
        {
            get
            {
                if (Node is CallMethodNode callMethodNode)
                {
                    return App.ReflectionProvider.GetMethodDocumentation(callMethodNode.MethodSpecifier);
                }

                return null;
            }
        }

        // Wrapped attributes of Node

        /// <summary>
        /// Name of the node.
        /// </summary>
        public string Name
        {
            get => node.Name;
            set
            {
                if (node.Name != value)
                {
                    node.Name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        public ObservableViewModelCollection<NodePinVM, NodeInputDataPin> InputDataPins
        {
            get => inputDataPins;
            set
            {
                if (inputDataPins != value)
                {
                    SetProperty(ref inputDataPins, value);
                }
            }
        }

        public ObservableViewModelCollection<NodePinVM, NodeOutputDataPin> OutputDataPins
        {
            get => outputDataPins;
            set
            {
                if (outputDataPins != value)
                {
                    SetProperty(ref outputDataPins, value);
                }
            }
        }

        public ObservableViewModelCollection<NodePinVM, NodeInputExecPin> InputExecPins
        {
            get => inputExecPins;
            set
            {
                if (inputExecPins != value)
                {
                    SetProperty(ref inputExecPins, value);
                }
            }
        }

        public ObservableViewModelCollection<NodePinVM, NodeOutputExecPin> OutputExecPins
        {
            get => outputExecPins;
            set
            {
                if (outputExecPins != value)
                {
                    SetProperty(ref outputExecPins, value);
                }
            }
        }

        public ObservableViewModelCollection<NodePinVM, NodeInputTypePin> InputTypePins
        {
            get => inputTypePins;
            set
            {
                if (inputTypePins != value)
                {
                    SetProperty(ref inputTypePins, value);
                }
            }
        }

        public ObservableViewModelCollection<NodePinVM, NodeOutputTypePin> OutputTypePins
        {
            get => outputTypePins;
            set
            {
                if (outputTypePins != value)
                {
                    SetProperty(ref outputTypePins, value);
                }
            }
        }

        private ObservableViewModelCollection<NodePinVM, NodeInputDataPin> inputDataPins;
        private ObservableViewModelCollection<NodePinVM, NodeOutputDataPin> outputDataPins;
        private ObservableViewModelCollection<NodePinVM, NodeInputExecPin> inputExecPins;
        private ObservableViewModelCollection<NodePinVM, NodeOutputExecPin> outputExecPins;
        private ObservableViewModelCollection<NodePinVM, NodeInputTypePin> inputTypePins;
        private ObservableViewModelCollection<NodePinVM, NodeOutputTypePin> outputTypePins;

        public bool IsPure
        {
            get => node.IsPure;
            set => node.IsPure = value;
        }

        public bool CanSetPure
        {
            get => node.CanSetPure;
        }

        // Wrapped Node
        public Node Node
        {
            get => node;
            set
            {
                if (node != value)
                {
                    if (node != null)
                    {
                        node.InputTypeChanged -= OnInputTypeChanged;
                    }

                    SetProperty(ref node, value);

                    if (node != null)
                    {
                        node.InputTypeChanged += OnInputTypeChanged;

                        InputDataPins = new ObservableViewModelCollection<NodePinVM, NodeInputDataPin>(
                            Node.InputDataPins, p => new NodePinVM(p));

                        OutputDataPins = new ObservableViewModelCollection<NodePinVM, NodeOutputDataPin>(
                            Node.OutputDataPins, p => new NodePinVM(p));

                        InputExecPins = new ObservableViewModelCollection<NodePinVM, NodeInputExecPin>(
                            Node.InputExecPins, p => new NodePinVM(p));

                        OutputExecPins = new ObservableViewModelCollection<NodePinVM, NodeOutputExecPin>(
                            Node.OutputExecPins, p => new NodePinVM(p));

                        InputTypePins = new ObservableViewModelCollection<NodePinVM, NodeInputTypePin>(
                            Node.InputTypePins, p => new NodePinVM(p));

                        OutputTypePins = new ObservableViewModelCollection<NodePinVM, NodeOutputTypePin>(
                            Node.OutputTypePins, p => new NodePinVM(p));
                    }

                    UpdateOverloads();

                    OnPropertyChanged(nameof(Brush));
                    OnPropertyChanged(nameof(ToolTip));
                    OnPropertyChanged(nameof(IsRerouteNode));
                    OnPropertyChanged(nameof(ShowLeftPinButtons));
                    OnPropertyChanged(nameof(ShowRightPinButtons));
                    OnPropertyChanged(nameof(LeftPlusToolTip));
                    OnPropertyChanged(nameof(LeftMinusToolTip));
                    OnPropertyChanged(nameof(RightPlusToolTip));
                    OnPropertyChanged(nameof(RightMinusToolTip));
                    OnPropertyChanged(nameof(Label));
                    OnPropertyChanged(nameof(IsPure));
                    OnPropertyChanged(nameof(CanSetPure));
                }
            }
        }

        private void OnInputTypeChanged(object sender, EventArgs e)
        {
            OnPropertyChanged(nameof(Label));
        }

        public string Label
        {
            get => Node.ToString();
        }

        /// <summary>
        /// Overloads for Constructor and CallMethod nodes
        /// </summary>
        public ObservableRangeCollection<object> Overloads
        {
            get => overloads;
            set
            {
                SetProperty(ref overloads, value);
                OnPropertyChanged(nameof(ShowOverloads));
            }
        }

        private ObservableRangeCollection<object> overloads = [];

        /// <summary>
        /// Whether to show the overloads element.
        /// </summary>
        public bool ShowOverloads
        {
            get => Overloads.Count > 0;
        }

        /// <summary>
        /// Currently overload or null if invalid for the node type.
        /// </summary>
        public object CurrentOverload
        {
            get
            {
                if (Node is CallMethodNode callMethodNode)
                {
                    return callMethodNode.MethodSpecifier;
                }
                else if (Node is ConstructorNode constructorNode)
                {
                    return constructorNode.ConstructorSpecifier;
                }
                else if (Node is MakeArrayNode makeArrayNode)
                {
                    return makeArrayNode.UsePredefinedSize ? "Use initializer list" : "Use predefined size";
                }

                return null;
            }
        }

        /// <summary>
        /// Changes the called method of this node if it is a CallMethodNode.
        /// Throws an exception if it is not.
        /// </summary>
        /// <param name="methodSpecifier">Method to change to.</param>
        public void ChangeOverload(object overload)
        {
            Node newNode = null;
            if (overload is MethodSpecifier methodSpecifier && Node is CallMethodNode)
            {
                newNode = new CallMethodNode(Node.Graph, methodSpecifier);
            }
            else if (overload is ConstructorSpecifier constructorSpecifier && Node is ConstructorNode)
            {
                newNode = new ConstructorNode(Node.Graph, constructorSpecifier);
            }
            else if (overload is string overloadString && Node is MakeArrayNode makeArrayNode)
            {
                makeArrayNode.UsePredefinedSize = string.Equals(overloadString, "Use predefined size", StringComparison.OrdinalIgnoreCase);
                UpdateOverloads();
            }
            else
            {
                throw new NetPrints.Exceptions.NetPrintsException("Tried to change overload for underlying node even though it does not support overloads.");
            }

            if (newNode != null)
            {
                // Remember old exec pins to reconnect them.
                // Data pin's are trickier to reconnect (or impossible).
                // They could be reconnected by heuristics (eg. name, type etc.).

                NodeOutputExecPin[] oldIncomingPins = null;
                NodeInputExecPin oldOutgoingPin = null;

                bool oldPurity = Node.IsPure;
                if (!oldPurity)
                {
                    oldIncomingPins = Node.InputExecPins[0].IncomingPins.ToArray();
                    oldOutgoingPin = Node.OutputExecPins[0].OutgoingPin;
                }

                // Disconnect the old node from other nodes and remove it
                GraphUtil.DisconnectNodePins(Node);
                Node.Graph.Nodes.Remove(Node);

                // Move the new node to the same location
                newNode.PositionX = Node.PositionX;
                newNode.PositionY = Node.PositionY;

                // Restore old purity
                if (newNode.CanSetPure)
                {
                    newNode.IsPure = oldPurity;
                }

                // Reconnect execution pins
                if (!newNode.IsPure)
                {
                    if (oldOutgoingPin != null)
                    {
                        GraphUtil.ConnectExecPins(newNode.OutputExecPins[0], oldOutgoingPin);
                    }

                    if (oldIncomingPins != null)
                    {
                        foreach (NodeOutputExecPin oldIncomingPin in oldIncomingPins)
                        {
                            GraphUtil.ConnectExecPins(oldIncomingPin, newNode.InputExecPins[0]);
                        }
                    }
                }

                // Set the node of this view model which will trigger an update
                Node = newNode;
            }
        }

        public MethodGraph Method
        {
            get => node.MethodGraph;
        }

        public NodeGraph Graph
        {
            get => node.Graph;
        }

        /// <summary>
        /// Whether we should show the +/- buttons under the
        /// left pins.
        /// </summary>
        public bool ShowLeftPinButtons
        {
            get => node is MakeArrayNode || node is MethodEntryNode || (node is ReturnNode && node == Method.MainReturnNode) || node is ClassReturnNode;
        }

        /// <summary>
        /// Whether we should show the +/- buttons under the
        /// right pins.
        /// </summary>
        public bool ShowRightPinButtons
        {
            get => node is MethodEntryNode;
        }

        /// <summary>
        /// Called when the left pins' plus button was clicked.
        /// </summary>
        public void LeftPinsPlusClicked()
        {
            if (node is MakeArrayNode makeArrayNode)
            {
                makeArrayNode.AddElementPin();
            }
            else if (node is MethodEntryNode entryNode)
            {
                entryNode.AddArgument();
            }
            else if (node is ReturnNode returnNode)
            {
                returnNode.AddReturnType();
            }
            else if (node is ClassReturnNode classReturnNode)
            {
                classReturnNode.AddInterfacePin();
            }
        }

        /// <summary>
        /// Called when the left pins' minus button was clicked.
        /// </summary>
        public void LeftPinsMinusClicked()
        {
            if (node is MakeArrayNode makeArrayNode)
            {
                makeArrayNode.RemoveElementPin();
            }
            else if (node is MethodEntryNode entryNode)
            {
                entryNode.RemoveArgument();
            }
            else if (node is ReturnNode returnNode)
            {
                returnNode.RemoveReturnType();
            }
            else if (node is ClassReturnNode classReturnNode)
            {
                classReturnNode.RemoveInterfacePin();
            }
        }

        /// <summary>
        /// Called when the right pins' plus button was clicked.
        /// </summary>
        public void RightPinsPlusClicked()
        {
            if (node is MethodEntryNode entryNode)
            {
                entryNode.AddGenericArgument();
            }
        }

        /// <summary>
        /// Called when the right pins' minus button was clicked.
        /// </summary>
        public void RightPinsMinusClicked()
        {
            if (node is MethodEntryNode entryNode)
            {
                entryNode.RemoveGenericArgument();
            }
        }

        private Node node;

        public NodeVM(Node node)
        {
            PropertyChanged += OnPropertyChanged;
            Node = node;
        }

        private void UpdateOverloads()
        {
            // Get the new overloads. Exclude the current method.
            if (node is CallMethodNode callMethodNode && callMethodNode.MethodSpecifier != null)
            {
                Overloads.ReplaceRange(App.ReflectionProvider
                    .GetPublicMethodOverloads(callMethodNode.MethodSpecifier)
                    .Except(new MethodSpecifier[] { callMethodNode.MethodSpecifier }));
            }
            else if (node is ConstructorNode constructorNode && constructorNode.ConstructorSpecifier != null)
            {
                Overloads.ReplaceRange(App.ReflectionProvider
                    .GetConstructors(constructorNode.ConstructorSpecifier.DeclaringType)
                    .Except(new ConstructorSpecifier[] { constructorNode.ConstructorSpecifier }));
            }
            else if (node is MakeArrayNode makeArrayNode)
            {
                if (makeArrayNode.UsePredefinedSize)
                {
                    Overloads.Replace("Use initializer list");
                }
                else
                {
                    Overloads.Replace("Use predefined size");
                }
            }
            else
            {
                Overloads.Clear();
            }

            OnPropertyChanged(nameof(ShowOverloads));
            OnPropertyChanged(nameof(Overloads));
        }

        #region Dragging
        public delegate void NodeDragStartEventHandler(NodeVM node);
        public delegate void NodeDragEndEventHandler(NodeVM node);
        public delegate void NodeDragMoveEventHandler(NodeVM node, double dx, double dy);
        public event NodeDragStartEventHandler OnDragStart;
        public event NodeDragEndEventHandler OnDragEnd;
        public event NodeDragMoveEventHandler OnDragMove;

        public void DragStart() => OnDragStart?.Invoke(this);
        public void DragEnd() => OnDragEnd?.Invoke(this);
        public void DragMove(double dx, double dy) => OnDragMove?.Invoke(this, dx, dy);
        #endregion

        private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // HACK: call UpdateOverloads() here because for some reason it is not
            //       updated correctly.
            //if (!e.PropertyName.Contains("overload", StringComparison.OrdinalIgnoreCase))
            if (!e.PropertyName.Contains("Overload"))
            {
                UpdateOverloads();
            }
        }

        public void Select()
        {
            WeakReferenceMessenger.Default.Send(new NodeSelectionMessage(this));
        }
    }
}
