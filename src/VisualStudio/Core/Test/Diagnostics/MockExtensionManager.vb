' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.ComponentModel
Imports System.Globalization
Imports System.Xml
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.ExtensionManager
Imports Roslyn.Utilities

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.Diagnostics
    Friend Class MockExtensionManager
        Implements IVsExtensionManager

        Private ReadOnly _contentType As String
        Private ReadOnly _locations() As String

        Public Sub New(contentType As String, ParamArray locations() As String)
            _contentType = contentType
            _locations = locations
        End Sub

        Public Function GetEnabledExtensionContentLocations(contentTypeName As String) As IEnumerable(Of String) Implements IVsExtensionManager.GetEnabledExtensionContentLocations
            Assert.Equal(_contentType, contentTypeName)
            Return _locations
        End Function

        Public Iterator Function GetEnabledExtensions(contentTypeName As String) As IEnumerable(Of IInstalledExtension) Implements IVsExtensionManager.GetEnabledExtensions
            Assert.Equal(_contentType, contentTypeName)

            For Each location In _locations
                Yield New MockInstalledExtension(_contentType, location)
            Next
        End Function

        Private Class MockInstalledExtension
            Implements IInstalledExtension

            Private ReadOnly _contentType As String
            Private ReadOnly _location As String

            Public Sub New(contentType As String, location As String)
                _contentType = contentType
                _location = location
            End Sub

            Public ReadOnly Property Content As IEnumerable(Of IExtensionContent) Implements IExtension.Content
                Get
                    Return SpecializedCollections.SingletonEnumerable(Of IExtensionContent)(New MockContent(_contentType, _location))
                End Get
            End Property

            Public ReadOnly Property Header As IExtensionHeader Implements IExtension.Header
                Get
                    Return New MockHeader("Vsix")
                End Get
            End Property

            Public ReadOnly Property InstallPath As String Implements IInstalledExtension.InstallPath
                Get
                    Return "\InstallPath"
                End Get
            End Property

            Friend Class MockContent
                Implements IExtensionContent

                Private ReadOnly _contentType As String
                Private ReadOnly _location As String

                Public Sub New(contentType As String, location As String)
                    _contentType = contentType
                    _location = location
                End Sub

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

#Region "Not used"
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

            Private Class MockHeader
                Implements IExtensionHeader

                Private ReadOnly _name As String

                Public Sub New(name As String)
                    _name = name
                End Sub

                Public ReadOnly Property LocalizedName As String Implements IExtensionHeader.LocalizedName
                    Get
                        Return _name
                    End Get
                End Property

#Region "Not Used"
                Public ReadOnly Property AdditionalElements As IList(Of XmlElement) Implements IExtensionHeader.AdditionalElements
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property AllUsers As Boolean Implements IExtensionHeader.AllUsers
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Author As String Implements IExtensionHeader.Author
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Description As String Implements IExtensionHeader.Description
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property GettingStartedGuide As Uri Implements IExtensionHeader.GettingStartedGuide
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property GlobalScope As Boolean Implements IExtensionHeader.GlobalScope
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Icon As String Implements IExtensionHeader.Icon
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Identifier As String Implements IExtensionHeader.Identifier
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property InstalledByMsi As Boolean Implements IExtensionHeader.InstalledByMsi
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property License As String Implements IExtensionHeader.License
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property LicenseClickThrough As Boolean Implements IExtensionHeader.LicenseClickThrough
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property LicenseFormat As String Implements IExtensionHeader.LicenseFormat
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Locale As CultureInfo Implements IExtensionHeader.Locale
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property LocalizedAdditionalElements As IList(Of XmlElement) Implements IExtensionHeader.LocalizedAdditionalElements
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property LocalizedDescription As String Implements IExtensionHeader.LocalizedDescription
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property MoreInfoUrl As Uri Implements IExtensionHeader.MoreInfoUrl
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Name As String Implements IExtensionHeader.Name
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property PreviewImage As String Implements IExtensionHeader.PreviewImage
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property ReleaseNotes As Uri Implements IExtensionHeader.ReleaseNotes
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property ReleaseNotesContent As Byte() Implements IExtensionHeader.ReleaseNotesContent
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property ReleaseNotesFormat As String Implements IExtensionHeader.ReleaseNotesFormat
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property ShortcutPath As String Implements IExtensionHeader.ShortcutPath
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property SupportedFrameworkVersionRange As VersionRange Implements IExtensionHeader.SupportedFrameworkVersionRange
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property SystemComponent As Boolean Implements IExtensionHeader.SystemComponent
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Tags As IEnumerable(Of String) Implements IExtensionHeader.Tags
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property

                Public ReadOnly Property Version As Version Implements IExtensionHeader.Version
                    Get
                        Throw New NotImplementedException()
                    End Get
                End Property
#End Region
            End Class
#Region "Not Used"
            Public ReadOnly Property AdditionalElements As IList(Of XmlElement) Implements IExtension.AdditionalElements
                Get
                    Throw New NotImplementedException()
                End Get
            End Property


            Public ReadOnly Property InstalledOn As DateTimeOffset? Implements IInstalledExtension.InstalledOn
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property InstalledPerMachine As Boolean Implements IInstalledExtension.InstalledPerMachine
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property IsPackComponent As Boolean Implements IInstalledExtension.IsPackComponent
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property LocalizedAdditionalElements As IList(Of XmlElement) Implements IExtension.LocalizedAdditionalElements
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property References As IEnumerable(Of IExtensionReference) Implements IExtension.References
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property SchemaVersion As Version Implements IExtension.SchemaVersion
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property SizeInBytes As ULong Implements IInstalledExtension.SizeInBytes
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property State As EnabledState Implements IInstalledExtension.State
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property Targets As IEnumerable(Of IExtensionRequirement) Implements IExtension.Targets
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public ReadOnly Property Type As String Implements IExtension.Type
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Function IsProductSupported(productId As String, version As Version) As Boolean Implements IExtension.IsProductSupported
                Throw New NotImplementedException()
            End Function
#End Region
        End Class
#Region "Not Used"
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

        Public Function IsInstalled(extension As IExtension) As Boolean Implements IVsExtensionManager.IsInstalled
            Throw New NotImplementedException()
        End Function

        Public Function TryGetInstalledExtension(identifier As String, ByRef result As IInstalledExtension) As Boolean Implements IVsExtensionManager.TryGetInstalledExtension
            Throw New NotImplementedException()
        End Function
#End Region
    End Class
End Namespace
