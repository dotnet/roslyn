Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design

Namespace Microsoft.VisualStudio.Editors.SettingsDesigner

    ''' <summary>
    ''' Wrapper around a DesignerTransaction. Since we don't always have a designer host available,
    ''' we can't assume that we can create a transaction. This class allows the use of a "Using" statement
    ''' regardless of if a designer host is available or not...
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class SettingsDesignerUndoTransaction
        Implements IDisposable

        ''' <summary>
        ''' Our wrapped transaction, or nothing if not designer host is available
        ''' </summary>
        ''' <remarks></remarks>
        Private m_Transaction As DesignerTransaction

        ''' <summary>
        ''' Create a new instance
        ''' </summary>
        ''' <param name="Provider"></param>
        ''' <param name="Description"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal Provider As IServiceProvider, ByVal Description As String)
            If Provider IsNot Nothing Then
                Initialize(DirectCast(Provider.GetService(GetType(IDesignerHost)), IDesignerHost), Description)
            Else
                Initialize(Nothing, Description)
            End If
        End Sub

        ''' <summary>
        ''' Create a new instance
        ''' </summary>
        ''' <param name="DesignerHost"></param>
        ''' <param name="Description"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal DesignerHost As IDesignerHost, ByVal Description As String)
            Initialize(DesignerHost, Description)
        End Sub

        ''' <summary>
        ''' Common initialization of this instance
        ''' </summary>
        ''' <param name="DesignerHost"></param>
        ''' <param name="Description"></param>
        ''' <remarks></remarks>
        Private Sub Initialize(ByVal DesignerHost As IDesignerHost, ByVal Description As String)
            If DesignerHost IsNot Nothing Then
                m_Transaction = DesignerHost.CreateTransaction(Description)
            End If
        End Sub

        ''' <summary>
        ''' Cancel this transaction (if any)
        ''' </summary>
        ''' <remarks>If no wrapped transaction is available, this is a nop</remarks>
        Public Sub Cancel()
            If m_Transaction IsNot Nothing Then
                m_Transaction.Cancel()
            End If
        End Sub

        ''' <summary>
        ''' Commit this transaction (if any)
        ''' </summary>
        ''' <remarks>If no wrapped transaction is available, this is a nop</remarks>
        Public Sub Commit()
            If m_Transaction IsNot Nothing Then
                m_Transaction.Commit()
            End If
        End Sub


        ''' <summary>
        ''' Cancel the transaction (if any)
        ''' </summary>
        ''' <remarks>Allows for a "using" statement</remarks>
        Public Sub Dispose() Implements System.IDisposable.Dispose
            If m_Transaction IsNot Nothing AndAlso Not m_Transaction.Committed() Then
                m_Transaction.Cancel()
            End If
        End Sub

    End Class

End Namespace