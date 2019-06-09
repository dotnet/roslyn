' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic

Friend Class MockVbi
    Inherits VisualBasicCompiler

    Public Sub New(responseFile As String, workingDirectory As String, args As String())
        MyBase.New(VisualBasicCommandLineParser.Script, responseFile, args, CreateBuildPaths(workingDirectory), Nothing, RuntimeUtilities.CreateAnalyzerAssemblyLoader())
    End Sub

    Private Shared Function CreateBuildPaths(workingDirectory As String) As BuildPaths
        Return RuntimeUtilities.CreateBuildPaths(workingDirectory)
    End Function
End Class
