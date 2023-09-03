using System.IO;
using System.Runtime.Serialization;

namespace NetPrints.Core
{
    [DataContract()]
    public class AssemblyReference(string assemblyPath) : CompilationReference
    {
        [DataMember()]
        public string AssemblyPath
        {
            get;
            set;
        } = assemblyPath;

        public override string ToString() => $"{Path.GetFileNameWithoutExtension(AssemblyPath)} at {AssemblyPath}";
    }
}
