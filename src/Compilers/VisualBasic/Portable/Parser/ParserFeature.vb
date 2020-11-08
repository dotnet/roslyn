' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

    Partial Friend Class Parser

        ''' <summary>
        ''' Check to see if the given <paramref name="feature"/> is available with the <see cref="LanguageVersion"/>
        ''' of the parser.  If it is not available a diagnostic will be added to the returned value.
        ''' </summary>
        Private Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode) As TNode
            Return CheckFeatureAvailability(feature, node, _scanner.Options.LanguageVersion)
        End Function

        Friend Shared Function CheckFeatureAvailability(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            If CheckFeatureAvailability(languageVersion, feature) Then
                Return node
            End If

            Return ReportFeatureUnavailable(feature, node, languageVersion)
        End Function

        Private Shared Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode, languageVersion As LanguageVersion) As TNode
            Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
            Dim requiredVersion = New VisualBasicRequiredLanguageVersion(feature.GetLanguageVersion())
            Return ReportSyntaxError(node, ERRID.ERR_LanguageVersion, languageVersion.GetErrorName(), featureName, requiredVersion)
        End Function

        Friend Function ReportFeatureUnavailable(Of TNode As VisualBasicSyntaxNode)(feature As Feature, node As TNode) As TNode
            Return ReportFeatureUnavailable(feature, node, _scanner.Options.LanguageVersion)
        End Function

        Friend Function CheckFeatureAvailability(feature As Feature) As Boolean
            Return CheckFeatureAvailability(_scanner.Options.LanguageVersion, feature)
        End Function

        Friend Shared Function CheckFeatureAvailability(languageVersion As LanguageVersion, feature As Feature) As Boolean
            Dim required = feature.GetLanguageVersion()
            Return required <= languageVersion
        End Function

        ''' <summary>
        ''' Returns false and reports an error if the feature is un-available
        ''' </summary>
        Friend Shared Function CheckFeatureAvailability(diagnostics As DiagnosticBag, location As Location, languageVersion As LanguageVersion, feature As Feature) As Boolean
            If Not CheckFeatureAvailability(languageVersion, feature) Then
                Dim featureName = ErrorFactory.ErrorInfo(feature.GetResourceId())
                Dim requiredVersion = New VisualBasicRequiredLanguageVersion(feature.GetLanguageVersion())
                diagnostics.Add(ERRID.ERR_LanguageVersion, location, languageVersion.GetErrorName(), featureName, requiredVersion)
                Return False
            End If
            Return True
        End Function

        'TODO - These should be removed.  Checks should be in binding.
        <Flags>
        Friend Enum ParameterSpecifiers
            [ByRef] = &H1
            [ByVal] = &H2
            [Optional] = &H4
            [ParamArray] = &H8
        End Enum

    End Class

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
        InferredTupleNames
        LeadingDigitSeparator
        NonTrailingNamedArguments
        PrivateProtected
        UnconstrainedTypeParameterInConditional
        CommentsAfterLineContinuation
    End Enum

    Friend Module FeatureExtensions
        <Extension>
        Friend Function GetFeatureFlag(feature As Feature) As String
            Select Case feature
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

                Case Feature.LeadingDigitSeparator,
                    Feature.NonTrailingNamedArguments,
                    Feature.PrivateProtected
                    Return LanguageVersion.VisualBasic15_5

                Case Feature.UnconstrainedTypeParameterInConditional,
                    Feature.CommentsAfterLineContinuation
                    Return LanguageVersion.VisualBasic16

                Case Else
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
                Case Feature.LeadingDigitSeparator
                    Return ERRID.FEATURE_LeadingDigitSeparator
                Case Feature.PrivateProtected
                    Return ERRID.FEATURE_PrivateProtected
                Case Feature.InterpolatedStrings
                    Return ERRID.FEATURE_InterpolatedStrings
                Case Feature.UnconstrainedTypeParameterInConditional
                    Return ERRID.FEATURE_UnconstrainedTypeParameterInConditional
                Case Feature.CommentsAfterLineContinuation
                    Return ERRID.FEATURE_CommentsAfterLineContinuation
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(feature)
            End Select
        End Function
    End Module
End Namespace
