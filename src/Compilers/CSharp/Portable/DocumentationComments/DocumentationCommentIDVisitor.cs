// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class DocumentationCommentIDVisitor : CSharpSymbolVisitor<StringBuilder, object>
    {
        /// <summary>
        /// Generates a documentation comment ID for a single symbol.
        /// </summary>
        public static readonly DocumentationCommentIDVisitor Instance = new DocumentationCommentIDVisitor(overloadedMethods: false);

        /// <summary>
        /// Generates documentation comment IDs for methods which resolve to the overload instead of a single specific
        /// method.
        /// </summary>
        public static readonly DocumentationCommentIDVisitor OverloadInstance = new DocumentationCommentIDVisitor(overloadedMethods: true);

        private readonly bool _overloadedMethods;

        private DocumentationCommentIDVisitor(bool overloadedMethods)
        {
            _overloadedMethods = overloadedMethods;
        }

        public override object DefaultVisit(Symbol symbol, StringBuilder builder)
        {
            // We need to return something to API users, but this should never happen within Roslyn.
            return null;
        }

        public override object VisitNamespace(NamespaceSymbol symbol, StringBuilder builder)
        {
            if (!symbol.IsGlobalNamespace)
            {
                builder.Append("N:");
                PartVisitor.Instance.Visit(symbol, builder);
            }

            return null;
        }

        public override object VisitMethod(MethodSymbol symbol, StringBuilder builder)
        {
            if (_overloadedMethods)
            {
                builder.Append("O:");
                PartVisitor.OverloadInstance.Visit(symbol, builder);
            }
            else
            {
                builder.Append("M:");
                PartVisitor.Instance.Visit(symbol, builder);
            }

            return null;
        }

        public override object VisitField(FieldSymbol symbol, StringBuilder builder)
        {
            builder.Append("F:");
            PartVisitor.Instance.Visit(symbol, builder);

            return null;
        }

        public override object VisitEvent(EventSymbol symbol, StringBuilder builder)
        {
            builder.Append("E:");
            PartVisitor.Instance.Visit(symbol, builder);

            return null;
        }

        public override object VisitProperty(PropertySymbol symbol, StringBuilder builder)
        {
            builder.Append("P:");
            PartVisitor.Instance.Visit(symbol, builder);

            return null;
        }

        public override object VisitNamedType(NamedTypeSymbol symbol, StringBuilder builder)
        {
            builder.Append("T:");
            PartVisitor.Instance.Visit(symbol, builder);

            return null;
        }

        public override object VisitDynamicType(DynamicTypeSymbol symbol, StringBuilder builder)
        {
            // NOTE: Unlike dev11, roslyn allows "dynamic" in parameter types.  However, it still
            // does not allow direct references to "dynamic" (because "dynamic" is only a candidate
            // in type-only contexts).  Therefore, if you ask the dynamic type for its doc comment
            // ID, it should return null.
            return DefaultVisit(symbol, builder);
        }

        public override object VisitErrorType(ErrorTypeSymbol symbol, StringBuilder builder)
        {
            builder.Append("!:");
            PartVisitor.Instance.Visit(symbol, builder);

            return null;
        }

        public override object VisitTypeParameter(TypeParameterSymbol symbol, StringBuilder builder)
        {
            builder.Append("!:");
            builder.Append(symbol.Name);

            return null;
        }
    }
}
