// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;
using Microsoft.VisualStudio.Composition;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
{
    public class VisualBasicWorkspaceFactory : TestWorkspaceFactory
    {
        /// <summary>
        /// Creates a single buffer in a workspace.
        /// </summary>
        /// <param name="lines">Lines of text, the buffer contents</param>
        public static Task<TestWorkspace> CreateWorkspaceFromLinesAsync(params string[] lines)
        {
            return CreateWorkspaceFromLinesAsync(lines, exportProvider: null);
        }

        public static Task<TestWorkspace> CreateWorkspaceFromLinesAsync(
            string[] lines,
            ExportProvider exportProvider,
            string[] metadataReferences = null)
        {
            var file = lines.Join(Environment.NewLine);
            return CreateWorkspaceFromFileAsync(file, exportProvider: exportProvider, metadataReferences: metadataReferences);
        }

        public static Task<TestWorkspace> CreateWorkspaceFromFileAsync(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateWorkspaceFromFilesAsync(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences);
        }

        /// <param name="files">Can pass in multiple file contents: files will be named test1.vb, test2.vb, etc. and additional metadata references</param>
        public static TestWorkspace CreateWorkspaceFromFiles(
            string[] files,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return TestWorkspaceFactory.CreateWorkspaceFromFiles(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider, metadataReferences);
        }

        public static Task<TestWorkspace> CreateWorkspaceFromFilesAsync(
            string[] files,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return TestWorkspaceFactory.CreateWorkspaceFromFilesAsync(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider, metadataReferences);
        }

        /// <param name="files">Can pass in multiple file contents with individual source kind: files will be named test1.vb, test2.vbx, etc.</param>
        public static TestWorkspace CreateWorkspaceFromFiles(
            string[] files,
            ParseOptions[] parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null)
        {
            return TestWorkspaceFactory.CreateWorkspaceFromFiles(LanguageNames.VisualBasic, compilationOptions, parseOptions, files, exportProvider);
        }
    }
}
