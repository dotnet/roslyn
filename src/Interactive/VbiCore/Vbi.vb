' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting

    Friend NotInheritable Class Vbi
        Friend Const InteractiveResponseFileName As String = "vbi.rsp"

        Public Shared Function Main(args As String()) As Integer
            Try
                Dim responseFile = Path.Combine(AppContext.BaseDirectory, InteractiveResponseFileName)

                Dim compiler = New VisualBasicInteractiveCompiler(
                    responseFile,
                    AppContext.BaseDirectory,
                    CorLightup.Desktop.TryGetRuntimeDirectory(),
                    AppContext.BaseDirectory,
                    args,
                    New NotImplementedAnalyzerLoader())

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

