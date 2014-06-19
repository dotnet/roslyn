'-----------------------------------------------------------------------------
' Copyright (c) Microsoft Corporation. All rights reserved.
'-----------------------------------------------------------------------------

Imports System.Threading
Imports Roslyn.Compilers.VisualBasic

Namespace Roslyn.Compilers.VisualBasic

    Partial Friend Class SourceNamedTypeSymbol

        Private m_delegateInvokeMethod As MethodSymbol = Nothing

        ''' <summary>
        ''' Adds the delegate members to the given named type symbol
        ''' </summary>
        ''' <param name="members">The member collection where to add the member to</param>
        ''' <param name="tree">The syntax tree</param>
        ''' <param name="syntax">The syntax of the delegate declaration (sub or function delegate statement)</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        ''' <param name="cancellationToken">The cancellation token.</param>
        Private Sub AddDelegateMembers(
            ByRef members As Dictionary(Of String, ArrayBuilder(Of Symbol)),
            tree As SyntaxTree,
            syntax As DelegateStatementSyntax,
            diagnostics As DiagnosticBag,
            cancellationToken As CancellationToken)

            Dim bodyBinder = BinderBuilder.CreateBinderForType(m_containingModule, tree, Me)

            ' A delegate has the following members: (see CLI spec 13.6)
            ' (1) a method named Invoke with the specified signature
            m_delegateInvokeMethod = New DelegateInvokeMethodImplementation(Me, syntax, bodyBinder, diagnostics, cancellationToken)
            AddMember(m_delegateInvokeMethod,
                        bodyBinder,
                        diagnostics,
                        members)

            ' (2) a constructor with argument types (object, System.IntPtr)
            Dim delegateConstructorSymbol = New DelegateConstructor(Me, syntax, bodyBinder, diagnostics, cancellationToken)
            AddMember(delegateConstructorSymbol,
                        bodyBinder,
                        diagnostics,
                        members)

            Dim delegateBinder = New DelegateBinder(bodyBinder, Me, CType(m_delegateInvokeMethod, DelegateInvokeMethodImplementation))

            ' (3) BeginInvoke
            Dim beginInvokeSymbol = New DelegateBeginInvokeMethod(Me, syntax, delegateBinder, diagnostics, cancellationToken)
            AddMember(beginInvokeSymbol,
                        bodyBinder,
                        diagnostics,
                        members)

            ' and (4) EndInvoke methods
            Dim endInvokeSymbol = New DelegateEndInvokeMethod(Me, syntax, delegateBinder, diagnostics, cancellationToken)
            AddMember(endInvokeSymbol,
                        bodyBinder,
                        diagnostics,
                        members)
        End Sub

        ''' <summary>
        ''' Gets the delegate invoke method.
        ''' </summary>
        Public Overrides ReadOnly Property DelegateInvokeMethod As MethodSymbol
            Get
                Return m_delegateInvokeMethod
            End Get
        End Property

        Private NotInheritable Class DelegateBinder
            Inherits Binder

            Protected Friend delegateType As SourceNamedTypeSymbol
            Protected Friend invoke As DelegateInvokeMethodImplementation

            Protected Friend Sub New(bodyBinder As Binder, delegateType As SourceNamedTypeSymbol, invoke As DelegateInvokeMethodImplementation)
                MyBase.New(bodyBinder)

                Me.delegateType = delegateType
                Me.invoke = invoke
            End Sub

        End Class
    End Class
End Namespace