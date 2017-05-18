' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.ValidateFormatString
Imports System.Collections.Immutable

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ValidateFormatString
    Public Class ValidateFormatStringTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicValidateFormatStringDiagnosticAnalyzer, Nothing)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function ParamsObjectArray() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format(""This {0} {1} {[||]2} works"", New Object  { ""test"", ""test2"", ""test3"" })
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function TwoPlaceholders() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format(""This {0} {1[||]} works"", ""test"", ""also"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function IFormatProviderAndThreePlaceholders() As Task
            Await TestDiagnosticMissingAsync("
Imports System.Globalization
Class C
     Sub Main 
        Dim culture as CultureInfo = ""da - da""
        string.Format(culture, ""The current price is {0[||]:C2} per ounce"", 2.45)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function OnePlaceholderOutOfBounds() As Task
            Dim diagnosticMessage = String.Format(FeaturesResources.Format_string_0_contains_invalid_placeholder_1, """This {1} is my test""", "{1}")
            Await TestDiagnosticInfoAsync("
Class C
     Sub Main 
        string.Format(""This [|{1}|] is my test"", ""teststring1"")
    End Sub
End Class",
        options:=Nothing,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=diagnosticMessage)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FourPlaceholdersWithOnePlaceholderOutOfBounds() As Task
            Dim diagnosticMessage = String.Format(
                FeaturesResources.Format_string_0_contains_invalid_placeholder_1, """This{0}{1}{2}{3}{4} is my test""", "{4}")
            Await TestDiagnosticInfoAsync("
Class C
     Sub Main 
        string.Format(""This{0}{1}{2}{3}[|{4}|] is my test"", ""teststring1"", ""teststring2"", ""teststring3"", ""teststring4"")
    End Sub
End Class",
        options:=Nothing,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=diagnosticMessage)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function IFormatProviderAndTwoPlaceholdersWithOnePlaceholderOutOfBounds() As Task
            Dim diagnosticMessage = String.Format(
                FeaturesResources.Format_string_0_contains_invalid_placeholder_1, """This {2} is my test""", "{2}")
            Await TestDiagnosticInfoAsync("
Imports System.Globalization
Class C
     Sub Main 
        Dim culture As CultureInfo = ""da - da""
        string.Format(culture, ""This [|{2}|] is my test"", ""teststring1"", ""teststring2"")
    End Sub
End Class",
        options:=Nothing,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=diagnosticMessage)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParameters() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format(arg0:= ""test"", arg1:= ""also"", format:= ""This {0[||]} {1} works"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParametersOneOutOfBounds() As Task
            Dim diagnosticMessage = String.Format(
                FeaturesResources.Format_string_0_contains_invalid_placeholder_1, """This {0} {2} works""", "{2}")
            Await TestDiagnosticInfoAsync("
Class C
     Sub Main 
        string.Format(arg0:= ""test"", arg1:= ""also"", format:= ""This {0} [|{2}|] works"")
    End Sub
End Class",
        options:=Nothing,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=diagnosticMessage)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParametersWithIFormatProvider() As Task
            Await TestDiagnosticMissingAsync("
Imports System.Globalization
Class C
     Sub Main 
        Dim culture As CultureInfo = ""da - da""
        string.Format(arg0:= 2.45, provider:=culture, format :=""The current price is {0[||]:C2} per ounce -no squiggles"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParametersWithIFormatProviderAndParamsObject() As Task
            Await TestDiagnosticMissingAsync("
Imports System.Globalization
Class C
     Sub Main 
        Dim culture As CultureInfo = ""da - da""
        string.Format(format:= ""This {0} {1[||]} {2} works"", args:=New Object  { ""test"", ""it"", ""really"" }, provider:=culture)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamespaceAliasForStringClass() As Task
            Await TestDiagnosticMissingAsync("
Imports stringalias = System.String
Class C
     Sub Main 
        stringAlias.Format(""This {[||]0} works"", ""test"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function VerbatimMultipleLines() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format(@""This {[||]0} 
{1} {2} works"", ""multiple"", ""line"", ""test""))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function Interpolated() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format($""This {0} 
{1[||]} {2} works"", ""multiple"", ""line"", ""test""))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function Empty() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format(""[||]"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function LeftParenOnly() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format([||]
    End Sub
End Class")
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function ParenthesesOnly() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format [||]
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function DifferentFunction() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Compare [||]
    End Sub
End Class")
        End Function

    End Class

End Namespace

