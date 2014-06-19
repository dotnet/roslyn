using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    partial class SourceNamedTypeSymbol
    {
        private sealed class DelegateInvokeMethodImplementation : DelegateMethodSymbol
        {
            internal DelegateInvokeMethodImplementation(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                Binder binder,
                DiagnosticBag diagnostics,
                CancellationToken cancellationToken)
                : base(delegateType, CommonMemberNames.DelegateInvokeName, syntax, MethodKind.DelegateInvoke, DeclarationModifiers.Virtual | DeclarationModifiers.Public, binder, diagnostics, cancellationToken)
            {
            }

            protected override ReadOnlyArray<ParameterSymbol> MakeParameters(
                Binder binder, DelegateDeclarationSyntax syntax,
                out bool isExtensionMethod,
                out bool isVararg,
                DiagnosticBag diagnostics,
                CancellationToken cancellationToken)
            {
                return MakeParameters(binder, syntax.ParameterList, out isExtensionMethod, out isVararg, diagnostics, cancellationToken);
            }

            protected override TypeSymbol MakeReturnType(Binder bodyBinder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                return bodyBinder.BindType(syntax.ReturnType, diagnostics);
            }
        }
    }
}