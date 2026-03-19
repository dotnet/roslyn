' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    <Trait(Traits.Feature, Traits.Features.NamingStyle)>
    Partial Public Class NamingStyleTests
        <Fact>
        Public Sub TestEmptyStringWithPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="_")
            TestNameCreation(namingStyle, "_")
        End Sub

        <Fact>
        Public Sub TestEmptyStringWithSuffix()
            Dim namingStyle = CreateNamingStyle(suffix:="_")
            TestNameCreation(namingStyle, "_")
        End Sub

        <Fact>
        Public Sub TestEmptyStringWithPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="_")
            TestNameCreation(namingStyle, "__")
        End Sub

        <Fact>
        Public Sub TestSingleWordWithPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_Pascal", "Pascal")
        End Sub

        <Fact>
        Public Sub TestSingleWordWithSuffix()
            Dim namingStyle = CreateNamingStyle(suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal_", "Pascal")
        End Sub

        <Fact>
        Public Sub TestSingleWordWithPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_Pascal_", "Pascal")
        End Sub

        <Fact>
        Public Sub TestSingleWordWithDifferentPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="__", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_Pascal__", "Pascal")
        End Sub

        <Fact>
        Public Sub TestMultipleWordsWithPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_PascalCase", "Pascal", "Case")
        End Sub

        <Fact>
        Public Sub TestMultipleWordsWithSuffix()
            Dim namingStyle = CreateNamingStyle(suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "PascalCase_", "Pascal", "Case")
        End Sub

        <Fact>
        Public Sub TestMultipleWordsWithPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_PascalCase_", "Pascal", "Case")
        End Sub

        <Fact>
        Public Sub TestMultipleWordsWithDifferentPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="__", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_PascalCase__", "Pascal", "Case")
        End Sub

        <Fact>
        Public Sub TestPrefixAndSuffixBothApplyEvenWhenOverlapping()
            Dim namingStyle = CreateNamingStyle(prefix:="prefix", suffix:="suffix", capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "prefixprefixsuffixsuffix", "prefixsuffix")
        End Sub
    End Class
End Namespace
