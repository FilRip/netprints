using System.Collections.Generic;
using System.Linq;

using NetPrints.Core;

namespace NetPrints.Graph
{
    public static class GenericsHelper
    {
        /*public static TypeSpecifier DetermineTypeNodeType(TypeNode node)
        {
            // TODO: Copy node.Type

            // Replace generic arguments with input pins
            foreach (NodeInputTypePin inputTypePin in node.InputTypePins)
            {
                // TODO: Check inputTypePin constraints
                TypeSpecifier inputType = DetermineTypeNodeType(inputTypePin.Node);

                int pinIndex = node.InputTypePins.IndexOf(inputTypePin);
                node.Type.GenericArguments[pinIndex] = inputType;
            }

            return node.Type;
        }*/

        public static BaseType ConstructWithTypePins(BaseType type, IEnumerable<NodeInputTypePin> inputTypePins)
        {
            if (type is TypeSpecifier typeSpecifier)
            {
                // Find types to replace and build dictionary
                Dictionary<GenericType, BaseType> replacementTypes = new();

                foreach (NodeInputTypePin inputTypePin in inputTypePins)
                {
                    if (inputTypePin.InferredType?.Value is BaseType replacementType && replacementType is not null &&
                        (!(typeSpecifier.GenericArguments.SingleOrDefault(arg => arg.Name == inputTypePin.Name) is not GenericType typeToReplace)))
                    {
                        // If we can not replace all 
                        replacementTypes.Add(typeToReplace, replacementType);
                    }
                }

                try
                {
                    TypeSpecifier constructedType = typeSpecifier.Construct(replacementTypes);
                    return constructedType;
                }
                catch
                {
                    return typeSpecifier;
                }
            }
            else if (type is GenericType)
            {
                BaseType replacementType = inputTypePins.SingleOrDefault(t => t.Name == type.Name)?.InferredType?.Value;
                if (replacementType != null)
                {
                    return replacementType;
                }
            }

            return type;
        }
    }
}
