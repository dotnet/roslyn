' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.Framework
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim
Imports Microsoft.VisualStudio.LanguageServices.VisualBasic.ProjectSystemShim.Interop

Namespace Microsoft.VisualStudio.LanguageServices.UnitTests.ProjectSystemShim.VisualBasicHelpers
    Friend Module VisualBasicHelpers
        Public Function CreateVisualBasicProject(environment As TestEnvironment, projectName As String, Optional compilerHost As IVbCompilerHost = Nothing) As VisualBasicProjectShimWithServices
            Dim projectBinPath = Path.GetTempPath()
            Return New VisualBasicProjectShimWithServices(environment.ProjectTracker,
                                                          If(compilerHost, MockCompilerHost.FullFrameworkCompilerHost),
                                                          projectName,
                                                          environment.CreateHierarchy(projectName, projectBinPath, "VB"),
                                                          environment.ServiceProvider,
                                                          New VisualBasicCommandLineParserService())
        End Function

        Public Function CreateMinimalCompilerOptions(project As VisualBasicProjectShimWithServices) As VBCompilerOptions
            Dim options As VBCompilerOptions = Nothing
            options.wszExeName = project.ProjectSystemName + ".exe"
            options.OutputType = VBCompilerOutputTypes.OUTPUT_ConsoleEXE
            options.wszOutputPath = "C:\OutputPath"

            Return options
        End Function
    End Module
End Namespace
