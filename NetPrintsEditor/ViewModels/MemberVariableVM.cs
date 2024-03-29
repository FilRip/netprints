﻿using System;
using System.Collections.Generic;

using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Messaging;

using NetPrints.Core;
using NetPrints.Graph;

using NetPrintsEditor.Messages;

namespace NetPrintsEditor.ViewModels
{
    public class MemberVariableVM(Variable variable) : ObservableObjectSerializable
    {
        public TypeSpecifier Type => Variable.Type;

        public string Name
        {
            get => Variable.Name;
            set => Variable.Name = value;
        }

        public VariableModifiers Modifiers
        {
            get => Variable.Modifiers;
            set => Variable.Modifiers = value;
        }

        public MemberVisibility Visibility
        {
            get => Variable.Visibility;
            set
            {
                if (Variable.Visibility != value)
                {
                    // Change visibility of accessors if it was the same as the visibility
                    // of the property itself.
                    // Ideally we would have a way to check if the visibility is user-set,
                    // for example by making the getter / setter visibility nullable.

                    if (Variable.GetterMethod != null && Variable.GetterMethod.Visibility == Variable.Visibility)
                    {
                        Variable.GetterMethod.Visibility = value;
                    }

                    if (Variable.SetterMethod != null && Variable.SetterMethod.Visibility == Variable.Visibility)
                    {
                        Variable.SetterMethod.Visibility = value;
                    }

                    Variable.Visibility = value;
                }
            }
        }

        public VariableSpecifier Specifier => Variable.Specifier;

        public bool HasGetter => Getter != null;

        public bool HasSetter => Setter != null;

        public MethodGraph Getter
        {
            get => Variable.GetterMethod;
            set => Variable.GetterMethod = value;
        }

        public MethodGraph Setter
        {
            get => Variable.SetterMethod;
            set => Variable.SetterMethod = value;
        }

        public string VisibilityName
        {
            get => Enum.GetName(typeof(MemberVisibility), Visibility);
            set => Visibility = (MemberVisibility)Enum.Parse(typeof(MemberVisibility), value);
        }

        public static IEnumerable<MemberVisibility> PossibleVisibilities => new[]
        {
            MemberVisibility.Internal,
            MemberVisibility.Private,
            MemberVisibility.Protected,
            MemberVisibility.Public,
        };

        public Variable Variable
        {
            get; set;
        } = variable;

        public void AddGetter()
        {
            MethodGraph method = new($"get_{Name}")
            {
                Class = Variable.Class,
                Visibility = Visibility
            };

            // Set position of entry and return node
            method.EntryNode.PositionX = 560;
            method.EntryNode.PositionY = 504;
            method.MainReturnNode.PositionX = method.EntryNode.PositionX + 672;
            method.MainReturnNode.PositionY = method.EntryNode.PositionY;

            // Connect entry and return node execution pins
            GraphUtil.ConnectExecPins(method.EntryNode.InitialExecutionPin, method.MainReturnNode.ReturnPin);

            // Create return input pin with correct type
            // TODO: Make sure we can't delete type pins.
            const int offsetX = -308;
            const int offsetY = -112;
            TypeNode returnTypeNode = GraphUtil.CreateNestedTypeNode(method, Type, method.MainReturnNode.PositionX + offsetX, method.MainReturnNode.PositionY + offsetY);
            method.MainReturnNode.AddReturnType();
            GraphUtil.ConnectTypePins(returnTypeNode.OutputTypePins[0], method.MainReturnNode.InputTypePins[0]);

            Getter = method;
        }

        public void RemoveGetter()
        {
            Getter = null;
        }

        public void AddSetter()
        {
            MethodGraph method = new($"set_{Name}")
            {
                Class = Variable.Class,
                Visibility = Visibility
            };

            // Set position of entry and return node
            method.EntryNode.PositionX = 560;
            method.EntryNode.PositionY = 504;
            method.MainReturnNode.PositionX = method.EntryNode.PositionX + 672;
            method.MainReturnNode.PositionY = method.EntryNode.PositionY;

            // Connect entry and return node execution pins
            GraphUtil.ConnectExecPins(method.EntryNode.InitialExecutionPin, method.MainReturnNode.ReturnPin);

            // Create argument output pin with correct type
            // TODO: Make sure we can't delete type pins.
            const int offsetX = -308;
            const int offsetY = -112;
            TypeNode argTypeNode = GraphUtil.CreateNestedTypeNode(method, Type, method.EntryNode.PositionX + offsetX, method.EntryNode.PositionY + offsetY);
            method.MethodEntryNode.AddArgument();
            GraphUtil.ConnectTypePins(argTypeNode.OutputTypePins[0], method.EntryNode.InputTypePins[0]);

            Setter = method;
        }

        public void RemoveSetter()
        {
            Setter = null;
        }

        public void OpenGetterGraph()
        {
            WeakReferenceMessenger.Default.Send(new OpenGraphMessage(Getter));
        }

        public void OpenSetterGraph()
        {
            WeakReferenceMessenger.Default.Send(new OpenGraphMessage(Setter));
        }

        public void OpenTypeGraph()
        {
            WeakReferenceMessenger.Default.Send(new OpenGraphMessage(Variable.TypeGraph));
        }
    }
}
