using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.Windows.Threading;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;

using NetPrints.Core;
using NetPrints.Graph;
using NetPrints.Translator;

using NetPrintsEditor.Messages;

namespace NetPrintsEditor.ViewModels
{
    public class ClassEditorVM : ObservableObjectSerializable
    {
        public Project Project => Class?.Project;

        private NodeGraphVM m_graphVm;
        public NodeGraphVM OpenedGraph
        {
            get { return m_graphVm; }
            set
            {
                SetProperty(ref m_graphVm, value);
            }
        }

        // Wrapped attributes of Class
        public ObservableViewModelCollection<MemberVariableVM, Variable> Variables { get; set; }

        public ObservableViewModelCollection<NodeGraphVM, MethodGraph> Methods { get; set; }

        public ObservableViewModelCollection<NodeGraphVM, ConstructorGraph> Constructors { get; set; }

        /// <summary>
        /// Specifiers for methods that this class can override.
        /// </summary>
        public IEnumerable<MethodSpecifier> OverridableMethods =>
            Class.AllBaseTypes.SelectMany(type => App.ReflectionProvider.GetOverridableMethodsForType(type));

        public TypeSpecifier Type => Class?.Type();

        public string FullName => Class?.FullName;

        public string Namespace
        {
            get => Class.Namespace;
            set => Class.Namespace = value;
        }

        public string Name
        {
            get => Class?.Name;
            set => Class.Name = value;
        }

        public ClassModifiers Modifiers
        {
            get => Class?.Modifiers ?? ClassModifiers.None;
            set => Class.Modifiers = value;
        }

        public MemberVisibility Visibility
        {
            get => Class?.Visibility ?? MemberVisibility.Invalid;
            set => Class.Visibility = value;
        }

        public static IEnumerable<MemberVisibility> PossibleVisibilities => new[]
        {
            MemberVisibility.Internal,
            MemberVisibility.Private,
            MemberVisibility.Protected,
            MemberVisibility.Public,
        };

        private ClassGraph m_class;
        public ClassGraph Class
        {
            get { return m_class; }
            set
            {
                if (m_class != value)
                {
                    SetProperty(ref m_class, value);
                    OnClassChanged();
                }
            }
        }

        private string m_generatedCode;
        /// <summary>
        /// Generated code for the current class.
        /// </summary>
        public string GeneratedCode
        {
            get { return m_generatedCode; }
            set { SetProperty(ref m_generatedCode, value); }
        }

        private readonly ClassTranslator classTranslator = new();

        private readonly Timer codeTimer;

        public ClassEditorVM(ClassGraph cls)
        {
            WeakReferenceMessenger.Default.Register(this, new MessageHandler<object, OpenGraphMessage>((obj, message) => OnOpenGraphReceived(message)));

            Class = cls;

            codeTimer = new Timer(1000);
            codeTimer.Elapsed += CodeTimerThread;
            codeTimer.Start();
        }

        private void CodeTimerThread(object sender, EventArgs e)
        {
            codeTimer.Stop();

            string code;

            try
            {
                code = classTranslator.TranslateClass(Class);
            }
            catch (Exception ex)
            {
                code = ex.ToString();
            }

            Dispatcher.CurrentDispatcher.Invoke(() => GeneratedCode = code);

            codeTimer.Start();
        }

        ~ClassEditorVM()
        {
            codeTimer?.Stop();
        }

        private void OnClassChanged()
        {
            Methods = new ObservableViewModelCollection<NodeGraphVM, MethodGraph>(
                Class.Methods, m => new NodeGraphVM(m) { Class = this });

            Constructors = new ObservableViewModelCollection<NodeGraphVM, ConstructorGraph>(
                Class.Constructors, c => new NodeGraphVM(c) { Class = this });

            Variables = new ObservableViewModelCollection<MemberVariableVM, Variable>(
                Class.Variables, v => new MemberVariableVM(v));
        }

        private void OnOpenGraphReceived(OpenGraphMessage msg)
        {
            OpenedGraph = new NodeGraphVM(msg.Graph) { Class = this };
        }

        public static void OpenGraph(NodeGraph graph)
        {
            WeakReferenceMessenger.Default.Send(new OpenGraphMessage(graph));
        }

        public void OpenClassGraph()
        {
            WeakReferenceMessenger.Default.Send(new OpenGraphMessage(Class));
        }

        public void CreateConstructor(double gridCellSize)
        {
            ConstructorGraph newConstructor = new()
            {
                Class = Class,
                Visibility = MemberVisibility.Public,
            };

            newConstructor.EntryNode.PositionX = gridCellSize * 4;
            newConstructor.EntryNode.PositionY = gridCellSize * 4;

            Class.Constructors.Add(newConstructor);

            OpenGraph(newConstructor);
        }

        public void CreateMethod(string name, double gridCellSize)
        {
            MethodGraph newMethod = new(name)
            {
                Class = Class,
            };

            newMethod.EntryNode.PositionX = gridCellSize * 4;
            newMethod.EntryNode.PositionY = gridCellSize * 4;
            newMethod.ReturnNodes.First().PositionX = newMethod.EntryNode.PositionX + gridCellSize * 15;
            newMethod.ReturnNodes.First().PositionY = newMethod.EntryNode.PositionY;
            GraphUtil.ConnectExecPins(newMethod.EntryNode.InitialExecutionPin, newMethod.MainReturnNode.ReturnPin);

            Class.Methods.Add(newMethod);

            OpenGraph(newMethod);
        }

        public void CreateOverrideMethod(MethodSpecifier methodSpecifier)
        {
            MethodGraph methodGraph = GraphUtil.AddOverrideMethod(Class, methodSpecifier);

            if (methodGraph != null)
            {
                OpenGraph(methodGraph);
            }
        }
    }
}
