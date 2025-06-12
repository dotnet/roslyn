// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;

[ComVisible(true)]
[ComDefaultInterface(typeof(ICodeElements))]
public sealed class ExternalTypeCollection : AbstractCodeElementCollection
{
    internal static EnvDTE.CodeElements Create(
        CodeModelState state,
        object parent,
        ProjectId projectId,
        ImmutableArray<INamedTypeSymbol> typeSymbols)
    {
        var collection = new ExternalTypeCollection(state, parent, projectId, typeSymbols);
        return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
    }

    private readonly ProjectId _projectId;
    private readonly ImmutableArray<INamedTypeSymbol> _typeSymbols;

    private ExternalTypeCollection(CodeModelState state, object parent, ProjectId projectId, ImmutableArray<INamedTypeSymbol> typeSymbols)
        : base(state, parent)
    {
        _projectId = projectId;
        _typeSymbols = typeSymbols;
    }

    protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
    {
        if (index < _typeSymbols.Length)
        {
            element = this.State.CodeModelService.CreateCodeType(this.State, _projectId, _typeSymbols[index]);
            return true;
        }

        element = null;
        return false;
    }

    protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
    {
        var index = _typeSymbols.IndexOf(t => t.Name == name);

        if (index >= 0 && index < _typeSymbols.Length)
        {
            element = this.State.CodeModelService.CreateCodeType(this.State, _projectId, _typeSymbols[index]);
            return true;
        }

        element = null;
        return false;
    }

    public override int Count
    {
        get { return _typeSymbols.Length; }
    }
}
