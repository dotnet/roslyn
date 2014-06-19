'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend Class SourceNamedTypeSymbol

        ''' <summary>
        ''' Instances of this class represent a synthesized BeginInvoke method
        ''' </summary>
        Friend NotInheritable Class DelegateBeginInvokeMethod
            Inherits DelegateMethodSymbol

            ''' <summary>
            ''' Initializes a new instance of the <see cref="DelegateBeginInvokeMethod" /> class.
            ''' </summary>
            ''' <param name="delegateType">Type of the delegate.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="binder">The binder.</param>
            ''' <param name="diagnostics">The diagnostics.</param>
            ''' <param name="cancellationToken">The cancellation token.</param>
            Friend Sub New(delegateType As SourceNamedTypeSymbol,
                syntax As DelegateStatementSyntax,
                binder As Binder,
                diagnostics As DiagnosticBag,
                cancellationToken As CancellationToken)

                MyBase.New(delegateType, CommonMemberNames.DelegateBeginInvokeName, syntax, DelegateMethodSymbol.DelegateCommonMethodFlags, binder, diagnostics, cancellationToken)
            End Sub

            ''' <summary>
            ''' Makes the parameters for the delegate method.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param>
            ''' <param name="cancellationToken">The cancellation token.</param><returns></returns>
            Protected Overrides Function MakeParameters(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag, cancellationToken As System.Threading.CancellationToken) As ReadOnlyArray(Of ParameterSymbol)

                Dim delegateBinder = CType(binder, DelegateBinder)
                Dim parameters = ArrayBuilder(Of ParameterSymbol).GetInstance()

                For Each parameter In delegateBinder.invoke.Parameters
                    cancellationToken.ThrowIfCancellationRequested()
                    parameters.Add(New SynthesizedParameterSymbol(Me, parameter.Type, parameter.Ordinal, parameter.IsByRef(), parameter.Name))
                Next

                parameters.Add(New SynthesizedParameterSymbol(Me, binder.Compilation.GetSpecialType(Compilers.SpecialType.System_AsyncCallback), delegateBinder.invoke.Parameters.Count, False, "DelegateCallback"))
                parameters.Add(New SynthesizedParameterSymbol(Me, binder.GetSpecialType(Compilers.SpecialType.System_Object, syntax, diagnostics), delegateBinder.invoke.Parameters.Count + 1, False, "DelegateAsyncState"))
                Return parameters.ToReadOnlyAndFree()
            End Function

            ''' <summary>
            ''' Makes the type of the return.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
            Protected Overrides Function MakeReturnType(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag) As TypeSymbol
                Return binder.Compilation.GetWellKnownType(WellKnownType.System_IAsyncResult)
            End Function

        End Class
    End Class
End Namespace
