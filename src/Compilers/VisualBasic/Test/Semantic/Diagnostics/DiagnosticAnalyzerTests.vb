' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.CommonDiagnosticAnalyzers
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class DiagnosticAnalyzerTests
        Inherits BasicTestBase

        Public Class ComplainAboutX
            Inherits DiagnosticAnalyzer

            Private Shared ReadOnly s_CA9999_UseOfVariableThatStartsWithX As DiagnosticDescriptor = New DiagnosticDescriptor(id:="CA9999_UseOfVariableThatStartsWithX", title:="CA9999_UseOfVariableThatStartsWithX", messageFormat:="Use of variable whose name starts with 'x': '{0}'", category:="Test", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(s_CA9999_UseOfVariableThatStartsWithX)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.IdentifierName)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim id = CType(context.Node, IdentifierNameSyntax)
                If id.Identifier.ValueText.StartsWith("x", StringComparison.Ordinal) Then
                    context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_CA9999_UseOfVariableThatStartsWithX, id.GetLocation, id.Identifier.ValueText))
                End If
            End Sub
        End Class

        <Fact>
        Public Sub TestGetEffectiveDiagnostics()
            Dim noneDiagDescriptor = New DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault:=True)
            Dim infoDiagDescriptor = New DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault:=True)
            Dim warningDiagDescriptor = New DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
            Dim errorDiagDescriptor = New DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Error, isEnabledByDefault:=True)

            Dim noneDiag = Microsoft.CodeAnalysis.Diagnostic.Create(noneDiagDescriptor, Location.None)
            Dim infoDiag = Microsoft.CodeAnalysis.Diagnostic.Create(infoDiagDescriptor, Location.None)
            Dim warningDiag = Microsoft.CodeAnalysis.Diagnostic.Create(warningDiagDescriptor, Location.None)
            Dim errorDiag = Microsoft.CodeAnalysis.Diagnostic.Create(errorDiagDescriptor, Location.None)

            Dim diags = New Diagnostic() {noneDiag, infoDiag, warningDiag, errorDiag}

            ' Escalate all diagnostics to error.
            Dim specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(noneDiagDescriptor.Id, ReportDiagnostic.[Error])
            specificDiagOptions.Add(infoDiagDescriptor.Id, ReportDiagnostic.[Error])
            specificDiagOptions.Add(warningDiagDescriptor.Id, ReportDiagnostic.[Error])
            Dim options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            Dim comp = CreateCompilationWithMscorlib({""}, options:=options)
            Dim effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(diags.Length, effectiveDiags.Length)
            For Each effectiveDiag In effectiveDiags
                Assert.True(effectiveDiag.Severity = DiagnosticSeverity.Error)
            Next

            ' Suppress all diagnostics.
            ' NOTE: Diagnostics with default severity error cannot be suppressed and its severity cannot be lowered.
            specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(noneDiagDescriptor.Id, ReportDiagnostic.Suppress)
            specificDiagOptions.Add(infoDiagDescriptor.Id, ReportDiagnostic.Suppress)
            specificDiagOptions.Add(warningDiagDescriptor.Id, ReportDiagnostic.Suppress)
            specificDiagOptions.Add(errorDiagDescriptor.Id, ReportDiagnostic.Suppress)
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(0, effectiveDiags.Length)

            ' Shuffle diagnostic severity.
            specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(noneDiagDescriptor.Id, ReportDiagnostic.Info)
            specificDiagOptions.Add(infoDiagDescriptor.Id, ReportDiagnostic.Hidden)
            specificDiagOptions.Add(warningDiagDescriptor.Id, ReportDiagnostic.[Error])
            specificDiagOptions.Add(errorDiagDescriptor.Id, ReportDiagnostic.Warn)
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(diags.Length, effectiveDiags.Length)
            Dim diagIds = New HashSet(Of String)(diags.[Select](Function(d) d.Id))
            For Each effectiveDiag In effectiveDiags
                Assert.[True](diagIds.Remove(effectiveDiag.Id))

                Select Case effectiveDiag.Severity
                    Case DiagnosticSeverity.Hidden
                        Assert.Equal(infoDiagDescriptor.Id, effectiveDiag.Id)

                    Case DiagnosticSeverity.Info
                        Assert.Equal(noneDiagDescriptor.Id, effectiveDiag.Id)
                        Exit Select

                    Case DiagnosticSeverity.Warning
                        Assert.Equal(errorDiagDescriptor.Id, effectiveDiag.Id)
                        Exit Select

                    Case DiagnosticSeverity.Error
                        Assert.Equal(warningDiagDescriptor.Id, effectiveDiag.Id)
                        Exit Select
                    Case Else

                        Throw ExceptionUtilities.Unreachable
                End Select
            Next

            Assert.Empty(diagIds)

        End Sub

        <Fact>
        Public Sub TestGetEffectiveDiagnosticsGlobal()
            Dim noneDiagDescriptor = New DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault:=True)
            Dim infoDiagDescriptor = New DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault:=True)
            Dim warningDiagDescriptor = New DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
            Dim errorDiagDescriptor = New DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.[Error], isEnabledByDefault:=True)

            Dim noneDiag = Microsoft.CodeAnalysis.Diagnostic.Create(noneDiagDescriptor, Location.None)
            Dim infoDiag = Microsoft.CodeAnalysis.Diagnostic.Create(infoDiagDescriptor, Location.None)
            Dim warningDiag = Microsoft.CodeAnalysis.Diagnostic.Create(warningDiagDescriptor, Location.None)
            Dim errorDiag = Microsoft.CodeAnalysis.Diagnostic.Create(errorDiagDescriptor, Location.None)

            Dim diags = New Diagnostic() {noneDiag, infoDiag, warningDiag, errorDiag}

            Dim options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Default)
            Dim comp = CreateCompilationWithMscorlib({""}, options:=options)
            Dim effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.IsWarningAsError))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Warn)
            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Warning))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Info)
            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Info))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Hidden)
            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Hidden))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(2, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Hidden))

        End Sub

        <Fact>
        Public Sub TestDisabledDiagnostics()
            Dim disabledDiagDescriptor = New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)
            Dim enabledDiagDescriptor = New DiagnosticDescriptor("XX002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Dim disabledDiag = CodeAnalysis.Diagnostic.Create(disabledDiagDescriptor, Location.None)
            Dim enabledDiag = CodeAnalysis.Diagnostic.Create(enabledDiagDescriptor, Location.None)

            Dim diags = {disabledDiag, enabledDiag}

            ' Verify that only the enabled diag shows up after filtering.
            Dim options = TestOptions.ReleaseDll
            Dim comp = CreateCompilationWithMscorlib({""}, options:=options)
            Dim effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(1, effectiveDiags.Length)
            Assert.Contains(enabledDiag, effectiveDiags)

            ' If the disabled diag was enabled through options, then it should show up.
            Dim specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(disabledDiagDescriptor.Id, ReportDiagnostic.Warn)
            specificDiagOptions.Add(enabledDiagDescriptor.Id, ReportDiagnostic.Suppress)

            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)
            comp = CreateCompilationWithMscorlib({""}, options:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(1, effectiveDiags.Length)
            Assert.Contains(disabledDiag, effectiveDiags)
        End Sub

        Public Class FullyDisabledAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)
            Public Shared desc2 As New DiagnosticDescriptor("XX002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(desc1, desc2)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
            End Sub
        End Class

        Public Class PartiallyDisabledAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("XX003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)
            Public Shared desc2 As New DiagnosticDescriptor("XX004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(desc1, desc2)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
            End Sub
        End Class

        <Fact>
        Public Sub TestDisabledAnalyzers()
            Dim FullyDisabledAnalyzer = New FullyDisabledAnalyzer()
            Dim PartiallyDisabledAnalyzer = New PartiallyDisabledAnalyzer()

            Dim options = TestOptions.ReleaseDll
            Assert.True(FullyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options))
            Assert.False(PartiallyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options))

            Dim specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(FullyDisabledAnalyzer.desc1.Id, ReportDiagnostic.Warn)
            specificDiagOptions.Add(PartiallyDisabledAnalyzer.desc2.Id, ReportDiagnostic.Suppress)

            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)
            Assert.False(FullyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options))
            Assert.True(PartiallyDisabledAnalyzer.IsDiagnosticAnalyzerSuppressed(options))
        End Sub

        Public Class ModuleStatementAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(desc1)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.ModuleStatement)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim moduleStatement = DirectCast(context.Node, ModuleStatementSyntax)
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(desc1, context.Node.GetLocation))
            End Sub
        End Class

        <Fact>
        Public Sub TestModuleStatementSyntaxAnalyzer()
            Dim analyzer = New ModuleStatementAnalyzer()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Public Module ThisModule
End Module
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                                           Diagnostic("XX001", <![CDATA[Public Module ThisModule]]>))
        End Sub

        Public Class MockSymbolAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(desc1)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.NamedType)
            End Sub

            Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                Dim sourceLoc = context.Symbol.Locations.First(Function(l) l.IsInSource)
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(desc1, sourceLoc))
            End Sub
        End Class

        <WorkItem(998724, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/998724")>
        <Fact>
        Public Sub TestSymbolAnalyzerNotInvokedForMyTemplateSymbols()
            Dim analyzer = New MockSymbolAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Public Class C
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                references:={SystemCoreRef, MsvbRef},
                options:=TestOptions.ReleaseDll)

            Dim MyTemplate = MyTemplateTests.GetMyTemplateTree(compilation)
            Assert.NotNull(MyTemplate)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                                           Diagnostic("XX001", <![CDATA[C]]>))
        End Sub

        Public Class NamespaceAndTypeNodeAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(desc1)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.NamespaceBlock, SyntaxKind.ClassBlock)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim location As Location = Nothing
                Select Case context.Node.Kind
                    Case SyntaxKind.NamespaceBlock
                        location = DirectCast(context.Node, NamespaceBlockSyntax).NamespaceStatement.Name.GetLocation
                    Case SyntaxKind.ClassBlock
                        location = DirectCast(context.Node, ClassBlockSyntax).BlockStatement.Identifier.GetLocation
                End Select
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(desc1, location))
            End Sub
        End Class

        <Fact>
        Public Sub TestSyntaxAnalyzerInvokedForNamespaceBlockAndClassBlock()
            Dim analyzer = New NamespaceAndTypeNodeAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Namespace N
    Public Class C
    End Class
End Namespace
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                references:={SystemCoreRef, MsvbRef},
                options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                                           Diagnostic("XX001", <![CDATA[N]]>),
                                           Diagnostic("XX001", <![CDATA[C]]>))
        End Sub

        Private Class CodeBlockAnalyzer
            Inherits DiagnosticAnalyzer

            Private Shared ReadOnly s_descriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("CodeBlockDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(s_descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCodeBlockAction(AddressOf OnCodeBlock)
            End Sub

            Private Shared Sub OnCodeBlock(context As CodeBlockAnalysisContext)
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(s_descriptor, context.OwningSymbol.DeclaringSyntaxReferences.First.GetLocation))
            End Sub
        End Class

        <Fact, WorkItem(1008059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1008059")>
        Public Sub TestCodeBlockAnalyzersForNoExecutableCode()
            Dim analyzer = New CodeBlockAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Public MustInherit Class C
    Public Property P() As Integer
    Public field As Integer
    Public MustOverride Sub Method()
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                references:={SystemCoreRef, MsvbRef},
                options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer})
        End Sub

        <Fact, WorkItem(1008059, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1008059")>
        Public Sub TestCodeBlockAnalyzersForEmptyMethodBody()
            Dim analyzer = New CodeBlockAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Public Class C
    Public Sub Method()
    End Sub
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                references:={SystemCoreRef, MsvbRef},
                options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False, Diagnostic("CodeBlockDiagnostic", <![CDATA[Public Sub Method()]]>))
        End Sub

        <Fact, WorkItem(1096600, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1096600")>
        Private Sub TestDescriptorForConfigurableCompilerDiagnostics()
            ' Verify that all configurable compiler diagnostics, i.e. all non-error diagnostics,
            ' have a non-null and non-empty Title and Category.
            ' These diagnostic descriptor fields show up in the ruleset editor and hence must have a valid value.

            Dim analyzer = New VisualBasicCompilerDiagnosticAnalyzer()
            For Each descriptor In analyzer.SupportedDiagnostics
                Assert.Equal(descriptor.IsEnabledByDefault, True)

                If descriptor.IsNotConfigurable() Then
                    Continue For
                End If

                Dim title = descriptor.Title.ToString()
                If String.IsNullOrEmpty(title) Then
                    Dim id = Integer.Parse(descriptor.Id.Substring(2))
                    Dim missingResource = [Enum].GetName(GetType(ERRID), id) & "_Title"
                    Dim message = String.Format("Add resource string named '{0}' for Title of '{1}' to '{2}'", missingResource, descriptor.Id, NameOf(VBResources))

                    ' This assert will fire if you are adding a new compiler diagnostic (non-error severity),
                    ' but did not add a title resource string for the diagnostic.
                    Assert.True(False, message)
                End If

                Dim category = descriptor.Category
                If String.IsNullOrEmpty(title) Then
                    Dim message = String.Format("'{0}' must have a non-null non-empty 'Category'", descriptor.Id)
                    Assert.True(False, message)
                End If
            Next
        End Sub

        Public Class FieldSymbolAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("FieldSymbolDiagnostic", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(desc1)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.Field)
            End Sub

            Public Sub AnalyzeSymbol(context As SymbolAnalysisContext)
                Dim sourceLoc = context.Symbol.Locations.First(Function(l) l.IsInSource)
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(desc1, sourceLoc))
            End Sub
        End Class

        <Fact, WorkItem(1109126, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1109126")>
        Public Sub TestFieldSymbolAnalyzer_EnumField()
            Dim analyzer = New FieldSymbolAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Public Enum E
    X = 0
End Enum
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic("FieldSymbolDiagnostic", <![CDATA[X]]>))
        End Sub

        <Fact, WorkItem(1111667, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1111667")>
        Public Sub TestFieldSymbolAnalyzer_FieldWithoutInitializer()
            Dim analyzer = New FieldSymbolAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Public Class TestClass
    Public Field As System.IntPtr
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic("FieldSymbolDiagnostic", <![CDATA[Field]]>))
        End Sub

        Public Class FieldDeclarationAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("FieldDeclarationDiagnostic", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(desc1)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.FieldDeclaration)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim sourceLoc = DirectCast(context.Node, FieldDeclarationSyntax).GetLocation
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(desc1, sourceLoc))
            End Sub
        End Class

        <Fact, WorkItem(565, "https://github.com/dotnet/roslyn/issues/565")>
        Public Sub TestFieldDeclarationAnalyzer()
            Dim analyzer = New FieldDeclarationAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Public Class C
    Dim x, y As Integer
    Dim z As Integer
    Dim x2 = 0, y2 = 0
    Dim z2 = 0
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim x, y As Integer]]>),
                    Diagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim z As Integer]]>),
                    Diagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim x2 = 0, y2 = 0]]>),
                    Diagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim z2 = 0]]>))
        End Sub

        <Fact, WorkItem(4745, "https://github.com/dotnet/roslyn/issues/4745")>
        Public Sub TestNamespaceDeclarationAnalyzer()
            Dim analyzer = New VisualBasicNamespaceDeclarationAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Namespace Foo.Bar.FooBar
End Namespace
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic(VisualBasicNamespaceDeclarationAnalyzer.DiagnosticId, <![CDATA[Namespace Foo.Bar.FooBar]]>))
        End Sub

        <Fact, WorkItem(5463, "https://github.com/dotnet/roslyn/issues/5463")>
        Public Sub TestObjectCreationInCodeBlockAnalyzer()
            Dim analyzer = New VisualBasicCodeBlockObjectCreationAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Public Class C1
End Class

Public Class C2
End Class

Public Class C3
End Class

Public Class D
    Dim x As C1 = New C1()
    Dim y As New C2()
    Public ReadOnly Property Z As New C3()
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=TestOptions.ReleaseDll)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic(VisualBasicCodeBlockObjectCreationAnalyzer.DiagnosticDescriptor.Id, <![CDATA[New C1()]]>),
                    Diagnostic(VisualBasicCodeBlockObjectCreationAnalyzer.DiagnosticDescriptor.Id, <![CDATA[New C2()]]>),
                    Diagnostic(VisualBasicCodeBlockObjectCreationAnalyzer.DiagnosticDescriptor.Id, <![CDATA[New C3()]]>))
        End Sub

        <Fact, WorkItem(1473, "https://github.com/dotnet/roslyn/issues/1473")>
        Public Sub TestReportingNotConfigurableDiagnostic()
            Dim analyzer = New NotConfigurableDiagnosticAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[]]>
                              </file>
                          </compilation>

            ' Verify, not configurable enabled diagnostic is always reported and disabled diagnostic is never reported..
            Dim options = TestOptions.ReleaseDll
            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=options)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id))

            ' Verify not configurable enabled diagnostic cannot be suppressed.
            Dim specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)
            specificDiagOptions.Add(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id, ReportDiagnostic.Suppress)
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=options)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id))


            ' Verify not configurable disabled diagnostic cannot be enabled.
            specificDiagOptions.Clear()
            specificDiagOptions.Add(NotConfigurableDiagnosticAnalyzer.DisabledRule.Id, ReportDiagnostic.Warn)
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            compilation = CreateCompilationWithMscorlibAndReferences(sources,
                    references:={SystemCoreRef, MsvbRef},
                    options:=options)

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic(NotConfigurableDiagnosticAnalyzer.EnabledRule.Id))
        End Sub

        <Fact, WorkItem(1709, "https://github.com/dotnet/roslyn/issues/1709")>
        Public Sub TestCodeBlockAction()
            Dim analyzer = New CodeBlockActionAnalyzer()
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Class C 
    Public Sub M()
    End Sub
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources, references:={SystemCoreRef, MsvbRef})

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic(CodeBlockActionAnalyzer.CodeBlockTopLevelRule.Id, <![CDATA[M]]>).WithArguments("M"),
                    Diagnostic(CodeBlockActionAnalyzer.CodeBlockPerCompilationRule.Id, <![CDATA[M]]>).WithArguments("M"))
        End Sub

        <Fact, WorkItem(1709, "https://github.com/dotnet/roslyn/issues/1709")>
        Public Sub TestCodeBlockAction_OnlyStatelessAction()
            Dim analyzer = New CodeBlockActionAnalyzer(onlyStatelessAction:=True)
            Dim sources = <compilation>
                              <file name="c.vb">
                                  <![CDATA[
Class C 
    Public Sub M()
    End Sub
End Class
]]>
                              </file>
                          </compilation>

            Dim compilation = CreateCompilationWithMscorlibAndReferences(sources, references:={SystemCoreRef, MsvbRef})

            compilation.VerifyDiagnostics()
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                    Diagnostic(CodeBlockActionAnalyzer.CodeBlockTopLevelRule.Id, <![CDATA[M]]>).WithArguments("M"))
        End Sub

        Private Shared Sub TestEffectiveSeverity(defaultSeverity As DiagnosticSeverity, expectedEffectiveSeverity As ReportDiagnostic, Optional specificOptions As Dictionary(Of String, ReportDiagnostic) = Nothing, Optional generalOption As ReportDiagnostic = ReportDiagnostic.Default, Optional isEnabledByDefault As Boolean = True)
            specificOptions = If(specificOptions, New Dictionary(Of String, ReportDiagnostic))
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication,
                                                            generalDiagnosticOption:=generalOption,
                                                            specificDiagnosticOptions:=specificOptions)
            Dim descriptor = New DiagnosticDescriptor(id:="Test0001", title:="Test0001", messageFormat:="Test0001", category:="Test0001", defaultSeverity:=defaultSeverity, isEnabledByDefault:=isEnabledByDefault)
            Dim effectiveSeverity = descriptor.GetEffectiveSeverity(options)
            Assert.Equal(expectedEffectiveSeverity, effectiveSeverity)
        End Sub

        <Fact>
        <WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")>
        <WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")>
        Public Sub EffectiveSeverity_DiagnosticDefault1()
            TestEffectiveSeverity(DiagnosticSeverity.Warning, ReportDiagnostic.Warn)
        End Sub

        <Fact>
        <WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")>
        <WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")>
        Public Sub EffectiveSeverity_DiagnosticDefault2()
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic) From {{"Test0001", ReportDiagnostic.Default}}
            Dim generalOption = ReportDiagnostic.Error

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity:=ReportDiagnostic.Warn, specificOptions:=specificOptions, generalOption:=generalOption)
        End Sub

        <Fact>
        <WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")>
        <WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")>
        Public Sub EffectiveSeverity_GeneralOption()
            Dim generalOption = ReportDiagnostic.Error
            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity:=generalOption, generalOption:=generalOption)
        End Sub

        <Fact>
        <WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")>
        <WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")>
        Public Sub EffectiveSeverity_SpecificOption()
            Dim specificOption = ReportDiagnostic.Suppress
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic) From {{"Test0001", specificOption}}
            Dim generalOption = ReportDiagnostic.Error

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity:=specificOption, specificOptions:=specificOptions, generalOption:=generalOption)
        End Sub

        <Fact>
        <WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")>
        <WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")>
        Public Sub EffectiveSeverity_GeneralOptionDoesNotEnableDisabledDiagnostic()
            Dim generalOption = ReportDiagnostic.Error
            Dim enabledByDefault = False

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity:=ReportDiagnostic.Suppress, generalOption:=generalOption, isEnabledByDefault:=enabledByDefault)
        End Sub

        <Fact>
        <WorkItem(1107500, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1107500")>
        <WorkItem(2598, "https://github.com/dotnet/roslyn/issues/2598")>
        Public Sub EffectiveSeverity_SpecificOptionEnablesDisabledDiagnostic()
            Dim specificOption = ReportDiagnostic.Warn
            Dim specificOptions = New Dictionary(Of String, ReportDiagnostic) From {{"Test0001", specificOption}}
            Dim generalOption = ReportDiagnostic.Error
            Dim enabledByDefault = False

            TestEffectiveSeverity(DiagnosticSeverity.Warning, expectedEffectiveSeverity:=specificOption, specificOptions:=specificOptions, generalOption:=generalOption, isEnabledByDefault:=enabledByDefault)
        End Sub

        <Fact, WorkItem(6998, "https://github.com/dotnet/roslyn/issues/6998")>
        Public Sub TestGeneratedCodeAnalyzer()
            Dim source = <![CDATA[
<System.CodeDom.Compiler.GeneratedCodeAttribute("tool", "version")> _
Class GeneratedCode{0}
	Private Class Nested{0}
	End Class
End Class

Class NonGeneratedCode{0}
	<System.CodeDom.Compiler.GeneratedCodeAttribute("tool", "version")> _
	Private Class NestedGeneratedCode{0}
	End Class
End Class
]]>.Value

            Dim generatedFileNames =
                {"TemporaryGeneratedFile_036C0B5B-1481-4323-8D20-8F5ADCB23D92.vb", "Test.designer.vb", "Test.Designer.vb", "Test.generated.vb", "Test.g.vb", "Test.g.i.vb"}

            Dim builder = ImmutableArray.CreateBuilder(Of SyntaxTree)()
            Dim treeNum As Integer = 0

            ' Trees with non-generated code file names
            Dim tree = VisualBasicSyntaxTree.ParseText(String.Format(source, treeNum), path:="SourceFileRegular.vb")
            builder.Add(tree)
            treeNum = treeNum + 1

            tree = VisualBasicSyntaxTree.ParseText(String.Format(source, treeNum), path:="AssemblyInfo.vb")
            builder.Add(tree)
            treeNum = treeNum + 1

            ' Trees with generated code file names
            For Each fileName In generatedFileNames
                tree = VisualBasicSyntaxTree.ParseText(String.Format(source, treeNum), path:=fileName)
                builder.Add(tree)
                treeNum = treeNum + 1
            Next

            ' Tree with '<auto-generated>' comment
            Dim autoGeneratedPrefixes = {"' <auto-generated> ", "' <autogenerated> "}
            For i = 0 To autoGeneratedPrefixes.Length - 1
                Dim autoGeneratedPrefix = autoGeneratedPrefixes(i)
                tree = VisualBasicSyntaxTree.ParseText(String.Format(autoGeneratedPrefix + source, treeNum), path:=$"SourceFileWithAutoGeneratedComment{i}.vb")
                builder.Add(tree)
                treeNum = treeNum + 1
            Next

            ' Verify no compiler diagnostics.
            Dim trees = builder.ToImmutable()
            Dim compilation = CreateCompilationWithMscorlib45(trees, {SystemRef}, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim isGeneratedFile As Func(Of String, Boolean) = Function(fileName) fileName.Contains("SourceFileWithAutoGeneratedComment") OrElse generatedFileNames.Contains(fileName)

            ' (1) Verify default mode of analysis when there is no generated code configuration.
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, generatedCodeAnalysisFlagsOpt:=Nothing)

            ' (2) Verify ConfigureGeneratedCodeAnalysis with different combinations of GeneratedCodeAnalysisFlags.
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.None)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.Default)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.Analyze)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.ReportDiagnostics)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFile, GeneratedCodeAnalysisFlags.Analyze Or GeneratedCodeAnalysisFlags.ReportDiagnostics)

            ' (4) Ensure warnaserror doesn't produce noise in generated files.
            Dim options = compilation.Options.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
            Dim warnAsErrorCompilation = compilation.WithOptions(options)
            VerifyGeneratedCodeAnalyzerDiagnostics(warnAsErrorCompilation, isGeneratedFile, generatedCodeAnalysisFlagsOpt:=Nothing)
        End Sub

        <Fact, WorkItem(6998, "https://github.com/dotnet/roslyn/issues/6998")>
        Public Sub TestGeneratedCodeAnalyzerPartialType()
            Dim source As String = <![CDATA['
<System.CodeDom.Compiler.GeneratedCodeAttribute("tool", "version")> _
Partial Class PartialType
End Class

Partial Class PartialType
End Class
]]>.Value
            Dim tree = VisualBasicSyntaxTree.ParseText(source, path:="SourceFileRegular.vb")
            Dim compilation = CreateCompilationWithMscorlib45({tree}, {SystemRef}, TestOptions.ReleaseDll)
            compilation.VerifyDiagnostics()

            Dim builder = ArrayBuilder(Of DiagnosticDescription).GetInstance()

            ' Expected symbol diagnostics
            Dim squiggledText = "PartialType"
            Dim diagnosticArgument = squiggledText
            Dim line = 3
            Dim column = 15
            AddExpectedLocalDiagnostics(builder, False, squiggledText, line, column, GeneratedCodeAnalysisFlags.ReportDiagnostics, diagnosticArgument)

            ' Expected tree diagnostics
            squiggledText = "Class"
            diagnosticArgument = tree.FilePath
            line = 7
            column = 5
            AddExpectedLocalDiagnostics(builder, False, squiggledText, line, column, GeneratedCodeAnalysisFlags.ReportDiagnostics, diagnosticArgument)

            ' Expected compilation diagnostics
            AddExpectedNonLocalDiagnostic(builder, "PartialType", compilation.SyntaxTrees(0).FilePath)

            Dim expected = builder.ToArrayAndFree()

            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, generatedCodeAnalysisFlagsOpt:=Nothing)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.None)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.Default)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.Analyze)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.ReportDiagnostics)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, GeneratedCodeAnalysisFlags.Analyze Or GeneratedCodeAnalysisFlags.ReportDiagnostics)
        End Sub

        Friend Class OwningSymbolTestAnalyzer
            Inherits DiagnosticAnalyzer

            Public Shared ReadOnly ExpressionDescriptor As New DiagnosticDescriptor("Expression", "Expression", "Expression found.", "Testing", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public NotOverridable Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(ExpressionDescriptor)
                End Get
            End Property

            Public NotOverridable Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSyntaxNodeAction(
                     Sub(nodeContext)
                         If nodeContext.OwningSymbol.Name.StartsWith("Funky") AndAlso nodeContext.Compilation.Language = "Visual Basic" Then
                             nodeContext.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(ExpressionDescriptor, nodeContext.Node.GetLocation()))
                         End If
                     End Sub,
                     SyntaxKind.IdentifierName,
                     SyntaxKind.NumericLiteralExpression)
            End Sub
        End Class

        <Fact>
        Public Sub OwningSymbolVisualBasic()
            Dim source = <compilation>
                             <file name="c.vb">
                                 <![CDATA[
Class C
    Public Sub UnFunkyMethod()
        Dim x As Integer = 0
        Dim y As Integer = x
    End Sub

    Public Sub FunkyMethod()
        Dim x As Integer = 0
        Dim y As Integer = x
    End Sub

    Public FunkyField As Integer = 12
    Public UnFunkyField As Integer = 12
End Class
]]>
                             </file>
                         </compilation>

            Dim comp = CompilationUtils.CreateCompilationWithMscorlibAndVBRuntime(source)
            comp.VerifyDiagnostics()
            comp.VerifyAnalyzerDiagnostics({New OwningSymbolTestAnalyzer}, Nothing, Nothing, False,
                                           Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "0").WithLocation(8, 28),
                                           Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "x").WithLocation(9, 28),
                                           Diagnostic(OwningSymbolTestAnalyzer.ExpressionDescriptor.Id, "12").WithLocation(12, 36))
        End Sub

        Private Shared Sub VerifyGeneratedCodeAnalyzerDiagnostics(compilation As Compilation, isGeneratedFileName As Func(Of String, Boolean), generatedCodeAnalysisFlagsOpt As GeneratedCodeAnalysisFlags?)
            Dim expected = GetExpectedGeneratedCodeAnalyzerDiagnostics(compilation, isGeneratedFileName, generatedCodeAnalysisFlagsOpt)
            VerifyGeneratedCodeAnalyzerDiagnostics(compilation, expected, generatedCodeAnalysisFlagsOpt)
        End Sub

        Private Shared Sub VerifyGeneratedCodeAnalyzerDiagnostics(compilation As Compilation, expected As DiagnosticDescription(), generatedCodeAnalysisFlagsOpt As GeneratedCodeAnalysisFlags?)
            Dim analyzers = New DiagnosticAnalyzer() {New GeneratedCodeAnalyzer(generatedCodeAnalysisFlagsOpt)}
            compilation.VerifyAnalyzerDiagnostics(analyzers, Nothing, Nothing, False, expected)
        End Sub

        Private Shared Function GetExpectedGeneratedCodeAnalyzerDiagnostics(compilation As Compilation, isGeneratedFileName As Func(Of String, Boolean), generatedCodeAnalysisFlagsOpt As GeneratedCodeAnalysisFlags?) As DiagnosticDescription()
            Dim analyzers = New DiagnosticAnalyzer() {New GeneratedCodeAnalyzer(generatedCodeAnalysisFlagsOpt)}
            Dim files = compilation.SyntaxTrees.Select(Function(t) t.FilePath).ToImmutableArray()
            Dim sortedCallbackSymbolNames = New SortedSet(Of String)()
            Dim sortedCallbackTreePaths = New SortedSet(Of String)()
            Dim builder = ArrayBuilder(Of DiagnosticDescription).GetInstance()
            For i As Integer = 0 To compilation.SyntaxTrees.Count() - 1
                Dim file = files(i)
                Dim isGeneratedFile = isGeneratedFileName(file)

                ' Type "GeneratedCode{0}"
                Dim squiggledText = String.Format("GeneratedCode{0}", i)
                Dim diagnosticArgument = squiggledText
                Dim line = 3
                Dim column = 7
                Dim isGeneratedCode = True
                AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument)

                    ' Type "Nested{0}"
                    squiggledText = String.Format("Nested{0}", i)
                    diagnosticArgument = squiggledText
                    line = 4
                    column = 16
                    isGeneratedCode = True
                    AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument)

                    ' Type "NonGeneratedCode{0}"
                    squiggledText = String.Format("NonGeneratedCode{0}", i)
                    diagnosticArgument = squiggledText
                    line = 8
                    column = 7
                    isGeneratedCode = isGeneratedFile
                    AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument)

                    ' Type "NestedGeneratedCode{0}"
                    squiggledText = String.Format("NestedGeneratedCode{0}", i)
                    diagnosticArgument = squiggledText
                    line = 10
                    column = 16
                    isGeneratedCode = True
                    AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument)

                    ' File diagnostic
                    squiggledText = "Class" ' last token in file.
                    diagnosticArgument = file
                    line = 12
                    column = 5
                    isGeneratedCode = isGeneratedFile
                    AddExpectedLocalDiagnostics(builder, isGeneratedCode, squiggledText, line, column, generatedCodeAnalysisFlagsOpt, diagnosticArgument)

                    ' Compilation end summary diagnostic (verify callbacks into analyzer)
                    ' Analyzer always called for generated code, unless generated code analysis is explicitly disabled.
                    If generatedCodeAnalysisFlagsOpt Is Nothing OrElse (generatedCodeAnalysisFlagsOpt And GeneratedCodeAnalysisFlags.Analyze) <> 0 Then
                        sortedCallbackSymbolNames.Add(String.Format("GeneratedCode{0}", i))
                        sortedCallbackSymbolNames.Add(String.Format("Nested{0}", i))
                        sortedCallbackSymbolNames.Add(String.Format("NonGeneratedCode{0}", i))
                        sortedCallbackSymbolNames.Add(String.Format("NestedGeneratedCode{0}", i))

                        sortedCallbackTreePaths.Add(file)
                    ElseIf Not isGeneratedFile Then
                        ' Analyzer always called for non-generated code.
                        sortedCallbackSymbolNames.Add(String.Format("NonGeneratedCode{0}", i))
                        sortedCallbackTreePaths.Add(file)
                    End If
                Next

                ' Compilation end summary diagnostic (verify callbacks into analyzer)
                Dim arg1 = sortedCallbackSymbolNames.Join(",")
                Dim arg2 = sortedCallbackTreePaths.Join(",")
                AddExpectedNonLocalDiagnostic(builder, {arg1, arg2})

                If compilation.Options.GeneralDiagnosticOption = ReportDiagnostic.Error Then
                    For i As Integer = 0 To builder.Count - 1
                        If DirectCast(builder(i).Code, String) <> GeneratedCodeAnalyzer.Error.Id Then
                            builder(i) = builder(i).WithWarningAsError(True)
                        End If
                    Next
                End If

                Return builder.ToArrayAndFree()
            End Function

            Private Shared Sub AddExpectedLocalDiagnostics(
            builder As ArrayBuilder(Of DiagnosticDescription),
            isGeneratedCode As Boolean,
            squiggledText As String,
            line As Integer,
            column As Integer,
            generatedCodeAnalysisFlagsOpt As GeneratedCodeAnalysisFlags?,
            ParamArray arguments As String())

                ' Always report diagnostics in generated code, unless explicitly suppressed or we are not even analyzing generated code.
                Dim reportInGeneratedCode = generatedCodeAnalysisFlagsOpt Is Nothing OrElse
                ((generatedCodeAnalysisFlagsOpt And GeneratedCodeAnalysisFlags.ReportDiagnostics) <> 0 AndAlso
                 (generatedCodeAnalysisFlagsOpt And GeneratedCodeAnalysisFlags.Analyze) <> 0)

                If Not isGeneratedCode OrElse reportInGeneratedCode Then
                    Dim diag = Diagnostic(GeneratedCodeAnalyzer.Warning.Id, squiggledText).WithArguments(arguments).WithLocation(line, column)
                    builder.Add(diag)

                    diag = Diagnostic(GeneratedCodeAnalyzer.Error.Id, squiggledText).WithArguments(arguments).WithLocation(line, column)
                    builder.Add(diag)
                End If
            End Sub

            Private Shared Sub AddExpectedNonLocalDiagnostic(builder As ArrayBuilder(Of DiagnosticDescription), ParamArray arguments As String())
                AddExpectedDiagnostic(builder, GeneratedCodeAnalyzer.Summary.Id, Nothing, 1, 1, arguments)
            End Sub

            Private Shared Sub AddExpectedDiagnostic(builder As ArrayBuilder(Of DiagnosticDescription), diagnosticId As String, squiggledText As String, line As Integer, column As Integer, ParamArray arguments As String())
                Dim diag = Diagnostic(diagnosticId, squiggledText).WithArguments(arguments).WithLocation(line, column)
                builder.Add(diag)
            End Sub
        End Class
End Namespace
