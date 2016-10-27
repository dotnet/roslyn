' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    Partial Public Class NamingStyleTests
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestEmptyStringWithPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="_")
            TestNameCreation(namingStyle, "_")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestEmptyStringWithSuffix()
            Dim namingStyle = CreateNamingStyle(suffix:="_")
            TestNameCreation(namingStyle, "_")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestEmptyStringWithPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="_")
            TestNameCreation(namingStyle, "__")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestSingleWordWithPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_Pascal", "Pascal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestSingleWordWithSuffix()
            Dim namingStyle = CreateNamingStyle(suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal_", "Pascal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestSingleWordWithPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_Pascal_", "Pascal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestSingleWordWithDifferentPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="__", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_Pascal__", "Pascal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestMultipleWordsWithPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_PascalCase", "Pascal", "Case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestMultipleWordsWithSuffix()
            Dim namingStyle = CreateNamingStyle(suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "PascalCase_", "Pascal", "Case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestMultipleWordsWithPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_PascalCase_", "Pascal", "Case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestMultipleWordsWithDifferentPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="_", suffix:="__", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "_PascalCase__", "Pascal", "Case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestPrefixAndSuffixBothApplyEvenWhenOverlapping()
            Dim namingStyle = CreateNamingStyle(prefix:="prefix", suffix:="suffix", capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "prefixprefixsuffixsuffix", "prefixsuffix")
        End Sub
    End Class
End Namespace
