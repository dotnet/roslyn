' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateNamedTypeSyntax(symbol As INamedTypeSymbol, onlyNames As Boolean) As TypeSyntax
            If Not symbol.TupleElements.IsDefault Then
                Return GenerateTupleTypeSyntax(symbol, onlyNames)
            End If

            If symbol.SpecialType <> SpecialType.None Then
                Return GenerateSpecialTypeSyntax(symbol, onlyNames)
            End If

            Dim nameSyntax = If(symbol.TypeArguments.IsDefaultOrEmpty,
                DirectCast(IdentifierName(symbol.Name), SimpleNameSyntax),
                GenericName(Identifier(symbol.Name), GenerateTypeArgumentList(symbol.TypeArguments)))

            If symbol.ContainingType IsNot Nothing Then
                Dim containingType = symbol.ContainingType.GenerateNameSyntax()
                Return QualifiedName(containingType, nameSyntax)
            ElseIf symbol.ContainingNamespace IsNot Nothing Then
                'If symbol.ContainingNamespace.IsGlobalNamespace Then
                '    Return AliasQualifiedName(SyntaxFacts.GetText(SyntaxKind.GlobalKeyword), nameSyntax)
                'End If

                Dim containingNamespace = symbol.ContainingNamespace.GenerateNameSyntax()
                Return QualifiedName(containingNamespace, nameSyntax)
            End If

            Return nameSyntax
        End Function

        Private Function GenerateTupleTypeSyntax(symbol As INamedTypeSymbol, onlyNames As Boolean) As TypeSyntax
            If symbol.TupleElements.Length < 2 OrElse onlyNames Then
                Return CodeGenerator.GenerateValueTuple(
                    symbol.TupleElements, 0, symbol.TupleElements.Length).GenerateNameSyntax()
            End If

            Using temp = GetArrayBuilder(Of TupleElementSyntax)()
                Dim elements = temp.Builder

                For Each field In symbol.TupleElements
                    Dim fieldType = field.Type.GenerateTypeSyntax()
                    If String.IsNullOrEmpty(field.Name) Then
                        elements.Add(TypedTupleElement(fieldType))
                    Else
                        elements.Add(NamedTupleElement(Identifier(field.Name), SimpleAsClause(fieldType)))
                    End If
                Next

                Return TupleType(SeparatedList(elements))
            End Using
        End Function

        Private Function GenerateSpecialTypeSyntax(symbol As INamedTypeSymbol, onlyNames As Boolean) As TypeSyntax
            If onlyNames Then
                Return CodeGenerator.GenerateSystemType(symbol.SpecialType).GenerateNameSyntax()
            End If

            Select Case symbol.SpecialType
                Case SpecialType.System_Object
                    Return PredefinedType(Token(SyntaxKind.ObjectKeyword))
                Case SpecialType.System_Boolean
                    Return PredefinedType(Token(SyntaxKind.BooleanKeyword))
                Case SpecialType.System_Char
                    Return PredefinedType(Token(SyntaxKind.CharKeyword))
                Case SpecialType.System_SByte
                    Return PredefinedType(Token(SyntaxKind.SByteKeyword))
                Case SpecialType.System_Byte
                    Return PredefinedType(Token(SyntaxKind.ByteKeyword))
                Case SpecialType.System_Int16
                    Return PredefinedType(Token(SyntaxKind.ShortKeyword))
                Case SpecialType.System_UInt16
                    Return PredefinedType(Token(SyntaxKind.UShortKeyword))
                Case SpecialType.System_Int32
                    Return PredefinedType(Token(SyntaxKind.IntegerKeyword))
                Case SpecialType.System_UInt32
                    Return PredefinedType(Token(SyntaxKind.UIntegerKeyword))
                Case SpecialType.System_Int64
                    Return PredefinedType(Token(SyntaxKind.LongKeyword))
                Case SpecialType.System_UInt64
                    Return PredefinedType(Token(SyntaxKind.ULongKeyword))
                Case SpecialType.System_Decimal
                    Return PredefinedType(Token(SyntaxKind.DecimalKeyword))
                Case SpecialType.System_Single
                    Return PredefinedType(Token(SyntaxKind.SingleKeyword))
                Case SpecialType.System_Double
                    Return PredefinedType(Token(SyntaxKind.DoubleKeyword))
                Case SpecialType.System_String
                    Return PredefinedType(Token(SyntaxKind.StringKeyword))
                Case SpecialType.System_DateTime
                    Return PredefinedType(Token(SyntaxKind.DateKeyword))
            End Select

            Throw New NotImplementedException()
        End Function

        Private Function GenerateNamedTypeDeclaration(symbol As INamedTypeSymbol) As DeclarationStatementSyntax
            If symbol.TypeKind = TypeKind.Enum Then
                Return GenerateEnumDeclaration(symbol)
            ElseIf symbol.TypeKind = TypeKind.Delegate Then
                Return GenerateDelegateDeclaration(symbol)
            End If

            Dim blockKind =
                If(symbol.TypeKind = TypeKind.Structure, SyntaxKind.StructureBlock,
                If(symbol.TypeKind = TypeKind.Interface, SyntaxKind.InterfaceBlock,
                   SyntaxKind.ClassBlock))

            Dim statementKind =
                If(symbol.TypeKind = TypeKind.Structure, SyntaxKind.StructureStatement,
                If(symbol.TypeKind = TypeKind.Interface, SyntaxKind.InterfaceStatement,
                   SyntaxKind.ClassStatement))

            Dim typeKeyword = Token(
                If(symbol.TypeKind = TypeKind.Structure, SyntaxKind.StructureKeyword,
                If(symbol.TypeKind = TypeKind.Interface, SyntaxKind.InterfaceKeyword,
                   SyntaxKind.ClassKeyword)))

            Dim inheritsList =
                If(symbol.TypeKind = TypeKind.Interface, GenerateInheritsList(symbol.Interfaces),
                If(symbol.TypeKind = TypeKind.Class, GenerateInheritsList(ImmutableArray.Create(symbol.BaseType)), Nothing))

            Dim implementsList =
                If(symbol.TypeKind = TypeKind.Class OrElse symbol.TypeKind = TypeKind.Structure,
                   GenerateImplementsList(symbol.Interfaces),
                   Nothing)

            Dim endStatement =
                If(symbol.TypeKind = TypeKind.Structure, EndStructureStatement(),
                If(symbol.TypeKind = TypeKind.Interface, EndInterfaceStatement(),
                   EndClassStatement()))

            Return TypeBlock(
                blockKind,
                TypeStatement(
                    statementKind,
                    GenerateAttributeLists(symbol.GetAttributes()),
                    GenerateModifiers(isType:=True, symbol.DeclaredAccessibility, symbol.GetModifiers()),
                    typeKeyword,
                    Identifier(symbol.Name),
                    GenerateTypeParameterList(symbol.TypeArguments)),
                inheritsList,
                implementsList,
                GenerateMemberStatements(symbol.GetMembers()),
                endStatement)
        End Function

        Private Function GenerateImplementsList(interfaces As ImmutableArray(Of INamedTypeSymbol)) As SyntaxList(Of ImplementsStatementSyntax)
            Using temp = GetArrayBuilder(Of ImplementsStatementSyntax)()
                Dim builder = temp.Builder

                For Each baseType In interfaces
                    If baseType IsNot Nothing Then
                        builder.Add(ImplementsStatement(baseType.GenerateTypeSyntax()))
                    End If
                Next

                Return List(builder)
            End Using
        End Function

        Private Function GenerateInheritsList(interfaces As ImmutableArray(Of INamedTypeSymbol)) As SyntaxList(Of InheritsStatementSyntax)
            Using temp = GetArrayBuilder(Of InheritsStatementSyntax)()
                Dim builder = temp.Builder

                For Each baseType In interfaces
                    If baseType IsNot Nothing Then
                        builder.Add(InheritsStatement(baseType.GenerateTypeSyntax()))
                    End If
                Next

                Return List(builder)
            End Using
        End Function

        Private Function GenerateDelegateDeclaration(symbol As INamedTypeSymbol) As DeclarationStatementSyntax
            Throw New NotImplementedException()
        End Function
    End Module
End Namespace
