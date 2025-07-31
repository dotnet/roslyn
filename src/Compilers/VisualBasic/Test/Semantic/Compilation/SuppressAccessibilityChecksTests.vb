' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Test.Utilities
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UnitTests
    Public Class SuppressAccessibilityChecksTests
        Inherits BasicTestBase

        Private Function GetSemanticModelWithIgnoreAccessibility() As SemanticModel

            Dim compilationA = CreateVisualBasicCompilation(<![CDATA[
Class A

    Private _num As Integer

    Private Function M() As A

        Return New A()
    
    End Function
End Class
]]>.Value)

            Dim referenceA = MetadataReference.CreateFromStream(compilationA.EmitToStream())

            Dim compilationB = CreateVisualBasicCompilation(<![CDATA[
Class B

    Sub Main()

        Dim v = New A().M()

    End Sub

End Class

]]>.Value, referencedAssemblies:=New MetadataReference() {referenceA},
                                                            compilationOptions:=TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim syntaxTree2 = compilationB.SyntaxTrees(0)
            Return compilationB.GetSemanticModel(syntaxTree2, ignoreAccessibility:=True)
        End Function

        <Fact>
        Public Sub TestAccessPrivateMemberOfInternalType()
            Dim semanticModel = GetSemanticModelWithIgnoreAccessibility()
            Dim invocation = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Dim position = invocation.FullSpan.Start

            Assert.Equal("A", semanticModel.GetTypeInfo(invocation).Type.Name)
            Assert.Equal("M", semanticModel.GetSymbolInfo(invocation).Symbol.Name)

            Assert.NotEmpty(semanticModel.LookupSymbols(position, name:="A"))

        End Sub

        <Fact>
        Public Sub TestAccessChecksInSpeculativeExpression()
            Dim semanticModel = GetSemanticModelWithIgnoreAccessibility()
            Dim invocation = semanticModel.SyntaxTree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()

            Dim speculativeInvocation = SyntaxFactory.ParseExpression("New A().M()._num")
            Dim position = invocation.FullSpan.Start

            Assert.Equal("Int32", semanticModel.GetSpeculativeTypeInfo(position, speculativeInvocation, SpeculativeBindingOption.BindAsExpression).Type.Name)
            Assert.Equal("_num", semanticModel.GetSpeculativeSymbolInfo(position, speculativeInvocation, SpeculativeBindingOption.BindAsExpression).Symbol.Name)
        End Sub

        <Fact>
        Public Sub TestAccessChecksInSpeculativeSemanticModel()
            Dim semanticModel = GetSemanticModelWithIgnoreAccessibility()
            Dim syntaxTree = semanticModel.SyntaxTree
            Dim invocation = syntaxTree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()
            Dim position = invocation.FullSpan.Start

            Dim speculativeSemanticModel As SemanticModel = Nothing
            Dim statement = DirectCast(SyntaxFactory.ParseExecutableStatement("Dim v = New A().M()"), ExecutableStatementSyntax)

            semanticModel.TryGetSpeculativeSemanticModel(position, statement, speculativeSemanticModel)
            Dim creationExpression = speculativeSemanticModel.GetTypeInfo(statement.DescendantNodes().OfType(Of ObjectCreationExpressionSyntax)().Single())

            Assert.Equal("A", creationExpression.Type.Name)
        End Sub

        <Fact>
        Public Sub AccessChecksInsideLambdaExpression()

            Dim source = <![CDATA[
        Imports System.Collections.Generic

        Class P
            Private _p As Boolean
        End Class

        Class C

            Shared Sub M()

                Dim tmp = New List(Of P)()
                Dim answer = tmp.Find(Function(a) a._p)

            End Sub
        End Class
        ]]>.Value

            Dim tree = SyntaxFactory.ParseSyntaxTree(source)
            Dim comp = CreateCompilationWithMscorlib40(tree)
            Dim model = comp.GetSemanticModel(tree, ignoreAccessibility:=True)

            Dim root = tree.GetCompilationUnitRoot()
            Dim expr = DirectCast(root.DescendantNodes().OfType(Of SingleLineLambdaExpressionSyntax)().Single().Body, ExpressionSyntax)

            Dim symbolInfo = model.GetSpeculativeSymbolInfo(expr.SpanStart, SyntaxFactory.ParseExpression("a._p"), SpeculativeBindingOption.BindAsExpression)

            Assert.Equal("_p", symbolInfo.Symbol.Name)
        End Sub

        <Fact>
        Public Sub AccessCheckCrossAssemblyPrivateExtensions()

            Dim source =
                <compilation name="ext">
                    <file name="a.vb">
Imports System.Runtime.CompilerServices

Public Class A

    Function M() As A
    
        Return New A()

    End Function

    Friend _num As Integer

End Class

Friend Module E

    &lt;Extension&gt;
    Friend Function InternalExtension(theClass As A, newNum As Integer) As Integer
    
        theClass._num = newNum
        
        Return newNum

    End Function
End Module

Namespace System.Runtime.CompilerServices

    &lt;AttributeUsage(AttributeTargets.Assembly Or AttributeTargets.Class Or AttributeTargets.Method)&gt;
    Class ExtensionAttribute
        Inherits Attribute
    End Class

End Namespace

                    </file>
                </compilation>

            Dim compilationA = CreateCompilationWithMscorlib40AndVBRuntime(source)

            Dim referenceA = MetadataReference.CreateFromStream(compilationA.EmitToStream())

            Dim compilationB = CreateCompilationWithMscorlib40(New String() {<![CDATA[
Class B 

    Sub Main() 
    
        Dim t = New A().M()

    End Sub
End Class
]]>.Value}, New MetadataReference() {referenceA}, TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.All))

            Dim syntaxTree = compilationB.SyntaxTrees(0)
            Dim semanticModel = compilationB.GetSemanticModel(syntaxTree, ignoreAccessibility:=True)

            Dim invocation = syntaxTree.GetRoot().DescendantNodes().OfType(Of InvocationExpressionSyntax)().Single()

            Assert.Equal("A", semanticModel.GetTypeInfo(invocation).Type.Name)
            Assert.Equal("M", semanticModel.GetSymbolInfo(invocation).Symbol.Name)

            Dim speculativeInvocation = SyntaxFactory.ParseExpression("new A().InternalExtension(67)")
            Dim position = invocation.FullSpan.Start

            Assert.Equal("Int32", semanticModel.GetSpeculativeTypeInfo(position, speculativeInvocation, SpeculativeBindingOption.BindAsExpression).Type.Name)
            Assert.Equal("InternalExtension", semanticModel.GetSpeculativeSymbolInfo(position, speculativeInvocation, SpeculativeBindingOption.BindAsExpression).Symbol.Name)

            Assert.NotNull(semanticModel.LookupSymbols(position, name:="A"))
        End Sub

        <Fact>
        Public Sub TestGetSpeculativeSemanticModelForPropertyAccessorBody()

            Dim source = <![CDATA[
Class R

    Private _p As Integer

End Class

Class C
    Inherits R 
    
    Private Property M As Integer
        Set
            Dim y As Integer = 1000
        End Set
    End Property
End Class
]]>.Value

            Dim compilationA = CreateCompilationWithMscorlib40(SyntaxFactory.ParseSyntaxTree(source))

            Dim blockStatement = SyntaxFactory.ParseSyntaxTree(<![CDATA[
                                                               
    Private Property M As Integer
        Set
           Dim z As Integer = 0

           _p = 123
        End Set
    End Property
]]>.Value).GetRoot()

            Dim tree = compilationA.SyntaxTrees(0)
            Dim root = tree.GetCompilationUnitRoot()
            Dim typeDecl = DirectCast(root.Members(1), ClassBlockSyntax)
            Dim propertyDecl = DirectCast(typeDecl.Members(0), PropertyBlockSyntax)
            Dim methodDecl = propertyDecl.Accessors(0)
            Dim model = compilationA.GetSemanticModel(tree, ignoreAccessibility:=True)

            Dim speculatedMethod =
                propertyDecl.ReplaceNode(propertyDecl.Accessors(0), blockStatement.ChildNodes().OfType(Of PropertyBlockSyntax).Single().Accessors(0))

            Dim speculativeModel As SemanticModel = Nothing

            Dim success =
                model.TryGetSpeculativeSemanticModelForMethodBody(
                    methodDecl.Statements(0).SpanStart, speculatedMethod.Accessors(0), speculativeModel)

            Assert.True(success)
            Assert.NotNull(speculativeModel)

            Dim privateCandidate =
                speculativeModel.SyntaxTree.GetRoot() _
                .DescendantNodes() _
                .OfType(Of IdentifierNameSyntax)() _
                .Single(Function(s) s.Identifier.ValueText = "_p")

            Dim symbolSpeculation =
                speculativeModel.GetSpeculativeSymbolInfo(privateCandidate.FullSpan.Start, privateCandidate,
                    SpeculativeBindingOption.BindAsExpression)

            Dim typeSpeculation =
                speculativeModel.GetSpeculativeTypeInfo(privateCandidate.FullSpan.Start, privateCandidate,
                                SpeculativeBindingOption.BindAsExpression)

            Assert.Equal("_p", symbolSpeculation.Symbol.Name)
            Assert.Equal("Int32", typeSpeculation.Type.Name)

        End Sub
    End Class
End Namespace
