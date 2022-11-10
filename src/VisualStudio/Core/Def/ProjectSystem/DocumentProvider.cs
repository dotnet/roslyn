// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.ProjectSystem
{
    [Obsolete("This is a compatibility shim for TypeScript; please do not use it.")]
    internal sealed class DocumentProvider
    {
        internal class ShimDocument : IVisualStudioHostDocument
        {
            public ShimDocument(AbstractProject hostProject, DocumentId id, string filePath, SourceCodeKind sourceCodeKind = SourceCodeKind.Regular)
            {
                Project = hostProject;
                Id = id ?? DocumentId.CreateNewId(hostProject.Id, filePath);
                FilePath = filePath;
                SourceCodeKind = sourceCodeKind;
            }

            public AbstractProject Project { get; }

            public DocumentId Id { get; }

            public string FilePath { get; }

            public SourceCodeKind SourceCodeKind { get; }
        }
    }
}
