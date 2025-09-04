// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.PooledObjects;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.Collections;
using Microsoft.VisualStudio.LanguageServices.Implementation.Utilities;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.CodeModel.ExternalElements;

public abstract class AbstractExternalCodeType : AbstractExternalCodeElement, EnvDTE.CodeType
{
    internal AbstractExternalCodeType(CodeModelState state, ProjectId projectId, ITypeSymbol symbol)
        : base(state, projectId, symbol)
    {
    }

    protected internal ITypeSymbol TypeSymbol
    {
        get { return (ITypeSymbol)LookupSymbol(); }
    }

    protected override object GetExtenderNames()
        => CodeModelService.GetExternalTypeExtenderNames();

    protected override object GetExtender(string name)
    {
        var type = TypeSymbol;
        if (type == null)
        {
            throw Exceptions.ThrowEUnexpected();
        }

        var assembly = type.ContainingAssembly;
        if (assembly == null)
        {
            return string.Empty;
        }

        var compilation = GetCompilation();
        if (compilation.GetMetadataReference(assembly) is not PortableExecutableReference metadataReference)
        {
            return string.Empty;
        }

        return CodeModelService.GetExternalTypeExtender(name, metadataReference.FilePath);
    }

    public EnvDTE.CodeElements Bases
    {
        get
        {
            var builder = ArrayBuilder<INamedTypeSymbol>.GetInstance();

            var typeSymbol = TypeSymbol;
            if (typeSymbol.TypeKind == TypeKind.Interface)
            {
                builder.AddRange(typeSymbol.AllInterfaces);
            }
            else if (typeSymbol.BaseType != null)
            {
                builder.Add(typeSymbol.BaseType);
            }

            return ExternalTypeCollection.Create(this.State, this, this.ProjectId,
                builder.ToImmutableAndFree());
        }
    }

    public virtual EnvDTE.CodeElements ImplementedInterfaces
    {
        get { throw Exceptions.ThrowENotImpl(); }
    }

    public EnvDTE.CodeElements DerivedTypes
    {
        get { throw Exceptions.ThrowENotImpl(); }
    }

    public bool IsAbstract
    {
        get { return TypeSymbol.IsAbstract; }
    }

    public override bool IsCodeType
    {
        get { return true; }
    }

    public bool IsSealed
    {
        get { return TypeSymbol.IsSealed; }
    }

    public EnvDTE.CodeElements Members
    {
        get { return ExternalMemberCollection.Create(this.State, this, this.ProjectId, this.TypeSymbol); }
    }

    public EnvDTE.CodeNamespace Namespace
    {
        get { return ExternalCodeNamespace.Create(this.State, this.ProjectId, this.TypeSymbol.ContainingNamespace); }
    }

    public bool get_IsDerivedFrom(string fullName)
    {
        var currentType = TypeSymbol;
        if (currentType == null)
        {
            return false;
        }

        var baseType = GetCompilation().GetTypeByMetadataName(fullName);
        if (baseType == null)
        {
            return false;
        }

        return currentType.InheritsFromOrEquals(baseType);
    }

    public EnvDTE.CodeElement AddBase(object @base, object position)
        => throw Exceptions.ThrowEFail();

    public void RemoveBase(object element)
        => throw Exceptions.ThrowEFail();

    public void RemoveMember(object element)
        => throw Exceptions.ThrowEFail();
}
