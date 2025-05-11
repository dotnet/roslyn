// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser.Lists;

internal sealed class ReferenceListItem : ObjectListItem
{
    private readonly string _name;

    public ReferenceListItem(ProjectId projectId, string name, MetadataReference reference)
        : base(projectId, StandardGlyphGroup.GlyphAssembly)
    {
        _name = name;
        MetadataReference = reference;
    }

    public override string DisplayText
    {
        get { return _name; }
    }

    public override string FullNameText
    {
        get { return _name; }
    }

    public override string SearchText
    {
        get { return _name; }
    }

    public MetadataReference MetadataReference { get; }

    public IAssemblySymbol GetAssembly(Compilation compilation)
        => compilation.GetAssemblyOrModuleSymbol(MetadataReference) as IAssemblySymbol;
}
