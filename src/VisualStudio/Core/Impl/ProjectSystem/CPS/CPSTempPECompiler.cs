// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(ITempPECompiler))]
    internal class CPSTempPECompiler : ITempPECompiler
    {
        private readonly VisualStudioWorkspace _workspace;

        [ImportingConstructor]
        public CPSTempPECompiler(VisualStudioWorkspace workspace)
        {
            _workspace = workspace;
        }

        public async Task<bool> CompileAsync(IWorkspaceProjectContext context, string outputFileName, HashSet<string> filesToInclude, CancellationToken cancellationToken)
        {
            if (filesToInclude == null || filesToInclude.Count == 0)
            {
                throw new ArgumentException(nameof(filesToInclude), "Must specify some files to compile.");
            }
            if (outputFileName == null)
            {
                throw new ArgumentException(nameof(outputFileName), "Must specify an output file name.");
            }

            var project = _workspace.CurrentSolution.GetProject(context.Id);

            // Remove all files except the ones we care about
            var documents = project.Documents;
            foreach (var document in documents)
            {
                if (!filesToInclude.Contains(document.FilePath))
                {
                    project = project.RemoveDocument(document.Id);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            var options = project.LanguageServices.GetRequiredService<ICompilationFactoryService>().GetDefaultCompilationOptions()
                    // copied from TempPECompilerServices.cs used by legacy, for parity
                    .WithAssemblyIdentityComparer(DesktopAssemblyIdentityComparer.Default)
                    .WithSourceReferenceResolver(SourceFileResolver.Default)
                    .WithXmlReferenceResolver(XmlFileResolver.Default)
                    // we always want to produce a DLL
                    .WithOutputKind(OutputKind.DynamicallyLinkedLibrary);

            project = project.WithCompilationOptions(options);

            var compilation = await project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            var outputPath = Path.GetDirectoryName(outputFileName);

            Directory.CreateDirectory(outputPath);

            using (var file = FileUtilities.CreateFileStreamChecked(File.Create, outputFileName, nameof(outputFileName)))
            {
                return compilation.Emit(file, cancellationToken: cancellationToken).Success;
            }
        }
    }
}
