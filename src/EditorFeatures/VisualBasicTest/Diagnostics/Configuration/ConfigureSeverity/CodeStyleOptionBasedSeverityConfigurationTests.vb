' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureSeverity
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Configuration.ConfigureSeverity
    Partial Public MustInherit Class CodeStyleOptionBasedSeverityConfigurationTests
        Inherits AbstractSuppressionDiagnosticTest

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
            Return New Tuple(Of DiagnosticAnalyzer, IConfigurationFixProvider)(New VisualBasicUseObjectInitializerDiagnosticAnalyzer(), New ConfigureSeverityLevelCodeFixProvider())
        End Function

        Public Class NoneConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

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
dotnet_style_object_initializer = true:none
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
dotnet_style_object_initializer = true:suggestion
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
dotnet_style_object_initializer = true:suggestion

[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:none
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
dotnet_style_object_initializer = true:none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidRule_None() As Task
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

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:none
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class SilentConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 1
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:suggestion
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
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Silent() As Task
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
dotnet_style_object_initializer = true:suggestion
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
dotnet_style_object_initializer = true:suggestion

[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Silent() As Task
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
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidRule_Silent() As Task
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

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class SuggestionConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 2
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:warning
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
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Suggestion() As Task
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
dotnet_style_object_initializer = true:warning
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
dotnet_style_object_initializer = true:warning

[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Suggestion() As Task
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
dotnet_style_object_initializer = true:warning
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
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidRule_Suggestion() As Task
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
dotnet_style_object_initializerr = true:warning
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
dotnet_style_object_initializerr = true:warning

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class WarningConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 3
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:suggestion
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
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Warning() As Task
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
dotnet_style_object_initializer = true:suggestion
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
dotnet_style_object_initializer = true:suggestion

[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Warning() As Task
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
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidRule_Warning() As Task
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

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class ErrorConfigurationTests
            Inherits CodeStyleOptionBasedSeverityConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 4
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
dotnet_style_object_initializer = true:suggestion
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
dotnet_style_object_initializer = true:suggestion

[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
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

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:error
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class
    End Class
End Namespace
