// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading;
using Microsoft.Cci;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class MethodSymbol : Symbol, IMethodSymbol
    {
        private readonly Symbols.MethodSymbol _underlying;
        private ITypeSymbol _lazyReturnType;
        private ImmutableArray<ITypeSymbol> _lazyTypeArguments;
        private ImmutableArray<IParameterSymbol> _lazyParameters;
        private ITypeSymbol _lazyReceiverType;

        public MethodSymbol(Symbols.MethodSymbol underlying)
        {
            Debug.Assert(underlying is object);
            _underlying = underlying;
        }

        internal override CSharp.Symbol UnderlyingSymbol => _underlying;
        internal Symbols.MethodSymbol UnderlyingMethodSymbol => _underlying;

        MethodKind IMethodSymbol.MethodKind
        {
            get
            {
                switch (_underlying.MethodKind)
                {
                    case MethodKind.AnonymousFunction:
                        return MethodKind.AnonymousFunction;
                    case MethodKind.Constructor:
                        return MethodKind.Constructor;
                    case MethodKind.Conversion:
                        return MethodKind.Conversion;
                    case MethodKind.DelegateInvoke:
                        return MethodKind.DelegateInvoke;
                    case MethodKind.Destructor:
                        return MethodKind.Destructor;
                    case MethodKind.EventAdd:
                        return MethodKind.EventAdd;
                    case MethodKind.EventRemove:
                        return MethodKind.EventRemove;
                    case MethodKind.ExplicitInterfaceImplementation:
                        return MethodKind.ExplicitInterfaceImplementation;
                    case MethodKind.UserDefinedOperator:
                        return MethodKind.UserDefinedOperator;
                    case MethodKind.BuiltinOperator:
                        return MethodKind.BuiltinOperator;
                    case MethodKind.Ordinary:
                        return MethodKind.Ordinary;
                    case MethodKind.PropertyGet:
                        return MethodKind.PropertyGet;
                    case MethodKind.PropertySet:
                        return MethodKind.PropertySet;
                    case MethodKind.ReducedExtension:
                        return MethodKind.ReducedExtension;
                    case MethodKind.StaticConstructor:
                        return MethodKind.StaticConstructor;
                    case MethodKind.LocalFunction:
                        return MethodKind.LocalFunction;
                    case MethodKind.FunctionPointerSignature:
                        return MethodKind.FunctionPointerSignature;
                    default:
                        throw ExceptionUtilities.UnexpectedValue(_underlying.MethodKind);
                }
            }
        }

        ITypeSymbol IMethodSymbol.ReturnType
        {
            get
            {
                if (_lazyReturnType is null)
                {
                    Interlocked.CompareExchange(ref _lazyReturnType, _underlying.ReturnTypeWithAnnotations.GetPublicSymbol(), null);
                }

                return _lazyReturnType;
            }
        }

        CodeAnalysis.NullableAnnotation IMethodSymbol.ReturnNullableAnnotation
        {
            get
            {
                return _underlying.ReturnTypeWithAnnotations.ToPublicAnnotation();
            }
        }

        ImmutableArray<ITypeSymbol> IMethodSymbol.TypeArguments
        {
            get
            {
                return InterlockedOperations.Initialize(
                    ref _lazyTypeArguments,
                    static underlying => underlying.TypeArgumentsWithAnnotations.GetPublicSymbols(),
                    _underlying);
            }
        }

        ImmutableArray<CodeAnalysis.NullableAnnotation> IMethodSymbol.TypeArgumentNullableAnnotations =>
            _underlying.TypeArgumentsWithAnnotations.ToPublicAnnotations();

        ImmutableArray<ITypeParameterSymbol> IMethodSymbol.TypeParameters
        {
            get
            {
                return _underlying.TypeParameters.GetPublicSymbols();
            }
        }

        ImmutableArray<IParameterSymbol> IMethodSymbol.Parameters
        {
            get
            {
                return InterlockedOperations.Initialize(
                    ref _lazyParameters,
                    static underlying => underlying.Parameters.GetPublicSymbols(),
                    _underlying);
            }
        }

        IMethodSymbol IMethodSymbol.ConstructedFrom
        {
            get
            {
                return _underlying.ConstructedFrom.GetPublicSymbol();
            }
        }

        bool IMethodSymbol.IsReadOnly
        {
            get
            {
                return _underlying.IsEffectivelyReadOnly;
            }
        }

        bool IMethodSymbol.IsInitOnly
        {
            get
            {
                return _underlying.IsInitOnly;
            }
        }

        IMethodSymbol IMethodSymbol.OriginalDefinition
        {
            get
            {
                return _underlying.OriginalDefinition.GetPublicSymbol();
            }
        }

        IMethodSymbol IMethodSymbol.OverriddenMethod
        {
            get
            {
                return _underlying.OverriddenMethod.GetPublicSymbol();
            }
        }

        ITypeSymbol IMethodSymbol.ReceiverType
        {
            get
            {
                if (_lazyReceiverType is null)
                {
                    Interlocked.CompareExchange(ref _lazyReceiverType, _underlying.ReceiverType?.GetITypeSymbol(_underlying.ReceiverNullableAnnotation), null);
                }

                return _lazyReceiverType;
            }
        }

        CodeAnalysis.NullableAnnotation IMethodSymbol.ReceiverNullableAnnotation => _underlying.ReceiverNullableAnnotation;

        IMethodSymbol IMethodSymbol.ReducedFrom
        {
            get
            {
                return _underlying.ReducedFrom.GetPublicSymbol();
            }
        }

        ITypeSymbol IMethodSymbol.GetTypeInferredDuringReduction(ITypeParameterSymbol reducedFromTypeParameter)
        {
            return _underlying.GetTypeInferredDuringReduction(
                reducedFromTypeParameter.EnsureCSharpSymbolOrNull(nameof(reducedFromTypeParameter))).
                GetPublicSymbol();
        }

#nullable enable
        IMethodSymbol? IMethodSymbol.ReduceExtensionMethod(ITypeSymbol receiverType)
        {
            return _underlying.ReduceExtensionMethod(
                receiverType.EnsureCSharpSymbolOrNull(nameof(receiverType)), compilation: null).
                GetPublicSymbol();
        }

        IMethodSymbol? IMethodSymbol.ReduceExtensionMember(ITypeSymbol receiverType)
        {
            if (_underlying.IsExtensionBlockMember() && SourceMemberContainerTypeSymbol.IsAllowedExtensionMember(_underlying))
            {
                var csharpReceiver = receiverType.EnsureCSharpSymbolOrNull(nameof(receiverType));
                return (IMethodSymbol?)SourceNamedTypeSymbol.ReduceExtensionMember(compilation: null, _underlying, csharpReceiver, wasExtensionFullyInferred: out _).GetPublicSymbol();
            }

            return null;
        }
#nullable disable

        ImmutableArray<IMethodSymbol> IMethodSymbol.ExplicitInterfaceImplementations
        {
            get
            {
                return _underlying.ExplicitInterfaceImplementations.GetPublicSymbols();
            }
        }

        ISymbol IMethodSymbol.AssociatedSymbol
        {
            get
            {
                return _underlying.AssociatedSymbol.GetPublicSymbol();
            }
        }

        bool IMethodSymbol.IsGenericMethod
        {
            get
            {
                return _underlying.IsGenericMethod;
            }
        }

        bool IMethodSymbol.IsAsync
        {
            get
            {
                return _underlying.IsAsync;
            }
        }

        bool IMethodSymbol.HidesBaseMethodsByName
        {
            get
            {
                return _underlying.HidesBaseMethodsByName;
            }
        }

        ImmutableArray<CustomModifier> IMethodSymbol.ReturnTypeCustomModifiers
        {
            get
            {
                return _underlying.ReturnTypeWithAnnotations.CustomModifiers;
            }
        }

        ImmutableArray<CustomModifier> IMethodSymbol.RefCustomModifiers
        {
            get
            {
                return _underlying.RefCustomModifiers;
            }
        }

        ImmutableArray<AttributeData> IMethodSymbol.GetReturnTypeAttributes()
        {
            return _underlying.GetReturnTypeAttributes().Cast<CSharpAttributeData, AttributeData>();
        }

        SignatureCallingConvention IMethodSymbol.CallingConvention => _underlying.CallingConvention.ToSignatureConvention();

        ImmutableArray<INamedTypeSymbol> IMethodSymbol.UnmanagedCallingConventionTypes => _underlying.UnmanagedCallingConventionTypes.SelectAsArray(t => t.GetPublicSymbol());

        IMethodSymbol IMethodSymbol.Construct(params ITypeSymbol[] typeArguments)
        {
            return _underlying.Construct(ConstructTypeArguments(typeArguments)).GetPublicSymbol();
        }

        IMethodSymbol IMethodSymbol.Construct(ImmutableArray<ITypeSymbol> typeArguments, ImmutableArray<CodeAnalysis.NullableAnnotation> typeArgumentNullableAnnotations)
        {
            return _underlying.Construct(ConstructTypeArguments(typeArguments, typeArgumentNullableAnnotations)).GetPublicSymbol();
        }

        IMethodSymbol IMethodSymbol.PartialImplementationPart
        {
            get
            {
                return _underlying.PartialImplementationPart.GetPublicSymbol();
            }
        }

        IMethodSymbol IMethodSymbol.PartialDefinitionPart
        {
            get
            {
                return _underlying.PartialDefinitionPart.GetPublicSymbol();
            }
        }

        bool IMethodSymbol.IsPartialDefinition => _underlying.IsDefinition && _underlying.IsPartialDefinition();

        INamedTypeSymbol IMethodSymbol.AssociatedAnonymousDelegate
        {
            get
            {
                return null;
            }
        }

        int IMethodSymbol.Arity => _underlying.Arity;

        bool IMethodSymbol.IsExtensionMethod => _underlying.IsExtensionMethod;

        System.Reflection.MethodImplAttributes IMethodSymbol.MethodImplementationFlags => _underlying.ImplementationAttributes;

        bool IMethodSymbol.IsVararg => _underlying.IsVararg;

        bool IMethodSymbol.IsCheckedBuiltin => _underlying.IsCheckedBuiltin;

        bool IMethodSymbol.ReturnsVoid => _underlying.ReturnsVoid;

        bool IMethodSymbol.ReturnsByRef => _underlying.ReturnsByRef;

        bool IMethodSymbol.ReturnsByRefReadonly => _underlying.ReturnsByRefReadonly;

        RefKind IMethodSymbol.RefKind => _underlying.RefKind;

        bool IMethodSymbol.IsConditional => _underlying.IsConditional;

        bool IMethodSymbol.IsIterator => _underlying.IsIterator;

        DllImportData IMethodSymbol.GetDllImportData() => _underlying.GetDllImportData();

#nullable enable
        IMethodSymbol? IMethodSymbol.AssociatedExtensionImplementation
        {
            get
            {
                if (!_underlying.IsExtensionBlockMember())
                {
                    return null;
                }

                var implDefinition = _underlying.OriginalDefinition.TryGetCorrespondingExtensionImplementationMethod();
                if (implDefinition is null)
                {
                    return null;
                }

                var enclosing = _underlying.ContainingType.ContainingType;
                var implementation = implDefinition.AsMember(enclosing);
                if (implementation.Arity != 0)
                {
                    var typeArguments = ArrayBuilder<TypeWithAnnotations>.GetInstance(implementation.Arity);
                    typeArguments.AddRange(_underlying.ContainingType.TypeArgumentsWithAnnotationsNoUseSiteDiagnostics);
                    typeArguments.AddRange(_underlying.TypeArgumentsWithAnnotations);
                    implementation = implementation.Construct(typeArguments.ToImmutableAndFree());
                }

                return implementation.GetPublicSymbol();
            }
        }
#nullable disable

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitMethod(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitMethod(this);
        }

        protected override TResult Accept<TArgument, TResult>(SymbolVisitor<TArgument, TResult> visitor, TArgument argument)
        {
            return visitor.VisitMethod(this, argument);
        }

        #endregion
    }
}
