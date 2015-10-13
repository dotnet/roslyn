' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop

Friend Class MockVbi
    Inherits VisualBasicCompiler

    Public Sub New(responseFile As String, baseDirectory As String, args As String())
        MyBase.New(VisualBasicCommandLineParser.ScriptRunner, responseFile, args, Path.GetDirectoryName(GetType(VisualBasicCompiler).Assembly.Location), baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Nothing, New SimpleAnalyzerAssemblyLoader())
    End Sub

    Protected Overrides Sub CompilerSpecificSqm(sqm As IVsSqmMulti, sqmSession As UInteger)
        Throw New NotImplementedException()
    End Sub

    Protected Overrides Function GetSqmAppID() As UInteger
        Throw New NotImplementedException()
    End Function
End Class
