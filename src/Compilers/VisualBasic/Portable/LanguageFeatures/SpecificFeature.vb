' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.LanguageFeatures

    Friend Module SpecificFeature

        ''' <summary>Inference of tuple element names was added in VB 15.3</summary>
        <Extension>
        Friend Function DisallowInferredTupleElementNames(self As LanguageVersion) As Boolean
            Return self < Feature.InferredTupleNames.GetLanguageVersion()
        End Function

        <Extension>
        Friend Function AllowNonTrailingNamedArguments(self As LanguageVersion) As Boolean
            Return self >= Feature.NonTrailingNamedArguments.GetLanguageVersion()
        End Function

        ''' <summary>After VB15.5 it is possible to use named arguments in non-trailing position, except in attribute lists (where it remains disallowed)</summary>
        Friend Function ReportNonTrailingNamedArgumentIfNeeded(argument As ArgumentSyntax, seenNames As Boolean, allowNonTrailingNamedArguments As Boolean) As ArgumentSyntax
            If Not seenNames OrElse allowNonTrailingNamedArguments Then
                Return argument
            End If

            Return Parser.ReportSyntaxError(argument, ERRID.ERR_ExpectedNamedArgument, VisualBasicRequiredLanguageVersionService.Instance.GetRequiredLanguageVersion(Feature.NonTrailingNamedArguments))
        End Function

    End Module

End Namespace
