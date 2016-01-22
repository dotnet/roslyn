// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.Composition;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public partial class CSharpWorkspaceFactory : TestWorkspaceFactory
    {
        public static Task<TestWorkspace> CreateWorkspaceFromFileAsync(
            string file,
            CSharpParseOptions parseOptions = null,
            CSharpCompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateWorkspaceFromFilesAsync(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences);
        }

        public static Task<TestWorkspace> CreateWorkspaceFromFilesAsync(
            string[] files,
            CSharpParseOptions parseOptions = null,
            CSharpCompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateWorkspaceFromFilesAsync(LanguageNames.CSharp, compilationOptions, parseOptions, files, exportProvider, metadataReferences);
        }

        public static Task<TestWorkspace> CreateWorkspaceFromFilesAsync(
            string[] files,
            CSharpParseOptions[] parseOptions = null,
            CSharpCompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null)
        {
            return CreateWorkspaceFromFilesAsync(LanguageNames.CSharp, compilationOptions, parseOptions, files, exportProvider);
        }
    }
}