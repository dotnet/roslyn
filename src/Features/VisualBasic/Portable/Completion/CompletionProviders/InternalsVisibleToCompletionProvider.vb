' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    Friend NotInheritable Class InternalsVisibleToCompletionProvider
        Inherits AbstractInternalsVisibleToCompletionProvider

        Protected Overrides Function GetAssemblyScopedAttributeSyntaxNodesOfDocument(documentRoot As SyntaxNode) As IImmutableList(Of SyntaxNode)
            Dim builder As ImmutableList(Of SyntaxNode).Builder = Nothing
            Dim compilationUnit = TryCast(documentRoot, CompilationUnitSyntax)
            If Not compilationUnit Is Nothing Then
                For Each attributeStatement In compilationUnit.Attributes
                    For Each attributeList In attributeStatement.AttributeLists
                        builder = If(builder, ImmutableList.CreateBuilder(Of SyntaxNode)())
                        builder.AddRange(attributeList.Attributes)
                    Next
                Next
            End If

            Return If(builder Is Nothing, ImmutableList(Of SyntaxNode).Empty, builder.ToImmutable())
        End Function

        Protected Overrides Function GetConstructorArgumentOfInternalsVisibleToAttribute(internalsVisibleToAttribute As SyntaxNode) As SyntaxNode
            Dim arguments = DirectCast(internalsVisibleToAttribute, AttributeSyntax).ArgumentList.Arguments
            ' InternalsVisibleTo has only one constructor argument. 
            ' https://msdn.microsoft.com/en-us/library/system.runtime.compilerservices.internalsvisibletoattribute.internalsvisibletoattribute(v=vs.110).aspx
            ' We can assume that this is the assemblyName argument.
            Return If(arguments.Count > 0, arguments(0).GetExpression(), Nothing)
        End Function
    End Class
End Namespace
