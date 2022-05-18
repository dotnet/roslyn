// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;

namespace Microsoft.CodeAnalysis.CSharp.Test.Utilities
{
    // PROTOTYPE: Share common base class with NativeIntegerAttributesVisitor.
    internal sealed class LifetimeAnnotationAttributesVisitor : CSharpSymbolVisitor
    {
        internal static string GetString(PEModuleSymbol module)
        {
            var builder = new StringBuilder();
            var visitor = new LifetimeAnnotationAttributesVisitor(builder);
            visitor.Visit(module);
            return builder.ToString();
        }

        private readonly StringBuilder _builder;

        private LifetimeAnnotationAttributesVisitor(StringBuilder builder)
        {
            _builder = builder;
        }

        public override void DefaultVisit(Symbol symbol)
        {
        }

        public override void VisitModule(ModuleSymbol module)
        {
            Visit(module.GlobalNamespace);
        }

        public override void VisitNamespace(NamespaceSymbol @namespace)
        {
            foreach (var member in @namespace.GetMembers())
            {
                Visit(member);
            }
        }

        public override void VisitNamedType(NamedTypeSymbol type)
        {
            foreach (var member in type.GetMembers())
            {
                // Skip accessors since those are covered by associated symbol.
                if (member.IsAccessor()) continue;
                Visit(member);
            }
        }

        public override void VisitEvent(EventSymbol @event)
        {
            Visit(@event.AddMethod);
            Visit(@event.RemoveMethod);
        }

        public override void VisitProperty(PropertySymbol property)
        {
            Visit(property.GetMethod);
            Visit(property.SetMethod);
        }

        public override void VisitMethod(MethodSymbol method)
        {
            var parameters = method.Parameters;
            if (!parameters.Any(p => GetLifetimeAnnotationAttribute(p.GetAttributes()) is { }))
            {
                return;
            }
            _builder.AppendLine(method.ToTestDisplayString());
            foreach (var parameter in parameters)
            {
                _builder.Append("    ");
                if (GetLifetimeAnnotationAttribute(parameter.GetAttributes()) is { } attribute)
                {
                    _builder.Append(ReportAttribute(attribute));
                    _builder.Append(" ");
                }
                _builder.AppendLine(parameter.ToTestDisplayString());
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

        private static CSharpAttributeData GetLifetimeAnnotationAttribute(ImmutableArray<CSharpAttributeData> attributes) =>
            GetAttribute(attributes, "System.Runtime.CompilerServices", "LifetimeAnnotationAttribute");

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
