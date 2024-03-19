' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    <Trait(Traits.Feature, Traits.Features.NamingStyle)>
    Partial Public Class NamingStyleTests
#Region "PascalCase"
        <Fact>
        Public Sub TestPascalCaseWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestPascalCaseWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal", "Pascal")
        End Sub

        <Fact>
        Public Sub TestPascalCaseWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "Pascal", "pascal")
        End Sub

        <Fact>
        Public Sub TestPascalCaseCapitalizationOfFirstCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "PascalCase", "pascal", "case")
        End Sub

        <Fact>
        Public Sub TestPascalCaseLeavesSubsequentCharactersAlone()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "PasCalCase", "PasCal", "Case")
        End Sub
#End Region

#Region "camelCase"
        <Fact>
        Public Sub TestCamelCaseWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestCamelCaseWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camel", "camel")
        End Sub

        <Fact>
        Public Sub TestCamelCaseWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camel", "Camel")
        End Sub

        <Fact>
        Public Sub TestCamelCaseCapitalizationOfAppropriateFirstCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camelCase", "camel", "case")
        End Sub

        <Fact>
        Public Sub TestCamelCaseDecapitalizationOfFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "camelCase", "Camel", "Case")
        End Sub

        <Fact>
        Public Sub TestCamelCaseLeavesSubsequentCharactersAlone()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "caMelCase", "caMel", "case")
        End Sub
#End Region

#Region "Firstupper"
        <Fact>
        Public Sub TestFirstUpperWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestFirstUpperWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "First", "First")
        End Sub

        <Fact>
        Public Sub TestFirstUpperWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "First", "first")
        End Sub

        <Fact>
        Public Sub TestFirstUpperCapitalizationOfFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "Firstupper", "first", "upper")
        End Sub

        <Fact>
        Public Sub TestFirstUpperDecapitalizationOfAppropriateFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "Firstupper", "First", "Upper")
        End Sub

        <Fact>
        Public Sub TestFirstUpperLeavesSubsequentCharactersAlone()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "FiRstupper", "fiRst", "upper")
        End Sub
#End Region

#Region "alllower"
        <Fact>
        Public Sub TestAllLowerWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestAllLowerWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "all", "all")
        End Sub

        <Fact>
        Public Sub TestAllLowerWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "all", "ALL")
        End Sub

        <Fact>
        Public Sub TestAllLowerWithMultipleAllUpperWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "alllower", "ALL", "LOWER")
        End Sub

        <Fact>
        Public Sub TestAllLowerWithMixedWords1()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "alllower", "ALL", "Lower")
        End Sub

        <Fact>
        Public Sub TestAllLowerWithMixedWords2()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "alllower", "AlL", "LoWeR")
        End Sub
#End Region

#Region "ALLUPPER"
        <Fact>
        Public Sub TestAllUpperWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestAllUpperWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALL", "ALL")
        End Sub

        <Fact>
        Public Sub TestAllUpperWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALL", "all")
        End Sub

        <Fact>
        Public Sub TestAllUpperWithMultipleAllLowerWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALLUPPER", "all", "upper")
        End Sub

        <Fact>
        Public Sub TestAllUpperWithMixedWords1()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALLUPPER", "all", "uPPER")
        End Sub

        <Fact>
        Public Sub TestAllUpperWithMixedWords2()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ALLUPPER", "AlL", "UpPeR")
        End Sub
#End Region
    End Class
End Namespace
