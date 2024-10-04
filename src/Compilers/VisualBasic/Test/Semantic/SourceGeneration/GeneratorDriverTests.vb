' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities
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
            VerifyGeneratorExceptionDiagnostic(Of Exception)(outputDiagnostics.Single(), NameOf(CallbackGenerator), "Init Exception", initialization:=True)
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
            VerifyGeneratorExceptionDiagnostic(Of Exception)(outputDiagnostics.Single(), NameOf(CallbackGenerator), "Generate Exception")

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
            VerifyGeneratorExceptionDiagnostic(Of Exception)(outputDiagnostics.Single(), NameOf(VBGenerator), "Syntax Walk")
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
        Public Sub Diagnostics_Respect_SuppressMessageAttribute()
            Dim gen001 = VBDiagnostic.Create("GEN001", "generators", "message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, True, 2)

            ' reported diagnostics can have a location in source
            VerifyDiagnosticsWithLocation("
Class C
    'comment
End Class",
                                          {(gen001, "com")},
                                          Diagnostic("GEN001", "com").WithLocation(3, 6))

            ' diagnostics are suppressed via SuppressMessageAttribute
            VerifyDiagnosticsWithLocation("
<System.Diagnostics.CodeAnalysis.SuppressMessage("""", ""GEN001"")>
Class C
    'comment
End Class",
                                          {(gen001, "com")},
                                          Diagnostic("GEN001", "com", isSuppressed:=True).WithLocation(4, 6))

            ' but not when they don't have a source location
            VerifyDiagnosticsWithLocation("
<System.Diagnostics.CodeAnalysis.SuppressMessage("""", ""GEN001"")>
Class C
    'comment
End Class",
                                          {(gen001, "")},
                                          Diagnostic("GEN001").WithLocation(1, 1))

            ' different ID suppressed + multiple diagnostics
            VerifyDiagnosticsWithLocation("
<System.Diagnostics.CodeAnalysis.SuppressMessage("""", ""GEN002"")>
Class C
    'comment
    'another
End Class",
                                          {(gen001, "com"), (gen001, "ano")},
                                          Diagnostic("GEN001", "com").WithLocation(4, 6),
                                          Diagnostic("GEN001", "ano").WithLocation(5, 6))
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_DetachedSyntaxTree_Incremental()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)

            Dim generator = New PipelineCallbackGenerator(
                Sub(ctx)
                    ctx.RegisterSourceOutput(ctx.CompilationProvider,
                        Sub(ctx2, comp)
                            Dim syntaxTree = VisualBasicSyntaxTree.ParseText(comp.SyntaxTrees.Single().GetText(), parseOptions, path:="/detached")
                            ctx2.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                                "TEST0001",
                                "Test",
                                "Test diagnostic",
                                DiagnosticSeverity.Warning,
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault:=True,
                                warningLevel:=1,
                                location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 4))))
                        End Sub)
                End Sub).AsSourceGenerator()

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, Diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_DetachedSyntaxTree_Incremental_AdditionalLocations()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)

            Dim generator = New PipelineCallbackGenerator(
                Sub(ctx)
                    ctx.RegisterSourceOutput(ctx.CompilationProvider,
                        Sub(ctx2, comp)
                            Dim validSyntaxTree = comp.SyntaxTrees.Single()
                            Dim invalidSyntaxTree = VisualBasicSyntaxTree.ParseText(validSyntaxTree.GetText(), parseOptions, path:="/detached")
                            ctx2.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                                "TEST0001",
                                "Test",
                                "Test diagnostic",
                                DiagnosticSeverity.Warning,
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault:=True,
                                warningLevel:=1,
                                location:=Location.Create(validSyntaxTree, TextSpan.FromBounds(2, 4)),
                                additionalLocations:={Location.Create(invalidSyntaxTree, TextSpan.FromBounds(2, 4))}))
                        End Sub)
                End Sub).AsSourceGenerator()

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_DetachedSyntaxTree_Execute()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)

            Dim generator As ISourceGenerator = New CallbackGenerator(
                Sub(ctx)
                End Sub,
                Sub(ctx)
                    Dim syntaxTree = VisualBasicSyntaxTree.ParseText(ctx.Compilation.SyntaxTrees.Single().GetText(), parseOptions, path:="/detached")
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault:=True,
                        warningLevel:=1,
                        location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 4))))
                End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_DetachedSyntaxTree_Execute_AdditionalLocations()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)

            Dim generator As ISourceGenerator = New CallbackGenerator(
                Sub(ctx)
                End Sub,
                Sub(ctx)
                    Dim validSyntaxTree = ctx.Compilation.SyntaxTrees.Single()
                    Dim invalidSyntaxTree = VisualBasicSyntaxTree.ParseText(validSyntaxTree.GetText(), parseOptions, path:="/detached")
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault:=True,
                        warningLevel:=1,
                        location:=Location.Create(validSyntaxTree, TextSpan.FromBounds(2, 4)),
                        additionalLocations:={Location.Create(invalidSyntaxTree, TextSpan.FromBounds(2, 4))}))
                End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location in file '/detached', which is not part of the compilation being analyzed.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_SpanOutsideRange_Incremental()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions, sourcePath:="/original")

            Dim generator = New PipelineCallbackGenerator(
                Sub(ctx)
                    ctx.RegisterSourceOutput(ctx.CompilationProvider,
                        Sub(ctx2, comp)
                            Dim syntaxTree = comp.SyntaxTrees.Single()
                            ctx2.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                                "TEST0001",
                                "Test",
                                "Test diagnostic",
                                DiagnosticSeverity.Warning,
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault:=True,
                                warningLevel:=1,
                                location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 100))))
                        End Sub)
                End Sub).AsSourceGenerator()

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[2..100)' in file '/original', which is outside of the given file.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_SpanOutsideRange_Incremental_AdditionalLocations()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions, sourcePath:="/original")

            Dim generator = New PipelineCallbackGenerator(
                Sub(ctx)
                    ctx.RegisterSourceOutput(ctx.CompilationProvider,
                        Sub(ctx2, comp)
                            Dim syntaxTree = comp.SyntaxTrees.Single()
                            ctx2.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                                "TEST0001",
                                "Test",
                                "Test diagnostic",
                                DiagnosticSeverity.Warning,
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault:=True,
                                warningLevel:=1,
                                location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 4)),
                                additionalLocations:={Location.Create(syntaxTree, TextSpan.FromBounds(2, 100))}))
                        End Sub)
                End Sub).AsSourceGenerator()

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(PipelineCallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[2..100)' in file '/original', which is outside of the given file.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_SpanOutsideRange_Execute()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions, sourcePath:="/original")

            Dim generator As ISourceGenerator = New CallbackGenerator(
                Sub(ctx)
                End Sub,
                Sub(ctx)
                    Dim syntaxTree = ctx.Compilation.SyntaxTrees.Single()
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault:=True,
                        warningLevel:=1,
                        location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 100))))
                End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[2..100)' in file '/original', which is outside of the given file.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_SpanOutsideRange_Execute_AdditionalLocations()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions, sourcePath:="/original")

            Dim generator As ISourceGenerator = New CallbackGenerator(
                Sub(ctx)
                End Sub,
                Sub(ctx)
                    Dim syntaxTree = ctx.Compilation.SyntaxTrees.Single()
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault:=True,
                        warningLevel:=1,
                        location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 4)),
                        additionalLocations:={Location.Create(syntaxTree, TextSpan.FromBounds(2, 100))}))
                End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(CallbackGenerator), "Reported diagnostic 'TEST0001' has a source location '[2..100)' in file '/original', which is outside of the given file.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_SpaceInIdentifier_Incremental()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)

            Dim generator = New PipelineCallbackGenerator(
                Sub(ctx)
                    ctx.RegisterSourceOutput(ctx.CompilationProvider,
                        Sub(ctx2, comp)
                            Dim syntaxTree = comp.SyntaxTrees.Single()
                            ctx2.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                                "TEST 0001",
                                "Test",
                                "Test diagnostic",
                                DiagnosticSeverity.Warning,
                                DiagnosticSeverity.Warning,
                                isEnabledByDefault:=True,
                                warningLevel:=1,
                                location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 4))))
                        End Sub)
                End Sub).AsSourceGenerator()

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(PipelineCallbackGenerator), "Reported diagnostic has an ID 'TEST 0001', which is not a valid identifier.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <ConditionalFact(GetType(IsEnglishLocal))>
        <WorkItem("https://devdiv.visualstudio.com/DevDiv/_workitems/edit/1805836")>
        Public Sub Diagnostic_SpaceInIdentifier_Execute()
            Dim parseOptions = TestOptions.Regular
            Dim compilation As Compilation = GetCompilation(parseOptions)

            Dim generator As ISourceGenerator = New CallbackGenerator(
                Sub(ctx)
                End Sub,
                Sub(ctx)
                    Dim syntaxTree = ctx.Compilation.SyntaxTrees.Single()
                    ctx.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(
                        "TEST 0001",
                        "Test",
                        "Test diagnostic",
                        DiagnosticSeverity.Warning,
                        DiagnosticSeverity.Warning,
                        isEnabledByDefault:=True,
                        warningLevel:=1,
                        location:=Location.Create(syntaxTree, TextSpan.FromBounds(2, 4))))
                End Sub)

            Dim driver As GeneratorDriver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(generator), parseOptions:=parseOptions)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, compilation, diagnostics)
            VerifyArgumentExceptionDiagnostic(diagnostics.Single(), NameOf(CallbackGenerator), "Reported diagnostic has an ID 'TEST 0001', which is not a valid identifier.", "diagnostic")
            compilation.VerifyDiagnostics()
        End Sub

        <Fact>
        Public Sub Enable_Incremental_Generators()

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
            Assert.True(testGenerator._initialized)
        End Sub

        <Fact>
        Public Sub Prefer_Incremental_Generators()

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
            Assert.True(testGenerator._initialized)
            Assert.False(testGenerator._sourceInitialized)
            Assert.False(testGenerator._sourceExecuted)
        End Sub

        Shared Function GetCompilation(parseOptions As VisualBasicParseOptions, Optional source As String = "", Optional sourcePath As String = "") As Compilation
            If (String.IsNullOrWhiteSpace(source)) Then
                source = "
Public Class C
End Class
"
            End If

            Dim compilation As Compilation = CreateCompilation(BasicTestSource.Parse(source, sourcePath, parseOptions), options:=TestOptions.DebugDll)
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

        Shared Sub VerifyDiagnosticsWithLocation(source As String, reportDiagnostics As IReadOnlyList(Of (Diagnostic As Diagnostic, Location As String)), ParamArray expected As DiagnosticDescription())
            Dim parseOptions = TestOptions.Regular
            source = source.Replace(Environment.NewLine, vbCrLf)
            Dim compilation = CreateCompilation(source, parseOptions:=parseOptions)
            compilation.VerifyDiagnostics()
            Dim syntaxTree = compilation.SyntaxTrees.Single()
            Dim actualDiagnostics = reportDiagnostics.SelectAsArray(
                Function(x)
                    If String.IsNullOrEmpty(x.Location) Then
                        Return x.Diagnostic
                    End If
                    Dim start = source.IndexOf(x.Location)
                    Assert.True(start >= 0, $"Not found in source: '{x.Location}'")
                    Dim endpoint = start + x.Location.Length
                    Return x.Diagnostic.WithLocation(Location.Create(syntaxTree, TextSpan.FromBounds(start, endpoint)))
                End Function)

            Dim gen As ISourceGenerator = New CallbackGenerator(
                Sub(c)
                End Sub,
                Sub(c)
                    For Each d In actualDiagnostics
                        c.ReportDiagnostic(d)
                    Next
                End Sub)

            Dim driver = VisualBasicGeneratorDriver.Create(ImmutableArray.Create(gen), parseOptions:=parseOptions)
            Dim outputCompilation As Compilation = Nothing
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            driver.RunGeneratorsAndUpdateCompilation(compilation, outputCompilation, diagnostics)
            outputCompilation.VerifyDiagnostics()
            diagnostics.Verify(expected)
        End Sub

        Shared Sub VerifyArgumentExceptionDiagnostic(diagnostic As Diagnostic, generatorName As String, message As String, parameterName As String, Optional initialization As Boolean = False)
#If NET Then
            Dim expectedMessage = $"{message} (Parameter '{parameterName}')"
#Else
            Dim expectedMessage = $"{message}{Environment.NewLine}Parameter name: {parameterName}"
#End If
            VerifyGeneratorExceptionDiagnostic(Of ArgumentException)(diagnostic, generatorName, expectedMessage, initialization)
        End Sub

        Shared Sub VerifyGeneratorExceptionDiagnostic(Of T As Exception)(diagnostic As Diagnostic, generatorName As String, message As String, Optional initialization As Boolean = False)
            Dim errorCode = If(initialization, ERRID.WRN_GeneratorFailedDuringInitialization, ERRID.WRN_GeneratorFailedDuringGeneration)
            Assert.Equal("BC" & CInt(errorCode), diagnostic.Id)
            Assert.Equal(NoLocation.Singleton, diagnostic.Location)
            Assert.Equal(4, diagnostic.Arguments.Count)
            Assert.Equal(generatorName, diagnostic.Arguments(0))
            Dim typeName = GetType(T).Name
            Assert.Equal(typeName, diagnostic.Arguments(1))
            Assert.Equal(message, diagnostic.Arguments(2))
            Dim expectedDetails = $"System.{typeName}: {message}{Environment.NewLine}   "
            Assert.StartsWith(expectedDetails, TryCast(diagnostic.Arguments(3), String))
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
