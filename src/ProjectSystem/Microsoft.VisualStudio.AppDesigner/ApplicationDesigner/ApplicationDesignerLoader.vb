' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design
Imports System.Text
Imports System.Windows.Forms
Imports System.ComponentModel.Design.Serialization
Imports Common = Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Design
Imports Microsoft.VisualStudio.Shell.Design.Serialization
Imports Microsoft.VisualStudio.Shell.Interop
Imports System.Drawing

Namespace Microsoft.VisualStudio.Editors.ApplicationDesigner

    'We might use this attribute for temporary measure to register an extension
    '<ProvideDesignerLoader(Microsoft.VisualStudio.Shell.Design.AttributeScope.File, ".vbapp$AppDesigner")> _
    ''' <summary>
    ''' Designer loader for the ApplicationDesigner
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class ApplicationDesignerLoader
        Inherits BasicDesignerLoader
        Implements System.IDisposable

        'We use the DesignerDocDataService class as a cheap way of getting check-in/out behavior.  See
        '  the Modify property.  We let it manage our DocData as its "primary" (in this case, only)
        '  doc data.  It will automatically track changes and handle check-in/check-out (see the
        '  Modifyi property).
        Protected m_DocDataService As DesignerDocDataService

        Private _punkDocData As Object

#If DEBUG Then
        Private _designerEventService As IDesignerEventService
#End If

        ''' <summary>
        ''' Initialize the designer loader. This is called just after begin load, so we shoud
        ''' have a loader host here.
        ''' This is the place where we add services!
        ''' NOTE: Remember to call RemoveService on any service object we don't own, when the Loader is disposed
        '''  Otherwise, the service container will dispose those objects. 
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            Dim callback As ServiceCreatorCallback = New ServiceCreatorCallback(AddressOf Me.OnCreateService)
            LoaderHost.AddService(GetType(WindowPaneProviderService), callback)
            LoaderHost.AddService(GetType(DesignerDocDataService), callback)

#If DEBUG Then
            If Common.Switches.PDDesignerActivations.Level <> TraceLevel.Off Then
                _designerEventService = DirectCast(LoaderHost.GetService(GetType(IDesignerEventService)), IDesignerEventService)
                If _designerEventService IsNot Nothing Then
                    AddHandler _designerEventService.ActiveDesignerChanged, AddressOf Me.OnActiveDesignerChanged
                End If
            End If
#End If
        End Sub

        Private Function OnCreateService(ByVal container As IServiceContainer, ByVal serviceType As Type) As Object
            If (serviceType Is GetType(WindowPaneProviderService)) Then
                Return New DeferrableWindowPaneProviderService(container, m_DocDataService.PrimaryDocData)
            End If
            If (serviceType Is GetType(DesignerDocDataService)) Then
                If m_DocDataService Is Nothing Then
                    Debug.Fail("DesignerDocDataService has not been created")
                End If
                Return m_DocDataService
            End If
            Return Nothing
        End Function

        ''' <summary>
        ''' This method is called to initialize the designer loader with the text
        ''' buffer to read from and a service provider through which we
        ''' can ask for services.
        ''' </summary>
        ''' <param name="ServiceProvider"></param>
        ''' <param name="Hierarchy"></param>
        ''' <param name="ItemId"></param>
        ''' <param name="punkDocData"></param>
        ''' <remarks></remarks>
        Public Sub InitializeEx(ByVal ServiceProvider As Shell.ServiceProvider, ByVal Hierarchy As IVsHierarchy, ByVal ItemId As UInteger, ByVal punkDocData As Object)

            If m_DocDataService IsNot Nothing Then
                Debug.Fail("InitializeEx() should only be called once!")
                Return
            End If

            If punkDocData Is Nothing Then
                Debug.Fail("Docdata must be supplied")
                Throw New InvalidOperationException()
            End If

            _punkDocData = punkDocData

            'In order to get free check-out behavior, we use the new VSIP class DesignerDocDataService.
            '  We pass it our punkDocData, and it wraps it with a new DocData class.  This DocData
            '  class is accessible as PrimaryDocData (in our case, it's the only doc data that the
            '  DesignerDocDataService instance will be handling).
            m_DocDataService = New DesignerDocDataService(ServiceProvider, Hierarchy, ItemId, punkDocData)
        End Sub

        ''' <summary>
        ''' This is how we handle save (although it does not necessarily correspond
        ''' to the exact point at which the file is saved, just to when the IDE thinks
        ''' it needs an updated version of the file contents).
        ''' </summary>
        ''' <param name="serializationManager"></param>
        ''' <remarks></remarks>
        Protected Overrides Sub PerformFlush(ByVal serializationManager As System.ComponentModel.Design.Serialization.IDesignerSerializationManager)
            Debug.Assert(Modified, "PerformFlush shouldn't get called if the designer's not dirty")

            If LoaderHost.RootComponent IsNot Nothing Then
                ' Make sure the property page changes have been flushed from the UI
                CType(LoaderHost.RootComponent, ApplicationDesignerRootComponent).RootDesigner.CommitAnyPendingChanges()
            Else
                Debug.Fail("LoaderHost.RootComponent is Nothing")
            End If
        End Sub

        ''' <summary>
        ''' Initializes the designer.  We are not file bsed, so not much to do
        ''' </summary>
        ''' <param name="serializationManager"></param>
        ''' <remarks>
        ''' If the load fails, this routine should throw an exception.  That exception
        ''' will automatically be added to the ErrorList by VSDesignerLoader.  If there
        ''' are more specific local exceptions, they can be added to ErrorList manually.
        '''</remarks>
        Protected Overrides Sub PerformLoad(ByVal serializationManager As System.ComponentModel.Design.Serialization.IDesignerSerializationManager)

            'Do nothing but add a root component
            '... BasicDesignerLoader requires that we call SetBaseComponentClassName() during load.
            SetBaseComponentClassName(GetType(ApplicationDesignerRootComponent).AssemblyQualifiedName)

            Dim NewApplicationDesignerRoot As ApplicationDesignerRootComponent

            Using New Common.WaitCursor
                Debug.Assert(LoaderHost IsNot Nothing, "No host")
                If LoaderHost IsNot Nothing Then
                    NewApplicationDesignerRoot = CType(LoaderHost.CreateComponent(GetType(ApplicationDesignerRootComponent)), ApplicationDesignerRootComponent)
                End If
            End Using
            Return

        End Sub

#If DEBUG Then
        Private Declare Auto Function GetDC Lib "user32" (ByVal hWnd As IntPtr) As IntPtr
        Private Declare Auto Function ReleaseDC Lib "user32" (ByVal hWnd As IntPtr, ByVal hDC As IntPtr) As Integer
        Private _designerChangeCount As Integer = 0
        Private Sub OnActiveDesignerChanged(ByVal sender As Object, ByVal e As ActiveDesignerEventArgs)
            _designerChangeCount += 1
            If Common.Switches.PDDesignerActivations.TraceWarning Then
                Dim s As New StringBuilder

                Dim oldDesigner As String = "", newDesigner As String = ""
                If e.OldDesigner IsNot Nothing AndAlso e.OldDesigner.RootComponent IsNot Nothing Then
                    oldDesigner = CType(e.OldDesigner.RootComponent, Object).GetType().FullName()
                End If
                If e.NewDesigner IsNot Nothing AndAlso e.NewDesigner.RootComponent IsNot Nothing Then
                    newDesigner = CType(e.NewDesigner.RootComponent, Object).GetType().FullName()
                End If
                s.AppendLine(String.Format("OnActiveDesignerChanged: {0}: #{1}: '{2}' -> '{3}'", Common.Switches.TimeCode, _designerChangeCount, oldDesigner, newDesigner))

                If Common.Switches.PDDesignerActivations.TraceInfo Then
                    'Print the currently-active designer to the screen DC - that makes it a lot easier to verify that the active designer is
                    '  being set correctly while testing scenarios.
                    Dim hDC As IntPtr = GetDC(CType(0, IntPtr))
                    Try
                        Using g As Graphics = Graphics.FromHdc(hDC)
                            Dim rect As New Rectangle(100, Screen.PrimaryScreen.Bounds.Height - 30, Screen.PrimaryScreen.Bounds.Width - 100, 30)
                            g.FillRectangle(Brushes.White, rect)
                            g.DrawString("Current Active Designer: " & _designerChangeCount & ": " & newDesigner, _
                                New Font("Arial", 14, FontStyle.Bold, GraphicsUnit.Pixel), Brushes.Red, rect)
                        End Using
                    Finally
                        ReleaseDC(CType(0, IntPtr), hDC)
                    End Try
                End If

                If Common.Switches.PDDesignerActivations.TraceVerbose Then
                    s.AppendLine(New System.Diagnostics.StackTrace().ToString())
                    'If e.NewDesigner Is Me.GetService(GetType(IDesignerHost)) Then
                    '    Dim rootComponent As ApplicationDesignerRootComponent = TryCast(e.NewDesigner.RootComponent, ApplicationDesignerRootComponent)
                    '    e.NewDesigner.Activate()
                    'End If
                End If
                Trace.WriteLine(s)
            End If
        End Sub
#End If

#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Dispose of managed and unmanaged resources
        ''' </summary>
        ''' <param name="disposing">True if calling from Dispose()</param>
        ''' <remarks></remarks>
        Protected Overloads Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                LoaderHost.RemoveService(GetType(DesignerDocDataService))

                ' Dispose of managed resources.
                If m_DocDataService IsNot Nothing Then
                    m_DocDataService.Dispose()
                    m_DocDataService = Nothing
                End If

#If DEBUG Then
                If _designerEventService IsNot Nothing Then
                    RemoveHandler _designerEventService.ActiveDesignerChanged, AddressOf Me.OnActiveDesignerChanged
                End If
#End If
                Debug.Assert(_punkDocData IsNot Nothing)
                _punkDocData = Nothing
            End If
            'Dispose of unmanaged resources
        End Sub

        ''' <summary>
        ''' Semi-standard IDisposable implementation
        ''' </summary>
        ''' <remarks>MyBase.Dispose called since base does not implement IDisposable</remarks>
        <CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2123:OverrideLinkDemandsShouldBeIdenticalToBase")> _
        Public Overloads Overrides Sub Dispose() Implements System.IDisposable.Dispose
            Dispose(True)
            MyBase.Dispose() 'Necessary because the base does not implement IDisposable
            GC.SuppressFinalize(Me)
        End Sub
#End Region

    End Class

End Namespace
