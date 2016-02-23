// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
    internal static partial class TextDocumentExtensions
    {
        /// <summary>
        /// Creates a new instance of this text document updated to have the text specified.
        /// </summary>
        public static TextDocument WithText(this TextDocument textDocument, SourceText text)
        {
            var document = textDocument as Document;
            if (document != null)
            {
                return document.WithText(text);
            }

            return textDocument.WithAdditionalDocumentText(text);
        }

        /// <summary>
        /// Creates a new instance of this additional document updated to have the text specified.
        /// </summary>
        public static TextDocument WithAdditionalDocumentText(this TextDocument textDocument, SourceText text)
        {
            Contract.ThrowIfTrue(textDocument is Document);
            return textDocument.Project.Solution.WithAdditionalDocumentText(textDocument.Id, text, PreservationMode.PreserveIdentity).GetTextDocument(textDocument.Id);
        }

        public static TextDocument GetTextDocument(this Project project, DocumentId documentId)
        {
            return project.Solution.GetTextDocument(documentId);
        }

        public static IEnumerable<TextDocument> GetTextDocuments(this Project project)
        {
            return project.Documents.Concat(project.AdditionalDocuments);
        }

        public static IEnumerable<DocumentId> GetTextDocumentIds(this Project project)
        {
            return project.DocumentIds.Concat(project.AdditionalDocumentIds);
        }

        public static IEnumerable<DocumentId> GetTextDocumentIds(this ProjectState project)
        {
            return project.DocumentIds.Concat(project.AdditionalDocumentIds);
        }

        public static TextDocumentState GetTextDocumentState(this ProjectState project, DocumentId documentId)
        {
            return project.GetDocumentState(documentId) ?? project.GetAdditionalDocumentState(documentId);
        }
    }
}
