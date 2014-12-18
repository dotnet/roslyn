' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeGeneration
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Globalization
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Interoperability
Imports Microsoft.CodeAnalysis.FxCopAnalyzers.Utilities
Imports Microsoft.CodeAnalysis.LanguageServices
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.FxCopAnalyzers.Globalization
    <ExportCodeFixProvider(PInvokeDiagnosticAnalyzer.CA2101, LanguageNames.VisualBasic), [Shared]>
    Public Class CA2101BasicCodeFixProvider
        Inherits CA2101CodeFixProviderBase

        Friend Overrides Function GetUpdatedDocumentAsync(document As Document, model As SemanticModel, root As SyntaxNode, nodeToFix As SyntaxNode, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            cancellationToken.ThrowIfCancellationRequested()
            Dim charSetType = WellKnownTypes.CharSet(model.Compilation)
            Dim dllImportType = WellKnownTypes.DllImportAttribute(model.Compilation)
            Dim marshalAsType = WellKnownTypes.MarshalAsAttribute(model.Compilation)
            Dim unmanagedType = WellKnownTypes.UnmanagedType(model.Compilation)
            If charSetType Is Nothing OrElse dllImportType Is Nothing OrElse marshalAsType Is Nothing OrElse unmanagedType Is Nothing Then
                Return Task.FromResult(document)
            End If

            Dim syntaxFactoryService = document.Project.LanguageServices.GetService(Of SyntaxGenerator)()

            ' return the unchanged root if no fix is available
            Dim newRoot = root
            Select Case nodeToFix.VBKind()
                Case SyntaxKind.Attribute
                    ' could be either a <DllImport> Or <MarshalAs> attribute
                    Dim attribute = CType(nodeToFix, AttributeSyntax)
                    Dim attributeType = model.GetSymbolInfo(attribute).Symbol
                    Dim arguments = attribute.ArgumentList.Arguments
                    If dllImportType.Equals(attributeType.ContainingType) Then
                        ' <DllImport> attribute, add Or replace CharSet named parameter
                        Dim argumentValue = CreateCharSetArgument(syntaxFactoryService, charSetType).WithAdditionalAnnotations(Formatter.Annotation)
                        Dim namedParameter = Aggregate arg In arguments.OfType(Of SimpleArgumentSyntax)
                                             Where arg.IsNamed
                                             Into FirstOrDefault(arg.NameColonEquals.Name.Identifier.Text = CharSetText)

                        If namedParameter Is Nothing Then
                            ' add the parameter
                            namedParameter = SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(SyntaxFactory.Identifier(CharSetText))), CType(argumentValue, ExpressionSyntax)).
                            WithAdditionalAnnotations(Formatter.Annotation)
                            Dim newArguments = arguments.Add(namedParameter)
                            Dim newArgumentList = attribute.ArgumentList.WithArguments(newArguments)
                            newRoot = root.ReplaceNode(attribute.ArgumentList, newArgumentList)
                        Else
                            ' replace the parameter
                            Dim newNamedParameter = namedParameter.WithExpression(CType(argumentValue, ExpressionSyntax))
                            newRoot = root.ReplaceNode(namedParameter, newNamedParameter)
                        End If
                    ElseIf marshalAsType.Equals(attributeType.ContainingType) AndAlso arguments.Count = 1 Then
                        ' <MarshalAs> attribute, replace the only argument
                        Dim argument = CType(arguments(0), SimpleArgumentSyntax)
                        Dim newExpression = CreateMarshalAsArgument(syntaxFactoryService, unmanagedType).
                        WithLeadingTrivia(argument.GetLeadingTrivia()).
                        WithTrailingTrivia(argument.GetTrailingTrivia())
                        Dim newArgument = argument.WithExpression(CType(newExpression, ExpressionSyntax))
                        newRoot = root.ReplaceNode(argument, newArgument)
                    End If
                Case SyntaxKind.DeclareFunctionStatement, SyntaxKind.DeclareSubStatement
                    Dim decl = CType(nodeToFix, DeclareStatementSyntax)
                    Dim newCharSetKeyword = SyntaxFactory.Token(SyntaxKind.UnicodeKeyword).
                    WithLeadingTrivia(decl.CharsetKeyword.LeadingTrivia).
                    WithTrailingTrivia(decl.CharsetKeyword.TrailingTrivia).
                    WithAdditionalAnnotations(Formatter.Annotation)
                    Dim newDecl = decl.WithCharsetKeyword(newCharSetKeyword)
                    newRoot = root.ReplaceNode(decl, newDecl)
            End Select

            Return Task.FromResult(document.WithSyntaxRoot(newRoot))
        End Function
    End Class
End Namespace