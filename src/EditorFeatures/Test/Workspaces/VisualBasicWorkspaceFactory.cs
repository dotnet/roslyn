// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class VisualBasicWorkspaceFactory : TestWorkspaceFactory
    {
        public static Task<TestWorkspace> CreateVisualBasicWorkspaceFromFileAsync(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateVisualBasicWorkspaceFromFilesAsync(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences);
        }

        public static Task<TestWorkspace> CreateVisualBasicWorkspaceFromFilesAsync(
            string[] files,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return TestWorkspaceFactory.CreateWorkspaceFromFilesAsync(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider, metadataReferences);
        }

        /// <param name="files">Can pass in multiple file contents with individual source kind: files will be named test1.vb, test2.vbx, etc.</param>
        public static Task<TestWorkspace> CreateVisualBasicWorkspaceFromFilesAsync(
            string[] files,
            ParseOptions[] parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null)
        {
            return TestWorkspaceFactory.CreateWorkspaceFromFilesAsync(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider);
        }
    }
}
