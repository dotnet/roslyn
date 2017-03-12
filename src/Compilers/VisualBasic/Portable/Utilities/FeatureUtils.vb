' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Option Compare Binary
Option Strict On

Imports Microsoft.CodeAnalysis.VisualBasic

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Class FeatureUtils

        Private Shared Function CheckLanguageVersion(languageVersion As LanguageVersion, feature As Feature) As Boolean
            Dim required = feature.GetLanguageVersion()
            Return required <= languageVersion
        End Function

        Protected Friend Shared Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            Return Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, languageVersion.GetErrorName(), featureName)
        End Function

        '''' <summary>
        '''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        '''' of the parser.
        '''' </summary>
        Protected Friend Shared Function CheckFeatureAvailability(parseOptions As VisualBasicParseOptions, feature As Feature) As Boolean
            Dim featureFlag = feature.GetFeatureFlag()
            If featureFlag IsNot Nothing Then Return parseOptions.Features.ContainsKey(featureFlag)
            Return CheckLanguageVersion(parseOptions.LanguageVersion, feature)
        End Function

        '''' <summary>
        '''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        '''' of the parser.  If it is not available a diagnostic will be added to the returned value.
        '''' </summary>
        Protected Friend Shared Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, parseoptions As VisualBasicParseOptions) As TNode
            If CheckFeatureAvailability(parseoptions, feature) Then Return node
            Return ReportFeatureUnavailable(feature, node, parseoptions.LanguageVersion)
        End Function

        ''' <summary>Returns false and reports an error if the feature is un-available</summary>
        Protected Friend Shared Function CheckFeatureAvailability(diagnostics As DiagnosticBag, location As Location, parseoptions As VisualBasicParseOptions, feature As Feature) As Boolean
            If CheckFeatureAvailability(parseoptions, feature) Then Return True
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            diagnostics.Add(ERRID.ERR_LanguageVersion, location, parseoptions.LanguageVersion.GetErrorName(), featureName)
            Return False
        End Function

    End Class

End Namespace
