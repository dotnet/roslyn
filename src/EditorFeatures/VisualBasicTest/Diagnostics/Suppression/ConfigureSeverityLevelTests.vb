Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Suppression
    Partial Public MustInherit Class ConfigurationTestsBase
        Inherits AbstractSuppressionDiagnosticTest

        Protected Overrides Function CreateWorkspaceFromFile(initialMarkup As String, parameters As TestParameters) As TestWorkspace
            Return TestWorkspace.CreateVisualBasic(
                initialMarkup,
                parameters.parseOptions,
                If(parameters.compilationOptions, New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary)))
        End Function

        Protected Overrides Function MassageActions(ByVal actions As ImmutableArray(Of CodeAction)) As ImmutableArray(Of CodeAction)
            Return actions(0).NestedCodeActions
        End Function

        Protected Overrides Function GetLanguage() As String
            Return LanguageNames.VisualBasic
        End Function

        Protected Overrides Function GetScriptOptions() As ParseOptions
            Return TestOptions.Script
        End Function

        Friend Overrides Function CreateDiagnosticProviderAndFixer(ByVal workspace As Workspace) As Tuple(Of DiagnosticAnalyzer, ISuppressionOrConfigurationFixProvider)
            Return New Tuple(Of DiagnosticAnalyzer, ISuppressionOrConfigurationFixProvider)(New VisualBasicUseObjectInitializerDiagnosticAnalyzer(), New VisualBasicConfigureSeverityLevel())
        End Function

        Public Class NoneConfigurationTests
            Inherits ConfigurationTestsBase

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 0
                End Get
            End Property

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_Empty_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_RuleExists_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion

[*.vb]
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:none
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidRule_None() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
dotnet_style_object_initializer = true:none
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class SilentConfigurationTests
            Inherits ConfigurationTestsBase

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 1
                End Get
            End Property

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_Empty_Silent() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_RuleExists_Silent() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Silent() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion

[*.vb]
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Silent() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:silent
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidRule_Silent() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
dotnet_style_object_initializer = true:silent
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class SuggestionConfigurationTests
            Inherits ConfigurationTestsBase

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 2
                End Get
            End Property

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_Empty_Suggestion() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_RuleExists_Suggestion() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Suggestion() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:warning

[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Suggestion() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidRule_Suggestion() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:warning
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class WarningConfigurationTests
            Inherits ConfigurationTestsBase

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 3
                End Get
            End Property

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_Empty_Warning() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_RuleExists_Warning() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Warning() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion

[*.vb]
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Warning() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidRule_Warning() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
dotnet_style_object_initializer = true:warning
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class ErrorConfigurationTests
            Inherits ConfigurationTestsBase

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 4
                End Get
            End Property

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_Empty_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig""></AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_RuleExists_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.cs]
dotnet_style_object_initializer = true:suggestion

[*.vb]
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentOption_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.{cs,vb}]
dotnet_style_object_initializer = false:error
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsSuppression)>
            Public Async Function ConfigureEditorconfig_InvalidRule_Error() As Task
                Dim input = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
        <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
</AdditionalDocument>
    </Project>
</Workspace>"
                Dim expected = "
<Workspace>
    <Project Language=""Visual Basic"" AssemblyName=""Assembly1"" CommonReferences=""true"">
         <Document>
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
        <AdditionalDocument FilePath="".editorconfig"">[*.vb]
dotnet_style_object_initializerr = true:suggestion
dotnet_style_object_initializer = true:error
</AdditionalDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class
    End Class
End Namespace
