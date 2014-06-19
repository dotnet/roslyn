using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Roslyn.Compilers;
using Roslyn.Compilers.Common;
using Roslyn.Compilers.CSharp;
using Roslyn.Utilities;

namespace Roslyn.Scripting.CSharp
{
    internal sealed class SubmissionCompilationFactory : CommonSubmissionCompilationFactory
    {
        private static readonly ParseOptions DefaultInteractive = new ParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Interactive);
        private static readonly ParseOptions DefaultScript = new ParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Script);

        public SubmissionCompilationFactory(IEnumerable<string> importedNamespaces, MetadataFileProvider metadataFileProvider)
            : base(CheckNamespaces(importedNamespaces), metadataFileProvider)
        {
        }

        private static IEnumerable<string> CheckNamespaces(IEnumerable<string> importedNamespaces)
        {
            if (importedNamespaces != null)
            {
                string invalidNamespace = importedNamespaces.FirstOrDefault(u => !u.IsValidClrNamespaceName());
                if (invalidNamespace != null)
                {
                    throw new ArgumentException(String.Format("Invalid namespace name: '{0}'", invalidNamespace), "importedNamespaces");
                }
            }

            return importedNamespaces;
        }

        internal override CommonCompilation CreateCompilation(IText code, string path, bool isInteractive, Session session, Type returnType, DiagnosticBag diagnostics)
        {
            Debug.Assert(code != null && path != null && diagnostics != null);

            Compilation previousSubmission = (session != null) ? (Compilation)session.LastSubmission : null;

            IEnumerable<MetadataReference> references = GetReferences(session);
            ReadOnlyArray<string> usings = GetImportedNamespaces(session);

            // TODO (tomat): BaseDirectory should be a property on ScriptEngine?
            var fileResolver = Session.GetFileResolver(session, Directory.GetCurrentDirectory());

            // parse:
            var parseOptions = isInteractive ? DefaultInteractive : DefaultScript;
            var tree = SyntaxTree.ParseText(code, path, parseOptions);
            diagnostics.Add(tree.GetDiagnostics());
            if (diagnostics.HasAnyErrors())
            {
                return null;
            }

            // create compilation:
            string assemblyName, submissionTypeName;
            GenerateSubmissionId(out assemblyName, out submissionTypeName);

            var compilation = Compilation.CreateSubmission(
                assemblyName,
                new CompilationOptions(
                    outputKind: OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName: null,
                    scriptClassName: submissionTypeName,
                    usings: usings.ToList(),
                    optimize: false,                    // TODO (tomat)
                    checkOverflow: true,                // TODO (tomat)
                    allowUnsafe: false,                 // TODO (tomat)
                    cryptoKeyContainer: null,
                    cryptoKeyFile: null,
                    delaySign: null,
                    fileAlignment: 0,
                    baseAddress: 0L,
                    platform: Platform.AnyCPU,
                    generalWarningOption: ReportWarning.Default,
                    warningLevel: 4,
                    specificWarningOptions: null,
                    highEntropyVirtualAddressSpace: false
                ),
                tree,
                previousSubmission,
                references,
                fileResolver,
                this.metadataFileProvider,
                returnType,
                (session != null) ? session.HostObjectType : null
            );

            ValidateReferences(compilation, diagnostics);
            if (diagnostics.HasAnyErrors())
            {
                return null;
            }

            return compilation;
        }

        /// <summary>
        /// Checks that the compilation doesn't have any references whose name start with the reserved prefix.
        /// </summary>
        internal void ValidateReferences(CommonCompilation compilation, DiagnosticBag diagnostics)
        {
            foreach (AssemblyIdentity reference in compilation.ReferencedAssemblyNames)
            {
                if (IsReservedAssemblyName(reference))
                {
                    diagnostics.Add(ErrorCode.ERR_ReservedAssemblyName, null, reference.GetDisplayName());
                }
            }
        }
    }
}
