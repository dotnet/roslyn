// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Returns a string with all symbols containing nullable attributes.
    /// </summary>
    internal sealed class NullableAttributesVisitor : CSharpSymbolVisitor
    {
        internal static string GetString(PEModuleSymbol module)
        {
            var builder = new StringBuilder();
            var visitor = new NullableAttributesVisitor(module, builder);
            visitor.Visit(module);
            return builder.ToString();
        }

        private readonly PEModuleSymbol _module;
        private readonly StringBuilder _builder;
        private readonly HashSet<Symbol> _reported;
        private CSharpAttributeData _nullableContext;

        private NullableAttributesVisitor(PEModuleSymbol module, StringBuilder builder)
        {
            _module = module;
            _builder = builder;
            _reported = new HashSet<Symbol>();
        }

        public override void DefaultVisit(Symbol symbol)
        {
            ReportSymbol(symbol);
        }

        public override void VisitModule(ModuleSymbol module)
        {
            Visit(module.GlobalNamespace);
        }

        public override void VisitNamespace(NamespaceSymbol @namespace)
        {
            VisitList(@namespace.GetMembers());
        }

        public override void VisitNamedType(NamedTypeSymbol type)
        {
            var previousContext = _nullableContext;
            _nullableContext = GetNullableContextAttribute(type.GetAttributes()) ?? _nullableContext;

            ReportSymbol(type);
            VisitList(type.TypeParameters);

            foreach (var member in type.GetMembers())
            {
                // Skip accessors since those are covered by associated symbol.
                if (member.IsAccessor()) continue;
                Visit(member);
            }

            _nullableContext = previousContext;
        }

        public override void VisitMethod(MethodSymbol method)
        {
            var previousContext = _nullableContext;
            _nullableContext = GetNullableContextAttribute(method.GetAttributes()) ?? _nullableContext;

            ReportSymbol(method);
            VisitList(method.TypeParameters);
            VisitList(method.Parameters);

            _nullableContext = previousContext;
        }

        public override void VisitEvent(EventSymbol @event)
        {
            ReportSymbol(@event);
            Visit(@event.AddMethod);
            Visit(@event.RemoveMethod);
        }

        public override void VisitProperty(PropertySymbol property)
        {
            ReportSymbol(property);
            VisitList(property.Parameters);
            Visit(property.GetMethod);
            Visit(property.SetMethod);
        }

        public override void VisitTypeParameter(TypeParameterSymbol typeParameter)
        {
            ReportSymbol(typeParameter);
        }

        private void VisitList<TSymbol>(ImmutableArray<TSymbol> symbols) where TSymbol : Symbol
        {
            foreach (var symbol in symbols)
            {
                Visit(symbol);
            }
        }

        /// <summary>
        /// Return the containing symbol used in the hierarchy here. Specifically, the
        /// hierarchy contains types, members, and parameters only, and accessors are
        /// considered members of the associated symbol rather than the type.
        /// </summary>
        private static Symbol GetContainingSymbol(Symbol symbol)
        {
            if (symbol.IsAccessor())
            {
                return ((MethodSymbol)symbol).AssociatedSymbol;
            }
            var containingSymbol = symbol.ContainingSymbol;
            return containingSymbol?.Kind == SymbolKind.Namespace ? null : containingSymbol;
        }

        private static string GetIndentString(Symbol symbol)
        {
            int level = 0;
            while (true)
            {
                symbol = GetContainingSymbol(symbol);
                if (symbol is null)
                {
                    break;
                }
                level++;
            }
            return new string(' ', level * 4);
        }

        private static readonly SymbolDisplayFormat _displayFormat = SymbolDisplayFormat.TestFormatWithConstraints.
            WithMemberOptions(
                SymbolDisplayMemberOptions.IncludeParameters |
                SymbolDisplayMemberOptions.IncludeType |
                SymbolDisplayMemberOptions.IncludeRef |
                SymbolDisplayMemberOptions.IncludeExplicitInterface);

        private void ReportContainingSymbols(Symbol symbol)
        {
            symbol = GetContainingSymbol(symbol);
            if (symbol is null)
            {
                return;
            }
            if (_reported.Contains(symbol))
            {
                return;
            }
            ReportContainingSymbols(symbol);
            _builder.Append(GetIndentString(symbol));
            _builder.AppendLine(symbol.ToDisplayString(_displayFormat));
            _reported.Add(symbol);
        }

        private void ReportSymbol(Symbol symbol)
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

            _builder.AppendLine(symbol.ToDisplayString(_displayFormat));
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

        private static string ReportAttribute(CSharpAttributeData attribute)
        {
            var builder = new StringBuilder();
            builder.Append("[");

            var name = attribute.AttributeClass.Name;
            if (name.EndsWith("Attribute")) name = name.Substring(0, name.Length - 9);
            builder.Append(name);

            var arguments = attribute.ConstructorArguments.ToImmutableArray();
            if (arguments.Length > 0)
            {
                builder.Append("(");
                printValues(builder, arguments);
                builder.Append(")");
            }

            builder.Append("]");
            return builder.ToString();

            static void printValues(StringBuilder builder, ImmutableArray<TypedConstant> values)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(", ");
                    }
                    printValue(builder, values[i]);
                }
            }

            static void printValue(StringBuilder builder, TypedConstant value)
            {
                if (value.Kind == TypedConstantKind.Array)
                {
                    builder.Append("{ ");
                    printValues(builder, value.Values);
                    builder.Append(" }");
                }
                else
                {
                    builder.Append(value.Value);
                }
            }
        }

        private static CSharpAttributeData GetNullableContextAttribute(ImmutableArray<CSharpAttributeData> attributes) =>
            GetAttribute(attributes, "System.Runtime.CompilerServices", "NullableContextAttribute");

        private static CSharpAttributeData GetNullableAttribute(ImmutableArray<CSharpAttributeData> attributes) =>
            GetAttribute(attributes, "System.Runtime.CompilerServices", "NullableAttribute");

        private static CSharpAttributeData GetAttribute(ImmutableArray<CSharpAttributeData> attributes, string namespaceName, string name)
        {
            foreach (var attribute in attributes)
            {
                var containingType = attribute.AttributeConstructor.ContainingType;
                if (containingType is { Name: name, ContainingNamespace: { QualifiedName: namespaceName } })
                {
                    return attribute;
                }
            }
            return null;
        }
    }
}
