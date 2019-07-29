' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Collections.ObjectModel
Imports System.Globalization
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class VisualBasicCompilationOptionsTests
        Inherits BasicTestBase

        ''' <summary>
        ''' Using an instance of <see cref="VisualBasicCompilationOptions"/>, tests a property in <see cref="CompilationOptions"/> , even it is hidden by <see cref="VisualBasicCompilationOptions"/>.
        ''' </summary>
        Private Sub TestHiddenProperty(Of T)(factory As Func(Of CompilationOptions, T, CompilationOptions),
                                       getter As Func(Of CompilationOptions, T),
                                       validNonDefaultValue As T)
            TestPropertyGeneric(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication), factory, getter, validNonDefaultValue)
        End Sub

        <Fact>
        Public Sub ShadowInvariants()
            TestHiddenProperty(Function(old, value) old.WithOutputKind(value), Function(opt) opt.OutputKind, OutputKind.DynamicallyLinkedLibrary)
            TestHiddenProperty(Function(old, value) old.WithModuleName(value), Function(opt) opt.ModuleName, "goo.dll")
            TestHiddenProperty(Function(old, value) old.WithMainTypeName(value), Function(opt) opt.MainTypeName, "Goo.Bar")
            TestHiddenProperty(Function(old, value) old.WithScriptClassName(value), Function(opt) opt.ScriptClassName, "<Script>")

            TestHiddenProperty(Function(old, value) old.WithOptimizationLevel(value), Function(opt) opt.OptimizationLevel, OptimizationLevel.Release)
            TestHiddenProperty(Function(old, value) old.WithOverflowChecks(value), Function(opt) opt.CheckOverflow, False)
            TestHiddenProperty(Function(old, value) old.WithCryptoKeyContainer(value), Function(opt) opt.CryptoKeyContainer, "goo")
            TestHiddenProperty(Function(old, value) old.WithCryptoKeyFile(value), Function(opt) opt.CryptoKeyFile, "goo")
            TestHiddenProperty(Function(old, value) old.WithCryptoPublicKey(value), Function(opt) opt.CryptoPublicKey, ImmutableArray.CreateRange(Of Byte)({1, 2, 3, 4}))
            TestHiddenProperty(Function(old, value) old.WithDelaySign(value), Function(opt) opt.DelaySign, True)
            TestHiddenProperty(Function(old, value) old.WithPlatform(value), Function(opt) opt.Platform, Platform.X64)
            TestHiddenProperty(Function(old, value) old.WithGeneralDiagnosticOption(value), Function(opt) opt.GeneralDiagnosticOption, ReportDiagnostic.Suppress)

            TestHiddenProperty(Function(old, value) old.WithSpecificDiagnosticOptions(value), Function(opt) opt.SpecificDiagnosticOptions,
                New Dictionary(Of String, ReportDiagnostic) From {{"VB0001", ReportDiagnostic.Error}}.ToImmutableDictionary())
            TestHiddenProperty(Function(old, value) old.WithReportSuppressedDiagnostics(value), Function(opt) opt.ReportSuppressedDiagnostics, True)

            TestHiddenProperty(Function(old, value) old.WithConcurrentBuild(value), Function(opt) opt.ConcurrentBuild, False)

            TestHiddenProperty(Function(old, value) old.WithXmlReferenceResolver(value), Function(opt) opt.XmlReferenceResolver, New XmlFileResolver(Nothing))
            TestHiddenProperty(Function(old, value) old.WithSourceReferenceResolver(value), Function(opt) opt.SourceReferenceResolver, New SourceFileResolver(ImmutableArray(Of String).Empty, Nothing))
            TestHiddenProperty(Function(old, value) old.WithMetadataReferenceResolver(value), Function(opt) opt.MetadataReferenceResolver, New TestMetadataReferenceResolver())
            TestHiddenProperty(Function(old, value) old.WithAssemblyIdentityComparer(value), Function(opt) opt.AssemblyIdentityComparer, New DesktopAssemblyIdentityComparer(New AssemblyPortabilityPolicy()))
        End Sub

        Private Sub TestProperty(Of T)(factory As Func(Of VisualBasicCompilationOptions, T, VisualBasicCompilationOptions),
                                       getter As Func(Of VisualBasicCompilationOptions, T),
                                       validNonDefaultValue As T)
            TestPropertyGeneric(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication), factory, getter, validNonDefaultValue)
        End Sub

        Private Shared Sub TestPropertyGeneric(Of TOptions As CompilationOptions, T)(oldOptions As TOptions,
                                                     factory As Func(Of TOptions, T, TOptions),
                                                     getter As Func(Of TOptions, T), validNonDefaultValue As T)
            Dim validDefaultValue = getter(oldOptions)

            '  we need non-default value to test Equals And GetHashCode
            Assert.NotEqual(validNonDefaultValue, validDefaultValue)

            ' check that the assigned value can be read
            Dim newOpt1 = factory(oldOptions, validNonDefaultValue)
            Assert.Equal(validNonDefaultValue, getter(newOpt1))
            Assert.Equal(0, newOpt1.Errors.Length)

            'check that creating new options with the same value yields the same options instance
            Dim newOpt1_alias = factory(newOpt1, validNonDefaultValue)
            Assert.Same(newOpt1_alias, newOpt1)

            ' check that Equals And GetHashCode work
            Dim newOpt2 = factory(oldOptions, validNonDefaultValue)
            Assert.False(newOpt1.Equals(oldOptions))
            Assert.True(newOpt1.Equals(newOpt2))

            Assert.Equal(newOpt1.GetHashCode(), newOpt2.GetHashCode())

            ' test Nothing:
            Assert.NotNull(factory(oldOptions, Nothing))
        End Sub

        <Fact>
        Public Sub Invariants()
            TestProperty(Function(old, value) old.WithOutputKind(value), Function(opt) opt.OutputKind, OutputKind.DynamicallyLinkedLibrary)
            TestProperty(Function(old, value) old.WithModuleName(value), Function(opt) opt.ModuleName, "goo.dll")
            TestProperty(Function(old, value) old.WithMainTypeName(value), Function(opt) opt.MainTypeName, "Goo.Bar")
            TestProperty(Function(old, value) old.WithScriptClassName(value), Function(opt) opt.ScriptClassName, "<Script>")

            TestProperty(Function(old, value) old.WithGlobalImports(value), Function(opt) opt.GlobalImports,
                ImmutableArray.Create(GlobalImport.Parse("Goo.Bar"), GlobalImport.Parse("Baz")))

            TestProperty(Function(old, value) old.WithRootNamespace(value), Function(opt) opt.RootNamespace, "A.B.C")
            TestProperty(Function(old, value) old.WithOptionStrict(value), Function(opt) opt.OptionStrict, OptionStrict.On)
            TestProperty(Function(old, value) old.WithOptionInfer(value), Function(opt) opt.OptionInfer, False)
            TestProperty(Function(old, value) old.WithOptionExplicit(value), Function(opt) opt.OptionExplicit, False)
            TestProperty(Function(old, value) old.WithOptionCompareText(value), Function(opt) opt.OptionCompareText, True)

            TestProperty(Function(old, value) old.WithParseOptions(value), Function(opt) opt.ParseOptions,
                         New VisualBasicParseOptions(kind:=SourceCodeKind.Script))

            TestProperty(Function(old, value) old.WithEmbedVbCoreRuntime(value), Function(opt) opt.EmbedVbCoreRuntime, True)
            TestProperty(Function(old, value) old.WithOptimizationLevel(value), Function(opt) opt.OptimizationLevel, OptimizationLevel.Release)
            TestProperty(Function(old, value) old.WithOverflowChecks(value), Function(opt) opt.CheckOverflow, False)
            TestProperty(Function(old, value) old.WithCryptoKeyContainer(value), Function(opt) opt.CryptoKeyContainer, "goo")
            TestProperty(Function(old, value) old.WithCryptoKeyFile(value), Function(opt) opt.CryptoKeyFile, "goo")
            TestProperty(Function(old, value) old.WithCryptoPublicKey(value), Function(opt) opt.CryptoPublicKey, ImmutableArray.CreateRange(Of Byte)({1, 2, 3, 4}))
            TestProperty(Function(old, value) old.WithDelaySign(value), Function(opt) opt.DelaySign, True)
            TestProperty(Function(old, value) old.WithPlatform(value), Function(opt) opt.Platform, Platform.X64)
            TestProperty(Function(old, value) old.WithGeneralDiagnosticOption(value), Function(opt) opt.GeneralDiagnosticOption, ReportDiagnostic.Suppress)

            TestProperty(Function(old, value) old.WithSpecificDiagnosticOptions(value), Function(opt) opt.SpecificDiagnosticOptions,
                New Dictionary(Of String, ReportDiagnostic) From {{"VB0001", ReportDiagnostic.Error}}.ToImmutableDictionary())
            TestProperty(Function(old, value) old.WithReportSuppressedDiagnostics(value), Function(opt) opt.ReportSuppressedDiagnostics, True)

            TestProperty(Function(old, value) old.WithConcurrentBuild(value), Function(opt) opt.ConcurrentBuild, False)
            TestProperty(Function(old, value) old.WithCurrentLocalTime(value), Function(opt) opt.CurrentLocalTime, #2015/1/1#)
            TestProperty(Function(old, value) old.WithDebugPlusMode(value), Function(opt) opt.DebugPlusMode, True)

            TestProperty(Function(old, value) old.WithXmlReferenceResolver(value), Function(opt) opt.XmlReferenceResolver, New XmlFileResolver(Nothing))
            TestProperty(Function(old, value) old.WithSourceReferenceResolver(value), Function(opt) opt.SourceReferenceResolver, New SourceFileResolver(ImmutableArray(Of String).Empty, Nothing))
            TestProperty(Function(old, value) old.WithMetadataReferenceResolver(value), Function(opt) opt.MetadataReferenceResolver, New TestMetadataReferenceResolver())
            TestProperty(Function(old, value) old.WithAssemblyIdentityComparer(value), Function(opt) opt.AssemblyIdentityComparer, New DesktopAssemblyIdentityComparer(New AssemblyPortabilityPolicy()))
            TestProperty(Function(old, value) old.WithStrongNameProvider(value), Function(opt) opt.StrongNameProvider, New DesktopStrongNameProvider())
        End Sub

        <Fact>
        Public Sub WithXxx()
            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName(Nothing).Errors,
<expected>
BC2014: the value 'Nothing' is invalid for option 'ScriptClassName'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName("blah" & ChrW(0) & "goo").Errors,
<expected>
BC2014: the value '<%= "blah" & ChrW(0) & "goo" %>' is invalid for option 'ScriptClassName'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithScriptClassName("").Errors,
<expected>
BC2014: the value '' is invalid for option 'ScriptClassName'
</expected>)

            Assert.True(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName(Nothing).Errors.IsEmpty)
            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName("blah" & ChrW(0) & "goo").Errors,
<expected>
BC2014: the value '<%= "blah" & ChrW(0) & "goo" %>' is invalid for option 'MainTypeName'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithMainTypeName("").Errors,
<expected>
BC2014: the value '' is invalid for option 'MainTypeName'
</expected>)

            Assert.True(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace(Nothing).Errors.IsEmpty)
            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("blah" & ChrW(0) & "goo").Errors,
<expected>
BC2014: the value '<%= "blah" & ChrW(0) & "goo" %>' is invalid for option 'RootNamespace'
</expected>)

            Assert.True(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace("").Errors.IsEmpty)

            Assert.Equal(0, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("Goo.Bar")).WithGlobalImports(DirectCast(Nothing, IEnumerable(Of GlobalImport))).GlobalImports.Count)
            Assert.Equal(0, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse("Goo.Bar")).WithGlobalImports(DirectCast(Nothing, GlobalImport())).GlobalImports.Count)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOutputKind(CType(Int32.MaxValue, OutputKind)).Errors,
<expected>
BC2014: the value '<%= Int32.MaxValue %>' is invalid for option 'OutputKind'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOutputKind(CType(Int32.MinValue, OutputKind)).Errors,
<expected>
BC2014: the value '<%= Int32.MinValue %>' is invalid for option 'OutputKind'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptimizationLevel(CType(Int32.MaxValue, OptimizationLevel)).Errors,
<expected>
BC2014: the value '<%= Int32.MaxValue %>' is invalid for option 'OptimizationLevel'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptimizationLevel(CType(Int32.MinValue, OptimizationLevel)).Errors,
<expected>
BC2014: the value '<%= Int32.MinValue %>' is invalid for option 'OptimizationLevel'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithOptionStrict(CType(3, OptionStrict)).Errors,
<expected>
BC2014: the value '3' is invalid for option 'OptionStrict'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(CType(Int32.MaxValue, Platform)).Errors,
<expected>
BC2014: the value '<%= Int32.MaxValue %>' is invalid for option 'Platform'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithPlatform(CType(Int32.MinValue, Platform)).Errors,
<expected>
BC2014: the value '<%= Int32.MinValue %>' is invalid for option 'Platform'
</expected>)

            Assert.Equal(Nothing, TestOptions.ReleaseDll.WithModuleName("goo").WithModuleName(Nothing).ModuleName)
            AssertTheseDiagnostics(TestOptions.ReleaseDll.WithModuleName("").Errors,
<expected>
BC37206: Invalid module name: Name cannot be empty.
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseDll.WithModuleName("a\0a").Errors,
<expected>
BC37206: Invalid module name: Name contains invalid characters.
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseDll.WithModuleName("a\uD800b").Errors,
<expected>
BC37206: Invalid module name: Name contains invalid characters.
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseDll.WithModuleName("a\\b").Errors,
<expected>
BC37206: Invalid module name: Name contains invalid characters.
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseDll.WithModuleName("a/b").Errors,
<expected>
BC37206: Invalid module name: Name contains invalid characters.
</expected>)

            AssertTheseDiagnostics(TestOptions.ReleaseDll.WithModuleName("a:b").Errors,
<expected>
BC37206: Invalid module name: Name contains invalid characters.
</expected>)
        End Sub

        <Fact>
        Public Sub ConstructorValidation()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, scriptClassName:=Nothing)
            Assert.Equal("Script", options.ScriptClassName)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, scriptClassName:="blah" & ChrW(0) & "goo").Errors,
<expected>
BC2014: the value '<%= "blah" & ChrW(0) & "goo" %>' is invalid for option 'ScriptClassName'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, scriptClassName:="").Errors,
<expected>
BC2014: the value '' is invalid for option 'ScriptClassName'
</expected>)


            Assert.True(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, mainTypeName:=Nothing).Errors.IsEmpty)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, mainTypeName:=("blah" & ChrW(0) & "goo")).Errors,
<expected>
BC2014: the value '<%= "blah" & ChrW(0) & "goo" %>' is invalid for option 'MainTypeName'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, mainTypeName:="").Errors,
<expected>
BC2014: the value '' is invalid for option 'MainTypeName'
</expected>)


            Assert.True(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, rootNamespace:=Nothing).Errors.IsEmpty)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, rootNamespace:=("blah" & ChrW(0) & "goo")).Errors,
<expected>
BC2014: the value '<%= "blah" & ChrW(0) & "goo" %>' is invalid for option 'RootNamespace'
</expected>)

            Assert.True(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, rootNamespace:="").Errors.IsEmpty)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(outputKind:=CType(Int32.MaxValue, OutputKind)).Errors,
<expected>
BC2014: the value '<%= Int32.MaxValue %>' is invalid for option 'OutputKind'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(outputKind:=CType(Int32.MinValue, OutputKind)).Errors,
<expected>
BC2014: the value '<%= Int32.MinValue %>' is invalid for option 'OutputKind'
</expected>)


            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=CType(Int32.MaxValue, OptimizationLevel)).Errors,
<expected>
BC2014: the value '<%= Int32.MaxValue %>' is invalid for option 'OptimizationLevel'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optimizationLevel:=CType(Int32.MinValue, OptimizationLevel)).Errors,
<expected>
BC2014: the value '<%= Int32.MinValue %>' is invalid for option 'OptimizationLevel'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, optionStrict:=CType(3, OptionStrict)).Errors,
<expected>
BC2014: the value '3' is invalid for option 'OptionStrict'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, platform:=CType(Int32.MaxValue, Platform)).Errors,
<expected>
BC2014: the value '<%= Int32.MaxValue %>' is invalid for option 'Platform'
</expected>)

            AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication, platform:=CType(Int32.MinValue, Platform)).Errors,
<expected>
BC2014: the value '<%= Int32.MinValue %>' is invalid for option 'Platform'
</expected>)
        End Sub

        ' Make sure the given root namespace is good and parses as expected
        Private Sub CheckRootNamespaceIsGood(rootNs As String, rootNsArray As String())
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace(rootNs)

            Assert.Equal(options.RootNamespace, rootNs)
            Assert.True(options.Errors.IsEmpty)
        End Sub

        ' Make sure the given root namespace is bad, the correct error is generated, and
        ' we have an empty root namespace as a result.
        Private Sub CheckRootNamespaceIsBad(rootNs As String)
            If rootNs Is Nothing Then
                Assert.True(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace(rootNs).Errors.IsEmpty)
            Else
                AssertTheseDiagnostics(New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithRootNamespace(rootNs).Errors,
<expected>
BC2014: the value '<%= rootNs %>' is invalid for option 'RootNamespace'
</expected>)
            End If
        End Sub

        <Fact>
        Public Sub TestRootNamespace()
            CheckRootNamespaceIsGood("", {})
            CheckRootNamespaceIsGood("Goo", {"Goo"})
            CheckRootNamespaceIsGood("Goo.Bar", {"Goo", "Bar"})
            CheckRootNamespaceIsGood("Goo.Bar.q9", {"Goo", "Bar", "q9"})

            CheckRootNamespaceIsBad(Nothing)
            CheckRootNamespaceIsBad(" ")
            CheckRootNamespaceIsBad(".")
            CheckRootNamespaceIsBad("Goo.")
            CheckRootNamespaceIsBad("Goo. Bar")
            CheckRootNamespaceIsBad(".Goo")
            CheckRootNamespaceIsBad("X.7Y")
            CheckRootNamespaceIsBad("#")
            CheckRootNamespaceIsBad("A.$B")
        End Sub

        Private Sub CheckImportsAreGood(importStrings As String())
            Dim opt = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithGlobalImports(GlobalImport.Parse(importStrings))

            Assert.Equal(importStrings.Length, opt.GlobalImports.Count)
            For i = 0 To importStrings.Length - 1
                Assert.Equal(importStrings(i).Trim(), opt.GlobalImports(i).Clause.ToString)
            Next
        End Sub

        Private Sub CheckImportsAreBad(importStrings As String(), expectedErrors As String())
            Assert.Throws(Of ArgumentException)(Function() GlobalImport.Parse(importStrings))

            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            Dim globalImports = GlobalImport.Parse(importStrings, diagnostics)

            Assert.Equal(0, globalImports.Count)

            Assert.NotNull(diagnostics)
            Assert.NotEmpty(diagnostics)

            Dim errorTexts = (From e In diagnostics Let text = e.GetMessage(CultureInfo.GetCultureInfo("en")) Order By text Select text).ToArray()
            Dim expectedTexts = (From e In expectedErrors Order By e Select e).ToArray()

            For i = 0 To diagnostics.Length - 1
                Assert.Equal(expectedTexts(i), errorTexts(i))
            Next
        End Sub

        <Fact>
        Public Sub TestImports()
            CheckImportsAreGood({})
            CheckImportsAreGood({"A.B", "G.F(Of G)", "Q", "A = G.X"})

            CheckImportsAreBad({"A.B.435",
                               "Global.Goo"},
                                {"Error in project-level import 'A.B.435' at '.435' : End of statement expected.",
                                "Error in project-level import 'Global.Goo' at 'Global' : 'Global' not allowed in this context; identifier expected."})
        End Sub

        <Fact>
        Public Sub TestGlobalOptionsParseReturnsNonNullDiagnostics()
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Nothing
            Dim globalImports = GlobalImport.Parse({"System"}, diagnostics)

            Assert.Equal(1, globalImports.Count())
            Assert.NotNull(diagnostics)
            Assert.Empty(diagnostics)
        End Sub

        <Fact>
        Public Sub WarningTest()
            Assert.Equal(0, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication).WithSpecificDiagnosticOptions(Nothing).SpecificDiagnosticOptions.Count)

            Dim source =
                <compilation name="WarningTest">
                    <file name="a.vb">
Module Program
    Sub Main(args As String())
        Dim x As Integer
        Dim y As Integer
        Const z As Long = 0
    End Sub

    Function goo()
    End Function
End Module
                    </file>
                </compilation>

            ' Baseline
            Dim commonoption = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)
            Dim comp = CreateCompilationWithMscorlib40AndVBRuntime(source, commonoption)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x"),
                Diagnostic(ERRID.WRN_UnusedLocal, "y").WithArguments("y"),
                Diagnostic(ERRID.WRN_UnusedLocalConst, "z").WithArguments("z"),
                Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo"))

            ' Suppress All
            ' vbc a.vb /nowarn
            Dim options = commonoption.WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics()

            ' Suppress 42024
            ' vbc a.vb /nowarn:42024
            Dim warnings As IDictionary(Of String, ReportDiagnostic) = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
            options = commonoption.WithSpecificDiagnosticOptions(New ReadOnlyDictionary(Of String, ReportDiagnostic)(warnings))
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UnusedLocalConst, "z").WithArguments("z"),
                Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo"))

            ' Suppress 42024, 42099
            ' vbc a.vb /nowarn:42024,42099
            warnings = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42099), ReportDiagnostic.Suppress)
            options = commonoption.WithSpecificDiagnosticOptions(New ReadOnlyDictionary(Of String, ReportDiagnostic)(warnings))
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics(Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo"))

            ' Treat All as Errors
            ' vbc a.vb /warnaserror
            options = commonoption.WithGeneralDiagnosticOption(ReportDiagnostic.Error)
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x").WithWarningAsError(True),
                Diagnostic(ERRID.WRN_UnusedLocal, "y").WithArguments("y").WithWarningAsError(True),
                Diagnostic(ERRID.WRN_UnusedLocalConst, "z").WithArguments("z").WithWarningAsError(True),
                Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo").WithWarningAsError(True))

            ' Treat 42105 as Error
            ' vbc a.vb /warnaserror:42105
            warnings = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42105), ReportDiagnostic.Error)
            options = commonoption.WithSpecificDiagnosticOptions(New ReadOnlyDictionary(Of String, ReportDiagnostic)(warnings))
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x"),
                Diagnostic(ERRID.WRN_UnusedLocal, "y").WithArguments("y"),
                Diagnostic(ERRID.WRN_UnusedLocalConst, "z").WithArguments("z"),
                Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo").WithWarningAsError(True))

            ' Treat 42105 and 42099 as Errors
            ' vbc a.vb /warnaserror:42105,42099
            warnings = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42105), ReportDiagnostic.Error)
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42099), ReportDiagnostic.Error)
            options = commonoption.WithSpecificDiagnosticOptions(New ReadOnlyDictionary(Of String, ReportDiagnostic)(warnings))
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UnusedLocal, "x").WithArguments("x"),
                Diagnostic(ERRID.WRN_UnusedLocal, "y").WithArguments("y"),
                Diagnostic(ERRID.WRN_UnusedLocalConst, "z").WithArguments("z").WithWarningAsError(True),
                Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo").WithWarningAsError(True))

            ' Treat All as Errors but Suppress 42024
            ' vbc a.vb /warnaserror /nowarn:42024
            warnings = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Suppress)
            options = commonoption.WithSpecificDiagnosticOptions(New ReadOnlyDictionary(Of String, ReportDiagnostic)(warnings)).WithGeneralDiagnosticOption(ReportDiagnostic.Error)
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics(
                Diagnostic(ERRID.WRN_UnusedLocalConst, "z").WithArguments("z").WithWarningAsError(True),
                Diagnostic(ERRID.WRN_DefAsgNoRetValFuncRef1, "End Function").WithArguments("goo").WithWarningAsError(True))

            ' Suppress All with treating 42024 as an error, which will be ignored
            ' vbc a.vb /warnaserror:42024 /nowarn or
            ' vbc a.vb /nowarn /warnaserror
            warnings = New Dictionary(Of String, ReportDiagnostic)()
            warnings.Add(MessageProvider.Instance.GetIdForErrorCode(42024), ReportDiagnostic.Error)
            options = commonoption.WithSpecificDiagnosticOptions(New ReadOnlyDictionary(Of String, ReportDiagnostic)(warnings)).WithGeneralDiagnosticOption(ReportDiagnostic.Suppress)
            comp = CreateCompilationWithMscorlib40AndVBRuntime(source, options)
            comp.VerifyDiagnostics()

        End Sub

        <Fact, WorkItem(529809, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529809")>
        Public Sub NetModuleWithVbCore()
            Dim options As New VisualBasicCompilationOptions(OutputKind.NetModule, embedVbCoreRuntime:=True)

            Assert.Equal(2042, options.Errors.Single().Code)

            AssertTheseDiagnostics(CreateCompilationWithMscorlib40AndVBRuntime(<compilation><file/></compilation>, options),
                                   <expected>
BC2042: The options /vbruntime* and /target:module cannot be combined.
                                   </expected>)
        End Sub

        ''' <summary>
        ''' If this test fails, please update the <see cref="VisualBasicCompilationOptions.GetHashCode" />
        ''' and <see cref="VisualBasicCompilationOptions.Equals" /> methods and
        ''' <see cref="VisualBasicCompilationOptions"/> constructor(s) to
        ''' make sure they are doing the right thing with your new field And then update the baseline
        ''' here.
        ''' </summary>
        <Fact>
        Public Sub TestFieldsForEqualsAndGetHashCode()
            ReflectionAssert.AssertPublicAndInternalFieldsAndProperties(
                (GetType(VisualBasicCompilationOptions)),
                "GlobalImports",
                "Language",
                "RootNamespace",
                "OptionStrict",
                "OptionInfer",
                "OptionExplicit",
                "OptionCompareText",
                "EmbedVbCoreRuntime",
                "SuppressEmbeddedDeclarations",
                "NullableContextOptions",
                "ParseOptions",
                "IgnoreCorLibraryDuplicatedTypes")
        End Sub

        <Fact>
        Public Sub WithCryptoPublicKey()
            Dim options = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)

            Assert.Equal(ImmutableArray(Of Byte).Empty, options.CryptoPublicKey)
            Assert.Equal(ImmutableArray(Of Byte).Empty, options.WithCryptoPublicKey(Nothing).CryptoPublicKey)

            Assert.Same(options, options.WithCryptoPublicKey(Nothing))
            Assert.Same(options, options.WithCryptoPublicKey(ImmutableArray(Of Byte).Empty))
        End Sub

        <Fact>
        Public Sub WithIgnoreCorLibraryDuplicatedTypes()
            Dim optionFalse = New VisualBasicCompilationOptions(OutputKind.ConsoleApplication)
            Dim optionTrue = optionFalse.WithIgnoreCorLibraryDuplicatedTypes(True)

            Assert.False(optionFalse.IgnoreCorLibraryDuplicatedTypes)
            Assert.True(optionTrue.IgnoreCorLibraryDuplicatedTypes)

            Assert.Same(optionFalse, optionFalse.WithIgnoreCorLibraryDuplicatedTypes(False))

            Assert.True(optionTrue.Equals(optionFalse.WithIgnoreCorLibraryDuplicatedTypes(True)))
            Assert.False(optionTrue.Equals(optionFalse))

            Dim optionTrueClone = New VisualBasicCompilationOptions(optionTrue)
            Assert.Equal(optionTrue, optionTrueClone)
            Assert.NotEqual(optionFalse, optionTrueClone)
        End Sub

    End Class
End Namespace
