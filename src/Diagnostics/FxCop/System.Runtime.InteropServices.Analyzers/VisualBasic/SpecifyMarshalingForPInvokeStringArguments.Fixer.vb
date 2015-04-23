' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Editing
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace System.Runtime.InteropServices.Analyzers
    <ExportCodeFixProvider(LanguageNames.CSharp, Name:=PInvokeDiagnosticAnalyzer.CA2101), [Shared]>
    Public Class BasicSpecifyMarshalingForPInvokeStringArgumentsFixer
        Inherits SpecifyMarshalingForPInvokeStringArgumentsFixer

        Protected Overrides Function IsAttribute(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.Attribute)
        End Function

        Protected Overrides Function FindNamedArgument(arguments As IReadOnlyList(Of SyntaxNode), argumentName As String) As SyntaxNode
            Return Aggregate arg In arguments.OfType(Of SimpleArgumentSyntax)
                   Where arg.IsNamed
                   Into FirstOrDefault(arg.NameColonEquals.Name.Identifier.Text = argumentName)
        End Function

        Protected Overrides Function IsDeclareStatement(node As SyntaxNode) As Boolean
            Return node.IsKind(SyntaxKind.DeclareFunctionStatement) OrElse
                   node.IsKind(SyntaxKind.DeclareSubStatement)
        End Function

        Protected Overrides Async Function FixDeclareStatement(document As Document, node As SyntaxNode, cancellationToken As CancellationToken) As Task(Of Document)
            Dim editor = Await DocumentEditor.CreateAsync(document, cancellationToken).ConfigureAwait(False)
            Dim decl = CType(node, DeclareStatementSyntax)
            Dim newCharSetKeyword = SyntaxFactory.Token(SyntaxKind.UnicodeKeyword).
                    WithLeadingTrivia(decl.CharsetKeyword.LeadingTrivia).
                    WithTrailingTrivia(decl.CharsetKeyword.TrailingTrivia).
                    WithAdditionalAnnotations(Formatter.Annotation)
            Dim newDecl = decl.WithCharsetKeyword(newCharSetKeyword)

            editor.ReplaceNode(decl, newDecl)
            Return editor.GetChangedDocument()
        End Function

    End Class
End Namespace
