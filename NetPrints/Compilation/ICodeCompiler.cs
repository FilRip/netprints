using System;
using System.Collections.Generic;

namespace NetPrints.Compilation
{
    /// <summary>
    /// Contains results of a compilation.
    /// </summary>
    [Serializable()]
    public class CodeCompileResults(bool success, IEnumerable<string> errors, string pathToAssembly)
    {
        /// <summary>
        /// Whether the compilation was successful.
        /// </summary>
        public bool Success
        {
            get;
        } = success;

        /// <summary>
        /// Errors of the compilation.
        /// </summary>
        public IEnumerable<string> Errors
        {
            get;
        } = errors;

        /// <summary>
        /// Path to the generated assembly.
        /// </summary>
        public string PathToAssembly
        {
            get;
        } = pathToAssembly;
    }

    /// <summary>
    /// Interface for code compilers.
    /// </summary>
    public interface ICodeCompiler
    {
        /// <summary>
        /// Compiles code into a binary.
        /// </summary>
        /// <param name="outputPath">Output path for the compilation.</param>
        /// <param name="assemblyPaths">Paths to assemblies to reference.</param>
        /// <param name="sources">Source code to compile.</param>
        /// <param name="generateExecutable">Whether to generate an executable or a dynamically linked library.</param>
        /// <returns>Results for the compilation.</returns>
        CodeCompileResults CompileSources(string outputPath, IEnumerable<string> assemblyPaths,
            IEnumerable<string> sources, bool generateExecutable);
    }
}
