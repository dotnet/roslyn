'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend Class SourceNamedTypeSymbol

        ''' <summary>
        ''' Instances of this class represent a synthesized Invoke method
        ''' </summary>
        Friend NotInheritable Class DelegateInvokeMethodImplementation
            Inherits DelegateMethodSymbol

            ''' <summary>
            ''' Initializes a new instance of the <see cref="DelegateInvokeMethodImplementation" /> class.
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

                MyBase.New(delegateType, CommonMemberNames.DelegateInvokeName, syntax, DelegateMethodSymbol.DelegateCommonMethodFlags, binder, diagnostics, cancellationToken)
            End Sub

            ''' <summary>
            ''' Makes the parameters for the delegate method.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param>
            ''' <param name="cancellationToken">The cancellation token.</param><returns></returns>
            Protected Overrides Function MakeParameters(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag, cancellationToken As System.Threading.CancellationToken) As ReadOnlyArray(Of ParameterSymbol)
                Return binder.DecodeParameterList(Me, False, syntax.ParameterListOpt, diagnostics, cancellationToken)
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

            ''' <summary>
            ''' Gets the kind of the method.
            ''' </summary>
            ''' <value>
            ''' The kind of the method.
            ''' </value>
            Public Overrides ReadOnly Property MethodKind As MethodKind
                Get
                    Return MethodKind.DelegateInvoke
                End Get
            End Property
        End Class
    End Class
End Namespace
