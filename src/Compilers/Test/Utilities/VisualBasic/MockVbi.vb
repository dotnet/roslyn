' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic

Friend Class MockVbi
    Inherits VisualBasicCompiler

    Public Sub New(responseFile As String, workingDirectory As String, args As String())
        MyBase.New(VisualBasicCommandLineParser.Script, responseFile, args, CreateBuildPaths(workingDirectory), Nothing, New DefaultAnalyzerAssemblyLoader())
    End Sub

    Private Shared Function CreateBuildPaths(workingDirectory As String) As BuildPaths
        Return RuntimeUtilities.CreateBuildPaths(workingDirectory)
    End Function
End Class
