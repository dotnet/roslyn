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
            if (!parameters.Any(p => TryGetLifetimeAnnotationAttribute((PEParameterSymbol)p, out _)))
            {
                return;
            }
            _builder.AppendLine(method.ToTestDisplayString());
            foreach (var parameter in parameters)
            {
                _builder.Append("    ");
                if (TryGetLifetimeAnnotationAttribute((PEParameterSymbol)parameter, out var pair))
                {
                    _builder.Append($"[LifetimeAnnotation({pair.IsRefScoped}, {pair.IsValueScoped})] ");
                }
                _builder.AppendLine(parameter.ToTestDisplayString());
            }
        }

        private bool TryGetLifetimeAnnotationAttribute(PEParameterSymbol parameter, out (bool IsRefScoped, bool IsValueScoped) pair)
        {
            var module = ((PEModuleSymbol)parameter.ContainingModule).Module;
            return module.HasLifetimeAnnotationAttribute(parameter.Handle, out pair);
        }
    }
}
