' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.CodeAnalysis.VisualBasic.CommandLine

    Friend NotInheritable Class Vbc
        Inherits VisualBasicCompiler

        Friend Sub New(responseFile As String, baseDirectory As String, args As String())
            MyBase.New(VisualBasicCommandLineParser.Default, responseFile, args, baseDirectory, Environment.GetEnvironmentVariable("LIB"))
        End Sub

        Overloads Shared Function Run(responseFile As String, args As String()) As Integer

            Dim compiler = New Vbc(responseFile, Directory.GetCurrentDirectory(), args)

            FatalError.Handler = AddressOf FailFast.OnFatalException

            ' We store original encoding and restore it later to revert 
            ' the changes that might be done by /utf8output options
            ' NOTE: original encoding may not be restored if process terminated 
            Dim origEncoding = Console.OutputEncoding
            Try
                If compiler.Arguments.Utf8Output AndAlso Console.IsOutputRedirected Then
                    Console.OutputEncoding = Encoding.UTF8
                End If
                Return compiler.Run(cancellationToken:=Nothing, consoleOutput:=Console.Out)
            Finally
                Try
                    Console.OutputEncoding = origEncoding
                Catch
                    'Try to reset the output encoding, ignore if we can't
                End Try
            End Try

        End Function

        Protected Overrides Function GetSqmAppID() As UInt32
            Return SqmServiceProvider.BASIC_APPID
        End Function

        Protected Overrides Sub CompilerSpecificSqm(sqm As IVsSqmMulti, sqmSession As UInt32)
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, CType(SqmServiceProvider.CompilerType.Compiler, UInt32))
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_LANGUAGEVERSION, CType(Arguments.ParseOptions.LanguageVersion, UInt32))
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_WARNINGLEVEL, CType(If(Arguments.CompilationOptions.GeneralDiagnosticOption = ReportDiagnostic.Suppress, 1, 0), UInt32))
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_EMBEDVBCORE, CType(If(Arguments.CompilationOptions.EmbedVbCoreRuntime, 1, 0), UInt32))

            ' Project complexity # of source files, # of references
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_SOURCES, CType(Arguments.SourceFiles.Count(), UInt32))
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_REFERENCES, CType(Arguments.ReferencePaths.Count(), UInt32))
        End Sub
    End Class
End Namespace
