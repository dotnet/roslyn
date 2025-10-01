// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;
using Microsoft.CodeAnalysis.Shared.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions;

internal static partial class ISymbolExtensions2
{
    public static Glyph GetGlyph(this ISymbol symbol)
    {
        Glyph publicIcon;

        switch (symbol.Kind)
        {
            case SymbolKind.Alias:
                return ((IAliasSymbol)symbol).Target.GetGlyph();

            case SymbolKind.Assembly:
                return Glyph.Assembly;

            case SymbolKind.ArrayType:
                return Glyph.ClassPublic;

            case SymbolKind.DynamicType:
                return Glyph.ClassPublic;

            case SymbolKind.Event:
                publicIcon = Glyph.EventPublic;
                break;

            case SymbolKind.Field:
                var containingType = symbol.ContainingType;
                if (containingType != null && containingType.TypeKind == TypeKind.Enum)
                {
                    return Glyph.EnumMemberPublic;
                }

                publicIcon = ((IFieldSymbol)symbol).IsConst ? Glyph.ConstantPublic : Glyph.FieldPublic;
                break;

            case SymbolKind.Label:
                return Glyph.Label;

            case SymbolKind.Local:
            case SymbolKind.Discard:
                return Glyph.Local;

            case SymbolKind.NamedType:
            case SymbolKind.ErrorType:
                {
                    switch (((INamedTypeSymbol)symbol).TypeKind)
                    {
                        case TypeKind.Extension:
                        case TypeKind.Class:
                            publicIcon = Glyph.ClassPublic;
                            break;

                        case TypeKind.Delegate:
                            publicIcon = Glyph.DelegatePublic;
                            break;

                        case TypeKind.Enum:
                            publicIcon = Glyph.EnumPublic;
                            break;

                        case TypeKind.Interface:
                            publicIcon = Glyph.InterfacePublic;
                            break;

                        case TypeKind.Module:
                            publicIcon = Glyph.ModulePublic;
                            break;

                        case TypeKind.Struct:
                            publicIcon = Glyph.StructurePublic;
                            break;

                        case TypeKind.Error:
                            return Glyph.Error;

                        default:
                            throw new ArgumentException(FeaturesResources.The_symbol_does_not_have_an_icon, nameof(symbol));
                    }

                    break;
                }

            case SymbolKind.Method:
                {
                    var methodSymbol = (IMethodSymbol)symbol;

                    if (methodSymbol.MethodKind is MethodKind.UserDefinedOperator or MethodKind.Conversion or MethodKind.BuiltinOperator)
                    {
                        publicIcon = Glyph.OperatorPublic;
                    }
                    else if (methodSymbol.IsExtensionMethod ||
                             methodSymbol.MethodKind == MethodKind.ReducedExtension ||
                             methodSymbol.ContainingType?.IsExtension is true)
                    {
                        publicIcon = Glyph.ExtensionMethodPublic;
                    }
                    else
                    {
                        publicIcon = Glyph.MethodPublic;
                    }
                }

                break;

            case SymbolKind.Namespace:
                return Glyph.Namespace;

            case SymbolKind.NetModule:
                return Glyph.Assembly;

            case SymbolKind.Parameter:
                return symbol.IsImplicitValueParameter()
                    ? Glyph.Keyword
                    : Glyph.Parameter;

            case SymbolKind.PointerType:
                return ((IPointerTypeSymbol)symbol).PointedAtType.GetGlyph();

            case SymbolKind.FunctionPointerType:
                return Glyph.Intrinsic;

            case SymbolKind.Property:
                {
                    var propertySymbol = (IPropertySymbol)symbol;

                    if (propertySymbol.IsWithEvents)
                    {
                        publicIcon = Glyph.FieldPublic;
                    }
                    else
                    {
                        publicIcon = Glyph.PropertyPublic;
                    }
                }

                break;

            case SymbolKind.Preprocessing:
                return Glyph.Keyword;

            case SymbolKind.RangeVariable:
                return Glyph.RangeVariable;

            case SymbolKind.TypeParameter:
                return Glyph.TypeParameter;

            default:
                throw new ArgumentException(FeaturesResources.The_symbol_does_not_have_an_icon, nameof(symbol));
        }

        switch (symbol.DeclaredAccessibility)
        {
            case Accessibility.Private:
                publicIcon += Glyph.ClassPrivate - Glyph.ClassPublic;
                break;

            case Accessibility.Protected:
            case Accessibility.ProtectedAndInternal:
            case Accessibility.ProtectedOrInternal:
                publicIcon += Glyph.ClassProtected - Glyph.ClassPublic;
                break;

            case Accessibility.Internal:
                publicIcon += Glyph.ClassInternal - Glyph.ClassPublic;
                break;
        }

        return publicIcon;
    }

    public static ImmutableArray<TaggedText> GetDocumentationParts(this ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter, CancellationToken cancellationToken)
        => formatter.Format(GetAppropriateDocumentationComment(symbol, semanticModel.Compilation, cancellationToken).SummaryText,
            symbol, semanticModel, position, CrefFormat, cancellationToken);

    /// <summary>
    /// Returns the <see cref="DocumentationComment"/> for a symbol, even if it involves going to other symbols to find it.
    /// </summary>
    public static DocumentationComment GetAppropriateDocumentationComment(this ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
    {
        symbol = symbol.OriginalDefinition;

        return symbol switch
        {
            IParameterSymbol parameter => GetParameterDocumentation(parameter, compilation, cancellationToken) ?? DocumentationComment.Empty,
            ITypeParameterSymbol typeParam => typeParam.ContainingSymbol.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken)?.GetTypeParameter(typeParam.Name) ?? DocumentationComment.Empty,
            IMethodSymbol method => GetMethodDocumentation(method, compilation, cancellationToken),
            IAliasSymbol alias => alias.Target.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken),
            _ => symbol.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken),
        };
    }

    private static DocumentationComment? GetParameterDocumentation(IParameterSymbol parameter, Compilation compilation, CancellationToken cancellationToken)
    {
        var containingSymbol = parameter.ContainingSymbol;
        if (containingSymbol.ContainingSymbol.IsDelegateType() && containingSymbol is IMethodSymbol methodSymbol)
        {
            // There are two ways to invoke a delegate that we care about here: the Invoke()/BeginInvoke() methods. (Direct invocation is equivalent to an Invoke() call.)
            // DynamicInvoke() takes an object array, and EndInvoke() takes a System.IAsyncResult, so we can (and should) ignore those here.

            var symbolName = methodSymbol.Name;
            if (symbolName == WellKnownMemberNames.DelegateBeginInvokeName && parameter.Ordinal >= (methodSymbol.Parameters.Length - 2))
            {
                // Return null (similar to DocumentationComment.GetParameterText()) for the last two implicit parameters (usually called "callback" and "@object").
                // We can't rely on those names because they might be renamed to avoid collision with a user-defined delegate parameter of the same name,
                // and we have to treat them separately, because a user might add e.g. a '<param name="callback">' tag to the delegate, which would be displayed in Signature Help for that implicit parameter.
                return null;
            }

            if (symbolName is WellKnownMemberNames.DelegateInvokeName or WellKnownMemberNames.DelegateBeginInvokeName)
            {
                // We know that containingSymbol is the [Begin]Invoke() method of a delegate type, so we need to go up a level and take the method's containing symbol (i.e. the delegate), which contains the documentation.
                containingSymbol = containingSymbol.ContainingSymbol;
            }
        }

        // Get the comments from the original definition of the containing symbol.
        return containingSymbol.OriginalDefinition.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken)?.GetParameter(parameter.Name);
    }

    public static Func<CancellationToken, IEnumerable<TaggedText>> GetDocumentationPartsFactory(
        this ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter)
        => cancellationToken => symbol.GetDocumentationParts(semanticModel, position, formatter, cancellationToken);

    public static readonly SymbolDisplayFormat CrefFormat =
        new(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            propertyStyle: SymbolDisplayPropertyStyle.NameOnly,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters | SymbolDisplayGenericsOptions.IncludeVariance,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeContainingType,
            parameterOptions:
                SymbolDisplayParameterOptions.IncludeParamsRefOut |
                SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions:
                SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

    private static DocumentationComment GetMethodDocumentation(this IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken)
    {
        switch (method.MethodKind)
        {
            case MethodKind.EventAdd:
            case MethodKind.EventRaise:
            case MethodKind.EventRemove:
            case MethodKind.PropertyGet:
            case MethodKind.PropertySet:
                return method.AssociatedSymbol?.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken) ?? DocumentationComment.Empty;
            default:
                return method.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken);
        }
    }
}
