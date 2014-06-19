'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend Class SourceNamedTypeSymbol

        ''' <summary>
        ''' Instances of this class represent a synthesized EndInvoke method
        ''' </summary>
        Friend NotInheritable Class DelegateEndInvokeMethod
            Inherits DelegateMethodSymbol

            ''' <summary>
            ''' Initializes a new instance of the <see cref="DelegateEndInvokeMethod" /> class.
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

                MyBase.New(delegateType, CommonMemberNames.DelegateEndInvokeName, syntax, DelegateMethodSymbol.DelegateCommonMethodFlags, binder, diagnostics, cancellationToken)
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
                    If parameter.IsByRef Then
                        parameters.Add(New SynthesizedParameterSymbol(Me, parameter.Type, parameters.Count, parameter.IsByRef(), parameter.Name))
                    End If
                Next

                parameters.Add(New SynthesizedParameterSymbol(Me, binder.Compilation.GetWellKnownType(WellKnownType.System_IAsyncResult), parameters.Count, False, "DelegateAsyncResult"))
                Return parameters.ToReadOnlyAndFree()
            End Function

            ''' <summary>
            ''' Makes the type of the return.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
            Protected Overrides Function MakeReturnType(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag) As TypeSymbol
                If syntax.Kind = SyntaxKind.DelegateSubStatement Then
                    Return binder.GetSpecialType(Compilers.SpecialType.System_Void, syntax, diagnostics)
                End If

                Return binder.BindTypeSyntax(syntax.AsClauseOpt.Type, Nothing)
            End Function

        End Class
    End Class
End Namespace
