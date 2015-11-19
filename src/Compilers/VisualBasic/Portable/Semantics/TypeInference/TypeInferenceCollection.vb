' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.Collections
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    <Flags()>
    Friend Enum InferenceErrorReasons As Byte
        Other = &H0
        Ambiguous = &H1
        NoBest = &H2
    End Enum

    Friend Class DominantTypeData
        Public ResultType As TypeSymbol
        Public InferenceRestrictions As RequiredConversion

        Public IsStrictCandidate As Boolean ' did this candidate satisfy all hints solely through widening/identity?
        Public IsUnstrictCandidate As Boolean ' did this candidate satisfy all hints solely through narrowing/widening/identity?

        Public Sub New()
            ResultType = Nothing
            InferenceRestrictions = RequiredConversion.Any
            IsStrictCandidate = False
            IsUnstrictCandidate = False
        End Sub

    End Class


    Friend Class TypeInferenceCollection(Of TDominantTypeData As DominantTypeData)
        Private ReadOnly _dominantTypeDataList As ArrayBuilder(Of TDominantTypeData)

        Public Sub New()
            _dominantTypeDataList = New ArrayBuilder(Of TDominantTypeData)()
        End Sub

        Public Function GetTypeDataList() As ArrayBuilder(Of TDominantTypeData)
            Return _dominantTypeDataList
        End Function

        Public Enum HintSatisfaction
            ThroughIdentity
            ThroughWidening
            ThroughNarrowing
            Unsatisfied
            ' count: number of elements in this enum, used to construct an array
            Count
        End Enum


        ' This method, given a set of types and constraints, attempts to find the
        ' best type that's in the set.
        Friend Sub FindDominantType(
            resultList As ArrayBuilder(Of TDominantTypeData),
            ByRef inferenceErrorReasons As InferenceErrorReasons,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        )

            ' THE RULES FOR DOMINANT TYPE ARE THESE:
            '
            ' Say we have hints T:{A+, B+, C-, D-}. Then calculate the following table...
            ' For each hint+ we check "Hint<=Candidate", i.e. that Hint widens to Candidate
            ' For each hint- we check "Candidate<=Hint", i.e. that Candidate widens to Hint.
            '
            '                hint:A+  hint:B+  hint:C-  hint:D-
            ' candidate:A    A <= A   B <= A   A <= C   A <= D
            ' candidate:B    A <= B   B <= B   B <= C   B <= D
            ' candidate:C    A <= C   B <= C   C <= C   C <= D
            ' candidate:D    A <= D   B <= D   D <= C   D <= D
            '
            ' 1. If there is a unique "strict" candidate for which every check is ID/Widening, then return it.
            ' 2. If there are multiple "strict" candidates for which each check is ID/Widening, but these
            '    candidates have a unique widest ("dominant") type, then pick it.
            ' 3. If there are no "strict" candidates for which each check is ID/Widening, but there is
            '    one "unstrict" candidate for which each check is ID/Widening/Narrowing, then pick it.
            ' 4. Otherwise, fail.
            '
            ' The motive for rule "2" was for cases like T:{Mammal+,Animal-} which would be ambiguous
            ' otherwise, but for which C# picks Animal.
            '
            ' Rule "2" might seem recursive ("to calculate dominant type we need to calculate dominant type
            ' of a subset") but it's not really. NB. we might be tempted to just say that
            ' if candidate A satisfies hint:B+ through widening, then A is okay as a dominant type for B; and
            ' if candidate A satisfies hint:C- through widening, then A is not okay as a dominant type for C.
            ' The first is true (and would let us re-use results that we've already calculated).
            ' But the second is NOT true, because convertibility isn't symmetric.
            ' So we need to calculate this table. Actually, we can't even re-use results from above.
            ' That's because the first sweep through dominant would compare whether *expressions* could
            ' be widened to the candidate type (for cases where a hint had an expression, e.g. for array
            ' literals). The test we do for dominant type will compare candidate types only against
            ' other candidate types, and won't check expressions.
            '
            '               hint:A+  hint:B+  hint:C+
            ' candidate:A   .        .        C <= A
            ' candidate:B   .        .        C <= B
            ' candidate:C   .        .        C <= C
            '
            ' e.g. T:{Mammal+, Animal-},
            ' candidate "Mammal": ID, Widening     <-- a strict candidate
            ' candidate "Animal": Widening, ID     <-- a strict candidate; also the unique widest one
            '
            ' e.g. T:{Tiger+, Mammal-, Animal-},
            ' candidate "Tiger":  ID, Widening, Widening  <-- a strict candidate
            ' candidate "Mammal": Widening, ID, Widening  <-- a strict candidate; also the unique widest one
            ' candidate "Animal": Widening, Narrowing, ID  <-- an unstrict candidate
            ' 
            resultList.Clear()

            ' The following four variables are so that, if we find an unambiguous answer,
            ' we'll be able to return it immediately, rather than doing extra work.
            Dim oneStrictCandidate As TDominantTypeData = Nothing
            Dim oneUnstrictCandidate As TDominantTypeData = Nothing ' Note: we count strict candidates as unstrict candidates also
            Dim numberOfStrictCandidates As Integer = 0
            Dim numberOfUnstrictCandidates As Integer = 0

            Dim numberSatisfied = ArrayBuilder(Of Integer).GetInstance(HintSatisfaction.Count, fillWithValue:=0)

            ' Now check each candidate against all its hints.
            For Each candidateTypeData As TDominantTypeData In _dominantTypeDataList

                ' expression-only nodes aren't real candidates for dominant type; they're here solely as hints:
                If candidateTypeData.ResultType Is Nothing Then
                    candidateTypeData.IsStrictCandidate = False
                    candidateTypeData.IsUnstrictCandidate = False
                    Continue For
                End If

                ' set all elements of array to 0
                numberSatisfied.ZeroInit(HintSatisfaction.Count)

                For Each hintTypeData As TDominantTypeData In _dominantTypeDataList

                    Dim hintSatisfaction As HintSatisfaction = CheckHintSatisfaction(
                                                                    candidateTypeData,
                                                                    hintTypeData,
                                                                    hintTypeData.InferenceRestrictions,
                                                                    useSiteDiagnostics)

                    numberSatisfied(hintSatisfaction) += 1
                Next

                ' At this stage, for a candidate, we've gone through all the hints and made a count of how
                ' well it satisfied them. So we store that information.
                ' Incidentally, when we restart the type inference algorithm, the "candidateTypeData" object
                ' persists. That's not a problem because we're overriding its boolean fields.
                candidateTypeData.IsStrictCandidate = (numberSatisfied(HintSatisfaction.Unsatisfied) = 0 AndAlso numberSatisfied(HintSatisfaction.ThroughNarrowing) = 0)
                candidateTypeData.IsUnstrictCandidate = (numberSatisfied(HintSatisfaction.Unsatisfied) = 0)

                ' We might save the current candidate, as a shortcut: if in the end it turns out that there
                ' was only one strict candidate, or only one unstrict candidate, then we can return that immediately.
                If candidateTypeData.IsStrictCandidate Then
                    numberOfStrictCandidates += 1
                    oneStrictCandidate = candidateTypeData
                End If

                If candidateTypeData.IsUnstrictCandidate Then
                    numberOfUnstrictCandidates += 1
                    oneUnstrictCandidate = candidateTypeData
                End If
            Next

            numberSatisfied.Free()

            ' NOTE: we count strict candidates as unstrict candidates also

            ' Rule 1. "If there is a unique candidate for which every check is ID/Widening, then return it."
            If numberOfStrictCandidates = 1 Then
                resultList.Add(oneStrictCandidate)
                Return
            End If

            ' Rule 3. "If there are no candidates for which each check is ID/Widening, but there is
            ' one candidate for which each check is ID/Widening/Narrowing, then pick it."
            If numberOfStrictCandidates = 0 AndAlso numberOfUnstrictCandidates = 1 Then
                resultList.Add(oneUnstrictCandidate)
                Return
            End If

            ' Rule 4. "Otherwise, fail."
            If numberOfUnstrictCandidates = 0 Then
                Debug.Assert(numberOfStrictCandidates = 0, "code logic error: since every strict candidate is also an unstrict candidate.")
                inferenceErrorReasons = inferenceErrorReasons Or inferenceErrorReasons.NoBest
                Return
            End If

            ' If there were no strict candidates, but several unstrict ones, then we list them all and return ambiguity:
            If numberOfStrictCandidates = 0 Then
                Debug.Assert(numberOfUnstrictCandidates > 1, "code logic error: we should already have covered this case")

                For Each iCurrent In _dominantTypeDataList
                    If iCurrent.IsUnstrictCandidate Then
                        resultList.Add(iCurrent)
                    End If
                Next

                Debug.Assert(resultList.Count() = numberOfUnstrictCandidates, "code logic error: we should have >1 unstrict candidates, like we calculated earlier")
                inferenceErrorReasons = inferenceErrorReasons Or inferenceErrorReasons.Ambiguous
                Return
            End If

            ' The only possibility remaining is that there were several widening candidates.
            Debug.Assert(numberOfStrictCandidates > 1, "code logic error: we should have >1 widening candidates; all other possibilities have already been covered")

            ' Rule 2. "If there are multiple candidates for which each check is ID/Widening, but these
            ' candidates have a unique dominant type, then pick it."
            '
            ' Note that we're only now looking for a unique dominant type amongst the IsStrictCandidates;
            ' not amongst all candidates. Note also that this loop CANNOT have been already done inside the
            ' candidate/hint loop above. That's because this loop requires knowledge of the IsStrictCandidate
            ' flag for every candidate, and these flags were only complete at the end of the previous loop.
            '
            ' Note that we could have reused some of the previous calculations, e.g. if a candidate satisfied "+r"
            ' for every hint then it must also be a (possibly-joint-) widest candidate amongst just those hints that
            ' are strict candidates. But we won't bother with this optimization, since it would break the modularity
            ' of the "CheckHintSatisfaction" routine, and is a rare case anyway.

            For Each outer As TDominantTypeData In _dominantTypeDataList
                ' We're only now looking for widest candidates amongst the strict candidates;
                ' so we're not concerned about conversions to candidates that weren't strict.
                If Not outer.IsStrictCandidate Then
                    Continue For
                End If

                ' we'll assume it is a (possibly-joint-)widest candidate, and only put "false" if it turns out not to be.
                Dim isOuterAWidestCandidate As Boolean = True

                For Each inner As TDominantTypeData In _dominantTypeDataList

                    ' We're only now looking for widest candidates amongst the strict candidates;
                    ' so we're not concerned about conversions from candidates that weren't strict.
                    If Not inner.IsStrictCandidate Then
                        Continue For
                    End If

                    ' No need to check against self: this will always work!
                    If outer Is inner Then
                        Continue For
                    End If

                    ' A node was only ever a candidate if it had a type. e.g. "AddressOf" was never a candidate.
                    If outer.ResultType Is Nothing OrElse inner.ResultType Is Nothing Then
                        Debug.Assert(False, "How can a typeless hint be a candidate?")
                        Continue For
                    End If

                    ' Following is the same test as is done in CheckHintSatisfaction / TypeArgumentAnyConversion

                    ' convert TO outer FROM inner
                    Dim conversion As ConversionKind

                    Dim arrayLiteralType = TryCast(inner.ResultType, ArrayLiteralTypeSymbol)

                    If arrayLiteralType Is Nothing Then
                        conversion = Conversions.ClassifyConversion(inner.ResultType, outer.ResultType, useSiteDiagnostics).Key
                    Else
                        ' If source is an array literal then use ClassifyArrayLiteralConversion
                        Dim arrayLiteral = arrayLiteralType.ArrayLiteral
                        conversion = Conversions.ClassifyConversion(arrayLiteral, outer.ResultType, arrayLiteral.Binder, useSiteDiagnostics).Key
                        If Conversions.IsWideningConversion(conversion) AndAlso
                            IsSameTypeIgnoringCustomModifiers(arrayLiteralType, outer.ResultType) Then
                            conversion = ConversionKind.Identity
                        End If
                    End If

                    If Not Conversions.IsWideningConversion(conversion) Then
                        isOuterAWidestCandidate = False
                        Exit For
                    End If
                Next

                If isOuterAWidestCandidate Then
                    resultList.Add(outer)
                End If
            Next

            If resultList.Count > 1 Then
                ' If there are candidates that aren't array literals, remove all array literals from the list
                Dim lastNonArrayLiteral = -1
                For i As Integer = 0 To resultList.Count - 1
                    If TypeOf resultList(i).ResultType IsNot ArrayLiteralTypeSymbol Then
                        lastNonArrayLiteral += 1
                        If lastNonArrayLiteral <> i Then
                            resultList(lastNonArrayLiteral) = resultList(i)
                        End If
                    End If
                Next

                If lastNonArrayLiteral > -1 Then
                    resultList.Clip(lastNonArrayLiteral + 1)
                Else
                    ' All candidates are array literals convertible to each other,
                    ' Let's infer element type across all of them.

                    ' Trivial case - all types are the same
                    Dim inferredType As TypeSymbol = resultList(0).ResultType

                    For i As Integer = 1 To resultList.Count - 1
                        If Not resultList(i).ResultType.IsSameTypeIgnoringCustomModifiers(inferredType) Then
                            inferredType = Nothing
                            Exit For
                        End If
                    Next

                    If inferredType IsNot Nothing Then
                        resultList.Clip(1)
                    Else
                        Dim rank As Integer = DirectCast(resultList(0).ResultType, ArrayLiteralTypeSymbol).Rank

                        For i As Integer = 1 To resultList.Count - 1
                            If DirectCast(resultList(i).ResultType, ArrayLiteralTypeSymbol).Rank <> rank Then
                                rank = -1
                                Exit For
                            End If
                        Next

                        ' If rank for all array literals is the same, infer element type based on all elements from all literals
                        Debug.Assert(rank <> -1) ' Can we get different ranks at all? If we can, we should investigate if we can do something better than just give up.
                        If rank <> -1 Then
                            Dim elements As ArrayBuilder(Of BoundExpression) = ArrayBuilder(Of BoundExpression).GetInstance

                            For Each candidate In resultList
                                AppendArrayElements(DirectCast(candidate.ResultType, ArrayLiteralTypeSymbol).ArrayLiteral.Initializer, elements)
                            Next

                            Dim inferredElementType = DirectCast(resultList(0).ResultType, ArrayLiteralTypeSymbol).ArrayLiteral.
                                    Binder.InferDominantTypeOfExpressions(VisualBasicSyntaxTree.Dummy.GetRoot(Nothing), elements, New DiagnosticBag(), Nothing)

                            If inferredElementType IsNot Nothing Then
                                ' That should match an element type inferred for one of the array literals 
                                Dim match As TDominantTypeData = Nothing
                                Dim matchLiteral As BoundArrayLiteral = Nothing

                                For Each candidate In resultList
                                    Dim candidateType = DirectCast(candidate.ResultType, ArrayLiteralTypeSymbol)
                                    If candidateType.ElementType.IsSameTypeIgnoringCustomModifiers(inferredElementType) Then
                                        Dim candidateLiteral As BoundArrayLiteral = candidateType.ArrayLiteral
                                        If match Is Nothing OrElse
                                            (candidateLiteral.HasDominantType AndAlso
                                              (Not matchLiteral.HasDominantType OrElse
                                                matchLiteral.NumberOfCandidates < candidateLiteral.NumberOfCandidates)) Then

                                            match = candidate
                                            matchLiteral = candidateType.ArrayLiteral
                                        End If
                                    End If
                                Next

                                ' The inferred element type might be Object when each literal has a more specific inferred element type. 
                                ' Let's treat it as a failure to infer the dominant type.
                                If match IsNot Nothing Then
                                    resultList.Clear()
                                    resultList.Add(match)
                                End If
                            End If
                        End If
                    End If
                End If
            End If

            ' // Rule 2. "If there are multiple candidates for which each check is ID/Widening, but these
            ' // candidates have a unique dominant type, then pick it."

            If resultList.Count = 1 Then
                Return
            End If

            ' If there were multiple dominant types out of that set, then return them all and say "ambiguous"
            If resultList.Count > 1 Then
                inferenceErrorReasons = inferenceErrorReasons Or inferenceErrorReasons.Ambiguous
                Return
            End If

            ' The only other possibility is that there were multiple strict candidates (and no widest ones).
            ' So we'll return them all and say "ambiguous"

            ' Actually, I believe this case to be impossible, but I can't figure out how to prove it.
            ' So I'll leave the code in for now.
            Debug.Assert(False, "unexpected: how can there be multiple strict candidates and no widest ones??? please tell lwischik if you find such a case.")

            For Each returnAllStrictCandidatesIterCurrent In _dominantTypeDataList
                If returnAllStrictCandidatesIterCurrent.IsStrictCandidate Then
                    resultList.Add(returnAllStrictCandidatesIterCurrent)
                End If
            Next

            Debug.Assert(resultList.Count > 0, "code logic error: we already said there were multiple strict candidates")
            inferenceErrorReasons = inferenceErrorReasons Or inferenceErrorReasons.Ambiguous
        End Sub

        Private Shared Sub AppendArrayElements(source As BoundArrayInitialization, elements As ArrayBuilder(Of BoundExpression))
            For Each sourceElement In source.Initializers
                If sourceElement.Kind = BoundKind.ArrayInitialization Then
                    AppendArrayElements(DirectCast(sourceElement, BoundArrayInitialization), elements)
                Else
                    elements.Add(sourceElement)
                End If
            Next
        End Sub

        Private Function CheckHintSatisfaction(
            candidateData As DominantTypeData,
            hintData As DominantTypeData,
            hintRestrictions As RequiredConversion,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As HintSatisfaction
            ' This algorithm is used in type inference (where we examine whether a candidate works against a set of hints)
            ' It is also used in inferring a dominant type out of a collection e.g. for array literals:
            ' in that case, "candidate" is the one we're currently examining against all the others, and "hint" is

            Debug.Assert(RequiredConversion.Count = 8, "If you've updated the type argument inference restrictions, then please also update CheckHintSatisfaction()")

            Dim candidate As TypeSymbol = candidateData.ResultType
            Dim hint As TypeSymbol = hintData.ResultType

            Dim conversion As ConversionKind

            If candidate Is Nothing Then
                ' This covers the case when the "candidate" is dominant-typeless array literal or Nothing or a lambda. Obviously
                ' it's not a real candidate. So we skip over it.
                conversion = Nothing
            ElseIf hintRestrictions = RequiredConversion.None Then
                conversion = ConversionKind.Identity
            ElseIf hintRestrictions = RequiredConversion.Identity Then
                conversion = Conversions.ClassifyDirectCastConversion(hint, candidate, useSiteDiagnostics)

                If Not Conversions.IsIdentityConversion(conversion) Then
                    conversion = Nothing
                End If

            ElseIf hintRestrictions = RequiredConversion.Any Then
                ' copy TO the candidate for the parameter type T FROM the hint (i.e. argument)

                Dim arrayLiteralType = TryCast(hint, ArrayLiteralTypeSymbol)

                If arrayLiteralType Is Nothing Then
                    conversion = Conversions.ClassifyConversion(hint, candidate, useSiteDiagnostics).Key
                Else
                    ' If source is an array literal then use ClassifyArrayLiteralConversion
                    Dim arrayLiteral = arrayLiteralType.ArrayLiteral
                    conversion = Conversions.ClassifyConversion(arrayLiteral, candidate, arrayLiteral.Binder, useSiteDiagnostics).Key
                    If Conversions.IsWideningConversion(conversion) Then
                        If IsSameTypeIgnoringCustomModifiers(arrayLiteralType, candidate) Then
                            ' ClassifyConversion returns widening for identity.  For hint satisfaction it should be promoted to Identity
                            conversion = ConversionKind.Identity
                        ElseIf (conversion And ConversionKind.InvolvesNarrowingFromNumericConstant) <> 0 Then
                            ' ClassifyConversion returns Widening with InvolvesNarrowingFromNumericConstant for {1L, 2L} and {1, 2}.
                            ' For hint satisfaction it should be demoted to Narrowing. This is required for the following case, otherwise,
                            ' type of t is ambiguous.
                            '
                            '    Foo2Params({1, 2, 3}, {1L, 2L, 3L})
                            '
                            '    Function Foo2Params(Of t)(x As t, y As t) As String

                            conversion = ConversionKind.Narrowing
                        End If
                    End If
                End If

            ElseIf hintRestrictions = RequiredConversion.AnyReverse Then
                ' copyback TO the hint (i.e. argument) FROM the candidate for parameter type T
                conversion = Conversions.ClassifyConversion(candidate, hint, useSiteDiagnostics).Key

            ElseIf hintRestrictions = RequiredConversion.AnyAndReverse Then
                ' forwards copy of argument into parameter
                Dim inConversion As ConversionKind = Conversions.ClassifyConversion(hint, candidate, useSiteDiagnostics).Key

                ' back copy from the parameter back into the argument
                Dim outConversion As ConversionKind = Conversions.ClassifyConversion(candidate, hint, useSiteDiagnostics).Key

                ' Pick the lowest for our classification of identity/widening/narrowing/error.
                If Conversions.NoConversion(inConversion) OrElse Conversions.NoConversion(outConversion) Then
                    conversion = Nothing
                ElseIf Conversions.IsNarrowingConversion(inConversion) OrElse Conversions.IsNarrowingConversion(outConversion) Then
                    conversion = ConversionKind.Narrowing
                ElseIf Conversions.IsIdentityConversion(inConversion) AndAlso Conversions.IsIdentityConversion(outConversion) Then
                    conversion = ConversionKind.Identity
                Else
                    Debug.Assert(Conversions.IsWideningConversion(inConversion) AndAlso Conversions.IsWideningConversion(outConversion) AndAlso
                                 Not (Conversions.IsIdentityConversion(inConversion) AndAlso Conversions.IsIdentityConversion(outConversion)))
                    conversion = ConversionKind.Widening
                End If

            ElseIf hintRestrictions = RequiredConversion.ArrayElement Then
                conversion = Conversions.ClassifyArrayElementConversion(hint, candidate, useSiteDiagnostics)

            ElseIf hintRestrictions = RequiredConversion.Reference Then

                If hint.IsReferenceType AndAlso candidate.IsReferenceType Then
                    conversion = Conversions.ClassifyDirectCastConversion(hint, candidate, useSiteDiagnostics)



                    ' Dev10#595234: to preserve backwards-compatibility with Orcas, a narrowing
                    ' counts as type inference failure if it happens inside a generic type parameter.
                    ' e.g. type inference will never infer T:Object for parameter IComparable(Of T)
                    ' when given argument IComparable(Of String), since it String->Object is narrowing,
                    ' and hence the candidate T:Object is deemed an error candidate.
                    If Conversions.IsNarrowingConversion(conversion) Then
                        conversion = Nothing
                    End If
                ElseIf hint.IsSameTypeIgnoringCustomModifiers(candidate) Then
                    conversion = ConversionKind.Identity
                Else
                    conversion = Nothing
                End If

            ElseIf hintRestrictions = RequiredConversion.ReverseReference Then

                If hint.IsReferenceType AndAlso candidate.IsReferenceType Then
                    '  in reverse
                    conversion = Conversions.ClassifyDirectCastConversion(candidate, hint, useSiteDiagnostics)

                    ' Dev10#595234: as above, if there's narrowing inside a generic type parameter context is unforgivable.
                    If Conversions.IsNarrowingConversion(conversion) Then
                        conversion = Nothing
                    End If
                ElseIf candidate.IsSameTypeIgnoringCustomModifiers(hint) Then
                    conversion = ConversionKind.Identity
                Else
                    conversion = Nothing
                End If
            Else
                Debug.Assert(False, "code logic error: inferenceRestrictions; we should have dealt with all of them already")
                conversion = Nothing
            End If

            If Conversions.NoConversion(conversion) Then
                Return HintSatisfaction.Unsatisfied
            ElseIf Conversions.IsNarrowingConversion(conversion) Then
                Return HintSatisfaction.ThroughNarrowing
            ElseIf Conversions.IsIdentityConversion(conversion) Then
                Return HintSatisfaction.ThroughIdentity
            ElseIf Conversions.IsWideningConversion(conversion) Then
                Return HintSatisfaction.ThroughWidening
            Else
                Debug.Assert(False, "code logic error: ConversionClass; we should have dealt with them already")
                Return HintSatisfaction.Unsatisfied
            End If
        End Function

    End Class

    Friend Class TypeInferenceCollection
        Inherits TypeInferenceCollection(Of DominantTypeData)

        Public Sub AddType(
            type As TypeSymbol,
            conversion As RequiredConversion,
            sourceExpression As BoundExpression
        )
            Debug.Assert(type IsNot Nothing)

            If type.IsVoidType() Then
                Debug.Assert(Not type.IsVoidType(), "Please do not put Void types into the dominant type algorithm. That doesn't make sense.")
                Return
            End If

            ' Don't add error types to the dominant type inference collection.
            If type.IsErrorType then
                return
            End If

            ' We will add only unique types into this collection. Otherwise, say, if we added two types
            ' "Integer" and "Integer", the dominant type routine would say that the two candidates are
            ' ambiguous! This of course means we'll have to combine the restrictions of the two hints types.
            Dim foundInList As Boolean = False

            ' Do not merge array literals with other expressions
            If TypeOf type IsNot ArrayLiteralTypeSymbol Then

                For Each competitor As DominantTypeData In Me.GetTypeDataList()

                    ' Do not merge array literals with other expressions
                    If TypeOf competitor.ResultType IsNot ArrayLiteralTypeSymbol AndAlso type.IsSameTypeIgnoringCustomModifiers(competitor.ResultType) Then

                        competitor.InferenceRestrictions = Conversions.CombineConversionRequirements(
                                                            competitor.InferenceRestrictions,
                                                            conversion)

                        ' TODO: should we simply get out of the loop here? For some reason Dev10 continues, I guess it verifies uniqueness this way.
                        Debug.Assert(Not foundInList, "List is supposed to be unique: how can we already find two of the same type in this list.")
                        foundInList = True
                    End If
                Next
            End If

            If Not foundInList Then
                Dim typeData As New DominantTypeData()
                typeData.ResultType = type
                typeData.InferenceRestrictions = conversion

                Me.GetTypeDataList().Add(typeData)
            End If
        End Sub

    End Class

End Namespace

