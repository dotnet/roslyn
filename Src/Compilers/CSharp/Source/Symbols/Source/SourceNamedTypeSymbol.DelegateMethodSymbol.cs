using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    partial class SourceNamedTypeSymbol
    {
        private abstract class DelegateMethodSymbol : SourceMethodSymbol
        {
            private readonly ReadOnlyArray<ParameterSymbol> parameters;
            private readonly TypeSymbol returnType;
            private readonly bool isVararg;

            protected DelegateMethodSymbol(
                SourceNamedTypeSymbol containingType,
                string name,
                DelegateDeclarationSyntax syntax,
                MethodKind methodKind,
                DeclarationModifiers declarationModifiers,
                Binder binder,
                DiagnosticBag diagnostics,
                CancellationToken cancellationToken)
                : base(containingType, name, binder.GetSyntaxReference(syntax), blockSyntax: null, location: binder.Location(syntax.Identifier))
            {
                var location = this.locations[0];

                bool isExtensionMethod;
                this.parameters = MakeParameters(binder, syntax, out isExtensionMethod, out this.isVararg, diagnostics, cancellationToken);
                this.returnType = MakeReturnType(binder, syntax, diagnostics);
                this.flags = MakeFlags(methodKind, declarationModifiers, IsVoidType(this.returnType), isExtensionMethod: isExtensionMethod);

                var info = ModifierUtils.CheckAccessibility(this.DeclarationModifiers);
                if (info != null)
                {
                    diagnostics.Add(info, location);
                }
            }

            protected abstract ReadOnlyArray<ParameterSymbol> MakeParameters(Binder binder, DelegateDeclarationSyntax syntax, out bool isExtensionMethod, out bool isVararg, DiagnosticBag diagnostics, CancellationToken cancellationToken);
            protected abstract TypeSymbol MakeReturnType(Binder binder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics);

            public override bool IsVararg
            {
                get
                {
                    return this.isVararg;
                }
            }

            public override ReadOnlyArray<ParameterSymbol> Parameters
            {
                get
                {
                    return this.parameters;
                }
            }

            public override TypeSymbol ReturnType
            {
                get
                {
                    return this.returnType;
                }
            }

            public override bool IsSynthesized
            {
                get
                {
                    return true;
                }
            }
        }
    }
}