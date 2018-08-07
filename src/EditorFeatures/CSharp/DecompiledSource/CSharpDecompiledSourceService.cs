// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor.CSharp.DecompiledSource
{
    internal class CSharpDecompiledSourceService : IDecompiledSourceService
    {
        private HostLanguageServices provider;

        public CSharpDecompiledSourceService(HostLanguageServices provider)
        {
            this.provider = provider;
        }

        public async Task<Document> AddSourceToAsync(Document document, ISymbol symbol, CancellationToken cancellationToken = default)
        {
            // Get the name of the type the symbol is in
            var containingOrThis = symbol.GetContainingTypeOrThis();
            var fullName = GetFullReflectionName(containingOrThis);

            var compilation = await document.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            string assemblyLocation = null;
            var isReferenceAssembly = symbol.ContainingAssembly.GetAttributes().Any(attribute => attribute.AttributeClass.Name == nameof(ReferenceAssemblyAttribute)
                && attribute.AttributeClass.ToNameDisplayString() == typeof(ReferenceAssemblyAttribute).FullName);
            if (isReferenceAssembly)
            {
                try
                {
                    var fullAssemblyName = symbol.ContainingAssembly.Identity.GetDisplayName();
                    GlobalAssemblyCache.Instance.ResolvePartialName(fullAssemblyName, out assemblyLocation, preferredCulture: CultureInfo.CurrentCulture);
                }
                catch (Exception e) when (FatalError.ReportWithoutCrash(e))
                {
                }
            }

            if (assemblyLocation == null)
            {
                var reference = compilation.GetMetadataReference(symbol.ContainingAssembly);
                assemblyLocation = (reference as PortableExecutableReference)?.FilePath;
                if (assemblyLocation == null)
                {
                    throw new NotSupportedException(EditorFeaturesResources.Cannot_navigate_to_the_symbol_under_the_caret);
                }
            }

            document = PerformDecompilation(document, fullName, compilation, assemblyLocation);

            return document;
        }

        static Document PerformDecompilation(Document document, string fullName, Compilation compilation, string assemblyLocation)
        {
            // Load the assembly.
            var pefile = new PEFile(assemblyLocation, PEStreamOptions.PrefetchEntireImage);

            // Initialize a decompiler with default settings.
            var settings = new DecompilerSettings(LanguageVersion.Latest);
            var decompiler = new CSharpDecompiler(pefile, new RoslynAssemblyResolver(compilation), settings);
            // Escape invalid identifiers to prevent Roslyn from failing to parse the generated code.
            // (This happens for example, when there is compiler-generated code that is not yet recognized/transformed by the decompiler.)
            decompiler.AstTransforms.Add(new EscapeInvalidIdentifiers());

            var fullTypeName = new FullTypeName(fullName);

            var decompilerVersion = FileVersionInfo.GetVersionInfo(typeof(CSharpDecompiler).Assembly.Location);

            // Add header to match output of metadata-only view.
            // (This also makes debugging easier, because you can see which assembly was decompiled inside VS.)
            var header = $"#region {FeaturesResources.Assembly} {pefile.FullName}" + Environment.NewLine
                + $"// {assemblyLocation}" + Environment.NewLine
                + $"// Decompiled with ICSharpCode.Decompiler {decompilerVersion.FileVersion}" + Environment.NewLine
                + "#endregion" + Environment.NewLine;

            // Try to decompile; if an exception is thrown the caller will handle it
            var text = decompiler.DecompileTypeAsString(fullTypeName);
            return document.WithText(SourceText.From(header + text));
        }

        private class RoslynAssemblyResolver : IAssemblyResolver
        {
            private readonly Compilation parentCompilation;
            private static readonly Version zeroVersion = new Version(0, 0, 0, 0);

            public RoslynAssemblyResolver(Compilation parentCompilation)
            {
                this.parentCompilation = parentCompilation;
            }

            public PEFile Resolve(IAssemblyReference name)
            {
                foreach (var assembly in parentCompilation.GetReferencedAssemblySymbols())
                {
                    // First, find the correct IAssemblySymbol by name and PublicKeyToken.
                    if (assembly.Identity.Name != name.Name
                        || !assembly.Identity.PublicKeyToken.SequenceEqual(name.PublicKeyToken ?? Array.Empty<byte>()))
                    {
                        continue;
                    }

                    // Normally we skip versions that do not match, except if the reference is "mscorlib" (see comments below)
                    // or if the name.Version is '0.0.0.0'. This is because we require the metadata of all transitive references
                    // and modules, to achieve best decompilation results.
                    // In the case of .NET Standard projects for example, the 'netstandard' reference contains no references
                    // with actual versions. All versions are '0.0.0.0', therefore we have to ignore those version numbers,
                    // and can just use the references provided by Roslyn instead.
                    if (assembly.Identity.Version != name.Version && name.Version != zeroVersion
                        && !string.Equals("mscorlib", assembly.Identity.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        // MSBuild treats mscorlib special for the purpose of assembly resolution/unification, where all
                        // versions of the assembly are considered equal. The same policy is adopted here.
                        continue;
                    }

                    // reference assemblies should be fine here, we only need the metadata of references.
                    var reference = parentCompilation.GetMetadataReference(assembly);
                    return new PEFile(reference.Display, PEStreamOptions.PrefetchMetadata);
                }

                // not found
                return null;
            }

            public PEFile ResolveModule(PEFile mainModule, string moduleName)
            {
                // Primitive implementation to support multi-module assemblies
                // where all modules are located next to the main module.
                string baseDirectory = Path.GetDirectoryName(mainModule.FileName);
                string moduleFileName = Path.Combine(baseDirectory, moduleName);
                if (!File.Exists(moduleFileName))
                    return null;
                return new PEFile(moduleFileName, PEStreamOptions.PrefetchMetadata);
            }
        }

        private string GetFullReflectionName(INamedTypeSymbol containingType)
        {
            var stack = new Stack<string>();
            stack.Push(containingType.MetadataName);
            var ns = containingType.ContainingNamespace;
            do
            {
                stack.Push(ns.Name);
                ns = ns.ContainingNamespace;
            }
            while (ns != null && !ns.IsGlobalNamespace);

            return string.Join(".", stack);
        }

    }
}
