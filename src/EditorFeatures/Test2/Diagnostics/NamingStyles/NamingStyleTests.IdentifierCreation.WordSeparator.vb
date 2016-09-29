' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    Partial Public Class NamingStyleTests
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestEmptyStringWithWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_")
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestSingleWordWithWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal", "Pascal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestTwoWordsWithWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal_Case", "Pascal", "Case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestThreeWordsWithWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal_Case_Test", "Pascal", "Case", "Test")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestWordSeparatorsAddedEvenWhenOverlappingWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal___Case__Test", "Pascal_", "_Case", "_Test")
        End Sub
    End Class
End Namespace
