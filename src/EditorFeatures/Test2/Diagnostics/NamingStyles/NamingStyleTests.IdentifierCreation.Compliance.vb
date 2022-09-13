' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    <Trait(Traits.Feature, Traits.Features.NamingStyle)>
    Partial Public Class NamingStyleTests
#Region "Edge cases"
        <Fact>
        Public Sub TestNonoverlappingPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "p_Pascal_s")
        End Sub

        <Fact>
        Public Sub TestAdjacentOverlappingPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "p__s")
        End Sub

        <Fact>
        Public Sub TestPartiallyOverlappingPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "p_s")
        End Sub

        <Fact>
        Public Sub TestFullyOverlappingPrefixAndSuffix()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="p_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "p_")
        End Sub

        <Fact>
        Public Sub TestManyEmptyWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "_____", "_")
        End Sub

        <Fact>
        Public Sub TestPascalCaseMultiplePrefixAndSuffixFixes()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "_", "p_s", "p___s")
        End Sub

        <Fact>
        Public Sub TestPrefixAndCommonPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="Test_", suffix:="_z", capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "Test_m_BaseName", "Test_M_BaseName_z", "Test_BaseName_z")
        End Sub

        <Fact>
        Public Sub TestCommonPrefixAndPrefix()
            Dim namingStyle = CreateNamingStyle(prefix:="Test_", suffix:="_z", capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "m_Test_BaseName", "Test_BaseName_z")
        End Sub
#End Region

#Region "PascalCase"
        <Fact>
        Public Sub TestPascalCaseComplianceWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestPascalCaseComplianceWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "Pascal")
        End Sub

        <Fact>
        Public Sub TestPascalCaseNoncomplianceWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "pascal", "Pascal")
        End Sub

        <Fact>
        Public Sub TestPascalCaseComplianceWithCapitalizationOfFirstCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "PascalCase")
        End Sub

        <Fact>
        Public Sub TestPascalCaseNoncomplianceWithNoncapitalizationOfFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "pascalCase", "PascalCase")
        End Sub

        <Fact>
        Public Sub TestPascalCaseNoncomplianceWithNoncapitalizationOfFirstCharacterOfSubsequentSplitWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "Pascal_case", "Pascal_Case")
        End Sub

        <Fact>
        Public Sub TestPascalCaseIgnoresSeeminglyNoncompliantPrefixOrSuffix()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", prefix:="t_", suffix:="_t", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "t_Pascal_Case_t")
        End Sub

        <Fact>
        Public Sub TestPascalCaseIgnoresSeeminglyNoncompliantWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_t_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "Pascal_t_Case")
        End Sub

        <Fact>
        Public Sub TestPascalCaseAllowsUncasedCharacters()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCompliance(namingStyle, "私の家_2nd")
        End Sub

        <Fact>
        Public Sub TestPascalCaseWithWordSeperation()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "thisIsMyMethod", "This_Is_My_Method")
        End Sub
#End Region

#Region "camelCase"
        <Fact>
        Public Sub TestCamelCaseComplianceWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCompliance(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestCamelCaseComplianceWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCompliance(namingStyle, "camel")
        End Sub

        <Fact>
        Public Sub TestCamelCaseNoncomplianceWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "Camel", "camel")
        End Sub

        <Fact>
        Public Sub TestCamelCaseComplianceWithCorrectCapitalizationOfFirstCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameCompliance(namingStyle, "camelCase")
        End Sub

        <Fact>
        Public Sub TestCamelCaseNoncomplianceWithCapitalizationOfFirstCharacter()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.CamelCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "CamelCase", "camelCase")
        End Sub

        <Fact>
        Public Sub TestCamelCaseNoncomplianceWithNoncapitalizationOfFirstCharacterOfSubsequentSplitWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.CamelCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "camel_case", "camel_Case")
        End Sub

        <Fact>
        Public Sub TestCamelCaseIgnoresSeeminglyNoncompliantPrefixOrSuffix()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", prefix:="t_", suffix:="_t", capitalizationScheme:=Capitalization.CamelCase)
            TestNameCompliance(namingStyle, "t_camel_Case_t")
        End Sub

        <Fact>
        Public Sub TestCamelCaseIgnoresSeeminglyNoncompliantWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_t_", capitalizationScheme:=Capitalization.CamelCase)
            TestNameCompliance(namingStyle, "camel_t_Case")
        End Sub

        <Fact>
        Public Sub TestCamelCaseAllowsUncasedCharacters()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.CamelCase)
            TestNameCompliance(namingStyle, "私の家_2nd")
        End Sub

        <Fact>
        Public Sub TestCamelCaseWithWordSeperation()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.CamelCase)
            TestNameNoncomplianceAndFixedNames(namingStyle, "ThisIsMyMethod", "this_Is_My_Method")
        End Sub
#End Region

#Region "Firstupper"
        <Fact>
        Public Sub TestFirstUpperComplianceWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCompliance(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestFirstUpperComplianceWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCompliance(namingStyle, "First")
        End Sub

        <Fact>
        Public Sub TestFirstUpperNoncomplianceWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameNoncomplianceAndFixedNames(namingStyle, "first", "First")
        End Sub

        <Fact>
        Public Sub TestFirstUpperComplianceWithCorrectCapitalizationOfFirstCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCompliance(namingStyle, "Firstupper")
        End Sub

        <Fact>
        Public Sub TestFirstUpperNoncomplianceWithNoncapitalizationOfFirstCharacter()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.FirstUpper)
            TestNameNoncomplianceAndFixedNames(namingStyle, "first_upper", "First_upper")
        End Sub

        <Fact>
        Public Sub TestFirstUpperNoncomplianceWithCapitalizationOfFirstCharacterOfSubsequentWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.FirstUpper)
            TestNameNoncomplianceAndFixedNames(namingStyle, "First_Upper", "First_upper")
        End Sub

        <Fact>
        Public Sub TestFirstUpperIgnoresSeeminglyNoncompliantPrefixOrSuffix()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", prefix:="t_", suffix:="_T", capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCompliance(namingStyle, "t_First_upper_T")
        End Sub

        <Fact>
        Public Sub TestFirstUpperIgnoresSeeminglyNoncompliantWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_T_", capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCompliance(namingStyle, "First_T_upper")
        End Sub

        <Fact>
        Public Sub TestFirstUpperAllowsUncasedCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCompliance(namingStyle, "私の家")
        End Sub
#End Region

#Region "ALLUPPER"
        <Fact>
        Public Sub TestAllUpperComplianceWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCompliance(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestAllUpperComplianceWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCompliance(namingStyle, "ALL")
        End Sub

        <Fact>
        Public Sub TestAllUpperNoncomplianceWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameNoncomplianceAndFixedNames(namingStyle, "all", "ALL")
        End Sub

        <Fact>
        Public Sub TestAllUpperComplianceWithCorrectCapitalizationOfAllWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.AllUpper)
            TestNameCompliance(namingStyle, "ALL_UPPER")
        End Sub

        <Fact>
        Public Sub TestAllUpperNoncomplianceWithNoncapitalizationOfSomeWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.AllUpper)
            TestNameNoncomplianceAndFixedNames(namingStyle, "ALL_UppeR", "ALL_UPPER")
        End Sub

        <Fact>
        Public Sub TestAllUpperIgnoresSeeminglyNoncompliantPrefixOrSuffix()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", prefix:="t_", suffix:="_t", capitalizationScheme:=Capitalization.AllUpper)
            TestNameCompliance(namingStyle, "t_ALL_UPPER_t")
        End Sub

        <Fact>
        Public Sub TestAllUpperIgnoresSeeminglyNoncompliantWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_t_", capitalizationScheme:=Capitalization.AllUpper)
            TestNameCompliance(namingStyle, "ALL_t_UPPER")
        End Sub

        <Fact>
        Public Sub TestAllUpperAllowsUncasedCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllUpper)
            TestNameCompliance(namingStyle, "私AB23CのDE家")
        End Sub

        <Fact>
        Public Sub TestAllUpperWithWordSeperation()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.AllUpper)
            TestNameNoncomplianceAndFixedNames(namingStyle, "ThisIsMyMethod", "THIS_IS_MY_METHOD")
        End Sub

#End Region

#Region "alllower"
        <Fact>
        Public Sub TestAllLowerComplianceWithZeroWords()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCompliance(namingStyle, "")
        End Sub

        <Fact>
        Public Sub TestAllLowerComplianceWithOneConformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCompliance(namingStyle, "all")
        End Sub

        <Fact>
        Public Sub TestAllLowerNoncomplianceWithOneNonconformingWord()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameNoncomplianceAndFixedNames(namingStyle, "ALL", "all")
        End Sub

        <Fact>
        Public Sub TestAllLowerComplianceWithCorrectCapitalizationOfAllWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.AllLower)
            TestNameCompliance(namingStyle, "all_lower")
        End Sub

        <Fact>
        Public Sub TestAllLowerNoncomplianceWithNoncapitalizationOfSomeWords()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.AllLower)
            TestNameNoncomplianceAndFixedNames(namingStyle, "aLL_Lower", "all_lower")
        End Sub

        <Fact>
        Public Sub TestAllLowerIgnoresSeeminglyNoncompliantPrefixOrSuffix()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", prefix:="T_", suffix:="_T", capitalizationScheme:=Capitalization.AllLower)
            TestNameCompliance(namingStyle, "T_all_lower_T")
        End Sub

        <Fact>
        Public Sub TestAllLowerIgnoresSeeminglyNoncompliantWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_T_", capitalizationScheme:=Capitalization.AllLower)
            TestNameCompliance(namingStyle, "all_T_lower")
        End Sub

        <Fact>
        Public Sub TestAllLowerAllowsUncasedCharacters()
            Dim namingStyle = CreateNamingStyle(capitalizationScheme:=Capitalization.AllLower)
            TestNameCompliance(namingStyle, "私ab23cのde家")
        End Sub

        <Fact>
        Public Sub TestAllLowerWithWordSeperation()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="_", capitalizationScheme:=Capitalization.AllLower)
            TestNameNoncomplianceAndFixedNames(namingStyle, "ThisIsMyMethod", "this_is_my_method")
        End Sub
#End Region
    End Class
End Namespace
