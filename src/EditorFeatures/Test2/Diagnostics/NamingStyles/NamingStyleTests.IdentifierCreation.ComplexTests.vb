' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics.Analyzers.NamingStyles

Namespace Microsoft.CodeAnalysis.Editor.Implementation.Diagnostics.UnitTests
    Partial Public Class NamingStyleTests
        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCapitalizationNotAppliedToPrefix1()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "p_One", "one")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCapitalizationNotAppliedToPrefix2()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "p_ONE", "one")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCapitalizationNotAppliedToSuffix1()
            Dim namingStyle = CreateNamingStyle(suffix:="_t", capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ONE_t", "one")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCapitalizationNotAppliedToSuffix2()
            Dim namingStyle = CreateNamingStyle(suffix:="_T", capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "one_T", "one")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCapitalizationNotAppliedToWordSeparator()
            Dim namingStyle = CreateNamingStyle(wordSeparator:="t", capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "ONEtTWO", "one", "two")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestPascalCaseComplex1()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", wordSeparator:="__", capitalizationScheme:=Capitalization.PascalCase)
            TestNameCreation(namingStyle, "p_P_one__Two__ThRee_s", "p_one", "two", "thRee")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestCamelCaseComplex1()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", wordSeparator:="__", capitalizationScheme:=Capitalization.CamelCase)
            TestNameCreation(namingStyle, "p_p_one__Two__ThRee_s", "P_one", "two", "thRee")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestFirstUpperComplex1()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", wordSeparator:="__", capitalizationScheme:=Capitalization.FirstUpper)
            TestNameCreation(namingStyle, "p_P_one__two__thRee_s", "p_one", "Two", "thRee")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllLowerComplex1()
            Dim namingStyle = CreateNamingStyle(prefix:="p_", suffix:="_s", wordSeparator:="__", capitalizationScheme:=Capitalization.AllLower)
            TestNameCreation(namingStyle, "p_p_one__two__three_s", "P_one", "Two", "thRee")
        End Sub

        <Fact, Trait(Traits.Feature, Traits.Features.NamingStyle)>
        Public Sub TestAllUpperComplex1()
            Dim namingStyle = CreateNamingStyle(prefix:="P_", suffix:="_S", wordSeparator:="__", capitalizationScheme:=Capitalization.AllUpper)
            TestNameCreation(namingStyle, "P_P_ONE__TWO__THREE_S", "p_One", "Two", "thRee")
        End Sub
    End Class
End Namespace
