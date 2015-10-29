' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Globalization
Imports System.Xml
Imports Microsoft.CodeAnalysis
<<<<<<< HEAD
=======
Imports Microsoft.VisualStudio.ExtensionManager
Imports Moq
>>>>>>> origin/master
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Friend Class MockExtensionManager

        Private ReadOnly _contentType As String
        Private ReadOnly _locations() As String

        Public Sub New(contentType As String, ParamArray locations() As String)
            _contentType = contentType
            _locations = locations
        End Sub

        Public Function GetEnabledExtensionContentLocations(contentTypeName As String) As IEnumerable(Of String)
            Assert.Equal(_contentType, contentTypeName)
            Return _locations
        End Function

        Public Iterator Function GetEnabledExtensions(contentTypeName As String) As IEnumerable(Of MockInstalledExtension)
            Assert.Equal(_contentType, contentTypeName)

            For Each location In _locations
                Dim installedExceptionMock As New Mock(Of IInstalledExtension)(MockBehavior.Strict)

                installedExceptionMock.SetupGet(Function(m) m.InstallPath).Returns("\InstallPath")
                installedExceptionMock.SetupGet(Function(m) m.Content).Returns(
                    SpecializedCollections.SingletonEnumerable(Of IExtensionContent)(New MockContent(_contentType, location)))

                Dim headerMock As New Mock(Of IExtensionHeader)(MockBehavior.Strict)
                headerMock.SetupGet(Function(h) h.LocalizedName).Returns("Vsix")

                installedExceptionMock.SetupGet(Function(m) m.Header).Returns(headerMock.Object)

                Yield installedExceptionMock.Object
            Next
        End Function

<<<<<<< HEAD
        Friend Class MockInstalledExtension
=======
        Friend Class MockContent
            Implements IExtensionContent
>>>>>>> origin/master

            Private ReadOnly _contentType As String
            Private ReadOnly _location As String

            Public Sub New(contentType As String, location As String)
                _contentType = contentType
                _location = location
            End Sub

<<<<<<< HEAD
            Public ReadOnly Property Content As IEnumerable(Of MockContent)
                Get
                    Return SpecializedCollections.SingletonEnumerable(Of MockContent)(New MockContent(_contentType, _location))
                End Get
            End Property

            Public ReadOnly Property Header As MockHeader
                Get
                    Return New MockHeader("Vsix")
                End Get
            End Property

            Public ReadOnly Property InstallPath As String
                Get
                    Return "\InstallPath"
                End Get
            End Property

            Friend Class MockContent

                Private ReadOnly _contentType As String
                Private ReadOnly _location As String

                Public Sub New(contentType As String, location As String)
                    _contentType = contentType
                    _location = location
                End Sub

                Public ReadOnly Property ContentTypeName As String
                    Get
                        Return _contentType
                    End Get
                End Property

                Public ReadOnly Property RelativePath As String
                    Get
                        Return _location
                    End Get
                End Property

            End Class

            Friend Class MockHeader

                Private ReadOnly _name As String

                Public Sub New(name As String)
                    _name = name
                End Sub

                Public ReadOnly Property LocalizedName As String
                    Get
                        Return _name
                    End Get
                End Property

            End Class

        End Class
=======
            Public ReadOnly Property ContentTypeName As String Implements IExtensionContent.ContentTypeName
                Get
                    Return _contentType
                End Get
            End Property

            Public ReadOnly Property RelativePath As String Implements IExtensionContent.RelativePath
                Get
                    Return _location
                End Get
            End Property

#Region "Unused methods of IExtensionContent"
            Public ReadOnly Property AdditionalElements As IList(Of XmlElement) Implements IExtensionContent.AdditionalElements
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property Attributes As IDictionary(Of String, String) Implements IExtensionContent.Attributes
                Get
                    Throw New NotImplementedException()
                End Get
            End Property
#End Region
        End Class

#Region "Unused methods of IVsExtensionManager"
        Public ReadOnly Property DidLoadUserExtensions As Boolean Implements IVsExtensionManager.DidLoadUserExtensions
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public ReadOnly Property RestartRequired As RestartReason Implements IVsExtensionManager.RestartRequired
            Get
                Throw New NotImplementedException()
            End Get
        End Property

        Public Event InstallCompleted As EventHandler(Of InstallCompletedEventArgs) Implements IVsExtensionManager.InstallCompleted
        Public Event InstallProgressChanged As EventHandler(Of InstallProgressChangedEventArgs) Implements IVsExtensionManager.InstallProgressChanged
        Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

        Public Sub Close() Implements IVsExtensionManager.Close
            Throw New NotImplementedException()
        End Sub

        Public Sub InstallAsync(extension As IInstallableExtension, perMachine As Boolean) Implements IVsExtensionManager.InstallAsync
            Throw New NotImplementedException()
        End Sub

        Public Sub InstallAsync(extension As IInstallableExtension, perMachine As Boolean, userState As Object) Implements IVsExtensionManager.InstallAsync
            Throw New NotImplementedException()
        End Sub

        Public Sub InstallAsyncCancel(userState As Object) Implements IVsExtensionManager.InstallAsyncCancel
            Throw New NotImplementedException()
        End Sub

        Public Sub RevertUninstall(extension As IInstalledExtension) Implements IVsExtensionManager.RevertUninstall
            Throw New NotImplementedException()
        End Sub

        Public Sub Uninstall(extension As IInstalledExtension) Implements IVsExtensionManager.Uninstall
            Throw New NotImplementedException()
        End Sub

        Public Function CreateExtension(extensionPath As String) As IExtension Implements IVsExtensionManager.CreateExtension
            Throw New NotImplementedException()
        End Function

        Public Function CreateInstallableExtension(extensionPath As String) As IInstallableExtension Implements IVsExtensionManager.CreateInstallableExtension
            Throw New NotImplementedException()
        End Function

        Public Function Disable(extension As IInstalledExtension) As RestartReason Implements IVsExtensionManager.Disable
            Throw New NotImplementedException()
        End Function

        Public Function Enable(extension As IInstalledExtension) As RestartReason Implements IVsExtensionManager.Enable
            Throw New NotImplementedException()
        End Function

        Public Function FindMissingReferences(extension As IExtension) As IEnumerable(Of IExtensionReference) Implements IVsExtensionManager.FindMissingReferences
            Throw New NotImplementedException()
        End Function

        Public Function GetEnabledExtensionContentLocations(contentTypeName As String, attributes As IDictionary(Of String, String)) As IEnumerable(Of String) Implements IVsExtensionManager.GetEnabledExtensionContentLocations
            Throw New NotImplementedException()
        End Function

        Public Function GetEnabledExtensions() As IEnumerable(Of IInstalledExtension) Implements IVsExtensionManager.GetEnabledExtensions
            Throw New NotImplementedException()
        End Function

        Public Function GetImmediateDependants(extension As IInstalledExtension) As IEnumerable(Of IInstalledExtension) Implements IVsExtensionManager.GetImmediateDependants
            Throw New NotImplementedException()
        End Function

        Public Function GetInstalledExtension(identifier As String) As IInstalledExtension Implements IVsExtensionManager.GetInstalledExtension
            Throw New NotImplementedException()
        End Function

        Public Function GetInstalledExtensions() As IEnumerable(Of IInstalledExtension) Implements IVsExtensionManager.GetInstalledExtensions
            Throw New NotImplementedException()
        End Function

        Public Function GetLastExtensionsChangedTimestamp() As Long Implements IVsExtensionManager.GetLastExtensionsChangedTimestamp
            Throw New NotImplementedException()
        End Function

        Public Function Install(extension As IInstallableExtension, perMachine As Boolean) As RestartReason Implements IVsExtensionManager.Install
            Throw New NotImplementedException()
        End Function
>>>>>>> origin/master

    End Class
End Namespace
