' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel.Design.Serialization
Imports Microsoft.VisualStudio.Editors.AppDesCommon
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.Editors.PropPageDesigner

    ''' <summary>
    ''' Designer loader for the PropPageDesigner
    ''' </summary>
    ''' <remarks></remarks>
    Public NotInheritable Class PropPageDesignerLoader
        Inherits BasicDesignerLoader
        Implements System.IDisposable

        Private _punkDocData As Object

        ''' <summary>
        ''' This method is called immediately after the first time
        '''   BeginLoad is invoked.  This is an appopriate place to
        '''   add custom services to the loader host.  Remember to
        '''   remove any custom services you add here by overriding
        '''   Dispose.
        ''' </summary>
        ''' <remarks></remarks>
        Protected Overrides Sub Initialize()
            MyBase.Initialize()

            'Add our ComponentSerializationService so that the basic desiger will give us automatic Undo/Redo
            Dim SerializationService As New PropertyPageSerializationService(LoaderHost)
            LoaderHost.AddService(GetType(ComponentSerializationService), SerializationService)
            LoaderHost.AddService(GetType(Microsoft.VisualStudio.Shell.Design.WindowPaneProviderService), _
                New Microsoft.VisualStudio.Editors.PropPageDesigner.DeferrableWindowPaneProviderService(LoaderHost))
            Debug.Assert(GetService(GetType(ComponentSerializationService)) IsNot Nothing, _
                "We just made the ComponentSerializationService service available.  Why isn't it there?")
        End Sub


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

            If punkDocData Is Nothing Then
                Debug.Fail("Docdata must be supplied")
                Throw New InvalidOperationException()
            End If

            Debug.Assert(TypeOf punkDocData Is PropPageDesignerDocData, "Unexpected docdata type")
            If TypeOf punkDocData Is PropPageDesignerDocData Then
                _punkDocData = punkDocData
            End If

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
                CType(LoaderHost.RootComponent, PropPageDesignerRootComponent).RootDesigner.CommitAnyPendingChanges()
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

            '... BasicDesignerLoader requires that we call SetBaseComponentClassName() during load.
            SetBaseComponentClassName(GetType(PropPageDesignerRootComponent).AssemblyQualifiedName)

            Dim NewPropPageDesignerRoot As PropPageDesignerRootComponent

            Using New WaitCursor
                Debug.Assert(LoaderHost IsNot Nothing, "No host")
                If LoaderHost IsNot Nothing Then
                    NewPropPageDesignerRoot = CType(LoaderHost.CreateComponent(GetType(PropPageDesignerRootComponent), "PropPageDesignerRootComponent"), PropPageDesignerRootComponent)
                End If
            End Using

        End Sub


#Region "Dispose/IDisposable"
        ''' <summary>
        ''' Dispose of managed and unmanaged resources
        ''' </summary>
        ''' <param name="disposing">True if calling from Dispose()</param>
        ''' <remarks></remarks>
        Protected Overloads Sub Dispose(ByVal disposing As Boolean)
            If disposing Then
                ' Dispose of managed resources.
                If TypeOf _punkDocData Is PropPageDesignerDocData Then
                    DirectCast(_punkDocData, PropPageDesignerDocData).Dispose()
                End If
                'Remove our ComponentSerializationService
                LoaderHost.RemoveService(GetType(ComponentSerializationService))
            End If
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
