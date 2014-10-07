// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis
{
    public partial class MSBuildWorkspace
    {
        internal class DocumentData
        {
            private MSBuildWorkspace workspace;
            private DocumentInfo initialState;

            public DocumentData(
                MSBuildWorkspace workspace,
                DocumentId id,
                string filePath,
                string name,
                IEnumerable<string> folders,
                SourceCodeKind sourceCodeKind,
                bool isGenerated)
            {
                this.workspace = workspace;
                this.initialState = DocumentInfo.Create(id, name, folders, sourceCodeKind, loader: new FileTextLoader(filePath), filePath: filePath, isGenerated: isGenerated);
            }

            public DocumentId Id
            {
                get { return this.initialState.Id; }
            }

            public string FilePath
            {
                get { return this.initialState.FilePath; }
            }

            public DocumentInfo InitialState
            {
                get { return this.initialState; }
            }

            public virtual void Save(SourceText newText)
            {
                try
                {
                    var dir = Path.GetDirectoryName(this.initialState.FilePath);
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    using (var writer = new StreamWriter(this.initialState.FilePath))
                    {
                        newText.Write(writer);
                    }
                }
                catch (System.IO.IOException exception)
                {
                    this.workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, this.Id));
                }
            }

            public virtual void DeleteFile()
            {
                var fullPath = this.initialState.FilePath;

                try
                {
                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                    }
                }
                catch (System.IO.IOException exception)
                {
                    this.workspace.OnWorkspaceFailed(new DocumentDiagnostic(WorkspaceDiagnosticKind.FileAccessFailure, exception.Message, this.Id));
                }
            }
        }
    }
}