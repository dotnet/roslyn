' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports Microsoft.CodeAnalysis.Completion
Imports Microsoft.CodeAnalysis.Completion.Providers
Imports Microsoft.CodeAnalysis.Host.Mef
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Completion.Providers

    <ExportCompletionProvider(NameOf(InternalsVisibleToCompletionProvider), LanguageNames.VisualBasic)>
    <ExtensionOrder(After:=NameOf(XmlDocCommentCompletionProvider))>
    <[Shared]>
    Friend NotInheritable Class InternalsVisibleToCompletionProvider
        Inherits AbstractInternalsVisibleToCompletionProvider

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Friend Overrides ReadOnly Property Language As String
            Get
                Return LanguageNames.VisualBasic
            End Get
        End Property

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

        Protected Overrides Function ShouldTriggerAfterQuotes(text As SourceText, insertedCharacterPosition As Integer) As Boolean
            Return CompletionUtilities.IsWordStartCharacter(text(insertedCharacterPosition))
        End Function
    End Class
End Namespace
