using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace NetPrints.Core
{
    /// <summary>
    /// Specifier describing a constructor.
    /// </summary>
    [Serializable()]
    [DataContract()]
    public partial class ConstructorSpecifier(IEnumerable<MethodParameter> arguments, TypeSpecifier declaringType)
    {
        /// <summary>
        /// Specifier for the type this constructor is for.
        /// </summary>
        [DataMember()]
        public TypeSpecifier DeclaringType
        {
            get;
            private set;
        } = declaringType;

        /// <summary>
        /// Specifiers for the arguments this constructor takes.
        /// </summary>
        [DataMember()]
        public IList<MethodParameter> Arguments
        {
            get;
            private set;
        } = arguments.ToList();

        public override string ToString()
        {
            string constructorString = "";

            string argTypeString = string.Join(", ", Arguments);

            constructorString += $"{DeclaringType.Name}({argTypeString})";

            return constructorString;
        }
    }
}
