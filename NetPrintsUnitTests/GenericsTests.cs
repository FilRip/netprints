using System.Collections.Generic;
using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using NetPrints.Core;
using NetPrints.Graph;
using NetPrints.Translator;

namespace NetPrintsUnitTests
{
    [TestClass()]
    public class GenericsTests
    {
        [TestMethod()]
        public void TestGenerics()
        {
            // Create the open class<T> which contains a List<T>

            GenericType genericClassArg = new("T");

            ClassGraph openClass = new()
            {
                Name = "OpenClass",
                Namespace = "Namespace"
            };
            openClass.DeclaredGenericArguments.Add(genericClassArg);

            TypeSpecifier listType = TypeSpecifier.FromType(typeof(List<>));

            Assert.AreEqual(listType.GenericArguments.Count, 1);

            listType.GenericArguments[0] = genericClassArg;

            MethodGraph openMethod = new("OpenMethod");

            // Add open list parameter
            TypeNode listTypeNode = new(openMethod, listType);

            openMethod.MainReturnNode.AddReturnType();
            GraphUtil.ConnectTypePins(listTypeNode.OutputTypePins[0], openMethod.MainReturnNode.InputTypePins[0]);

            DefaultNode defaultNode = new(openMethod);
            GraphUtil.ConnectTypePins(listTypeNode.OutputTypePins[0], defaultNode.TypePin);
            GraphUtil.ConnectDataPins(defaultNode.DefaultValuePin, openMethod.MainReturnNode.InputDataPins[0]);

            GraphUtil.ConnectExecPins(openMethod.EntryNode.InitialExecutionPin, openMethod.ReturnNodes.First().ReturnPin);

            openClass.Methods.Add(openMethod);

            // Create the closed class which contains a List<string>

            ClassGraph closedClass = new()
            {
                Name = "ClosedClass",
                Namespace = "Namespace"
            };

            TypeSpecifier closedListType = TypeSpecifier.FromType<string>();

            MethodGraph closedMethod = new("ClosedMethod");

            // Add closed list parameter
            TypeNode closedListTypeNode = new(closedMethod, closedListType);
            closedMethod.MainReturnNode.AddReturnType();
            GraphUtil.ConnectTypePins(closedListTypeNode.OutputTypePins[0], closedMethod.MainReturnNode.InputTypePins[0]);

            DefaultNode closedDefaultNode = new(closedMethod);
            GraphUtil.ConnectTypePins(closedListTypeNode.OutputTypePins[0], closedDefaultNode.TypePin);
            GraphUtil.ConnectDataPins(closedDefaultNode.DefaultValuePin, closedMethod.MainReturnNode.InputDataPins[0]);

            GraphUtil.ConnectExecPins(closedMethod.EntryNode.InitialExecutionPin, closedMethod.ReturnNodes.First().ReturnPin);

            closedClass.Methods.Add(closedMethod);

            // Translate the classes

            ClassTranslator translator = new();

            string openClassTranslated = translator.TranslateClass(openClass);

            string closedClassTranslated = translator.TranslateClass(closedClass);
        }

        [TestMethod()]
        public void TestTypeGraph()
        {
            MethodGraph method = new("Method");

            var unboundListType = new TypeSpecifier("System.Collections.Generic.List", genericArguments: new BaseType[] { new GenericType("T") });

            var literalNode = new LiteralNode(method, unboundListType);
            Assert.AreEqual(literalNode.InputTypePins.Count, 1);

            var typeNode = new TypeNode(method, TypeSpecifier.FromType<int>());
            Assert.AreEqual(literalNode.InputTypePins.Count, 1);

            GraphUtil.ConnectTypePins(typeNode.OutputTypePins[0], literalNode.InputTypePins[0]);
            Assert.AreEqual(literalNode.InputTypePins[0].InferredType.Value, new TypeSpecifier("System.Int32"));
            Assert.AreEqual(literalNode.OutputDataPins[0].PinType.Value, new TypeSpecifier("System.Collections.Generic.List", genericArguments: new BaseType[] { new TypeSpecifier("System.Int32") }));
        }
    }
}
