// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal sealed class LambdaSymbol : MethodSymbol
    {
        private readonly Symbol _containingSymbol;
        private readonly MessageID _messageID;
        private readonly CSharpSyntaxNode _syntax;
        private readonly ImmutableArray<ParameterSymbol> _parameters;
        private TypeSymbolWithAnnotations _returnType;
        private readonly bool _isSynthesized;
        private readonly bool _isAsync;

        /// <summary>
        /// This symbol is used as the return type of a LambdaSymbol when we are interpreting 
        /// lambda's body in order to infer its return type.
        /// </summary>
        internal static readonly TypeSymbolWithAnnotations ReturnTypeIsBeingInferred = TypeSymbolWithAnnotations.Create(new UnsupportedMetadataTypeSymbol());

        public LambdaSymbol(
            CSharpCompilation compilation,
            Symbol containingSymbol,
            UnboundLambda unboundLambda,
            ImmutableArray<ParameterSymbol> delegateParameters,
            TypeSymbol returnType)
        {
            _containingSymbol = containingSymbol;
            _messageID = unboundLambda.Data.MessageID;
            _syntax = unboundLambda.Syntax;
            _returnType = (object)returnType == null ? ReturnTypeIsBeingInferred : TypeSymbolWithAnnotations.Create(returnType);
            _isSynthesized = unboundLambda.WasCompilerGenerated;
            _isAsync = unboundLambda.IsAsync;
            // No point in making this lazy. We are always going to need these soon after creation of the symbol.
            _parameters = MakeParameters(compilation, unboundLambda, delegateParameters);
        }

        public LambdaSymbol(
            Symbol containingSymbol,
            ImmutableArray<ParameterSymbol> parameters,
            TypeSymbol returnType,
            MessageID messageID,
            CSharpSyntaxNode syntax,
            bool isSynthesized,
            bool isAsync)
        {
            _containingSymbol = containingSymbol;
            _messageID = messageID;
            _syntax = syntax;
            _returnType = (object)returnType == null ? ReturnTypeIsBeingInferred : TypeSymbolWithAnnotations.Create(returnType); 
            _isSynthesized = isSynthesized;
            _isAsync = isAsync;
            _parameters = parameters.SelectAsArray(CopyParameter, this);
        }

        public LambdaSymbol(
            Symbol containingSymbol,
            ImmutableArray<ParameterSymbol> parameters,
            TypeSymbolWithAnnotations returnType,
            MessageID messageID,
            CSharpSyntaxNode syntax,
            bool isSynthesized,
            bool isAsync)
        {
            _containingSymbol = containingSymbol;
            _messageID = messageID;
            _syntax = syntax;
            _returnType = returnType;
            _isSynthesized = isSynthesized;
            _isAsync = isAsync;
            _parameters = parameters.SelectAsArray(CopyParameter, this);
        }

        internal LambdaSymbol ToContainer(Symbol containingSymbol)
        {
            return new LambdaSymbol(
                containingSymbol,
                _parameters,
                _returnType,
                _messageID,
                _syntax,
                _isSynthesized,
                _isAsync);
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
            get { return (object)this.ReturnType != null && this.ReturnType.SpecialType == SpecialType.System_Void; }
        }

        public override TypeSymbolWithAnnotations ReturnType
        {
            get { return _returnType; }
        }

        // In error recovery and type inference scenarios we do not know the return type
        // until after the body is bound, but the symbol is created before the body
        // is bound.  Fill in the return type post hoc in these scenarios; the
        // IDE might inspect the symbol and want to know the return type.
        internal void SetInferredReturnType(TypeSymbol inferredReturnType)
        {
            Debug.Assert((object)inferredReturnType != null);
            Debug.Assert((object)_returnType == ReturnTypeIsBeingInferred);
            _returnType = TypeSymbolWithAnnotations.Create(inferredReturnType);
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

        public override ImmutableArray<TypeSymbolWithAnnotations> TypeArguments
        {
            get { return ImmutableArray<TypeSymbolWithAnnotations>.Empty; }
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
            ImmutableArray<ParameterSymbol> delegateParameters)
        {
            if (!unboundLambda.HasSignature || unboundLambda.ParameterCount == 0)
            {
                // The parameters may be omitted in source, but they are still present on the symbol.
                return delegateParameters.SelectAsArray(CopyParameter, this);
            }

            var builder = ArrayBuilder<ParameterSymbol>.GetInstance();
            var hasExplicitlyTypedParameterList = unboundLambda.HasExplicitlyTypedParameterList;
            var numDelegateParameters = delegateParameters.IsDefault ? 0 : delegateParameters.Length;

            for (int p = 0; p < unboundLambda.ParameterCount; ++p)
            {
                // If there are no types given in the lambda then used the delegate type.
                // If the lambda is typed then the types probably match the delegate types;
                // if they do not, use the lambda types for binding. Either way, if we 
                // can, then we use the lambda types. (Whatever you do, do not use the names 
                // in the delegate parameters; they are not in scope!)

                TypeSymbolWithAnnotations type;
                RefKind refKind;
                if (hasExplicitlyTypedParameterList)
                {
                    type = unboundLambda.ParameterType(p);
                    refKind = unboundLambda.RefKind(p);
                }
                else if (p < numDelegateParameters)
                {
                    ParameterSymbol delegateParameter = delegateParameters[p];
                    type = delegateParameter.Type;
                    refKind = delegateParameter.RefKind;
                }
                else
                {
                    type = TypeSymbolWithAnnotations.Create(new ExtendedErrorTypeSymbol(compilation, name: string.Empty, arity: 0, errorInfo: null));
                    refKind = RefKind.None;
                }

                var name = unboundLambda.ParameterName(p);
                var location = unboundLambda.ParameterLocation(p);
                var locations = ImmutableArray.Create<Location>(location);
                var parameter = new SourceSimpleParameterSymbol(this, type, p, refKind, name, locations);

                builder.Add(parameter);
            }

            var result = builder.ToImmutableAndFree();

            return result;
        }

        private static ParameterSymbol CopyParameter(ParameterSymbol parameter, MethodSymbol owner)
        {
            return new SynthesizedParameterSymbol(
                    owner,
                parameter.Type.TypeSymbol,
                parameter.Ordinal,
                parameter.RefKind,
                    string.Empty); // Make sure nothing binds to this.
        }

        public sealed override bool Equals(object symbol)
        {
            if ((object)this == symbol) return true;

            var lambda = symbol as LambdaSymbol;
            return (object)lambda != null
                && lambda._syntax == _syntax
                && lambda.ReturnType.TypeSymbol == this.ReturnType.TypeSymbol
                && System.Linq.ImmutableArrayExtensions.SequenceEqual(lambda.ParameterTypes, this.ParameterTypes)
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

        internal override int CalculateLocalSyntaxOffset(int localPosition, SyntaxTree localTree)
        {
            throw ExceptionUtilities.Unreachable;
        }
    }
}
