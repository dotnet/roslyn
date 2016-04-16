// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class NavigationBarProjectItem : NavigationBarItem
    {
        public DocumentId DocumentId { get; }
        public Workspace Workspace { get; }
        public string Language { get; }

        public NavigationBarProjectItem(
            string text,
            Glyph glyph,
            Workspace workspace,
            DocumentId documentId,
            string language,
            int indent = 0,
            bool bolded = false,
            bool grayed = false)
                : base(text, glyph, SpecializedCollections.EmptyList<TextSpan>(), /*childItems:*/ null, indent, bolded, grayed)
        {
            this.Workspace = workspace;
            this.DocumentId = documentId;
            this.Language = language;
        }

        internal void SwitchToContext()
        {
            if (this.Workspace.CanChangeActiveContextDocument)
            {
                // TODO: Can we pass something better?
                this.Workspace.SetDocumentContext(DocumentId);
            }
        }
    }
}
