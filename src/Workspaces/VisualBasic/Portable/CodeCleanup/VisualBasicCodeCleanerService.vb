' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.CodeCleanup
Imports Microsoft.CodeAnalysis.CodeCleanup.Providers

Namespace Microsoft.CodeAnalysis.VisualBasic.CodeCleanup
    Partial Friend Class VisualBasicCodeCleanerService
        Inherits AbstractCodeCleanerService

        Private Shared ReadOnly s_defaultProviders As ImmutableArray(Of ICodeCleanupProvider) = ImmutableArray.Create(Of ICodeCleanupProvider)(
                New AddMissingTokensCodeCleanupProvider(),
                New FixIncorrectTokensCodeCleanupProvider(),
                New ReduceTokensCodeCleanupProvider(),
                New NormalizeModifiersOrOperatorsCodeCleanupProvider(),
                New RemoveUnnecessaryLineContinuationCodeCleanupProvider(),
                New CaseCorrectionCodeCleanupProvider(),
                New SimplificationCodeCleanupProvider(),
                New FormatCodeCleanupProvider())

        Public Overrides Function GetDefaultProviders() As ImmutableArray(Of ICodeCleanupProvider)
            Return s_defaultProviders
        End Function
    End Class
End Namespace