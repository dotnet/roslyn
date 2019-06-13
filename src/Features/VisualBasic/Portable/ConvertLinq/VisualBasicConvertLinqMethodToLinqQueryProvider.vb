' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Composition
Imports Microsoft.CodeAnalysis.CodeRefactorings
Imports Microsoft.CodeAnalysis.ConvertLinq
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.ConvertLinq
    <ExportCodeRefactoringProvider(LanguageNames.VisualBasic, Name:=NameOf(VisualBasicConvertLinqMethodToLinqQueryProvider)), [Shared]>
    Partial Friend NotInheritable Class VisualBasicConvertLinqMethodToLinqQueryProvider
        Inherits AbstractConvertLinqMethodToLinqQueryProvider

        Protected Overrides Function CreateAnalyzer(semanticModel As SemanticModel, cancellationToken As CancellationToken) As IAnalyzer
            Return New VisualBasicAnalyzer(semanticModel, cancellationToken)
        End Function

        Private NotInheritable Class VisualBasicAnalyzer
            Inherits Analyzer(Of ExpressionSyntax, QueryExpressionSyntax)

            Public Sub New(semanticModel As SemanticModel, cancellationToken As CancellationToken)
                MyBase.New(semanticModel, cancellationToken)
            End Sub

            Protected Overrides ReadOnly Property Title() As String
                Get
                    Return VBFeaturesResources.Convert_to_query
                End Get
            End Property

            Protected Overrides Function TryConvert(source As ExpressionSyntax, ByRef destination As QueryExpressionSyntax) As Boolean
                Throw New NotImplementedException()
            End Function

        End Class
    End Class
End Namespace
