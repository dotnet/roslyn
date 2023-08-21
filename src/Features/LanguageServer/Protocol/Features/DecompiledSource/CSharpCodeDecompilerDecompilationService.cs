// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Composition;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.Transforms;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.CodeAnalysis.DecompiledSource;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CSharp.DecompiledSource
{
    [ExportLanguageService(typeof(IDecompilationService), LanguageNames.CSharp), Shared]
    internal class CSharpDecompilationService : IDecompilationService
    {
        private static readonly FileVersionInfo s_decompilerVersion = FileVersionInfo.GetVersionInfo(typeof(CSharpDecompiler).Assembly.Location);

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public CSharpDecompilationService()
        {
        }

        public FileVersionInfo GetDecompilerVersion()
        {
            return s_decompilerVersion;
        }

        public Document? PerformDecompilation(Document document, string fullName, Compilation compilation, MetadataReference? metadataReference, string? assemblyLocation)
        {
            var logger = new StringBuilder();
            var resolver = new AssemblyResolver(compilation, logger);

            // Load the assembly.
            PEFile? file = null;
            if (metadataReference is not null)
                file = resolver.TryResolve(metadataReference, PEStreamOptions.PrefetchEntireImage);

            if (file is null && assemblyLocation is not null)
                file = new PEFile(assemblyLocation, PEStreamOptions.PrefetchEntireImage);

            if (file is null)
                return null;

            // Initialize a decompiler with default settings.
            var decompiler = new CSharpDecompiler(file, resolver, new DecompilerSettings());
            // Escape invalid identifiers to prevent Roslyn from failing to parse the generated code.
            // (This happens for example, when there is compiler-generated code that is not yet recognized/transformed by the decompiler.)
            decompiler.AstTransforms.Add(new EscapeInvalidIdentifiers());

            var fullTypeName = new FullTypeName(fullName);

            // ILSpy only allows decompiling a type that comes from the 'Main Module'.  They will throw on anything
            // else.  Prevent this by doing this quick check corresponding to:
            // https://github.com/icsharpcode/ILSpy/blob/4ebe075e5859939463ae420446f024f10c3bf077/ICSharpCode.Decompiler/CSharp/CSharpDecompiler.cs#L978
            var type = decompiler.TypeSystem.MainModule.GetTypeDefinition(fullTypeName);
            if (type is null)
                return null;

            // Try to decompile; if an exception is thrown the caller will handle it
            var text = decompiler.DecompileTypeAsString(fullTypeName);

            text += "#if false // " + FeaturesResources.Decompilation_log + Environment.NewLine;
            text += logger.ToString();
            text += "#endif" + Environment.NewLine;

            return document.WithText(SourceText.From(text, encoding: null, checksumAlgorithm: SourceHashAlgorithms.Default));
        }
    }
}
