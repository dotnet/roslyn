' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Test
Imports Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.UnitTests

    Public Class CommandLineRunnerTests
        Inherits TestBase

        Private Shared ReadOnly CompilerVersion As String =
            GetType(VisualBasicInteractiveCompiler).GetTypeInfo().Assembly.GetCustomAttribute(Of AssemblyFileVersionAttribute)().Version

        Private Shared ReadOnly DefaultArgs As String() = {"/R:System"}

        Private Shared Function CreateRunner(
            Optional args As String() = Nothing,
            Optional input As String = "",
            Optional responseFile As String = Nothing,
            Optional workingDirectory As String = Nothing
        ) As CommandLineRunner
            Dim io = New TestConsoleIO(input)

            Dim compiler = New VisualBasicInteractiveCompiler(
                responseFile,
                If(workingDirectory, AppContext.BaseDirectory),
                CorLightup.Desktop.TryGetRuntimeDirectory(),
                If(args, DefaultArgs),
                New NotImplementedAnalyzerLoader())

            Return New CommandLineRunner(
                io,
                compiler,
                VisualBasicScriptCompiler.Instance,
                VisualBasicObjectFormatter.Instance)
        End Function

        <Fact()>
        Public Sub TestPrint()
            Dim runner = CreateRunner(input:="? 10")

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + CompilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> ? 10
10
>", runner.Console.Out.ToString())
        End Sub

        <Fact()>
        Public Sub TestReference()
            Dim runner = CreateRunner(args:={"/Imports:<xmlns:xmlNamespacePrefix='xmlNamespaceName'>"})

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + CompilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
>", runner.Console.Out.ToString())
        End Sub

    End Class

End Namespace