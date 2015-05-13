' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports System.IO
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop
Imports Microsoft.CodeAnalysis.Scripting
Imports Roslyn.Utilities
Imports VisualBasicInteractive.BasicInteractive

Friend NotInheritable Class Vbi
    Inherits VisualBasicCompiler

    Friend Const InteractiveResponseFileName As String = "vbi.rsp"

    Friend Sub New(responseFile As String, baseDirectory As String, args As String(), analyzerLoader As IAnalyzerAssemblyLoader)
        MyBase.New(VisualBasicCommandLineParser.Interactive, responseFile, args, Path.GetDirectoryName(GetType(VisualBasicCompiler).Assembly.Location), baseDirectory, RuntimeEnvironment.GetRuntimeDirectory(), Nothing, analyzerLoader) ' TODO: what to pass as additionalReferencePaths?
    End Sub

    Public Shared Function Main(args As String()) As Integer
        Try
            Dim responseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, InteractiveResponseFileName)
            Return ScriptCompilerUtil.RunInteractive(New Vbi(responseFile, Directory.GetCurrentDirectory(), args, New SimpleAnalyzerAssemblyLoader()), Console.Out)
        Catch ex As Exception
            Console.WriteLine(ex.ToString())
            Return Failed
        End Try
    End Function

    Friend Overrides Function GetExternalMetadataResolver(touchedFiles As TouchedFileLogger) As MetadataFileReferenceResolver
        ' We don't log touched files atm.
        Return New GacFileResolver(Arguments.ReferencePaths, Arguments.BaseDirectory, GacFileResolver.Default.Architectures, CultureInfo.CurrentCulture)
    End Function

    Public Overrides Sub PrintLogo(consoleOutput As TextWriter)
        Dim thisAssembly As Assembly = GetType(Vbi).Assembly
        consoleOutput.WriteLine(VbiResources.LogoLine1, FileVersionInfo.GetVersionInfo(thisAssembly.Location).FileVersion)
        consoleOutput.WriteLine(VbiResources.LogoLine2)
        consoleOutput.WriteLine()
    End Sub

    Public Overrides Sub PrintHelp(consoleOutput As TextWriter)
        ' TODO
        consoleOutput.WriteLine("                        Roslyn Interactive Compiler Options")
    End Sub

    Protected Overrides Function GetSqmAppID() As UInt32
        Return SqmServiceProvider.BASIC_APPID
    End Function

    Protected Overrides Sub CompilerSpecificSqm(sqm As IVsSqmMulti, sqmSession As UInt32)
        sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, CType(SqmServiceProvider.CompilerType.Interactive, UInt32))
    End Sub

End Class

