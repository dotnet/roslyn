' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Diagnostics.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Semantics

    Partial Public Class DiagnosticAnalyzerTests
        Inherits BasicTestBase

        <Serializable>
        Class TestDiagnostic
            Inherits Diagnostic
            Implements ISerializable

            Private ReadOnly m_kind As String
            Private ReadOnly m_severity As DiagnosticSeverity
            Private ReadOnly m_location As Location
            Private ReadOnly m_message As String
            Private ReadOnly m_isWarningAsError As Boolean
            Private ReadOnly m_arguments As Object()
            Private ReadOnly m_descriptor As DiagnosticDescriptor

            Public Sub New(id As String,
                           kind As String,
                           severity As DiagnosticSeverity,
                           location As Location,
                           message As String,
                           ParamArray arguments As Object())
                Me.New(New DiagnosticDescriptor(id, String.Empty, message, id, severity, True), kind, severity, location, message, arguments)
            End Sub

            Private Sub New(info As SerializationInfo, context As StreamingContext)
                Dim id = info.GetString("id")
                Me.m_kind = info.GetString("kind")
                Me.m_message = info.GetString("message")
                Me.m_location = CType(info.GetValue("location", GetType(Location)), Location)
                Me.m_severity = CType(info.GetValue("severity", GetType(DiagnosticSeverity)), DiagnosticSeverity)
                Dim defaultSeverity = CType(info.GetValue("defaultSeverity", GetType(DiagnosticSeverity)), DiagnosticSeverity)
                Me.m_arguments = CType(info.GetValue("arguments", GetType(Object())), Object())
                Me.m_descriptor = New DiagnosticDescriptor(id, String.Empty, m_message, id, defaultSeverity, True)
            End Sub

            Private Sub New(descriptor As DiagnosticDescriptor,
                           kind As String,
                           severity As DiagnosticSeverity,
                           location As Location,
                           message As String,
                           ParamArray arguments As Object())
                Me.m_descriptor = descriptor
                Me.m_kind = kind
                Me.m_severity = severity
                Me.m_location = location
                Me.m_message = message
                Me.m_arguments = arguments
            End Sub

            Public Overrides ReadOnly Property AdditionalLocations As IReadOnlyList(Of Location)
                Get
                    Dim loc As Location() = New Location(0) {}
                    Return loc
                End Get
            End Property

            Public Overrides ReadOnly Property Id As String
                Get
                    Return m_descriptor.Id
                End Get
            End Property

            Public Overrides ReadOnly Property Descriptor As DiagnosticDescriptor
                Get
                    Return m_descriptor
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

            Public Overrides ReadOnly Property WarningLevel As Integer
                Get
                    Return 2
                End Get
            End Property

            Public Sub GetObjectData(info As SerializationInfo, context As StreamingContext) Implements ISerializable.GetObjectData
                info.AddValue("id", Me.m_descriptor.Id)
                info.AddValue("kind", Me.m_kind)
                info.AddValue("message", Me.m_message)
                info.AddValue("location", Me.m_location, GetType(Location))
                info.AddValue("severity", Me.m_severity, GetType(DiagnosticSeverity))
                info.AddValue("defaultSeverity", Me.m_descriptor.DefaultSeverity, GetType(DiagnosticSeverity))
                info.AddValue("arguments", Me.m_arguments, GetType(Object()))
            End Sub

            Friend Overrides Function WithLocation(location As Location) As Diagnostic
                Throw New NotImplementedException()
            End Function

            Public Overrides Function GetMessage(Optional formatProvider As IFormatProvider = Nothing) As String
                Return String.Format(m_message, m_arguments)
            End Function

            Public Overrides Function GetHashCode() As Integer
                Return Hash.Combine(Me.m_descriptor.GetHashCode(), Me.m_kind.GetHashCode())
            End Function

            Public Overloads Overrides Function Equals(obj As Object) As Boolean
                Return Me.Equals(TryCast(obj, TestDiagnostic))
            End Function

            Public Overloads Overrides Function Equals(obj As Diagnostic) As Boolean
                Return Me.Equals(TryCast(obj, TestDiagnostic))
            End Function

            Public Overloads Function Equals(other As TestDiagnostic) As Boolean
                If other Is Nothing OrElse Me.GetType() <> other.GetType() Then Return False
                Return Me.m_descriptor.Id = other.m_descriptor.Id AndAlso
                    Me.m_kind = other.m_kind AndAlso
                    Me.m_location = other.m_location AndAlso
                    Me.m_message = other.m_message AndAlso
                    SameData(Me.m_arguments, other.m_arguments)
            End Function

            Private Shared Function SameData(d1 As Object(), d2 As Object()) As Boolean
                Return (d1 Is Nothing) = (d2 Is Nothing) AndAlso (d1 Is Nothing OrElse d1.SequenceEqual(d2))
            End Function

            Friend Overrides Function WithSeverity(severity As DiagnosticSeverity) As Diagnostic
                Return New TestDiagnostic(Me.m_descriptor.Id, Me.m_kind, severity, Me.m_location, Me.m_message, Me.m_arguments)
            End Function
        End Class

        Class ComplainAboutX
            Inherits DiagnosticAnalyzer

            Private Shared ReadOnly CA9999_UseOfVariableThatStartsWithX As DiagnosticDescriptor = New DiagnosticDescriptor(id:="CA9999", title:="CA9999_UseOfVariableThatStartsWithX", messageFormat:="Use of variable whose name starts with 'x': '{0}'", category:="Test", defaultSeverity:=DiagnosticSeverity.Warning, isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(CA9999_UseOfVariableThatStartsWithX)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.IdentifierName)
            End Sub

            Public Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
                Dim id = CType(context.Node, IdentifierNameSyntax)
                If id.Identifier.ValueText.StartsWith("x", StringComparison.Ordinal) Then
                    context.ReportDiagnostic(New TestDiagnostic("CA9999_UseOfVariableThatStartsWithX", "CsTest", DiagnosticSeverity.Warning, id.GetLocation(), "Use of variable whose name starts with 'x': '{0}'", False, id.Identifier.ValueText))
                End If
            End Sub
        End Class

        <Fact>
        Public Sub TestGetEffectiveDiagnostics()
            Dim noneDiagDesciptor = New DiagnosticDescriptor("XX0001", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Hidden, isEnabledByDefault:=True)
            Dim infoDiagDesciptor = New DiagnosticDescriptor("XX0002", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Info, isEnabledByDefault:=True)
            Dim warningDiagDesciptor = New DiagnosticDescriptor("XX0003", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Warning, isEnabledByDefault:=True)
            Dim errorDiagDesciptor = New DiagnosticDescriptor("XX0004", "DummyDescription", "DummyMessage", "DummyCategory", DiagnosticSeverity.Error, isEnabledByDefault:=True)

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
                Assert.True(effectiveDiag.Severity = DiagnosticSeverity.Error)
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
                        Assert.Equal(errorDiagDesciptor.Id, effectiveDiag.Id)
                        Exit Select

                    Case DiagnosticSeverity.Error
                        Assert.Equal(warningDiagDesciptor.Id, effectiveDiag.Id)
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

        Class PartiallyDisabledAnalyzer
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
            comp.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                                           AnalyzerDiagnostic("XX001", <![CDATA[Public Module ThisModule]]>))
        End Sub

        Class MockSymbolAnalyzer
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
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False,
                                           AnalyzerDiagnostic("XX001", <![CDATA[C]]>))
        End Sub

        Class NamespaceAndTypeNodeAnalyzer
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
        Sub TestSyntaxAnalyzerInvokedForNamespaceBlockAndClassBlock()
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
                                           AnalyzerDiagnostic("XX001", <![CDATA[N]]>),
                                           AnalyzerDiagnostic("XX001", <![CDATA[C]]>))
        End Sub

        Private Class CodeBlockAnalyzer
            Inherits DiagnosticAnalyzer

            Private Shared Descriptor As DiagnosticDescriptor = DescriptorFactory.CreateSimpleDescriptor("CodeBlockDiagnostic")

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Descriptor)
                End Get
            End Property

            Public Overrides Sub Initialize(context As AnalysisContext)
                context.RegisterCodeBlockAction(AddressOf OnCodeBlock)
            End Sub

            Private Shared Sub OnCodeBlock(context As CodeBlockAnalysisContext)
                context.ReportDiagnostic(CodeAnalysis.Diagnostic.Create(Descriptor, context.OwningSymbol.DeclaringSyntaxReferences.First.GetLocation))
            End Sub
        End Class

        <Fact, WorkItem(1008059)>
        Sub TestCodeBlockAnalyzersForNoExecutableCode()
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

        <Fact, WorkItem(1008059)>
        Sub TestCodeBlockAnalyzersForEmptyMethodBody()
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
            compilation.VerifyAnalyzerDiagnostics({analyzer}, Nothing, Nothing, False, AnalyzerDiagnostic("CodeBlockDiagnostic", <![CDATA[Public Sub Method()]]>))
        End Sub

        <Fact, WorkItem(1096600)>
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

        Class FieldSymbolAnalyzer
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

        <Fact, WorkItem(1109126)>
        Sub TestFieldSymbolAnalyzer_EnumField()
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
                    AnalyzerDiagnostic("FieldSymbolDiagnostic", <![CDATA[X]]>))
        End Sub

        <Fact, WorkItem(1111667)>
        Sub TestFieldSymbolAnalyzer_FieldWithoutInitializer()
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
                    AnalyzerDiagnostic("FieldSymbolDiagnostic", <![CDATA[Field]]>))
        End Sub

        Class FieldDeclarationAnalyzer
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

        <Fact, WorkItem(565)>
        Sub TestFieldDeclarationAnalyzer()
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
                    AnalyzerDiagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim x, y As Integer]]>),
                    AnalyzerDiagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim z As Integer]]>),
                    AnalyzerDiagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim x2 = 0, y2 = 0]]>),
                    AnalyzerDiagnostic("FieldDeclarationDiagnostic", <![CDATA[Dim z2 = 0]]>))
        End Sub
    End Class
End Namespace
