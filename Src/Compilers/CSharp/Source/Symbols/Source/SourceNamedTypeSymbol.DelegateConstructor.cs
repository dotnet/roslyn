using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    partial class SourceNamedTypeSymbol
    {
        private sealed class DelegateConstructor : DelegateMethodSymbol
        {
            internal DelegateConstructor(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                Binder binder,
                CancellationToken cancellationToken)
                : base(delegateType, CommonMemberNames.InstanceConstructorName, syntax, MethodKind.Constructor, DeclarationModifiers.Public, binder, diagnostics: null, cancellationToken: cancellationToken)
            {
            }

            protected override ReadOnlyArray<ParameterSymbol> MakeParameters(Binder binder, DelegateDeclarationSyntax syntax, out bool isExtensionMethod, out bool isVararg, DiagnosticBag diagnostics, CancellationToken cancellationToken)
            {
                isExtensionMethod = false;
                isVararg = false;
                return ReadOnlyArray<ParameterSymbol>.CreateFrom(
                    new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_Object, diagnostics, syntax), 0, RefKind.None, "object"),
                    new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_IntPtr, diagnostics, syntax), 1, RefKind.None, "method"));
            }

            protected override TypeSymbol MakeReturnType(Binder binder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                return GetVoidType(binder, syntax, diagnostics);
            }
        }
    }
}