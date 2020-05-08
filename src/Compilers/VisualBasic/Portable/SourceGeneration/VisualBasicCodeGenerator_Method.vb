' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.SourceGeneration
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFactory

Namespace Microsoft.CodeAnalysis.VisualBasic.SourceGeneration
    Partial Friend Module VisualBasicCodeGenerator
        Private Function GenerateMethodDeclaration(symbol As IMethodSymbol) As DeclarationStatementSyntax
            Select Case symbol.MethodKind
                Case MethodKind.Constructor
                    Return GenerateConstructor(symbol)
                Case MethodKind.Conversion
                    Return GenerateConversion(symbol)
                Case MethodKind.Destructor
                    Return GenerateDestructor(symbol)
                Case MethodKind.UserDefinedOperator
                    Return GenerateOperator(symbol)
                Case MethodKind.Ordinary
                    Return GenerateOrdinaryMethod(symbol)
            End Select

            Throw New NotImplementedException()
        End Function

        Private Function GenerateConstructor(symbol As IMethodSymbol) As DeclarationStatementSyntax
            Throw New NotImplementedException()
        End Function

        Private Function GenerateConversion(symbol As IMethodSymbol) As DeclarationStatementSyntax
            Throw New NotImplementedException()
        End Function

        Private Function GenerateDestructor(symbol As IMethodSymbol) As DeclarationStatementSyntax
            Throw New NotImplementedException()
        End Function

        Private Function GenerateOperator(symbol As IMethodSymbol) As DeclarationStatementSyntax
            Throw New NotImplementedException()
        End Function

        Private Function GenerateOrdinaryMethod(symbol As IMethodSymbol) As DeclarationStatementSyntax
            Dim isSub = symbol.ReturnType.SpecialType = SpecialType.System_Void
            Return MethodStatement(
                If(isSub, SyntaxKind.SubStatement, SyntaxKind.FunctionStatement),
                GenerateAttributeLists(symbol.GetAttributes()),
                GenerateModifiers(isType:=False, symbol.DeclaredAccessibility, symbol.GetModifiers()),
                Token(If(isSub, SyntaxKind.SubKeyword, SyntaxKind.FunctionKeyword)),
                Identifier(symbol.Name),
                GenerateTypeParameterList(symbol.TypeArguments),
                GenerateParameterList(symbol.Parameters),
                If(isSub, Nothing, SimpleAsClause(symbol.ReturnType.GenerateTypeSyntax())),
                handlesClause:=Nothing,
                GenerateImplementsClause(symbol.ExplicitInterfaceImplementations))
        End Function
    End Module
End Namespace
