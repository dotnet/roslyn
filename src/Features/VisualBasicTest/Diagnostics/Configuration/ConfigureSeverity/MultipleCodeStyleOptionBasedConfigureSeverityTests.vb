' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.UseInferredMemberName

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Configuration.ConfigureSeverity
    Partial Public MustInherit Class MultipleCodeStyleOptionBasedConfigureSeverityTests
        Inherits AbstractSuppressionDiagnosticTest_NoEditor

        Protected Overrides Function SetParameterDefaults(parameters As TestParameters) As TestParameters
            Return parameters.WithCompilationOptions(If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(ByVal workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, IConfigurationFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, IConfigurationFixProvider)(New VisualBasicUseInferredMemberNameDiagnosticAnalyzer(), New ConfigureSeverityLevelCodeFixProvider())
        End Function
    End Class

    <Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
    Public Class ErrorConfigurationTests
        Inherits MultipleCodeStyleOptionBasedConfigureSeverityTests

        Protected Overrides ReadOnly Property CodeActionIndex As Integer
            Get
                Return 4
            End Get
        End Property

        <WorkItem("https://github.com/dotnet/roslyn/issues/39664")>
        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Async Function ConfigureEditorconfig_Empty_Error() As Task
            Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = ([||]a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_diagnostic.IDE0037.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39664")>
        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Async Function ConfigureEditorconfig_BothRulesExist_Error() As Task
            Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = ([||]a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:suggestion

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:error

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:error

# IDE0037: Use inferred member name
dotnet_diagnostic.IDE0037.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39664")>
        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Async Function ConfigureEditorconfig_OneRuleExists_Error() As Task
            Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = ([||]a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:error

# IDE0037: Use inferred member name
dotnet_diagnostic.IDE0037.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
        End Function

        <WorkItem("https://github.com/dotnet/roslyn/issues/39664")>
        <ConditionalFact(GetType(IsEnglishLocal))>
        Public Async Function ConfigureEditorconfig_AllPossibleEntriesExist_Error() As Task
            Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = ([||]a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:suggestion

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:warning

# IDE0037: Use inferred member name
dotnet_diagnostic.IDE0037.severity = silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class C
    Sub M()
        Dim a As Integer = 1
        Dim t = (a:= a, 2)
    End Sub
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_tuple_names = true:error

# IDE0037: Use inferred member name
dotnet_style_prefer_inferred_anonymous_type_member_names = true:error

# IDE0037: Use inferred member name
dotnet_diagnostic.IDE0037.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
            Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
        End Function
    End Class
End Namespace

