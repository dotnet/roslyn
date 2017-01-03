﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Reflection
Imports System.IO
Imports Microsoft.CodeAnalysis.Scripting
Imports Microsoft.CodeAnalysis.Scripting.Hosting
Imports Microsoft.CodeAnalysis.Scripting.Test
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Scripting.Hosting
Imports Roslyn.Test.Utilities
Imports Roslyn.Utilities
Imports Xunit

Namespace Microsoft.CodeAnalysis.VisualBasic.Scripting.UnitTests

    Public Class CommandLineRunnerTests
        Inherits TestBase

        Private Shared ReadOnly s_compilerVersion As String =
            GetType(VisualBasicInteractiveCompiler).GetTypeInfo().Assembly.GetCustomAttribute(Of AssemblyFileVersionAttribute)().Version

        Private Shared ReadOnly s_defaultArgs As String() = {"/R:System"}

        Private Shared Function CreateRunner(
            Optional args As String() = Nothing,
            Optional input As String = "",
            Optional responseFile As String = Nothing,
            Optional workingDirectory As String = Nothing
        ) As CommandLineRunner
            Dim io = New TestConsoleIO(input)

            Dim buildPaths = New BuildPaths(
                clientDir:=AppContext.BaseDirectory,
                workingDir:=If(workingDirectory, AppContext.BaseDirectory),
                sdkDir:=RuntimeMetadataReferenceResolver.GetDesktopFrameworkDirectory(),
                tempDir:=Path.GetTempPath())

            Dim compiler = New VisualBasicInteractiveCompiler(
                responseFile,
                buildPaths,
                If(args, s_defaultArgs),
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
"Microsoft (R) Visual Basic Interactive Compiler version " + s_compilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> ? 10
10
>", runner.Console.Out.ToString())
        End Sub

        <Fact()>
        Public Sub TestImportArgument()
            Dim runner = CreateRunner(args:={"/Imports:<xmlns:xmlNamespacePrefix='xmlNamespaceName'>"})

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + s_compilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
>", runner.Console.Out.ToString())
        End Sub

        <Fact()>
        Public Sub TestReferenceDirective()
            Dim file1 = Temp.CreateFile("1.dll").WriteAllBytes(TestCompilationFactory.CreateVisualBasicCompilationWithCorlib("
public Class C1
Public Function Foo() As String
    Return ""Bar""
End Function
End Class", "1").EmitToArray())

            Dim runner = CreateRunner(args:={}, input:="#r """ & file1.Path & """" & vbCrLf & "? New C1().Foo()")

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + s_compilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> #r """ & file1.Path & """
> ? New C1().Foo()
""Bar""
>", runner.Console.Out.ToString())

            runner = CreateRunner(args:={}, input:="? New C1().Foo()")

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + s_compilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> ? New C1().Foo()
«Red»
(1) : error BC30002: Type 'C1' is not defined.
«Gray»
>", runner.Console.Out.ToString())
        End Sub

        <Fact()>
        Public Sub TestReferenceDirectiveWhenReferenceMissing()
            Dim runner = CreateRunner(args:={}, input:="#r ""://invalidfilepath""")

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + s_compilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> #r ""://invalidfilepath""
«Red»
(1) : error BC2017: could not find library '://invalidfilepath'
«Gray»
>", runner.Console.Out.ToString())
        End Sub

        <Fact()>
        <WorkItem(7133, "https://github.com/dotnet/roslyn/issues/7133")>
        Public Sub TestDisplayResultsWithCurrentUICulture()
            Dim runner = CreateRunner(args:={}, input:="Imports System.Globalization
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(""en-GB"")
? System.Math.PI
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(""de-DE"")
? System.Math.PI")

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + s_compilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> Imports System.Globalization
> System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(""en-GB"")
> ? System.Math.PI
3.1415926535897931
> System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(""de-DE"")
> ? System.Math.PI
3,1415926535897931
>", runner.Console.Out.ToString())

            ' Tests that DefaultThreadCurrentUICulture is respected and not DefaultThreadCurrentCulture.
            runner = CreateRunner(args:={}, input:="Imports System.Globalization
System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(""en-GB"")
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(""en-GB"")
? System.Math.PI
System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(""de-DE"")
? System.Math.PI")

            runner.RunInteractive()

            AssertEx.AssertEqualToleratingWhitespaceDifferences(
"Microsoft (R) Visual Basic Interactive Compiler version " + s_compilerVersion + "
Copyright (C) Microsoft Corporation. All rights reserved.

Type ""#help"" for more information.
> Imports System.Globalization
> System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo(""en-GB"")
> System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(""en-GB"")
> ? System.Math.PI
3.1415926535897931
> System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo(""de-DE"")
> ? System.Math.PI
3.1415926535897931
>", runner.Console.Out.ToString())
        End Sub

        <Fact>
        Public Sub Version()
            Dim runner = CreateRunner({"/version"})
            Assert.Equal(0, runner.RunInteractive())
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString())

            runner = CreateRunner({"/version", "/help"})
            Assert.Equal(0, runner.RunInteractive())
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString())

            runner = CreateRunner({"/version", "/r:somefile"})
            Assert.Equal(0, runner.RunInteractive())
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString())

            runner = CreateRunner({"/version", "/nologo"})
            Assert.Equal(0, runner.RunInteractive())
            AssertEx.AssertEqualToleratingWhitespaceDifferences(s_compilerVersion, runner.Console.Out.ToString())
        End Sub

    End Class

End Namespace
