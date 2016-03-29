'//------------------------------------------------------------------------------
'// <copyright file="DeferrableWindowPaneProviderService.cs" company="Microsoft">
'     Copyright (c) Microsoft Corporation.  All rights reserved.
' </copyright>                                                                
'------------------------------------------------------------------------------

Imports Microsoft.VisualStudio
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.VisualStudio.Shell
Imports System
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Windows.Forms


Namespace Microsoft.VisualStudio.Editors.PropPageDesigner


    ''' <summary>
    ''' The only purpose of this class is to allow us to create a window pane
    '''   specific to the property page designer (PropPageDesignerWindowPane)
    '''   instead of the one that the base one provides.
    ''' This allows us to have more control of the WindowPane.
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class DeferrableWindowPaneProviderService
        Inherits AppDesDesignerFramework.DeferrableWindowPaneProviderServiceBase

        ''' <summary>
        ''' Creates a new DeferrableWindowPaneProviderService.  This service is used by the shell
        '''   to create a PropPageDesignerWindowPane when needed (i.e., its creation is deferred).
        ''' </summary>
        ''' <param name="provider"></param>
        ''' <remarks></remarks>
        Public Sub New(ByVal provider As IServiceProvider)
            MyBase.New(provider, Nothing)
        End Sub


        ''' <summary>
        ''' We override this so that we create a window pane specific to the 
        '''   property page designer.
        ''' </summary>
        ''' <param name="surface"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Overrides Function CreateWindowPane(ByVal surface As DesignSurface) As DesignerWindowPane
            Return New PropPageDesignerWindowPane(surface)
        End Function

    End Class

End Namespace
