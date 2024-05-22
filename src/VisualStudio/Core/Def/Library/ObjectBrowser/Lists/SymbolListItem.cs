// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Editor.Shared.Extensions;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;

internal abstract class SymbolListItem : ObjectListItem
{
    private readonly SymbolKey _symbolKey;
    private readonly Accessibility _accessibility;
    private readonly string _displayText;
    private readonly string _fullNameText;
    private readonly string _searchText;

    private readonly bool _supportsGoToDefinition;
    private readonly bool _supportsFindAllReferences;

    protected SymbolListItem(ProjectId projectId, ISymbol symbol, string displayText, string fullNameText, string searchText, bool isHidden)
        : base(projectId, symbol.GetGlyph().GetStandardGlyphGroup(), symbol.GetGlyph().GetStandardGlyphItem(), isHidden)
    {
        _symbolKey = symbol.GetSymbolKey();
        _accessibility = symbol.DeclaredAccessibility;
        _displayText = displayText;
        _fullNameText = fullNameText;
        _searchText = searchText;

        _supportsGoToDefinition = symbol.Kind != SymbolKind.Namespace;
        _supportsFindAllReferences = symbol.Kind != SymbolKind.Namespace;
    }

    public Accessibility Accessibility
    {
        get { return _accessibility; }
    }

    public override string DisplayText
    {
        get { return _displayText; }
    }

    public override string FullNameText
    {
        get { return _fullNameText; }
    }

    public override string SearchText
    {
        get { return _searchText; }
    }

    public override bool SupportsGoToDefinition
    {
        get { return _supportsGoToDefinition; }
    }

    public override bool SupportsFindAllReferences
    {
        get { return _supportsFindAllReferences; }
    }

    public ISymbol ResolveSymbol(Compilation compilation)
        => _symbolKey.Resolve(compilation, ignoreAssemblyKey: false).Symbol;
}
