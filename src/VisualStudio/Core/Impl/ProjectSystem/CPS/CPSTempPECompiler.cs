// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem.CPS
{
    [Export(typeof(ITempPECompiler))]
    internal class CPSTempPECompiler : ITempPECompiler
    {
        public async Task<bool> CompileAsync(IWorkspaceProjectContext context, string outputFileName, string[] filesToInclude, CancellationToken cancellationToken)
        {
			if (filesToInclude == null || filesToInclude.Length == 0)
			{
				throw new ArgumentException(nameof(filesToInclude), "Must specify some files to compile.");
			}
			if (string.IsNullOrWhiteSpace(outputFileName))
			{
				throw new ArgumentException(nameof(outputFileName), "Must specify a filename to output to.");
			}

            var snapshot = ((CPSProject)context).GetProjectSnapshot();
            // Allow for faster checking because projects could be very large
            var files = new HashSet<string>(filesToInclude);

            // Remove all files except the ones we care about
            var documents = snapshot.Documents;
            foreach (var document in documents)
            {
                if (!files.Contains(document.FilePath))
                {
                    snapshot = snapshot.RemoveDocument(document.Id);
                }
            }

            // We want to produce a DLL regardless of project type
            snapshot = snapshot.WithCompilationOptions(snapshot.CompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

            cancellationToken.ThrowIfCancellationRequested();

            var compilation = await snapshot.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
            if (compilation.GetDiagnostics(cancellationToken).HasAnyErrors())
            {
                return false;
            }

            cancellationToken.ThrowIfCancellationRequested();

            var outputPath = Path.GetDirectoryName(outputFileName);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            using (var file = File.OpenWrite(outputFileName))
            {
                return compilation.Emit(file, cancellationToken: cancellationToken).Success;
            }
        }
    }
}
