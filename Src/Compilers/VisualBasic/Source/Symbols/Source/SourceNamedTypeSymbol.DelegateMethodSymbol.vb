'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Threading

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend Class SourceNamedTypeSymbol

        ''' <summary>
        ''' Abstract base class of all synthesized delegate methods
        ''' </summary>
        Friend MustInherit Class DelegateMethodSymbol
            Inherits SourceMethodSymbol

            ' method flags for the synthesized delegate methods
            Protected Const DelegateConstructorMethodFlags As SourceMemberFlags = CType(Accessibility.Public Or SourceMemberFlags.MethodKindConstructor, SourceMemberFlags)
            Protected Const DelegateCommonMethodFlags As SourceMemberFlags = CType(SourceMemberFlags.AccessibilityPublic Or SourceMemberFlags.MethodKindDelegateInvoke Or SourceMemberFlags.Overrides Or SourceMemberFlags.Overridable, SourceMemberFlags)

            ''' <summary>
            ''' Initializes a new instance of the <see cref="DelegateMethodSymbol" /> class.
            ''' </summary>
            ''' <param name="containingType">Containing type</param>
            ''' <param name="name">The name of the delegate</param>
            ''' <param name="syntax">The syntax of the delegate declaration</param>
            ''' <param name="flags">The flags (accessibility, ...)</param>
            ''' <param name="binder">The binder</param>
            ''' <param name="diagnostics">The diagnostics.</param>
            ''' <param name="cancellationToken">The cancellation token</param>
            Protected Sub New(containingType As SourceNamedTypeSymbol,
                        name As String,
                        syntax As DelegateStatementSyntax,
                        flags As SourceMemberFlags,
                        binder As Binder,
                        diagnostics As DiagnosticBag,
                        cancellationToken As CancellationToken)
                MyBase.New(containingType, name, flags, binder.GetSyntaxReference(syntax), Nothing)

                Me.SetReturnType(MakeReturnType(binder, syntax, diagnostics)) ' MakeParameters consumes m_return, so initialize first
                Me.SetParameters(MakeParameters(binder, syntax, diagnostics, cancellationToken))
            End Sub


            ''' <summary>
            ''' Makes the parameters for the delegate method.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param>
            ''' <param name="cancellationToken">The cancellation token.</param><returns></returns>
            Protected MustOverride Overloads Function MakeParameters(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag, cancellationToken As CancellationToken) As ReadOnlyArray(Of ParameterSymbol)

            ''' <summary>
            ''' Makes the type of the return.
            ''' </summary>
            ''' <param name="binder">The binder.</param>
            ''' <param name="syntax">The syntax.</param>
            ''' <param name="diagnostics">The diagnostics.</param><returns></returns>
            Protected MustOverride Function MakeReturnType(binder As Binder, syntax As DelegateStatementSyntax, diagnostics As DiagnosticBag) As TypeSymbol

            ''' <summary>
            ''' Gets a value indicating whether this instance is synthesized.
            ''' </summary>
            ''' <value>
            ''' <c>true</c> if this instance is synthesized; otherwise, <c>false</c>.
            ''' </value>
            Public Overrides ReadOnly Property IsSynthesized As Boolean
                Get
                    Return True
                End Get
            End Property
        End Class

    End Class
End Namespace
