using System;
using System.Collections.Generic;
using System.Windows.Input;

using NetPrintsEditor.ViewModels;

namespace NetPrintsEditor.Commands
{
    /// <summary>
    /// Commands for modifying the NetPrints structures.
    /// </summary>
    public static class NetPrintsCommands
    {
        /// <summary>
        /// Command for removing a method from a class.
        /// </summary>
        public static readonly RoutedUICommand RemoveMethod = new(nameof(RemoveMethod), nameof(RemoveMethod), typeof(NetPrintsCommands));

        /// <summary>
        /// Command for adding an attribute to a class.
        /// </summary>
        public static readonly RoutedUICommand AddVariable = new(nameof(AddVariable), nameof(AddVariable), typeof(NetPrintsCommands));

        /// <summary>
        /// Command for removing an attribute from a class.
        /// </summary>
        public static readonly RoutedUICommand RemoveVariable = new(nameof(RemoveVariable), nameof(RemoveVariable), typeof(NetPrintsCommands));

        /// <summary>
        /// Command that does nothing. Currently used in undoing when no undo is implemented.
        /// </summary>
        public static readonly RoutedUICommand DoNothing = new(nameof(DoNothing), nameof(DoNothing), typeof(NetPrintsCommands));

        /// <summary>
        /// Command for changing the overload of a node.
        /// </summary>
        public static readonly RoutedUICommand ChangeNodeOverload = new(nameof(ChangeNodeOverload), nameof(ChangeNodeOverload), typeof(NetPrintsCommands));

        /// <summary>
        /// Command for adding a getter to a variable.
        /// </summary>
        public static readonly RoutedUICommand AddGetter = new(nameof(AddGetter), nameof(AddGetter), typeof(NetPrintsCommands));

        /// <summary>
        /// Command for adding a setter to a variable.
        /// </summary>
        public static readonly RoutedUICommand AddSetter = new(nameof(AddSetter), nameof(AddSetter), typeof(NetPrintsCommands));

        /// <summary>
        /// Command for removing a getter from a variable.
        /// </summary>
        public static readonly RoutedUICommand RemoveGetter = new(nameof(RemoveGetter), nameof(RemoveGetter), typeof(NetPrintsCommands));

        /// <summary>
        /// Command for removing a setter from a variable.
        /// </summary>
        public static readonly RoutedUICommand RemoveSetter = new(nameof(RemoveSetter), nameof(RemoveSetter), typeof(NetPrintsCommands));

        public class ChangeNodeOverloadParameters
        {
            public NodeVM Node;
            public object NewOverload;

            public ChangeNodeOverloadParameters(NodeVM node, object newOverload)
            {
                Node = node;
                NewOverload = newOverload;
            }
        }

        public delegate Tuple<ICommand, object> MakeUndoCommandDelegate(object parameters);

        public static readonly Dictionary<ICommand, MakeUndoCommandDelegate> MakeUndoCommand = new()
        {
            { RemoveMethod, (p) => new Tuple<ICommand, object>(DoNothing, p) },
            { AddVariable, (p) => new Tuple<ICommand, object>(RemoveVariable, p) },
            { RemoveVariable, (p) => new Tuple<ICommand, object>(AddVariable, p) },
            {
                ChangeNodeOverload,
                (p) =>
                {
                    if (p is ChangeNodeOverloadParameters overloadParams)
                    {
                        // Restore the old overload
                        var undoParams = new ChangeNodeOverloadParameters(overloadParams.Node, overloadParams.Node.CurrentOverload);
                        return new Tuple<ICommand, object>(ChangeNodeOverload, undoParams);
                    }

                    throw new ArgumentException("Expected parameters of type SetNodeOverloadParameters");
                }
            },
            { AddGetter, (p) => new Tuple<ICommand, object>(RemoveGetter, p) },
            { AddSetter, (p) => new Tuple<ICommand, object>(RemoveSetter, p) },
            { RemoveGetter, (p) => new Tuple<ICommand, object>(AddGetter, p) }, // TODO: Restore old method
            { RemoveSetter, (p) => new Tuple<ICommand, object>(AddSetter, p) }, // TODO: Restore old method
        };
    }
}
