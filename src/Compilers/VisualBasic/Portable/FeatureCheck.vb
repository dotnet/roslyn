' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

'-----------------------------------------------------------------------------
' Contains Function to check the availability of Features.
'-----------------------------------------------------------------------------

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Class FeatureCheck

        Friend Shared Function CheckFeatureAvailability(token As SyntaxToken, feature As Feature, parseOptions As VisualBasicParseOptions) As SyntaxToken
            If CheckFeatureAvailability(parseOptions, feature) Then Return token
            Dim errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_LanguageVersion, parseOptions.LanguageVersion.GetErrorName(), ErrorFactory.ErrorInfo(feature.GetResourceId()))
            Return DirectCast(token.AddError(errorInfo), SyntaxToken)
        End Function

        ''' <summary>
        ''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' of the parser.  If it is not available a diagnostic will be added to the returned value.
        ''' </summary>
        Friend Shared Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, Options As VisualBasicParseOptions) As TNode
            Return CheckFeatureAvailability(feature, node, Options.LanguageVersion)
        End Function

        Friend Shared Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            If CheckFeatureAvailability(languageVersion, feature) Then Return node
            If feature <> Feature.InterpolatedStrings Then Return ReportFeatureUnavailable(feature, node, languageVersion)
            ' Bug: It is too late in the release cycle to update localized strings.  As a short term measure we will output 
            ' an unlocalized string and fix this to be localized in the next release.
            Return Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, languageVersion.GetErrorName(), "interpolated strings")
        End Function

        Private Shared Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            Return Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, languageVersion.GetErrorName(), featureName)
        End Function

        Friend Shared Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, Options As VisualBasicParseOptions) As TNode
            Return ReportFeatureUnavailable(feature, node, Options.LanguageVersion)
        End Function

        Friend Shared Function CheckFeatureAvailability(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Return CheckFeatureAvailability(options.LanguageVersion, feature)
        End Function

        Friend Shared Function CheckFeatureAvailability(languageVersion As LanguageVersion, feature As Feature) As Boolean
            Dim required = feature.GetLanguageVersion()
            Return CInt(required) <= CInt(languageVersion)
        End Function

        Friend Shared Sub CheckFeatureAvailability(diagnostics As DiagnosticBag, location As Location, languageVersion As LanguageVersion, feature As Feature)
            If CheckFeatureAvailability(languageVersion, feature) Then Exit Sub
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            diagnostics.Add(ERRID.ERR_LanguageVersion, location, languageVersion.GetErrorName(), featureName)
        End Sub

        Friend Shared Function CheckFeatureAvailability(opts As VisualBasicParseOptions, feature As Feature) As Boolean
            Dim featureFlag = feature.GetFeatureFlag()
            If featureFlag IsNot Nothing Then Return opts.Features.ContainsKey(featureFlag)
            Return CheckFeatureAvailability(opts.LanguageVersion, feature)
        End Function

    End Class

End Namespace