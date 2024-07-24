' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.LanguageService
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.LanguageService
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    <ExportLanguageService(GetType(SyntaxGeneratorInternal), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicSyntaxGeneratorInternal
        Inherits SyntaxGeneratorInternal

        Public Shared ReadOnly Instance As New VisualBasicSyntaxGeneratorInternal()

        Public Shared ReadOnly s_fieldModifiers As DeclarationModifiers = DeclarationModifiers.Const Or DeclarationModifiers.[New] Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.Static Or DeclarationModifiers.WithEvents
        Public Shared ReadOnly s_methodModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.Async Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.Partial Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Public Shared ReadOnly s_constructorModifiers As DeclarationModifiers = DeclarationModifiers.Static
        Public Shared ReadOnly s_propertyModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.WriteOnly Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Public Shared ReadOnly s_indexerModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.ReadOnly Or DeclarationModifiers.WriteOnly Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static Or DeclarationModifiers.Virtual
        Public Shared ReadOnly s_classModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Partial Or DeclarationModifiers.Sealed Or DeclarationModifiers.Static
        Public Shared ReadOnly s_structModifiers As DeclarationModifiers = DeclarationModifiers.[New] Or DeclarationModifiers.Partial
        Public Shared ReadOnly s_interfaceModifiers As DeclarationModifiers = DeclarationModifiers.[New] Or DeclarationModifiers.Partial
        Public Shared ReadOnly s_accessorModifiers As DeclarationModifiers = DeclarationModifiers.Abstract Or DeclarationModifiers.[New] Or DeclarationModifiers.Override Or DeclarationModifiers.Virtual

        <ImportingConstructor>
        <SuppressMessage("RoslynDiagnosticsReliability", "RS0033:Importing constructor should be [Obsolete]", Justification:="Incorrectly used in production code: https://github.com/dotnet/roslyn/issues/42839")>
        Public Sub New()
        End Sub

        Public Overrides ReadOnly Property SyntaxFacts As ISyntaxFacts = VisualBasicSyntaxFacts.Instance

        Public Overrides Function EndOfLine(text As String) As SyntaxTrivia
            Return SyntaxFactory.EndOfLine(text)
        End Function

        Public Overloads Overrides Function LocalDeclarationStatement(type As SyntaxNode, identifier As SyntaxToken, Optional initializer As SyntaxNode = Nothing, Optional isConst As Boolean = False) As SyntaxNode
            Return SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.TokenList(SyntaxFactory.Token(If(isConst, SyntaxKind.ConstKeyword, SyntaxKind.DimKeyword))),
                SyntaxFactory.SingletonSeparatedList(VariableDeclarator(type, SyntaxFactory.ModifiedIdentifier(identifier), initializer)))
        End Function

        Public Overrides Function WithInitializer(variableDeclarator As SyntaxNode, initializer As SyntaxNode) As SyntaxNode
            Return DirectCast(variableDeclarator, VariableDeclaratorSyntax).WithInitializer(DirectCast(initializer, EqualsValueSyntax))
        End Function

        Public Overrides Function EqualsValueClause(operatorToken As SyntaxToken, value As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.EqualsValue(operatorToken, DirectCast(value, ExpressionSyntax))
        End Function

        Friend Shared Function VariableDeclarator(type As SyntaxNode, name As ModifiedIdentifierSyntax, Optional expression As SyntaxNode = Nothing) As VariableDeclaratorSyntax
            Return SyntaxFactory.VariableDeclarator(
                SyntaxFactory.SingletonSeparatedList(name),
                If(type Is Nothing, Nothing, SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax))),
                If(expression Is Nothing,
                   Nothing,
                   SyntaxFactory.EqualsValue(DirectCast(expression, ExpressionSyntax))))
        End Function

        Public Overrides Function Identifier(text As String) As SyntaxToken
            Return SyntaxFactory.Identifier(text)
        End Function

        Public Overrides Function ConditionalAccessExpression(expression As SyntaxNode, whenNotNull As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.ConditionalAccessExpression(
                DirectCast(expression, ExpressionSyntax),
                DirectCast(whenNotNull, ExpressionSyntax))
        End Function

        Public Overrides Function MemberBindingExpression(name As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.SimpleMemberAccessExpression(DirectCast(name, SimpleNameSyntax))
        End Function

        Public Overrides Function RefExpression(expression As SyntaxNode) As SyntaxNode
            Return expression
        End Function

        Public Overrides Function AddParentheses(expression As SyntaxNode, Optional includeElasticTrivia As Boolean = True, Optional addSimplifierAnnotation As Boolean = True) As SyntaxNode
            Return Parenthesize(expression, addSimplifierAnnotation)
        End Function

        Friend Shared Function Parenthesize(expression As SyntaxNode, Optional addSimplifierAnnotation As Boolean = True) As ParenthesizedExpressionSyntax
            Return DirectCast(expression, ExpressionSyntax).Parenthesize(addSimplifierAnnotation)
        End Function

        Public Overrides Function YieldReturnStatement(expression As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.YieldStatement(DirectCast(expression, ExpressionSyntax))
        End Function

        Public Overrides Function RequiresLocalDeclarationType() As Boolean
            ' VB supports `dim x = ...` as well as `dim x as Y = ...`.  The local declaration type
            ' is not required.
            Return False
        End Function

        Public Overrides Function InterpolatedStringExpression(startToken As SyntaxToken, content As IEnumerable(Of SyntaxNode), endToken As SyntaxToken) As SyntaxNode
            Return SyntaxFactory.InterpolatedStringExpression(
                startToken, SyntaxFactory.List(content.Cast(Of InterpolatedStringContentSyntax)), endToken)
        End Function

        Public Overrides Function InterpolatedStringText(textToken As SyntaxToken) As SyntaxNode
            Return SyntaxFactory.InterpolatedStringText(textToken)
        End Function

        Public Overrides Function InterpolatedStringTextToken(content As String, value As String) As SyntaxToken
            Return SyntaxFactory.InterpolatedStringTextToken(content, value)
        End Function

        Public Overrides Function Interpolation(syntaxNode As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.Interpolation(DirectCast(syntaxNode, ExpressionSyntax))
        End Function

        Public Overrides Function InterpolationAlignmentClause(alignment As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.InterpolationAlignmentClause(
                SyntaxFactory.Token(SyntaxKind.CommaToken),
                DirectCast(alignment, ExpressionSyntax))
        End Function

        Public Overrides Function InterpolationFormatClause(format As String) As SyntaxNode
            Return SyntaxFactory.InterpolationFormatClause(
                SyntaxFactory.Token(SyntaxKind.ColonToken),
                SyntaxFactory.InterpolatedStringTextToken(format, format))
        End Function

        Public Overrides Function TypeParameterList(typeParameterNames As IEnumerable(Of String)) As SyntaxNode
            Return SyntaxFactory.TypeParameterList(
                SyntaxFactory.SeparatedList(Of TypeParameterSyntax)(
                    typeParameterNames.Select(Function(n) SyntaxFactory.TypeParameter(n))))
        End Function

        Public Overrides Function Type(typeSymbol As ITypeSymbol, typeContext As Boolean) As SyntaxNode
            Return If(typeContext, typeSymbol.GenerateTypeSyntax(), typeSymbol.GenerateExpressionSyntax())
        End Function

        Public Overrides Function NegateEquality(generator As SyntaxGenerator, node As SyntaxNode, left As SyntaxNode, negatedKind As Operations.BinaryOperatorKind, right As SyntaxNode) As SyntaxNode
            Select Case negatedKind
                Case BinaryOperatorKind.Equals
                    Return If(node.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression),
                        generator.ValueEqualsExpression(left, right),
                        generator.ReferenceEqualsExpression(left, right))
                Case BinaryOperatorKind.NotEquals
                    Return If(node.IsKind(SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression),
                        generator.ValueNotEqualsExpression(left, right),
                        generator.ReferenceNotEqualsExpression(left, right))
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(negatedKind)
            End Select
        End Function

        Public Overrides Function IsNotTypeExpression(expression As SyntaxNode, type As SyntaxNode) As SyntaxNode
            Return SyntaxFactory.TypeOfIsNotExpression(DirectCast(expression, ExpressionSyntax), DirectCast(type, TypeSyntax))
        End Function

        Public Function CustomEventDeclarationWithRaise(
            name As String,
            type As SyntaxNode,
            Optional accessibility As Accessibility = Accessibility.NotApplicable,
            Optional modifiers As DeclarationModifiers = Nothing,
            Optional parameters As IEnumerable(Of SyntaxNode) = Nothing,
            Optional addAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing,
            Optional removeAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing,
            Optional raiseAccessorStatements As IEnumerable(Of SyntaxNode) = Nothing) As SyntaxNode

            Dim accessors = New List(Of AccessorBlockSyntax)()

            If modifiers.IsAbstract Then
                addAccessorStatements = Nothing
                removeAccessorStatements = Nothing
                raiseAccessorStatements = Nothing
            Else
                If addAccessorStatements Is Nothing Then
                    addAccessorStatements = SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
                End If

                If removeAccessorStatements Is Nothing Then
                    removeAccessorStatements = SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
                End If

                If raiseAccessorStatements Is Nothing Then
                    raiseAccessorStatements = SpecializedCollections.EmptyEnumerable(Of SyntaxNode)()
                End If
            End If

            accessors.Add(CreateAddHandlerAccessorBlock(type, addAccessorStatements))
            accessors.Add(CreateRemoveHandlerAccessorBlock(type, removeAccessorStatements))
            accessors.Add(CreateRaiseEventAccessorBlock(parameters, raiseAccessorStatements))

            Dim evStatement = SyntaxFactory.EventStatement(
                attributeLists:=Nothing,
                modifiers:=GetModifierList(accessibility, modifiers And GetAllowedModifiers(SyntaxKind.EventStatement), declaration:=Nothing, DeclarationKind.Event),
                customKeyword:=SyntaxFactory.Token(SyntaxKind.CustomKeyword),
                eventKeyword:=SyntaxFactory.Token(SyntaxKind.EventKeyword),
                identifier:=name.ToIdentifierToken(),
                parameterList:=Nothing,
                asClause:=SyntaxFactory.SimpleAsClause(DirectCast(type, TypeSyntax)),
                implementsClause:=Nothing)

            Return SyntaxFactory.EventBlock(
                eventStatement:=evStatement,
                accessors:=SyntaxFactory.List(accessors),
                endEventStatement:=SyntaxFactory.EndEventStatement())
        End Function

        Friend Shared Function CreateAddHandlerAccessorBlock(delegateType As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(delegateType, TypeSyntax))

            Dim valueParameter = SyntaxFactory.Parameter(
                attributeLists:=Nothing,
                modifiers:=Nothing,
                identifier:=SyntaxFactory.ModifiedIdentifier("value"),
                asClause:=asClause,
                [default]:=Nothing)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.AddHandlerAccessorBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.AddHandlerAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    accessorKeyword:=SyntaxFactory.Token(SyntaxKind.AddHandlerKeyword),
                    parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParameter))),
                GetStatementList(statements),
                SyntaxFactory.EndAddHandlerStatement())
        End Function

        Friend Shared Function CreateRemoveHandlerAccessorBlock(delegateType As SyntaxNode, statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Dim asClause = SyntaxFactory.SimpleAsClause(DirectCast(delegateType, TypeSyntax))

            Dim valueParameter = SyntaxFactory.Parameter(
                attributeLists:=Nothing,
                modifiers:=Nothing,
                identifier:=SyntaxFactory.ModifiedIdentifier("value"),
                asClause:=asClause,
                [default]:=Nothing)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.RemoveHandlerAccessorBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.RemoveHandlerAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    accessorKeyword:=SyntaxFactory.Token(SyntaxKind.RemoveHandlerKeyword),
                    parameterList:=SyntaxFactory.ParameterList(SyntaxFactory.SingletonSeparatedList(valueParameter))),
                GetStatementList(statements),
                SyntaxFactory.EndRemoveHandlerStatement())
        End Function

        Friend Function CreateRaiseEventAccessorBlock(parameters As IEnumerable(Of SyntaxNode), statements As IEnumerable(Of SyntaxNode)) As AccessorBlockSyntax
            Dim parameterList = GetParameterList(parameters)

            Return SyntaxFactory.AccessorBlock(
                SyntaxKind.RaiseEventAccessorBlock,
                SyntaxFactory.AccessorStatement(
                    kind:=SyntaxKind.RaiseEventAccessorStatement,
                    attributeLists:=Nothing,
                    modifiers:=Nothing,
                    accessorKeyword:=SyntaxFactory.Token(SyntaxKind.RaiseEventKeyword),
                    parameterList:=parameterList),
                GetStatementList(statements),
                SyntaxFactory.EndRaiseEventStatement())
        End Function

        Friend Shared Function GetStatementList(nodes As IEnumerable(Of SyntaxNode)) As SyntaxList(Of StatementSyntax)
            If nodes Is Nothing Then
                Return Nothing
            Else
                Return SyntaxFactory.List(nodes.Select(AddressOf AsStatement))
            End If
        End Function

        Friend Shared Function AsStatement(node As SyntaxNode) As StatementSyntax
            Dim expr = TryCast(node, ExpressionSyntax)
            If expr IsNot Nothing Then
                Return SyntaxFactory.ExpressionStatement(expr)
            Else
                Return DirectCast(node, StatementSyntax)
            End If
        End Function

        Friend Shared Function GetAllowedModifiers(kind As SyntaxKind) As DeclarationModifiers
            Select Case kind
                Case SyntaxKind.ClassBlock, SyntaxKind.ClassStatement
                    Return s_classModifiers

                Case SyntaxKind.EnumBlock, SyntaxKind.EnumStatement
                    Return DeclarationModifiers.[New]

                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Return DeclarationModifiers.[New]

                Case SyntaxKind.InterfaceBlock, SyntaxKind.InterfaceStatement
                    Return s_interfaceModifiers

                Case SyntaxKind.StructureBlock, SyntaxKind.StructureStatement
                    Return s_structModifiers

                Case SyntaxKind.FunctionBlock,
                     SyntaxKind.FunctionStatement,
                     SyntaxKind.SubBlock,
                     SyntaxKind.SubStatement,
                     SyntaxKind.OperatorBlock,
                     SyntaxKind.OperatorStatement
                    Return s_methodModifiers

                Case SyntaxKind.ConstructorBlock,
                     SyntaxKind.SubNewStatement
                    Return s_constructorModifiers

                Case SyntaxKind.FieldDeclaration
                    Return s_fieldModifiers

                Case SyntaxKind.PropertyBlock,
                     SyntaxKind.PropertyStatement
                    Return s_propertyModifiers

                Case SyntaxKind.EventBlock,
                     SyntaxKind.EventStatement
                    Return s_propertyModifiers

                Case SyntaxKind.GetAccessorBlock,
                     SyntaxKind.GetAccessorStatement,
                     SyntaxKind.SetAccessorBlock,
                     SyntaxKind.SetAccessorStatement,
                     SyntaxKind.AddHandlerAccessorBlock,
                     SyntaxKind.AddHandlerAccessorStatement,
                     SyntaxKind.RemoveHandlerAccessorBlock,
                     SyntaxKind.RemoveHandlerAccessorStatement,
                     SyntaxKind.RaiseEventAccessorBlock,
                     SyntaxKind.RaiseEventAccessorStatement
                    Return s_accessorModifiers

                Case SyntaxKind.EnumMemberDeclaration
                Case SyntaxKind.Parameter
                Case SyntaxKind.LocalDeclarationStatement
                Case Else
                    Return DeclarationModifiers.None
            End Select
        End Function

        Friend Shared Function GetParameterList(parameters As IEnumerable(Of SyntaxNode)) As ParameterListSyntax
            Return If(parameters IsNot Nothing, SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList(parameters.Cast(Of ParameterSyntax)())), SyntaxFactory.ParameterList())
        End Function

        Friend Shared Function GetModifierList(accessibility As Accessibility, modifiers As DeclarationModifiers, declaration As SyntaxNode, kind As DeclarationKind, Optional isDefault As Boolean = False) As SyntaxTokenList
            Dim _list = SyntaxFactory.TokenList()

            ' While partial must always be last in C#, its preferred position in VB is to be first,
            ' even before accessibility modifiers. This order is enforced by line commit.
            If modifiers.IsPartial Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            End If

            If isDefault Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.DefaultKeyword))
            End If

            Select Case (accessibility)
                Case Accessibility.Internal
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword))
                Case Accessibility.Public
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.PublicKeyword))
                Case Accessibility.Private
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
                Case Accessibility.Protected
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                Case Accessibility.ProtectedOrInternal
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.FriendKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                Case Accessibility.ProtectedAndInternal
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)).Add(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword))
                Case Accessibility.NotApplicable
                Case Else
                    Throw New NotSupportedException(String.Format("Accessibility '{0}' not supported.", accessibility))
            End Select

            Dim isClass = kind = DeclarationKind.Class OrElse declaration.IsKind(SyntaxKind.ClassStatement)
            If modifiers.IsAbstract Then
                If isClass Then
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.MustInheritKeyword))
                Else
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.MustOverrideKeyword))
                End If
            End If

            If modifiers.IsNew Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ShadowsKeyword))
            End If

            If modifiers.IsSealed Then
                If isClass Then
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.NotInheritableKeyword))
                Else
                    _list = _list.Add(SyntaxFactory.Token(SyntaxKind.NotOverridableKeyword))
                End If
            End If

            If modifiers.IsOverride Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.OverridesKeyword))
            End If

            If modifiers.IsVirtual Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.OverridableKeyword))
            End If

            If modifiers.IsStatic Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.SharedKeyword))
            End If

            If modifiers.IsAsync Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.AsyncKeyword))
            End If

            If modifiers.IsConst Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ConstKeyword))
            End If

            If modifiers.IsReadOnly Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
            End If

            If modifiers.IsWriteOnly Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.WriteOnlyKeyword))
            End If

            If modifiers.IsUnsafe Then
                Throw New NotSupportedException("Unsupported modifier")
                ''''_list = _list.Add(SyntaxFactory.Token(SyntaxKind.UnsafeKeyword))
            End If

            If modifiers.IsWithEvents Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.WithEventsKeyword))
            End If

            If (kind = DeclarationKind.Field AndAlso _list.Count = 0) Then
                _list = _list.Add(SyntaxFactory.Token(SyntaxKind.DimKeyword))
            End If

            Return _list
        End Function
#Region "Patterns"

        Public Overrides Function SupportsPatterns(options As ParseOptions) As Boolean
            Return False
        End Function

        Public Overrides Function IsPatternExpression(expression As SyntaxNode, isToken As SyntaxToken, pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function AndPattern(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function ConstantPattern(expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function DeclarationPattern(type As INamedTypeSymbol, name As String) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function LessThanRelationalPattern(expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function LessThanEqualsRelationalPattern(expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GreaterThanRelationalPattern(expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function GreaterThanEqualsRelationalPattern(expression As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function NotPattern(pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function OrPattern(left As SyntaxNode, right As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function ParenthesizedPattern(pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function TypePattern(type As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

        Public Overrides Function UnaryPattern(operatorToken As SyntaxToken, pattern As SyntaxNode) As SyntaxNode
            Throw New NotImplementedException()
        End Function

#End Region
    End Class
End Namespace
