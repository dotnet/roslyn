' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Runtime.CompilerServices

Namespace Microsoft.CodeAnalysis.VisualBasic.Syntax.InternalSyntax

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
        _FeatureWithoutLangVersion_ ' This used to unittest the feature cmd arg. (NEVER place under a language version.)
    End Enum

    Friend Module FeatureExtensions
        <Extension>
        Friend Function GetFeatureFlag(feature As Feature) As String
            Select Case feature
                Case Feature.DigitSeparators
                    Return "digitSeparators"

                Case Feature.BinaryLiterals
                    Return "binaryLiterals"
                Case Feature._FeatureWithoutLangVersion_
                    Return "_FeatureWithoutLangVersion_"

                Case Else
                    Return Nothing
            End Select
        End Function

        <Extension>
        Friend Function TryGetLanguageVersion(feature As Feature, ByRef langVersion As LanguageVersion) As Boolean
            langVersion = LanguageVersion.None
            Select Case feature
                Case Feature.AutoProperties,
                     Feature.LineContinuation,
                     Feature.StatementLambdas,
                     Feature.CoContraVariance,
                     Feature.CollectionInitializers,
                     Feature.SubLambdas,
                     Feature.ArrayLiterals
                    langVersion = LanguageVersion.VisualBasic10

                Case Feature.AsyncExpressions,
                     Feature.Iterators,
                     Feature.GlobalNamespace
                    langVersion = LanguageVersion.VisualBasic11

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
                    langVersion = LanguageVersion.VisualBasic14
            End Select
            Return langVersion <> LanguageVersion.None
        End Function

        <Extension>
        Friend Function TryGetResourceId(feature As Feature, ByRef errID As ERRID) As Boolean
            errID = 0
            Select Case feature
                Case Feature.AutoProperties
                    errID = ERRID.FEATURE_AutoProperties
                Case Feature.ReadonlyAutoProperties
                    errID = ERRID.FEATURE_ReadonlyAutoProperties
                Case Feature.LineContinuation
                    errID = ERRID.FEATURE_LineContinuation
                Case Feature.StatementLambdas
                    errID = ERRID.FEATURE_StatementLambdas
                Case Feature.CoContraVariance
                    errID = ERRID.FEATURE_CoContraVariance
                Case Feature.CollectionInitializers
                    errID = ERRID.FEATURE_CollectionInitializers
                Case Feature.SubLambdas
                    errID = ERRID.FEATURE_SubLambdas
                Case Feature.ArrayLiterals
                    errID = ERRID.FEATURE_ArrayLiterals
                Case Feature.AsyncExpressions
                    errID = ERRID.FEATURE_AsyncExpressions
                Case Feature.Iterators
                    errID = ERRID.FEATURE_Iterators
                Case Feature.GlobalNamespace
                    errID = ERRID.FEATURE_GlobalNamespace
                Case Feature.NullPropagatingOperator
                    errID = ERRID.FEATURE_NullPropagatingOperator
                Case Feature.NameOfExpressions
                    errID = ERRID.FEATURE_NameOfExpressions
                Case Feature.RegionsEverywhere
                    errID = ERRID.FEATURE_RegionsEverywhere
                Case Feature.MultilineStringLiterals
                    errID = ERRID.FEATURE_MultilineStringLiterals
                Case Feature.CObjInAttributeArguments
                    errID = ERRID.FEATURE_CObjInAttributeArguments
                Case Feature.LineContinuationComments
                    errID = ERRID.FEATURE_LineContinuationComments
                Case Feature.TypeOfIsNot
                    errID = ERRID.FEATURE_TypeOfIsNot
                Case Feature.YearFirstDateLiterals
                    errID = ERRID.FEATURE_YearFirstDateLiterals
                Case Feature.WarningDirectives
                    errID = ERRID.FEATURE_WarningDirectives
                Case Feature.PartialModules
                    errID = ERRID.FEATURE_PartialModules
                Case Feature.PartialInterfaces
                    errID = ERRID.FEATURE_PartialInterfaces
                Case Feature.ImplementingReadonlyOrWriteonlyPropertyWithReadwrite
                    errID = ERRID.FEATURE_ImplementingReadonlyOrWriteonlyPropertyWithReadwrite
                Case Feature.DigitSeparators
                    errID = ERRID.FEATURE_DigitSeparators
                Case Feature.BinaryLiterals
                    errID = ERRID.FEATURE_BinaryLiterals
                Case Feature._FeatureWithoutLangVersion_
                    errID = ERRID._FeatureWithoutLangVersion_
            End Select
            Return (errID >= ERRID.FEATURE_AutoProperties) And (errID <= ERRID._FeatureWithoutLangVersion_)
        End Function

    End Module
End Namespace