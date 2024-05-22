// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Editor;

internal sealed class NavigationBarProjectItem(
    string text,
    Glyph glyph,
    Workspace workspace,
    DocumentId documentId,
    string language) : NavigationBarItem(textVersion: null, text, glyph,
               spans: ImmutableArray<TextSpan>.Empty,
               childItems: ImmutableArray<NavigationBarItem>.Empty,
               indent: 0, bolded: false, grayed: false), IEquatable<NavigationBarProjectItem>
{
    public Workspace Workspace { get; } = workspace;
    public DocumentId DocumentId { get; } = documentId;
    public string Language { get; } = language;

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
        => base.Equals(item) &&
           Workspace == item.Workspace &&
           DocumentId == item.DocumentId &&
           Language == item.Language;

    public override int GetHashCode()
        => throw new NotImplementedException();
}
