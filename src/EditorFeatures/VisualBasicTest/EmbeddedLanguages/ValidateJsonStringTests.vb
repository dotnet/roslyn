' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.CodeActions
Imports Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Features.EmbeddedLanguages.Json.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic.Features.EmbeddedLanguages
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.EmbeddedLanguages
    Public Class ValidateJsonStringTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest

        Public Sub New(logger As ITestOutputHelper)
            MyBase.New(logger)
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(Workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicJsonDiagnosticAnalyzer(), Nothing)
        End Function

        Private Shared Function OptionOn() As OptionsCollection
            Dim result = New OptionsCollection(LanguageNames.VisualBasic)
            result.Add(JsonFeatureOptions.ReportInvalidJsonPatterns, True)
            Return result
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestWarning1() As Task
            Await TestDiagnosticInfoAsync("
class Program
{
    void Main()
    {
        var r = /*lang=json,strict*/ ""[|new|] Json()""
    }     
}",
                options:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0, FeaturesResources.Constructors_not_allowed))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestWarning2() As Task
            Await TestDiagnosticInfoAsync("
class Program
{
    void Main()
    {
        var r = /*lang=json*/ ""[|}|]""
    }     
}",
                options:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    String.Format(FeaturesResources._0_unexpected, "}")))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentWithTrailingComma() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1[|,|]]"")
    }     
}
        </Document>
    </Project>
</Workspace>",
                options:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Trailing_comma_not_allowed))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentTrailingCommaDisallowed() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1[|,|]]"", new JsonDocumentOptions { AllowTrailingCommas = false })
    }     
}
        </Document>
    </Project>
</Workspace>",
                options:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Trailing_comma_not_allowed))
        end Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentTrailingCommaAllowed() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1[|,|]]"", new JsonDocumentOptions { AllowTrailingCommas = true })
    }     
}
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentTrailingCommaAllowedImplicitObject() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1[|,|]]"", new() { AllowTrailingCommas = true })
    }     
}
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentWithComments() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1][|/*comment*/|]"")
    }     
}
        </Document>
    </Project>
</Workspace>",
                options:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Comments_not_allowed))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentCommentsDisallowed() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1][|/*comment*/|]"", new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Disallow })
    }     
}
        </Document>
    </Project>
</Workspace>",
                options:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Comments_not_allowed))
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentCommentsAllowed() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1][|/*comment*/|]"", new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Allow })
    }     
}
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
        Public Async Function TestJsonDocumentCommentsAllowedImplicitObject() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""C#"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
{
    void Main()
    {
        var r = JsonDocument.Parse(""[1][|/*comment*/|]"", new() { CommentHandling = JsonCommentHandling.Allow })
    }     
}
        </Document>
    </Project>
</Workspace>")
        End Function
    End Class
End Namespace
