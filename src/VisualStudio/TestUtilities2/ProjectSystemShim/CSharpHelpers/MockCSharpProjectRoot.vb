' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim.Interop
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CSharpHelpers

    Friend NotInheritable Class MockCSharpProjectRoot
        Implements ICSharpProjectRoot

        Private ReadOnly _hierarchy As IVsHierarchy

        Public Sub New(hierarchy As IVsHierarchy)
            _hierarchy = hierarchy
        End Sub

        Private Function BelongsToProject(pszFileName As String) As Integer Implements ICSharpProjectRoot.BelongsToProject
            Throw New NotImplementedException()
        End Function

        Private Function BuildPerConfigCacheFileName() As String Implements ICSharpProjectRoot.BuildPerConfigCacheFileName
            Throw New NotImplementedException()
        End Function

        Private Function CanCreateFileCodeModel(pszFile As String) As Boolean Implements ICSharpProjectRoot.CanCreateFileCodeModel
            Throw New NotImplementedException()
        End Function

        Private Sub ConfigureCompiler(compiler As ICSCompiler, inputSet As ICSInputSet, addSources As Boolean) Implements ICSharpProjectRoot.ConfigureCompiler
            Throw New NotImplementedException()
        End Sub

        Private Function CreateFileCodeModel(pszFile As String, ByRef riid As Guid) As Object Implements ICSharpProjectRoot.CreateFileCodeModel
            Throw New NotImplementedException()
        End Function

        Private Function GetActiveConfigurationName() As String Implements ICSharpProjectRoot.GetActiveConfigurationName
            Throw New NotImplementedException()
        End Function

        Private Function GetFullProjectName() As String Implements ICSharpProjectRoot.GetFullProjectName
            Throw New NotImplementedException()
        End Function

        Private Function GetHierarchyAndItemID(pszFile As String, ByRef ppHier As IVsHierarchy, ByRef pItemID As UInteger) As Integer Implements ICSharpProjectRoot.GetHierarchyAndItemID
            ppHier = _hierarchy

            ' Each item should have it's own ItemID, but for simplicity we'll just hard-code a value of
            ' no particular significance.
            pItemID = 42

            Return VSConstants.S_OK
        End Function

        Private Sub GetHierarchyAndItemIDOptionallyInProject(pszFile As String, ByRef ppHier As IVsHierarchy, ByRef pItemID As UInteger, mustBeInProject As Boolean) Implements ICSharpProjectRoot.GetHierarchyAndItemIDOptionallyInProject
            Throw New NotImplementedException()
        End Sub

        Private Function GetProjectLocation() As String Implements ICSharpProjectRoot.GetProjectLocation
            Throw New NotImplementedException()
        End Function

        Private Function GetProjectSite(ByRef riid As Guid) As Object Implements ICSharpProjectRoot.GetProjectSite
            Throw New NotImplementedException()
        End Function

        Private Sub SetProjectSite(site As ICSharpProjectSite) Implements ICSharpProjectRoot.SetProjectSite
            Throw New NotImplementedException()
        End Sub

    End Class

End Namespace
