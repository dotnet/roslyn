// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationCommentFormatting;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Shared.Extensions
{
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
                    return ((IArrayTypeSymbol)symbol).ElementType.GetGlyph();

                case SymbolKind.DynamicType:
                    return Glyph.ClassPublic;

                case SymbolKind.Event:
                    publicIcon = Glyph.EventPublic;
                    break;

                case SymbolKind.Field:
                    var containingType = symbol.ContainingType;
                    if (containingType != null && containingType.TypeKind == TypeKind.Enum)
                    {
                        return Glyph.EnumMember;
                    }

                    publicIcon = ((IFieldSymbol)symbol).IsConst ? Glyph.ConstantPublic : Glyph.FieldPublic;
                    break;

                case SymbolKind.Label:
                    return Glyph.Label;

                case SymbolKind.Local:
                    return Glyph.Local;

                case SymbolKind.NamedType:
                case SymbolKind.ErrorType:
                    {
                        switch (((INamedTypeSymbol)symbol).TypeKind)
                        {
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
                                throw new ArgumentException(FeaturesResources.TheSymbolDoesNotHaveAnIcon, "symbol");
                        }

                        break;
                    }

                case SymbolKind.Method:
                    {
                        var methodSymbol = (IMethodSymbol)symbol;

                        if (methodSymbol.MethodKind == MethodKind.UserDefinedOperator || methodSymbol.MethodKind == MethodKind.Conversion)
                        {
                            return Glyph.Operator;
                        }
                        else if (methodSymbol.IsExtensionMethod || methodSymbol.MethodKind == MethodKind.ReducedExtension)
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
                    return symbol.IsValueParameter()
                        ? Glyph.Keyword
                        : Glyph.Parameter;

                case SymbolKind.PointerType:
                    return ((IPointerTypeSymbol)symbol).PointedAtType.GetGlyph();

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

                case SymbolKind.RangeVariable:
                    return Glyph.RangeVariable;

                case SymbolKind.TypeParameter:
                    return Glyph.TypeParameter;

                default:
                    throw new ArgumentException(FeaturesResources.TheSymbolDoesNotHaveAnIcon, "symbol");
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

        public static IEnumerable<SymbolDisplayPart> GetDocumentationParts(this ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter, CancellationToken cancellationToken)
        {
            var documentation = symbol.TypeSwitch(
                    (IParameterSymbol parameter) => parameter.ContainingSymbol.OriginalDefinition.GetDocumentationComment(cancellationToken: cancellationToken).GetParameterText(symbol.Name),
                    (ITypeParameterSymbol typeParam) => typeParam.ContainingSymbol.GetDocumentationComment(cancellationToken: cancellationToken).GetTypeParameterText(symbol.Name),
                    (IMethodSymbol method) => GetMethodDocumentation(method),
                    (IAliasSymbol alias) => alias.Target.GetDocumentationComment(cancellationToken: cancellationToken).SummaryText,
                    _ => symbol.GetDocumentationComment(cancellationToken: cancellationToken).SummaryText);

            return documentation != null
                ? formatter.Format(documentation, semanticModel, position, CrefFormat)
                : SpecializedCollections.EmptyEnumerable<SymbolDisplayPart>();
        }

        public static Func<CancellationToken, IEnumerable<SymbolDisplayPart>> GetDocumentationPartsFactory(this ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter)
        {
            return c => symbol.GetDocumentationParts(semanticModel, position, formatter, cancellationToken: c);
        }

        public static readonly SymbolDisplayFormat CrefFormat =
            new SymbolDisplayFormat(
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

        private static string GetMethodDocumentation(IMethodSymbol method)
        {
            switch (method.MethodKind)
            {
                case MethodKind.EventAdd:
                case MethodKind.EventRaise:
                case MethodKind.EventRemove:
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    return method.ContainingSymbol.GetDocumentationComment().SummaryText;
                default:
                    return method.GetDocumentationComment().SummaryText;
            }
        }
    }
}
