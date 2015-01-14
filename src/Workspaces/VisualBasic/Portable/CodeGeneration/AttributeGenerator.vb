' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.CodeGeneration.CodeGenerationHelpers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeGeneration
    Friend Module AttributeGenerator

        Public Function GenerateAttributeBlocks(attributes As ImmutableArray(Of AttributeData), options As CodeGenerationOptions, Optional target As SyntaxToken? = Nothing) As SyntaxList(Of AttributeListSyntax)
            If Not attributes.Any() Then
                Return Nothing
            End If

            Return SyntaxFactory.List(Of AttributeListSyntax)(attributes.OrderBy(Function(a) a.AttributeClass.Name).Select(Function(a) GenerateAttributeBlock(a, options, target)))
        End Function

        Private Function GenerateAttributeBlock(attribute As AttributeData, options As CodeGenerationOptions, target As SyntaxToken?) As AttributeListSyntax
            Return SyntaxFactory.AttributeList(
                SyntaxFactory.SingletonSeparatedList(GenerateAttribute(attribute, options, target)))
        End Function

        Private Function GenerateAttribute(attribute As AttributeData, options As CodeGenerationOptions, target As SyntaxToken?) As AttributeSyntax
            Dim reusableSyntax = GetReuseableSyntaxNodeForAttribute(Of AttributeSyntax)(attribute, options)
            If reusableSyntax IsNot Nothing Then
                Return reusableSyntax
            End If

            Return SyntaxFactory.Attribute(If(target.HasValue, SyntaxFactory.AttributeTarget(target.Value), Nothing),
                                           attribute.AttributeClass.GenerateTypeSyntax(),
                                           GenerateArgumentList(attribute))
        End Function

        Private Function GenerateArgumentList(attribute As AttributeData) As ArgumentListSyntax
            If attribute.ConstructorArguments.Length = 0 AndAlso attribute.NamedArguments.Length = 0 Then
                Return Nothing
            End If

            Dim arguments = New List(Of ArgumentSyntax)

            arguments.AddRange(attribute.ConstructorArguments.Select(
                Function(a) SyntaxFactory.SimpleArgument(GenerateExpression(a))))

            arguments.AddRange(attribute.NamedArguments.Select(
                Function(kvp) SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(kvp.Key.ToIdentifierName()), GenerateExpression(kvp.Value))))

            Return SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments))
        End Function
    End Module
End Namespace
