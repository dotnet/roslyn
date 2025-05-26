// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.Internal.VisualStudio.PlatformUI;
using Roslyn.Utilities;
using Microsoft.VisualStudio.Imaging.Interop;
using Microsoft.CodeAnalysis.Editor.Wpf;
using System.Linq;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.SolutionExplorer;

internal sealed class SymbolTreeItem(
    string name,
    Glyph glyph,
    SyntaxNode syntaxNode)
    : BaseItem(name, canPreview: true),
    IInvocationController
{
    public RootSymbolTreeItemSourceProvider SourceProvider = null!;
    public DocumentId DocumentId = null!;
    public ISolutionExplorerSymbolTreeItemProvider ItemProvider = null!;

    public override ImageMoniker IconMoniker { get; } = glyph.GetImageMoniker();

    public readonly SyntaxNode SyntaxNode = syntaxNode;

    public override IInvocationController? InvocationController => this;

    public bool Invoke(IEnumerable<object> items, InputSource inputSource, bool preview)
    {
        if (items.FirstOrDefault() is not SymbolTreeItem item)
            return false;

        SourceProvider.NavigateTo(item, preview);
        return true;
    }
}
