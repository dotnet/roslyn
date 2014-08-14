' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.Serialization
Imports System.Threading
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class DiagnosticAnalyzerTests
        Inherits BasicTestBase

        <Serializable>
        Class TestDiagnostic
            Inherits Diagnostic
            Implements ISerializable

            Private ReadOnly m_id As String
            Private ReadOnly m_kind As String
            Private ReadOnly m_severity As DiagnosticSeverity
            Private ReadOnly m_location As Location
            Private ReadOnly m_message As String
            Private ReadOnly m_isWarningAsError As Boolean
            Private ReadOnly m_arguments As Object()

            Public Sub New(id As String,
                           kind As String,
                           severity As DiagnosticSeverity,
                           location As Location,
                           message As String,
                           isWarningAsError As Boolean,
                           ParamArray arguments As Object())
                Me.m_id = id
                Me.m_kind = kind
                Me.m_severity = severity
                Me.m_location = location
                Me.m_message = message
                Me.m_isWarningAsError = isWarningAsError
                Me.m_arguments = arguments
            End Sub

            Private Sub New(info As SerializationInfo, context As StreamingContext)
                Me.m_id = info.GetString("id")
                Me.m_kind = info.GetString("kind")
                Me.m_message = info.GetString("message")
                Me.m_location = CType(info.GetValue("location", GetType(Location)), Location)
                Me.m_severity = CType(info.GetValue("severity", GetType(DiagnosticSeverity)), DiagnosticSeverity)
                Me.m_isWarningAsError = info.GetBoolean("isWarningAsError")
                Me.m_arguments = CType(info.GetValue("arguments", GetType(Object())), Object())
            End Sub

            Public Overrides ReadOnly Property AdditionalLocations As IReadOnlyList(Of Location)
                Get
                    Dim loc As Location() = New Location(0) {}
                    Return loc
                End Get
            End Property

            Public Overrides ReadOnly Property CustomTags As IReadOnlyList(Of String)
                Get
                    Dim tags As String() = New String(0) {}
                    Return tags
                End Get
            End Property

            Public Overrides ReadOnly Property Id As String
                Get
                    Return m_id
                End Get
            End Property

            Public Overrides ReadOnly Property Category As String
                Get
                    Return m_id
                End Get
            End Property

            Public Overrides ReadOnly Property Location As Location
                Get
                    Return m_location
                End Get
            End Property

            Public Overrides ReadOnly Property Severity As DiagnosticSeverity
                Get
                    Return m_severity
                End Get
            End Property

            Public Overrides ReadOnly Property IsEnabledByDefault As Boolean
                Get
                    Return True
                End Get
            End Property

            Public Overrides ReadOnly Property WarningLevel As Integer
                Get
                    Return 2
                End Get
            End Property

            Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
                info.AddValue("id", Me.m_id)
                info.AddValue("kind", Me.m_kind)
                info.AddValue("message", Me.m_message)
                info.AddValue("location", Me.m_location, GetType(Location))
                info.AddValue("severity", Me.m_severity, GetType(DiagnosticSeverity))
                info.AddValue("isWarningAsError", Me.m_isWarningAsError)
                info.AddValue("arguments", Me.m_arguments, GetType(Object()))
            End Sub

            Friend Overrides Function WithLocation(location As Location) As Diagnostic
                Throw New NotImplementedException()
            End Function

            Friend Overrides Function WithWarningAsError(isWarningAsError As Boolean) As Diagnostic
                If isWarningAsError AndAlso Severity = DiagnosticSeverity.Warning Then
                    Return New TestDiagnostic(Id, Category, DiagnosticSeverity.Error, m_location, m_message, isWarningAsError, m_arguments)
                Else
                    Return Me
                End If
            End Function

            Public Overrides Function GetMessage(Optional culture As CultureInfo = Nothing) As String
                Return String.Format(m_message, m_arguments)
            End Function

            Public Overrides Function Equals(obj As Diagnostic) As Boolean
                If obj Is Nothing OrElse Me.GetType() <> obj.GetType() Then Return False
                Dim other As TestDiagnostic = CType(obj, TestDiagnostic)
                Return Me.m_id = other.m_id AndAlso
                    Me.m_kind = other.m_kind AndAlso
                    Me.m_location = other.m_location AndAlso
                    Me.m_message = other.m_message AndAlso
                    SameData(Me.m_arguments, other.m_arguments)
            End Function

            Private Shared Function SameData(d1 As Object(), d2 As Object()) As Boolean
                Return (d1 Is Nothing) = (d2 Is Nothing) AndAlso (d1 Is Nothing OrElse d1.SequenceEqual(d2))
            End Function

            Friend Overrides Function WithSeverity(severity As DiagnosticSeverity) As Diagnostic
                Throw New NotImplementedException()
            End Function
        End Class

        Class ComplainAboutX
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Private Shared ReadOnly m_kindsOfInterest As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(Of SyntaxKind)(SyntaxKind.IdentifierName)

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return m_kindsOfInterest
                End Get
            End Property

            Private Shared ReadOnly CA9999_UseOfVariableThatStartsWithX As DiagnosticDescriptor = New DiagnosticDescriptor(id:="CA9999", title:="CA9999_UseOfVariableThatStartsWithX", messageFormat:="Use of variable whose name starts with 'x': '{0}'", category:="Test", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Private ReadOnly Property IDiagnosticAnalyzer_SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzer.SupportedDiagnostics
                Get
                    Return ImmutableArray.Create(CA9999_UseOfVariableThatStartsWithX)
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                Dim id = CType(node, IdentifierNameSyntax)
                If id.Identifier.ValueText.StartsWith("x") Then
                    addDiagnostic(New TestDiagnostic("CA9999_UseOfVariableThatStartsWithX", "CsTest", DiagnosticSeverity.Warning, id.GetLocation(), "Use of variable whose name starts with 'x': '{0}'", False, id.Identifier.ValueText))
                End If
            End Sub
        End Class

        <Fact>
        Public Sub TestGetEffectiveDiagnostics()
            Dim noneDiagDesciptor = New DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault:=True)
            Dim infoDiagDesciptor = New DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault:=True)
            Dim warningDiagDesciptor = New DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
            Dim errorDiagDesciptor = New DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.[Error], isEnabledByDefault:=True)

            Dim noneDiag = Microsoft.CodeAnalysis.Diagnostic.Create(noneDiagDesciptor, Location.None)
            Dim infoDiag = Microsoft.CodeAnalysis.Diagnostic.Create(infoDiagDesciptor, Location.None)
            Dim warningDiag = Microsoft.CodeAnalysis.Diagnostic.Create(warningDiagDesciptor, Location.None)
            Dim errorDiag = Microsoft.CodeAnalysis.Diagnostic.Create(errorDiagDesciptor, Location.None)

            Dim diags = New Diagnostic() {noneDiag, infoDiag, warningDiag, errorDiag}

            ' Escalate all diagnostics to error.
            Dim specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.[Error])
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.[Error])
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.[Error])
            Dim options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            Dim comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            Dim effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(diags.Length, effectiveDiags.Length)
            For Each effectiveDiag In effectiveDiags
                Assert.[True](effectiveDiag.Severity = DiagnosticSeverity.[Error] OrElse (effectiveDiag.Severity = DiagnosticSeverity.Warning AndAlso effectiveDiag.IsWarningAsError))
            Next

            ' Suppress all diagnostics.
            ' NOTE: Diagnostics with default severity error cannot be suppressed and its severity cannot be lowered.
            specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Suppress)
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Suppress)
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.Suppress)
            specificDiagOptions.Add(errorDiagDesciptor.Id, ReportDiagnostic.Suppress)
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(0, effectiveDiags.Length)

            ' Shuffle diagnostic severity.
            specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(noneDiagDesciptor.Id, ReportDiagnostic.Info)
            specificDiagOptions.Add(infoDiagDesciptor.Id, ReportDiagnostic.Hidden)
            specificDiagOptions.Add(warningDiagDesciptor.Id, ReportDiagnostic.[Error])
            specificDiagOptions.Add(errorDiagDesciptor.Id, ReportDiagnostic.Warn)
            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)

            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(diags.Length, effectiveDiags.Length)
            Dim diagIds = New HashSet(Of String)(diags.[Select](Function(d) d.Id))
            For Each effectiveDiag In effectiveDiags
                Assert.[True](diagIds.Remove(effectiveDiag.Id))

                Select Case effectiveDiag.Severity
                    Case DiagnosticSeverity.Hidden
                        Assert.Equal(infoDiagDesciptor.Id, effectiveDiag.Id)

                    Case DiagnosticSeverity.Info
                        Assert.Equal(noneDiagDesciptor.Id, effectiveDiag.Id)
                        Exit Select

                    Case DiagnosticSeverity.Warning
                        If Not effectiveDiag.IsWarningAsError Then
                            Assert.Equal(errorDiagDesciptor.Id, effectiveDiag.Id)
                        Else
                            Assert.Equal(warningDiagDesciptor.Id, effectiveDiag.Id)
                        End If

                        Exit Select

                    Case DiagnosticSeverity.[Error]
                        ' Diagnostics with default severity error cannot be suppressed and its severity cannot be lowered.
                        Assert.Equal(errorDiagDesciptor.Id, effectiveDiag.Id)
                        Exit Select
                    Case Else

                        Throw ExceptionUtilities.Unreachable
                End Select
            Next

            Assert.Empty(diagIds)

        End Sub

        <Fact>
        Sub TestGetEffectiveDiagnosticsGlobal()
            Dim noneDiagDesciptor = New DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault:=True)
            Dim infoDiagDesciptor = New DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault:=True)
            Dim warningDiagDesciptor = New DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
            Dim errorDiagDesciptor = New DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.[Error], isEnabledByDefault:=True)

            Dim noneDiag = Microsoft.CodeAnalysis.Diagnostic.Create(noneDiagDesciptor, Location.None)
            Dim infoDiag = Microsoft.CodeAnalysis.Diagnostic.Create(infoDiagDesciptor, Location.None)
            Dim warningDiag = Microsoft.CodeAnalysis.Diagnostic.Create(warningDiagDesciptor, Location.None)
            Dim errorDiag = Microsoft.CodeAnalysis.Diagnostic.Create(errorDiagDesciptor, Location.None)

            Dim diags = New Diagnostic() {noneDiag, infoDiag, warningDiag, errorDiag}

            Dim options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Default)
            Dim comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            Dim effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.IsWarningAsError))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Warn)
            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Warning))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Info)
            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Info))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Hidden)
            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(4, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Hidden))

            options = TestOptions.ReleaseDll.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(2, effectiveDiags.Length)
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Error))
            Assert.Equal(1, effectiveDiags.Count(Function(d) d.Severity = DiagnosticSeverity.Hidden))

        End Sub

        <Fact>
        Sub TestDisabledDiagnostics()
            Dim disabledDiagDescriptor = New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)
            Dim enabledDiagDescriptor = New DiagnosticDescriptor("XX002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Dim disabledDiag = CodeAnalysis.Diagnostic.Create(disabledDiagDescriptor, Location.None)
            Dim enabledDiag = CodeAnalysis.Diagnostic.Create(enabledDiagDescriptor, Location.None)

            Dim diags = {disabledDiag, enabledDiag}

            ' Verify that only the enabled diag shows up after filtering.
            Dim options = TestOptions.ReleaseDll
            Dim comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            Dim effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(1, effectiveDiags.Length)
            Assert.Contains(enabledDiag, effectiveDiags)

            ' If the disabled diag was enabled through options, then it should show up.
            Dim specificDiagOptions = New Dictionary(Of String, ReportDiagnostic)()
            specificDiagOptions.Add(disabledDiagDescriptor.Id, ReportDiagnostic.Warn)
            specificDiagOptions.Add(enabledDiagDescriptor.Id, ReportDiagnostic.Suppress)

            options = TestOptions.ReleaseDll.WithSpecificDiagnosticOptions(specificDiagOptions)
            comp = CreateCompilationWithMscorlib({""}, compOptions:=options)
            effectiveDiags = comp.GetEffectiveDiagnostics(diags).ToArray()
            Assert.Equal(1, effectiveDiags.Length)
            Assert.Contains(disabledDiag, effectiveDiags)
        End Sub

        Class FullyDisabledAnalyzer
            Implements IDiagnosticAnalyzer
            Public Shared desc1 As New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)
            Public Shared desc2 As New DiagnosticDescriptor("XX002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)

            Public ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzer.SupportedDiagnostics
                Get
                    Return ImmutableArray.Create(desc1, desc2)
                End Get
            End Property

        End Class

        Class PartiallyDisabledAnalyzer
            Implements IDiagnosticAnalyzer
            Public Shared desc1 As New DiagnosticDescriptor("XX003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=False)
            Public Shared desc2 As New DiagnosticDescriptor("XX004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzer.SupportedDiagnostics
                Get
                    Return ImmutableArray.Create(desc1, desc2)
                End Get
            End Property

        End Class

        <Fact>
        Sub TestDisabledAnalyzers()
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

        Class ModuleStatementAnalyzer
            Implements ISyntaxNodeAnalyzer(Of SyntaxKind)

            Public Shared desc1 As New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzer.SupportedDiagnostics
                Get
                    Return ImmutableArray.Create(desc1)
                End Get
            End Property

            Public ReadOnly Property SyntaxKindsOfInterest As ImmutableArray(Of SyntaxKind) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).SyntaxKindsOfInterest
                Get
                    Return ImmutableArray.Create(SyntaxKind.ModuleStatement)
                End Get
            End Property

            Public Sub AnalyzeNode(node As SyntaxNode, semanticModel As SemanticModel, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISyntaxNodeAnalyzer(Of SyntaxKind).AnalyzeNode
                Dim moduleStatement = DirectCast(node, ModuleStatementSyntax)
                addDiagnostic(CodeAnalysis.Diagnostic.Create(desc1, node.GetLocation))
            End Sub
        End Class

        <Fact>
        Sub TestModuleStatementSyntaxAnalyzer()
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
            comp.VerifyAnalyzerDiagnostics({analyzer}, Nothing,
                                           AnalyzerDiagnostic("XX001", <![CDATA[Public Module ThisModule]]>))
        End Sub

        Class MockSymbolAnalyzer
            Implements ISymbolAnalyzer

            Public Shared desc1 As New DiagnosticDescriptor("XX001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor) Implements IDiagnosticAnalyzer.SupportedDiagnostics
                Get
                    Return ImmutableArray.Create(desc1)
                End Get
            End Property

            Public ReadOnly Property SymbolKindsOfInterest As ImmutableArray(Of SymbolKind) Implements ISymbolAnalyzer.SymbolKindsOfInterest
                Get
                    Return ImmutableArray.Create(SymbolKind.NamedType)
                End Get
            End Property

            Public Sub AnalyzeSymbol(symbol As ISymbol, compilation As Compilation, addDiagnostic As Action(Of Diagnostic), options As AnalyzerOptions, cancellationToken As CancellationToken) Implements ISymbolAnalyzer.AnalyzeSymbol
                Dim sourceLoc = symbol.Locations.First(Function(l) l.IsInSource)
                addDiagnostic(CodeAnalysis.Diagnostic.Create(desc1, sourceLoc))
            End Sub
        End Class

        <WorkItem(998724)>
        <Fact>
        Sub TestSymbolAnalyzerNotInvokedForMyTemplateSymbols()
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
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing,
                                           AnalyzerDiagnostic("XX001", <![CDATA[C]]>))
        End Sub
    End Class
End Namespace
