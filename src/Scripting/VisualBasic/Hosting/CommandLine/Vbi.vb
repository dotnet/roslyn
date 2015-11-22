' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend NotInheritable Class VisualBasicInteractiveCompiler
        Inherits VisualBasicCompiler

        Friend Sub New(responseFile As String, baseDirectory As String, sdkDirectory As String, args As String(), analyzerLoader As IAnalyzerAssemblyLoader)
            MyBase.New(VisualBasicCommandLineParser.ScriptRunner, responseFile, args, AppContext.BaseDirectory, baseDirectory, sdkDirectory, Nothing, analyzerLoader)
        End Sub

        Friend Overrides Function GetCommandLineMetadataReferenceResolver(loggerOpt As TouchedFileLogger) As MetadataReferenceResolver
            Return CommandLineRunner.GetMetadataReferenceResolver(Arguments, loggerOpt)
        End Function

        Public Overrides Sub PrintLogo(consoleOutput As TextWriter)
            Dim version = GetType(VisualBasicInteractiveCompiler).GetTypeInfo().Assembly.GetCustomAttribute(Of AssemblyFileVersionAttribute)().Version
            consoleOutput.WriteLine(VBScriptingResources.LogoLine1, version)
            consoleOutput.WriteLine(VBScriptingResources.LogoLine2)
            consoleOutput.WriteLine()
        End Sub

        Public Overrides Sub PrintHelp(consoleOutput As TextWriter)
            consoleOutput.Write(VBScriptingResources.InteractiveHelp)
        End Sub

        Protected Overrides Function GetSqmAppID() As UInteger
            Return SqmServiceProvider.BASIC_APPID
        End Function

        Protected Overrides Sub CompilerSpecificSqm(sqm As IVsSqmMulti, sqmSession As UInteger)
            sqm.SetDatapoint(sqmSession, SqmServiceProvider.DATAID_SQM_ROSLYN_COMPILERTYPE, CType(SqmServiceProvider.CompilerType.Interactive, UInteger))
        End Sub
    End Class

End Namespace
