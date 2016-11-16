' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.UseCollectionInitializer
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.UseCollectionInitializer
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=PredefinedCodeFixProviderNames.UseCollectionInitializer), [Shared]>
    Friend Class VisualBasicUseCollectionInitializerCodeFixProvider
        Inherits AbstractUseCollectionInitializerCodeFixProvider(Of
            SyntaxKind,
            ExpressionSyntax,
            StatementSyntax,
            ObjectCreationExpressionSyntax,
            MemberAccessExpressionSyntax,
            InvocationExpressionSyntax,
            ExpressionStatementSyntax,
            VariableDeclaratorSyntax)

        Public Sub New()
            MyBase.New(New VisualBasicUseCollectionInitializerDiagnosticAnalyzer())
        End Sub
    End Class
End Namespace