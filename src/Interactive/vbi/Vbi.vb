' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.IO
Imports System.Reflection
Imports Microsoft.CodeAnalysis.Scripting.Hosting

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend NotInheritable Class Vbi
        Friend Const InteractiveResponseFileName As String = "vbi.rsp"

        Public Shared Function Main(args As String()) As Integer
            Try
                ' Note that AppContext.BaseDirectory isn't necessarily the directory containing vbi.exe.
                ' For example, when executed via corerun it's the directory containing corerun.
                Dim vbiDirectory = Path.GetDirectoryName(GetType(Vbi).GetTypeInfo().Assembly.ManifestModule.FullyQualifiedName)

                Dim buildPaths = New BuildPaths(
                    clientDir:=vbiDirectory,
                    workingDir:=Directory.GetCurrentDirectory(),
                    sdkDir:=RuntimeMetadataReferenceResolver.GetDesktopFrameworkDirectory(),
                    tempDir:=Path.GetTempPath())

                Dim compiler = New VisualBasicInteractiveCompiler(
                    responseFile:=Path.Combine(vbiDirectory, InteractiveResponseFileName),
                    buildPaths:=buildPaths,
                    args:=args,
                    analyzerLoader:=New NotImplementedAnalyzerLoader())

                Dim runner = New CommandLineRunner(
                    ConsoleIO.Default,
                    compiler,
                    VisualBasicScriptCompiler.Instance,
                    VisualBasicObjectFormatter.Instance)

                Return runner.RunInteractive()
            Catch ex As Exception
                Console.WriteLine(ex.ToString())
                Return 1
            End Try
        End Function
    End Class

End Namespace

