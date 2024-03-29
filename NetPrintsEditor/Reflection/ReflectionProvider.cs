﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

using NetPrints.Core;

namespace NetPrintsEditor.Reflection
{
    public static class ISymbolExtensions
    {
        private static readonly Dictionary<ITypeSymbol, List<ISymbol>> allMembersCache = [];

        /// <summary>
        /// Gets all members of a symbol including inherited ones, but not overriden ones.
        /// </summary>
        public static IEnumerable<ISymbol> GetAllMembers(this ITypeSymbol symbol)
        {
            if (allMembersCache.TryGetValue(symbol, out List<ISymbol> allMembers))
            {
                return allMembers;
            }

            List<ISymbol> members = [];
            HashSet<IMethodSymbol> overridenMethods = [];

            ITypeSymbol startSymbol = symbol;

            while (symbol != null)
            {
                System.Collections.Immutable.ImmutableArray<ISymbol> symbolMembers = symbol.GetMembers();

                // Add symbols which weren't overriden yet
                List<ISymbol> newMembers = symbolMembers.Where(m => m is not IMethodSymbol methodSymbol || !overridenMethods.Contains(methodSymbol)).ToList();

                members.AddRange(newMembers);

                // Recursively add overriden methods
                List<IMethodSymbol> newOverridenMethods = symbolMembers.OfType<IMethodSymbol>().ToList();
                while (newOverridenMethods.Count > 0)
                {
                    newOverridenMethods.ForEach(m => overridenMethods.Add(m));
                    newOverridenMethods = newOverridenMethods
                        .Where(m => m.OverriddenMethod != null)
                        .Select(m => m.OverriddenMethod)
                        .ToList();
                }

                symbol = symbol.BaseType;
            }

            allMembersCache.Add(startSymbol, members.ToList());

            return members;
        }

        public static bool IsPublic(this ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public;
        }

        public static bool IsProtected(this ISymbol symbol)
        {
            return symbol.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Protected;
        }

        public static IEnumerable<IMethodSymbol> GetMethods(this ITypeSymbol symbol)
        {
            return symbol.GetAllMembers()
                    .Where(member => member.Kind == SymbolKind.Method)
                    .Cast<IMethodSymbol>()
                    .Where(method => method.MethodKind == MethodKind.Ordinary || method.MethodKind == MethodKind.BuiltinOperator || method.MethodKind == MethodKind.UserDefinedOperator);
        }

        public static IEnumerable<IMethodSymbol> GetConverters(this ITypeSymbol symbol)
        {
            return symbol.GetAllMembers()
                    .Where(member => member.Kind == SymbolKind.Method)
                    .Cast<IMethodSymbol>()
                    .Where(method => method.MethodKind == MethodKind.Conversion);
        }

        public static bool IsSubclassOf(this ITypeSymbol symbol, ITypeSymbol cls)
        {
            // If cls is an interface type, check if the interface is implemented
            // TODO: Currently only checking full name and type parameter count for interfaces.
            if (symbol != null && cls.TypeKind == TypeKind.Interface && cls is INamedTypeSymbol namedCls)
            {
                static bool IsSameInterface(INamedTypeSymbol a, INamedTypeSymbol b)
                {
                    return a.GetFullName() == b.GetFullName() && a.TypeParameters.Length == b.TypeParameters.Length;
                }

                return (symbol is INamedTypeSymbol namedSymbol && IsSameInterface(namedSymbol, namedCls))
                    || symbol.AllInterfaces.Any(interf =>
                        cls is INamedTypeSymbol namedCls && IsSameInterface(interf, namedCls));
            }

            // Traverse base types to find out if symbol inherits from cls
            ITypeSymbol candidateBaseType = symbol;
            while (candidateBaseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(candidateBaseType, cls))
                {
                    return true;
                }

                candidateBaseType = candidateBaseType.BaseType;
            }

            return false;
        }

        public static string GetFullName(this ITypeSymbol typeSymbol)
        {
            string fullName = typeSymbol.MetadataName;
            if (typeSymbol.ContainingNamespace != null && !typeSymbol.ContainingNamespace.IsGlobalNamespace)
            {
                fullName = $"{typeSymbol.ContainingNamespace.MetadataName}.{fullName}";
            }
            return fullName;
        }
    }

    public class ReflectionProvider : IReflectionProvider
    {
        private readonly CSharpCompilation compilation;
        private readonly DocumentationUtil documentationUtil;
        private readonly List<IMethodSymbol> extensionMethods;

        private static (EmitResult, Stream) CompileInMemory(CSharpCompilation compilation)
        {
            MemoryStream stream = new();
            EmitResult compilationResults = compilation.Emit(stream);
            stream.Seek(0, SeekOrigin.Begin);
            return (compilationResults, stream);
        }

        private static SyntaxTree ParseSyntaxTree(string source)
        {
            // LanguageVersion.Preview is not defined in the Roslyn version used
            // at the time of writing. However MaxValue - 1 (as defined in the newer versions
            // see https://github.com/dotnet/roslyn/blob/472276accaf70a8356747dc7111cfb6231871077/src/Compilers/CSharp/Portable/LanguageVersion.cs#L135
            // seems to work.
            const LanguageVersion previewVersion = (LanguageVersion)(int.MaxValue - 1);

            // Return a syntax tree of our source code
            return CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(languageVersion: previewVersion));
        }

        /// <summary>
        /// Creates a ReflectionProvider given paths to assemblies and source files.
        /// </summary>
        /// <param name="assemblyPaths">Paths to assemblies.</param>
        /// <param name="sourcePaths">Paths to source files.</param>
        public ReflectionProvider(IEnumerable<string> assemblyPaths, IEnumerable<string> sourcePaths, IEnumerable<string> sources)
        {
            CSharpCompilationOptions compilationOptions = new(OutputKind.DynamicallyLinkedLibrary);

            // Create assembly metadata references
            List<PortableExecutableReference> assemblyReferences = assemblyPaths.Select(path =>
            {
                DocumentationProvider documentationProvider = DocumentationProvider.Default;

                // Try to find the documentation in the framework doc path
                string docPath = Path.ChangeExtension(path, ".xml");
                if (!File.Exists(docPath))
                {
                    docPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        "Reference Assemblies/Microsoft/Framework/.NETFramework/v4.X",
                        $"{Path.GetFileNameWithoutExtension(path)}.xml");
                }

                if (File.Exists(docPath))
                {
                    documentationProvider = XmlDocumentationProvider.CreateFromFile(docPath);
                }

                return MetadataReference.CreateFromFile(path, documentation: documentationProvider);
            }).ToList();

            // Create syntax trees from sources
            sources = sources.Concat(sourcePaths.Select(path => File.ReadAllText(path))).Distinct();
            List<SyntaxTree> syntaxTrees = sources.Select(source => ParseSyntaxTree(source)).ToList();

            compilation = CSharpCompilation.Create("C", syntaxTrees, assemblyReferences, compilationOptions);

            // Try to compile, on success create a new compilation that references the created assembly instead of the sources.
            // The compilation will fail eg. if the sources have references to the not-yet-compiled assembly.
            (EmitResult compilationResults, Stream stream) = CompileInMemory(compilation);

            if (compilationResults.Success)
            {
                assemblyReferences = assemblyReferences.Concat(new[] { MetadataReference.CreateFromStream(stream) }).ToList();
                compilation = CSharpCompilation.Create("C", references: assemblyReferences, options: compilationOptions);
            }

            extensionMethods = new List<IMethodSymbol>(GetValidTypes().SelectMany(t => t.GetMethods().Where(m => m.IsExtensionMethod)));

            documentationUtil = new DocumentationUtil(compilation);
        }

        /// <summary>
        /// Gets all classes declared in the compilation's syntax trees.
        /// Useful for when they can not be compiled into assemblies because
        /// of errors and we still want their symbols.
        /// </summary>
        private IEnumerable<INamedTypeSymbol> GetSyntaxTreeTypes()
        {
            foreach (SyntaxTree syntaxTree in compilation.SyntaxTrees)
            {
                SemanticModel model = compilation.GetSemanticModel(syntaxTree, true);
                List<ClassDeclarationSyntax> classSyntaxes = syntaxTree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                List<INamedTypeSymbol> classes = classSyntaxes.Select(syntax => model.GetDeclaredSymbol(syntax)).ToList();
                foreach (INamedTypeSymbol cls in classes)
                {
                    yield return cls;
                }
            }
        }

        private static IEnumerable<INamedTypeSymbol> GetTypeNestedTypes(INamedTypeSymbol typeSymbol)
        {
            System.Collections.Immutable.ImmutableArray<INamedTypeSymbol> typeMembers = typeSymbol.GetTypeMembers();
            return typeMembers.Concat(typeMembers.SelectMany(t => GetTypeNestedTypes(t)));
        }

        private static IEnumerable<INamedTypeSymbol> GetNamespaceTypes(INamespaceSymbol namespaceSymbol)
        {
            IEnumerable<INamedTypeSymbol> types = namespaceSymbol.GetTypeMembers();
            types = types.Concat(types.SelectMany(t => GetTypeNestedTypes(t)));
            return types.Concat(namespaceSymbol.GetNamespaceMembers().SelectMany(ns => GetNamespaceTypes(ns)));
        }

        private IEnumerable<INamedTypeSymbol> GetValidTypes()
        {
            return compilation.SourceModule.ReferencedAssemblySymbols.SelectMany(module => GetNamespaceTypes(module.GlobalNamespace))
                .Concat(GetSyntaxTreeTypes());
        }

        private IEnumerable<INamedTypeSymbol> GetValidTypes(string name)
        {
            return compilation.SourceModule.ReferencedAssemblySymbols.Select(module =>
            {
                try { return module.GetTypeByMetadataName(name); }
                catch { return null; }
            })
            .Where(t => t != null)
            .Concat(GetSyntaxTreeTypes().Where(t => t.GetFullName() == name)); // TODO: Correct full name
        }

        #region IReflectionProvider
        public IEnumerable<TypeSpecifier> GetNonStaticTypes()
        {
            return GetValidTypes().Where(
                    t => t.IsPublic() && !(t.IsAbstract && t.IsSealed))
                .OrderBy(t => t.ContainingNamespace?.Name)
                .ThenBy(t => t.Name)
                .Select(t => ReflectionConverter.TypeSpecifierFromSymbol(t));
        }

        public IEnumerable<MethodSpecifier> GetOverridableMethodsForType(TypeSpecifier typeSpecifier)
        {
            ITypeSymbol type = GetTypeFromSpecifier(typeSpecifier);

            if (type != null)
            {
                // Get all overridable methods, ignore special ones (properties / events)

                return type.GetMethods()
                    .Where(m =>
                        (m.IsVirtual || m.IsOverride || m.IsAbstract)
                        && m.MethodKind == MethodKind.Ordinary)
                    .OrderBy(m => m.ContainingNamespace?.Name)
                    .ThenBy(m => m.ContainingType?.Name)
                    .ThenBy(m => m.Name)
                    .Select(m => ReflectionConverter.MethodSpecifierFromSymbol(m));
            }
            else
            {
                return Array.Empty<MethodSpecifier>();
            }
        }

        public IEnumerable<MethodSpecifier> GetPublicMethodOverloads(MethodSpecifier methodSpecifier)
        {
            ITypeSymbol type = GetTypeFromSpecifier(methodSpecifier.DeclaringType);

            // TODO: Get a better way to determine is a method specifier is an operator.
            bool isOperator = methodSpecifier.Name.StartsWith("op_");

            if (type != null)
            {
                return type.GetMethods()
                        .Where(m =>
                            m.Name == methodSpecifier.Name
                            && m.IsPublic()
                            && m.IsStatic == methodSpecifier.Modifiers.HasFlag(MethodModifiers.Static)
                            && (isOperator ?
                                (m.MethodKind == MethodKind.BuiltinOperator || m.MethodKind == MethodKind.UserDefinedOperator) :
                                m.MethodKind == MethodKind.Ordinary))
                        .OrderBy(m => m.ContainingNamespace?.Name)
                        .ThenBy(m => m.ContainingType?.Name)
                        .ThenBy(m => m.Name)
                        .Select(m => ReflectionConverter.MethodSpecifierFromSymbol(m));
            }
            else
            {
                return Array.Empty<MethodSpecifier>();
            }
        }

        public IEnumerable<ConstructorSpecifier> GetConstructors(TypeSpecifier typeSpecifier)
        {
            INamedTypeSymbol symbol = GetTypeFromSpecifier<INamedTypeSymbol>(typeSpecifier);

            if (symbol != null)
            {
                return symbol.Constructors.Select(c => ReflectionConverter.ConstructorSpecifierFromSymbol(c));
            }

            return Array.Empty<ConstructorSpecifier>();
        }

        public IEnumerable<string> GetEnumNames(TypeSpecifier typeSpecifier)
        {
            ITypeSymbol symbol = GetTypeFromSpecifier(typeSpecifier);

            if (symbol != null)
            {
                return symbol.GetAllMembers()
                    .Where(member => member.Kind == SymbolKind.Field)
                    .Select(member => member.Name);
            }

            return Array.Empty<string>();
        }

        public bool TypeSpecifierIsSubclassOf(TypeSpecifier a, TypeSpecifier b)
        {
            ITypeSymbol typeA = GetTypeFromSpecifier(a);
            ITypeSymbol typeB = GetTypeFromSpecifier(b);

            return typeA != null && typeB != null && typeA.IsSubclassOf(typeB);
        }

        private readonly Dictionary<TypeSpecifier, ITypeSymbol> cachedTypeSpecifierSymbols = [];

        private T GetTypeFromSpecifier<T>(TypeSpecifier specifier)
        {
            return (T)GetTypeFromSpecifier(specifier);
        }

        private ITypeSymbol GetTypeFromSpecifier(TypeSpecifier specifier)
        {
            if (cachedTypeSpecifierSymbols.TryGetValue(specifier, out ITypeSymbol symbol))
            {
                return symbol;
            }

            string lookupName = specifier.Name;

            // Find array ranks and remove them from the lookup name.
            // Example: int[][,] -> arrayRanks: { 1, 2 }, lookupName: int
            Stack<int> arrayRanks = new();
            while (lookupName.EndsWith(']'))
            {
                lookupName = lookupName.Remove(lookupName.Length - 1);
                int arrayRank = 1;
                while (lookupName.EndsWith(','))
                {
                    arrayRank++;
                    lookupName = lookupName.Remove(lookupName.Length - 1);
                }
                arrayRanks.Push(arrayRank);

                if (lookupName.Last() != '[')
                {
                    throw new NetPrints.Exceptions.NetPrintsException("Expected [ in lookupName");
                }

                lookupName = lookupName.Remove(lookupName.Length - 1);
            }

            if (specifier.GenericArguments.Count > 0)
                lookupName += $"`{specifier.GenericArguments.Count}";

            IEnumerable<INamedTypeSymbol> types = GetValidTypes(lookupName);

            ITypeSymbol foundType = null;

            foreach (INamedTypeSymbol t in types)
            {
                if (t != null)
                {
                    if (specifier.GenericArguments.Count > 0)
                    {
                        ITypeSymbol[] typeArguments = specifier.GenericArguments
                            .Select(baseType => baseType is TypeSpecifier typeSpec ?
                                GetTypeFromSpecifier(typeSpec) :
                                t.TypeArguments[specifier.GenericArguments.IndexOf(baseType)])
                            .ToArray();
                        foundType = t.Construct(typeArguments);
                    }
                    else
                    {
                        foundType = t;
                    }

                    break;
                }
            }

            if (foundType != null)
            {
                // Make array
                //while (arrayRanks.TryPop(out int arrayRank))
                while (arrayRanks.Count > 0)
                {
                    int arrayRank = arrayRanks.Pop();
                    foundType = compilation.CreateArrayTypeSymbol(foundType, arrayRank);
                }
            }

            cachedTypeSpecifierSymbols.Add(specifier, foundType);

            return foundType;
        }

        private IMethodSymbol GetMethodInfoFromSpecifier(MethodSpecifier specifier)
        {
            INamedTypeSymbol declaringType = GetTypeFromSpecifier<INamedTypeSymbol>(specifier.DeclaringType);
            return declaringType?.GetMethods().FirstOrDefault(
                    m => m.Name == specifier.Name
                    && m.Parameters.Select(p => ReflectionConverter.BaseTypeSpecifierFromSymbol(p.Type)).SequenceEqual(specifier.ArgumentTypes()));
        }

        // Documentation

        public string GetMethodDocumentation(MethodSpecifier methodSpecifier)
        {
            IMethodSymbol methodInfo = GetMethodInfoFromSpecifier(methodSpecifier);

            if (methodInfo == null)
            {
                return null;
            }

            return documentationUtil.GetMethodSummary(methodInfo);
        }

        public string GetMethodParameterDocumentation(MethodSpecifier methodSpecifier, int parameterIndex)
        {
            IMethodSymbol methodInfo = GetMethodInfoFromSpecifier(methodSpecifier);

            if (methodInfo == null)
            {
                return null;
            }

            return documentationUtil.GetMethodParameterInfo(methodInfo.Parameters[parameterIndex]);
        }

        public string GetMethodReturnDocumentation(MethodSpecifier methodSpecifier, int returnIndex)
        {
            IMethodSymbol methodInfo = GetMethodInfoFromSpecifier(methodSpecifier);

            if (methodInfo == null)
            {
                return null;
            }

            return documentationUtil.GetMethodReturnInfo(methodInfo);
        }

        public bool HasImplicitCast(TypeSpecifier fromType, TypeSpecifier toType)
        {
            // Check if there exists a conversion that is implicit between the types.

            ITypeSymbol fromSymbol = GetTypeFromSpecifier(fromType);
            ITypeSymbol toSymbol = GetTypeFromSpecifier(toType);

            return fromSymbol != null && toSymbol != null
                && compilation.ClassifyConversion(fromSymbol, toSymbol).IsImplicit;
        }

        public IEnumerable<MethodSpecifier> GetMethods(ReflectionProviderMethodQuery query)
        {
            IEnumerable<IMethodSymbol> methodSymbols;

            // Check if type is set (no type => get all methods)
            if (query.Type is not null)
            {
                // Get all methods of the type
                ITypeSymbol type = GetTypeFromSpecifier(query.Type);

                if (type == null)
                {
                    return Array.Empty<MethodSpecifier>();
                }

                methodSymbols = type.GetMethods();

                List<IMethodSymbol> extensions = extensionMethods.Where(m => type.IsSubclassOf(m.Parameters[0].Type)).ToList();

                // Add applicable extension methods
                methodSymbols = methodSymbols.Concat(extensions);
            }
            else
            {
                // Get all methods of all public types
                methodSymbols = GetValidTypes()
                                .Where(t => t.IsPublic())
                                .SelectMany(t => t.GetMethods());
            }

            // Check static
            if (query.Static.HasValue)
            {
                methodSymbols = methodSymbols.Where(m => m.IsStatic == query.Static.Value);
            }

            // Check has generic arguments
            if (query.HasGenericArguments.HasValue)
            {
                methodSymbols = methodSymbols.Where(m => query.HasGenericArguments.Value ?
                    m.TypeParameters.Any() :
                    !m.TypeParameters.Any());
            }

            // Check visibility
            if (query.VisibleFrom is not null)
            {
                methodSymbols = methodSymbols.Where(m => NetPrintsUtil.IsVisible(query.VisibleFrom,
                    ReflectionConverter.TypeSpecifierFromSymbol(m.ContainingType),
                    ReflectionConverter.VisibilityFromAccessibility(m.DeclaredAccessibility),
                    TypeSpecifierIsSubclassOf));
            }

            // Check argument type
            if (query.ArgumentType is not null)
            {
                ITypeSymbol searchType = GetTypeFromSpecifier(query.ArgumentType);

                methodSymbols = methodSymbols
                    .Where(m => m.Parameters
                        .Select(p => p.Type)
                        .Any(t => SymbolEqualityComparer.Default.Equals(t, searchType)
                                    || searchType.IsSubclassOf(t)
                                    || t.TypeKind == TypeKind.TypeParameter));
            }

            // Check return type
            if (query.ReturnType is not null)
            {
                ITypeSymbol searchType = GetTypeFromSpecifier(query.ReturnType);

                methodSymbols = methodSymbols
                    .Where(m => SymbolEqualityComparer.Default.Equals(m.ReturnType, searchType)
                                || m.ReturnType.IsSubclassOf(searchType)
                                || m.ReturnType.TypeKind == TypeKind.TypeParameter);
            }

            List<MethodSpecifier> methodSpecifiers = methodSymbols
                .OrderBy(m => m.ContainingNamespace?.Name)
                .ThenBy(m => m.ContainingType?.Name)
                .ThenBy(m => m.Name)
                .Select(m => ReflectionConverter.MethodSpecifierFromSymbol(m)).ToList();

            // HACK: Add default operators which we can not find by
            //       reflection at this time.
            if (query.HasGenericArguments != true && query.Static != false)
            {
                List<MethodSpecifier> defaultOperatorSpecifiers = DefaultOperatorSpecifiers.All.ToList();

                if (query.Type is not null)
                {
                    defaultOperatorSpecifiers = defaultOperatorSpecifiers.Where(t => t.DeclaringType == query.Type).ToList();
                }

                if (query.ReturnType is not null)
                {
                    defaultOperatorSpecifiers = defaultOperatorSpecifiers.Where(t => t.ReturnTypes.Any(rt => rt == query.ReturnType)).ToList();
                }

                if (query.ArgumentType is not null)
                {
                    defaultOperatorSpecifiers = defaultOperatorSpecifiers.Where(t => t.ArgumentTypes().Any(at => at == query.ArgumentType)).ToList();
                }

                methodSpecifiers = defaultOperatorSpecifiers.Concat(methodSpecifiers).ToList();
            }

            return methodSpecifiers;
        }

        public IEnumerable<VariableSpecifier> GetVariables(ReflectionProviderVariableQuery query)
        {
            // Note: Currently we handle fields and properties in this function
            //       so there is some extra logic for handling the fields.
            //       This should be unified or seperated later.

            static ITypeSymbol TypeSymbolFromFieldOrProperty(ISymbol symbol)
            {
                if (symbol is IFieldSymbol fieldSymbol)
                {
                    return fieldSymbol.Type;
                }
                else if (symbol is IPropertySymbol propertySymbol)
                {
                    return propertySymbol.Type;
                }

                throw new ArgumentException("symbol not a property nor field symbol.");
            }

            IEnumerable<ISymbol> propertySymbols;

            // Check if type is set (no type => get all methods)
            if (query.Type is not null)
            {
                // Get all properties of the type
                ITypeSymbol type = GetTypeFromSpecifier(query.Type);

                if (type == null)
                {
                    return Array.Empty<VariableSpecifier>();
                }

                propertySymbols = type.GetAllMembers()
                    .Where(m => m.Kind is SymbolKind.Property or SymbolKind.Field);
            }
            else
            {
                // Get all properties of all public types
                propertySymbols = GetValidTypes()
                    .SelectMany(t => t.GetAllMembers()
                        .Where(m => m.Kind is SymbolKind.Property or SymbolKind.Field));
            }

            // Check static
            if (query.Static.HasValue)
            {
                propertySymbols = propertySymbols.Where(m => m.IsStatic == query.Static.Value);
            }

            // Check visibility
            if (query.VisibleFrom is not null)
            {
                propertySymbols = propertySymbols.Where(p => NetPrintsUtil.IsVisible(query.VisibleFrom,
                    ReflectionConverter.TypeSpecifierFromSymbol(p.ContainingType),
                    ReflectionConverter.VisibilityFromAccessibility(p.DeclaredAccessibility),
                    TypeSpecifierIsSubclassOf));
            }

            // Check property type
            if (query.VariableType is not null)
            {
                ITypeSymbol searchType = GetTypeFromSpecifier(query.VariableType);

                propertySymbols = propertySymbols.Where(p => query.VariableTypeDerivesFrom ?
                    TypeSymbolFromFieldOrProperty(p).IsSubclassOf(searchType) :
                    searchType.IsSubclassOf(TypeSymbolFromFieldOrProperty(p)));
            }

            return propertySymbols
                .OrderBy(p => p.ContainingNamespace?.Name)
                .ThenBy(p => p.ContainingType?.Name)
                .ThenBy(p => p.Name)
                .Select(p => p is IPropertySymbol propertySymbol ? ReflectionConverter.VariableSpecifierFromSymbol(propertySymbol) : ReflectionConverter.VariableSpecifierFromField((IFieldSymbol)p));
        }

        #endregion
    }
}
