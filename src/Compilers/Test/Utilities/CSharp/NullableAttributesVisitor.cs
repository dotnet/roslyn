// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    /// <summary>
    /// Returns a string with all symbols containing nullable attributes.
    /// </summary>
    internal sealed class NullableAttributesVisitor : CSharpSymbolVisitor
    {
        internal static string GetString(Symbol symbol)
        {
            var builder = new StringBuilder();
            var visitor = new NullableAttributesVisitor(builder);
            visitor.Visit(symbol);
            return builder.ToString();
        }

        private readonly StringBuilder _builder;
        private readonly HashSet<Symbol> _reported;

        private NullableAttributesVisitor(StringBuilder builder)
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
            VisitList(type.GetMembers());
        }

        public override void VisitMethod(MethodSymbol method)
        {
            // Skip accessors since those are covered by associated symbol.
            if (method.IsAccessor()) return;

            ReportSymbol(method);
            VisitList(method.TypeParameters);
            VisitList(method.Parameters);
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

        private static string GetIndentString(Symbol symbol)
        {
            int level = 0;
            while (true)
            {
                symbol = symbol.ContainingSymbol;
                if (symbol.Kind == SymbolKind.Namespace)
                {
                    break;
                }
                level++;
            }
            return new string(' ', level * 4);
        }

        private static readonly SymbolDisplayFormat _displayFormat =
            new SymbolDisplayFormat(
                globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.OmittedAsContaining,
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
                propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
                genericsOptions:
                    SymbolDisplayGenericsOptions.IncludeTypeParameters |
                    SymbolDisplayGenericsOptions.IncludeVariance |
                    SymbolDisplayGenericsOptions.IncludeTypeConstraints,
                memberOptions:
                    SymbolDisplayMemberOptions.IncludeParameters |
                    SymbolDisplayMemberOptions.IncludeType |
                    SymbolDisplayMemberOptions.IncludeRef |
                    SymbolDisplayMemberOptions.IncludeExplicitInterface,
                parameterOptions:
                    SymbolDisplayParameterOptions.IncludeOptionalBrackets |
                    SymbolDisplayParameterOptions.IncludeDefaultValue |
                    SymbolDisplayParameterOptions.IncludeParamsRefOut |
                    SymbolDisplayParameterOptions.IncludeExtensionThis |
                    SymbolDisplayParameterOptions.IncludeType |
                    SymbolDisplayParameterOptions.IncludeName,
                miscellaneousOptions:
                    SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers |
                    SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
                    SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier,
                compilerInternalOptions:
                    SymbolDisplayCompilerInternalOptions.UseMetadataMethodNames |
                    SymbolDisplayCompilerInternalOptions.IncludeNonNullableTypeModifier);

        private void ReportContainingSymbols(Symbol symbol)
        {
            if (symbol.Kind == SymbolKind.Namespace)
            {
                return;
            }
            if (_reported.Contains(symbol))
            {
                return;
            }
            ReportContainingSymbols(symbol.ContainingSymbol);
            ReportSymbol(symbol, includeAlways: true);
        }

        private void ReportSymbol(Symbol symbol, bool includeAlways = false)
        {
            var attributes = (symbol.Kind == SymbolKind.Method) ? ((MethodSymbol)symbol).GetReturnTypeAttributes() : symbol.GetAttributes();
            var nullableAttribute = GetAttribute(attributes, "System.Runtime.CompilerServices", "NullableAttribute");
            if (!includeAlways && nullableAttribute == null)
            {
                return;
            }
            ReportContainingSymbols(symbol.ContainingSymbol);
            _builder.Append(GetIndentString(symbol));
            if (nullableAttribute != null)
            {
                _builder.Append($"{ReportAttribute(nullableAttribute)} ");
            }
            _builder.AppendLine(symbol.ToDisplayString(_displayFormat));
            _reported.Add(symbol);
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

        private static CSharpAttributeData GetAttribute(ImmutableArray<CSharpAttributeData> attributes, string namespaceName, string name)
        {
            foreach (var attribute in attributes)
            {
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
