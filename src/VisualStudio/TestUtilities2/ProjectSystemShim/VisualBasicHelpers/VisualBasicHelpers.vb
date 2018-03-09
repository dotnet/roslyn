' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
    Friend Module VisualBasicHelpers
        Public Function CreateVisualBasicProject(environment As TestEnvironment, projectName As String, Optional compilerHost As IVbCompilerHost = Nothing) As VisualBasicProject
            Dim projectBinPath = Path.GetTempPath()
            Return New VisualBasicProject(environment.ProjectTracker,
                                          projectName,
                                          If(compilerHost, MockCompilerHost.FullFrameworkCompilerHost),
                                          environment.CreateHierarchy(projectName, projectBinPath, "VB"),
                                          environment.ServiceProvider,
                                          commandLineParserServiceOpt:=New VisualBasicCommandLineParserService())
        End Function

        Public Function CreateVisualBasicProjectWithNullBinPath(environment As TestEnvironment, projectName As String) As VisualBasicProject
            Return New VisualBasicProject(environment.ProjectTracker,
                                          projectName,
                                          MockCompilerHost.FullFrameworkCompilerHost,
                                          environment.CreateHierarchy(projectName, projectBinPath:=Nothing, projectCapabilities:="VB"),
                                          environment.ServiceProvider,
                                          commandLineParserServiceOpt:=New VisualBasicCommandLineParserService())
        End Function

        Public Function CreateMinimalCompilerOptions(project As VisualBasicProject) As VBCompilerOptions
            Dim options As VBCompilerOptions = Nothing
            options.wszExeName = project.ProjectSystemName + ".exe"
            options.OutputType = VBCompilerOutputTypes.OUTPUT_ConsoleEXE
            options.wszOutputPath = "C:\OutputPath"

            Return options
        End Function
    End Module
End Namespace
