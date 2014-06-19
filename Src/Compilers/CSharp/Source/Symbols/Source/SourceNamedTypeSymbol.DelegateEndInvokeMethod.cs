using System.Threading;

namespace Roslyn.Compilers.CSharp
{
    partial class SourceNamedTypeSymbol
    {
        private sealed class DelegateEndInvokeMethod : DelegateMethodSymbol
        {
            internal DelegateEndInvokeMethod(
                SourceNamedTypeSymbol delegateType,
                DelegateDeclarationSyntax syntax,
                DelegateBinder binder,
                DiagnosticBag diagnostics,
                CancellationToken cancellationToken)
                : base(delegateType, CommonMemberNames.DelegateEndInvokeName, syntax, MethodKind.Ordinary, DeclarationModifiers.Virtual | DeclarationModifiers.Public, binder, diagnostics, cancellationToken)
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
                out bool isExtensionMethod,
                out bool isVararg,
                DiagnosticBag diagnostics,
                CancellationToken cancellationToken)
            {
                isExtensionMethod = false;
                isVararg = false;
                var delegateBinder = binder as DelegateBinder;
                var parameters = ArrayBuilder<ParameterSymbol>.GetInstance();
                int ordinal = 0;
                foreach (var p in delegateBinder.invoke.Parameters)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    if (p.RefKind != RefKind.None)
                    {
                        parameters.Add(new SynthesizedParameterSymbol(this, p.Type, ordinal++, p.RefKind, p.Name));
                    }
                }

                parameters.Add(new SynthesizedParameterSymbol(this, binder.GetSpecialType(SpecialType.System_IAsyncResult, diagnostics, syntax), ordinal++, RefKind.None, "result"));
                return parameters.ToReadOnlyAndFree();
            }
        }
    }
}