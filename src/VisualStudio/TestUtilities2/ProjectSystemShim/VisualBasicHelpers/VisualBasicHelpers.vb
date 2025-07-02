' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
    Friend Module VisualBasicHelpers
        Public Function CreateVisualBasicProject(environment As TestEnvironment, projectName As String, Optional compilerHost As IVbCompilerHost = Nothing) As VisualBasicProject
            Dim projectBinPath = Path.GetTempPath()
            Return New VisualBasicProject(projectName,
                                          If(compilerHost, MockCompilerHost.FullFrameworkCompilerHost),
                                          environment.CreateHierarchy(projectName, projectBinPath, projectRefPath:=Nothing, "VB"),
                                          isIntellisenseProject:=False,
                                          environment.ServiceProvider,
                                          environment.ThreadingContext)
        End Function

        Public Function CreateMinimalCompilerOptions(project As VisualBasicProject) As VBCompilerOptions
            Dim options As VBCompilerOptions = Nothing
            options.wszExeName = project.AssemblyName + ".exe"
            options.OutputType = VBCompilerOutputTypes.OUTPUT_ConsoleEXE
            options.wszOutputPath = "C:\OutputPath"

            Return options
        End Function
    End Module
End Namespace
