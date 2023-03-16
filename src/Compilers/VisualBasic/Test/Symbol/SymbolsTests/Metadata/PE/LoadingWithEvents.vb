' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices
Imports CompilationCreationTestHelpers
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Test.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests.Symbols.Metadata.PE

    Public Class LoadingWithEvents : Inherits BasicTestBase

        <Fact>
        Public Sub LoadingSimpleWithEvents1()
            Dim source =
<compilation name="LoadingSimpleWithEvents1">
    <file name="a.vb">
    </file>
</compilation>
            Dim simpleWithEvents = MetadataReference.CreateFromImage(TestResources.SymbolsTests.WithEvents.SimpleWithEvents.AsImmutableOrNull())
            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, {simpleWithEvents}, TestOptions.ReleaseExe)

            Dim ns = DirectCast(c1.GlobalNamespace.GetMembers("SimpleWithEvents").Single, NamespaceSymbol)

            Dim Class1 = ns.GetTypeMembers("Class1").Single

            Dim Class1_WE1 = DirectCast(Class1.GetMember("WE1"), PropertySymbol)
            Dim Class1_WE2 = DirectCast(Class1.GetMember("WE2"), PropertySymbol)

            Assert.True(Class1_WE1.IsWithEvents)
            Assert.True(Class1_WE2.IsWithEvents)

        End Sub

        <Fact>
        Public Sub LoadingSimpleWithEventsDerived()
            Dim source =
<compilation name="LoadingSimpleWithEvents1">
    <file name="a.vb">
    </file>
</compilation>
            Dim ref = MetadataReference.CreateFromImage(TestResources.SymbolsTests.WithEvents.SimpleWithEvents.AsImmutableOrNull())
            Dim c1 = CompilationUtils.CreateCompilationWithMscorlib40AndVBRuntime(source, {ref}, TestOptions.ReleaseExe)

            Dim ns = DirectCast(c1.GlobalNamespace.GetMembers("SimpleWithEvents").Single, NamespaceSymbol)

            Dim Derived = ns.GetTypeMembers("Derived").Single

            Dim Derived_WE1 = DirectCast(Derived.GetMember("WE1"), PropertySymbol)

            Assert.True(Derived_WE1.IsWithEvents)
            Assert.True(Derived_WE1.IsOverrides)
            Assert.True(Derived_WE1.OverriddenProperty.IsWithEvents)

        End Sub

    End Class

End Namespace

