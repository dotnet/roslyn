' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeCleanup.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeCleanup
    Partial Friend Class VisualBasicCodeCleanerService
        Inherits AbstractCodeCleanerService

        Private Shared ReadOnly defaultProviders As IEnumerable(Of ICodeCleanupProvider)

        Shared Sub New()
            defaultProviders = ImmutableArray.Create(Of ICodeCleanupProvider)(
                New AddMissingTokensCodeCleanupProvider(),
                New FixIncorrectTokensCodeCleanupProvider(),
                New ReduceTokensCodeCleanupProvider(),
                New NormalizeModifiersOrOperatorsCodeCleanupProvider(),
                New RemoveUnnecessaryLineContinuationCodeCleanupProvider(),
                New CaseCorrectionCodeCleanupProvider(),
                New SimplificationCodeCleanupProvider(),
                New FormatCodeCleanupProvider()
            )

            System.Diagnostics.Debug.Assert(defaultProviders.Count > 0)
        End Sub

        Public Overrides Function GetDefaultProviders() As IEnumerable(Of ICodeCleanupProvider)
            Return defaultProviders
        End Function
    End Class
End Namespace