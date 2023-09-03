using NetPrints.Core;

namespace NetPrintsEditor.Dialogs
{
    public class MakeDelegateTypeInfo(TypeSpecifier type, TypeSpecifier fromType)
    {
        public TypeSpecifier Type
        {
            get;
        } = type;

        public TypeSpecifier FromType
        {
            get;
        } = fromType;
    }
}
