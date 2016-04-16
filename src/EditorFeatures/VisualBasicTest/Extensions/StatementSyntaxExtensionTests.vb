' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions
Imports Microsoft.CodeAnalysis.VisualBasic.Extensions.ContextQuery
Imports System.Threading

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Extensions
    Public Class StatementSyntaxExtensionTests

        Private Sub TestStatementDeclarationWithPublicModifier(Of T As StatementSyntax)(node As T)
            Dim modifierList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            Dim newNode = DirectCast(node.WithModifiers(modifierList), T)
            Dim actual = newNode.GetModifiers().First().ToString()
            Assert.Equal("Public", actual)
        End Sub

        Private Sub VerifyTokenName(Of T As DeclarationStatementSyntax)(code As String, expectedName As String)
            Dim node = SyntaxFactory.ParseCompilationUnit(code).DescendantNodes.OfType(Of T).First()
            Dim actualNameToken = node.GetNameToken()
            Assert.Equal(expectedName, actualNameToken.ToString())
        End Sub

        <Fact>
        Public Sub MethodReturnType()
            Dim methodDeclaration = SyntaxFactory.FunctionStatement(attributeLists:=Nothing,
                                                              modifiers:=Nothing,
                                                              identifier:=SyntaxFactory.Identifier("F1"),
                                                              typeParameterList:=Nothing,
                                                              parameterList:=Nothing,
                                                              asClause:=SyntaxFactory.SimpleAsClause(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntegerKeyword))),
                                                              handlesClause:=Nothing,
                                                              implementsClause:=Nothing)
            Assert.True(methodDeclaration.HasReturnType())

            Dim result = methodDeclaration.GetReturnType()
            Dim returnTypeName = result.ToString()
            Assert.Equal("Integer", returnTypeName)
        End Sub

        <Fact>
        Public Sub PropertyReturnType()
            Dim propertyDeclaration = SyntaxFactory.PropertyStatement(attributeLists:=Nothing,
                                                               modifiers:=Nothing,
                                                               identifier:=SyntaxFactory.Identifier("P1"),
                                                               parameterList:=Nothing,
                                                               asClause:=SyntaxFactory.SimpleAsClause(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.ByteKeyword))),
                                                               initializer:=Nothing,
                                                               implementsClause:=Nothing)
            Assert.True(propertyDeclaration.HasReturnType())

            Dim result = propertyDeclaration.GetReturnType()
            Dim returnTypeName = result.ToString()
            Assert.Equal("Byte", returnTypeName)
        End Sub

        Private Sub TestTypeBlockWithPublicModifier(Of T As TypeBlockSyntax)(code As String)
            Dim node = SyntaxFactory.ParseCompilationUnit(code).Members.First()
            Dim modifierList = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
            Dim newNode = DirectCast(node.WithModifiers(modifierList), T)
            Dim actual = newNode.GetModifiers().First().ToString()
            Assert.Equal("Public", actual)
        End Sub

        <Fact>
        Public Sub GetClassStatementModifiers()
            Dim code = <String>Public Class C</String>.Value
            Dim node = SyntaxFactory.ParseCompilationUnit(code).DescendantNodes.OfType(Of ClassStatementSyntax).First()
            Dim actualModifierName = node.Modifiers().First().ToString()
            Assert.Equal("Public", actualModifierName)
        End Sub

        <Fact>
        Public Sub GetEnumStatementModifiers()
            Dim code = <String>Public Enum E</String>.Value
            Dim node = SyntaxFactory.ParseCompilationUnit(code).DescendantNodes.OfType(Of EnumStatementSyntax).First()
            Dim actualModifierName = node.Modifiers().First().ToString()
            Assert.Equal("Public", actualModifierName)
        End Sub

        <Fact>
        Public Sub InterfaceBlockWithPublicModifier()
            Dim code = <String>Interface I
End Interface</String>.Value

            TestTypeBlockWithPublicModifier(Of InterfaceBlockSyntax)(code)
        End Sub

        <Fact>
        Public Sub ModuleBlockWithPublicModifier()
            Dim code = <String>Module M
End Module</String>.Value

            TestTypeBlockWithPublicModifier(Of ModuleBlockSyntax)(code)
        End Sub

        <Fact>
        Public Sub StructureBlockWithPublicModifier()
            Dim code = <string>Structure S
End Structure</string>.Value

            TestTypeBlockWithPublicModifier(Of StructureBlockSyntax)(code)
        End Sub

        <Fact>
        Public Sub EnumBlockWithPublicModifier()
            Dim code = <String>Enum E
End Enum</String>.Value

            Dim node = DirectCast(SyntaxFactory.ParseCompilationUnit(code).Members.First(), EnumBlockSyntax)
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub ClassStatementWithPublicModifier()
            Dim node = SyntaxFactory.ClassStatement(SyntaxFactory.Identifier("C"))
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub EnumStatementWithPublicModifier()
            Dim node = SyntaxFactory.EnumStatement(SyntaxFactory.Identifier("E"))
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub FieldDeclarationWithPublicModifier()
            Dim code = <String>Class C
    dim _field as Integer = 1
End Class</String>.Value
            Dim node = SyntaxFactory.ParseCompilationUnit(code).DescendantNodes.OfType(Of FieldDeclarationSyntax).First()
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub EventBlockWithPublicModifier()
            Dim code = <String>Custom Event E As EventHandler
End Event</String>.Value
            Dim node = SyntaxFactory.ParseCompilationUnit(code).DescendantNodes.OfType(Of EventBlockSyntax).First()
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub EventStatementWithPublicModifier()
            Dim node = SyntaxFactory.EventStatement(SyntaxFactory.Identifier("E"))
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub PropertyBlockWithPublicModifier()
            Dim code = <String>Property P as Integer
End Property</String>.Value
            Dim node = SyntaxFactory.ParseCompilationUnit(code).DescendantNodes.OfType(Of PropertyBlockSyntax).First()
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub SubBlockWithPublicModifier()
            Dim code = <String>Sub Foo
End Sub</String>.Value
            Dim node = SyntaxFactory.ParseCompilationUnit(code).DescendantNodes.OfType(Of MethodBlockSyntax).First()
            TestStatementDeclarationWithPublicModifier(node)
        End Sub

        <Fact>
        Public Sub VerifyClassNameToken()
            Dim code = <String>Class C
End Class</String>.Value
            VerifyTokenName(Of ClassBlockSyntax)(code, "C")
        End Sub

        <Fact>
        Public Sub VerifyInterfaceNameToken()
            Dim code = <String>Interface I
End Interface</String>.Value
            VerifyTokenName(Of InterfaceBlockSyntax)(code, "I")
        End Sub

        <Fact>
        Public Sub VerifyStructureNameToken()
            Dim code = <String>Structure S
End Structure</String>.Value
            VerifyTokenName(Of StructureBlockSyntax)(code, "S")
        End Sub

        <Fact>
        Public Sub VerifyModuleNameToken()
            Dim code = <String>Module M
End Module</String>.Value
            VerifyTokenName(Of ModuleBlockSyntax)(code, "M")
        End Sub

        <Fact>
        Public Sub VerifyStructureStatementNameToken()
            Dim code = <String>Structure SS
</String>.Value
            VerifyTokenName(Of StructureStatementSyntax)(code, "SS")
        End Sub

        <Fact>
        Public Sub VerifyConstructorNameTokenIsNothing()
            Dim code = <String>Class C
    Sub New()
End Class</String>.Value
            VerifyTokenName(Of SubNewStatementSyntax)(code, "")
        End Sub

        <Fact, WorkItem(552823, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/552823")>
        Public Sub TestIsInStatementBlockOfKindForBrokenCode()
            Dim code = <String>End Sub
End Module
End Namespace
d</String>.Value

            Dim tree = SyntaxFactory.ParseSyntaxTree(code)

            Dim token = tree.GetRoot() _
                            .DescendantTokens() _
                            .Where(Function(t) t.Kind = SyntaxKind.NamespaceKeyword) _
                            .First()

            For position = token.SpanStart To token.Span.End
                Dim targetToken = tree.GetTargetToken(position, CancellationToken.None)
                tree.IsInStatementBlockOfKind(position, targetToken, CancellationToken.None)
            Next
        End Sub
    End Class
End Namespace
