' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Configuration.ConfigureSeverity
    Partial Public MustInherit Class CodeStyleOptionBasedSeverityConfigurationTests
        Inherits AbstractSuppressionDiagnosticTest

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
            Return New Tuple(Of DiagnosticAnalyzer, IConfigurationFixProvider)(New VisualBasicUseObjectInitializerDiagnosticAnalyzer(), New ConfigureSeverityLevelCodeFixProvider())
        End Function

        <Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
        Public Class NoneConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 0
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal))>
            Public Async Function ConfigureEditorconfig_Empty_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_diagnostic.IDE0017.severity = none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact>
            Public Async Function ConfigureEditorconfig_RuleExists_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]    # Comment1
dotnet_style_object_initializer = true:suggestion    ; Comment2
dotnet_diagnostic.IDE0017.severity = warning   ;; Comment3
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]    # Comment1
dotnet_style_object_initializer = true:none    ; Comment2
dotnet_diagnostic.IDE0017.severity = none   ;; Comment3
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        <Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
        Public Class SilentConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 1
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal))>
            Public Async Function ConfigureEditorconfig_Empty_Silent() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_diagnostic.IDE0017.severity = silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact>
            Public Async Function ConfigureEditorconfig_RuleExists_Silent() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.IDE0017.severity = suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.IDE0017.severity = silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        <Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
        Public Class SuggestionConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 2
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal))>
            Public Async Function ConfigureEditorconfig_Empty_Suggestion() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_diagnostic.IDE0017.severity = suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact>
            Public Async Function ConfigureEditorconfig_RuleExists_Suggestion() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.IDE0017.severity = warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.IDE0017.severity = suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        <Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
        Public Class WarningConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 3
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal))>
            Public Async Function ConfigureEditorconfig_Empty_Warning() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_diagnostic.IDE0017.severity = warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact>
            Public Async Function ConfigureEditorconfig_RuleExists_Warning() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.IDE0017.severity = suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_diagnostic.IDE0017.severity = warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        <Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
        Public Class ErrorConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 4
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal))>
            Public Async Function ConfigureEditorconfig_Empty_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig""></AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_diagnostic.IDE0017.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact>
            Public Async Function ConfigureEditorconfig_RuleExists_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
dotnet_diagnostic.IDE0017.severity = warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializer = true:error
dotnet_diagnostic.IDE0017.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal))>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_diagnostic.IDE0017.severity = suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.cs]
dotnet_diagnostic.IDE0017.severity = suggestion

[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_diagnostic.IDE0017.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = true:suggestion
dotnet_diagnostic.IDE0017.severity = warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = true:error
dotnet_diagnostic.IDE0017.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal))>
            Public Async Function ConfigureEditorconfig_InvalidRule_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
dotnet_diagnostic.IDE0017.severityyy = warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document FilePath=""z:\\file.vb"">
Class Program1
    Private Shared Sub Main()
        ' dotnet_style_object_initializer = true
        Dim obj = New Customer() With {
            ._age = 21
        }
        ' dotnet_style_object_initializer = false
        Dim obj2 As Customer = [|New Customer()|]
        obj2._age = 21
    End Sub

    Friend Class Customer
        Public _age As Integer

        Public Sub New()
        End Sub
    End Class
End Class
        </Document>
        <AnalyzerConfigDocument FilePath=""z:\\.editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
dotnet_diagnostic.IDE0017.severityyy = warning

# IDE0017: Simplify object initialization
dotnet_diagnostic.IDE0017.severity = error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class
    End Class
End Namespace
