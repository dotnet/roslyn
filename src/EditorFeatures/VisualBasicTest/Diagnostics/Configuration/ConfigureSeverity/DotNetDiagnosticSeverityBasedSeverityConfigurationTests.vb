' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Configuration.ConfigureSeverity
    Partial Public MustInherit Class DotNetDiagnosticSeverityBasedSeverityConfigurationTests
        Inherits AbstractSuppressionDiagnosticTest

        Private NotInheritable Class CustomDiagnosticAnalyzer
            Inherits DiagnosticAnalyzer

            Private Shared ReadOnly Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
                id:="XYZ0001",
                title:="Title",
                messageFormat:="Message",
                category:="Category",
                defaultSeverity:=DiagnosticSeverity.Info,
                isEnabledByDefault:=True)

            Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
                Get
                    Return ImmutableArray.Create(Rule)
                End Get
            End Property

            Public Overrides Sub Initialize(ByVal context As AnalysisContext)
                context.RegisterSyntaxNodeAction(
                    Sub(c) c.ReportDiagnostic(Diagnostic.Create(Rule, c.Node.GetLocation())),
                    SyntaxKind.ClassBlock)
            End Sub
        End Class

        Protected Overrides Function CreateWorkspaceFromFile(initialMarkup As String, parameters As TestParameters) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(
                initialMarkup,
                parameters.parseOptions,
                If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(ByVal workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, IConfigurationFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, IConfigurationFixProvider)(New CustomDiagnosticAnalyzer(), New ConfigureSeverityLevelCodeFixProvider())
        End Function

        Public Class NoneConfigurationTests
            Inherits DotNetDiagnosticSeverityBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 0
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_Empty_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
[|Class Program1
End Class|]
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]

# XYZ0001: Title
dotnet_diagnostic.XYZ0001.severity = none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_RuleExists_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
[|Class Program1
End Class|]
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.XYZ0001.severity = suggestion   # Comment
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.XYZ0001.severity = none   # Comment
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
[|Class Program1
End Class|]
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_diagnostic.XYZ0001.severity = suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_diagnostic.XYZ0001.severity = suggestion

[*.vb]

# XYZ0001: Title
dotnet_diagnostic.XYZ0001.severity = none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
[|Class Program1
End Class|]
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]
dotnet_diagnostic.XYZ0001.severity = none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestMissingInRegularAndScriptAsync(input)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidRule_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
[|Class Program1
End Class|]
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.XYZ1111.severity = none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.XYZ1111.severity = none

# XYZ0001: Title
dotnet_diagnostic.XYZ0001.severity = none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class
    End Class
End Namespace
