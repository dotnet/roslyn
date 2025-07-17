// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// Shared <see cref="SymbolVisitor"/> that looks for instances of <see cref="ITypeParameterSymbol"/>
    /// that are not in a provided allow list.
    /// </summary>
    internal abstract class AbstractTypeParameterChecker : SymbolVisitor
    {
        private readonly HashSet<ITypeParameterSymbol> _acceptableTypeParameters;

        protected AbstractTypeParameterChecker(ImmutableArray<ITypeParameterSymbol> acceptableTypeParameters)
        {
            _acceptableTypeParameters = [.. acceptableTypeParameters];
        }

        public abstract IParameterSymbol GetThisParameter(IMethodSymbol method);

        public sealed override void VisitTypeParameter(ITypeParameterSymbol symbol)
        {
            // Using an if, rather than assert condition, to make debugging easier.
            if (!_acceptableTypeParameters.Contains(symbol))
            {
                Debug.Assert(false,
                    string.Format("Unexpected type parameter {0} owned by {1}", symbol, symbol.ContainingSymbol));
            }

            foreach (var constraintType in symbol.ConstraintTypes)
            {
                Visit(constraintType);
            }
        }

        public sealed override void VisitAlias(IAliasSymbol symbol)
        {
            Visit(symbol.Target);
        }

        public sealed override void VisitArrayType(IArrayTypeSymbol symbol)
        {
            Visit(symbol.ElementType);
        }

        public sealed override void VisitPointerType(IPointerTypeSymbol symbol)
        {
            Visit(symbol.PointedAtType);
        }

        public sealed override void VisitParameter(IParameterSymbol symbol)
        {
            Visit(symbol.Type);

            // Specifically not visiting containing symbol because VisitMethod visits the parameters (i.e. cycle).
        }

        public sealed override void VisitLocal(ILocalSymbol symbol)
        {
            Visit(symbol.Type);

            Visit(symbol.ContainingSymbol);
        }

        public sealed override void VisitEvent(IEventSymbol symbol)
        {
            Visit(symbol.Type);

            Visit(symbol.ContainingSymbol);
        }

        public sealed override void VisitField(IFieldSymbol symbol)
        {
            Visit(symbol.Type);

            Visit(symbol.ContainingSymbol);
        }

        public sealed override void VisitMethod(IMethodSymbol symbol)
        {
            if (!symbol.ReturnsVoid)
            {
                Visit(symbol.ReturnType);
            }

            foreach (var typeArgument in symbol.TypeArguments)
            {
                Visit(typeArgument);
            }

            Visit(GetThisParameter(symbol));

            foreach (var parameter in symbol.Parameters)
            {
                Visit(parameter);
            }

            Visit(symbol.ContainingSymbol);
        }

        public sealed override void VisitProperty(IPropertySymbol symbol)
        {
            Visit(symbol.Type);

            foreach (var parameter in symbol.Parameters)
            {
                Visit(parameter);
            }

            Visit(symbol.ContainingSymbol);
        }

        public sealed override void VisitNamedType(INamedTypeSymbol symbol)
        {
            foreach (var typeArgument in symbol.TypeArguments)
            {
                Visit(typeArgument);
            }

            Visit(symbol.ContainingSymbol);
        }

        public sealed override void VisitDynamicType(IDynamicTypeSymbol symbol)
        {
            // Fine.
        }

        public sealed override void VisitLabel(ILabelSymbol symbol)
        {
            // Fine.
        }

        public sealed override void VisitNamespace(INamespaceSymbol symbol)
        {
            // Fine.
        }

        public sealed override void VisitAssembly(IAssemblySymbol symbol)
        {
            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }

        public sealed override void VisitModule(IModuleSymbol symbol)
        {
            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }

        public sealed override void VisitRangeVariable(IRangeVariableSymbol symbol)
        {
            throw ExceptionUtilities.UnexpectedValue(symbol.Kind);
        }
    }
}
