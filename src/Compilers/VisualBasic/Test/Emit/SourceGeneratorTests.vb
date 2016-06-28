' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.VisualStudio.Shell.Interop

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class SourceGeneratorTests
        Inherits BasicTestBase

        ''' <summary>
        ''' Report errors from source generator.
        ''' </summary>
        <Fact()>
        Public Sub ReportErrors()
            Const text =
"Class C
 End Class"
            Using directory = New DisposableDirectory(Temp)
                Dim file = directory.CreateFile("c.vb")
                file.WriteAllText(text)
                Dim diagnostics = DiagnosticBag.GetInstance()
                RunCompiler(
                    directory.Path,
                    file.Path,
                    diagnostics,
                    ImmutableArray.Create(Of SourceGenerator)(
                        New SimpleSourceGenerator(
                            Sub(c)
                                c.ReportDiagnostic(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_AsyncSubMain), Location.None))
                            End Sub)))
                diagnostics.Verify(
                    Diagnostic(ERRID.ERR_AsyncSubMain).WithLocation(1, 1))
                diagnostics.Free()
            End Using
        End Sub

        Private Shared Sub RunCompiler(baseDirectory As String, filePath As String, diagnostics As DiagnosticBag, generators As ImmutableArray(Of SourceGenerator))
            Dim compiler = New MyCompiler(
                baseDirectory,
                args:={"/nologo", "/preferreduilang:en", "/t:library", filePath},
                generators:=generators)
            Dim errorLogger = New DiagnosticBagErrorLogger(diagnostics)
            Dim builder = New StringBuilder()
            Using writer = New StringWriter(builder)
                compiler.RunCore(writer, errorLogger, Nothing)
            End Using
        End Sub

        Private NotInheritable Class MyCompiler
            Inherits VisualBasicCompiler

            Private ReadOnly _generators As ImmutableArray(Of SourceGenerator)

            Friend Sub New(
                baseDirectory As String,
                args As String(),
                generators As ImmutableArray(Of SourceGenerator))

                MyBase.New(
                    VisualBasicCommandLineParser.Default,
                    responseFile:=Nothing,
                    args:=args,
                    clientDirectory:=Nothing,
                    baseDirectory:=baseDirectory,
                    sdkDirectory:=RuntimeEnvironment.GetRuntimeDirectory(),
                    additionalReferenceDirectories:=Environment.GetEnvironmentVariable("LIB"),
                    analyzerLoader:=Nothing)
                _generators = generators
            End Sub

            Protected Overrides Sub ResolveAnalyzersAndGeneratorsFromArguments(
                diagnostics As List(Of DiagnosticInfo),
                messageProvider As CommonMessageProvider,
                ByRef analyzers As ImmutableArray(Of DiagnosticAnalyzer),
                ByRef generators As ImmutableArray(Of SourceGenerator))

                analyzers = ImmutableArray(Of DiagnosticAnalyzer).Empty
                generators = _generators
            End Sub

            Protected Overrides Sub CompilerSpecificSqm(sqm As IVsSqmMulti, sqmSession As UInteger)
                Throw New NotImplementedException()
            End Sub

            Protected Overrides Function GetSqmAppID() As UInteger
                Throw New NotImplementedException()
            End Function
        End Class

    End Class

End Namespace