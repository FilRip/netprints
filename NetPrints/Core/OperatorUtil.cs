using System.Collections.Generic;

namespace NetPrints.Core
{
    public class OperatorInfo
    {
        public string DisplayName { get; }
        public string Symbol { get; }
        public bool Unary { get; }
        public bool UnaryRightPosition { get; }

        public OperatorInfo(string displayName, string symbol, bool unary, bool unaryRightPosition = false)
        {
            DisplayName = displayName;
            Symbol = symbol;
            Unary = unary;
            UnaryRightPosition = unaryRightPosition;
        }
    }

    public static class OperatorUtil
    {
        private const string OperatorPrefix = "op_";

        /// <summary>
        /// Mapping from operator method name to operator definitions (display name, symbol, arity, position).
        /// </summary>
        private static readonly Dictionary<string, OperatorInfo> operatorSymbols = new()
        {
            // Unary
            [OperatorPrefix + "Increment"] = new OperatorInfo("Increment", "++", true, true),
            [OperatorPrefix + "Decrement"] = new OperatorInfo("Decrement", "--", true, true),
            [OperatorPrefix + "UnaryPlus"] = new OperatorInfo("Unary Plus", "+", true),
            [OperatorPrefix + "UnaryNegation"] = new OperatorInfo("Unary Negation", "-", true),
            [OperatorPrefix + "LogicalNot"] = new OperatorInfo("Not", "!", true),

            // Binary
            [OperatorPrefix + "Addition"] = new OperatorInfo("Add", "+", false),
            [OperatorPrefix + "Subtraction"] = new OperatorInfo("Subtract", "-", false),
            [OperatorPrefix + "Multiply"] = new OperatorInfo("Multiply", "*", false),
            [OperatorPrefix + "Division"] = new OperatorInfo("Divide", "/", false),
            [OperatorPrefix + "Modulus"] = new OperatorInfo("Modulus", "%", false),
            [OperatorPrefix + "GreaterThan"] = new OperatorInfo("Greater than", ">", false),
            [OperatorPrefix + "GreaterThanOrEqual"] = new OperatorInfo("Greater than or equal", ">=", false),
            [OperatorPrefix + "Equality"] = new OperatorInfo("Equal", "==", false),
            [OperatorPrefix + "Inequality"] = new OperatorInfo("Not Equal", "!=", false),
            [OperatorPrefix + "LessThan"] = new OperatorInfo("Less than", "<", false),
            [OperatorPrefix + "LessThanOrEqual"] = new OperatorInfo("Less than or equal", "<=", false),
            [OperatorPrefix + "BitwiseAnd"] = new OperatorInfo("Bitwise AND", "&", false),
            [OperatorPrefix + "BitwiseOr"] = new OperatorInfo("Bitwise OR", "|", false),
            [OperatorPrefix + "ExclusiveOr"] = new OperatorInfo("Bitwise XOR", "^", false),
            [OperatorPrefix + "LeftShift"] = new OperatorInfo("Shift Left", "<<", false),
            [OperatorPrefix + "RightShift"] = new OperatorInfo("Shift Right", ">>", false),

            // Custom (not part of .NET symbols)
            [OperatorPrefix + "BitwiseNot"] = new OperatorInfo("Bitwise NOT", "~", true),
            [OperatorPrefix + "LogicalAnd"] = new OperatorInfo("And", "&&", false),
            [OperatorPrefix + "LogicalOr"] = new OperatorInfo("Or", "||", false),
        };

        /// <summary>
        /// Returns whether the method specifier is an operator.
        /// </summary>
        /// <param name="methodName">Name of the method.</param>
        /// <returns></returns>
        public static bool IsOperator(MethodSpecifier methodSpecifier) =>
            operatorSymbols.ContainsKey(methodSpecifier.Name);

        /// <summary>
        /// Tries to get operator info for a method specifier.
        /// </summary>
        /// <param name="methodSpecifier">Method specifier to find operator info for.</param>
        /// <param name="operatorInfo">Operator info for the method specifier if found.</param>
        /// <returns></returns>
        public static bool TryGetOperatorInfo(MethodSpecifier methodSpecifier, out OperatorInfo operatorInfo) =>
            operatorSymbols.TryGetValue(methodSpecifier.Name, out operatorInfo);
    }
}
