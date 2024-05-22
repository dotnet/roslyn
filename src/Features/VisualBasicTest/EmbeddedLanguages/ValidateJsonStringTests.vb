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
    <Trait(Traits.Feature, Traits.Features.ValidateJsonString)>
    Public Class ValidateJsonStringTests
        Inherits AbstractVisualBasicDiagnosticProviderBasedUserDiagnosticTest_NoEditor

        Public Sub New(logger As ITestOutputHelper)
            MyBase.New(logger)
        End Sub

        Friend Overrides Function CreateDiagnosticProviderAndFixer(Workspace As Workspace) As (DiagnosticAnalyzer, CodeFixProvider)
            Return (New VisualBasicJsonDiagnosticAnalyzer(), Nothing)
        End Function

        Private Function OptionOn() As OptionsCollection
            Return [Option](IdeAnalyzerOptionsStorage.ReportInvalidJsonPatterns, True)
        End Function

        <Fact>
        Public Async Function TestWarning1() As Task
            Await TestDiagnosticInfoAsync("
class Program
    sub Main()
        ' lang=json,strict
        dim r = ""[|new|] Json()""
    end sub     
end class",
                globalOptions:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0, FeaturesResources.Constructors_not_allowed))
        End Function

        <Fact>
        Public Async Function TestWarning2() As Task
            Await TestDiagnosticInfoAsync("
class Program
    sub Main()
        ' lang=json
        dim r = ""[|}|]""
    end sub     
end class",
                globalOptions:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    String.Format(FeaturesResources._0_unexpected, "}")))
        End Function

        <Fact>
        Public Async Function TestJsonDocumentWithTrailingComma() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = JsonDocument.Parse(""[1[|,|]]"")
    end sub
end class
        </Document>
    </Project>
</Workspace>",
                globalOptions:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Trailing_comma_not_allowed))
        End Function

        <Fact>
        Public Async Function TestJsonDocumentTrailingCommaDisallowed() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = JsonDocument.Parse(""[1[|,|]]"", new JsonDocumentOptions() with { .AllowTrailingCommas = false })
    end sub
end class
        </Document>
    </Project>
</Workspace>",
                globalOptions:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Trailing_comma_not_allowed))
        End Function

        <Fact>
        Public Async Function TestJsonDocumentTrailingCommaAllowed() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = JsonDocument.Parse(""[1[|,|]]"", new JsonDocumentOptions() with { .AllowTrailingCommas = true })
    end sub
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact>
        Public Async Function TestJsonDocumentTrailingCommaAllowedCaseChange() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = JsonDocument.Parse(""[1[|,|]]"", new jsondocumentoptions() with { .allowTrailingCommas = true })
    end sub
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact>
        Public Async Function TestJsonDocumentWithComments() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = JsonDocument.Parse(""[1][|/*comment*/|]"")
    end sub
end class
        </Document>
    </Project>
</Workspace>",
                globalOptions:=OptionOn(),
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Comments_not_allowed))
        End Function

        <Fact>
        Public Async Function TestJsonDocumentCommentsDisallowed() As Task
            Await TestDiagnosticInfoAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = JsonDocument.Parse(""[1][|/*comment*/|]"", new JsonDocumentOptions() with { .CommentHandling = JsonCommentHandling.Disallow })
    end sub
end class
        </Document>
    </Project>
</Workspace>",
                diagnosticId:=AbstractJsonDiagnosticAnalyzer.DiagnosticId,
                diagnosticSeverity:=DiagnosticSeverity.Warning,
                diagnosticMessage:=String.Format(FeaturesResources.JSON_issue_0,
                    FeaturesResources.Comments_not_allowed),
                globalOptions:=OptionOn())
        End Function

        <Fact>
        Public Async Function TestJsonDocumentCommentsAllowed() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = JsonDocument.Parse(""[1][|/*comment*/|]"", new JsonDocumentOptions() with { .CommentHandling = JsonCommentHandling.Allow })
    end sub
end class
        </Document>
    </Project>
</Workspace>")
        End Function

        <Fact>
        Public Async Function TestJsonDocumentCommentsAllowedCaseInsensitive() As Task
            Await TestDiagnosticMissingAsync("<Workspace>
    <Project Language=""Visual Basic"" CommonReferencesNet6=""true"">
        <Document>
imports System.Text.Json

class Program
    sub Main()
        dim r = jsonDocument.parse(""[1][|/*comment*/|]"", new jsonDocumentOptions() with { .commentHandling = jsonCommentHandling.allow })
    end sub
end class
        </Document>
    </Project>
</Workspace>")
        End Function
    End Class
End Namespace
