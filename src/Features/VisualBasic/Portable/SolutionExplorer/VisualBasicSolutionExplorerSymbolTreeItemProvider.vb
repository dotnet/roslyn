' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Composition
Imports System.Text
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.FindSymbols
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SolutionExplorer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.SolutionExplorer
    <ExportLanguageService(GetType(ISolutionExplorerSymbolTreeItemProvider), LanguageNames.VisualBasic), [Shared]>
    Friend NotInheritable Class VisualBasicSolutionExplorerSymbolTreeItemProvider
        Inherits AbstractSolutionExplorerSymbolTreeItemProvider(Of
            CompilationUnitSyntax,
            StatementSyntax,
            NamespaceBlockSyntax,
            EnumBlockSyntax,
            TypeBlockSyntax)

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Protected Overrides Function GetMembers(root As CompilationUnitSyntax) As SyntaxList(Of StatementSyntax)
            Return root.Members
        End Function

        Protected Overrides Function GetMembers(baseNamespace As NamespaceBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return baseNamespace.Members
        End Function

        Protected Overrides Function GetMembers(typeDeclaration As TypeBlockSyntax) As SyntaxList(Of StatementSyntax)
            Return typeDeclaration.Members
        End Function

        Protected Overrides Function TryAddType(documentId As DocumentId, member As StatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder) As Boolean
            Dim typeBlock = TryCast(member, TypeBlockSyntax)
            If typeBlock IsNot Nothing Then
                AddTypeBlock(documentId, typeBlock, items, nameBuilder)
                Return True
            End If

            Dim enumBlock = TryCast(member, EnumBlockSyntax)
            If enumBlock IsNot Nothing Then
                AddEnumBlock(documentId, enumBlock, items)
                Return True
            End If

            Dim delegateStatement = TryCast(member, DelegateStatementSyntax)
            If delegateStatement IsNot Nothing Then
                AddDelegateStatement(documentId, delegateStatement, items, nameBuilder)
                Return True
            End If

            Return False
        End Function

        Private Shared Sub AddTypeBlock(documentId As DocumentId, typeBlock As TypeBlockSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            Dim blockStatement As TypeStatementSyntax = typeBlock.BlockStatement

            nameBuilder.Append(blockStatement.Identifier.ValueText)
            AppendTypeParameterList(nameBuilder, blockStatement.TypeParameterList)

            Dim kind = GetDeclaredSymbolInfoKind(typeBlock)
            Dim accessibility = GetAccessibility(typeBlock.Parent, blockStatement, blockStatement.Modifiers)

            items.Add(New SymbolTreeItemData(
                documentId,
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(kind, accessibility),
                hasItems:=typeBlock.Members.Count > 0,
                typeBlock,
                blockStatement.Identifier))
        End Sub

        Private Shared Sub AddEnumBlock(documentId As DocumentId, enumBlock As EnumBlockSyntax, items As ArrayBuilder(Of SymbolTreeItemData))
            Dim accessibility = GetAccessibility(enumBlock.Parent, enumBlock.EnumStatement, enumBlock.EnumStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                documentId,
                enumBlock.EnumStatement.Identifier.ValueText,
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Enum, accessibility),
                hasItems:=enumBlock.Members.Count > 0,
                enumBlock,
                enumBlock.EnumStatement.Identifier))
        End Sub

        Private Shared Sub AddDelegateStatement(documentId As DocumentId, delegateStatement As DelegateStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            nameBuilder.Append(delegateStatement.Identifier.ValueText)
            AppendTypeParameterList(nameBuilder, delegateStatement.TypeParameterList)
            AppendParameterList(nameBuilder, delegateStatement.ParameterList)
            AppendAsClause(nameBuilder, delegateStatement.AsClause)

            Dim accessibility = GetAccessibility(delegateStatement.Parent, delegateStatement, delegateStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                documentId,
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Delegate, accessibility),
                hasItems:=False,
                delegateStatement,
                delegateStatement.Identifier))
        End Sub

        Protected Overrides Sub AddEnumDeclarationMembers(documentId As DocumentId, enumDeclaration As EnumBlockSyntax, items As ArrayBuilder(Of SymbolTreeItemData), cancellationToken As CancellationToken)
            For Each member In enumDeclaration.Members
                Dim enumMember = TryCast(member, EnumMemberDeclarationSyntax)
                items.Add(New SymbolTreeItemData(
                    documentId,
                    enumMember.Identifier.ValueText,
                    Glyph.EnumMemberPublic,
                    hasItems:=False,
                    enumMember,
                    enumMember.Identifier))
            Next
        End Sub

        Protected Overrides Sub AddMemberDeclaration(documentId As DocumentId, member As StatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            Dim container = member.Parent
            Dim methodStatement = If(TryCast(member, MethodStatementSyntax), TryCast(member, MethodBlockSyntax)?.SubOrFunctionStatement)
            If methodStatement IsNot Nothing Then
                AddMethodStatement(documentId, container, methodStatement, items, nameBuilder)
                Return
            End If

            Dim constructorStatement = If(TryCast(member, SubNewStatementSyntax), TryCast(member, ConstructorBlockSyntax)?.SubNewStatement)
            If constructorStatement IsNot Nothing Then
                AddConstructorStatement(documentId, container, constructorStatement, items, nameBuilder)
                Return
            End If

            Dim operatorStatement = If(TryCast(member, OperatorStatementSyntax), TryCast(member, OperatorBlockSyntax)?.OperatorStatement)
            If operatorStatement IsNot Nothing Then
                AddOperatorStatement(documentId, container, operatorStatement, items, nameBuilder)
                Return
            End If

            Dim propertystatement = If(TryCast(member, PropertyStatementSyntax), TryCast(member, PropertyBlockSyntax)?.PropertyStatement)
            If propertystatement IsNot Nothing Then
                AddPropertyStatement(documentId, container, propertystatement, items, nameBuilder)
                Return
            End If

            Dim eventStatement = If(TryCast(member, EventStatementSyntax), TryCast(member, EventBlockSyntax)?.EventStatement)
            If eventStatement IsNot Nothing Then
                AddEventStatement(documentId, container, eventStatement, items, nameBuilder)
                Return
            End If

            Dim fieldDeclaration = TryCast(member, FieldDeclarationSyntax)
            If fieldDeclaration IsNot Nothing Then
                AddFieldDeclaration(documentId, container, fieldDeclaration, items, nameBuilder)
                Return
            End If
        End Sub

        Private Shared Sub AddMethodStatement(documentId As DocumentId, container As SyntaxNode, methodStatement As MethodStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            nameBuilder.Append(methodStatement.Identifier.ValueText)
            AppendTypeParameterList(nameBuilder, methodStatement.TypeParameterList)
            AppendParameterList(nameBuilder, methodStatement.ParameterList)
            AppendAsClause(nameBuilder, methodStatement.AsClause)

            Dim isExtension = container.IsKind(SyntaxKind.ModuleBlock) AndAlso methodStatement.AttributeLists.Any(
                Function(list) list.Attributes.Any(
                    Function(attribute)
                        Dim name = attribute.Name.GetRightmostName().Identifier.ValueText
                        Return name = "Extension" OrElse name = "ExtensionAttribute"
                    End Function))

            Dim accesibility = GetAccessibility(container, methodStatement, methodStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                documentId,
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(If(isExtension, DeclaredSymbolInfoKind.ExtensionMethod, DeclaredSymbolInfoKind.Method), accesibility),
                hasItems:=False,
                methodStatement,
                methodStatement.Identifier))
        End Sub

        Private Shared Sub AddConstructorStatement(documentId As DocumentId, container As SyntaxNode, constructorStatement As SubNewStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            nameBuilder.Append("New")
            AppendParameterList(nameBuilder, constructorStatement.ParameterList)

            Dim accesibility = GetAccessibility(container, constructorStatement, constructorStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                documentId,
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Constructor, accesibility),
                hasItems:=False,
                constructorStatement,
                constructorStatement.NewKeyword))
        End Sub

        Private Shared Sub AddOperatorStatement(documentId As DocumentId, container As SyntaxNode, operatorStatement As OperatorStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            nameBuilder.Append("Operator ")
            nameBuilder.Append(operatorStatement.OperatorToken.ToString())
            AppendParameterList(nameBuilder, operatorStatement.ParameterList)
            AppendAsClause(nameBuilder, operatorStatement.AsClause, fallbackToObject:=True)

            Dim accesibility = GetAccessibility(container, operatorStatement, operatorStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                documentId,
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Operator, accesibility),
                hasItems:=False,
                operatorStatement,
                operatorStatement.OperatorToken))
        End Sub

        Private Shared Sub AddPropertyStatement(documentId As DocumentId, container As SyntaxNode, propertystatement As PropertyStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            nameBuilder.Append(propertystatement.Identifier.ValueText)
            AppendParameterList(nameBuilder, propertystatement.ParameterList)
            AppendAsClause(nameBuilder, propertystatement.AsClause, fallbackToObject:=True)

            Dim accesibility = GetAccessibility(container, propertystatement, propertystatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                documentId,
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Property, accesibility),
                hasItems:=False,
                propertystatement,
                propertystatement.Identifier))
        End Sub

        Private Shared Sub AddEventStatement(documentId As DocumentId, container As SyntaxNode, eventStatement As EventStatementSyntax, items As ArrayBuilder(Of SymbolTreeItemData), nameBuilder As StringBuilder)
            nameBuilder.Append(eventStatement.Identifier.ValueText)
            AppendAsClause(nameBuilder, eventStatement.AsClause)

            Dim accesibility = GetAccessibility(container, eventStatement, eventStatement.Modifiers)
            items.Add(New SymbolTreeItemData(
                documentId,
                nameBuilder.ToStringAndClear(),
                GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Event, accesibility),
                hasItems:=False,
                eventStatement,
                eventStatement.Identifier))
        End Sub

        Private Shared Sub AddFieldDeclaration(
                documentId As DocumentId,
                container As SyntaxNode,
                fieldDeclaration As FieldDeclarationSyntax,
                items As ArrayBuilder(Of SymbolTreeItemData),
                nameBuilder As StringBuilder)
            For Each declarator In fieldDeclaration.Declarators
                For Each name In declarator.Names
                    nameBuilder.Append(name.Identifier.ValueText)
                    AppendAsClause(nameBuilder, declarator.AsClause, fallbackToObject:=True)

                    Dim accesibility = GetAccessibility(container, fieldDeclaration, fieldDeclaration.Modifiers)
                    items.Add(New SymbolTreeItemData(
                        documentId,
                        nameBuilder.ToStringAndClear(),
                        GlyphExtensions.GetGlyph(DeclaredSymbolInfoKind.Field, accesibility),
                        hasItems:=False,
                        fieldDeclaration,
                        name.Identifier))
                Next
            Next
        End Sub

        Private Shared Sub AppendTypeParameterList(
            builder As StringBuilder,
            typeParameterList As TypeParameterListSyntax)

            AppendCommaSeparatedList(
            builder, "(Of ", ")", typeParameterList,
            Function(list) list.Parameters,
            Sub(parameter, innherBuilder) innherBuilder.Append(parameter.Identifier.ValueText))
        End Sub

        Private Shared Sub AppendParameterList(
            builder As StringBuilder,
            parameterList As ParameterListSyntax)

            AppendCommaSeparatedList(
                builder, "(", ")", parameterList,
                Function(list) list.Parameters,
                Sub(parameter, innerBuilder) AppendType(parameter?.AsClause?.Type, builder))
        End Sub

        Private Shared Sub AppendAsClause(
                nameBuilder As StringBuilder,
                asClause As AsClauseSyntax,
                Optional fallbackToObject As Boolean = False)
            If asClause IsNot Nothing Then

                Dim simpleAsClause = TryCast(asClause, SimpleAsClauseSyntax)
                If simpleAsClause IsNot Nothing Then
                    nameBuilder.Append(" As ")
                    AppendType(simpleAsClause.Type, nameBuilder)
                    Return
                End If

                Dim asNewClause = TryCast(asClause, AsNewClauseSyntax)
                If asNewClause IsNot Nothing Then
                    Dim newObjectCreation = TryCast(asNewClause.NewExpression, ObjectCreationExpressionSyntax)
                    If newObjectCreation IsNot Nothing Then
                        nameBuilder.Append(" As ")
                        AppendType(newObjectCreation.Type, nameBuilder)
                        Return
                    End If

                    Dim newArrayCreation = TryCast(asNewClause.NewExpression, ArrayCreationExpressionSyntax)
                    If newArrayCreation IsNot Nothing Then
                        nameBuilder.Append(" As ")
                        AppendType(newArrayCreation.Type, nameBuilder)
                        nameBuilder.Append("()")
                        Return
                    End If
                End If
            ElseIf fallbackToObject Then
                nameBuilder.Append(" As Object")
            End If
        End Sub

        Private Shared Sub AppendType(typeSyntax As TypeSyntax, builder As StringBuilder)
            If typeSyntax Is Nothing Then
                Return
            End If

            Dim tupleType = TryCast(typeSyntax, TupleTypeSyntax)
            If tupleType IsNot Nothing Then
                AppendCommaSeparatedList(
                    builder, "(", ")", tupleType.Elements,
                    Sub(element, innerBuilder) AppendTupleElement(element, innerBuilder))
                Return
            End If

            Dim arrayType = TryCast(typeSyntax, ArrayTypeSyntax)
            If arrayType IsNot Nothing Then
                AppendType(arrayType.ElementType, builder)
                For Each rankSpecifier In arrayType.RankSpecifiers
                    builder.Append("("c)
                    builder.Append(","c, rankSpecifier.CommaTokens.Count)
                    builder.Append(")"c)
                Next

                Return
            End If

            Dim nullableType = TryCast(typeSyntax, NullableTypeSyntax)
            If nullableType IsNot Nothing Then
                AppendType(nullableType.ElementType, builder)
                builder.Append("?"c)
                Return
            End If

            Dim predefinedType = TryCast(typeSyntax, PredefinedTypeSyntax)
            If predefinedType IsNot Nothing Then
                builder.Append(predefinedType.ToString())
                Return
            End If

            Dim identifierName = TryCast(typeSyntax, IdentifierNameSyntax)
            If identifierName IsNot Nothing Then
                builder.Append(identifierName.Identifier.ValueText)
                Return
            End If

            Dim genericName = TryCast(typeSyntax, GenericNameSyntax)
            If genericName IsNot Nothing Then
                builder.Append(genericName.Identifier.ValueText)
                AppendCommaSeparatedList(
                    builder, "(Of ", ")", genericName.TypeArgumentList.Arguments,
                    Sub(typeArgument, innerBuilder) AppendType(typeArgument, innerBuilder))
                Return
            End If

            Dim qualifiedName = TryCast(typeSyntax, QualifiedNameSyntax)
            If qualifiedName IsNot Nothing Then
                AppendType(qualifiedName.Right, builder)
                Return
            End If

            Debug.Fail("Unhandled type: " + typeSyntax.GetType().FullName)
        End Sub

        Private Shared Sub AppendTupleElement(element As TupleElementSyntax, builder As StringBuilder)
            Dim typedTupleElement = TryCast(element, TypedTupleElementSyntax)
            If typedTupleElement IsNot Nothing Then
                AppendType(typedTupleElement.Type, builder)
                Return
            End If

            Dim namedTupleElement = TryCast(element, NamedTupleElementSyntax)
            If namedTupleElement IsNot Nothing Then
                builder.Append(namedTupleElement.Identifier.ValueText)
                AppendAsClause(builder, namedTupleElement.AsClause)
                Return
            End If

            Debug.Fail("Unhandled type: " + element.GetType().FullName)

        End Sub
    End Class
End Namespace
