' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Threading
Imports System.Threading.Tasks
Imports Microsoft.CodeAnalysis.Editor.Implementation.BraceMatching

Namespace Microsoft.CodeAnalysis.Editor.VisualBasic.BraceMatching
    <ExportBraceMatcher(LanguageNames.VisualBasic)>
    Friend Class VisualBasicJsonBraceMatcher
        Implements IBraceMatcher

        Public Function FindBracesAsync(document As Document, position As Integer, Optional cancellationToken As CancellationToken = Nothing) As Task(Of BraceMatchingResult?) Implements IBraceMatcher.FindBracesAsync
            Return CommonJsonBraceMatcher.FindBracesAsync(document, position, cancellationToken)
        End Function
    End Class
End Namespace
