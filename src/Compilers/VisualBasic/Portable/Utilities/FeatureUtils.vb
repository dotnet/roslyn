' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    ''' <summary>
    ''' Utility methods to help with checking the availability of language features.
    ''' </summary>
    Friend Module FeatureUtils

        ''' <summary>
        ''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' of the parser.
        ''' </summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <returns>True if the feature's language version is compatible with the specified language version.</returns>
        <Extension>
        Private Function IsInLanguageVersion(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Dim required = feature.GetLanguageVersion()
            Dim current = options.LanguageVersion
            Return required <= current
        End Function

        <Extension>
        Private Function CheckFeatures(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Dim featureFlag = feature.GetFeatureFlag()
            Return ((featureFlag IsNot Nothing) AndAlso options.Features.ContainsKey(featureFlag))
        End Function

        ''' <summary>Report the unavailability of a language feature.</summary>
        ''' <typeparam name="TNode"></typeparam>
        ''' <param name="feature">Language feature to report as unavailable.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <param name="node">The node to attach the diagnostic (Feature Unavailable).</param>
        ''' <returns>Return the node with the diagnostic attached to it.</returns>
        <Extension>
        Public Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, options As VisualBasicParseOptions, node As TNode) As TNode
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            Dim requiredVersion As New VisualBasicRequiredLanguageVersion(feature.GetLanguageVersion())
            Return Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, options.LanguageVersion.GetErrorName(), featureName, requiredVersion)
        End Function

        ''' <summary>
        ''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' of the parser.
        ''' </summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        <Extension>
        Public Function IsAvailable(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Return feature.CheckFeatures(options) OrElse feature.IsInLanguageVersion(options)
        End Function

        ''' <summary>
        '''  Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' of the parser.  If it is not available a diagnostic will be added to the returned value.
        ''' </summary>
        ''' <param name="node">The node to attach the potential diagnostic (Feature Unavailable).</param>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        <Extension>
        Public Function CheckFeatureAvailable(Of TNode As VisualBasicSyntaxNode)(node As TNode, feature As Feature, options As VisualBasicParseOptions) As TNode
            Return If(feature.IsAvailable(options), node, feature.ReportFeatureUnavailable(options, node))
        End Function

        ''' <summary>Returns false and reports an error if the feature is unavailable.</summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <param name="diagnostics">The diagnostics to which add this diagnostic.</param>
        ''' <param name="location">The location to report the diagnostic.</param>
        <Extension>
        Public Function IsAvailable(feature As Feature, options As VisualBasicParseOptions, diagnostics As DiagnosticBag, location As Location) As Boolean
            If feature.IsAvailable(options) Then Return True
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            Dim requiredVersion As New VisualBasicRequiredLanguageVersion(feature.GetLanguageVersion())
            diagnostics.Add(ERRID.ERR_LanguageVersion, location, options.LanguageVersion.GetErrorName(), featureName, requiredVersion)
            Return False
        End Function

    End Module

End Namespace