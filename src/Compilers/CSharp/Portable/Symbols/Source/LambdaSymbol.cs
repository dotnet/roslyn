// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LambdaSymbol : SourceMethodSymbol
    {
        private readonly Symbol _containingSymbol;
        private readonly MessageID _messageID;
        private readonly SyntaxNode _syntax;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private RefKind _refKind;
        private TypeWithAnnotations _returnType;
        private readonly bool _isSynthesized;
        private readonly bool _isAsync;

        /// <summary>
        /// This symbol is used as the return type of a LambdaSymbol when we are interpreting
        /// lambda's body in order to infer its return type.
        /// </summary>
        internal static readonly TypeSymbol ReturnTypeIsBeingInferred = new UnsupportedMetadataTypeSymbol();

        /// <summary>
        /// This symbol is used as the return type of a LambdaSymbol when we failed to infer its return type.
        /// </summary>
        internal static readonly TypeSymbol InferenceFailureReturnType = new UnsupportedMetadataTypeSymbol();

        private static readonly TypeWithAnnotations UnknownReturnType = TypeWithAnnotations.Create(ErrorTypeSymbol.UnknownResultType);

        public LambdaSymbol(
            CSharpCompilation compilation,
            Symbol containingSymbol,
            UnboundLambda unboundLambda,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            RefKind refKind,
            TypeWithAnnotations returnType,
            DiagnosticBag diagnostics)
        {
            _containingSymbol = containingSymbol;
            _messageID = unboundLambda.Data.MessageID;
            _syntax = unboundLambda.Syntax;
            _refKind = refKind;
            _returnType = !returnType.HasType ? TypeWithAnnotations.Create(ReturnTypeIsBeingInferred) : returnType;
            _isSynthesized = unboundLambda.WasCompilerGenerated;
            _isAsync = unboundLambda.IsAsync;
            // No point in making this lazy. We are always going to need these soon after creation of the symbol.
            _parameters = MakeParameters(compilation, unboundLambda, parameterTypes, parameterRefKinds, diagnostics);
        }

        public MessageID MessageID { get { return _messageID; } }

        public override MethodKind MethodKind
        {
            get { return MethodKind.AnonymousFunction; }
        }

        public override bool IsExtern
        {
            get { return false; }
        }

        public override bool IsSealed
        {
            get { return false; }
        }

        public override bool IsAbstract
        {
            get { return false; }
        }

        public override bool IsVirtual
        {
            get { return false; }
        }

        public override bool IsOverride
        {
            get { return false; }
        }

        public override bool IsStatic
        {
            get { return false; }
        }

        public override bool IsAsync
        {
            get { return _isAsync; }
        }

        internal sealed override ObsoleteAttributeData ObsoleteAttributeData
        {
            get { return null; }
        }

        internal sealed override bool IsMetadataNewSlot(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal sealed override bool IsMetadataVirtual(bool ignoreInterfaceImplementationChanges = false)
        {
            return false;
        }

        internal override bool IsMetadataFinal
        {
            get
            {
                return false;
            }
        }

        public override bool IsVararg
        {
            get { return false; }
        }

        internal override bool HasSpecialName
        {
            get { return false; }
        }

        internal override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return default(System.Reflection.MethodImplAttributes); }
        }

        internal override bool RequiresSecurityObject
        {
            get { return false; }
        }

        public override DllImportData GetDllImportData()
        {
            return null;
        }

        internal override MarshalPseudoCustomAttributeData ReturnValueMarshallingInformation
        {
            get { return null; }
        }

        internal override bool HasDeclarativeSecurity
        {
            get { return false; }
        }

        internal override IEnumerable<Microsoft.Cci.SecurityAttribute> GetSecurityInformation()
        {
            throw ExceptionUtilities.Unreachable;
        }

        internal override ImmutableArray<string> GetAppliedConditionalSymbols()
        {
            return ImmutableArray<string>.Empty;
        }

        public override bool ReturnsVoid
        {
            get { return this.ReturnTypeWithAnnotations.HasType && this.ReturnType.IsVoidType(); }
        }

        public override RefKind RefKind
        {
            get { return _refKind; }
        }

        public override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get { return _returnType; }
        }

        public override FlowAnalysisAnnotations ReturnTypeFlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        public override ImmutableHashSet<string> ReturnNotNullIfParameterNotNull => ImmutableHashSet<string>.Empty;

        public override FlowAnalysisAnnotations FlowAnalysisAnnotations => FlowAnalysisAnnotations.None;

        // In error recovery and type inference scenarios we do not know the return type
        // until after the body is bound, but the symbol is created before the body
        // is bound.  Fill in the return type post hoc in these scenarios; the
        // IDE might inspect the symbol and want to know the return type.
        internal void SetInferredReturnType(RefKind refKind, TypeWithAnnotations inferredReturnType)
        {
            Debug.Assert(inferredReturnType.HasType);
            Debug.Assert((object)_returnType.Type == ReturnTypeIsBeingInferred);
            _refKind = refKind;
            _returnType = inferredReturnType;
        }

        public override ImmutableArray<CustomModifier> RefCustomModifiers
        {
            get { return ImmutableArray<CustomModifier>.Empty; }
        }

        internal override bool IsExplicitInterfaceImplementation
        {
            get { return false; }
        }

        public override ImmutableArray<MethodSymbol> ExplicitInterfaceImplementations
        {
            get { return ImmutableArray<MethodSymbol>.Empty; }
        }

        public override Symbol AssociatedSymbol
        {
            get { return null; }
        }

        public override ImmutableArray<TypeWithAnnotations> TypeArgumentsWithAnnotations
        {
            get { return ImmutableArray<TypeWithAnnotations>.Empty; }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get { return ImmutableArray<TypeParameterSymbol>.Empty; }
        }

        public override int Arity
        {
            get { return 0; }
        }

        public override ImmutableArray<ParameterSymbol> Parameters
        {
            get { return _parameters; }
        }

        internal override bool TryGetThisParameter(out ParameterSymbol thisParameter)
        {
            // Lambda symbols have no "this" parameter
            thisParameter = null;
            return true;
        }

        public override Accessibility DeclaredAccessibility
        {
            get { return Accessibility.Private; }
        }

        public override ImmutableArray<Location> Locations
        {
            get
            {
                return ImmutableArray.Create<Location>(_syntax.Location);
            }
        }

        /// <summary>
        /// Locations[0] on lambda symbols covers the entire syntax, which is inconvenient but remains for compatibility.
        /// For better diagnostics quality, use the DiagnosticLocation instead, which points to the "delegate" or the "=>".
        /// </summary>
        internal Location DiagnosticLocation
        {
            get
            {
                switch (_syntax.Kind())
                {
                    case SyntaxKind.AnonymousMethodExpression:
                        return ((AnonymousMethodExpressionSyntax)_syntax).DelegateKeyword.GetLocation();
                    case SyntaxKind.SimpleLambdaExpression:
                    case SyntaxKind.ParenthesizedLambdaExpression:
                        return ((LambdaExpressionSyntax)_syntax).ArrowToken.GetLocation();
                    default:
                        return Locations[0];
                }
            }
        }

        public override ImmutableArray<SyntaxReference> DeclaringSyntaxReferences
        {
            get
            {
                return ImmutableArray.Create<SyntaxReference>(_syntax.GetReference());
            }
        }

        public override Symbol ContainingSymbol
        {
            get { return _containingSymbol; }
        }

        internal override Microsoft.Cci.CallingConvention CallingConvention
        {
            get { return Microsoft.Cci.CallingConvention.Default; }
        }

        public override bool IsExtensionMethod
        {
            get { return false; }
        }

        public override bool HidesBaseMethodsByName
        {
            get { return false; }
        }

        private ImmutableArray<ParameterSymbol> MakeParameters(
            CSharpCompilation compilation,
            UnboundLambda unboundLambda,
            ImmutableArray<TypeWithAnnotations> parameterTypes,
            ImmutableArray<RefKind> parameterRefKinds,
            DiagnosticBag diagnostics)
        {
            Debug.Assert(parameterTypes.Length == parameterRefKinds.Length);

            if (!unboundLambda.HasSignature || unboundLambda.ParameterCount == 0)
            {
                // The parameters may be omitted in source, but they are still present on the symbol.
                return parameterTypes.SelectAsArray((type, ordinal, arg) =>
                                                        SynthesizedParameterSymbol.Create(
                                                            arg.owner,
                                                            type,
                                                            ordinal,
                                                            arg.refKinds[ordinal],
                                                            GeneratedNames.LambdaCopyParameterName(ordinal)), // Make sure nothing binds to this.
                                                     (owner: this, refKinds: parameterRefKinds));
            }

            var builder = ArrayBuilder<ParameterSymbol>.GetInstance(unboundLambda.ParameterCount);
            var hasExplicitlyTypedParameterList = unboundLambda.HasExplicitlyTypedParameterList;
            var numDelegateParameters = parameterTypes.Length;

            for (int p = 0; p < unboundLambda.ParameterCount; ++p)
            {
                // If there are no types given in the lambda then used the delegate type.
                // If the lambda is typed then the types probably match the delegate types;
                // if they do not, use the lambda types for binding. Either way, if we 
                // can, then we use the lambda types. (Whatever you do, do not use the names 
                // in the delegate parameters; they are not in scope!)

                TypeWithAnnotations type;
                RefKind refKind;
                if (hasExplicitlyTypedParameterList)
                {
                    type = unboundLambda.ParameterTypeWithAnnotations(p);
                    refKind = unboundLambda.RefKind(p);
                }
                else if (p < numDelegateParameters)
                {
                    type = parameterTypes[p];
                    refKind = parameterRefKinds[p];
                }
                else
                {
                    type = TypeWithAnnotations.Create(new ExtendedErrorTypeSymbol(compilation, name: string.Empty, arity: 0, errorInfo: null));
                    refKind = RefKind.None;
                }

                var name = unboundLambda.ParameterName(p);
                var location = unboundLambda.ParameterLocation(p);
                var locations = location == null ? ImmutableArray<Location>.Empty : ImmutableArray.Create<Location>(location);
                var parameter = new SourceSimpleParameterSymbol(this, type, p, refKind, name, locations);
                ParameterHelpers.AddNullCheckingErrorsToParameter(diagnostics, parameter);
                builder.Add(parameter);
            }

            var result = builder.ToImmutableAndFree();

            return result;
        }

        public sealed override bool Equals(Symbol symbol, TypeCompareKind compareKind)
        {
            if ((object)this == symbol) return true;

            var lambda = symbol as LambdaSymbol;
            return (object)lambda != null
                && lambda._syntax == _syntax
                && lambda._refKind == _refKind
                && TypeSymbol.Equals(lambda.ReturnType, this.ReturnType, compareKind)
                && System.Linq.ImmutableArrayExtensions.SequenceEqual(lambda.ParameterTypesWithAnnotations, this.ParameterTypesWithAnnotations, (p1, p2) => p1.Type.Equals(p2.Type))
                && Equals(lambda.ContainingSymbol, this.ContainingSymbol);
        }

        public override int GetHashCode()
        {
            return _syntax.GetHashCode();
        }

        public override bool IsImplicitlyDeclared
        {
            get
            {
                return _isSynthesized;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return true; }
        }

        internal override bool IsDeclaredReadOnly => false;

        public override ImmutableArray<TypeParameterConstraintClause> GetTypeParameterConstraintClauses() => ImmutableArray<TypeParameterConstraintClause>.Empty;

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
