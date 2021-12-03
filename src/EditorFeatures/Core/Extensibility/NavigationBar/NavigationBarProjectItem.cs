// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal sealed class NavigationBarProjectItem : NavigationBarItem, IEquatable<NavigationBarProjectItem>
    {
        public Workspace Workspace { get; }
        public DocumentId DocumentId { get; }
        public string Language { get; }

        public NavigationBarProjectItem(
            string text,
            Glyph glyph,
            Workspace workspace,
            DocumentId documentId,
            string language)
                : base(text, glyph,
                       spans: ImmutableArray<TextSpan>.Empty,
                       childItems: ImmutableArray<NavigationBarItem>.Empty,
                       indent: 0, bolded: false, grayed: false)
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

        public override bool Equals(object? obj)
            => Equals(obj as NavigationBarProjectItem);

        public bool Equals(NavigationBarProjectItem? item)
            => item is not null &&
               Text == item.Text &&
               Glyph == item.Glyph &&
               Workspace == item.Workspace &&
               DocumentId == item.DocumentId &&
               Language == item.Language;

        public override int GetHashCode()
            => Hash.Combine(Text,
               Hash.Combine((int)Glyph,
               Hash.Combine(Workspace,
               Hash.Combine(DocumentId,
                            Language.GetHashCode()))));
    }
}
