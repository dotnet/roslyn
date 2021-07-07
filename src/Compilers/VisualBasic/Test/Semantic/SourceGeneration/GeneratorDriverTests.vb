﻿' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities.TestGenerators

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests

    Public Class GeneratorDriverTests
        Inherits BasicTestBase

        <Fact>
        Public Sub Single_File_Is_Added()

            Dim generatorSource = "
Public Class GeneratedClass

End Class
"
            Dim parseOptions = TestOptions.Regular
            Dim compilation = GetCompilation(parseOptions)
            Dim testGenerator As SingleFileTestGenerator = New SingleFileTestGenerator(generatorSource)
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count())
            Assert.NotEqual(compilation, outputCompilation)
        End Sub

        <Fact>
        Public Sub Can_Access_Additional_Files()

            Dim additionalText = New InMemoryAdditionalText("a\\file1.cs", "Hello World")

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As CallbackGenerator = New CallbackGenerator(Sub(i)
                                                                           End Sub,
                                                                           Sub(e) Assert.Equal("Hello World", e.AdditionalFiles.First().GetText().ToString()))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator),
                                                                              additionalTexts:=ImmutableArray.Create(Of AdditionalText)(additionalText),
                                                                              parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()
        End Sub

        <Fact>
        Public Sub Generator_Can_Be_Written_In_Visual_Basic()

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As VBGenerator = New VBGenerator()
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(2, outputCompilation.SyntaxTrees.Count())
            Assert.NotEqual(compilation, outputCompilation)
        End Sub

        <Fact>
        Public Sub Generator_Can_See_Syntax()

            Dim source = "
Imports System
Namespace ANamespace
    Public Class AClass
        Public Sub AMethod(p as String)
            Throw New InvalidOperationException()
        End Sub
    End Class
End Namespace
"

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions, source)
            Dim testGenerator As VBGenerator = New VBGenerator()
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(23, testGenerator._receiver._nodes.Count)
            Assert.IsType(GetType(CompilationUnitSyntax), testGenerator._receiver._nodes(0))
            Assert.IsType(GetType(ClassStatementSyntax), testGenerator._receiver._nodes(8))
            Assert.IsType(GetType(ThrowStatementSyntax), testGenerator._receiver._nodes(16))
            Assert.IsType(GetType(EndBlockStatementSyntax), testGenerator._receiver._nodes(22))

        End Sub

        <Fact>
        Public Sub Exception_During_Init()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As CallbackGenerator = New CallbackGenerator(Sub(i) Throw New Exception("Init Exception"),
                                                                           Sub(e)
                                                                           End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify(
                    Diagnostic("BC42501").WithArguments("CallbackGenerator", "Exception", "Init Exception").WithLocation(1, 1)
            )
        End Sub

        <Fact>
        Public Sub Exception_During_Execute()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As CallbackGenerator = New CallbackGenerator(Sub(i)
                                                                           End Sub,
                                                                           Sub(e) Throw New Exception("Generate Exception"))

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify(
                    Diagnostic("BC42502").WithArguments("CallbackGenerator", "Exception", "Generate Exception").WithLocation(1, 1)
            )

        End Sub

        <Fact>
        Public Sub Exception_During_SyntaxWalk()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As VBGenerator = New VBGenerator()
            testGenerator._receiver._throw = True

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify(
                Diagnostic("BC42502").WithArguments("VBGenerator", "Exception", "Syntax Walk").WithLocation(1, 1)
            )
        End Sub

        <Fact>
        Public Sub SyntaxTrees_Are_Lazy()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator = New SingleFileTestGenerator("Class C : End Class")

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            driver = driver.RunGenerators(compilation)

            Dim results = driver.GetRunResult()

            Dim tree = Assert.Single(results.GeneratedTrees)

            Dim rootFromTryGetRoot As SyntaxNode = Nothing
            Assert.False(tree.TryGetRoot(rootFromTryGetRoot))
            Dim rootFromGetRoot = tree.GetRoot()
            Assert.NotNull(rootFromGetRoot)
            Assert.True(tree.TryGetRoot(rootFromTryGetRoot))
            Assert.Same(rootFromGetRoot, rootFromTryGetRoot)
        End Sub

        <Fact>
        Public Sub Diagnostics_Respect_Suppression()

            Dim compilation As Compilation = GetCompilation(TestOptions.Regular)

            Dim gen As CallbackGenerator = New CallbackGenerator(Sub(c)
                                                                 End Sub,
                                                                 Sub(c)
                                                                     c.ReportDiagnostic(VBDiagnostic.Create("GEN001", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, True, 2))
                                                                     c.ReportDiagnostic(VBDiagnostic.Create("GEN002", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, True, 3))
                                                                 End Sub)

            VerifyDiagnosticsWithOptions(gen, compilation,
                                         Diagnostic("GEN001").WithLocation(1, 1),
                                         Diagnostic("GEN002").WithLocation(1, 1))


            Dim warnings As IDictionary(Of String, ReportDiagnostic) = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add("GEN001", ReportDiagnostic.Suppress)
            VerifyDiagnosticsWithOptions(gen, compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(warnings)),
                                         Diagnostic("GEN002").WithLocation(1, 1))

            warnings.Clear()
            warnings.Add("GEN002", ReportDiagnostic.Suppress)
            VerifyDiagnosticsWithOptions(gen, compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(warnings)),
                                         Diagnostic("GEN001").WithLocation(1, 1))

            warnings.Clear()
            warnings.Add("GEN001", ReportDiagnostic.Error)
            VerifyDiagnosticsWithOptions(gen, compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(warnings)),
                                         Diagnostic("GEN001").WithLocation(1, 1).WithWarningAsError(True),
                                         Diagnostic("GEN002").WithLocation(1, 1))

            warnings.Clear()
            warnings.Add("GEN002", ReportDiagnostic.Error)
            VerifyDiagnosticsWithOptions(gen, compilation.WithOptions(compilation.Options.WithSpecificDiagnosticOptions(warnings)),
                                         Diagnostic("GEN001").WithLocation(1, 1),
                                         Diagnostic("GEN002").WithLocation(1, 1).WithWarningAsError(True))
        End Sub

        <Fact>
        Public Sub Diagnostics_Respect_Pragma_Suppression()

            Dim gen001 = VBDiagnostic.Create("GEN001", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, True, 2)

            VerifyDiagnosticsWithSource("'comment",
                                        gen001, TextSpan.FromBounds(1, 4),
                                        Diagnostic("GEN001", "com").WithLocation(1, 2))

            VerifyDiagnosticsWithSource("#disable warning
'comment",
                                        gen001, TextSpan.FromBounds(19, 22),
                                        Diagnostic("GEN001", "com", isSuppressed:=True).WithLocation(2, 2))

            VerifyDiagnosticsWithSource("#disable warning
'comment",
                                        gen001, New TextSpan(0, 0),
                                        Diagnostic("GEN001").WithLocation(1, 1))

            VerifyDiagnosticsWithSource("#disable warning GEN001
'comment",
                                        gen001, TextSpan.FromBounds(26, 29),
                                        Diagnostic("GEN001", "com", isSuppressed:=True).WithLocation(2, 2))

            VerifyDiagnosticsWithSource("#disable warning GEN001
'comment
#enable warning GEN001
'another",
                                        gen001, TextSpan.FromBounds(60, 63),
                                        Diagnostic("GEN001", "ano").WithLocation(4, 2))

        End Sub

        <Fact>
        Public Sub Does_Not_Enable_Incremental_Generators()

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As VBIncrementalGenerator = New VBIncrementalGenerator()
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(New IncrementalGeneratorWrapper(testGenerator)), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(1, outputCompilation.SyntaxTrees.Count())
            Assert.Equal(compilation, compilation)
            Assert.False(testGenerator._initialized)
        End Sub

        <Fact>
        Public Sub Does_Not_Prefer_Incremental_Generators()

            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)
            Dim testGenerator As VBIncrementalAndSourceGenerator = New VBIncrementalAndSourceGenerator()
            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(Of ISourceGenerator)(testGenerator), parseOptions:=parseOptions)

            Dim outputCompilation As Compilation = Nothing
            Dim outputDiagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, outputDiagnostics)
            outputDiagnostics.Verify()

            Assert.Equal(1, outputCompilation.SyntaxTrees.Count())
            Assert.Equal(compilation, compilation)
            Assert.False(testGenerator._initialized)
            Assert.True(testGenerator._sourceInitialized)
            Assert.True(testGenerator._sourceExecuted)
        End Sub

        Shared Function GetCompilation(parseOptions As VisualBasicParseOptions, Optional source As String = "") As Compilation
            If (String.IsNullOrWhiteSpace(source)) Then
                source = "
Public Class C
End Class
"
            End If

            Dim compilation As Compilation = CreateCompilation(source, options:=TestOptions.DebugDll, parseOptions:=parseOptions)
            compilation.VerifyDiagnostics()
            Assert.Single(compilation.SyntaxTrees)

            Return compilation
        End Function

        Shared Sub VerifyDiagnosticsWithOptions(generator As ISourceGenerator, compilation As Compilation, ParamArray expected As DiagnosticDescription())

            compilation.VerifyDiagnostics()
            Assert.Single(compilation.SyntaxTrees)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=TestOptions.Regular)

            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()

            diagnostics.Verify(expected)
        End Sub

        Shared Sub VerifyDiagnosticsWithSource(source As String, diag As Diagnostic, location As TextSpan, ParamArray expected As DiagnosticDescription())
            Dim parseOptions = TestOptions.Regular
            source = source.Replace(Environment.NewLine, vbCrLf)
            Dim compilation As Compilation = CreateCompilation(source)
            compilation.VerifyDiagnostics()
            Assert.Single(compilation.SyntaxTrees)

            Dim gen As ISourceGenerator = New CallbackGenerator(Sub(c)
                                                                End Sub,
                                                                Sub(c)
                                                                    If location.IsEmpty Then
                                                                        c.ReportDiagnostic(diag)
                                                                    Else
                                                                        c.ReportDiagnostic(diag.WithLocation(CodeAnalysis.Location.Create(c.Compilation.SyntaxTrees.First(), location)))
                                                                    End If
                                                                End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(gen), parseOptions:=TestOptions.Regular)

            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()

            diagnostics.Verify(expected)
        End Sub
    End Class


    <Generator(LanguageNames.VisualBasic)>
    Friend Class VBGenerator
        Implements ISourceGenerator

        Public _receiver As Receiver = New Receiver()

        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            context.RegisterForSyntaxNotifications(Function() _receiver)
        End Sub

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
            context.AddSource("source.vb", "
Public Class D
End Class
")
        End Sub

        Class Receiver
            Implements ISyntaxReceiver

            Public _throw As Boolean

            Public _nodes As List(Of SyntaxNode) = New List(Of SyntaxNode)()

            Public Sub OnVisitSyntaxNode(syntaxNode As SyntaxNode) Implements ISyntaxReceiver.OnVisitSyntaxNode
                If (_throw) Then
                    Throw New Exception("Syntax Walk")
                End If
                _nodes.Add(syntaxNode)
            End Sub
        End Class

    End Class

    <Generator(LanguageNames.VisualBasic)>
    Friend Class VBIncrementalGenerator
        Implements IIncrementalGenerator

        Public _initialized As Boolean

        Public Sub Initialize(context As IncrementalGeneratorInitializationContext) Implements IIncrementalGenerator.Initialize
            _initialized = True
        End Sub
    End Class

    <Generator(LanguageNames.VisualBasic)>
    Friend Class VBIncrementalAndSourceGenerator
        Implements IIncrementalGenerator
        Implements ISourceGenerator

        Public _initialized As Boolean
        Public _sourceInitialized As Boolean
        Public _sourceExecuted As Boolean

        Public Sub Initialize(context As IncrementalGeneratorInitializationContext) Implements IIncrementalGenerator.Initialize
            _initialized = True
        End Sub

        Public Sub Initialize(context As GeneratorInitializationContext) Implements ISourceGenerator.Initialize
            _sourceInitialized = True
        End Sub

        Public Sub Execute(context As GeneratorExecutionContext) Implements ISourceGenerator.Execute
            _sourceExecuted = True
        End Sub


    End Class

End Namespace
