// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols.PublicModel
{
    internal sealed class MethodSymbol : Symbol, IMethodSymbol
    {
        private readonly Symbols.MethodSymbol _underlying;
        private ITypeSymbol _lazyReturnType;
        private ImmutableArray<ITypeSymbol> _lazyTypeArguments;
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
                if (_lazyTypeArguments.IsDefault)
                {

                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyTypeArguments, _underlying.TypeArgumentsWithAnnotations.GetPublicSymbols(), default);
                }

                return _lazyTypeArguments;
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
                return _underlying.Parameters.GetPublicSymbols();
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

        IMethodSymbol IMethodSymbol.ReduceExtensionMethod(ITypeSymbol receiverType)
        {
            return _underlying.ReduceExtensionMethod(
                receiverType.EnsureCSharpSymbolOrNull(nameof(receiverType)), compilation: null).
                GetPublicSymbol();
        }

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

        INamedTypeSymbol IMethodSymbol.AssociatedAnonymousDelegate
        {
            get
            {
                return null;
            }
        }

        int IMethodSymbol.Arity => _underlying.Arity;

        bool IMethodSymbol.IsExtensionMethod => _underlying.IsExtensionMethod;

        bool IMethodSymbol.IsVararg => _underlying.IsVararg;

        bool IMethodSymbol.IsCheckedBuiltin => _underlying.IsCheckedBuiltin;

        bool IMethodSymbol.ReturnsVoid => _underlying.ReturnsVoid;

        bool IMethodSymbol.ReturnsByRef => _underlying.ReturnsByRef;

        bool IMethodSymbol.ReturnsByRefReadonly => _underlying.ReturnsByRefReadonly;

        RefKind IMethodSymbol.RefKind => _underlying.RefKind;

        bool IMethodSymbol.IsConditional => _underlying.IsConditional;

        DllImportData IMethodSymbol.GetDllImportData() => _underlying.GetDllImportData();

        #region ISymbol Members

        protected override void Accept(SymbolVisitor visitor)
        {
            visitor.VisitMethod(this);
        }

        protected override TResult Accept<TResult>(SymbolVisitor<TResult> visitor)
        {
            return visitor.VisitMethod(this);
        }

        #endregion
    }
}
