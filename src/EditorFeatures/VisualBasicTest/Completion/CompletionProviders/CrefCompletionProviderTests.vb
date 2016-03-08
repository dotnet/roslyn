' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Editor.UnitTests
Imports Microsoft.CodeAnalysis.Editor.UnitTests.Workspaces
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.UnitTests.Completion.CompletionProviders
    Public Class CrefCompletionProviderTests
        Inherits AbstractVisualBasicCompletionProviderTests

        Public Sub New(workspaceFixture As VisualBasicTestWorkspaceFixture)
            MyBase.New(workspaceFixture)
        End Sub

        Friend Overrides Function CreateCompletionProvider() As CompletionListProvider
            Return New CrefCompletionProvider()
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotOutsideCref() As Task
            Dim text = <File>
Class C
    ''' $$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotOutsideCref2() As Task
            Dim text = <File>
Class C
    $$
    Sub Foo()
    End Sub
End Class
</File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotOutsideCref3() As Task
            Dim text = <File>
Class C
    Sub Foo()
        Me.$$
    End Sub
End Class
</File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterCrefOpenQuote() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="$$
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            Await VerifyAnyItemExistsAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestRightSideOfQualifiedName() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program.$$"
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            Await VerifyItemExistsAsync(text, "Foo()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotInTypeParameterContext() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of $$
''' </summary>
Class Program(Of T)
    Sub Foo()
    End Sub
End Class]]></File>.Value

            Await VerifyItemIsAbsentAsync(text, "Integer")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInSignature_FirstParameter() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo($$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Integer")
            Await VerifyItemIsAbsentAsync(text, "Foo(Integer")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestInSignature_SecondParameter() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo(Integer, $$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Integer")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterSignature() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo(Integer, Integer)$$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNotAfterDotAfterSignature() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Program(Of T).Foo(Integer, Integer).$$"
''' </summary>
Class Program(Of T)
    Sub Foo(z as Integer, q as Integer)
    End Sub
End Class]]></File>.Value

            Await VerifyNoItemsExistAsync(text)
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestMethodParametersIncluded() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="Program(Of T).$$"
''' </summary>
Class Program(Of T)
    Sub Foo(ByRef z As Integer, ByVal x As Integer, ParamArray xx As Integer())
    End Sub
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Foo(ByRef Integer, Integer, Integer())")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestTypesSuggestedWithTypeParameters() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="$$"
''' </summary>
Class Program(Of TTypeParameter)
End Class

Class Program
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Program")
            Await VerifyItemExistsAsync(text, "Program(Of TTypeParameter)")
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOperators() As Task
            Dim text = <File><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Class C
    ''' <summary>
    '''  <see cref="<see cref="C.$$"
    ''' </summary>
    ''' <param name="c"></param>
    ''' <returns></returns>
    Public Shared Operator +(c As C)

    End Operator
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Operator +(C)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestModOperator() As Task
            Dim text = <File><![CDATA[Imports System
Imports System.Collections.Generic
Imports System.Linq

Module Program
    Sub Main(args As String())

    End Sub
End Module

Class C
    ''' <summary>
    '''  <see cref="<see cref="C.$$"
    ''' </summary>
    ''' <param name="c"></param>
    ''' <returns></returns>
    Public Shared Operator Mod (c As C, a as Integer)

    End Operator
End Class]]></File>.Value

            Await VerifyItemExistsAsync(text, "Operator Mod(C, Integer)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestConstructorsShown() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
    Sub New(x as Integer)
    End Sub
End Class
]]></File>.Value

            Await VerifyItemExistsAsync(text, "New(Integer)")
        End Function
        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAfterNamespace() As Task
            Dim text = <File><![CDATA[
Imports System
''' <summary>
''' <see cref="System.$$"/>
''' </summary>
Class C
    Sub New(x as Integer)
    End Sub
End Class
]]></File>.Value

            Await VerifyItemExistsAsync(text, "String")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestParameterizedProperties() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
    Public Property Item(x As Integer) As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
        End Set
    End Property
    Public Property Item(x As Integer, y As String) As Integer
        Get
            Return 1
        End Get
        Set(value As Integer)
        End Set
    End Property
End Class
 

]]></File>.Value

            Await VerifyItemExistsAsync(text, "Item(Integer)")
            Await VerifyItemExistsAsync(text, "Item(Integer, String)")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoIdentifierEscaping() As Task
            Dim text = <File><![CDATA[
''' <see cref="A.$$"/>
''' </summary>
Class A
End Class

]]></File>.Value

            Await VerifyItemExistsAsync(text, "GetType()")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestNoCommitOnParen() As Task
            Dim text = <File><![CDATA[
''' <summary>
''' <see cref="C.$$"/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Dim expected = <File><![CDATA[
''' <summary>
''' <see cref="C.("/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Await VerifyProviderCommitAsync(text, "bar(Integer, Integer)", expected, "("c, "bar")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestAllowTypingTypeParameters() As Task
            Dim text = <File><![CDATA[
Imports System.Collections.Generic
''' <summary>
''' <see cref="$$"/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Dim expected = <File><![CDATA[
Imports System.Collections.Generic
''' <summary>
''' <see cref=" "/>
''' </summary>
Class C
Sub bar(x As Integer, y As Integer)
End Sub
End Class
]]></File>.Value

            Await VerifyProviderCommitAsync(text, "List(Of T)", expected, " "c, "List(Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOfAfterParen() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Foo($$
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            Await VerifyItemExistsAsync(text, "Of")
        End Function

        <Fact, Trait(Traits.Feature, Traits.Features.Completion)>
        Public Async Function TestOfNotAfterComma() As Task
            Dim text = <File><![CDATA[
Imports System

''' <summary>
''' <see cref="Foo(a, $$
''' </summary>
Module Program
    Sub Foo()
    End Sub
End Module]]></File>.Value

            Await VerifyItemIsAbsentAsync(text, "Of")
        End Function

        Public Async Function TestCrefCompletionSpeculatesOutsideTrivia() As Task
            Dim text = <a><![CDATA[
Class C
    ''' <see cref="$$
    Sub foo()
    End Sub
End Class]]></a>.Value.NormalizeLineEndings()
            Dim exportProvider = MinimalTestExportProvider.CreateExportProvider(TestExportProvider.EntireAssemblyCatalogWithCSharpAndVisualBasic.WithPart(GetType(PickySemanticFactsService)))

            Using workspace = Await TestWorkspace.CreateAsync(LanguageNames.VisualBasic, New VisualBasicCompilationOptions(OutputKind.ConsoleApplication), New VisualBasicParseOptions(), {text}, exportProvider)
                ' This test uses MEF to compose in an ISyntaxFactsService that 
                ' asserts it isn't asked to speculate on nodes inside documentation trivia.
                ' This verifies that the provider is asking for a speculative SemanticModel
                ' by walking to the node the documentation is attached to. 

                Dim provider = New CrefCompletionProvider()
                Dim hostDocument = workspace.DocumentWithCursor
                Dim document = workspace.CurrentSolution.GetDocument(hostDocument.Id)
                Dim completionList = Await GetCompletionListAsync(provider, document, hostDocument.CursorPosition.Value, CompletionTriggerInfo.CreateInvokeCompletionTriggerInfo())
            End Using
        End Function

        <ExportLanguageServiceAttribute(GetType(ISyntaxFactsService), LanguageNames.VisualBasic, ServiceLayer.Host), [Shared]>
        Friend Class PickySemanticFactsService
            Implements ISyntaxFactsService

            Public ReadOnly Property IsCaseSensitive As Boolean Implements ISyntaxFactsService.IsCaseSensitive
                Get
                    Throw New NotImplementedException()
                End Get
            End Property

            Public Sub GetNameAndArityOfSimpleName(node As SyntaxNode, ByRef name As String, ByRef arity As Integer) Implements ISyntaxFactsService.GetNameAndArityOfSimpleName
                Throw New NotImplementedException()
            End Sub

            Public Function ContainsInMemberBody(node As SyntaxNode, span As TextSpan) As Boolean Implements ISyntaxFactsService.ContainsInMemberBody
                Throw New NotImplementedException()
            End Function

            Public Function ConvertToSingleLine(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.ConvertToSingleLine
                Throw New NotImplementedException()
            End Function

            Public Function FindTokenOnLeftOfPosition(node As SyntaxNode, position As Integer, Optional includeSkipped As Boolean = True, Optional includeDirectives As Boolean = False, Optional includeDocumentationComments As Boolean = False) As SyntaxToken Implements ISyntaxFactsService.FindTokenOnLeftOfPosition
                Throw New NotImplementedException()
            End Function

            Public Function FindTokenOnRightOfPosition(node As SyntaxNode, position As Integer, Optional includeSkipped As Boolean = True, Optional includeDirectives As Boolean = False, Optional includeDocumentationComments As Boolean = False) As SyntaxToken Implements ISyntaxFactsService.FindTokenOnRightOfPosition
                Throw New NotImplementedException()
            End Function

            Public Function GetBindableParent(token As SyntaxToken) As SyntaxNode Implements ISyntaxFactsService.GetBindableParent
                Throw New NotImplementedException()
            End Function

            Public Function GetConstructors(root As SyntaxNode, cancellationToken As CancellationToken) As IEnumerable(Of SyntaxNode) Implements ISyntaxFactsService.GetConstructors
                Throw New NotImplementedException()
            End Function

            Public Function GetContainingMemberDeclaration(root As SyntaxNode, position As Integer, Optional useFullSpan As Boolean = True) As SyntaxNode Implements ISyntaxFactsService.GetContainingMemberDeclaration
                Throw New NotImplementedException()
            End Function

            Public Function GetContainingTypeDeclaration(root As SyntaxNode, position As Integer) As SyntaxNode Implements ISyntaxFactsService.GetContainingTypeDeclaration
                Throw New NotImplementedException()
            End Function

            Public Function GetContainingVariableDeclaratorOfFieldDeclaration(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetContainingVariableDeclaratorOfFieldDeclaration
                Throw New NotImplementedException()
            End Function

            Public Function GetExpressionOfArgument(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfArgument
                Throw New NotImplementedException()
            End Function

            Public Function GetExpressionOfConditionalMemberAccessExpression(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfConditionalMemberAccessExpression
                Throw New NotImplementedException()
            End Function

            Public Function GetExpressionOfMemberAccessExpression(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetExpressionOfMemberAccessExpression
                Throw New NotImplementedException()
            End Function

            Public Function GetIdentifierOfGenericName(node As SyntaxNode) As SyntaxToken Implements ISyntaxFactsService.GetIdentifierOfGenericName
                Throw New NotImplementedException()
            End Function

            Public Function GetMemberBodySpanForSpeculativeBinding(node As SyntaxNode) As TextSpan Implements ISyntaxFactsService.GetMemberBodySpanForSpeculativeBinding
                Dim trivia = node.GetAncestor(Of DocumentationCommentTriviaSyntax)
                Assert.Null(trivia)

                Return Nothing
            End Function

            Public Function GetMethodLevelMember(root As SyntaxNode, memberId As Integer) As SyntaxNode Implements ISyntaxFactsService.GetMethodLevelMember
                Throw New NotImplementedException()
            End Function

            Public Function GetMethodLevelMemberId(root As SyntaxNode, node As SyntaxNode) As Integer Implements ISyntaxFactsService.GetMethodLevelMemberId
                Throw New NotImplementedException()
            End Function

            Public Function GetMethodLevelMembers(root As SyntaxNode) As List(Of SyntaxNode) Implements ISyntaxFactsService.GetMethodLevelMembers
                Throw New NotImplementedException()
            End Function

            Public Function GetNameOfAttribute(node As SyntaxNode) As SyntaxNode Implements ISyntaxFactsService.GetNameOfAttribute
                Throw New NotImplementedException()
            End Function

            Public Function GetRefKindOfArgument(node As SyntaxNode) As RefKind Implements ISyntaxFactsService.GetRefKindOfArgument
                Throw New NotImplementedException()
            End Function

            Public Function GetText(kind As Integer) As String Implements ISyntaxFactsService.GetText
                Throw New NotImplementedException()
            End Function

            Public Function HasIncompleteParentMember(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.HasIncompleteParentMember
                Throw New NotImplementedException()
            End Function

            Public Function IsAnonymousFunction(n As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAnonymousFunction
                Throw New NotImplementedException()
            End Function

            Public Function IsAttribute(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttribute
                Throw New NotImplementedException()
            End Function

            Public Function IsAttributeName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttributeName
                Throw New NotImplementedException()
            End Function

            Public Function IsAttributeNamedArgumentIdentifier(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsAttributeNamedArgumentIdentifier
                Throw New NotImplementedException()
            End Function

            Public Function IsAwaitKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsAwaitKeyword
                Throw New NotImplementedException()
            End Function

            Public Function IsBaseConstructorInitializer(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsBaseConstructorInitializer
                Throw New NotImplementedException()
            End Function

            Public Function IsBindableToken(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsBindableToken
                Throw New NotImplementedException()
            End Function

            Public Function IsConditionalMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsConditionalMemberAccessExpression
                Throw New NotImplementedException()
            End Function

            Public Function IsContextualKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsContextualKeyword
                Throw New NotImplementedException()
            End Function

            Public Function IsDirective(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsDirective
                Throw New NotImplementedException()
            End Function

            Public Function IsElementAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsElementAccessExpression
                Throw New NotImplementedException()
            End Function

            Public Function IsEntirelyWithinStringOrCharOrNumericLiteral(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsEntirelyWithinStringOrCharOrNumericLiteral
                Throw New NotImplementedException()
            End Function

            Public Function IsForEachStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsForEachStatement
                Throw New NotImplementedException()
            End Function

            Public Function IsGenericName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsGenericName
                Throw New NotImplementedException()
            End Function

            Public Function IsGlobalNamespaceKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsGlobalNamespaceKeyword
                Throw New NotImplementedException()
            End Function

            Public Function IsHashToken(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsHashToken
                Throw New NotImplementedException()
            End Function

            Public Function IsIdentifier(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsIdentifier
                Throw New NotImplementedException()
            End Function

            Public Function IsIdentifierEscapeCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsIdentifierEscapeCharacter
                Throw New NotImplementedException()
            End Function

            Public Function IsIdentifierPartCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsIdentifierPartCharacter
                Throw New NotImplementedException()
            End Function

            Public Function IsIdentifierStartCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsIdentifierStartCharacter
                Throw New NotImplementedException()
            End Function

            Public Function IsInConstantContext(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInConstantContext
                Throw New NotImplementedException()
            End Function

            Public Function IsInConstructor(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInConstructor
                Throw New NotImplementedException()
            End Function

            Public Function IsIndexerMemberCRef(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsIndexerMemberCRef
                Throw New NotImplementedException()
            End Function

            Public Function IsInInactiveRegion(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsInInactiveRegion
                Throw New NotImplementedException()
            End Function

            Public Function IsInNamespaceOrTypeContext(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInNamespaceOrTypeContext
                Throw New NotImplementedException()
            End Function

            Public Function IsInNonUserCode(syntaxTree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As Boolean Implements ISyntaxFactsService.IsInNonUserCode
                Throw New NotImplementedException()
            End Function

            Public Function IsInStaticContext(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInStaticContext
                Throw New NotImplementedException()
            End Function

            Public Function IsInvocationExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsInvocationExpression
                Throw New NotImplementedException()
            End Function

            Public Function IsKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsKeyword
                Throw New NotImplementedException()
            End Function

            Public Function IsLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsLiteral
                Throw New NotImplementedException()
            End Function

            Public Function IsLockStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsLockStatement
                Throw New NotImplementedException()
            End Function

            Public Function IsMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMemberAccessExpression
                Throw New NotImplementedException()
            End Function

            Public Function IsMemberAccessExpressionName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMemberAccessExpressionName
                Throw New NotImplementedException()
            End Function

            Public Function IsMethodLevelMember(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsMethodLevelMember
                Throw New NotImplementedException()
            End Function

            Public Function IsNamedParameter(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsNamedParameter
                Throw New NotImplementedException()
            End Function

            Public Function IsObjectCreationExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsObjectCreationExpression
                Throw New NotImplementedException()
            End Function

            Public Function IsObjectCreationExpressionType(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsObjectCreationExpressionType
                Throw New NotImplementedException()
            End Function

            Public Function IsObjectInitializerNamedAssignmentIdentifier(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsObjectInitializerNamedAssignmentIdentifier
                Throw New NotImplementedException()
            End Function

            Public Function IsOperator(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsOperator
                Throw New NotImplementedException()
            End Function

            Public Function IsPointerMemberAccessExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsPointerMemberAccessExpression
                Throw New NotImplementedException()
            End Function

            Public Function IsPredefinedOperator(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsPredefinedOperator
                Throw New NotImplementedException()
            End Function

            Public Function IsPredefinedOperator(token As SyntaxToken, op As PredefinedOperator) As Boolean Implements ISyntaxFactsService.IsPredefinedOperator
                Throw New NotImplementedException()
            End Function

            Public Function IsPredefinedType(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsPredefinedType
                Throw New NotImplementedException()
            End Function

            Public Function IsPredefinedType(token As SyntaxToken, type As PredefinedType) As Boolean Implements ISyntaxFactsService.IsPredefinedType
                Throw New NotImplementedException()
            End Function

            Public Function IsPreprocessorKeyword(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsPreprocessorKeyword
                Throw New NotImplementedException()
            End Function

            Public Function IsQueryExpression(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsQueryExpression
                Throw New NotImplementedException()
            End Function

            Public Function IsRightSideOfQualifiedName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsRightSideOfQualifiedName
                Throw New NotImplementedException()
            End Function

            Public Function IsSkippedTokensTrivia(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsSkippedTokensTrivia
                Throw New NotImplementedException()
            End Function

            Public Function IsStartOfUnicodeEscapeSequence(c As Char) As Boolean Implements ISyntaxFactsService.IsStartOfUnicodeEscapeSequence
                Throw New NotImplementedException()
            End Function

            Public Function IsStringLiteral(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsStringLiteral
                Throw New NotImplementedException()
            End Function

            Public Function IsThisConstructorInitializer(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsThisConstructorInitializer
                Throw New NotImplementedException()
            End Function

            Public Function IsTopLevelNodeWithMembers(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTopLevelNodeWithMembers
                Throw New NotImplementedException()
            End Function

            Public Function IsTypeCharacter(c As Char) As Boolean Implements ISyntaxFactsService.IsTypeCharacter
                Throw New NotImplementedException()
            End Function

            Public Function IsTypeNamedDynamic(token As SyntaxToken, parent As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTypeNamedDynamic
                Throw New NotImplementedException()
            End Function

            Public Function IsTypeNamedVarInVariableOrFieldDeclaration(token As SyntaxToken, parent As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsTypeNamedVarInVariableOrFieldDeclaration
                Throw New NotImplementedException()
            End Function

            Public Function IsUnsafeContext(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUnsafeContext
                Throw New NotImplementedException()
            End Function

            Public Function IsUsingDirectiveName(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUsingDirectiveName
                Throw New NotImplementedException()
            End Function

            Public Function IsUsingStatement(node As SyntaxNode) As Boolean Implements ISyntaxFactsService.IsUsingStatement
                Throw New NotImplementedException()
            End Function

            Public Function IsValidIdentifier(identifier As String) As Boolean Implements ISyntaxFactsService.IsValidIdentifier
                Throw New NotImplementedException()
            End Function

            Public Function IsVerbatimIdentifier(identifier As String) As Boolean Implements ISyntaxFactsService.IsVerbatimIdentifier
                Throw New NotImplementedException()
            End Function

            Public Function IsVerbatimIdentifier(token As SyntaxToken) As Boolean Implements ISyntaxFactsService.IsVerbatimIdentifier
                Throw New NotImplementedException()
            End Function

            Public Function Parenthesize(expression As SyntaxNode, Optional includeElasticTrivia As Boolean = True) As SyntaxNode Implements ISyntaxFactsService.Parenthesize
                Throw New NotImplementedException()
            End Function

            Public Function ToIdentifierToken(name As String) As SyntaxToken Implements ISyntaxFactsService.ToIdentifierToken
                Throw New NotImplementedException()
            End Function

            Public Function TryGetCorrespondingOpenBrace(token As SyntaxToken, ByRef openBrace As SyntaxToken) As Boolean Implements ISyntaxFactsService.TryGetCorrespondingOpenBrace
                Throw New NotImplementedException()
            End Function

            Public Function TryGetDeclaredSymbolInfo(node As SyntaxNode, ByRef declaredSymbolInfo As DeclaredSymbolInfo) As Boolean Implements ISyntaxFactsService.TryGetDeclaredSymbolInfo
                Throw New NotImplementedException()
            End Function

            Public Function GetDisplayName(node As SyntaxNode, options As DisplayNameOptions, Optional rootNamespace As String = Nothing) As String Implements ISyntaxFactsService.GetDisplayName
                Throw New NotImplementedException()
            End Function

            Public Function TryGetExternalSourceInfo(directive As SyntaxNode, ByRef info As ExternalSourceInfo) As Boolean Implements ISyntaxFactsService.TryGetExternalSourceInfo
                Throw New NotImplementedException()
            End Function

            Public Function TryGetPredefinedOperator(token As SyntaxToken, ByRef op As PredefinedOperator) As Boolean Implements ISyntaxFactsService.TryGetPredefinedOperator
                Throw New NotImplementedException()
            End Function

            Public Function TryGetPredefinedType(token As SyntaxToken, ByRef type As PredefinedType) As Boolean Implements ISyntaxFactsService.TryGetPredefinedType
                Throw New NotImplementedException()
            End Function

            Public Function GetInactiveRegionSpanAroundPosition(tree As SyntaxTree, position As Integer, cancellationToken As CancellationToken) As TextSpan Implements ISyntaxFactsService.GetInactiveRegionSpanAroundPosition
                Throw New NotImplementedException()
            End Function

            Public Function GetNameForArgument(argument As SyntaxNode) As String Implements ISyntaxFactsService.GetNameForArgument
                Throw New NotImplementedException()
            End Function
        End Class
    End Class
End Namespace
