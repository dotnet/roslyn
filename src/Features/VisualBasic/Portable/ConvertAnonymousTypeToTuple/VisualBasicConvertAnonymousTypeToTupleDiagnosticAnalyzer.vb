' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.ConvertAnonymousTypeToTuple
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertAnonymousTypeToTuple
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend Class VisualBasicConvertAnonymousTypeToTupleDiagnosticAnalyzer
        Inherits AbstractConvertAnonymousTypeToTupleDiagnosticAnalyzer(Of
            SyntaxKind, AnonymousObjectCreationExpressionSyntax)

        Protected Overrides Function GetAnonymousObjectCreationExpressionSyntaxKind() As SyntaxKind
            Return SyntaxKind.AnonymousObjectCreationExpression
        End Function

        Protected Overrides Function GetInitializerCount(anonymousType As AnonymousObjectCreationExpressionSyntax) As Integer
            Return anonymousType.Initializer.Initializers.Count
        End Function
    End Class
End Namespace
