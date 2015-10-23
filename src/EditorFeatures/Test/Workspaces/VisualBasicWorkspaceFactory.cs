// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
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
        public static TestWorkspace CreateWorkspaceFromLines(params string[] lines)
        {
            return CreateWorkspaceFromLines(lines, exportProvider: null);
        }

        public static TestWorkspace CreateWorkspaceFromLines(
            string[] lines,
            ExportProvider exportProvider,
            string[] metadataReferences = null)
        {
            var file = lines.Join(Environment.NewLine);
            return CreateWorkspaceFromFile(file, exportProvider: exportProvider, metadataReferences: metadataReferences);
        }

        /// <summary>
        /// Creates a single buffer in a workspace. 
        /// </summary>
        /// <param name="content">Lines of text, the buffer contents</param>
        public static TestWorkspace CreateWorkspaceFromFile(
            string file,
            ParseOptions parseOptions = null,
            CompilationOptions compilationOptions = null,
            ExportProvider exportProvider = null,
            string[] metadataReferences = null)
        {
            return CreateWorkspaceFromFiles(new[] { file }, parseOptions, compilationOptions, exportProvider, metadataReferences);
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
