'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend Class SourceNamedTypeSymbol

        ''' <summary>
        ''' Instances of this class represent a synthesized delegate constructor
        ''' </summary>
        Friend NotInheritable Class DelegateConstructor
            Inherits DelegateMethodSymbol

            ''' <summary>
            ''' Initializes a new instance of the <see cref="DelegateConstructor" /> class.
            ''' </summary>
            ''' <param name="delegateType">Type of the delegate.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="binder">The binder.</param>
            ''' <param name="diagnostics">The diagnostics.</param>
            ''' <param name="cancellationToken">The cancellation token.</param>
            Friend Sub New(
                delegateType As SourceNamedTypeSymbol,
                syntax As DelegateStatementSyntax,
                binder As Binder,
                diagnostics As DiagnosticBag,
                cancellationToken As CancellationToken)

                MyBase.New(delegateType, CommonMemberNames.InstanceConstructorName, syntax, DelegateMethodSymbol.DelegateConstructorMethodFlags, binder, diagnostics, cancellationToken)
            End Sub

            ''' <summary>
            ''' Makes the parameters for the delegate method.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param>
            ''' <param name="cancellationToken">The cancellation token.</param><returns></returns>
            Protected Overrides Function MakeParameters(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag, cancellationToken As CancellationToken) As ReadOnlyArray(Of ParameterSymbol)
                Return ReadOnlyArray(Of ParameterSymbol).CreateFrom(
                        New SynthesizedParameterSymbol(Me, binder.GetSpecialType(Compilers.SpecialType.System_Object, syntax, diagnostics), 0, False, "TargetObject"),
                        New SynthesizedParameterSymbol(Me, binder.GetSpecialType(Compilers.SpecialType.System_IntPtr, syntax, diagnostics), 1, False, "TargetMethod")
                        )
            End Function

            ''' <summary>
            ''' Makes the type of the return.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
            Protected Overrides Function MakeReturnType(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag) As TypeSymbol
                Return binder.GetSpecialType(Compilers.SpecialType.System_Void, syntax, diagnostics)
            End Function

        End Class
    End Class
End Namespace