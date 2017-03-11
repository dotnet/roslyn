' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.


Option Compare Binary
Option Strict On

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Text
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.SyntaxFacts
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Syntax.InternalSyntax
Imports CoreInternalSyntax = Microsoft.CodeAnalysis.Syntax.InternalSyntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Friend Class FeatureUtils
        Protected Friend Shared Function CheckFeatureAvailability(token As SyntaxToken, feature As Syntax.InternalSyntax.Feature, parseOptions As VisualBasicParseOptions) As SyntaxToken
            If CheckFeatureAvailability(parseOptions, feature) Then
                Return token
            End If
            Dim errorInfo = ErrorFactory.ErrorInfo(ERRID.ERR_LanguageVersion, parseOptions.LanguageVersion.GetErrorName(), ErrorFactory.ErrorInfo(feature.GetResourceId()))
            Return DirectCast(token.AddError(errorInfo), SyntaxToken)
        End Function


        Protected Friend Shared Function CheckFeatureAvailability(parseOptions As VisualBasicParseOptions, feature As Feature) As Boolean
            Dim featureFlag = feature.GetFeatureFlag()
            If featureFlag IsNot Nothing Then
                Return parseOptions.Features.ContainsKey(featureFlag)
            End If

            Dim required = feature.GetLanguageVersion()
            Dim actual = parseOptions.LanguageVersion
            Return CInt(required) <= CInt(actual)
        End Function

        ''' <summary>
        ''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' of the parser.  If it is not available a diagnostic will be added to the returned value.
        ''' </summary>
        Protected Friend Shared Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, scanner As Scanner) As TNode
            Return CheckFeatureAvailability(feature, node, scanner.Options.LanguageVersion)
        End Function

        Protected Friend Shared Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            If CheckFeatureAvailability(languageVersion, feature) Then
                Return node
            End If

            If feature = Feature.InterpolatedStrings Then
                ' Bug: It is too late in the release cycle to update localized strings.  As a short term measure we will output 
                ' an unlocalized string and fix this to be localized in the next release.
                Return Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, languageVersion.GetErrorName(), "interpolated strings")
            Else
                Return ReportFeatureUnavailable(feature, node, languageVersion)
            End If
        End Function

        Protected Friend Shared Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            Return Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, languageVersion.GetErrorName(), featureName)
        End Function


        Protected Friend Shared Function CheckFeatureAvailability(feature As Feature, scanner As Scanner) As Boolean
            Return CheckFeatureAvailability(scanner.Options.LanguageVersion, feature)
        End Function

        Protected Friend Shared Function CheckFeatureAvailability(languageVersion As LanguageVersion, feature As Feature) As Boolean
            Dim required = feature.GetLanguageVersion()
            Return required <= languageVersion
        End Function

        ''' <summary>
        ''' Returns false and reports an error if the feature is un-available
        ''' </summary>
        Protected Friend Shared Function CheckFeatureAvailability(diagnostics As DiagnosticBag, location As Location, languageVersion As LanguageVersion, feature As Feature) As Boolean
            If Not CheckFeatureAvailability(languageVersion, feature) Then
                Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
                diagnostics.Add(ERRID.ERR_LanguageVersion, location, languageVersion.GetErrorName(), featureName)
                Return False
            End If
            Return True
        End Function

    End Class

End Namespace
