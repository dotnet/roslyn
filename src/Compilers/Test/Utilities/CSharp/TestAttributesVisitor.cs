// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    internal abstract class TestAttributesVisitor : CSharpSymbolVisitor
    {
        protected readonly StringBuilder _builder;
        protected readonly HashSet<Symbol> _reported;

        protected TestAttributesVisitor(StringBuilder builder)
        {
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
            ReportSymbol(type);
            VisitList(type.TypeParameters);

            foreach (var member in type.GetMembers())
            {
                // Skip accessors since those are covered by associated symbol.
                if (member.IsAccessor()) continue;
                Visit(member);
            }
        }

        public override void VisitMethod(MethodSymbol method)
        {
            ReportSymbol(method);
            VisitList(method.TypeParameters);
            VisitList(method.Parameters);
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
        private static Symbol? GetContainingSymbol(Symbol symbol)
        {
            if (symbol.IsAccessor())
            {
                return ((MethodSymbol)symbol).AssociatedSymbol;
            }
            var containingSymbol = symbol.ContainingSymbol;
            return containingSymbol?.Kind == SymbolKind.Namespace ? null : containingSymbol;
        }

        protected static string GetIndentString(Symbol symbol)
        {
            int level = 0;
            var current = symbol;
            while (true)
            {
                current = GetContainingSymbol(current);
                if (current is null)
                {
                    break;
                }
                level++;
            }
            return new string(' ', level * 4);
        }

        protected abstract SymbolDisplayFormat DisplayFormat { get; }

        protected void ReportContainingSymbols(Symbol symbol)
        {
            var s = GetContainingSymbol(symbol);
            if (s is null)
            {
                return;
            }
            if (_reported.Contains(s))
            {
                return;
            }
            ReportContainingSymbols(s);
            _builder.Append(GetIndentString(s));
            _builder.AppendLine(s.ToDisplayString(DisplayFormat));
            _reported.Add(s);
        }

        protected virtual void ReportSymbol(Symbol symbol)
        {
            var type = (symbol as TypeSymbol) ?? symbol.GetTypeOrReturnType().Type;
            var attribute = GetTargetAttribute((symbol is MethodSymbol method) ? method.GetReturnTypeAttributes() : symbol.GetAttributes());
            Debug.Assert((!TypeRequiresAttribute(type)) || (attribute != null));
            if (attribute == null)
            {
                return;
            }
            ReportContainingSymbols(symbol);
            _builder.Append(GetIndentString(symbol));
            _builder.Append($"{ReportAttribute(attribute)} ");
            _builder.AppendLine(symbol.ToDisplayString(DisplayFormat));
            _reported.Add(symbol);
        }

        protected static string ReportAttribute(CSharpAttributeData attribute)
        {
            var builder = new StringBuilder();
            builder.Append('[');

            Assert.NotNull(attribute.AttributeClass);
            var name = attribute.AttributeClass.Name;
            if (name.EndsWith("Attribute")) name = name.Substring(0, name.Length - 9);
            builder.Append(name);

            var arguments = attribute.ConstructorArguments.ToImmutableArray();
            if (arguments.Length > 0)
            {
                builder.Append('(');
                printValues(builder, arguments);
                builder.Append(')');
            }

            builder.Append(']');
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

        protected abstract bool TypeRequiresAttribute(TypeSymbol? type);

        protected abstract CSharpAttributeData? GetTargetAttribute(ImmutableArray<CSharpAttributeData> attributes);

        protected static CSharpAttributeData? GetAttribute(ImmutableArray<CSharpAttributeData> attributes, string namespaceName, string name)
        {
            foreach (var attribute in attributes)
            {
                Assert.NotNull(attribute.AttributeConstructor);
                var containingType = attribute.AttributeConstructor.ContainingType;
                if (containingType.Name == name && containingType.ContainingNamespace.QualifiedName == namespaceName)
                {
                    return attribute;
                }
            }
            return null;
        }
    }
}
