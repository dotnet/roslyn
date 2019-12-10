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
        Private Sub Add(md As MultiDictionary(Of Feature, LanguageVersion), keys As Feature(), value As LanguageVersion)
            Debug.Assert(md IsNot Nothing, $"{NameOf(md)} is null.")
            Debug.Assert(keys IsNot Nothing, $"{NameOf(keys)} is null.")
            Debug.Assert(keys.Length > 0, $"{NameOf(keys)} is empty.")
            For Each key In keys
                md.Add(key, value)
            Next
        End Sub

        Private ReadOnly s_FeatureLanguageVersion As New MultiDictionary(Of Feature, LanguageVersion)() From
            {
               {{Feature.AutoProperties,
                 Feature.LineContinuation,
                 Feature.StatementLambdas,
                 Feature.CoContraVariance,
                 Feature.CollectionInitializers,
                 Feature.SubLambdas,
                 Feature.ArrayLiterals}, LanguageVersion.VisualBasic10},
               {{Feature.AsyncExpressions,
                 Feature.Iterators,
                 Feature.GlobalNamespace}, LanguageVersion.VisualBasic11},
               {{Feature.NullPropagatingOperator,
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
                 Feature.ImplementingReadonlyOrWriteonlyPropertyWithReadwrite}, LanguageVersion.VisualBasic14},
               {{Feature.Tuples,
                 Feature.BinaryLiterals,
                 Feature.DigitSeparators}, LanguageVersion.VisualBasic15},
               {{Feature.InferredTupleNames}, LanguageVersion.VisualBasic15_3},
               {{Feature.LeadingDigitSeparator,
                 Feature.NonTrailingNamedArguments,
                 Feature.PrivateProtected}, LanguageVersion.VisualBasic15_5},
               {{Feature.UnconstrainedTypeParameterInConditional,
                 Feature.CommentsAfterLineContinuation}, LanguageVersion.VisualBasic16}
        }

        <Extension>
        Friend Function GetLanguageVersion(feature As Feature) As LanguageVersion
            Dim f = s_FeatureLanguageVersion.Item(feature)
            If Not f.IsEmpty Then
                Return f.FirstOrDefault()
            End If
            Throw ExceptionUtilities.UnexpectedValue(feature)
        End Function

        Private ReadOnly s_FeatureResourceID As New Dictionary(Of Feature, ERRID)() From
        {
                {Feature.AutoProperties, ERRID.FEATURE_AutoProperties},
                {Feature.ReadonlyAutoProperties, ERRID.FEATURE_ReadonlyAutoProperties},
                {Feature.LineContinuation, ERRID.FEATURE_LineContinuation},
                {Feature.StatementLambdas, ERRID.FEATURE_StatementLambdas},
                {Feature.CoContraVariance, ERRID.FEATURE_CoContraVariance},
                {Feature.CollectionInitializers, ERRID.FEATURE_CollectionInitializers},
                {Feature.SubLambdas, ERRID.FEATURE_SubLambdas},
                {Feature.ArrayLiterals, ERRID.FEATURE_ArrayLiterals},
                {Feature.AsyncExpressions, ERRID.FEATURE_AsyncExpressions},
                {Feature.Iterators, ERRID.FEATURE_Iterators},
                {Feature.GlobalNamespace, ERRID.FEATURE_GlobalNamespace},
                {Feature.NullPropagatingOperator, ERRID.FEATURE_NullPropagatingOperator},
                {Feature.NameOfExpressions, ERRID.FEATURE_NameOfExpressions},
                {Feature.RegionsEverywhere, ERRID.FEATURE_RegionsEverywhere},
                {Feature.MultilineStringLiterals, ERRID.FEATURE_MultilineStringLiterals},
                {Feature.CObjInAttributeArguments, ERRID.FEATURE_CObjInAttributeArguments},
                {Feature.LineContinuationComments, ERRID.FEATURE_LineContinuationComments},
                {Feature.TypeOfIsNot, ERRID.FEATURE_TypeOfIsNot},
                {Feature.YearFirstDateLiterals, ERRID.FEATURE_YearFirstDateLiterals},
                {Feature.WarningDirectives, ERRID.FEATURE_WarningDirectives},
                {Feature.PartialModules, ERRID.FEATURE_PartialModules},
                {Feature.PartialInterfaces, ERRID.FEATURE_PartialInterfaces},
                {Feature.ImplementingReadonlyOrWriteonlyPropertyWithReadwrite, ERRID.FEATURE_ImplementingReadonlyOrWriteonlyPropertyWithReadwrite},
                {Feature.DigitSeparators, ERRID.FEATURE_DigitSeparators},
                {Feature.BinaryLiterals, ERRID.FEATURE_BinaryLiterals},
                {Feature.Tuples, ERRID.FEATURE_Tuples},
                {Feature.LeadingDigitSeparator, ERRID.FEATURE_LeadingDigitSeparator},
                {Feature.PrivateProtected, ERRID.FEATURE_PrivateProtected},
                {Feature.InterpolatedStrings, ERRID.FEATURE_InterpolatedStrings},
                {Feature.UnconstrainedTypeParameterInConditional, ERRID.FEATURE_UnconstrainedTypeParameterInConditional},
                {Feature.CommentsAfterLineContinuation, ERRID.FEATURE_CommentsAfterLineContinuation}
        }

        <Extension>
        Friend Function GetResourceId(feature As Feature) As ERRID
            Dim id As ERRID = Nothing
            IF s_FeatureResourceID.TryGetValue(feature, id) Then
                Return id
            End If
            Throw ExceptionUtilities.UnexpectedValue(feature)
        End Function
    End Module
End Namespace
