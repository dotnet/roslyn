using System.Collections.Generic;
using System.Linq;
using Roslyn.Compilers;
using Ref = System.Reflection;
using Roslyn.Compilers.CSharp;
using System;
using Roslyn.Compilers.Common;
using System.Diagnostics;
using System.IO;

namespace Roslyn.Scripting.CSharp
{
    /// <summary>
    /// Represents a runtime execution context for C# scripts.
    /// </summary>
    public sealed class ScriptEngine : CommonScriptEngine
    {
        private static readonly ParseOptions DefaultInteractive = new ParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Interactive);
        private static readonly ParseOptions DefaultScript = new ParseOptions(languageVersion: LanguageVersion.CSharp6, kind: SourceCodeKind.Script);

        public ScriptEngine(MetadataFileProvider metadataFileProvider = null, IAssemblyLoader assemblyLoader = null)
            : base(metadataFileProvider, assemblyLoader)
        {
        }

        internal override CommonCompilation CreateCompilation(IText code, string path, bool isInteractive, Session session, Type returnType, DiagnosticBag diagnostics)
        {
            Debug.Assert(session != null);
            Debug.Assert(code != null && path != null && diagnostics != null);

            Compilation previousSubmission = (Compilation)session.LastSubmission;

            var references = session.GetReferencesForCompilation();
            var usings = session.GetNamespacesForCompilation();

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
                    allowUnsafe: true,                  // TODO (tomat)
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
                session.FileResolver,
                this.metadataFileProvider,
                returnType,
                session.HostObjectType
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
