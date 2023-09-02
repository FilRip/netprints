using System.IO;

using NetPrints.Core;
using NetPrints.Serialization;
using NetPrints.Translator;

namespace NetPrints.VSIX
{
    public static class CompilationUtil
    {
        public static void CompileNetPrintsClass(string path, string outputPath)
        {
            CompileNetPrintsClass(SerializationHelper.LoadClass(path), outputPath);
        }

        public static void CompileNetPrintsClass(ClassGraph classGraph, string outputPath)
        {
            ClassTranslator classTranslator = new();

            string translated = classTranslator.TranslateClass(classGraph);

            File.WriteAllText(outputPath, translated);
        }
    }
}
