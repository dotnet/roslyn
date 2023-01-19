// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;

namespace Microsoft.CodeAnalysis.CSharp.ExpressionEvaluator
{
    /// <summary>
    /// The purpose of this symbol is to represent the method that we are actually in during evaluation,
    /// e.g. a lambda method in a display class. That method is actually a method from a PE module and we want
    /// to use it as a context for binding in order to make sure that the name lookup and other context dependent
    /// binding operations work properly. However, if we the use a symbol imported from the PE,
    /// symbols for locals, lambdas and local functions that will be directly or indirectly parented to it
    /// during binding won't be able to locate their <see cref="Symbol.DeclaringCompilation"/>, which will break
    /// assumptions made in different parts of the compilation pipeline. 
    /// Instead we create this symbol that represents exactly the same method, but pretends that 
    /// it is declared by EE compilation and allows to return that compilation as <see cref="Symbol.DeclaringCompilation"/>
    /// for all child symbols created during EE compilation process for evaluation.
    /// </summary>
    internal sealed class EECompilationContextMethod : WrappedMethodSymbol
    {
        private readonly MethodSymbol _underlyingMethod;
        private readonly CSharpCompilation _compilation;

        private readonly ImmutableArray<TypeParameterSymbol> _typeParameters;
        private readonly ImmutableArray<ParameterSymbol> _parameters;

        public EECompilationContextMethod(CSharpCompilation compilation, MethodSymbol underlyingMethod)
        {
            Debug.Assert(underlyingMethod.IsDefinition);

            _compilation = compilation;

            var typeMap = underlyingMethod.ContainingType.TypeSubstitution ?? TypeMap.Empty;
            typeMap.WithAlphaRename(underlyingMethod, this, out _typeParameters);

            _underlyingMethod = underlyingMethod.ConstructIfGeneric(TypeArgumentsWithAnnotations);
            _parameters = SynthesizedParameterSymbol.DeriveParameters(_underlyingMethod, this);
        }

        public override MethodSymbol UnderlyingMethod => _underlyingMethod;

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return _typeParameters; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return GetTypeParametersAsTypeArguments(); }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations => _underlyingMethod.ReturnTypeWithAnnotations;

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations => ImmutableArray<MethodSymbol>.Empty;

        public override ImmutableArray<CustomModifier> RefCustomModifiers => _underlyingMethod.RefCustomModifiers;

        public override Symbol? AssociatedSymbol => _underlyingMethod.AssociatedSymbol;

        public override Symbol ContainingSymbol => _underlyingMethod.ContainingSymbol;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            return _underlyingMethod.CalculateLocalSyntaxOffset(localPosition, localTree);
        }

        internal override UnmanagedCallersOnlyAttributeData? GetUnmanagedCallersOnlyAttributeData(bool forceComplete)
        {
            return _underlyingMethod.GetUnmanagedCallersOnlyAttributeData(forceComplete);
        }

        internal override bool IsNullableAnalysisEnabled()
        {
            return _underlyingMethod.IsNullableAnalysisEnabled();
        }

        internal override CSharpCompilation DeclaringCompilation => _compilation;

        internal override bool TryGetThisParameter(out ParameterSymbol? thisParameter)
        {
            ParameterSymbol underlyingThisParameter;
            if (!_underlyingMethod.TryGetThisParameter(out underlyingThisParameter))
            {
                thisParameter = null;
                return false;
            }

            thisParameter = (object)underlyingThisParameter != null
                ? new ThisParameterSymbol(this)
                : null;
            return true;
        }
    }
}
