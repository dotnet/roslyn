' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics.CodeAnalysis
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Roslyn.Diagnostics.Analyzers

Namespace Roslyn.Diagnostics.VisualBasic.Analyzers
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class VisualBasicDoNotCopyValue
        Inherits AbstractDoNotCopyValue

        Protected Overrides Function CreateWalker(context As OperationBlockAnalysisContext, cache As NonCopyableTypesCache) As NonCopyableWalker
            Return New VisualBasicNonCopyableWalker(context, cache)
        End Function

        Private NotInheritable Class VisualBasicNonCopyableWalker
            Inherits NonCopyableWalker

            Public Sub New(context As OperationBlockAnalysisContext, cache As NonCopyableTypesCache)
                MyBase.New(context, cache)
            End Sub

            Protected Overrides Function CheckForEachGetEnumerator(operation As IForEachLoopOperation, <DisallowNull> ByRef conversion As IConversionOperation, <DisallowNull> ByRef instance As IOperation) As Boolean
                ' Not supported (yet)
                Return False
            End Function
        End Class
    End Class
End Namespace
