' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop

Friend Class MockVisualBasicCompiler
    Inherits VisualBasicCompiler

    Sub New(baseDirectory As String, args As String())
        MyClass.New(Nothing, baseDirectory, args)
    End Sub

    Sub New(responseFile As String, baseDirectory As String, args As String())
        MyBase.New(VisualBasicCommandLineParser.Default, responseFile, args, baseDirectory, Environment.GetEnvironmentVariable("LIB"), IO.Path.GetTempPath())
    End Sub

    Protected Overrides Function GetSqmAppID() As UInteger
        Return SqmServiceProvider.BASIC_APPID
    End Function

    Protected Overrides Sub CompilerSpecificSqm(sqm As IVsSqmMulti, sqmSession As UInteger)
        Throw New NotImplementedException
    End Sub

End Class
