' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicConvertAnonymousTypeToTupleCodeRefactoringProvider
        Inherits AbstractConvertAnonymousTypeToTupleCodeRefactoringProvider(Of SyntaxKind)

        Protected Overrides Function GetAnonymousObjectCreationExpressionSyntaxKind() As syntaxkind
            Return SyntaxKind.AnonymousObjectCreationExpression
        End Function
    End Class
End Namespace
