﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Options
Imports Microsoft.CodeAnalysis.ValidateFormatString
Imports Microsoft.CodeAnalysis.VisualBasic.ValidateFormatString

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.ValidateFormatString
    Public Class ValidateFormatStringTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Friend Overrides Function CreateDiagnosticProviderAndFixer(
                workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicValidateFormatStringDiagnosticAnalyzer, Nothing)
        End Function

        Private Function VBOptionOnCSharpOptionOff() As IDictionary(Of OptionKey, Object)
            Dim optionsSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.CSharp), False},
                {New OptionKey(ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.VisualBasic), True}
            }

            Return optionsSet
        End Function

        Private Function VBOptionOffCSharpOptionOn() As IDictionary(Of OptionKey, Object)
            Dim optionsSet = New Dictionary(Of OptionKey, Object) From
            {
                {New OptionKey(ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.CSharp), True},
                {New OptionKey(ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatCalls, LanguageNames.VisualBasic), False}
            }

            Return optionsSet
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
            Await TestDiagnosticInfoAsync("
Class C
     Sub Main 
        string.Format(""This [|{1}|] is my test"", ""teststring1"")
    End Sub
End Class",
        options:=Nothing,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=FeaturesResources.Format_string_contains_invalid_placeholder)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FourPlaceholdersWithOnePlaceholderOutOfBounds() As Task
            Await TestDiagnosticInfoAsync("
Class C
     Sub Main 
        string.Format(""This{0}{1}{2}{3}[|{4}|] is my test"", ""teststring1"", ""teststring2"", ""teststring3"", ""teststring4"")
    End Sub
End Class",
        options:=Nothing,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=FeaturesResources.Format_string_contains_invalid_placeholder)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function IFormatProviderAndTwoPlaceholdersWithOnePlaceholderOutOfBounds() As Task
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
        diagnosticMessage:=FeaturesResources.Format_string_contains_invalid_placeholder)
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
            Await TestDiagnosticInfoAsync("
Class C
     Sub Main 
        string.Format(arg0:= ""test"", arg1:= ""also"", format:= ""This {0} [|{2}|] works"")
    End Sub
End Class",
        options:=Nothing,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=FeaturesResources.Format_string_contains_invalid_placeholder)
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
        Public Async Function DuplicateNamedParameters() As Task
            Await TestDiagnosticMissingAsync("
Imports System.Globalization
Class C
     Sub Main 
        Dim culture As CultureInfo = ""da - da""
        string.Format(format:= ""This {0} {1[||]} {2} works"", format:=New Object  { ""test"", ""it"", ""really"" }, provider:=culture)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function DuplicateNamedParametersInNet45() As Task
            Await TestDiagnosticMissingAsync("
<Workspace>
                             <Project Language=""Visual Basic"" CommonReferencesNet45=""true"">
                                 <Document FilePath=""SourceDocument"">
Imports System.Globalization
Class C
     Sub Main 
        Dim culture As CultureInfo = ""da - da""
        string.Format(format:= ""This {0} {1[||]} {2} works"", format:=New Object  { ""test"", ""it"", ""really"" }, provider:=culture)
    End Sub
End Class
        </Document>
            </Project>
                </Workspace>")
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

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FormatMethodOnGenericIdentifier() As Task
            Await TestDiagnosticMissingAsync("
Class G(Of T)
    Function Format(Of T)(foo as String)
        Return True
    End Function
End Class

Class C
    Sub Foo()
        Dim q As G(Of Integer)
        q.Format(Of Integer)(""TestStr[||]ing"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function OmittedArgument() As Task
            Await TestDiagnosticMissingAsync("Module M
    Sub Main()
         String.Format([||],)
    End Sub
End Module")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function WarningTurnedOff() As Task
            Await TestDiagnosticMissingAsync("
Class C
     Sub Main 
        string.Format(""This {0} {1} {[||]2} works"", New Object  { ""test"", ""test2"", ""test3"" })
    End Sub
End Class", New TestParameters(options:=VBOptionOffCSharpOptionOn))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function WarningTurnedOn() As Task
            Await TestDiagnosticInfoAsync("
Class C
     Sub Main 
        string.Format(""This {0} [|{2}|] works"", ""test"", ""also"")
    End Sub
End Class",
        options:=VBOptionOnCSharpOptionOff,
        diagnosticId:=IDEDiagnosticIds.ValidateFormatStringDiagnosticID,
        diagnosticSeverity:=DiagnosticSeverity.Warning,
        diagnosticMessage:=FeaturesResources.Format_string_contains_invalid_placeholder)
        End Function
    End Class
End Namespace