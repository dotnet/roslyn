' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    Partial Public Class NamingStyleTests
#Region "PascalCase"
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestPascalCaseWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestPascalCaseWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal", "Pascal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestPascalCaseWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal", "pascal")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestPascalCaseCapitalizationOfFirstCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "PascalCase", "pascal", "case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestPascalCaseLeavesSubsequentCharactersAlone()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "PasCalCase", "PasCal", "Case")
        End Sub
#End Region

#Region "camelCase"
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCamelCaseWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCamelCaseWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camel", "camel")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCamelCaseWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camel", "Camel")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCamelCaseCapitalizationOfAppropriateFirstCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camelCase", "camel", "case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCamelCaseDecapitalizationOfFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camelCase", "Camel", "Case")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCamelCaseLeavesSubsequentCharactersAlone()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "caMelCase", "caMel", "case")
        End Sub
#End Region

#Region "Firstupper"
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestFirstUpperWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestFirstUpperWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "First", "First")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestFirstUpperWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "First", "first")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestFirstUpperCapitalizationOfFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "Firstupper", "first", "upper")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestFirstUpperDecapitalizationOfAppropriateFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "Firstupper", "First", "Upper")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestFirstUpperLeavesSubsequentCharactersAlone()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "FiRstupper", "fiRst", "upper")
        End Sub
#End Region

#Region "alllower"
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllLowerWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllLowerWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "all", "all")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllLowerWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "all", "ALL")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllLowerWithMultipleAllUpperWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "alllower", "ALL", "LOWER")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllLowerWithMixedWords1()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "alllower", "ALL", "Lower")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllLowerWithMixedWords2()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "alllower", "AlL", "LoWeR")
        End Sub
#End Region

#Region "ALLUPPER"
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllUpperWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllUpperWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALL", "ALL")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllUpperWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALL", "all")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllUpperWithMultipleAllLowerWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALLUPPER", "all", "upper")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllUpperWithMixedWords1()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALLUPPER", "all", "uPPER")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllUpperWithMixedWords2()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALLUPPER", "AlL", "UpPeR")
        End Sub
#End Region
    End Class
End Namespace
