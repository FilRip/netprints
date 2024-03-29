﻿using System.Collections.Generic;
using System.Linq;

using NetPrints.Core;

namespace NetPrintsEditor.Reflection
{
    public static class DefaultOperatorSpecifiers
    {
        public static IEnumerable<MethodSpecifier> All
        {
            get
            {
                if (all == null)
                {
                    TypeSpecifier boolType = TypeSpecifier.FromType<bool>();
                    TypeSpecifier intType = TypeSpecifier.FromType<int>();

                    all = [];

                    // Numerical
                    foreach (TypeSpecifier defaultNumericType in defaultNumericTypes)
                    {
                        foreach (string unaryOpName in defaultNumericUnaryOperatorNames)
                        {
                            AddOperator(unaryOpName, true, defaultNumericType, defaultNumericType);
                        }

                        foreach (string unaryOpName in defaultNumericBinaryOperatorNames)
                        {
                            AddOperator(unaryOpName, false, defaultNumericType, defaultNumericType);
                        }

                        foreach (string unaryOpName in defaultNumericComparisonOperatorNames)
                        {
                            AddOperator(unaryOpName, false, defaultNumericType, boolType);
                        }
                    }

                    // Logical (boolean)
                    AddOperator("op_LogicalNot", true, boolType, boolType);
                    AddOperator("op_LogicalAnd", false, boolType, boolType);
                    AddOperator("op_LogicalOr", false, boolType, boolType);

                    // Integer bitwise operators
                    AddOperator("op_LogicalNot", true, intType, intType);
                    AddOperator("op_BitwiseAnd", false, intType, intType);
                    AddOperator("op_BitwiseOr", false, intType, intType);
                    AddOperator("op_ExclusiveOr", false, intType, intType);
                    AddOperator("op_LeftShift", false, intType, intType);
                    AddOperator("op_RightShift", false, intType, intType);

                    // String addition
                    TypeSpecifier stringType = TypeSpecifier.FromType<string>();
                    AddOperator("op_Addition", false, stringType, stringType);
                }

                return all;
            }
        }

        private static List<MethodSpecifier> all;

        private static void AddOperator(string opName, bool unary, TypeSpecifier argType, TypeSpecifier returnType)
        {
            IEnumerable<MethodParameter> parameters = new[]
            {
                new MethodParameter("a", argType, MethodParameterPassType.Default, false, null),
            };

            if (!unary)
            {
                parameters = parameters.Concat(new[] { new MethodParameter("b", argType, MethodParameterPassType.Default, false, null) });
            }

            all.Add(new MethodSpecifier(opName, parameters, new[] { returnType }, MethodModifiers.Static, MemberVisibility.Public, returnType, System.Array.Empty<BaseType>()));
        }

        private static readonly List<TypeSpecifier> defaultNumericTypes =
        [
            TypeSpecifier.FromType<byte>(),
            TypeSpecifier.FromType<short>(),
            TypeSpecifier.FromType<ushort>(),
            TypeSpecifier.FromType<int>(),
            TypeSpecifier.FromType<uint>(),
            TypeSpecifier.FromType<float>(),
            TypeSpecifier.FromType<double>(),
            TypeSpecifier.FromType<decimal>(),
        ];

        private static readonly IEnumerable<string> defaultNumericBinaryOperatorNames = new[]
        {
            "op_Addition",
            "op_Subtraction",
            "op_Multiply",
            "op_Division",
            "op_Modulus",
        };

        private static readonly IEnumerable<string> defaultNumericComparisonOperatorNames = new[]
        {
            "op_GreaterThan",
            "op_GreaterThanOrEqual",
            "op_Equality",
            "op_Inequality",
            "op_LessThan",
            "op_LessThanOrEqual",
        };

        private static readonly IEnumerable<string> defaultNumericUnaryOperatorNames = new[]
        {
            "op_Increment",
            "op_Decrement",
            "op_UnaryPlus",
            "op_UnaryNegation",
        };
    }
}
