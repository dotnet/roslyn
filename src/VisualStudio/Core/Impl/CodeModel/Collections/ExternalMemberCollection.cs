// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;

[ComVisible(true)]
[ComDefaultInterface(typeof(ICodeElements))]
public sealed class ExternalMemberCollection : AbstractCodeElementCollection
{
    internal static EnvDTE.CodeElements Create(
        CodeModelState state,
        object parent,
        ProjectId projectId,
        ITypeSymbol typeSymbol)
    {
        var collection = new ExternalMemberCollection(state, parent, projectId, typeSymbol);
        return (EnvDTE.CodeElements)ComAggregate.CreateAggregatedObject(collection);
    }

    private readonly ProjectId _projectId;
    private readonly SymbolKey _typeSymbolId;
    private ImmutableArray<EnvDTE.CodeElement> _children;

    private ExternalMemberCollection(CodeModelState state, object parent, ProjectId projectId, ITypeSymbol typeSymbol)
        : base(state, parent)
    {
        _projectId = projectId;
        _typeSymbolId = typeSymbol.GetSymbolKey();
    }

    private ImmutableArray<EnvDTE.CodeElement> GetChildren()
    {
        if (_children == null)
        {
            var project = this.State.Workspace.CurrentSolution.GetProject(_projectId);
            if (project == null)
            {
                throw Exceptions.ThrowEFail();
            }

            if (_typeSymbolId.Resolve(project.GetCompilationAsync().Result).Symbol is not ITypeSymbol typeSymbol)
            {
                throw Exceptions.ThrowEFail();
            }

            var childrenBuilder = ArrayBuilder<EnvDTE.CodeElement>.GetInstance();

            foreach (var member in typeSymbol.GetMembers())
            {
                if (this.CodeModelService.IsValidExternalSymbol(member))
                {
                    childrenBuilder.Add(this.State.CodeModelService.CreateExternalCodeElement(this.State, _projectId, member));
                }
            }

            foreach (var typeMember in typeSymbol.GetTypeMembers())
            {
                childrenBuilder.Add(this.State.CodeModelService.CreateExternalCodeElement(this.State, _projectId, typeMember));
            }

            _children = childrenBuilder.ToImmutableAndFree();
        }

        return _children;
    }

    protected override bool TryGetItemByIndex(int index, out EnvDTE.CodeElement element)
    {
        var children = GetChildren();
        if (index < children.Length)
        {
            element = children[index];
            return true;
        }

        element = null;
        return false;
    }

    protected override bool TryGetItemByName(string name, out EnvDTE.CodeElement element)
    {
        var children = GetChildren();
        var index = children.IndexOf(e => e.Name == name);

        if (index < children.Length)
        {
            element = children[index];
            return true;
        }

        element = null;
        return false;
    }

    public override int Count
    {
        get { return GetChildren().Length; }
    }
}
