Imports System
Imports System.ComponentModel
Imports System.ComponentModel.Design
Imports System.Diagnostics
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    '// {E18B7249-8322-44c3-9A57-FE5FF3889F89}
    'static const GUID <<name>> = 
    '{ 0xe18b7249, 0x8322, 0x44c3, { 0x9a, 0x57, 0xfe, 0x5f, 0xf3, 0x88, 0x9f, 0x89 } };

    ''' <summary>
    ''' This is the designer for the top-level resource editor component (PropPageDesigner).  I.e., this
    ''' is the top-level designer.  
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class PropPageDesignerRootDesigner
        Inherits Microsoft.VisualStudio.Editors.AppDesDesignerFramework.BaseRootDesigner
        Implements IRootDesigner

        'The view associated with this root designer.
        Private m_View As PropPageDesignerView

        ''' <summary>
        ''' Returns the PropPageDesignerRootComponent component that is being edited by this designer.
        ''' </summary>
        ''' <value>The PropPageDesignerRootComponent object.</value>
        ''' <remarks></remarks>
        Public Shadows ReadOnly Property Component() As PropPageDesignerRootComponent
            Get
                Dim RootComponent As PropPageDesignerRootComponent = CType(MyBase.Component, PropPageDesignerRootComponent)
                Debug.Assert(Not RootComponent Is Nothing)
                Return RootComponent
            End Get
        End Property

        ''' <summary>
        ''' Commits any current changes in the editor to the backing docdata 
        ''' the docdata is then persisted separately
        ''' </summary>
        ''' <remarks>
        '''This should be done before attempting to persist.
        ''' </remarks>
        Public Sub CommitAnyPendingChanges()
            'CONSIDER: We should force an apply here
            'GetView().CommitAnyPendingChanges()
        End Sub


        ''' <summary>
        ''' Called by the managed designer mechanism to determine what kinds of view technologies we support.
        ''' We currently support only Windows Forms technology (i.e., our designer view, ResourceEditorView,
        ''' inherits from System.Windows.Forms.Control)
        ''' </summary>
        ''' <value></value>
        ''' <remarks>
        ''' The view technology we support, which is currently only Windows Forms
        ''' </remarks>
        Private ReadOnly Property IRootDesigner_SupportedTechnologies() As ViewTechnology() Implements IRootDesigner.SupportedTechnologies
            Get
                Return New ViewTechnology() {ViewTechnology.Default}
            End Get
        End Property

        ''' <summary>
        '''   Called by the managed designer technology to get our view, or the actual control that implements
        '''   our resource editor's designer surface.  In this case, we return an instance of ResourceEditorView.
        ''' </summary>
        ''' <param name="Technology"></param>
        ''' <returns></returns>
        ''' <remarks>
        '''   The newly-instantiated ResourceEditorView object.
        ''' </remarks>
        Private Function RootDesigner_GetView(ByVal Technology As ViewTechnology) As Object Implements IRootDesigner.GetView
            If Technology <> ViewTechnology.Default Then
                Throw New ArgumentException("Not a supported view technology", "Technology")
            End If

            If m_View Is Nothing Then
                m_View = New PropPageDesignerView(Me)
            End If

            Return m_View
        End Function

        ''' <summary>
        ''' Wrapper function to expose our UI object
        ''' </summary>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Public Function GetView() As PropPageDesignerView
            Return CType(RootDesigner_GetView(ViewTechnology.Default), PropPageDesignerView)
        End Function

        ''' <summary>
        '''  Exposes GetService from ComponentDesigner to other classes in this assemlby to get a service.
        ''' </summary>
        ''' <param name="ServiceType">The type of the service being asked for.</param>
        ''' <returns>The requested service, if it exists.</returns>
        Public Shadows Function GetService(ByVal ServiceType As Type) As Object
            Return MyBase.GetService(ServiceType)
        End Function

#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Disposes of the root designer
        ''' </summary>
        ''' <param name="Disposing"></param>
        ''' <remarks></remarks>
        Protected Overloads Overrides Sub Dispose(ByVal Disposing As Boolean)
            If Disposing Then
                If m_View IsNot Nothing Then
                    m_View.Dispose()
                    m_View = Nothing
                End If
            End If

            MyBase.Dispose(Disposing)
        End Sub
#End Region

    End Class

End Namespace
