Option Strict On
Option Explicit On

Imports Microsoft.VisualStudio.Editors
Imports System

Namespace Microsoft.VisualStudio.Editors.DesignerFramework

    ''' <summary>
    ''' an exception is thrown when the customer cancel an operation. 
    '''  We need specialize it, because we don't need pop an error message when this happens
    ''' </summary>
    ''' <remarks></remarks>
    Friend NotInheritable Class UserCanceledException
        Inherits System.ApplicationException

        Public Sub New()
            MyBase.New(SR.GetString(SR.RSE_Err_UserCancel))
        End Sub

    End Class

End Namespace

