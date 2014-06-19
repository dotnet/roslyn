using System.Diagnostics;
using System.Threading;
using Roslyn.Compilers.Common;
using Roslyn.Utilities;

namespace Roslyn.Compilers.CSharp
{
    internal partial class SourceNamedTypeSymbol
    {
        private void AddDelegateMembers(
            ArrayBuilder<Symbol> symbols,
            DelegateDeclarationSyntax syntax,
            BinderFactory binderFactory,
            DiagnosticBag diagnostics)
        {
            var bodyBinder = binderFactory.GetBinder(syntax.ParameterList);

            // A delegate has the following members: (see CLI spec 13.6)
            // (1) a method named Invoke with the specified signature
            var invoke = new DelegateInvokeMethodImplementation(this, syntax, bodyBinder, diagnostics);
            invoke.CheckMethodVarianceSafety(diagnostics);
            symbols.Add(invoke);

            // (2) a constructor with argument types (object, System.IntPtr)
            symbols.Add(new DelegateConstructor(this, syntax, bodyBinder));

            var delegateBinder = new DelegateBinder(bodyBinder, this, invoke);

            // (3) BeginInvoke
            symbols.Add(new DelegateBeginInvokeMethod(this, syntax, delegateBinder, diagnostics));

            // and (4) EndInvoke methods
            symbols.Add(new DelegateEndInvokeMethod(this, syntax, delegateBinder, diagnostics));

            if (this.DeclaredAccessibility <= Accessibility.Private)
            {
                return;
            }

            if (!this.IsNoMoreVisibleThan(invoke.ReturnType))
            {
                // Inconsistent accessibility: return type '{1}' is less accessible than delegate '{0}'
                diagnostics.Add(ErrorCode.ERR_BadVisDelegateReturn, Locations[0], this, invoke.ReturnType);
            }

            foreach (var parameter in invoke.Parameters)
            {
                if (!parameter.Type.IsAtLeastAsVisibleAs(this))
                {
                    // Inconsistent accessibility: parameter type '{1}' is less accessible than delegate '{0}'
                    diagnostics.Add(ErrorCode.ERR_BadVisDelegateParam, Locations[0], this, parameter.Type);
                }
            }

        }

        private sealed class DelegateBinder : Binder
        {
            internal readonly SourceNamedTypeSymbol delegateType;
            internal readonly DelegateInvokeMethodImplementation invoke;

            internal DelegateBinder(Binder bodyBinder, SourceNamedTypeSymbol delegateType, DelegateInvokeMethodImplementation invoke)
                : base(bodyBinder)
            {
                this.delegateType = delegateType;
                this.invoke = invoke;
            }
        }

        private abstract class DelegateMethodSymbol : SourceMethodSymbol
        {
            private readonly ReadOnlyArray<ParameterSymbol> parameters;
            private readonly TypeSymbol returnType;

            protected DelegateMethodSymbol(
                SourceNamedTypeSymbol containingType,
                DelegateDeclarationSyntax syntax,
                MethodKind methodKind,
                DeclarationModifiers declarationModifiers,
                Binder binder,
                DiagnosticBag diagnostics)
                : base(containingType, binder.GetSyntaxReference(syntax), blockSyntaxReference: null, location: binder.Location(syntax.Identifier))
            {
                this.parameters = MakeParameters(binder, syntax, diagnostics);
                this.returnType = MakeReturnType(binder, syntax, diagnostics);
                this.flags = MakeFlags(methodKind, declarationModifiers, this.returnType.SpecialType == SpecialType.System_Void, isExtensionMethod: false);

                var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers);
                if (info != null)
                {
                    diagnostics.Add(info, this.locations[0]);
                }
            }

            protected override void MethodChecks(DiagnosticBag diagnostics)
            {
                // TODO: move more functionality into here, making these symbols more lazy
            }

            protected abstract ReadOnlyArray<ParameterSymbol> MakeParameters(Binder binder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics);
            protected abstract TypeSymbol MakeReturnType(Binder binder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics);

            public override bool IsVararg
            {
                get
                {
                    return false;
                }
            }

            public override ReadOnlyArray<ParameterSymbol> Parameters
            {
                get
                {
                    return this.parameters;
                }
            }

            public override ReadOnlyArray<TypeParameterSymbol> TypeParameters
            {
                get
                { 
                    return ReadOnlyArray<TypeParameterSymbol>.Empty;
                }
            }

            public override TypeSymbol ReturnType
            {
                get
                {
                    return this.returnType;
                }
            }

            public override bool IsImplicitlyDeclared
            {
                get
                {
                    return true;
                }
            }

            protected override IAttributeTargetSymbol AttributeOwner
            {
                get
                {
                    return (SourceNamedTypeSymbol)ContainingSymbol;
                }
            }

            internal override System.Reflection.MethodImplAttributes ImplementationAttributes
            {
                get { return System.Reflection.MethodImplAttributes.Runtime; }
            }

            internal override OneOrMany<SyntaxList<AttributeDeclarationSyntax>> GetAttributeDeclarations()
            {
                return OneOrMany.Create(((DelegateDeclarationSyntax)SyntaxNode).Attributes);
            }

            internal override System.AttributeTargets GetAttributeTarget()
            {
                return System.AttributeTargets.Delegate;
            }
        }

        private sealed class DelegateInvokeMethodImplementation : DelegateMethodSymbol
        {
            internal DelegateInvokeMethodImplementation(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                Binder binder,
                DiagnosticBag diagnostics)
                : base(delegateType, syntax, MethodKind.DelegateInvoke, DeclarationModifiers.Virtual | DeclarationModifiers.Public, binder, diagnostics)
            {
            }

            protected override ReadOnlyArray<ParameterSymbol> MakeParameters(
                Binder binder,
                DelegateDeclarationSyntax syntax,
                DiagnosticBag diagnostics)
            {
                SyntaxToken extensionMethodThis;
                SyntaxToken arglistToken;
                var parameters = ParameterHelpers.MakeParameters(binder, this, syntax.ParameterList, true, out extensionMethodThis, out arglistToken, diagnostics);
                if (arglistToken.Kind == SyntaxKind.ArgListKeyword)
                {
                    // This is a parse-time error in the native compiler; it is a semantic analysis error in Roslyn.

                    // error CS1669: __arglist is not valid in this context
                    diagnostics.Add(ErrorCode.ERR_IllegalVarArgs, new SourceLocation(arglistToken));
                }
        
                return parameters;
            }

            protected override TypeSymbol MakeReturnType(Binder bodyBinder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                TypeSymbol returnType = bodyBinder.BindType(syntax.ReturnType, diagnostics);

                if (returnType.IsRestrictedType())
                {
                    // Method or delegate cannot return type '{0}'
                    diagnostics.Add(ErrorCode.ERR_MethodReturnCantBeRefAny, syntax.ReturnType.Location, returnType);
                }

                return returnType;
            }

            public override string Name
            {
                get { return CommonMemberNames.DelegateInvokeName; }
            }
        }

        private sealed class DelegateBeginInvokeMethod : DelegateMethodSymbol
        {
            internal DelegateBeginInvokeMethod(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                DelegateBinder binder,
                DiagnosticBag diagnostics)
                : base(delegateType, syntax, MethodKind.Ordinary, DeclarationModifiers.Virtual | DeclarationModifiers.Public, binder, diagnostics)
            {
            }

            protected override TypeSymbol MakeReturnType(Binder bodyBinder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                return bodyBinder.GetSpecialType(SpecialType.System_IAsyncResult, diagnostics, syntax);
            }

            protected override ReadOnlyArray<ParameterSymbol> MakeParameters(
                Binder binder,
                DelegateDeclarationSyntax syntax,
                DiagnosticBag diagnostics)
            {
                var delegateBinder = binder as DelegateBinder;
                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                foreach (SourceParameterSymbol p in delegateBinder.invoke.Parameters)
                {
                    var synthesizedParam = new SourceClonedParameterSymbol(originalParam: p, newOwner: this, newOrdinal: p.Ordinal, suppressOptional: true);
                    parameters.Add(synthesizedParam);
                }

                int paramCount = delegateBinder.invoke.Parameters.Count;
                parameters.Add(new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_AsyncCallback, diagnostics, syntax), paramCount, RefKind.None, "callback"));
                parameters.Add(new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_Object, diagnostics, syntax), paramCount + 1, RefKind.None, "object"));

                return parameters.ToReadOnlyAndFree();
            }

            public override string Name
            {
                get { return CommonMemberNames.DelegateBeginInvokeName; }
            }

            internal override OneOrMany<SyntaxList<AttributeDeclarationSyntax>> GetReturnTypeAttributeDeclarations()
            {
                // BeginInvoke method doesn't have return type attributes
                // because it doesn't inherit Delegate declaration's return type.
                // It has a special return type: SpecialType.System.IAsyncResult.
                return OneOrMany.Create(default(SyntaxList<AttributeDeclarationSyntax>));
            }
        }

        private sealed class DelegateEndInvokeMethod : DelegateMethodSymbol
        {
            internal DelegateEndInvokeMethod(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                DelegateBinder binder,
                DiagnosticBag diagnostics)
                : base(delegateType, syntax, MethodKind.Ordinary, DeclarationModifiers.Virtual | DeclarationModifiers.Public, binder, diagnostics)
            {
            }

            protected override TypeSymbol MakeReturnType(Binder bodyBinder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                var delegateBinder = bodyBinder as DelegateBinder;
                return delegateBinder.invoke.ReturnType;
            }

            protected override ReadOnlyArray<ParameterSymbol> MakeParameters(
                Binder binder,
                DelegateDeclarationSyntax syntax,
                DiagnosticBag diagnostics)
            {
                var delegateBinder = binder as DelegateBinder;
                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                int ordinal = 0;
                foreach (SourceParameterSymbol p in delegateBinder.invoke.Parameters)
                {
                    if (p.RefKind != RefKind.None)
                    {
                        var synthesizedParam = new SourceClonedParameterSymbol(originalParam: p, newOwner: this, newOrdinal: ordinal++, suppressOptional: true);
                        parameters.Add(synthesizedParam);
                    }
                }

                parameters.Add(new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_IAsyncResult, diagnostics, syntax), ordinal++, RefKind.None, "result"));
                return parameters.ToReadOnlyAndFree();
            }

            public override string Name
            {
                get { return CommonMemberNames.DelegateEndInvokeName; }
            }
        }

        private sealed class DelegateConstructor : DelegateMethodSymbol
        {
            internal DelegateConstructor(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                Binder binder)
                : base(delegateType, syntax, MethodKind.Constructor, DeclarationModifiers.Public, binder, diagnostics: null)
            {
            }

            protected override ReadOnlyArray<ParameterSymbol> MakeParameters(Binder binder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                return ReadOnlyArray<ParameterSymbol>.CreateFrom(
                    new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_Object, diagnostics, syntax), 0, RefKind.None, "object"),
                    new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_IntPtr, diagnostics, syntax), 1, RefKind.None, "method"));
            }

            protected override TypeSymbol MakeReturnType(Binder binder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                return binder.GetSpecialType(SpecialType.System_Void, diagnostics, syntax);
            }

            public override string Name
            {
                get { return CommonMemberNames.InstanceConstructorName; }
            }

            internal override OneOrMany<SyntaxList<AttributeDeclarationSyntax>> GetReturnTypeAttributeDeclarations()
            {
                // Constructors don't have return type attributes
                return OneOrMany.Create(default(SyntaxList<AttributeDeclarationSyntax>));
            }
        }
    }
}
