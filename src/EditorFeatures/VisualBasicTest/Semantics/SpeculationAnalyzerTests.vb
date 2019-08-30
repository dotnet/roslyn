' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.IO
Imports System.Threading
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Semantics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Utilities

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Semantics

    Public Class SpeculationAnalyzerTests
        Inherits SpeculationAnalyzerTestsBase

        <Fact, WorkItem(672396, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/672396")>
        Public Sub SpeculationAnalyzerExtensionMethodExplicitInvocation()
            Test(<Code>
Module Oombr
    &lt;System.Runtime.CompilerServices.Extension&gt;
    Public Sub Vain(arg As Integer)
    End Sub

    Sub Main()
        Call [|5.Vain()|]
    End Sub
End Module
            </Code>.Value, "Vain(5)", False)
        End Sub

        <Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")>
        Public Sub SpeculationAnalyzerIndexerPropertyWithRedundantCast()
            Test(<Code>
Class Indexer
    Default Public ReadOnly Property Item(ByVal x As Integer) As Integer
        Get
            Return x
        End Get
    End Property
End Class
Class A
    Public ReadOnly Property Foo As Indexer
End Class
Class B
    Inherits A
End Class
Class Program
    Sub Main()
        Dim b As B = New B()
        Dim y As Integer = [|DirectCast(b, A)|].Foo(2)
    End Sub
End Class
            </Code>.Value, "b", False)
        End Sub

        <Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")>
        Public Sub SpeculationAnalyzerIndexerPropertyWithRequiredCast()
            Test(<Code>
Class Indexer
    Default Public ReadOnly Property Item(ByVal x As Integer) As Integer
        Get
            Return x
        End Get
    End Property
End Class
Class A
    Public ReadOnly Property Foo As Indexer
End Class
Class B
    Inherits A
    Public Shadows ReadOnly Property Foo As Indexer
End Class
Class Program
    Sub Main()
        Dim b As B = New B()
        Dim y As Integer = [|DirectCast(b, A)|].Foo(2)
    End Sub
End Class
            </Code>.Value, "b", True)
        End Sub

        <Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")>
        Public Sub SpeculationAnalyzerDelegatePropertyWithRedundantCast()
            Test(<Code>
Public Delegate Sub MyDelegate()
Class A
    Public ReadOnly Property Foo As MyDelegate
End Class
Class B
    Inherits A
End Class
Class Program
    Sub Main()
        Dim b As B = New B()
        [|DirectCast(b, A)|].Foo.Invoke()
    End Sub
End Class
            </Code>.Value, "b", False)
        End Sub

        <Fact, WorkItem(28412, "https://github.com/dotnet/roslyn/issues/28412")>
        Public Sub SpeculationAnalyzerDelegatePropertyWithRequiredCast()
            Test(<Code>
Public Delegate Sub MyDelegate()
Class A
    Public ReadOnly Property Foo As MyDelegate
End Class
Class B
    Inherits A
    Public Shadows ReadOnly Property Foo As MyDelegate
End Class
Class Program
    Sub Main()
        Dim b As B = New B()
        [|DirectCast(b, A)|].Foo.Invoke()
    End Sub
End Class
            </Code>.Value, "b", True)
        End Sub

        Protected Overrides Function Parse(text As String) As SyntaxTree
            Return SyntaxFactory.ParseSyntaxTree(text)
        End Function

        Protected Overrides Function IsExpressionNode(node As SyntaxNode) As Boolean
            Return TypeOf node Is ExpressionSyntax
        End Function

        Protected Overrides Function CreateCompilation(tree As SyntaxTree) As Compilation
            Return VisualBasicCompilation.Create(
                CompilationName,
                {DirectCast(tree, VisualBasicSyntaxTree)},
                References,
                TestOptions.ReleaseDll.WithSpecificDiagnosticOptions({KeyValuePairUtil.Create("BC0219", ReportDiagnostic.Suppress)}))
        End Function

        Protected Overrides Function CompilationSucceeded(compilation As Compilation, temporaryStream As Stream) As Boolean
            Dim langCompilation = DirectCast(compilation, VisualBasicCompilation)
            Return Not langCompilation.GetDiagnostics().Any() AndAlso Not langCompilation.Emit(temporaryStream).Diagnostics.Any()
        End Function

        Protected Overrides Function ReplacementChangesSemantics(initialNode As SyntaxNode, replacementNode As SyntaxNode, initialModel As SemanticModel) As Boolean
            Return New SpeculationAnalyzer(DirectCast(initialNode, ExpressionSyntax), DirectCast(replacementNode, ExpressionSyntax), initialModel, CancellationToken.None).ReplacementChangesSemantics()
        End Function
    End Class
End Namespace
