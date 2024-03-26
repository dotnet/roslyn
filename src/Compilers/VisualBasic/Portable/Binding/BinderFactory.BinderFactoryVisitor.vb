' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Partial Friend Class BinderFactory

        Friend NotInheritable Class BinderFactoryVisitor
            Inherits VisualBasicSyntaxVisitor(Of Binder)

            Private _position As Integer
            Private _factory As BinderFactory

            Public Sub Initialize(factory As BinderFactory, position As Integer)
                Me._factory = factory
                Me._position = position
            End Sub

            Public Sub Clear()
                _factory = Nothing
                _position = 0
            End Sub

            Public Overrides Function VisitXmlCrefAttribute(node As XmlCrefAttributeSyntax) As Binder
                Dim trivia As StructuredTriviaSyntax = node.EnclosingStructuredTrivia

                If trivia IsNot Nothing AndAlso trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                    Return _factory.CreateDocumentationCommentBinder(DirectCast(trivia, DocumentationCommentTriviaSyntax), DocumentationCommentBinder.BinderType.Cref)
                End If

                Return MyBase.VisitXmlCrefAttribute(node)
            End Function

            Public Overrides Function VisitXmlNameAttribute(node As XmlNameAttributeSyntax) As Binder
                Dim trivia As StructuredTriviaSyntax = node.EnclosingStructuredTrivia

                If trivia IsNot Nothing AndAlso trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                    Dim binderType As DocumentationCommentBinder.BinderType =
                        DocumentationCommentBinder.GetBinderTypeForNameAttribute(node)
                    If binderType <> DocumentationCommentBinder.BinderType.None Then
                        Return _factory.CreateDocumentationCommentBinder(DirectCast(trivia, DocumentationCommentTriviaSyntax), binderType)
                    End If
                End If

                Return MyBase.VisitXmlNameAttribute(node)
            End Function

            Public Overrides Function VisitXmlAttribute(node As XmlAttributeSyntax) As Binder
                If node.Name.Kind = SyntaxKind.XmlName Then
                    Dim attrName = DirectCast(node.Name, XmlNameSyntax).LocalName.ValueText

                    If DocumentationCommentXmlNames.AttributeEquals(attrName, DocumentationCommentXmlNames.CrefAttributeName) Then
                        Dim trivia As StructuredTriviaSyntax = node.EnclosingStructuredTrivia
                        If trivia IsNot Nothing AndAlso trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                            Return _factory.CreateDocumentationCommentBinder(DirectCast(trivia, DocumentationCommentTriviaSyntax), DocumentationCommentBinder.BinderType.Cref)
                        End If

                    ElseIf DocumentationCommentXmlNames.AttributeEquals(attrName, DocumentationCommentXmlNames.NameAttributeName) Then
                        Dim trivia As StructuredTriviaSyntax = node.EnclosingStructuredTrivia
                        If trivia IsNot Nothing AndAlso trivia.Kind = SyntaxKind.DocumentationCommentTrivia Then
                            Dim binderType As DocumentationCommentBinder.BinderType =
                                DocumentationCommentBinder.GetBinderTypeForNameAttribute(node)
                            If binderType <> DocumentationCommentBinder.BinderType.None Then
                                Return _factory.CreateDocumentationCommentBinder(DirectCast(trivia, DocumentationCommentTriviaSyntax), binderType)
                            End If
                        End If
                    End If
                End If

                Return MyBase.VisitXmlAttribute(node)
            End Function

            ' Visitors for different kinds of nodes.

            Private Function VisitMethodBaseDeclaration(methodBaseSyntax As MethodBaseSyntax) As Binder
                Dim possibleParentBlock = TryCast(methodBaseSyntax.Parent, MethodBlockBaseSyntax)
                Dim parentForEnclosingBinder As VisualBasicSyntaxNode = If(possibleParentBlock IsNot Nothing, possibleParentBlock.Parent, methodBaseSyntax.Parent)

                Return GetBinderForNodeAndUsage(methodBaseSyntax, NodeUsage.MethodFull, parentForEnclosingBinder, _position)
            End Function

            Public Overrides Function DefaultVisit(node As SyntaxNode) As Binder
                If _factory.InScript AndAlso node.Parent.Kind = SyntaxKind.CompilationUnit Then
                    ' Use CompilationUnitSyntax as a key to the cache for all statements to get a single shared instance of TopLeveCodeBinder
                    Return GetBinderForNodeAndUsage(DirectCast(node.Parent, VisualBasicSyntaxNode), NodeUsage.TopLevelExecutableStatement, DirectCast(node.Parent, VisualBasicSyntaxNode), _position)
                End If

                Return Nothing
            End Function

            Public Overrides Function VisitCompilationUnit(node As CompilationUnitSyntax) As Binder
                Return GetBinderForNodeAndUsage(node, If(_factory.InScript, NodeUsage.ScriptCompilationUnit, NodeUsage.CompilationUnit))
            End Function

            Public Overrides Function VisitNamespaceBlock(nsBlockSyntax As NamespaceBlockSyntax) As Binder
                ' The binder should be used only within the interior of the namespace block. The interior is from
                ' the end of Begin to the beginning of End (unless End is missing)
                If SyntaxFacts.InBlockInterior(nsBlockSyntax, _position) Then
                    Return GetBinderForNodeAndUsage(nsBlockSyntax, NodeUsage.NamespaceBlockInterior, nsBlockSyntax.Parent, _position)
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitEnumMemberDeclaration(node As EnumMemberDeclarationSyntax) As Binder
                If IsNotNothingAndContains(node.Initializer, _position) Then
                    Return GetBinderForNodeAndUsage(node, NodeUsage.FieldOrPropertyInitializer, node.Parent, _position)
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitVariableDeclarator(node As VariableDeclaratorSyntax) As Binder
                If IsNotNothingAndContains(node.Initializer, _position) OrElse IsNotNothingAndContains(TryCast(node.AsClause, AsNewClauseSyntax), _position) Then
                    Return GetBinderForNodeAndUsage(node, NodeUsage.FieldOrPropertyInitializer, node.Parent, _position)
                End If

                For Each name In node.Names
                    If IsNotNothingAndContains(name.ArrayBounds, _position) Then
                        Return GetBinderForNodeAndUsage(name, NodeUsage.FieldArrayBounds, node.Parent, _position)
                    End If
                Next

                Return Nothing
            End Function

            Public Overrides Function VisitPropertyStatement(node As PropertyStatementSyntax) As Binder
                If IsNotNothingAndContains(node.Initializer, _position) OrElse IsNotNothingAndContains(TryCast(node.AsClause, AsNewClauseSyntax), _position) Then
                    Return GetBinderForNodeAndUsage(node, NodeUsage.FieldOrPropertyInitializer, node.Parent, _position)
                Else
                    Return Nothing
                End If
            End Function

            Public Overrides Function VisitModuleBlock(ByVal moduleSyntax As ModuleBlockSyntax) As Binder
                Return GetBinderForNodeAndUsage(moduleSyntax.BlockStatement, NodeUsage.TypeBlockFull, moduleSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitClassBlock(ByVal classSyntax As ClassBlockSyntax) As Binder
                Return GetBinderForNodeAndUsage(classSyntax.BlockStatement, NodeUsage.TypeBlockFull, classSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitStructureBlock(ByVal structureSyntax As StructureBlockSyntax) As Binder
                Return GetBinderForNodeAndUsage(structureSyntax.BlockStatement, NodeUsage.TypeBlockFull, structureSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitAttribute(node As AttributeSyntax) As Binder
                Return GetBinderForNodeAndUsage(node, NodeUsage.Attribute, node.Parent, _position)
            End Function

            Public Overrides Function VisitInterfaceBlock(ByVal interfaceSyntax As InterfaceBlockSyntax) As Binder
                Return GetBinderForNodeAndUsage(interfaceSyntax.BlockStatement, NodeUsage.TypeBlockFull, interfaceSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitEnumBlock(enumBlockSyntax As EnumBlockSyntax) As Binder
                Return GetBinderForNodeAndUsage(enumBlockSyntax.EnumStatement, NodeUsage.EnumBlockFull, enumBlockSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitDelegateStatement(delegateSyntax As DelegateStatementSyntax) As Binder
                Return GetBinderForNodeAndUsage(delegateSyntax, NodeUsage.DelegateDeclaration, delegateSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitInheritsStatement(inheritsSyntax As InheritsStatementSyntax) As Binder
                Return GetBinderForNodeAndUsage(inheritsSyntax, NodeUsage.InheritsStatement, inheritsSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitImplementsStatement(implementsSyntax As ImplementsStatementSyntax) As Binder
                Return GetBinderForNodeAndUsage(implementsSyntax, NodeUsage.ImplementsStatement, implementsSyntax.Parent, _position)
            End Function

            ' All of these kinds of syntax nodes declare method symbols.

            Public Overrides Function VisitMethodStatement(node As MethodStatementSyntax) As Binder
                ' We might get here for an error scenario when 'Sub' or 'Function' keyword starts the body of a single-line statement lambda.
                ' We should not be treating it as a valid method declaration.
                If node.ContainsDiagnostics AndAlso node.Parent.Kind = SyntaxKind.SingleLineSubLambdaExpression Then
                    Return DefaultVisit(node)
                End If

                Return VisitMethodBaseDeclaration(node)
            End Function

            Public Overrides Function VisitSubNewStatement(node As SubNewStatementSyntax) As Binder
                Return VisitMethodBaseDeclaration(node)
            End Function

            Public Overrides Function VisitOperatorStatement(node As OperatorStatementSyntax) As Binder
                Return VisitMethodBaseDeclaration(node)
            End Function

            Public Overrides Function VisitDeclareStatement(node As DeclareStatementSyntax) As Binder
                Return VisitMethodBaseDeclaration(node)
            End Function

            Public Overrides Function VisitAccessorStatement(node As AccessorStatementSyntax) As Binder
                Return VisitMethodBaseDeclaration(node)
            End Function

            Public Overrides Function VisitParameter(node As ParameterSyntax) As Binder
                If IsNotNothingAndContains(node.Default, _position) Then
                    Return GetBinderForNodeAndUsage(node, NodeUsage.ParameterDefaultValue, node.Parent, _position)
                End If
                Return Nothing
            End Function

            Private Function VisitMethodBlockBase(methodBlockSyntax As MethodBlockBaseSyntax, begin As MethodBaseSyntax) As Binder
                Dim usage As NodeUsage
                If SyntaxFacts.InBlockInterior(methodBlockSyntax, _position) Then
                    ' We're in the interior from end of Begin to the beginning of End (unless the End is missing...)
                    usage = NodeUsage.MethodInterior
                Else
                    usage = NodeUsage.MethodFull
                End If

                Return GetBinderForNodeAndUsage(begin, usage, methodBlockSyntax.Parent, _position)
            End Function

            Public Overrides Function VisitMethodBlock(node As MethodBlockSyntax) As Binder
                Return VisitMethodBlockBase(node, node.BlockStatement)
            End Function

            Public Overrides Function VisitConstructorBlock(node As ConstructorBlockSyntax) As Binder
                Return VisitMethodBlockBase(node, node.BlockStatement)
            End Function

            Public Overrides Function VisitOperatorBlock(node As OperatorBlockSyntax) As Binder
                Return VisitMethodBlockBase(node, node.BlockStatement)
            End Function

            Public Overrides Function VisitAccessorBlock(node As AccessorBlockSyntax) As Binder
                Return VisitMethodBlockBase(node, node.BlockStatement)
            End Function

            Public Overrides Function VisitPropertyBlock(node As PropertyBlockSyntax) As Binder
                Return GetBinderForNodeAndUsage(node.PropertyStatement, NodeUsage.PropertyFull, node.Parent, _position)
            End Function

            Public Overrides Function VisitImportsStatement(node As ImportsStatementSyntax) As Binder
                Return BinderBuilder.CreateBinderForSourceFileImports(_factory._sourceModule, _factory._tree)
            End Function

            ' Given a node and usage, find the correct binder to use. Use the cache first, if not in the cache, then
            ' create a new binder. The parent node and position are used if we need to find an enclosing binder unless specified explicitly.
            Private Function GetBinderForNodeAndUsage(node As VisualBasicSyntaxNode,
                                                      usage As NodeUsage,
                                                      Optional parentNode As VisualBasicSyntaxNode = Nothing,
                                                      Optional position As Integer = -1
                                                      ) As Binder

                Return _factory.GetBinderForNodeAndUsage(node, usage, parentNode, position)
            End Function

            Private Shared Function IsNotNothingAndContains(nodeOpt As VisualBasicSyntaxNode, position As Integer) As Boolean
                Return (nodeOpt IsNot Nothing) AndAlso SyntaxFacts.InSpanOrEffectiveTrailingOfNode(nodeOpt, position)
            End Function
        End Class

    End Class

End Namespace
