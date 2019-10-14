' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeFixes.Configuration.ConfigureCodeStyle
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Diagnostics
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.VisualBasic.UseObjectInitializer

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Diagnostics.Configuration.ConfigureCodeStyle
    Partial Public MustInherit Class BooleanCodeStyleOptionConfigurationTests
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
            Return New Tuple(Of DiagnosticAnalyzer, IConfigurationFixProvider)(New VisualBasicUseObjectInitializerDiagnosticAnalyzer(), New ConfigureCodeStyleOptionCodeFixProvider())
        End Function

        Public Class TrueConfigurationTests
            Inherits BooleanCodeStyleOptionConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 0
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_Empty_True() As Task
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
            Public Async Function ConfigureEditorconfig_RuleExists_True() As Task
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
dotnet_style_object_initializer = false:suggestion    ; Comment2
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
dotnet_style_object_initializer = true:suggestion    ; Comment2
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_True() As Task
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
dotnet_style_object_initializer = true:none
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
dotnet_style_object_initializer = true:none

[*.{cs,vb}]

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentSeverity_True() As Task
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
dotnet_style_object_initializer = false:warning
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
            Public Async Function ConfigureEditorconfig_InvalidRule_True() As Task
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
dotnet_style_object_initializer_error = true:none
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
dotnet_style_object_initializer_error = true:none

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = true:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class

        Public Class FalseConfigurationTests
            Inherits BooleanCodeStyleOptionConfigurationTests

            Protected Overrides ReadOnly Property CodeActionIndex As Integer
                Get
                    Return 1
                End Get
            End Property

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_Empty_False() As Task
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
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_RuleExists_False() As Task
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
dotnet_style_object_initializer = true:silent
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
dotnet_style_object_initializer = false:silent
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidHeader_False() As Task
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
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <Fact, Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_MaintainCurrentSeverity_False() As Task
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
dotnet_style_object_initializer = false:warning
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function

            <ConditionalFact(GetType(IsEnglishLocal)), Trait(Traits.Feature, Traits.Features.CodeActionsConfiguration)>
            Public Async Function ConfigureEditorconfig_InvalidRule_False() As Task
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
dotnet_style_object_initializer_error = true:suggestion
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
dotnet_style_object_initializer_error = true:suggestion

# IDE0017: Simplify object initialization
dotnet_style_object_initializer = false:suggestion
</AnalyzerConfigDocument>
    </Project>
</Workspace>"
                Await TestInRegularAndScriptAsync(input, expected, CodeActionIndex)
            End Function
        End Class
    End Class
End Namespace
