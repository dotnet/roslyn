// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.DocumentationComments;
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
                    if (containingType is { TypeKind: TypeKind.Enum })
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

                        if (methodSymbol.MethodKind == MethodKind.UserDefinedOperator ||
                            methodSymbol.MethodKind == MethodKind.Conversion ||
                            methodSymbol.MethodKind == MethodKind.BuiltinOperator)
                        {
                            return Glyph.Operator;
                        }
                        else if (methodSymbol.IsExtensionMethod ||
                                 methodSymbol.MethodKind == MethodKind.ReducedExtension)
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

        public static IEnumerable<TaggedText> GetDocumentationParts(this ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter, CancellationToken cancellationToken)
        {
            var documentation = GetDocumentation(symbol, semanticModel.Compilation, cancellationToken);

            return documentation != null
                ? formatter.Format(documentation, semanticModel, position, CrefFormat)
                : SpecializedCollections.EmptyEnumerable<TaggedText>();
        }

        private static string GetDocumentation(ISymbol symbol, Compilation compilation, CancellationToken cancellationToken)
            => symbol switch
            {
                IParameterSymbol parameter => GetParameterDocumentation(parameter, compilation, cancellationToken),
                ITypeParameterSymbol typeParam => typeParam.ContainingSymbol.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken).GetTypeParameterText(symbol.Name),
                IMethodSymbol method => GetMethodDocumentation(method, compilation, cancellationToken),
                IAliasSymbol alias => alias.Target.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken).SummaryText,
                _ => symbol.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken).SummaryText,
            };

        private static string GetParameterDocumentation(IParameterSymbol parameter, Compilation compilation, CancellationToken cancellationToken)
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

                if (symbolName == WellKnownMemberNames.DelegateInvokeName || symbolName == WellKnownMemberNames.DelegateBeginInvokeName)
                {
                    // We know that containingSymbol is the [Begin]Invoke() method of a delegate type, so we need to go up a level and take the method's containing symbol (i.e. the delegate), which contains the documentation.
                    containingSymbol = containingSymbol.ContainingSymbol;
                }
            }

            // Get the comments from the original definition of the containing symbol.
            return containingSymbol.OriginalDefinition.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken).GetParameterText(parameter.Name);
        }

        public static Func<CancellationToken, IEnumerable<TaggedText>> GetDocumentationPartsFactory(
            this ISymbol symbol, SemanticModel semanticModel, int position, IDocumentationCommentFormattingService formatter)
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

        private static string GetMethodDocumentation(IMethodSymbol method, Compilation compilation, CancellationToken cancellationToken)
        {
            switch (method.MethodKind)
            {
                case MethodKind.EventAdd:
                case MethodKind.EventRaise:
                case MethodKind.EventRemove:
                case MethodKind.PropertyGet:
                case MethodKind.PropertySet:
                    return method.AssociatedSymbol.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken).SummaryText;
                default:
                    return method.GetDocumentationComment(compilation, expandIncludes: true, expandInheritdoc: true, cancellationToken: cancellationToken).SummaryText;
            }
        }

        public static IList<SymbolDisplayPart> ToAwaitableParts(this ISymbol symbol, string awaitKeyword, string initializedVariableName, SemanticModel semanticModel, int position)
        {
            var spacePart = new SymbolDisplayPart(SymbolDisplayPartKind.Space, null, " ");
            var parts = new List<SymbolDisplayPart>();

            parts.AddLineBreak();
            parts.AddText(WorkspacesResources.Usage_colon);
            parts.AddLineBreak();
            parts.AddText("  ");

            var returnType = symbol.InferAwaitableReturnType(semanticModel, position);
            returnType = returnType != null && returnType.SpecialType != SpecialType.System_Void ? returnType : null;
            if (returnType != null)
            {
                if (semanticModel.Language == "C#")
                {
                    parts.AddRange(returnType.ToMinimalDisplayParts(semanticModel, position));
                    parts.Add(spacePart);
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LocalName, null, initializedVariableName));
                }
                else
                {
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "Dim"));
                    parts.Add(spacePart);
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.LocalName, null, initializedVariableName));
                    parts.Add(spacePart);
                    parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, "as"));
                    parts.Add(spacePart);
                    parts.AddRange(returnType.ToMinimalDisplayParts(semanticModel, position));
                }

                parts.Add(spacePart);
                parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "="));
                parts.Add(spacePart);
            }

            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Keyword, null, awaitKeyword));
            parts.Add(spacePart);
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.MethodName, symbol, symbol.Name));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, "("));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, symbol.GetParameters().Any() ? "..." : ""));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, ")"));
            parts.Add(new SymbolDisplayPart(SymbolDisplayPartKind.Punctuation, null, semanticModel.Language == "C#" ? ";" : ""));

            return parts;
        }

        public static ITypeSymbol InferAwaitableReturnType(this ISymbol symbol, SemanticModel semanticModel, int position)
        {
            if (!(symbol is IMethodSymbol methodSymbol))
            {
                return null;
            }

            var returnType = methodSymbol.ReturnType;
            if (returnType == null)
            {
                return null;
            }

            var potentialGetAwaiters = semanticModel.LookupSymbols(position, container: returnType, name: WellKnownMemberNames.GetAwaiter, includeReducedExtensionMethods: true);
            var getAwaiters = potentialGetAwaiters.OfType<IMethodSymbol>().Where(x => !x.Parameters.Any());
            if (!getAwaiters.Any())
            {
                return null;
            }

            var getResults = getAwaiters.SelectMany(g => semanticModel.LookupSymbols(position, container: g.ReturnType, name: WellKnownMemberNames.GetResult));

            var getResult = getResults.OfType<IMethodSymbol>().FirstOrDefault(g => !g.IsStatic);
            if (getResult == null)
            {
                return null;
            }

            return getResult.ReturnType;
        }
    }
}
