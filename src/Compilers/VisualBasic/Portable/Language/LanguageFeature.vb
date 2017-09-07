' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Language

    Friend Enum Feature
        AutoProperties
        LineContinuation
        StatementLambdas
        CoContraVariance
        CollectionInitializers
        SubLambdas
        ArrayLiterals
        AsyncExpressions
        Iterators
        GlobalNamespace
        NullPropagatingOperator
        NameOfExpressions
        InterpolatedStrings
        ReadonlyAutoProperties
        RegionsEverywhere
        MultilineStringLiterals
        CObjInAttributeArguments
        LineContinuationComments
        TypeOfIsNot
        YearFirstDateLiterals
        WarningDirectives
        PartialModules
        PartialInterfaces
        ImplementingReadonlyOrWriteonlyPropertyWithReadwrite
        DigitSeparators
        BinaryLiterals
        Tuples
        IOperation
        InferredTupleNames
    End Enum

    <HideModuleName>
    Friend Module FeatureExtensions

        <Extension>
        Private Function RequiresExplicitFeatureFlag(feature As Feature) As Boolean
            Return feature = Feature.IOperation
        End Function

        <Extension>
        Friend Function GetFeatureFlag(feature As Feature) As String
            Select Case feature
                Case Feature.IOperation
                    Return "IOperation"

                Case Else
                    Return Nothing
            End Select
        End Function

        <Extension>
        Friend Function GetLanguageVersion(feature As Feature) As LanguageVersion

            Select Case feature
                Case Feature.AutoProperties,
                     Feature.LineContinuation,
                     Feature.StatementLambdas,
                     Feature.CoContraVariance,
                     Feature.CollectionInitializers,
                     Feature.SubLambdas,
                     Feature.ArrayLiterals
                    Return LanguageVersion.VisualBasic10

                Case Feature.AsyncExpressions,
                     Feature.Iterators,
                     Feature.GlobalNamespace
                    Return LanguageVersion.VisualBasic11

                Case Feature.NullPropagatingOperator,
                     Feature.NameOfExpressions,
                     Feature.InterpolatedStrings,
                     Feature.ReadonlyAutoProperties,
                     Feature.RegionsEverywhere,
                     Feature.MultilineStringLiterals,
                     Feature.CObjInAttributeArguments,
                     Feature.LineContinuationComments,
                     Feature.TypeOfIsNot,
                     Feature.YearFirstDateLiterals,
                     Feature.WarningDirectives,
                     Feature.PartialModules,
                     Feature.PartialInterfaces,
                     Feature.ImplementingReadonlyOrWriteonlyPropertyWithReadwrite
                    Return LanguageVersion.VisualBasic14

                Case Feature.Tuples,
                    Feature.BinaryLiterals,
                    Feature.DigitSeparators
                    Return LanguageVersion.VisualBasic15

                Case Feature.InferredTupleNames
                    Return LanguageVersion.VisualBasic15_3
                Case Else
                    ' Return a "dummy" version for any prototype feature.
                    If feature.RequiresExplicitFeatureFlag Then Return CType(_RequireExplictFeatureFlag, LanguageVersion)
                    ' Otherwise throw the Unexpected Value.
                    Throw ExceptionUtilities.UnexpectedValue(feature)
            End Select

        End Function

        <Extension>
        Friend Function GetResourceId(feature As Feature) As ERRID
            Select Case feature
                Case Feature.AutoProperties
                    Return ERRID.FEATURE_AutoProperties
                Case Feature.ReadonlyAutoProperties
                    Return ERRID.FEATURE_ReadonlyAutoProperties
                Case Feature.LineContinuation
                    Return ERRID.FEATURE_LineContinuation
                Case Feature.StatementLambdas
                    Return ERRID.FEATURE_StatementLambdas
                Case Feature.CoContraVariance
                    Return ERRID.FEATURE_CoContraVariance
                Case Feature.CollectionInitializers
                    Return ERRID.FEATURE_CollectionInitializers
                Case Feature.SubLambdas
                    Return ERRID.FEATURE_SubLambdas
                Case Feature.ArrayLiterals
                    Return ERRID.FEATURE_ArrayLiterals
                Case Feature.AsyncExpressions
                    Return ERRID.FEATURE_AsyncExpressions
                Case Feature.Iterators
                    Return ERRID.FEATURE_Iterators
                Case Feature.GlobalNamespace
                    Return ERRID.FEATURE_GlobalNamespace
                Case Feature.NullPropagatingOperator
                    Return ERRID.FEATURE_NullPropagatingOperator
                Case Feature.NameOfExpressions
                    Return ERRID.FEATURE_NameOfExpressions
                Case Feature.RegionsEverywhere
                    Return ERRID.FEATURE_RegionsEverywhere
                Case Feature.MultilineStringLiterals
                    Return ERRID.FEATURE_MultilineStringLiterals
                Case Feature.CObjInAttributeArguments
                    Return ERRID.FEATURE_CObjInAttributeArguments
                Case Feature.LineContinuationComments
                    Return ERRID.FEATURE_LineContinuationComments
                Case Feature.TypeOfIsNot
                    Return ERRID.FEATURE_TypeOfIsNot
                Case Feature.YearFirstDateLiterals
                    Return ERRID.FEATURE_YearFirstDateLiterals
                Case Feature.WarningDirectives
                    Return ERRID.FEATURE_WarningDirectives
                Case Feature.PartialModules
                    Return ERRID.FEATURE_PartialModules
                Case Feature.PartialInterfaces
                    Return ERRID.FEATURE_PartialInterfaces
                Case Feature.ImplementingReadonlyOrWriteonlyPropertyWithReadwrite
                    Return ERRID.FEATURE_ImplementingReadonlyOrWriteonlyPropertyWithReadwrite
                Case Feature.DigitSeparators
                    Return ERRID.FEATURE_DigitSeparators
                Case Feature.BinaryLiterals
                    Return ERRID.FEATURE_BinaryLiterals
                Case Feature.Tuples
                    Return ERRID.FEATURE_Tuples
                Case Feature.IOperation
                    Return ERRID.FEATURE_IOperation
                Case Feature.InterpolatedStrings
                    Return ERRID.FEATURE_InterpolatedStrings
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(feature)
            End Select
        End Function

#Region "Feature Checking Extensions"

        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' specified in the <paramref name="options"/> (<see cref="VisualBasicParseOptions"/>).
        ''' </summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <returns>True if the feature's language version is compatible with the specified language version.</returns>
        <Extension>
        Private Function IsInLanguageVersion(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Dim required = feature.GetLanguageVersion()
            Dim current = options.LanguageVersion
            Return CheckVersionNumbers(required, current)
        End Function

        <Extension>
        Private Function IsInLanguageVersion(feature As Feature, options As VisualBasicCompilation) As Boolean
            Dim required = feature.GetLanguageVersion()
            Dim current = options.LanguageVersion
            Return CheckVersionNumbers(required, current)
        End Function

        Private Const _RequireExplictFeatureFlag As Integer = Integer.MinValue

        Private Function CheckVersionNumbers(required As LanguageVersion, current As LanguageVersion) As Boolean
            ' This check ensures that any prototype feature, requires an explicit feature flag to be present.
            Return (_RequireExplictFeatureFlag < required) And (required <= current)
        End Function

        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is enabled via <see cref="VisualBasicParseOptions.Features"/>.
        ''' Via a feature flag.
        ''' </summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The <see cref="VisualBasicParseOptions"/> to check.</param>
        ''' <returns></returns>
        <Extension>
        Private Function CheckFeatures(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Dim featureFlag = feature.GetFeatureFlag()
            Return If(featureFlag Is Nothing, False, options.Features.ContainsKey(featureFlag))
        End Function

        ''' <summary>Report the unavailability of a language feature.</summary>
        ''' <typeparam name="TNode">The <see cref="VisualBasicSyntaxNode"/> to use.</typeparam>
        ''' <param name="feature">Language feature to report as unavailable.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <param name="node">The node to attach the diagnostic to, when feature is unavailable.</param>
        ''' <returns>
        ''' If Feature is unavailable return the <paramref name="node"/> with the unavailable diagnostic attached to it.</returns>
        <Extension>
        Friend Function ReportFeatureUnavailable(Of TNode As Syntax.InternalSyntax.VisualBasicSyntaxNode)(node As TNode, feature As Feature, options As VisualBasicParseOptions) As TNode
            Dim f = feature.GetNameAndRequiredVersion()
            Return Syntax.InternalSyntax.Parser.ReportSyntaxError(node, ERRID.ERR_LanguageVersion, options.LanguageVersion.GetErrorName(), f.Info, f.Version)
        End Function

        Private Function ReportFeatureUnavailable(feature As Feature, options As VisualBasicParseOptions, location As Location) As Diagnostic
            Dim f = feature.GetNameAndRequiredVersion()
            Dim info = ErrorFactory.ErrorInfo(ERRID.ERR_LanguageVersion, options.LanguageVersion.GetErrorName(), f.Info, f.Version)
            Return New VBDiagnostic(info, location)
        End Function

        <Extension>
        Private Function GetNameAndRequiredVersion(feature As Feature) As (Info As DiagnosticInfo, Version As VisualBasicRequiredLanguageVersion)
            Return (ErrorFactory.ErrorInfo(feature.GetResourceId), VisualBasicRequiredLanguageVersion.For(feature))
        End Function

        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with these <paramref name="options"/>.
        ''' </summary>
        ''' <remarks>
        ''' Feature maybe enabled explicitly with a Feature Flag or enabled implicitly via a Language Version.
        ''' </remarks>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        <Extension>
        Friend Function IsAvailable(feature As Feature, options As VisualBasicParseOptions) As Boolean
            Return feature.IsInLanguageVersion(options) OrElse feature.CheckFeatures(options)
        End Function

        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with this <paramref name="compilation"/>.
        ''' </summary>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="compilation">The <see cref="VisualBasicCompilation"/> being used.</param>
        <Extension>
        Friend Function IsAvailable(feature As Feature, compilation As VisualBasicCompilation) As Boolean
            Return feature.IsInLanguageVersion(compilation)
        End Function

        ''' <summary>
        ''' Check to see if a language <paramref name="feature"/> is available with the <paramref name="options"/> being used.
        ''' If unavailable the function return the node with an unavailable diagnostic attached to <paramref name="node"/>.
        ''' </summary>
        ''' <returns>If <see cref="feature"/> is not available, returns node with unavailable diagnostic attached to it.</returns>
        ''' <param name="node">The node to attach the potential diagnostic (Feature Unavailable).</param>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        <Extension>
        Friend Function CheckFeatureAvailability(Of TNode As Syntax.InternalSyntax.VisualBasicSyntaxNode)(node As TNode, feature As Feature, options As VisualBasicParseOptions) As TNode
            Return If(feature.IsAvailable(options), node, node.ReportFeatureUnavailable(feature, options))
        End Function

        ''' <summary>
        ''' Is a language <paramref name="feature"/> unavailable to use with these <paramref name="options"/>?
        ''' </summary>
        ''' <returns>
        ''' Should a feature be unavailable, a feature unavailable <see cref="Diagnostic"/> at <paramref name="location"/> is return.
        ''' Otherwise nothing is return.
        ''' </returns>
        ''' <param name="feature">Language feature to check is available.</param>
        ''' <param name="options">The parse options being used.</param>
        ''' <param name="location">The location to report the diagnostic.</param>
        <Extension>
        Friend Function CheckFeatureAvailability(feature As Feature, options As VisualBasicParseOptions, location As Location) As Diagnostic
            Return If(feature.IsAvailable(options), Nothing, ReportFeatureUnavailable(feature, options, location))
        End Function

#End Region
    End Module
End Namespace
