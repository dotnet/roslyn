' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.VisualStudio.LanguageServices.CSharp.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.CSharpHelpers

    Friend Module CSharpHelpers

        Public Function CreateCSharpProject(environment As TestEnvironment, projectName As String) As CSharpProjectShim
            Dim projectBinPath = Path.GetTempPath()
            Dim hierarchy = environment.CreateHierarchy(projectName, projectBinPath, projectRefPath:=Nothing, projectCapabilities:="CSharp")

            Return CreateCSharpProject(environment, projectName, hierarchy)
        End Function

        Public Function CreateCSharpProject(environment As TestEnvironment, projectName As String, hierarchy As IVsHierarchy) As CSharpProjectShim
            Return New CSharpProjectShim(
                New MockCSharpProjectRoot(hierarchy),
                projectSystemName:=projectName,
                hierarchy:=hierarchy,
                serviceProvider:=environment.ServiceProvider,
                threadingContext:=environment.ThreadingContext)
        End Function

    End Module

End Namespace
