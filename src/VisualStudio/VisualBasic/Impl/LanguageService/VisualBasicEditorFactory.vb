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

        Protected Overrides Function GetSolutionWithCorrectParseOptionsForProject(projectId As ProjectId, hierarchy As IVsHierarchy, solution As Solution) As Solution
            Dim project = solution.GetRequiredProject(projectId)

            Dim parseOptions = TryCast(project.ParseOptions, VisualBasicParseOptions)
            If parseOptions Is Nothing Then
                Return solution
            End If

            Dim propertyStorage = TryCast(hierarchy, IVsBuildPropertyStorage)
            If propertyStorage Is Nothing Then
                Return solution
            End If

            Dim langVersionString = ""
            If ErrorHandler.Failed(propertyStorage.GetPropertyValue("LangVersion", Nothing, CUInt(_PersistStorageType.PST_PROJECT_FILE), langVersionString)) Then
                Return solution
            End If

            Dim langVersion = LanguageVersion.Default
            If TryParse(langVersionString, langVersion) Then
                Return solution.WithProjectParseOptions(projectId, parseOptions.WithLanguageVersion(langVersion))
            End If

            Return solution
        End Function
    End Class
End Namespace
