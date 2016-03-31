Imports System
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    ''' <summary>
    ''' This class can be used to add a custom view other than a designer to the
    '''   surface on the project designer.  It provides the means to create a view
    '''   on demand (to keep load performance of the project designer down).  To
    '''   use this capability, inherit from this class, create a view class, and
    '''   assign an instance of this class to the CustomVie property of the 
    '''   ApplicationDesignerPanel class.
    ''' </summary>>
    Public MustInherit Class CustomViewProvider
        Implements IDisposable

        ''' <summary>
        ''' Returns the view control (if already created)
        ''' </summary>
        ''' <value></value>
        ''' <remarks></remarks>
        Public MustOverride ReadOnly Property View() As Control

        ''' <summary>
        ''' Creates the view control, if it doesn't already exist
        ''' </summary>
        ''' <remarks></remarks>
        Public MustOverride Sub CreateView()

        ''' <summary>
        ''' Close the view control, if not already closed
        ''' </summary>
        ''' <remarks></remarks>
        Public MustOverride Sub CloseView()

#Region "Dispose/IDisposable"

        Public Sub Dispose() Implements IDisposable.Dispose
            Dispose(True)
        End Sub

        ''' <summary>
        ''' Disposes of contained objects
        ''' </summary>
        ''' <param name="disposing"></param>
        ''' <remarks></remarks>
        Protected Overridable Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                ' Dispose managed resources.
            End If
        End Sub

#End Region

    End Class



    ''' <summary>
    ''' This is a simple, related class that returns the document moniker to use to
    '''   create a view.
    ''' </summary>
    ''' <remarks></remarks>
    Public MustInherit Class CustomDocumentMonikerProvider

        ''' <summary>
        ''' Retrieve the filename to use for the moniker.  The file may or may not exist
        '''   and should be verified by the client.
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public MustOverride Function GetDocumentMoniker() As String

    End Class

End Namespace
