using System.Runtime.Serialization;

namespace NetPrints.Core
{
    [DataContract()]
    public class VariableSpecifier(string name, TypeSpecifier type, MemberVisibility getterVisibility, MemberVisibility setterVisibility,
        TypeSpecifier declaringType, VariableModifiers modifiers)
    {
        /// <summary>
        /// Name of the property without any prefixes.
        /// </summary>
        [DataMember()]
        public string Name
        {
            get;
            set;
        } = name;

        /// <summary>
        /// Specifier for the type this property is contained in.
        /// </summary>
        [DataMember()]
        public TypeSpecifier DeclaringType
        {
            get;
            private set;
        } = declaringType;

        /// <summary>
        /// Specifier for the type of the property.
        /// </summary>
        [DataMember()]
        public TypeSpecifier Type
        {
            get;
            set;
        } = type;

        /// <summary>
        /// Whether this property has a public getter.
        /// </summary>
        [DataMember()]
        public MemberVisibility GetterVisibility
        {
            get;
            set;
        } = getterVisibility;

        /// <summary>
        /// Whether this property has a public setter.
        /// </summary>
        [DataMember()]
        public MemberVisibility SetterVisibility
        {
            get;
            set;
        } = setterVisibility;

        /// <summary>
        /// Visibility of this property.
        /// </summary>
        [DataMember()]
        public MemberVisibility Visibility
        {
            get;
            set;
        } = MemberVisibility.Private;

        /// <summary>
        /// Modifiers of this variable.
        /// </summary>
        [DataMember()]
        public VariableModifiers Modifiers
        {
            get;
            set;
        } = modifiers;
    }
}
