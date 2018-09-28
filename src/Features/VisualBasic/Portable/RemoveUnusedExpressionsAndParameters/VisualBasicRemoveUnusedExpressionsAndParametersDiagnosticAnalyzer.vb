' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.RemoveUnusedExpressionsAndParameters

Namespace Microsoft.CodeAnalysis.VisualBasic.RemoveUnusedExpressionsAndParameters

    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Friend NotInheritable Class VisualBasicRemoveUnusedExpressionsAndParametersDiagnosticAnalyzer
        Inherits AbstractRemoveUnusedExpressionsAndParametersDiagnosticAnalyzer
        Protected Overrides Function SupportsDiscard(tree As SyntaxTree) As Boolean
            Return False
        End Function

        Protected Overrides Function GetDefinitionLocationToFade(unusedDefinition As IOperation) As Location
            Return unusedDefinition.Syntax.GetLocation()
        End Function
    End Class
End Namespace
