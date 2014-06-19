using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    partial class SourceNamedTypeSymbol
    {
        private sealed class DelegateBeginInvokeMethod : DelegateMethodSymbol
        {
            internal DelegateBeginInvokeMethod(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                DelegateBinder binder,
                DiagnosticBag diagnostics,
                CancellationToken cancellationToken)
                : base(delegateType, CommonMemberNames.DelegateBeginInvokeName, syntax, MethodKind.Ordinary, DeclarationModifiers.Virtual | DeclarationModifiers.Public, binder, diagnostics, cancellationToken)
            {
            }

            protected override TypeSymbol MakeReturnType(Binder bodyBinder, DelegateDeclarationSyntax syntax, DiagnosticBag diagnostics)
            {
                return bodyBinder.GetSpecialType(SpecialType.System_IAsyncResult, diagnostics, syntax);
            }

            protected override ReadOnlyArray<ParameterSymbol> MakeParameters(
                Binder binder,
                DelegateDeclarationSyntax syntax,
                out bool isExtensionMethod,
                out bool isVararg,
                DiagnosticBag diagnostics,
                CancellationToken cancellationToken)
            {
                isExtensionMethod = false;
                isVararg = false;
                var delegateBinder = binder as DelegateBinder;
                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                foreach (var p in delegateBinder.invoke.Parameters)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    parameters.Add(new SynthesizedParameterSymbol(this, p.Type, p.Ordinal, p.RefKind, p.Name));
                }

                parameters.Add(new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_AsyncCallback, diagnostics, syntax), delegateBinder.invoke.Parameters.Count, RefKind.None, "callback"));
                parameters.Add(new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_Object, diagnostics, syntax), delegateBinder.invoke.Parameters.Count + 1, RefKind.None, "object"));
                return parameters.ToReadOnlyAndFree();
            }
        }
    }
}