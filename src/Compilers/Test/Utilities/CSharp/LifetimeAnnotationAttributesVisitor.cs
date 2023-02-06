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
    internal sealed class ScopedRefAttributesVisitor : CSharpSymbolVisitor
    {
        internal static string GetString(PEModuleSymbol module)
        {
            var builder = new StringBuilder();
            var visitor = new ScopedRefAttributesVisitor(builder);
            visitor.Visit(module);
            return builder.ToString();
        }

        private readonly StringBuilder _builder;

        private ScopedRefAttributesVisitor(StringBuilder builder)
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
            if (!parameters.Any(p => TryGetScopedRefAttribute((PEParameterSymbol)p)))
            {
                return;
            }
            _builder.AppendLine(method.ToTestDisplayString());
            foreach (var parameter in parameters)
            {
                _builder.Append("    ");
                if (TryGetScopedRefAttribute((PEParameterSymbol)parameter))
                {
                    _builder.Append($"[ScopedRef] ");
                }
                _builder.AppendLine(parameter.ToTestDisplayString());
            }
        }

        private bool TryGetScopedRefAttribute(PEParameterSymbol parameter)
        {
            var module = ((PEModuleSymbol)parameter.ContainingModule).Module;
            return module.HasScopedRefAttribute(parameter.Handle);
        }
    }
}
