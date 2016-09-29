' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend NotInheritable Class VisualBasicInteractiveCompiler
        Inherits VisualBasicCompiler

        Friend Sub New(responseFile As String, baseDirectory As String, sdkDirectoryOpt As String, clientDirectory As String, args As String(), analyzerLoader As IAnalyzerAssemblyLoader)
            MyBase.New(VisualBasicCommandLineParser.ScriptRunner, responseFile, args, clientDirectory, baseDirectory, sdkDirectoryOpt, Nothing, analyzerLoader)
        End Sub

        Friend Overrides Function GetCommandLineMetadataReferenceResolver(loggerOpt As TouchedFileLogger) As MetadataReferenceResolver
            Return CommandLineRunner.GetMetadataReferenceResolver(Arguments, loggerOpt)
        End Function

        Friend Overrides ReadOnly Property Type As Type
            Get
                Return GetType(VisualBasicInteractiveCompiler)
            End Get
        End Property

        Friend Overrides Function GetAssemblyFileVersion() As String
            Return Type.GetTypeInfo().Assembly.GetCustomAttribute(Of AssemblyFileVersionAttribute)().Version
        End Function

        Public Overrides Sub PrintLogo(consoleOutput As TextWriter)
            consoleOutput.WriteLine(VBScriptingResources.LogoLine1, GetAssemblyFileVersion())
            consoleOutput.WriteLine(VBScriptingResources.LogoLine2)
            consoleOutput.WriteLine()
        End Sub

        Public Overrides Sub PrintHelp(consoleOutput As TextWriter)
            consoleOutput.Write(VBScriptingResources.InteractiveHelp)
        End Sub
    End Class

End Namespace
