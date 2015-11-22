// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DocumentationCommentIDVisitor
    {
        /// <summary>
        /// A visitor that generates the part of the documentation comment after the initial type
        /// and colon.
        /// </summary>
        private sealed class PartVisitor : CSharpSymbolVisitor<StringBuilder, object>
        {
            // Everyone outside this type uses this one.
            internal static readonly PartVisitor Instance = new PartVisitor(inParameterOrReturnType: false);

            // Select callers within this type use this one.
            private static readonly PartVisitor s_parameterOrReturnTypeInstance = new PartVisitor(inParameterOrReturnType: true);

            private readonly bool _inParameterOrReturnType;

            private PartVisitor(bool inParameterOrReturnType)
            {
                _inParameterOrReturnType = inParameterOrReturnType;
            }

            public override object VisitArrayType(ArrayTypeSymbol symbol, StringBuilder builder)
            {
                Visit(symbol.ElementType.TypeSymbol, builder);

                // Rank-one arrays are displayed different than rectangular arrays
                if (symbol.IsSZArray)
                {
                    builder.Append("[]");
                }
                else
                {
                    builder.Append("[0:");

                    for (int i = 0; i < symbol.Rank - 1; i++)
                    {
                        builder.Append(",0:");
                    }

                    builder.Append(']');
                }

                return null;
            }

            public override object VisitField(FieldSymbol symbol, StringBuilder builder)
            {
                Visit(symbol.ContainingType, builder);
                builder.Append('.');
                builder.Append(symbol.Name);

                return null;
            }

            private void VisitParameters(ImmutableArray<ParameterSymbol> parameters, bool isVararg, StringBuilder builder)
            {
                builder.Append('(');
                bool needsComma = false;

                foreach (var parameter in parameters)
                {
                    if (needsComma)
                    {
                        builder.Append(',');
                    }

                    Visit(parameter, builder);
                    needsComma = true;
                }

                if (isVararg && needsComma)
                {
                    builder.Append(',');
                }

                builder.Append(')');
            }

            public override object VisitMethod(MethodSymbol symbol, StringBuilder builder)
            {
                Visit(symbol.ContainingType, builder);
                builder.Append('.');
                builder.Append(GetEscapedMetadataName(symbol));

                if (symbol.Arity != 0)
                {
                    builder.Append("``");
                    builder.Append(symbol.Arity);
                }

                if (symbol.Parameters.Any() || symbol.IsVararg)
                {
                    s_parameterOrReturnTypeInstance.VisitParameters(symbol.Parameters, symbol.IsVararg, builder);
                }

                if (symbol.MethodKind == MethodKind.Conversion)
                {
                    builder.Append('~');
                    s_parameterOrReturnTypeInstance.Visit(symbol.ReturnType.TypeSymbol, builder);
                }

                return null;
            }

            public override object VisitProperty(PropertySymbol symbol, StringBuilder builder)
            {
                Visit(symbol.ContainingType, builder);
                builder.Append('.');
                builder.Append(GetEscapedMetadataName(symbol));

                if (symbol.Parameters.Any())
                {
                    s_parameterOrReturnTypeInstance.VisitParameters(symbol.Parameters, false, builder);
                }

                return null;
            }

            public override object VisitEvent(EventSymbol symbol, StringBuilder builder)
            {
                Visit(symbol.ContainingType, builder);
                builder.Append('.');
                builder.Append(GetEscapedMetadataName(symbol));

                return null;
            }

            public override object VisitTypeParameter(TypeParameterSymbol symbol, StringBuilder builder)
            {
                int ordinalOffset = 0;

                // Is this a type parameter on a type?
                Symbol containingSymbol = symbol.ContainingSymbol;
                if (containingSymbol.Kind == SymbolKind.NamedType)
                {
                    // If the containing type is nested within other types, then we need to add their arities.
                    // e.g. A<T>.B<U>.M<V>(T t, U u, V v) should be M(`0, `1, ``0).
                    for (NamedTypeSymbol curr = containingSymbol.ContainingType; (object)curr != null; curr = curr.ContainingType)
                    {
                        ordinalOffset += curr.Arity;
                    }
                    builder.Append('`');
                }
                else if (containingSymbol.Kind == SymbolKind.Method)
                {
                    builder.Append("``");
                }
                else
                {
                    throw ExceptionUtilities.UnexpectedValue(containingSymbol.Kind);
                }

                builder.Append(symbol.Ordinal + ordinalOffset);

                return null;
            }

            public override object VisitNamedType(NamedTypeSymbol symbol, StringBuilder builder)
            {
                if ((object)symbol.ContainingSymbol != null && symbol.ContainingSymbol.Name.Length != 0)
                {
                    Visit(symbol.ContainingSymbol, builder);
                    builder.Append('.');
                }

                builder.Append(symbol.Name);

                if (symbol.Arity != 0)
                {
                    // Special case: dev11 treats types instances of the declaring type in the parameter list
                    // (and return type, for conversions) as constructed with its own type parameters.
                    if (!_inParameterOrReturnType && symbol == symbol.ConstructedFrom)
                    {
                        builder.Append('`');
                        builder.Append(symbol.Arity);
                    }
                    else
                    {
                        builder.Append('{');

                        bool needsComma = false;

                        foreach (var typeArgument in symbol.TypeArgumentsNoUseSiteDiagnostics)
                        {
                            if (needsComma)
                            {
                                builder.Append(',');
                            }

                            Visit(typeArgument.TypeSymbol, builder);

                            needsComma = true;
                        }

                        builder.Append('}');
                    }
                }

                return null;
            }

            public override object VisitPointerType(PointerTypeSymbol symbol, StringBuilder builder)
            {
                Visit(symbol.PointedAtType.TypeSymbol, builder);
                builder.Append('*');

                return null;
            }

            public override object VisitNamespace(NamespaceSymbol symbol, StringBuilder builder)
            {
                if ((object)symbol.ContainingNamespace != null && symbol.ContainingNamespace.Name.Length != 0)
                {
                    Visit(symbol.ContainingNamespace, builder);
                    builder.Append('.');
                }

                builder.Append(symbol.Name);

                return null;
            }

            public override object VisitParameter(ParameterSymbol symbol, StringBuilder builder)
            {
                Debug.Assert(_inParameterOrReturnType);

                Visit(symbol.Type.TypeSymbol, builder);

                // ref and out params are suffixed with @
                if (symbol.RefKind != RefKind.None)
                {
                    builder.Append('@');
                }

                return null;
            }

            public override object VisitErrorType(ErrorTypeSymbol symbol, StringBuilder builder)
            {
                return VisitNamedType(symbol, builder);
            }

            public override object VisitDynamicType(DynamicTypeSymbol symbol, StringBuilder builder)
            {
                // NOTE: this is a change from dev11, which did not allow dynamic in parameter types.
                // If we wanted to be really conservative, we would actually visit the symbol for
                // System.Object.  However, the System.Object type must always have exactly this
                // doc comment ID, so the hassle seems unjustifiable.
                builder.Append("System.Object");

                return null;
            }

            private static string GetEscapedMetadataName(Symbol symbol)
            {
                string metadataName = symbol.MetadataName;

                int colonColonIndex = metadataName.IndexOf("::", StringComparison.Ordinal);
                int startIndex = colonColonIndex < 0 ? 0 : colonColonIndex + 2;

                PooledStringBuilder pooled = PooledStringBuilder.GetInstance();
                pooled.Builder.Append(metadataName, startIndex, metadataName.Length - startIndex);
                pooled.Builder.Replace('.', '#').Replace('<', '{').Replace('>', '}');
                return pooled.ToStringAndFree();
            }
        }
    }
}
