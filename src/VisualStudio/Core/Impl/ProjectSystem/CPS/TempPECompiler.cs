// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(ITempPECompiler))]
    internal class TempPECompiler : ITempPECompiler
    {
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public TempPECompiler(VisualStudioWorkspace workspace)
            => _workspace = workspace;

        public async Task<bool> CompileAsync(IWorkspaceProjectContext context, string outputFileName, ISet<string> filesToInclude, CancellationToken cancellationToken)
        {
            if (filesToInclude == null || filesToInclude.Count == 0)
            {
                throw new ArgumentException(nameof(filesToInclude), "Must specify some files to compile.");
            }

            if (outputFileName == null)
            {
                throw new ArgumentException(nameof(outputFileName), "Must specify an output file name.");
            }

            var project = _workspace.CurrentSolution.GetRequiredProject(context.Id);

            // Start by fetching the compilation we have already have that will have references correct
            var compilation = await project.GetRequiredCompilationAsync(cancellationToken).ConfigureAwait(false);

            // Update to just the syntax trees we need to keep
            var syntaxTrees = new List<SyntaxTree>(capacity: filesToInclude.Count);
            foreach (var document in project.Documents)
            {
                if (document.FilePath != null && filesToInclude.Contains(document.FilePath))
                {
                    syntaxTrees.Add(await document.GetRequiredSyntaxTreeAsync(cancellationToken).ConfigureAwait(false));
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTrees);

            // We need to inherit most of the projects options, mainly for VB (RootNamespace, GlobalImports etc.), but we need to override about some specific things surrounding the output
            compilation = compilation.WithOptions(compilation.Options
                    // copied from the old TempPE compiler used by legacy, for parity.
                    // See: https://github.com/dotnet/roslyn/blob/fab7134296816fc80019c60b0f5bef7400cf23ea/src/VisualStudio/CSharp/Impl/ProjectSystemShim/TempPECompilerService.cs#L58
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .WithSourceReferenceResolver(SourceFileResolver.Default)
                    .WithXmlReferenceResolver(XmlFileResolver.Default)
                    // We always want to produce a debug, AnyCPU DLL
                    .WithOutputKind(OutputKind.DynamicallyLinkedLibrary)
                    .WithPlatform(Platform.AnyCpu)
                    .WithOptimizationLevel(OptimizationLevel.Debug)
                    // Turn off any warnings as errors just in case
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
                    .WithReportSuppressedDiagnostics(false)
                    .WithSpecificDiagnosticOptions(null)
                    // Turn off any signing and strong naming
                    .WithDelaySign(false)
                    .WithCryptoKeyFile(null)
                    .WithPublicSign(false)
                    .WithStrongNameProvider(null));

            // AssemblyName should be set to the filename of the output file because multiple TempPE DLLs can be created for the same project
            compilation = compilation.WithAssemblyName(Path.GetFileName(outputFileName));

            cancellationToken.ThrowIfCancellationRequested();

            var outputPath = Path.GetDirectoryName(outputFileName);

            Directory.CreateDirectory(outputPath);

            using var file = FileUtilities.CreateFileStreamChecked(File.Create, outputFileName, nameof(outputFileName));
            return compilation.Emit(file, cancellationToken: cancellationToken).Success;
        }
    }
}
