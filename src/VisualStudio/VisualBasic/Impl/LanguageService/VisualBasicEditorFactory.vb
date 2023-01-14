' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editor
Imports Microsoft.CodeAnalysis.Shared.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.ComponentModelHost
Imports Microsoft.VisualStudio.LanguageServices.Implementation
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.VisualBasic
    <Guid(Guids.VisualBasicEditorFactoryIdString)>
    Friend Class VisualBasicEditorFactory
        Inherits AbstractEditorFactory

        Public Sub New(componentModel As IComponentModel)
            MyBase.New(componentModel)
        End Sub

        Protected Overrides ReadOnly Property ContentTypeName As String = ContentTypeNames.VisualBasicContentType

        Protected Overrides ReadOnly Property LanguageName As String = LanguageNames.VisualBasic

        Protected Overrides Function GetProjectWithCorrectParseOptionsForProject(project As Project, hierarchy As IVsHierarchy) As Project
            Dim parseOptions = TryCast(project.ParseOptions, VisualBasicParseOptions)
            If parseOptions Is Nothing Then
                Return project
            End If

            Dim propertyStorage = TryCast(hierarchy, IVsBuildPropertyStorage)
            If propertyStorage Is Nothing Then
                Return project
            End If

            Dim langVersionString = ""
            If ErrorHandler.Failed(propertyStorage.GetPropertyValue("LangVersion", Nothing, CUInt(_PersistStorageType.PST_PROJECT_FILE), langVersionString)) Then
                Return project
            End If

            Dim langVersion = LanguageVersion.Default
            If TryParse(langVersionString, langVersion) Then
                Return project.WithParseOptions(parseOptions.WithLanguageVersion(langVersion))
            End If

            Return project
        End Function
    End Class
End Namespace
