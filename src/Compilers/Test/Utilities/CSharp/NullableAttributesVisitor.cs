// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Returns a string with all symbols containing nullable attributes.
    /// </summary>
    internal sealed class NullableAttributesVisitor : TestAttributesVisitor
    {
        internal static string GetString(PEModuleSymbol module)
        {
            var builder = new StringBuilder();
            var visitor = new NullableAttributesVisitor(module, builder);
            visitor.Visit(module);
            return builder.ToString();
        }

        private readonly PEModuleSymbol _module;
        private CSharpAttributeData? _nullableContext;

        private NullableAttributesVisitor(PEModuleSymbol module, StringBuilder builder) : base(builder)
        {
            _module = module;
        }

        public override void VisitNamedType(NamedTypeSymbol type)
        {
            var previousContext = _nullableContext;
            _nullableContext = GetNullableContextAttribute(type.GetAttributes()) ?? _nullableContext;

            base.VisitNamedType(type);

            _nullableContext = previousContext;
        }

        public override void VisitMethod(MethodSymbol method)
        {
            var previousContext = _nullableContext;
            _nullableContext = GetNullableContextAttribute(method.GetAttributes()) ?? _nullableContext;

            base.VisitMethod(method);

            _nullableContext = previousContext;
        }

        protected override SymbolDisplayFormat DisplayFormat => SymbolDisplayFormat.TestFormatWithConstraints.
            WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeRef |
                SymbolDisplayMemberOptions.IncludeExplicitInterface);

        protected override void ReportSymbol(Symbol symbol)
        {
            var nullableContextAttribute = GetNullableContextAttribute(symbol.GetAttributes());
            var nullableAttribute = GetNullableAttribute((symbol is MethodSymbol method) ? method.GetReturnTypeAttributes() : symbol.GetAttributes());

            if (nullableContextAttribute == null && nullableAttribute == null)
            {
                if (_nullableContext == null)
                {
                    // No explicit attributes on this symbol or above.
                    return;
                }
                // No explicit attributes on this symbol. Check if nullability metadata was dropped.
                if (!_module.ShouldDecodeNullableAttributes(GetAccessSymbol(symbol)))
                {
                    return;
                }
            }

            ReportContainingSymbols(symbol);
            _builder.Append(GetIndentString(symbol));

            if (nullableContextAttribute != null)
            {
                _builder.Append($"{ReportAttribute(nullableContextAttribute)} ");
            }

            if (nullableAttribute != null)
            {
                _builder.Append($"{ReportAttribute(nullableAttribute)} ");
            }

            _builder.AppendLine(symbol.ToDisplayString(DisplayFormat));
            _reported.Add(symbol);
        }

        private static Symbol GetAccessSymbol(Symbol symbol)
        {
            while (true)
            {
                switch (symbol.Kind)
                {
                    case SymbolKind.Parameter:
                    case SymbolKind.TypeParameter:
                        symbol = symbol.ContainingSymbol;
                        break;
                    default:
                        return symbol;
                }
            }
        }

        private static CSharpAttributeData? GetNullableContextAttribute(ImmutableArray<CSharpAttributeData> attributes) =>
            GetAttribute(attributes, "System.Runtime.CompilerServices", "NullableContextAttribute");

        private static CSharpAttributeData? GetNullableAttribute(ImmutableArray<CSharpAttributeData> attributes) =>
            GetAttribute(attributes, "System.Runtime.CompilerServices", "NullableAttribute");

        protected override bool TypeRequiresAttribute(TypeSymbol? type)
        {
            throw ExceptionUtilities.Unreachable();
        }

        protected override CSharpAttributeData GetTargetAttribute(ImmutableArray<CSharpAttributeData> attributes)
        {
            throw ExceptionUtilities.Unreachable();
        }
    }
}
