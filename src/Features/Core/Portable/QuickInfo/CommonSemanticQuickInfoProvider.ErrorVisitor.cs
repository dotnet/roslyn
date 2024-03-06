// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Microsoft.CodeAnalysis.Shared.Extensions;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.QuickInfo;

internal abstract partial class CommonSemanticQuickInfoProvider
{
    private class ErrorVisitor : SymbolVisitor<bool>
    {
        private static readonly ErrorVisitor s_instance = new();

        public static bool ContainsError(ISymbol? symbol)
            => s_instance.Visit(symbol);

        public override bool DefaultVisit(ISymbol symbol)
            => true;

        public override bool VisitAlias(IAliasSymbol symbol)
            => false;

        public override bool VisitArrayType(IArrayTypeSymbol symbol)
            => Visit(symbol.ElementType);

        public override bool VisitEvent(IEventSymbol symbol)
            => Visit(symbol.Type);

        public override bool VisitField(IFieldSymbol symbol)
            => Visit(symbol.Type);

        public override bool VisitLocal(ILocalSymbol symbol)
            => Visit(symbol.Type);

        public override bool VisitMethod(IMethodSymbol symbol)
        {
            foreach (var parameter in symbol.Parameters)
            {
                if (!Visit(parameter))
                {
                    return true;
                }
            }

            foreach (var typeParameter in symbol.TypeParameters)
            {
                if (!Visit(typeParameter))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool VisitNamedType(INamedTypeSymbol symbol)
        {
            foreach (var typeParameter in symbol.TypeArguments.Concat(symbol.TypeParameters))
            {
                if (Visit(typeParameter))
                {
                    return true;
                }
            }

            return symbol.IsErrorType();
        }

        public override bool VisitParameter(IParameterSymbol symbol)
            => Visit(symbol.Type);

        public override bool VisitProperty(IPropertySymbol symbol)
            => Visit(symbol.Type);

        public override bool VisitPointerType(IPointerTypeSymbol symbol)
            => Visit(symbol.PointedAtType);
    }
}
