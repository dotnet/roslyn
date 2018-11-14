// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.LanguageServices.ProjectSystem;
using Roslyn.Utilities;

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
            if (outputFileName == null)
            {
                throw new ArgumentException(nameof(outputFileName), "Must specify an output file name.");
            }

            var project = ((CPSProject)context).GetProjectSnapshot();

            // Allow for faster checking because projects could be very large
            var files = new HashSet<string>(filesToInclude, StringComparer.OrdinalIgnoreCase);

            // Remove all files except the ones we care about
            var documents = project.Documents;
            foreach (var document in documents)
            {
                if (!files.Contains(document.FilePath))
                {
                    project = project.RemoveDocument(document.Id);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            // We want to produce a DLL regardless of project type
            project = project.WithCompilationOptions(project.CompilationOptions.WithOutputKind(OutputKind.DynamicallyLinkedLibrary));

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
