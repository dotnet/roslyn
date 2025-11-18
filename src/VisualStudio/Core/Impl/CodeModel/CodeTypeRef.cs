// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

// Breaking changes from the legacy code model.
//
// CodeType: In the legacy Visual Basic code model, this property might return null. However, in
//     Roslyn this will always return a valid CodeType.
//
// TypeKind: In the legacy Visual Basic code model, this property would return vsCMTypeRefCodeType
//     if there was a valid code type -- even if it were some well-known type with a specific type
//     kind, like System.Int32 (vsCMTypeRefInt). In Roslyn, this will return the specific type kind,
//     regardless of the CodeType property.

using System;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Interop;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel;

[ComVisible(true)]
[ComDefaultInterface(typeof(EnvDTE.CodeTypeRef))]
public sealed class CodeTypeRef : AbstractCodeModelObject, EnvDTE.CodeTypeRef, EnvDTE80.CodeTypeRef2
{
    internal static EnvDTE.CodeTypeRef Create(CodeModelState state, object parent, ProjectId projectId, ITypeSymbol typeSymbol)
    {
        var newElement = new CodeTypeRef(state, parent, projectId, typeSymbol);
        return (EnvDTE.CodeTypeRef)ComAggregate.CreateAggregatedObject(newElement);
    }

    private readonly ParentHandle<object> _parentHandle;
    private readonly ProjectId _projectId;
    private readonly SymbolKey _symbolId;

    private CodeTypeRef(CodeModelState state, object parent, ProjectId projectId, ITypeSymbol typeSymbol)
        : base(state)
    {
        _parentHandle = new ParentHandle<object>(parent);
        _projectId = projectId;
        _symbolId = typeSymbol.GetSymbolKey();
    }

    internal ITypeSymbol LookupTypeSymbol()
    {
        if (CodeModelService.ResolveSymbol(this.State.Workspace, _projectId, _symbolId) is not ITypeSymbol typeSymbol)
        {
            throw Exceptions.ThrowEFail();
        }

        return typeSymbol;
    }

    public string AsFullName
    {
        get { return CodeModelService.GetAsFullNameForCodeTypeRef(LookupTypeSymbol()); }
    }

    public string AsString
    {
        get { return CodeModelService.GetAsStringForCodeTypeRef(LookupTypeSymbol()); }
    }

    public EnvDTE.CodeType CodeType
    {
        get { return (EnvDTE.CodeType)CodeModelService.CreateCodeType(this.State, _projectId, LookupTypeSymbol()); }
        set { throw Exceptions.ThrowENotImpl(); }
    }

    public EnvDTE.CodeTypeRef CreateArrayType(int rank)
    {
        var project = Workspace.CurrentSolution.GetProject(_projectId);
        if (project == null)
        {
            throw Exceptions.ThrowEFail();
        }

        var arrayType = project.GetCompilationAsync().Result.CreateArrayTypeSymbol(LookupTypeSymbol(), rank);
        return CodeTypeRef.Create(this.State, null, _projectId, arrayType);
    }

    public EnvDTE.CodeTypeRef ElementType
    {
        get
        {
            var typeSymbol = LookupTypeSymbol();
            if (typeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array)
            {
                return CodeTypeRef.Create(this.State, this, _projectId, ((IArrayTypeSymbol)typeSymbol).ElementType);
            }
            else if (typeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Pointer)
            {
                return CodeTypeRef.Create(this.State, this, _projectId, ((IPointerTypeSymbol)typeSymbol).PointedAtType);
            }
            else
            {
                throw Exceptions.ThrowEFail();
            }
        }

        set
        {
            throw Exceptions.ThrowENotImpl();
        }
    }

    public bool IsGeneric
    {
        get
        {
            return LookupTypeSymbol() is INamedTypeSymbol namedTypeSymbol
                && namedTypeSymbol.GetAllTypeArguments().Any();
        }
    }

    public object Parent
    {
        get { return _parentHandle.Value; }
    }

    public int Rank
    {
        get
        {
            var typeSymbol = LookupTypeSymbol();
            if (typeSymbol.TypeKind == Microsoft.CodeAnalysis.TypeKind.Array)
            {
                return ((IArrayTypeSymbol)typeSymbol).Rank;
            }

            throw Exceptions.ThrowEFail();
        }

        set
        {
            throw new NotImplementedException();
        }
    }

    public EnvDTE.vsCMTypeRef TypeKind
    {
        get { return CodeModelService.GetTypeKindForCodeTypeRef(LookupTypeSymbol()); }
    }
}
