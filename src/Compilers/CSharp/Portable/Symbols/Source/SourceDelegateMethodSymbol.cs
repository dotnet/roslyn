// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.PooledObjects;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceDelegateMethodSymbol : SourceMemberMethodSymbol
    {
        private ImmutableArray<ParameterSymbol> _parameters;
        private readonly TypeWithAnnotations _returnType;

        protected SourceDelegateMethodSymbol(
            SourceMemberContainerTypeSymbol delegateType,
            TypeWithAnnotations returnType,
            DelegateDeclarationSyntax syntax,
            MethodKind methodKind,
            RefKind refKind,
            DeclarationModifiers declarationModifiers)
            : base(delegateType, syntax.GetReference(), location: syntax.Identifier.GetLocation(), isIterator: false,
                   (declarationModifiers, MakeFlags(
                                                    methodKind, refKind, declarationModifiers, returnType.IsVoidType(), returnsVoidIsSet: true, isExpressionBodied: false,
                                                    isExtensionMethod: false, isVarArg: false, isNullableAnalysisEnabled: false, isExplicitInterfaceImplementation: false, hasThisInitializer: false)))
        {
            _returnType = returnType;
        }

        internal sealed override ExecutableCodeBinder TryGetBodyBinder(BinderFactory binderFactoryOpt = null, bool ignoreAccessibility = false) => throw ExceptionUtilities.Unreachable();

        protected void InitializeParameters(ImmutableArray<ParameterSymbol> parameters)
        {
            Debug.Assert(_parameters.IsDefault);
            _parameters = parameters;
        }

        internal static void AddDelegateMembers(
            SourceMemberContainerTypeSymbol delegateType,
            ArrayBuilder<Symbol> symbols,
            DelegateDeclarationSyntax syntax,
            BindingDiagnosticBag diagnostics)
        {
            Binder binder = delegateType.GetBinder(syntax.ParameterList);
            TypeSyntax returnTypeSyntax = syntax.ReturnType;
            Debug.Assert(returnTypeSyntax is not ScopedTypeSyntax);

            returnTypeSyntax = returnTypeSyntax.SkipScoped(out _).SkipRefInLocalOrReturn(diagnostics, out RefKind refKind);
            var returnType = binder.BindType(returnTypeSyntax, diagnostics);

            // reuse types to avoid reporting duplicate errors if missing:
            var voidType = TypeWithAnnotations.Create(binder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax));
            // https://github.com/dotnet/roslyn/issues/30079: Should the 'object', IAsyncResult and AsyncCallback parameters be considered nullable or not nullable?
            var objectType = TypeWithAnnotations.Create(binder.GetSpecialType(SpecialType.System_Object, diagnostics, syntax));
            var intPtrType = TypeWithAnnotations.Create(binder.GetSpecialType(SpecialType.System_IntPtr, diagnostics, syntax));

            if (returnType.IsRestrictedType(ignoreSpanLikeTypes: true))
            {
                // The return type of a method, delegate, or function pointer cannot be '{0}'
                diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, returnTypeSyntax.Location, returnType.Type);
            }

            // A delegate has the following members: (see CLI spec 13.6)
            // (1) a method named Invoke with the specified signature
            var invoke = new InvokeMethod(delegateType, refKind, returnType, syntax, binder, diagnostics);
            invoke.CheckDelegateVarianceSafety(diagnostics);
            symbols.Add(invoke);

            // (2) a constructor with argument types (object, System.IntPtr)
            symbols.Add(new Constructor(delegateType, voidType, objectType, intPtrType, syntax));

            if (binder.Compilation.GetSpecialType(SpecialType.System_IAsyncResult).TypeKind != TypeKind.Error &&
                binder.Compilation.GetSpecialType(SpecialType.System_AsyncCallback).TypeKind != TypeKind.Error &&
                // WinRT delegates don't have Begin/EndInvoke methods
                !delegateType.IsCompilationOutputWinMdObj())
            {
                var iAsyncResultType = TypeWithAnnotations.Create(binder.GetSpecialType(SpecialType.System_IAsyncResult, diagnostics, syntax));
                var asyncCallbackType = TypeWithAnnotations.Create(binder.GetSpecialType(SpecialType.System_AsyncCallback, diagnostics, syntax));

                // (3) BeginInvoke
                symbols.Add(new BeginInvokeMethod(invoke, iAsyncResultType, objectType, asyncCallbackType, syntax));

                // and (4) EndInvoke methods
                symbols.Add(new EndInvokeMethod(invoke, iAsyncResultType, syntax));
            }

            if (delegateType.DeclaredAccessibility <= Accessibility.Private)
            {
                return;
            }

            var useSiteInfo = new CompoundUseSiteInfo<AssemblySymbol>(diagnostics, delegateType.ContainingAssembly);

            if (!delegateType.IsNoMoreVisibleThan(invoke.ReturnTypeWithAnnotations, ref useSiteInfo))
            {
                // Inconsistent accessibility: return type '{1}' is less accessible than delegate '{0}'
                diagnostics.Add(ErrorCode.ERR_BadVisDelegateReturn, delegateType.GetFirstLocation(), delegateType, invoke.ReturnType);
            }

            var delegateTypeIsFile = delegateType.HasFileLocalTypes();
            if (!delegateTypeIsFile && invoke.ReturnType.HasFileLocalTypes())
            {
                diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, delegateType.GetFirstLocation(), invoke.ReturnType, delegateType);
            }

            for (int i = 0; i < invoke.Parameters.Length; i++)
            {
                var parameterSymbol = invoke.Parameters[i];
                if (!parameterSymbol.TypeWithAnnotations.IsAtLeastAsVisibleAs(delegateType, ref useSiteInfo))
                {
                    // Inconsistent accessibility: parameter type '{1}' is less accessible than delegate '{0}'
                    diagnostics.Add(ErrorCode.ERR_BadVisDelegateParam, delegateType.GetFirstLocation(), delegateType, parameterSymbol.Type);
                }
                else if (!delegateTypeIsFile && parameterSymbol.Type.HasFileLocalTypes())
                {
                    diagnostics.Add(ErrorCode.ERR_FileTypeDisallowedInSignature, delegateType.GetFirstLocation(), parameterSymbol.Type, delegateType);
                }
            }

            diagnostics.Add(delegateType.GetFirstLocation(), useSiteInfo);
        }

        protected override void MethodChecks(BindingDiagnosticBag diagnostics)
        {
            // TODO: move more functionality into here, making these symbols more lazy
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                Debug.Assert(!_parameters.IsDefault, $"Expected {nameof(InitializeParameters)} prior to accessing this property.");
                return _parameters.NullToEmpty();
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        public override ImmutableArray<ImmutableArray<TypeWithAnnotations>> GetTypeParameterConstraintTypes()
            => ImmutableArray<ImmutableArray<TypeWithAnnotations>>.Empty;

        public override ImmutableArray<TypeParameterConstraintKind> GetTypeParameterConstraintKinds()
            => ImmutableArray<TypeParameterConstraintKind>.Empty;

        public sealed override TypeWithAnnotations ReturnTypeWithAnnotations
        {
            get
            {
                return _returnType;
            }
        }

        public sealed override bool IsImplicitlyDeclared
        {
            get
            {
                return true;
            }
        }

        internal override bool GenerateDebugInfo
        {
            get { return false; }
        }

        protected sealed override IAttributeTargetSymbol AttributeOwner
        {
            get
            {
                return (SourceNamedTypeSymbol)ContainingSymbol;
            }
        }

        internal sealed override System.Reflection.MethodImplAttributes ImplementationAttributes
        {
            get { return System.Reflection.MethodImplAttributes.Runtime; }
        }

        internal sealed override OneOrMany<SyntaxList<AttributeListSyntax>> GetAttributeDeclarations()
        {
            // TODO: This implementation looks strange. It might make sense for the Invoke method, but
            //       not for constructor and other methods.
            return OneOrMany.Create(((SourceNamedTypeSymbol)ContainingSymbol).GetAttributeDeclarations());
        }

        internal sealed override System.AttributeTargets GetAttributeTarget()
        {
            return System.AttributeTargets.Delegate;
        }

        private sealed class Constructor : SourceDelegateMethodSymbol
        {
            internal Constructor(
                SourceMemberContainerTypeSymbol delegateType,
                TypeWithAnnotations voidType,
                TypeWithAnnotations objectType,
                TypeWithAnnotations intPtrType,
                DelegateDeclarationSyntax syntax)
                : base(delegateType, voidType, syntax, MethodKind.Constructor, RefKind.None, DeclarationModifiers.Public)
            {
                InitializeParameters(ImmutableArray.Create<ParameterSymbol>(
                    SynthesizedParameterSymbol.Create(this, objectType, 0, RefKind.None, "object"),
                    SynthesizedParameterSymbol.Create(this, intPtrType, 1, RefKind.None, "method")));
            }

            public override string Name
            {
                get { return WellKnownMemberNames.InstanceConstructorName; }
            }

            internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetReturnTypeAttributeDeclarations()
            {
                // Constructors don't have return type attributes
                return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
            }

            internal override LexicalSortKey GetLexicalSortKey()
            {
                // associate "Invoke and .ctor" with whole delegate declaration for the sorting purposes
                // other methods will be associated with delegate's identifier
                // we want this just to keep the order of synthesized methods the same as in Dev12
                // Dev12 order is not strictly alphabetical - .ctor and Invoke go before other members.
                // there are no real reasons for emitting the members in one order or another, 
                // so we will keep them the same.
                return new LexicalSortKey(this.syntaxReferenceOpt.GetLocation(), this.DeclaringCompilation);
            }

            protected override bool HasSetsRequiredMembersImpl => false;
        }

        // Note that `in`/`ref readonly` parameters currently don't have `modreq(In)`
        // even though the `Invoke` method is virtual. https://github.com/dotnet/roslyn/issues/69079
        private sealed class InvokeMethod : SourceDelegateMethodSymbol
        {
            private readonly ImmutableArray<CustomModifier> _refCustomModifiers;

            internal InvokeMethod(
                SourceMemberContainerTypeSymbol delegateType,
                RefKind refKind,
                TypeWithAnnotations returnType,
                DelegateDeclarationSyntax syntax,
                Binder binder,
                BindingDiagnosticBag diagnostics)
                : base(delegateType, returnType, syntax, MethodKind.DelegateInvoke, refKind, DeclarationModifiers.Virtual | DeclarationModifiers.Public)
            {
                SyntaxToken arglistToken;
                var parameters = ParameterHelpers.MakeParameters(
                    binder, this, syntax.ParameterList, out arglistToken,
                    allowRefOrOut: true,
                    allowThis: false,
                    addRefReadOnlyModifier: true,
                    diagnostics: diagnostics);

                if (arglistToken.Kind() == SyntaxKind.ArgListKeyword)
                {
                    // This is a parse-time error in the native compiler; it is a semantic analysis error in Roslyn.

                    // error CS1669: __arglist is not valid in this context
                    diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, new SourceLocation(arglistToken));
                }

                if (this.RefKind == RefKind.RefReadOnly)
                {
                    var modifierType = binder.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_InAttribute, diagnostics, syntax.ReturnType);
                    _refCustomModifiers = ImmutableArray.Create(CSharpCustomModifier.CreateRequired(modifierType));
                }
                else
                {
                    _refCustomModifiers = ImmutableArray<CustomModifier>.Empty;
                }

                InitializeParameters(parameters.Cast<SourceParameterSymbol, ParameterSymbol>());
            }

            public override string Name
            {
                get { return WellKnownMemberNames.DelegateInvokeName; }
            }

            internal override LexicalSortKey GetLexicalSortKey()
            {
                // associate "Invoke and .ctor" with whole delegate declaration for the sorting purposes
                // other methods will be associated with delegate's identifier
                // we want this just to keep the order of synthesized methods the same as in Dev12
                // Dev12 order is not strictly alphabetical - .ctor and Invoke go before other members.
                // there are no real reasons for emitting the members in one order or another, 
                // so we will keep them the same.
                return new LexicalSortKey(this.syntaxReferenceOpt.GetLocation(), this.DeclaringCompilation);
            }

            internal override void AfterAddingTypeMembersChecks(ConversionsBase conversions, BindingDiagnosticBag diagnostics)
            {
                var syntax = (DelegateDeclarationSyntax)SyntaxRef.GetSyntax();
                var location = syntax.ReturnType.GetLocation();
                var compilation = DeclaringCompilation;

                Debug.Assert(location != null);

                base.AfterAddingTypeMembersChecks(conversions, diagnostics);

                if (this.RefKind == RefKind.RefReadOnly)
                {
                    compilation.EnsureIsReadOnlyAttributeExists(diagnostics, location, modifyCompilation: true);
                }

                ParameterHelpers.EnsureRefKindAttributesExist(compilation, Parameters, diagnostics, modifyCompilation: true);

                if (compilation.ShouldEmitNativeIntegerAttributes(ReturnType))
                {
                    compilation.EnsureNativeIntegerAttributeExists(diagnostics, location, modifyCompilation: true);
                }

                ParameterHelpers.EnsureNativeIntegerAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);
                ParameterHelpers.EnsureScopedRefAttributeExists(compilation, Parameters, diagnostics, modifyCompilation: true);

                if (compilation.ShouldEmitNullableAttributes(this) &&
                    ReturnTypeWithAnnotations.NeedsNullableAttribute())
                {
                    compilation.EnsureNullableAttributeExists(diagnostics, location, modifyCompilation: true);
                }

                ParameterHelpers.EnsureNullableAttributeExists(compilation, this, Parameters, diagnostics, modifyCompilation: true);
            }

            public override ImmutableArray<CustomModifier> RefCustomModifiers => _refCustomModifiers;
        }

        private sealed class BeginInvokeMethod : SourceDelegateMethodSymbol
        {
            internal BeginInvokeMethod(
                InvokeMethod invoke,
                TypeWithAnnotations iAsyncResultType,
                TypeWithAnnotations objectType,
                TypeWithAnnotations asyncCallbackType,
                DelegateDeclarationSyntax syntax)
                : base((SourceNamedTypeSymbol)invoke.ContainingType, iAsyncResultType, syntax, MethodKind.Ordinary, RefKind.None, DeclarationModifiers.Virtual | DeclarationModifiers.Public)
            {
                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                foreach (SourceParameterSymbol p in invoke.Parameters)
                {
                    var synthesizedParam = new SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(originalParam: p, newOwner: this, newOrdinal: p.Ordinal);
                    parameters.Add(synthesizedParam);
                }

                int paramCount = invoke.ParameterCount;
                parameters.Add(SynthesizedParameterSymbol.Create(this, asyncCallbackType, paramCount, RefKind.None, GetUniqueParameterName(parameters, "callback")));
                parameters.Add(SynthesizedParameterSymbol.Create(this, objectType, paramCount + 1, RefKind.None, GetUniqueParameterName(parameters, "object")));

                InitializeParameters(parameters.ToImmutableAndFree());
            }

            public override string Name
            {
                get { return WellKnownMemberNames.DelegateBeginInvokeName; }
            }

            internal override OneOrMany<SyntaxList<AttributeListSyntax>> GetReturnTypeAttributeDeclarations()
            {
                // BeginInvoke method doesn't have return type attributes
                // because it doesn't inherit Delegate declaration's return type.
                // It has a special return type: SpecialType.System.IAsyncResult.
                return OneOrMany.Create(default(SyntaxList<AttributeListSyntax>));
            }
        }

        private sealed class EndInvokeMethod : SourceDelegateMethodSymbol
        {
            private readonly InvokeMethod _invoke;

            internal EndInvokeMethod(
                InvokeMethod invoke,
                TypeWithAnnotations iAsyncResultType,
                DelegateDeclarationSyntax syntax)
                : base((SourceNamedTypeSymbol)invoke.ContainingType, invoke.ReturnTypeWithAnnotations, syntax, MethodKind.Ordinary, invoke.RefKind, DeclarationModifiers.Virtual | DeclarationModifiers.Public)
            {
                _invoke = invoke;

                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                int ordinal = 0;

                foreach (SourceParameterSymbol p in invoke.Parameters)
                {
                    if (p.RefKind != RefKind.None)
                    {
                        var synthesizedParam = new SourceDelegateClonedParameterSymbolForBeginAndEndInvoke(originalParam: p, newOwner: this, newOrdinal: ordinal++);
                        parameters.Add(synthesizedParam);
                    }
                }

                parameters.Add(SynthesizedParameterSymbol.Create(this, iAsyncResultType, ordinal++, RefKind.None, GetUniqueParameterName(parameters, "result")));
                InitializeParameters(parameters.ToImmutableAndFree());
            }

            protected override SourceMemberMethodSymbol BoundAttributesSource => _invoke;

            public override string Name => WellKnownMemberNames.DelegateEndInvokeName;

            public override ImmutableArray<CustomModifier> RefCustomModifiers => _invoke.RefCustomModifiers;
        }

        private static string GetUniqueParameterName(ArrayBuilder<ParameterSymbol> currentParameters, string name)
        {
            while (!IsUnique(currentParameters, name))
            {
                name = "__" + name;
            }

            return name;
        }

        private static bool IsUnique(ArrayBuilder<ParameterSymbol> currentParameters, string name)
        {
            foreach (var p in currentParameters)
            {
                if (string.CompareOrdinal(p.Name, name) == 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
