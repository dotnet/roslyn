// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Generic;
using System;

namespace Microsoft.CodeAnalysis.CSharp.Symbols
{
    internal abstract class SourceDelegateMethodSymbol : SourceMethodSymbol
    {
        private ImmutableArray<ParameterSymbol> _parameters;
        private readonly TypeSymbol _returnType;

        protected SourceDelegateMethodSymbol(
            SourceMemberContainerTypeSymbol delegateType,
            TypeSymbol returnType,
            DelegateDeclarationSyntax syntax,
            MethodKind methodKind,
            DeclarationModifiers declarationModifiers)
            : base(delegateType, syntax.GetReference(), bodySyntaxReferenceOpt: null, location: syntax.Identifier.GetLocation())
        {
            _returnType = returnType;
            this.MakeFlags(methodKind, declarationModifiers, _returnType.SpecialType == SpecialType.System_Void, isExtensionMethod: false);
        }

        protected void InitializeParameters(ImmutableArray<ParameterSymbol> parameters)
        {
            Debug.Assert(_parameters.IsDefault);
            _parameters = parameters;
        }

        internal static void AddDelegateMembers(
            SourceMemberContainerTypeSymbol delegateType,
            ArrayBuilder<Symbol> symbols,
            DelegateDeclarationSyntax syntax,
            DiagnosticBag diagnostics)
        {
            Binder binder = delegateType.GetBinder(syntax.ParameterList);
            TypeSymbol returnType = binder.BindType(syntax.ReturnType, diagnostics);

            // reuse types to avoid reporting duplicate errors if missing:
            var voidType = binder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax);
            var objectType = binder.GetSpecialType(SpecialType.System_Object, diagnostics, syntax);
            var intPtrType = binder.GetSpecialType(SpecialType.System_IntPtr, diagnostics, syntax);

            if (returnType.IsRestrictedType())
            {
                // Method or delegate cannot return type '{0}'
                diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, syntax.ReturnType.Location, returnType);
            }

            // A delegate has the following members: (see CLI spec 13.6)
            // (1) a method named Invoke with the specified signature
            var invoke = new InvokeMethod(delegateType, returnType, syntax, binder, diagnostics);
            invoke.CheckDelegateVarianceSafety(diagnostics);
            symbols.Add(invoke);

            // (2) a constructor with argument types (object, System.IntPtr)
            symbols.Add(new Constructor(delegateType, voidType, objectType, intPtrType, syntax));

            if (binder.Compilation.GetSpecialType(SpecialType.System_IAsyncResult).TypeKind != TypeKind.Error &&
                binder.Compilation.GetSpecialType(SpecialType.System_AsyncCallback).TypeKind != TypeKind.Error &&
                // WinRT delegates don't have Begin/EndInvoke methods
                !delegateType.IsCompilationOutputWinMdObj())
            {
                var iAsyncResultType = binder.GetSpecialType(SpecialType.System_IAsyncResult, diagnostics, syntax);
                var asyncCallbackType = binder.GetSpecialType(SpecialType.System_AsyncCallback, diagnostics, syntax);

                // (3) BeginInvoke
                symbols.Add(new BeginInvokeMethod(invoke, iAsyncResultType, objectType, asyncCallbackType, syntax));

                // and (4) EndInvoke methods
                symbols.Add(new EndInvokeMethod(invoke, iAsyncResultType, syntax));
            }

            if (delegateType.DeclaredAccessibility <= Accessibility.Private)
            {
                return;
            }

            HashSet<DiagnosticInfo> useSiteDiagnostics = null;

            if (!delegateType.IsNoMoreVisibleThan(invoke.ReturnType, ref useSiteDiagnostics))
            {
                // Inconsistent accessibility: return type '{1}' is less accessible than delegate '{0}'
                diagnostics.Add(ErrorCode.ERR_BadVisDelegateReturn, delegateType.Locations[0], delegateType, invoke.ReturnType);
            }

            foreach (var parameter in invoke.Parameters)
            {
                if (!parameter.Type.IsAtLeastAsVisibleAs(delegateType, ref useSiteDiagnostics))
                {
                    // Inconsistent accessibility: parameter type '{1}' is less accessible than delegate '{0}'
                    diagnostics.Add(ErrorCode.ERR_BadVisDelegateParam, delegateType.Locations[0], delegateType, parameter.Type);
                }
            }

            diagnostics.Add(delegateType.Locations[0], useSiteDiagnostics);
        }

        protected override void MethodChecks(DiagnosticBag diagnostics)
        {
            // TODO: move more functionality into here, making these symbols more lazy
        }

        public sealed override bool IsVararg
        {
            get
            {
                return false;
            }
        }

        public sealed override ImmutableArray<ParameterSymbol> Parameters
        {
            get
            {
                return _parameters;
            }
        }

        public override ImmutableArray<TypeParameterSymbol> TypeParameters
        {
            get
            {
                return ImmutableArray<TypeParameterSymbol>.Empty;
            }
        }

        public sealed override TypeSymbol ReturnType
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

        internal override bool IsExpressionBodied
        {
            get { return false; }
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
                TypeSymbol voidType,
                TypeSymbol objectType,
                TypeSymbol intPtrType,
                DelegateDeclarationSyntax syntax)
                : base(delegateType, voidType, syntax, MethodKind.Constructor, DeclarationModifiers.Public)
            {
                InitializeParameters(ImmutableArray.Create<ParameterSymbol>(
                    new SynthesizedParameterSymbol(this, objectType, 0, RefKind.None, "object"),
                    new SynthesizedParameterSymbol(this, intPtrType, 1, RefKind.None, "method")));
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
        }

        private sealed class InvokeMethod : SourceDelegateMethodSymbol
        {
            internal InvokeMethod(
                SourceMemberContainerTypeSymbol delegateType,
                TypeSymbol returnType,
                DelegateDeclarationSyntax syntax,
                Binder binder,
                DiagnosticBag diagnostics)
                : base(delegateType, returnType, syntax, MethodKind.DelegateInvoke, DeclarationModifiers.Virtual | DeclarationModifiers.Public)
            {
                SyntaxToken arglistToken;
                var parameters = ParameterHelpers.MakeParameters(binder, this, syntax.ParameterList, true, out arglistToken, diagnostics, false);
                if (arglistToken.Kind() == SyntaxKind.ArgListKeyword)
                {
                    // This is a parse-time error in the native compiler; it is a semantic analysis error in Roslyn.

                    // error CS1669: __arglist is not valid in this context
                    diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, new SourceLocation(arglistToken));
                }

                InitializeParameters(parameters);
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
        }

        private sealed class BeginInvokeMethod : SourceDelegateMethodSymbol
        {
            internal BeginInvokeMethod(
                InvokeMethod invoke,
                TypeSymbol iAsyncResultType,
                TypeSymbol objectType,
                TypeSymbol asyncCallbackType,
                DelegateDeclarationSyntax syntax)
                : base((SourceNamedTypeSymbol)invoke.ContainingType, iAsyncResultType, syntax, MethodKind.Ordinary, DeclarationModifiers.Virtual | DeclarationModifiers.Public)
            {
                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                foreach (SourceParameterSymbol p in invoke.Parameters)
                {
                    var synthesizedParam = new SourceClonedParameterSymbol(originalParam: p, newOwner: this, newOrdinal: p.Ordinal, suppressOptional: true);
                    parameters.Add(synthesizedParam);
                }

                int paramCount = invoke.ParameterCount;
                parameters.Add(new SynthesizedParameterSymbol(this, asyncCallbackType, paramCount, RefKind.None, GetUniqueParameterName(parameters, "callback")));
                parameters.Add(new SynthesizedParameterSymbol(this, objectType, paramCount + 1, RefKind.None, GetUniqueParameterName(parameters, "object")));

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
            internal EndInvokeMethod(
                InvokeMethod invoke,
                TypeSymbol iAsyncResultType,
                DelegateDeclarationSyntax syntax)
                : base((SourceNamedTypeSymbol)invoke.ContainingType, invoke.ReturnType, syntax, MethodKind.Ordinary, DeclarationModifiers.Virtual | DeclarationModifiers.Public)
            {
                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                int ordinal = 0;

                foreach (SourceParameterSymbol p in invoke.Parameters)
                {
                    if (p.RefKind != RefKind.None)
                    {
                        var synthesizedParam = new SourceClonedParameterSymbol(originalParam: p, newOwner: this, newOrdinal: ordinal++, suppressOptional: true);
                        parameters.Add(synthesizedParam);
                    }
                }

                parameters.Add(new SynthesizedParameterSymbol(this, iAsyncResultType, ordinal++, RefKind.None, GetUniqueParameterName(parameters, "result")));
                InitializeParameters(parameters.ToImmutableAndFree());
            }

            protected override SourceMethodSymbol BoundAttributesSource
            {
                get
                {
                    // copy return attributes from InvokeMethod
                    return (SourceMethodSymbol)((SourceNamedTypeSymbol)this.ContainingSymbol).DelegateInvokeMethod;
                }
            }

            public override string Name
            {
                get { return WellKnownMemberNames.DelegateEndInvokeName; }
            }
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
