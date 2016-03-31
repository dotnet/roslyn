Option Strict On
Option Explicit On

Imports Microsoft.VisualStudio.Editors.Common
Imports System
Imports System.Diagnostics
Imports System.Drawing
Imports System.Windows.Forms

Namespace Microsoft.VisualStudio.Editors.DesignerFramework


    ''' <summary>
    ''' In case we're building an editor, the editor's view will contain some user controls built from FX.
    '''   These user controls handles context menu in a different way. To show the context menu the correct way,
    '''   we inherit from the FX's control and override their WndProc.
    '''   
    '''   DesignerListView is our control inherited from ListView.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Class DesignerListView
        Inherits System.Windows.Forms.ListView



        ''' <summary>
        ''' ContextMenuShow will be raised when this list view needs to show its context menu.
        ''' The derived control simply needs to handle this event to know when to show a
        '''   context menu
        ''' </summary>
        ''' <param name="sender"></param>
        ''' <param name="e"></param>
        ''' <remarks></remarks>
        Public Event ContextMenuShow(ByVal sender As Object, ByVal e As MouseEventArgs)




        ''' <summary>
        ''' We override Control.WndProc to raise the ContextMenuShow event.
        ''' </summary>
        ''' <param name="m">Windows message passed in by window.</param>
        ''' <remarks>Implementation based on sources\ndp\fx\src\WinForms\Managed\System\WinForms\Control.cs</remarks>
        Protected Overrides Sub WndProc(ByRef m As System.Windows.Forms.Message)
            ' We only handle the context menu specially.
            Select Case m.Msg
                Case Interop.win.WM_CONTEXTMENU
                    Debug.WriteLineIf(Switches.DFContextMenu.TraceVerbose, "WM_CONTEXTMENU")

                    Dim EventArgs As MouseEventArgs = DesignUtil.GetContextMenuMouseEventArgs(Me, m)
                    RaiseEvent ContextMenuShow(Me, EventArgs)
                Case Else
                    MyBase.WndProc(m)
            End Select
        End Sub



    End Class

End Namespace
