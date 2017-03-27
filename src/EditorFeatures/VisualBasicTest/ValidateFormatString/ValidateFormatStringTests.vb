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
            Return (New ValidateFormatStringDiagnosticAnalyzer(),
                    New EmptyCodeFixProvider())
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function ParamsObjectArray() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format(""This {0} {1} {2} works[||]"", New Object() { ""test"", ""test2"", ""test3"" })
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function TwoPlaceholders() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format(""This {0} {1} works[||]"", ""test"", ""also"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function IFormatProviderAndThreePlaceholders() As Task
            Await TestMissingAsync("
Imports System.Globalization
Class C
     Sub Main()
        Dim culture = ""da - da""
        string.Format(culture, ""The current price [||]is {0:C2} per ounce"", 2.45)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function PassingInOnePlaceholderOutOfBounds() As Task
            Await TestSpansAsync("
Class C
     Sub Main()
        string.Format([|""This {1} is my test""|], ""teststring1"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FormatStringPassingFourPlaceholdersWithOnePlaceholderOutOfBounds() As Task
            Await TestSpansAsync("
Class C
     Sub Main()
        string.Format([|""This{0}{1}{2}{3}{4} is my test""|], ""teststring1"", ""teststring2"", ""teststring3"", ""teststring4"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function PassingiFormatProviderAndTwoPlaceholdersWithOnePlaceholderOutOfBounds() As Task
            Await TestSpansAsync("
Imports System.Globalization
Class C
     Sub Main()
        Dim culture As CultureInfo = ""da - da""
        string.Format(culture, [|""This {2} is my test""|], ""teststring1"", ""teststring2"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParameters() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format(arg0:= ""test"", arg1:= ""also"", format:= ""This {0} {1} works[||]"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParametersOneOutOfBounds() As Task
            Await TestSpansAsync("
Class C
     Sub Main()
        string.Format(arg0:= ""test"", arg1:= ""also"", [|format:= ""This {0} {2} works""|])
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParametersWithIFormatProvider() As Task
            Await TestMissingAsync("
Imports System.Globalization
Class C
     Sub Main()
        Dim culture As CultureInfo = ""da - da""
        string.Format(arg0:= 2.45, provider:=culture, format :=""The current price [||]is {0:C2} per ounce -no squiggles"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamedParametersWithIFormatProviderAndParamsObject() As Task
            Await TestMissingAsync("
Imports System.Globalization
Class C
     Sub Main()
        Dim culture As CultureInfo = ""da - da""
        string.Format(format:= ""This {0} {1} {2} works[||]"", args:=New Object() { ""test"", ""it"", ""really"" }, provider:=culture)
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function NamespaceAliasForStringClass() As Task
            Await TestMissingAsync("
Imports stringalias = System.String
Class C
     Sub Main()
        stringAlias.Format(""This {0} works[||]"", ""test"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FormatStringVerbatimMultipleLines() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format(@""This {0} 
{1} {2} works[||]"", ""multiple"", ""line"", ""test""))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FormatStringInterpolated() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format($""This {0} 
{1} {2} works[||]"", ""multiple"", ""line"", ""test""))
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FormatStringEmpty() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format(""[||]"")
    End Sub
End Class")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FormatStringLeftParenOnly() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format([||]
    End Sub
End Class")
        End Function


        <Fact, Trait(Traits.Feature, Traits.Features.ValidateFormatString)>
        Public Async Function FormatStringParenthesesOnly() As Task
            Await TestMissingAsync("
Class C
     Sub Main()
        string.Format()[||]
    End Sub
End Class")
        End Function



    End Class

End Namespace

Namespace Microsoft.CodeAnalysis.VisualBasic.ValidateFormatString
    ' Currently the test infrastructure doesn't accomodate diagnostics without code fixes,
    ' so this empty code fix provider is a temporary solution to get tests running.
    ' I plan to add an appropriate test helper to test diagnostics without code fixes
    Public Class EmptyCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(IDEDiagnosticIds.ValidateFormatStringDiagnosticID)
            End Get
        End Property

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            context.RegisterCodeFix(New MyCodeAction(), context.Diagnostics.First())
            Return Task.CompletedTask
        End Function

        Private Class MyCodeAction
            Inherits CodeAction
            Public Overrides ReadOnly Property Title As String
                Get
                    Return ""
                End Get
            End Property
        End Class
    End Class
End Namespace

