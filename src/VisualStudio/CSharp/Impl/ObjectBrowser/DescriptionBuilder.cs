// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Microsoft.VisualStudio.LanguageServices.Implementation.Library.ObjectBrowser;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.CSharp.ObjectBrowser;

internal sealed class DescriptionBuilder(
    IVsObjectBrowserDescription3 description,
    ObjectBrowserLibraryManager libraryManager,
    ObjectListItem listItem,
    Project project) : AbstractDescriptionBuilder(description, libraryManager, listItem, project)
{
    protected override void BuildNamespaceDeclaration(INamespaceSymbol namespaceSymbol, _VSOBJDESCOPTIONS options)
    {
        AddText("namespace ");
        AddName(namespaceSymbol.ToDisplayString());
    }

    protected override async Task BuildDelegateDeclarationAsync(
        INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        Debug.Assert(typeSymbol.TypeKind == TypeKind.Delegate);

        BuildTypeModifiers(typeSymbol);
        AddText("delegate ");

        var delegateInvokeMethod = typeSymbol.DelegateInvokeMethod;

        await AddTypeLinkAsync(delegateInvokeMethod.ReturnType, LinkFlags.None, cancellationToken).ConfigureAwait(true);
        AddText(" ");

        var typeQualificationStyle = (options & _VSOBJDESCOPTIONS.ODO_USEFULLNAME) != 0
            ? SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces
            : SymbolDisplayTypeQualificationStyle.NameOnly;

        var typeNameFormat = new SymbolDisplayFormat(
            typeQualificationStyle: typeQualificationStyle,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

        AddName(typeSymbol.ToDisplayString(typeNameFormat));

        AddText("(");
        await BuildParameterListAsync(delegateInvokeMethod.Parameters, cancellationToken).ConfigureAwait(true);
        AddText(")");

        if (typeSymbol.IsGenericType)
            await BuildGenericConstraintsAsync(typeSymbol, cancellationToken).ConfigureAwait(true);
    }

    protected override async Task BuildTypeDeclarationAsync(
        INamedTypeSymbol typeSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        BuildTypeModifiers(typeSymbol);

        switch (typeSymbol.TypeKind)
        {
            case TypeKind.Enum:
                AddText("enum ");
                break;

            case TypeKind.Struct:
                AddText("struct ");
                break;

            case TypeKind.Interface:
                AddText("interface ");
                break;

            case TypeKind.Class:
                AddText("class ");
                break;

            default:
                Debug.Fail("Invalid type kind encountered: " + typeSymbol.TypeKind.ToString());
                break;
        }

        var typeNameFormat = new SymbolDisplayFormat(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

        AddName(typeSymbol.ToDisplayString(typeNameFormat));

        if (typeSymbol.TypeKind == TypeKind.Enum)
        {
            var underlyingType = typeSymbol.EnumUnderlyingType;
            if (underlyingType != null)
            {
                if (underlyingType.SpecialType != SpecialType.System_Int32)
                {
                    AddText(" : ");
                    await AddTypeLinkAsync(underlyingType, LinkFlags.None, cancellationToken).ConfigureAwait(true);
                }
            }
        }
        else
        {
            var baseType = typeSymbol.BaseType;
            if (baseType != null)
            {
                if (baseType.SpecialType is not SpecialType.System_Object and
                    not SpecialType.System_Delegate and
                    not SpecialType.System_MulticastDelegate and
                    not SpecialType.System_Enum and
                    not SpecialType.System_ValueType)
                {
                    AddText(" : ");
                    await AddTypeLinkAsync(baseType, LinkFlags.None, cancellationToken).ConfigureAwait(true);
                }
            }
        }

        if (typeSymbol.IsGenericType)
            await BuildGenericConstraintsAsync(typeSymbol, cancellationToken).ConfigureAwait(true);
    }

    private void BuildAccessibility(ISymbol symbol)
    {
        switch (symbol.DeclaredAccessibility)
        {
            case Accessibility.Public:
                AddText("public ");
                break;

            case Accessibility.Private:
                AddText("private ");
                break;

            case Accessibility.Protected:
                AddText("protected ");
                break;

            case Accessibility.Internal:
                AddText("internal ");
                break;

            case Accessibility.ProtectedOrInternal:
                AddText("protected internal ");
                break;

            case Accessibility.ProtectedAndInternal:
                AddText("private protected ");
                break;

            default:
                AddText("internal ");
                break;
        }
    }

    private void BuildTypeModifiers(INamedTypeSymbol typeSymbol)
    {
        BuildAccessibility(typeSymbol);

        if (typeSymbol.IsStatic)
        {
            AddText("static ");
        }

        if (typeSymbol.IsAbstract &&
            typeSymbol.TypeKind != TypeKind.Interface)
        {
            AddText("abstract ");
        }

        if (typeSymbol.IsSealed &&
            typeSymbol.TypeKind != TypeKind.Struct &&
            typeSymbol.TypeKind != TypeKind.Enum &&
            typeSymbol.TypeKind != TypeKind.Delegate)
        {
            AddText("sealed ");
        }
    }

    protected override async Task BuildMethodDeclarationAsync(
        IMethodSymbol methodSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        BuildMemberModifiers(methodSymbol);

        if (methodSymbol.MethodKind is not MethodKind.Constructor and
            not MethodKind.Destructor and
            not MethodKind.StaticConstructor and
            not MethodKind.Conversion)
        {
            await AddTypeLinkAsync(methodSymbol.ReturnType, LinkFlags.None, cancellationToken).ConfigureAwait(true);
            AddText(" ");
        }

        if (methodSymbol.MethodKind == MethodKind.Conversion)
        {
            switch (methodSymbol.Name)
            {
                case WellKnownMemberNames.ImplicitConversionName:
                    AddName("implicit operator ");
                    break;

                case WellKnownMemberNames.ExplicitConversionName:
                    AddName("explicit operator ");
                    break;
            }

            await AddTypeLinkAsync(methodSymbol.ReturnType, LinkFlags.None, cancellationToken).ConfigureAwait(true);
        }
        else
        {
            var methodNameFormat = new SymbolDisplayFormat(
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance);

            AddName(methodSymbol.ToDisplayString(methodNameFormat));
        }

        AddText("(");

        if (methodSymbol.IsExtensionMethod)
        {
            AddText("this ");
        }

        await BuildParameterListAsync(methodSymbol.Parameters, cancellationToken).ConfigureAwait(true);
        AddText(")");

        if (methodSymbol.IsGenericMethod)
            await BuildGenericConstraintsAsync(methodSymbol, cancellationToken).ConfigureAwait(true);
    }

    private void BuildMemberModifiers(ISymbol memberSymbol)
    {
        if (memberSymbol.ContainingType != null && memberSymbol.ContainingType.TypeKind == TypeKind.Interface)
        {
            return;
        }

        var methodSymbol = memberSymbol as IMethodSymbol;
        var fieldSymbol = memberSymbol as IFieldSymbol;

        if (methodSymbol != null &&
            methodSymbol.MethodKind == MethodKind.Destructor)
        {
            return;
        }

        if (fieldSymbol != null &&
            fieldSymbol.ContainingType.TypeKind == TypeKind.Enum)
        {
            return;
        }

        // TODO: 'new' modifier isn't exposed on symbols. Do we need it?

        // Note: we don't display the access modifier for static constructors
        if (methodSymbol == null ||
            methodSymbol.MethodKind != MethodKind.StaticConstructor)
        {
            BuildAccessibility(memberSymbol);
        }

        if (memberSymbol.RequiresUnsafeModifier())
        {
            AddText("unsafe ");
        }

        // Note: we don't display 'static' for constant fields
        if (memberSymbol.IsStatic &&
            (fieldSymbol == null || !fieldSymbol.IsConst))
        {
            AddText("static ");
        }

        if (memberSymbol.IsExtern)
        {
            AddText("extern ");
        }

        if (fieldSymbol != null && fieldSymbol.IsReadOnly)
        {
            AddText("readonly ");
        }

        if (fieldSymbol != null && fieldSymbol.IsConst)
        {
            AddText("const ");
        }

        if (fieldSymbol != null && fieldSymbol.IsVolatile)
        {
            AddText("volatile ");
        }

        if (memberSymbol.IsAbstract)
        {
            AddText("abstract ");
        }
        else if (memberSymbol.IsOverride)
        {
            if (memberSymbol.IsSealed)
            {
                AddText("sealed ");
            }

            AddText("override ");
        }
        else if (memberSymbol.IsVirtual)
        {
            AddText("virtual ");
        }
    }

    private async Task BuildGenericConstraintsAsync(INamedTypeSymbol typeSymbol, CancellationToken cancellationToken)
    {
        foreach (var typeParameterSymbol in typeSymbol.TypeParameters)
            await BuildConstraintsAsync(typeParameterSymbol, cancellationToken).ConfigureAwait(true);
    }

    private async Task BuildGenericConstraintsAsync(IMethodSymbol methodSymbol, CancellationToken cancellationToken)
    {
        foreach (var typeParameterSymbol in methodSymbol.TypeParameters)
            await BuildConstraintsAsync(typeParameterSymbol, cancellationToken).ConfigureAwait(true);
    }

    private async Task BuildConstraintsAsync(ITypeParameterSymbol typeParameterSymbol, CancellationToken cancellationToken)
    {
        if (typeParameterSymbol.ConstraintTypes.Length == 0 &&
            !typeParameterSymbol.HasConstructorConstraint &&
            !typeParameterSymbol.HasReferenceTypeConstraint &&
            !typeParameterSymbol.HasValueTypeConstraint &&
            !typeParameterSymbol.AllowsRefLikeType)
        {
            return;
        }

        AddLineBreak();
        AddText("\t");
        AddText("where ");
        AddName(typeParameterSymbol.Name);
        AddText(" : ");

        var isFirst = true;

        if (typeParameterSymbol.HasReferenceTypeConstraint)
        {
            if (!isFirst)
            {
                AddComma();
            }

            AddText("class");
            isFirst = false;
        }

        if (typeParameterSymbol.HasValueTypeConstraint)
        {
            if (!isFirst)
            {
                AddComma();
            }

            AddText("struct");
            isFirst = false;
        }

        foreach (var constraintType in typeParameterSymbol.ConstraintTypes)
        {
            if (!isFirst)
            {
                AddComma();
            }

            await AddTypeLinkAsync(constraintType, LinkFlags.None, cancellationToken).ConfigureAwait(true);
            isFirst = false;
        }

        if (typeParameterSymbol.HasConstructorConstraint)
        {
            if (!isFirst)
            {
                AddComma();
            }

            AddText("new()");
            isFirst = false;
        }

        if (typeParameterSymbol.AllowsRefLikeType)
        {
            if (!isFirst)
            {
                AddComma();
            }

            AddText("allows ref struct");
        }
    }

    private async Task BuildParameterListAsync(
        ImmutableArray<IParameterSymbol> parameters, CancellationToken cancellationToken)
    {
        var count = parameters.Length;
        if (count == 0)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            if (i > 0)
            {
                AddComma();
            }

            var current = parameters[i];

            if (current.IsOptional)
            {
                AddText("[");
            }

            if (current.RefKind == RefKind.Ref)
            {
                AddText("ref ");
            }
            else if (current.RefKind == RefKind.Out)
            {
                AddText("out ");
            }

            if (current.IsParams)
            {
                AddText("params ");
            }

            await AddTypeLinkAsync(current.Type, LinkFlags.None, cancellationToken).ConfigureAwait(true);
            AddText(" ");
            AddParam(current.Name);

            if (current.HasExplicitDefaultValue)
            {
                AddText(" = ");
                if (current.ExplicitDefaultValue == null)
                {
                    AddText("null");
                }
                else
                {
                    AddText(current.ExplicitDefaultValue.ToString());
                }
            }

            if (current.IsOptional)
            {
                AddText("]");
            }
        }
    }

    protected override async Task BuildFieldDeclarationAsync(
        IFieldSymbol fieldSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        BuildMemberModifiers(fieldSymbol);

        if (fieldSymbol.ContainingType.TypeKind != TypeKind.Enum)
        {
            await AddTypeLinkAsync(fieldSymbol.Type, LinkFlags.None, cancellationToken).ConfigureAwait(true);
            AddText(" ");
        }

        AddName(fieldSymbol.Name);
    }

    protected override async Task BuildPropertyDeclarationAsync(
        IPropertySymbol propertySymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        BuildMemberModifiers(propertySymbol);

        await AddTypeLinkAsync(propertySymbol.Type, LinkFlags.None, cancellationToken).ConfigureAwait(true);
        AddText(" ");

        if (propertySymbol.IsIndexer)
        {
            AddName("this");
            AddText("[");
            await BuildParameterListAsync(propertySymbol.Parameters, cancellationToken).ConfigureAwait(true);
            AddText("]");
        }
        else
        {
            AddName(propertySymbol.Name);
        }

        AddText(" { ");

        if (propertySymbol.GetMethod != null)
        {
            if (propertySymbol.GetMethod.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
            {
                BuildAccessibility(propertySymbol.GetMethod);
            }

            AddText("get; ");
        }

        if (propertySymbol.SetMethod != null)
        {
            if (propertySymbol.SetMethod.DeclaredAccessibility != propertySymbol.DeclaredAccessibility)
            {
                BuildAccessibility(propertySymbol.SetMethod);
            }

            AddText("set; ");
        }

        AddText("}");
    }

    protected override async Task BuildEventDeclarationAsync(
        IEventSymbol eventSymbol, _VSOBJDESCOPTIONS options, CancellationToken cancellationToken)
    {
        BuildMemberModifiers(eventSymbol);

        AddText("event ");

        await AddTypeLinkAsync(eventSymbol.Type, LinkFlags.None, cancellationToken).ConfigureAwait(true);
        AddText(" ");

        AddName(eventSymbol.Name);
    }
}
