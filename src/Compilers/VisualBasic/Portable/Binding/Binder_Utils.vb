' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic

    ' Utilities for decoding syntax for use in binding.
    Partial Friend Class Binder
        ''' <summary>
        ''' If the identifier has a type character, report an error on it.
        ''' </summary>
        Public Shared Sub DisallowTypeCharacter(identifier As SyntaxToken,
                                         diagBag As BindingDiagnosticBag,
                                         Optional errid As ERRID = ERRID.ERR_TypecharNotallowed)
            If (identifier.GetTypeCharacter() <> TypeCharacter.None) Then
                ReportDiagnostic(diagBag, identifier, errid)
            End If
        End Sub

        Public Shared Function DecodeVariance(varianceKeywordOpt As SyntaxToken) As VarianceKind
            Select Case varianceKeywordOpt.Kind
                Case SyntaxKind.None
                    Return VarianceKind.None
                Case SyntaxKind.InKeyword
                    Return VarianceKind.In
                Case SyntaxKind.OutKeyword
                    Return VarianceKind.Out
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(varianceKeywordOpt.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Given a list of keywords and a set of keywords kinds to search, return the first keyword
        ''' in the list, if any, that matches one of the keyword kinds.
        ''' </summary>
        Public Shared Function FindFirstKeyword(syntax As SyntaxTokenList,
                                                ParamArray keywordKinds As SyntaxKind()) As SyntaxToken
            For Each keywordSyntax In syntax
                If Array.IndexOf(keywordKinds, keywordSyntax.Kind) >= 0 Then
                    Return keywordSyntax
                End If
            Next

            Return Nothing
        End Function

        ' An array consisting of just the Friend keyword.
        Private Shared ReadOnly s_friendKeyword As SyntaxKind() = {SyntaxKind.FriendKeyword}

        ' Report an error on the first keyword to match one of the given kinds.
        Public Sub ReportModifierError(modifiers As SyntaxTokenList,
                                              errid As ERRID,
                                              diagBag As DiagnosticBag,
                                              ParamArray keywordKinds As SyntaxKind())
            Dim badKeyword = FindFirstKeyword(modifiers, keywordKinds)
            ' Special case: Report "Protected Friend" as error combination if 
            ' Protected is bad and both Protected and Friend found inside modifiers.
            If badKeyword.Kind = SyntaxKind.ProtectedKeyword Then
                Dim friendToken = FindFirstKeyword(modifiers, s_friendKeyword)
                If friendToken.Kind <> SyntaxKind.None Then
                    Dim startLoc As Integer = Math.Min(badKeyword.SpanStart, friendToken.SpanStart)
                    Dim endLoc As Integer = Math.Max(badKeyword.Span.End, friendToken.Span.End)
                    Dim location = Me.SyntaxTree.GetLocation(New TextSpan(startLoc, endLoc - startLoc))
                    ReportDiagnostic(diagBag, location, errid, badKeyword.ToString() & " " & friendToken.ToString())
                    Return
                End If
            End If

            ' Normal case.
            ReportDiagnostic(diagBag, badKeyword, errid, badKeyword.ToString())
        End Sub

        ''' <summary>
        ''' Map syntax kind of a modifier keyword to SourceMemberFlags value
        ''' </summary>
        Friend Shared Function MapKeywordToFlag(syntax As SyntaxToken) As SourceMemberFlags
            Select Case syntax.Kind
                Case SyntaxKind.PrivateKeyword : Return SourceMemberFlags.Private
                Case SyntaxKind.FriendKeyword : Return SourceMemberFlags.Friend
                Case SyntaxKind.ProtectedKeyword : Return SourceMemberFlags.Protected
                Case SyntaxKind.PublicKeyword : Return SourceMemberFlags.Public
                Case SyntaxKind.SharedKeyword : Return SourceMemberFlags.Shared
                Case SyntaxKind.ReadOnlyKeyword : Return SourceMemberFlags.ReadOnly
                Case SyntaxKind.WriteOnlyKeyword : Return SourceMemberFlags.WriteOnly
                Case SyntaxKind.OverloadsKeyword : Return SourceMemberFlags.Overloads
                Case SyntaxKind.OverridableKeyword : Return SourceMemberFlags.Overridable
                Case SyntaxKind.MustOverrideKeyword : Return SourceMemberFlags.MustOverride
                Case SyntaxKind.NotOverridableKeyword : Return SourceMemberFlags.NotOverridable
                Case SyntaxKind.OverridesKeyword : Return SourceMemberFlags.Overrides
                Case SyntaxKind.ShadowsKeyword : Return SourceMemberFlags.Shadows
                Case SyntaxKind.ConstKeyword : Return SourceMemberFlags.Const
                Case SyntaxKind.StaticKeyword : Return SourceMemberFlags.Static
                Case SyntaxKind.DefaultKeyword : Return SourceMemberFlags.Default
                Case SyntaxKind.WithEventsKeyword : Return SourceMemberFlags.WithEvents
                Case SyntaxKind.WideningKeyword : Return SourceMemberFlags.Widening
                Case SyntaxKind.NarrowingKeyword : Return SourceMemberFlags.Narrowing
                Case SyntaxKind.PartialKeyword : Return SourceMemberFlags.Partial
                Case SyntaxKind.DimKeyword : Return SourceMemberFlags.Dim
                Case SyntaxKind.MustInheritKeyword : Return SourceMemberFlags.MustInherit
                Case SyntaxKind.NotInheritableKeyword : Return SourceMemberFlags.NotInheritable
                Case SyntaxKind.AsyncKeyword : Return SourceMemberFlags.Async
                Case SyntaxKind.IteratorKeyword : Return SourceMemberFlags.Iterator

                Case Else
#If DEBUG Then
                    ' this case should only be reached for invalid code (invalid == error, not just warnings)
                    ' all possible modifiers should have been handled above.

                    Debug.Assert(syntax.ContainsDiagnostics)

                    Dim hasAtLeastOneSyntaxError As Boolean = False
                    For Each diag In syntax.Errors
                        If diag.Severity = DiagnosticSeverity.Error Then
                            hasAtLeastOneSyntaxError = True
                            Exit For
                        End If
                    Next
                    Debug.Assert(hasAtLeastOneSyntaxError)
#End If

                    Return SourceMemberFlags.None
            End Select
        End Function

        ''' <summary>
        ''' Decodes a set of modifier flags, reported any errors with the flags.
        ''' </summary>
        ''' <param name="syntax">The syntax list of the modifiers.</param>
        ''' <param name="allowableModifiers">A bit-flag of the allowable modifiers. If a bit isn't set, an error occurs.</param>
        ''' <param name="errIdBadModifier">Error ID to report if a bad modifier is found.</param>
        ''' <param name="defaultAccessibility">The default accessibility. </param>
        ''' <returns>Flags for the modifiers.</returns>
        Public Function DecodeModifiers(syntax As SyntaxTokenList,
                                        allowableModifiers As SourceMemberFlags,
                                        errIdBadModifier As ERRID,
                                        defaultAccessibility As Accessibility,
                                        diagBag As DiagnosticBag) As MemberModifiers
            Dim foundModifiers As SourceMemberFlags = Nothing
            Dim privateProtectedToken As SyntaxToken = Nothing
            Dim privateOverridableModifier As SyntaxToken = Nothing
            Dim privateMustOverrideModifier As SyntaxToken = Nothing
            Dim privateNotOverridableModifier As SyntaxToken = Nothing

            ' Go through each modifiers, accumulating flags of what we've seen and reporting errors.
            For Each keywordSyntax In syntax
                Dim currentModifier As SourceMemberFlags = MapKeywordToFlag(keywordSyntax)

                If currentModifier = SourceMemberFlags.None Then
                    Continue For
                End If

                ' Report errors with the modifier
                If (currentModifier And allowableModifiers) = 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, errIdBadModifier, keywordSyntax.ToString())
                ElseIf (currentModifier And foundModifiers) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_DuplicateSpecifier)
                ElseIf (currentModifier And SourceMemberFlags.AllAccessibilityModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.AllAccessibilityModifiers) <> 0 AndAlso
                       Not ((foundModifiers Or currentModifier) And SourceMemberFlags.AllAccessibilityModifiers) = (SourceMemberFlags.Protected Or SourceMemberFlags.Friend) AndAlso
                       Not (((foundModifiers Or currentModifier) And SourceMemberFlags.AllAccessibilityModifiers) = (SourceMemberFlags.Protected Or SourceMemberFlags.Private)) Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_DuplicateAccessCategoryUsed)
                ElseIf (currentModifier And SourceMemberFlags.AllOverrideModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.AllOverrideModifiers) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_DuplicateModifierCategoryUsed)
                ElseIf (currentModifier And SourceMemberFlags.AllWriteabilityModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.AllWriteabilityModifiers) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_DuplicateWriteabilityCategoryUsed)
                ElseIf (currentModifier And SourceMemberFlags.AllConversionModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.AllConversionModifiers) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_DuplicateConversionCategoryUsed)
                ElseIf (currentModifier And SourceMemberFlags.AllShadowingModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.AllShadowingModifiers) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_BadSpecifierCombo2, "Overloads", "Shadows")
                ElseIf (currentModifier And (SourceMemberFlags.Overrides Or SourceMemberFlags.Overridable)) <> 0 AndAlso
                       (foundModifiers And (SourceMemberFlags.Overrides Or SourceMemberFlags.Overridable)) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_OverridesImpliesOverridable)
                ElseIf (currentModifier And SourceMemberFlags.PrivateOverridableModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.PrivateOverridableModifiers) <> 0 Then
                    privateOverridableModifier = keywordSyntax
                    foundModifiers = foundModifiers Or currentModifier
                ElseIf (currentModifier And SourceMemberFlags.ShadowsAndOverrides) <> 0 AndAlso
                    (foundModifiers And SourceMemberFlags.ShadowsAndOverrides) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_BadSpecifierCombo2, "Overrides", "Shadows")
                ElseIf (currentModifier And SourceMemberFlags.PrivateMustOverrideModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.PrivateMustOverrideModifiers) <> 0 Then
                    privateMustOverrideModifier = keywordSyntax
                    foundModifiers = foundModifiers Or currentModifier
                ElseIf (currentModifier And SourceMemberFlags.PrivateNotOverridableModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.PrivateNotOverridableModifiers) <> 0 Then
                    privateNotOverridableModifier = keywordSyntax
                    foundModifiers = foundModifiers Or currentModifier
                ElseIf (currentModifier And (SourceMemberFlags.Iterator Or SourceMemberFlags.WriteOnly)) <> 0 AndAlso
                       (foundModifiers And (SourceMemberFlags.Iterator Or SourceMemberFlags.WriteOnly)) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_BadSpecifierCombo2, "Iterator", "WriteOnly")
                ElseIf (currentModifier And SourceMemberFlags.TypeInheritModifiers) <> 0 AndAlso
                       (foundModifiers And SourceMemberFlags.TypeInheritModifiers) <> 0 Then
                    ReportDiagnostic(diagBag, keywordSyntax, ERRID.ERR_BadSpecifierCombo2, "MustInherit", "NotInheritable")
                    '  Note: Need to be present both MustInherit & NotInheritable for properly reporting #30926
                    foundModifiers = foundModifiers Or currentModifier
                Else
                    If currentModifier = SourceMemberFlags.Private OrElse currentModifier = SourceMemberFlags.Protected Then
                        privateProtectedToken = keywordSyntax
                    End If
                    foundModifiers = foundModifiers Or currentModifier
                End If
            Next

            ' Convert the accessibility modifiers into accessibility.
            Dim access As Accessibility
            If (foundModifiers And SourceMemberFlags.Public) <> 0 Then
                access = Accessibility.Public
            ElseIf (foundModifiers And (SourceMemberFlags.Friend Or SourceMemberFlags.Protected)) = (SourceMemberFlags.Friend Or SourceMemberFlags.Protected) Then
                access = Accessibility.ProtectedOrFriend
            ElseIf (foundModifiers And (SourceMemberFlags.Private Or SourceMemberFlags.Protected)) = (SourceMemberFlags.Private Or SourceMemberFlags.Protected) Then
                access = Accessibility.ProtectedAndFriend
                InternalSyntax.Parser.CheckFeatureAvailability(
                    diagBag,
                    privateProtectedToken.GetLocation(),
                    DirectCast(privateProtectedToken.SyntaxTree, VisualBasicSyntaxTree).Options.LanguageVersion,
                    InternalSyntax.Feature.PrivateProtected)
            ElseIf (foundModifiers And SourceMemberFlags.Friend) <> 0 Then
                access = Accessibility.Friend
            ElseIf (foundModifiers And SourceMemberFlags.Protected) <> 0 Then
                access = Accessibility.Protected
            ElseIf (foundModifiers And SourceMemberFlags.Private) <> 0 Then
                access = Accessibility.Private
            Else
                access = defaultAccessibility
            End If

            If access = Accessibility.Private Then
                If (foundModifiers And SourceMemberFlags.Overridable) <> 0 Then
                    ReportDiagnostic(diagBag, privateOverridableModifier, ERRID.ERR_BadSpecifierCombo2, "Private", "Overridable")
                ElseIf (foundModifiers And SourceMemberFlags.MustOverride) <> 0 Then
                    ReportDiagnostic(diagBag, privateMustOverrideModifier, ERRID.ERR_BadSpecifierCombo2, "Private", "MustOverride")
                ElseIf (foundModifiers And SourceMemberFlags.NotOverridable) <> 0 Then
                    ReportDiagnostic(diagBag, privateNotOverridableModifier, ERRID.ERR_BadSpecifierCombo2, "Private", "NotOverridable")
                End If
                foundModifiers = foundModifiers And Not (SourceMemberFlags.Overridable Or SourceMemberFlags.MustOverride Or SourceMemberFlags.NotOverridable)
            End If

            ' Add accessibility into the flags.
            Return New MemberModifiers(foundModifiers, DirectCast(access, SourceMemberFlags))
        End Function

        ''' <summary>
        ''' Decode a list of parameter modifiers, and return the flags associated with it.
        ''' </summary>
        Private Shared Function DecodeParameterModifiers(container As Symbol,
                                                 modifiers As SyntaxTokenList,
                                                 checkModifier As CheckParameterModifierDelegate,
                                                 diagBag As BindingDiagnosticBag) As SourceParameterFlags
            Dim flags As SourceParameterFlags = Nothing

            ' Go through each modifiers, accumulating flags of what we've seen and reporting errors.
            For Each keywordSyntax In modifiers
                Dim foundFlag As SourceParameterFlags

                Select Case keywordSyntax.Kind
                    Case SyntaxKind.ByRefKeyword : foundFlag = SourceParameterFlags.ByRef
                    Case SyntaxKind.ByValKeyword : foundFlag = SourceParameterFlags.ByVal
                    Case SyntaxKind.OptionalKeyword : foundFlag = SourceParameterFlags.Optional
                    Case SyntaxKind.ParamArrayKeyword : foundFlag = SourceParameterFlags.ParamArray
                End Select

                ' Report errors with the modifier
                If checkModifier IsNot Nothing Then
                    ' check modifier will clear invalid flags
                    foundFlag = checkModifier(container, keywordSyntax, foundFlag, diagBag)
                End If

                flags = flags Or foundFlag
            Next

            Return flags
        End Function

        ''' <summary>
        ''' Create the Nullable version of a type.
        ''' </summary>
        Public Function CreateNullableOf(typeArgument As TypeSymbol,
                                         syntax As VisualBasicSyntaxNode,
                                         syntaxTypeArgument As VisualBasicSyntaxNode,
                                         diagBag As BindingDiagnosticBag) As NamedTypeSymbol
            ' Get the Nullable type
            Dim nullableType As NamedTypeSymbol = DirectCast(GetSpecialType(SpecialType.System_Nullable_T, syntax, diagBag), NamedTypeSymbol)

            ' Construct the Nullable(Of T).
            Dim constructedType = nullableType.Construct(ImmutableArray.Create(typeArgument))

            ' Validate the type argument meets the Structure constraint on the Nullable type.
            If ShouldCheckConstraints Then
                Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing
                constructedType.CheckConstraints(diagnosticsBuilder, useSiteDiagnosticsBuilder, template:=GetNewCompoundUseSiteInfo(diagBag))

                If useSiteDiagnosticsBuilder IsNot Nothing Then
                    diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
                End If

                For Each pair In diagnosticsBuilder
                    diagBag.Add(pair.UseSiteInfo, syntaxTypeArgument.GetLocation())
                Next
                diagnosticsBuilder.Free()
            End If

            Return constructedType
        End Function

        Public Function GetNewCompoundUseSiteInfo(futureDestination As BindingDiagnosticBag) As CompoundUseSiteInfo(Of AssemblySymbol)
            Return New CompoundUseSiteInfo(Of AssemblySymbol)(futureDestination, Compilation.Assembly)
        End Function

        ''' <summary>
        ''' Possible create the array version of type, given the element type and the array modifier syntax.
        ''' </summary>
        Public Function ApplyArrayRankSpecifiersToType(elementType As TypeSymbol,
                                      arrayModifierSyntax As SyntaxList(Of ArrayRankSpecifierSyntax),
                                      diagnostics As BindingDiagnosticBag) As TypeSymbol

            Dim currentType As TypeSymbol = elementType

            ' Array modifiers must be handled in reverse order, that's the language syntax.
            For i As Integer = arrayModifierSyntax.Count - 1 To 0 Step -1
                ' Rank of the array is the number of commas, plus one.
                Dim arrayModifier = arrayModifierSyntax(i)

                If arrayModifier.Rank > 32 Then
                    ReportDiagnostic(diagnostics, arrayModifier, ERRID.ERR_ArrayRankLimit)
                End If

                currentType = ArrayTypeSymbol.CreateVBArray(currentType, Nothing, arrayModifier.Rank, Compilation)
            Next

            Return currentType
        End Function

        ''' <summary>
        ''' Possibly create the array version of type, given the element type and the array modifier syntax.
        ''' </summary>
        Public Function ApplyArrayRankSpecifiersAndBoundsToType(elementType As TypeSymbol,
                                      arrayModifierSyntax As SyntaxList(Of ArrayRankSpecifierSyntax),
                                      arrayBoundsOpt As ArgumentListSyntax,
                                      diagnostics As BindingDiagnosticBag) As TypeSymbol

            Dim currentType As TypeSymbol = elementType

            ' Array modifiers must be handled in reverse order, that's the language syntax.
            currentType = ApplyArrayRankSpecifiersToType(elementType, arrayModifierSyntax, diagnostics)

            ' Array bounds must be handled in reverse order, that's the language syntax.
            If arrayBoundsOpt IsNot Nothing Then
                Dim rank As Integer = arrayBoundsOpt.Arguments.Count

                If rank = 0 Then
                    rank = 1
                End If

                If rank > 32 Then
                    ReportDiagnostic(diagnostics, arrayBoundsOpt, ERRID.ERR_ArrayRankLimit)
                End If

                currentType = ArrayTypeSymbol.CreateVBArray(currentType, Nothing, rank, Compilation)
            End If

            Return currentType
        End Function

        ''' <summary>
        ''' Create the array version of type, given the element type and the array modifier syntax. Throws if
        ''' there aren't any array modifiers and the result is not an array type.
        ''' </summary>
        Public Function CreateArrayOf(elementType As TypeSymbol,
                                      arrayModifierSyntax As SyntaxList(Of ArrayRankSpecifierSyntax),
                                      arrayBoundsOpt As ArgumentListSyntax,
                                      diagnostics As BindingDiagnosticBag) As ArrayTypeSymbol

            Debug.Assert(arrayModifierSyntax.Count > 0 OrElse
                                  arrayBoundsOpt IsNot Nothing)

            Return DirectCast(ApplyArrayRankSpecifiersAndBoundsToType(elementType,
                                                    arrayModifierSyntax,
                                                    arrayBoundsOpt,
                                                    diagnostics), ArrayTypeSymbol)
        End Function

        ''' <summary>
        ''' Given an identifier and an As clause, return true if the identifier does not have a type
        ''' declared for it (e.g., no type character and no as clause).
        ''' </summary>
        Private Shared Function HasDefaultType(identifierSyntax As SyntaxToken,
                                        asClauseOptSyntax As AsClauseSyntax) As Boolean
            Return (identifierSyntax.GetTypeCharacter() = TypeCharacter.None AndAlso asClauseOptSyntax Is Nothing)
        End Function

        ''' <summary>
        ''' Given an identifier and an As clause, return true if the identifier does not have a type
        ''' declared for it (e.g., no type character and no as clause).
        ''' </summary>
        Private Shared Function HasDefaultType(identifierSyntax As ModifiedIdentifierSyntax,
                                       asClauseOptSyntax As AsClauseSyntax) As Boolean
            Return HasDefaultType(identifierSyntax.Identifier, asClauseOptSyntax)
        End Function

        ''' <summary>
        ''' Given an identifier, return true if the identifier declares an array.
        ''' (e.g., identifier  specifies ())
        ''' </summary>
        Public Shared Function IsArrayType(identifierSyntax As ModifiedIdentifierSyntax) As Boolean
            Return identifierSyntax.ArrayBounds IsNot Nothing OrElse identifierSyntax.ArrayRankSpecifiers.Count > 0
        End Function

        ''' <summary>
        ''' Flags to specify where the decoding of the modified identifier's type happens.
        ''' </summary>
        <Flags()>
        Public Enum ModifiedIdentifierTypeDecoderContext
            ''' <summary>
            ''' No context given (default).
            ''' </summary>
            None = 0

            ''' <summary>
            ''' Modified identifier appeared in a lambda declaration.
            ''' </summary>
            LambdaType = 1 << 1

            ''' <summary>
            ''' Modified identifier appeared in a local declaration.
            ''' </summary>
            LocalType = 1 << 2

            ''' <summary>
            ''' Modified identifier appeared in a field declaration.
            ''' </summary>
            FieldType = 1 << 3

            ''' <summary>
            ''' Modified identifier appeared in a parameter.
            ''' </summary>
            ParameterType = 1 << 4

            ''' <summary>
            ''' Modified identifier appeared in a query range variable declaration.
            ''' </summary>
            QueryRangeVariableType = 1 << 5

            ''' <summary>
            ''' Combined flag to express that a modified identifier appeared in a local or field declaration.
            ''' </summary>
            LocalOrFieldType = LocalType Or
                               FieldType

            ''' <summary>
            ''' Combined flag to express that a modified identifier appeared in a parameter of a lambda.
            ''' </summary>
            LambdaParameterType = LambdaType Or
                                  ParameterType

            StaticLocalType = 1 << 6
        End Enum

        ''' <summary>
        ''' Given a modified identifier and a type, return the actual type to use. Applies the type character
        ''' and type modifiers to the given type.
        ''' </summary>
        ''' <param name="modifiedIdentifier">The modified identifier.</param>
        ''' <param name="asClauseOrValueType">Bound type after the As or a type from the initializing value expression. Can be nothing if no type was supplied.</param>
        ''' <param name="asClauseSyntaxOpt">If specified then it is the syntax for the as clause and the type is the bound type from this syntax.</param>
        ''' <param name="getRequireTypeDiagnosticInfoFunc">Delegate to get diagnostic info to generate if a required type is missing (Option Strict On/Custom) </param>
        ''' <returns>The type, as modified by the type character, type modifiers. Uses Object as default if needed.</returns>
        ''' <remarks></remarks>
        Public Function DecodeModifiedIdentifierType(modifiedIdentifier As ModifiedIdentifierSyntax,
                                                     asClauseOrValueType As TypeSymbol,
                                                     asClauseSyntaxOpt As AsClauseSyntax,
                                                     initializerSyntaxOpt As VisualBasicSyntaxNode,
                                                     getRequireTypeDiagnosticInfoFunc As Func(Of DiagnosticInfo),
                                                     diagBag As BindingDiagnosticBag,
                                                     Optional decoderContext As ModifiedIdentifierTypeDecoderContext = ModifiedIdentifierTypeDecoderContext.None
        ) As TypeSymbol
            Dim baseType As TypeSymbol = DecodeIdentifierType(modifiedIdentifier.Identifier, asClauseOrValueType, getRequireTypeDiagnosticInfoFunc, diagBag, decoderContext)

            ' Create nullable type.
            If modifiedIdentifier.Nullable.Node IsNot Nothing Then

                If asClauseSyntaxOpt IsNot Nothing Then
                    If asClauseSyntaxOpt.Type.Kind = SyntaxKind.NullableType Then
                        ' Special case error for e.g.: "x? as Integer?"
                        ReportDiagnostic(diagBag, asClauseSyntaxOpt, ERRID.ERR_CantSpecifyNullableOnBoth)
                        ' Return the type from the "as" clause, rather than creating a
                        ' second nullable type below (since the user probably intended
                        ' "x? as Integer?" to represent Integer? rather than Integer??).
                        Return baseType
                    ElseIf asClauseSyntaxOpt.Kind = SyntaxKind.AsNewClause Then
                        ReportDiagnostic(diagBag, asClauseSyntaxOpt, ERRID.ERR_CantSpecifyAsNewAndNullable)
                    ElseIf asClauseOrValueType.IsArrayType Then
                        ' ReportDiagnostic(diagBag, asClauseSyntaxOpt, ERRID.ERR_CantSpecifyArrayAndNullableOnBoth)
                    End If
                End If

                If asClauseOrValueType Is Nothing AndAlso baseType.IsObjectType() Then
                    ' We didn't have a type specified and ended up with Object,
                    ' which means that we didn't have type character as well.
                    Debug.Assert(asClauseSyntaxOpt Is Nothing)
                    If (decoderContext And ModifiedIdentifierTypeDecoderContext.ParameterType) <> 0 Then
                        ReportDiagnostic(diagBag, modifiedIdentifier, ERRID.ERR_NullableParameterMustSpecifyType)
                    ElseIf (decoderContext And ModifiedIdentifierTypeDecoderContext.LocalOrFieldType) <> 0 AndAlso
                           ((decoderContext And ModifiedIdentifierTypeDecoderContext.StaticLocalType) <> 0 OrElse
                            (initializerSyntaxOpt Is Nothing AndAlso
                             modifiedIdentifier.ArrayBounds Is Nothing)) AndAlso
                           OptionInfer Then
                        ' Note: fields do not support Option Infer On / local type inference, but Dev10 shared the error 
                        ' reporting between locals and fields like Roslyn does. So these error will also be shown (and they make 
                        ' sense) for fields in case Option Infer is On.

                        ' If option infer is off, variable declarations without an AsClause are explicitly
                        ' set to Object, but this message should only be shown for when the type is implicitly system.object
                        ReportDiagnostic(diagBag, modifiedIdentifier, ERRID.ERR_NullableImplicit)
                    End If

                ElseIf Not baseType.IsValueType Then
                    ' Trying to make a nullable reference type.
                    ReportDiagnostic(diagBag, modifiedIdentifier, ERRID.ERR_BadTypeArgForStructConstraintNull, baseType)
                Else
                    baseType = CreateNullableOf(baseType,
                                                modifiedIdentifier,
                                                If(asClauseSyntaxOpt IsNot Nothing,
                                                   asClauseSyntaxOpt.Type,
                                                   DirectCast(modifiedIdentifier, VisualBasicSyntaxNode)),
                                                diagBag)
                End If
            End If

            ' Check for array modifier on the identifier and array type. VB doesn't allow this combination. 
            If IsArrayType(modifiedIdentifier) Then
                If asClauseSyntaxOpt IsNot Nothing Then
                    If asClauseSyntaxOpt.Type.Kind = SyntaxKind.ArrayType Then
                        ReportDiagnostic(diagBag, asClauseSyntaxOpt.Type, ERRID.ERR_CantSpecifyArraysOnBoth)
                    ElseIf asClauseSyntaxOpt.Type.Kind = SyntaxKind.NullableType Then
                        ' ReportDiagnostic(diagBag, asClauseSyntaxOpt, ERRID.ERR_CantSpecifyArrayAndNullableOnBoth)
                    End If

                ElseIf (decoderContext And ModifiedIdentifierTypeDecoderContext.LambdaParameterType) = ModifiedIdentifierTypeDecoderContext.LambdaParameterType Then
                    ' In lambda parameters's we don't allow array specifiers without the type since
                    ' inference can become ambiguous, see bug dd:80897
                    ReportDiagnostic(diagBag, modifiedIdentifier, ERRID.ERR_CantSpecifyParamsOnLambdaParamNoType)
                End If
            End If

            Return ApplyArrayRankSpecifiersAndBoundsToType(baseType, modifiedIdentifier.ArrayRankSpecifiers, modifiedIdentifier.ArrayBounds, diagBag)

        End Function

        ''' <summary>
        ''' Given a modified identifier and a type syntax, return the actual type to use. Applies the type character
        ''' and type modifiers to the given type.
        ''' </summary>
        ''' <param name="modifiedIdentifier">The modified identifier.</param>
        ''' <param name="asClauseOpt"> As clause syntax. Can be nothing if no type was supplied.</param>
        ''' <param name="getRequireTypeDiagnosticInfoFunc">Delegate to get diagnostic info to generate if a required type is missing (Option Strict On/Custom) </param>
        ''' <param name="asClauseType">The type of the AsClauseOpt before applying any modifiers</param>
        ''' <returns>The type, as modified by the type character, type modifiers. Uses Object as default if needed.</returns>
        Public Function DecodeModifiedIdentifierType(modifiedIdentifier As ModifiedIdentifierSyntax,
                                                     asClauseOpt As AsClauseSyntax,
                                                     initializerSyntaxOpt As EqualsValueSyntax,
                                                     getRequireTypeDiagnosticInfoFunc As Func(Of DiagnosticInfo),
                                                     <Out()> ByRef asClauseType As TypeSymbol,
                                                     diagBag As BindingDiagnosticBag,
                                                     Optional decoderContext As ModifiedIdentifierTypeDecoderContext = Nothing
        ) As TypeSymbol

            If asClauseOpt IsNot Nothing Then

                ' NOTE: This method is NOT supposed to be called for 'As New With {...}'
                '       expression, if it is it should be revised to return bound node
                Debug.Assert(asClauseOpt.Kind <> SyntaxKind.AsNewClause OrElse
                             DirectCast(asClauseOpt, AsNewClauseSyntax).NewExpression.Kind <> SyntaxKind.AnonymousObjectCreationExpression)

                asClauseType = BindTypeSyntax(asClauseOpt.Type, diagBag)
            Else
                asClauseType = Nothing
            End If

            Return DecodeModifiedIdentifierType(modifiedIdentifier, asClauseType, asClauseOpt, initializerSyntaxOpt, getRequireTypeDiagnosticInfoFunc, diagBag, decoderContext)

        End Function

        ''' <summary>
        ''' Given a modified identifier and a type syntax, return the actual type to use. Applies the type character
        ''' and type modifiers to the given type.
        ''' </summary>
        ''' <param name="modifiedIdentifier">The modified identifier.</param>
        ''' <param name="asClauseOpt"> As clause syntax. Can be nothing if no type was supplied.</param>
        ''' <param name="getRequireTypeDiagnosticInfoFunc">Delegate to get diagnostic info to generate if a required type is missing (Option Strict On/Custom) </param>
        ''' <returns>The type, as modified by the type character, type modifiers. Uses Object as default if needed.</returns>
        Public Function DecodeModifiedIdentifierType(modifiedIdentifier As ModifiedIdentifierSyntax,
                                                     asClauseOpt As AsClauseSyntax,
                                                     initializerSyntaxOpt As EqualsValueSyntax,
                                                     getRequireTypeDiagnosticInfoFunc As Func(Of DiagnosticInfo),
                                                     diagBag As BindingDiagnosticBag,
                                                     Optional decoderContext As ModifiedIdentifierTypeDecoderContext = Nothing
        ) As TypeSymbol

            Dim asClauseType As TypeSymbol = Nothing
            Return DecodeModifiedIdentifierType(modifiedIdentifier, asClauseOpt, initializerSyntaxOpt, getRequireTypeDiagnosticInfoFunc, asClauseType, diagBag, decoderContext)

        End Function

        ''' <summary>
        ''' Given a identifier and as clause syntax, return the actual type to use. Uses the type character or the type syntax.
        ''' </summary>
        ''' <param name="identifier">The identifier.</param>
        ''' <param name="asClauseOpt">Syntax for optional as clause. Can be nothing if no type was supplied.</param>
        ''' <param name="getRequireTypeDiagnosticInfoFunc">Delegate to get diagnostic info to generate if a required type is missing (Option Strict On/Custom) </param>
        ''' <returns>The type, either from the type character or the as clause. Uses Object as default if needed.</returns>
        ''' <remarks></remarks>
        Public Function DecodeIdentifierType(identifier As SyntaxToken,
                                             asClauseOpt As AsClauseSyntax,
                                             getRequireTypeDiagnosticInfoFunc As Func(Of DiagnosticInfo),
                                             ByRef asClauseType As TypeSymbol,
                                             diagBag As BindingDiagnosticBag) As TypeSymbol

            If asClauseOpt IsNot Nothing Then
                asClauseType = BindTypeSyntax(asClauseOpt.Type, diagBag)
            Else
                asClauseType = Nothing
            End If

            Return DecodeIdentifierType(identifier, asClauseType, getRequireTypeDiagnosticInfoFunc, diagBag)
        End Function

        ''' <summary>
        ''' Given a identifier and as clause syntax, return the actual type to use. Uses the type character or the type syntax.
        ''' </summary>
        ''' <param name="identifier">The identifier.</param>
        ''' <param name="asClauseOpt">Syntax for optional as clause. Can be nothing if no type was supplied.</param>
        ''' <param name="getRequireTypeDiagnosticInfoFunc">Delegate to get diagnostic info to generate if a required type is missing (Option Strict On/Custom) </param>
        ''' <returns>The type, either from the type character or the as clause. Uses Object as default if needed.</returns>
        ''' <remarks></remarks>
        Public Function DecodeIdentifierType(identifier As SyntaxToken,
                                             asClauseOpt As AsClauseSyntax,
                                             getRequireTypeDiagnosticInfoFunc As Func(Of DiagnosticInfo),
                                             diagBag As BindingDiagnosticBag) As TypeSymbol
            Dim asClauseType As TypeSymbol = Nothing
            Return DecodeIdentifierType(identifier, asClauseOpt, getRequireTypeDiagnosticInfoFunc, asClauseType, diagBag)
        End Function

        ''' <summary>
        ''' Given a identifier and a type, return the actual type to use. Uses the type character or the given type.
        ''' </summary>
        ''' <param name="identifier">The identifier.</param>
        ''' <param name="asClauseType">Bound type after the As. Can be nothing if no type was supplied.</param>
        ''' <param name="getRequireTypeDiagnosticInfoFunc">Delegate to get diagnostic info to generate if a required type is missing (Option Strict On/Custom) </param>
        ''' <returns>The type, either from the type character or the as clause type. Uses Object as default if needed.</returns>
        ''' <remarks></remarks>
        Public Function DecodeIdentifierType(identifier As SyntaxToken,
                                             asClauseType As TypeSymbol,
                                             getRequireTypeDiagnosticInfoFunc As Func(Of DiagnosticInfo),
                                             diagBag As BindingDiagnosticBag,
                                             Optional decoderContext As ModifiedIdentifierTypeDecoderContext = ModifiedIdentifierTypeDecoderContext.None
        ) As TypeSymbol
            Dim typeCharacterType As TypeSymbol = Nothing
            Dim typeCharacterString As String = Nothing
            Dim specialType As SpecialType = GetSpecialTypeForTypeCharacter(identifier.GetTypeCharacter(), typeCharacterString)

            If specialType <> Microsoft.CodeAnalysis.SpecialType.None Then
                typeCharacterType = GetSpecialType(specialType, identifier, diagBag)
            End If

            If asClauseType IsNot Nothing Then
                ' We have a special error for this situation with query range variables, it is reported 
                ' by the query binding code.
                If typeCharacterType IsNot Nothing AndAlso
                   (decoderContext And ModifiedIdentifierTypeDecoderContext.QueryRangeVariableType) = 0 Then
                    ReportDiagnostic(diagBag, identifier, ERRID.ERR_TypeCharWithType1, typeCharacterString)
                End If
                Return asClauseType
            ElseIf typeCharacterType IsNot Nothing Then
                Return typeCharacterType
            Else
                If getRequireTypeDiagnosticInfoFunc IsNot Nothing Then
                    ReportDiagnostic(diagBag, identifier, getRequireTypeDiagnosticInfoFunc())
                End If

                ' default type is object.
                Return GetSpecialType(SpecialType.System_Object, identifier, diagBag)
            End If
        End Function

        Public Shared Function GetSpecialTypeForTypeCharacter(typeChar As TypeCharacter, ByRef typeCharacterString As String) As SpecialType
            Dim specialType As SpecialType = SpecialType.None

            Select Case typeChar
                Case TypeCharacter.Decimal
                    specialType = SpecialType.System_Decimal
                    typeCharacterString = "@"
                Case TypeCharacter.DecimalLiteral
                    specialType = SpecialType.System_Decimal
                    typeCharacterString = "D"
                Case TypeCharacter.Double
                    specialType = SpecialType.System_Double
                    typeCharacterString = "#"
                Case TypeCharacter.DoubleLiteral
                    specialType = SpecialType.System_Double
                    typeCharacterString = "R"
                Case TypeCharacter.Integer
                    specialType = SpecialType.System_Int32
                    typeCharacterString = "%"
                Case TypeCharacter.IntegerLiteral
                    specialType = SpecialType.System_Int32
                    typeCharacterString = "I"
                Case TypeCharacter.Long
                    specialType = SpecialType.System_Int64
                    typeCharacterString = "&"
                Case TypeCharacter.LongLiteral
                    specialType = SpecialType.System_Int64
                    typeCharacterString = "L"
                Case TypeCharacter.ShortLiteral
                    specialType = SpecialType.System_Int16
                    typeCharacterString = "S"
                Case TypeCharacter.Single
                    specialType = SpecialType.System_Single
                    typeCharacterString = "!"
                Case TypeCharacter.SingleLiteral
                    specialType = SpecialType.System_Single
                    typeCharacterString = "F"
                Case TypeCharacter.String
                    specialType = SpecialType.System_String
                    typeCharacterString = "$"
                Case TypeCharacter.UIntegerLiteral
                    specialType = SpecialType.System_UInt32
                    typeCharacterString = "UI"
                Case TypeCharacter.ULongLiteral
                    specialType = SpecialType.System_UInt64
                    typeCharacterString = "UL"
                Case TypeCharacter.UShortLiteral
                    specialType = SpecialType.System_UInt16
                    typeCharacterString = "US"
                Case TypeCharacter.None
                    typeCharacterString = Nothing
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(typeChar)
            End Select

            Return specialType
        End Function

        Public Shared Function ExtractTypeCharacter(node As SyntaxNode) As TypeCharacter
            Dim result As TypeCharacter = TypeCharacter.None

            If node IsNot Nothing Then
                Select Case node.Kind
                    Case SyntaxKind.IdentifierName
                        result = DirectCast(node, IdentifierNameSyntax).Identifier.GetTypeCharacter()

                    Case SyntaxKind.GenericName
                        result = DirectCast(node, GenericNameSyntax).Identifier.GetTypeCharacter()

                    Case SyntaxKind.SimpleMemberAccessExpression, SyntaxKind.DictionaryAccessExpression
                        result = ExtractTypeCharacter(DirectCast(node, MemberAccessExpressionSyntax).Name)
                End Select
            End If

            Return result
        End Function

        ''' <summary>
        ''' Decode an option "On" or "Off" values into true or false. Not specified is considered true.
        ''' </summary>
        Public Shared Function DecodeOnOff(keywordSyntax As SyntaxToken) As Boolean
            If keywordSyntax.Node Is Nothing Then
                Return True
            Else
                Select Case keywordSyntax.Kind
                    Case SyntaxKind.OnKeyword
                        Return True
                    Case SyntaxKind.OffKeyword
                        Return False
                    Case Else
                        Throw ExceptionUtilities.UnexpectedValue(keywordSyntax.Kind)
                End Select
            End If
        End Function

        ''' <summary>
        ''' Decode an option "Text" or "Binary" value into true or false. The syntax is not optional.
        ''' </summary>
        Public Shared Function DecodeTextBinary(keywordSyntax As SyntaxToken) As Boolean?
            If keywordSyntax.Node Is Nothing Then
                Return Nothing ' Must be a syntax error, an error is reported elsewhere
            End If

            Select Case keywordSyntax.Kind
                Case SyntaxKind.TextKeyword
                    Return True
                Case SyntaxKind.BinaryKeyword
                    Return False
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(keywordSyntax.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Decode a parameter list from a delegate declaration into a list of parameter symbols.
        ''' </summary>
        ''' <param name="container">Containing method declaration.</param>
        ''' <param name="syntaxOpt">Optional parameter list syntax</param>
        Public Function DecodeParameterListOfDelegateDeclaration(
            container As Symbol,
            syntaxOpt As ParameterListSyntax,
            diagBag As BindingDiagnosticBag
        ) As ImmutableArray(Of ParameterSymbol)

            If syntaxOpt Is Nothing Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            End If

            Dim parametersSyntax = syntaxOpt.Parameters
            Dim params = ArrayBuilder(Of ParameterSymbol).GetInstance(parametersSyntax.Count)
            Dim result As ImmutableArray(Of ParameterSymbol)
            DecodeParameterList(
                    container,
                    False,
                    SourceMemberFlags.None,
                    parametersSyntax,
                    params,
                    s_checkDelegateParameterModifierCallback,
                    diagBag)

            ' no checks for the parameters:
            ' Section 9.2.5, "Method Parameters" from the VB Language Reference mentions that duplicate parameter names are 
            ' allowed but discouraged for delegates and extern methods
            ' Dev10: Function delegates are allowed to have one or more parameters called Invoke

            result = params.ToImmutable
            params.Free()

            Return result
        End Function

        ''' <summary>
        ''' Decode a parameter list into a list of parameter symbols.
        ''' </summary>
        ''' <param name="container">Containing method declaration.</param>
        ''' <param name="isFromLambda">Parameter is for a lambda expression rather than a regular method.</param>
        ''' <param name="syntaxOpt">Optional parameter list syntax</param>
        ''' <remarks>DO NOT call this to get the parameters of a delegate declaration (<see>DecodeParameterListOfDelegateDeclaration</see>).</remarks>
        Public Function DecodeParameterList(container As Symbol,
                                            isFromLambda As Boolean,
                                            modifiers As SourceMemberFlags,
                                            syntaxOpt As ParameterListSyntax,
                                            diagBag As BindingDiagnosticBag) As ImmutableArray(Of ParameterSymbol)
            Debug.Assert(Not (container.Kind = SymbolKind.Method AndAlso DirectCast(container, MethodSymbol).MethodKind = MethodKind.DelegateInvoke))

            If syntaxOpt Is Nothing Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            End If

            Dim params = ArrayBuilder(Of ParameterSymbol).GetInstance(syntaxOpt.Parameters.Count)
            Dim result As ImmutableArray(Of ParameterSymbol)
            Dim methodSymbol = TryCast(container, MethodSymbol)
            Dim modifierValidator As CheckParameterModifierDelegate = Nothing

            If methodSymbol IsNot Nothing AndAlso methodSymbol.IsUserDefinedOperator() Then
                modifierValidator = s_checkOperatorParameterModifierCallback
            End If

            DecodeParameterList(
                container,
                isFromLambda,
                modifiers,
                syntaxOpt.Parameters,
                params,
                modifierValidator,
                diagBag)

            Dim typeParams As ImmutableArray(Of TypeParameterSymbol) = If(methodSymbol IsNot Nothing AndAlso Not isFromLambda,
                                                                         methodSymbol.TypeParameters,
                                                                         ImmutableArray(Of TypeParameterSymbol).Empty)

            For i = 0 To params.Count - 1
                Dim paramSyntax = syntaxOpt.Parameters(i)
                Dim param = params(i)

                If methodSymbol IsNot Nothing Then
                    If Not methodSymbol.IsSub AndAlso Not methodSymbol.IsUserDefinedOperator() Then
                        ' "Parameter cannot have the same name as its defining function."
                        CheckReservedParameterName(container.Name, paramSyntax, ERRID.ERR_ParamNameFunctionNameCollision, diagBag)
                    End If

                    ' Section 9.2.5, "Method Parameters" from the VB Language Reference mentions that duplicate parameter names 
                    ' are allowed but discouraged for delegates and extern methods
                    If Not methodSymbol.MethodKind = MethodKind.DeclareMethod Then
                        CheckParameterNameNotDuplicate(params, i, paramSyntax, param, diagBag)
                    End If
                End If

                Dim name = param.Name
                For Each tp In typeParams
                    If CaseInsensitiveComparison.Equals(tp.Name, name) Then
                        ' "'{0}' is already declared as a type parameter of this method."
                        ReportDiagnostic(diagBag, paramSyntax.Identifier, ERRID.ERR_NameSameAsMethodTypeParam1, name)
                        Exit For
                    End If
                Next
            Next

            result = params.ToImmutable
            params.Free()

            Return result
        End Function

        Private Shared ReadOnly s_checkOperatorParameterModifierCallback As CheckParameterModifierDelegate = AddressOf CheckOperatorParameterModifier

        Private Shared Function CheckOperatorParameterModifier(container As Symbol, token As SyntaxToken, flag As SourceParameterFlags, diagnostics As BindingDiagnosticBag) As SourceParameterFlags
            If (flag And SourceParameterFlags.ByRef) <> 0 Then
                diagnostics.Add(ERRID.ERR_ByRefIllegal1, token.GetLocation(), container.GetKindText())
                flag = flag And (Not SourceParameterFlags.ByRef)
            End If

            If (flag And SourceParameterFlags.ParamArray) <> 0 Then
                diagnostics.Add(ERRID.ERR_ParamArrayIllegal1, token.GetLocation(), container.GetKindText())
                flag = flag And (Not SourceParameterFlags.ParamArray)
            End If

            If (flag And SourceParameterFlags.Optional) <> 0 Then
                diagnostics.Add(ERRID.ERR_OptionalIllegal1, token.GetLocation(), container.GetKindText())
                flag = flag And (Not SourceParameterFlags.Optional)
            End If

            Return flag
        End Function

        ''' <summary>
        ''' Decode a parameter list into a list of parameter symbols.
        ''' </summary>
        ''' <param name="container">Containing property declaration.</param>
        ''' <param name="syntaxOpt">Optional parameter list syntax</param>
        Public Function DecodePropertyParameterList(container As PropertySymbol,
                                            syntaxOpt As ParameterListSyntax,
                                            diagBag As BindingDiagnosticBag) As ImmutableArray(Of ParameterSymbol)
            If syntaxOpt Is Nothing Then
                Return ImmutableArray(Of ParameterSymbol).Empty
            End If

            Dim params = ArrayBuilder(Of ParameterSymbol).GetInstance(syntaxOpt.Parameters.Count)
            Dim result As ImmutableArray(Of ParameterSymbol)
            DecodeParameterList(
                    container,
                    False,
                    SourceMemberFlags.None,
                    syntaxOpt.Parameters,
                    params,
                    s_checkPropertyParameterModifierCallback,
                    diagBag)

            For i = 0 To params.Count - 1
                Dim paramSyntax = syntaxOpt.Parameters(i)
                Dim param = params(i)

                ' "Property parameters cannot have the name 'Value'."
                If CheckReservedParameterName(StringConstants.ValueParameterName, paramSyntax, ERRID.ERR_PropertySetParamCollisionWithValue, diagBag) Then
                    ' "Parameter cannot have the same name as its defining function."
                    CheckReservedParameterName(container.Name, paramSyntax, ERRID.ERR_ParamNameFunctionNameCollision, diagBag)
                End If

                CheckParameterNameNotDuplicate(params, i, paramSyntax, param, diagBag)
            Next

            result = params.ToImmutable
            params.Free()

            Return result
        End Function

        Private Shared ReadOnly s_checkPropertyParameterModifierCallback As CheckParameterModifierDelegate = AddressOf CheckPropertyParameterModifier

        Private Shared Function CheckPropertyParameterModifier(container As Symbol, token As SyntaxToken, flag As SourceParameterFlags, diagnostics As BindingDiagnosticBag) As SourceParameterFlags
            If flag = SourceParameterFlags.ByRef Then
                Dim location = token.GetLocation()
                diagnostics.Add(ERRID.ERR_ByRefIllegal1, location, container.GetKindText(), token.ToString())
                Return flag And (Not SourceParameterFlags.ByRef)
            End If
            Return flag
        End Function

        Private Shared Function CheckReservedParameterName(reservedName As String, syntax As ParameterSyntax, errorId As ERRID, diagnostics As BindingDiagnosticBag) As Boolean
            Dim identifier = syntax.Identifier
            Dim name = identifier.Identifier.ValueText
            If IdentifierComparison.Equals(reservedName, name) Then
                Dim location = identifier.GetLocation()
                diagnostics.Add(errorId, location)
                Return False
            End If
            Return True
        End Function

        ' TODO: Caller is O(n^2).
        Friend Shared Sub CheckParameterNameNotDuplicate(
                                                 params As ArrayBuilder(Of ParameterSymbol),
                                                 nParams As Integer,
                                                 syntax As ParameterSyntax,
                                                 parameter As ParameterSymbol,
                                                 diagnostics As BindingDiagnosticBag)
            Dim name = parameter.Name
            For i = 0 To nParams - 1
                If IdentifierComparison.Equals(params(i).Name, name) Then
                    ' "Parameter already declared with name '{0}'."
                    ReportDiagnostic(diagnostics, syntax.Identifier, ERRID.ERR_DuplicateParamName1, name)
                    Return
                End If
            Next
        End Sub

        Friend Delegate Function CheckParameterModifierDelegate(container As Symbol, token As SyntaxToken, flag As SourceParameterFlags, diagnostics As BindingDiagnosticBag) As SourceParameterFlags

        Public Sub DecodeParameterList(container As Symbol,
                                            isFromLambda As Boolean,
                                            modifiers As SourceMemberFlags,
                                            syntax As SeparatedSyntaxList(Of ParameterSyntax),
                                            params As ArrayBuilder(Of ParameterSymbol),
                                            checkModifier As CheckParameterModifierDelegate,
                                            diagBag As BindingDiagnosticBag)

            Dim count As Integer = syntax.Count
            Dim ordinal = params.Count
            Dim paramHasImplicitType = False, paramHasExplicitType = False
            Dim flagsOfPreviousParameters As SourceParameterFlags = Nothing
            Dim reportedError = False

            For i = 0 To count - 1
                Dim paramSyntax = syntax(i)
                Dim name = paramSyntax.Identifier.Identifier.ValueText
                Dim flags As SourceParameterFlags = Nothing

                flags = DecodeParameterModifiers(container, paramSyntax.Modifiers, checkModifier, diagBag)

                If (flagsOfPreviousParameters And SourceParameterFlags.Optional) = SourceParameterFlags.Optional Then
                    If (flags And SourceParameterFlags.ParamArray) = SourceParameterFlags.ParamArray AndAlso
                        Not reportedError Then

                        ' If one of the previous parameters is optional then report an error if paramarray is specified.
                        ReportDiagnostic(diagBag, paramSyntax.Identifier.Identifier, ERRID.ERR_ParamArrayWithOptArgs)
                        reportedError = True

                    ElseIf (flags And SourceParameterFlags.Optional) <> SourceParameterFlags.Optional AndAlso
                        Not reportedError Then

                        ' If one of the previous parameters is optional then report an error if this one isn't optional.
                        ReportDiagnostic(diagBag, paramSyntax.Identifier.Identifier, ERRID.ERR_ExpectedOptional)
                        reportedError = True

                    End If
                End If

                If (flagsOfPreviousParameters And SourceParameterFlags.ParamArray) <> 0 AndAlso
                    Not reportedError Then

                    ReportDiagnostic(diagBag, paramSyntax, ERRID.ERR_ParamArrayMustBeLast)
                    reportedError = True

                End If

                Dim newParam As ParameterSymbol

                If isFromLambda Then
                    newParam = UnboundLambdaParameterSymbol.CreateFromSyntax(paramSyntax, name, flags, ordinal, Me, diagBag)
                Else
                    newParam = SourceComplexParameterSymbol.CreateFromSyntax(container, paramSyntax, name, flags, ordinal, Me, checkModifier, diagBag)
                End If
                ordinal += 1

                If newParam.IsByRef AndAlso (modifiers And SourceMemberFlags.Async) = SourceMemberFlags.Async Then
                    ReportDiagnostic(diagBag, paramSyntax, ERRID.ERR_BadAsyncByRefParam)

                ElseIf newParam.IsByRef AndAlso (modifiers And SourceMemberFlags.Iterator) = SourceMemberFlags.Iterator Then
                    ReportDiagnostic(diagBag, paramSyntax, ERRID.ERR_BadIteratorByRefParam)

                Else
                    Dim paramType As TypeSymbol = newParam.Type

                    If paramType IsNot Nothing Then
                        If paramType.IsArrayType() Then
                            Dim restrictedType As TypeSymbol = Nothing
                            If paramType.IsRestrictedArrayType(restrictedType) Then
                                ReportDiagnostic(diagBag, paramSyntax.AsClause.Type, ERRID.ERR_RestrictedType1, restrictedType)
                            End If
                        ElseIf newParam.IsByRef Then
                            If paramType.IsRestrictedType Then
                                ReportDiagnostic(diagBag, paramSyntax.AsClause.Type, ERRID.ERR_RestrictedType1, paramType)
                            End If
                        ElseIf (modifiers And (SourceMemberFlags.Async Or SourceMemberFlags.Iterator)) <> 0 Then
                            If paramType.IsRestrictedType Then
                                ReportDiagnostic(diagBag, paramSyntax.AsClause.Type, ERRID.ERR_RestrictedResumableType1, paramType)
                            End If
                        End If
                    End If
                End If

                If Not isFromLambda AndAlso Not newParam.Type.IsErrorType() Then
                    AccessCheck.VerifyAccessExposureForParameterType(container, newParam.Name,
                                                                     If(paramSyntax.AsClause IsNot Nothing,
                                                                            paramSyntax.AsClause.Type,
                                                                            DirectCast(paramSyntax, VisualBasicSyntaxNode)),
                                                                     newParam.Type, diagBag)
                End If

                params.Add(newParam)

                If HasDefaultType(paramSyntax.Identifier, paramSyntax.AsClause) Then
                    paramHasImplicitType = True
                Else
                    paramHasExplicitType = True
                End If

                flagsOfPreviousParameters = flagsOfPreviousParameters Or flags
            Next

            ' Special rule: if any parameters have implicit type, all must have implicit type.
            ' Report the error on each parameter with implicit type.
            If paramHasImplicitType AndAlso paramHasExplicitType Then
                For i = 0 To count - 1
                    Dim paramSyntax = syntax(i)
                    If HasDefaultType(paramSyntax.Identifier, paramSyntax.AsClause) Then
                        ReportDiagnostic(diagBag, paramSyntax.Identifier, ERRID.ERR_ParamTypingInconsistency)
                    End If
                Next
            End If
        End Sub

        ' An array consisting of just the NotInheritable keyword.
        Private Shared ReadOnly s_notInheritableKeyword As SyntaxKind() = {SyntaxKind.NotInheritableKeyword}

        ''' <summary>
        ''' Modifier validation code shared between properties and methods.
        ''' </summary>
        Public Function ValidateSharedPropertyAndMethodModifiers(modifierList As SyntaxTokenList,
                                                                 memberModifiers As MemberModifiers,
                                                                 isProperty As Boolean,
                                                                 container As SourceMemberContainerTypeSymbol,
                                                                 diagBag As DiagnosticBag) As MemberModifiers
            Dim flags = memberModifiers.FoundFlags

            If (flags And SourceMemberFlags.Shared) <> 0 AndAlso (flags And SourceMemberFlags.InvalidIfShared) <> 0 Then
                ReportModifierError(modifierList, If(isProperty, ERRID.ERR_BadFlagsOnSharedProperty1, ERRID.ERR_BadFlagsOnSharedMeth1), diagBag, InvalidModifiersIfShared)
                flags = flags And Not SourceMemberFlags.InvalidIfShared
                memberModifiers = New MemberModifiers(flags, memberModifiers.ComputedFlags)
            End If

            Select Case container.TypeKind
                Case TypeKind.Module
                    ' Don't allow overloads in module
                    If (flags And SourceMemberFlags.Overloads) <> 0 Then
                        ReportModifierError(modifierList, ERRID.ERR_OverloadsModifierInModule, diagBag, SyntaxKind.OverloadsKeyword)

                        flags = flags And Not SourceMemberFlags.Overloads
                    End If

                    ' Members in module are implicitly Shared, and cannot be explicitly Shared.
                    If (flags And SourceMemberFlags.InvalidInModule) <> 0 Then
                        ReportModifierError(modifierList, If(isProperty, ERRID.ERR_BadFlagsOnStdModuleProperty1, ERRID.ERR_ModuleCantUseMethodSpecifier1), diagBag, InvalidModifiersInModule)
                        If (flags And SourceMemberFlags.Protected) <> 0 Then
                            flags = flags And Not SourceMemberFlags.Friend ' remove "Friend" if removing "Protected"
                        End If
                        flags = flags And Not SourceMemberFlags.InvalidInModule
                    End If

                    memberModifiers = New MemberModifiers(flags, memberModifiers.ComputedFlags Or SourceMemberFlags.Shared)

                Case TypeKind.Interface
                    If (flags And SourceMemberFlags.InvalidInInterface) <> 0 Then
                        ReportModifierError(modifierList, If(isProperty, ERRID.ERR_BadInterfacePropertyFlags1, ERRID.ERR_BadInterfaceMethodFlags1), diagBag, InvalidModifiersInInterface)
                        flags = flags And Not SourceMemberFlags.InvalidInInterface
                    End If

                    ' Interface members are always public and always implicitly MustOverride.
                    memberModifiers = New MemberModifiers(flags, memberModifiers.ComputedFlags Or SourceMemberFlags.MustOverride)

                Case TypeKind.Structure
                    If (flags And SourceMemberFlags.Protected) <> 0 AndAlso (flags And SourceMemberFlags.Overrides) = 0 Then
                        ReportModifierError(modifierList, If(isProperty, ERRID.ERR_StructCantUseVarSpecifier1, ERRID.ERR_StructureCantUseProtected), diagBag, SyntaxKind.ProtectedKeyword)

                        flags = flags And Not SourceMemberFlags.Protected
                        memberModifiers = New MemberModifiers(flags,
                                          (memberModifiers.ComputedFlags And Not SourceMemberFlags.AccessibilityMask) Or SourceMemberFlags.AccessibilityPrivate)
                    End If
                    If (flags And SourceMemberFlags.InvalidInNotInheritableClass) <> 0 Then
                        ReportModifierError(modifierList, ERRID.ERR_StructCantUseVarSpecifier1, diagBag,
                                            SyntaxKind.OverridableKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.MustOverrideKeyword)

                        flags = flags And Not SourceMemberFlags.InvalidInNotInheritableClass
                    End If

            End Select

            ' Don't allow Overridable, etc. on NotInheritable.
            If container.IsNotInheritable Then
                If (flags And SourceMemberFlags.InvalidInNotInheritableClass) <> 0 Then
                    ' Somewhat strangely, the old VB compiler has different behavior depending on whether the containing type DECLARATION
                    ' has NotInheritable, vs. any partial has NotInheritable (although they are semantically the same). If the containing declaration
                    ' does not have NotInheritable, then only MustOverride has an error reported for it, and the error has a different code.

                    Dim containingTypeBLock = GetContainingTypeBlock(modifierList.First())
                    If containingTypeBLock IsNot Nothing AndAlso FindFirstKeyword(containingTypeBLock.BlockStatement.Modifiers, s_notInheritableKeyword).Kind = SyntaxKind.None Then
                        ' Containing type block doesn't have a NotInheritable modifier on it. Must be from other partial declaration.

                        If (flags And SourceMemberFlags.InvalidInNotInheritableOtherPartialClass) <> 0 Then
                            ReportModifierError(modifierList, ERRID.ERR_MustOverOnNotInheritPartClsMem1, diagBag, InvalidModifiersInNotInheritableOtherPartialClass)
                            flags = flags And Not SourceMemberFlags.InvalidInNotInheritableOtherPartialClass
                        End If
                    Else
                        ReportModifierError(modifierList, ERRID.ERR_BadFlagsInNotInheritableClass1, diagBag, InvalidModifiersInNotInheritableClass)
                        flags = flags And Not SourceMemberFlags.InvalidInNotInheritableClass
                    End If

                    memberModifiers = New MemberModifiers(flags, memberModifiers.ComputedFlags)
                End If
            End If

            Return memberModifiers
        End Function

        Public Function ValidateEventModifiers(modifierList As SyntaxTokenList,
                                                memberModifiers As MemberModifiers,
                                                container As SourceMemberContainerTypeSymbol,
                                                diagBag As DiagnosticBag) As MemberModifiers

            Dim flags = memberModifiers.FoundFlags

            Select Case container.TypeKind
                Case TypeKind.Module
                    ' Members in module are implicitly Shared, and cannot be explicitly Shared.
                    If (flags And SourceMemberFlags.InvalidInModule) <> 0 Then
                        ReportModifierError(modifierList, ERRID.ERR_ModuleCantUseEventSpecifier1, diagBag, InvalidModifiersInModule)
                        If (flags And SourceMemberFlags.Protected) <> 0 Then
                            flags = flags And Not SourceMemberFlags.Friend ' remove "Friend" if removing "Protected"
                        End If
                        flags = flags And Not SourceMemberFlags.InvalidInModule
                    End If

                    memberModifiers = New MemberModifiers(flags, memberModifiers.ComputedFlags Or SourceMemberFlags.Shared)

                Case TypeKind.Interface
                    If (flags And SourceMemberFlags.InvalidInInterface) <> 0 Then
                        ReportModifierError(modifierList, ERRID.ERR_InterfaceCantUseEventSpecifier1, diagBag, InvalidModifiersInInterface)
                        flags = flags And Not SourceMemberFlags.InvalidInInterface
                    End If

                    ' Interface members are always public and always implicitly MustOverride.
                    memberModifiers = New MemberModifiers(flags, memberModifiers.ComputedFlags Or SourceMemberFlags.MustOverride)

                Case TypeKind.Structure
                    If (flags And SourceMemberFlags.Protected) <> 0 Then
                        ReportModifierError(modifierList, ERRID.ERR_StructureCantUseProtected, diagBag, SyntaxKind.ProtectedKeyword)

                        flags = flags And Not SourceMemberFlags.Protected
                        memberModifiers = New MemberModifiers(flags,
                                          (memberModifiers.ComputedFlags And Not SourceMemberFlags.AccessibilityMask) Or SourceMemberFlags.AccessibilityPrivate)
                    End If

            End Select

            Return memberModifiers
        End Function

        ' Get the containing type block containing a modifier token on a declared member, or Nothing if no such type block.
        Private Shared Function GetContainingTypeBlock(modifierToken As SyntaxToken) As TypeBlockSyntax
            Dim containingSyntax = modifierToken.Parent
            If TypeOf containingSyntax.Parent Is MethodBlockBaseSyntax OrElse TypeOf containingSyntax.Parent Is PropertyBlockSyntax Then
                containingSyntax = containingSyntax.Parent
            End If
            Return TryCast(containingSyntax.Parent, TypeBlockSyntax)
        End Function

        Public Enum ConstantContext
            [Default]
            ParameterDefaultValue
        End Enum

        ''' <summary>
        ''' This function checks if the given expression is a constant from a language point of view and returns 
        ''' constant value if it is. This is different from the fact that the bound node has a constant value. 
        ''' This method also adds the required diagnostics for non const values.
        ''' </summary>
        ''' <param name="boundExpression">The bound expression.</param>
        ''' <param name="diagnostics">The diagnostics.</param>
        ''' <returns>ConstantValue if the bound expression is compile time constant and can be used 
        ''' for const field/local initializations or enum member initializations. Nothing if not</returns>
        Public Function GetExpressionConstantValueIfAny(boundExpression As BoundExpression, diagnostics As BindingDiagnosticBag, context As ConstantContext) As ConstantValue
            Dim nonConstantDetected As Boolean = False
            Do
                If boundExpression.Kind = BoundKind.Local Then
                    Dim local = DirectCast(boundExpression, BoundLocal).LocalSymbol
                    If Not local.IsConst Then
                        ReportDiagnostic(diagnostics, boundExpression.Syntax, ERRID.ERR_RequiredConstExpr)
                        Return Nothing
                    End If

                    Return If(nonConstantDetected, Nothing, local.GetConstantValue(Me))
                End If

                ' Check that the expression is constant.
                If boundExpression.ConstantValueOpt IsNot Nothing Then
                    ' if no non constant node was found this if node is constant.
                    Return If(nonConstantDetected, Nothing, boundExpression.ConstantValueOpt)

                Else
                    Select Case boundExpression.Kind
                        Case BoundKind.DirectCast
                            Dim conv = DirectCast(boundExpression, BoundDirectCast)
                            Dim result = CheckConversionForConstantExpression(conv, conv.Operand, diagnostics, context)
                            Return If(nonConstantDetected, Nothing, result)

                        Case BoundKind.TryCast
                            Dim conv = DirectCast(boundExpression, BoundTryCast)
                            Dim result = CheckConversionForConstantExpression(conv, conv.Operand, diagnostics, context)
                            Return If(nonConstantDetected, Nothing, result)

                        Case BoundKind.Conversion
                            Dim conv = DirectCast(boundExpression, BoundConversion)
                            Dim result = CheckConversionForConstantExpression(conv, conv.Operand, diagnostics, context)
                            Return If(nonConstantDetected, Nothing, result)

                        Case BoundKind.BinaryOperator
                            ' If we ever got to a binary operator it means there is no constant value,
                            ' but we want to keep the analysis going to properly report errors

                            Dim binaryOperator = DirectCast(boundExpression, BoundBinaryOperator)
                            ' the right side is expected to be shorter for binary operations, so we use
                            ' recursion for this side.
                            GetExpressionConstantValueIfAny(binaryOperator.Right, diagnostics, context)
                            nonConstantDetected = True
                            boundExpression = binaryOperator.Left

                        Case BoundKind.UnaryOperator
                            boundExpression = DirectCast(boundExpression, BoundUnaryOperator).Operand

                        Case BoundKind.Parenthesized
                            boundExpression = DirectCast(boundExpression, BoundParenthesized).Expression

                        Case BoundKind.BadExpression
                            Return Nothing

                        Case Else
                            ReportDiagnostic(diagnostics, boundExpression.Syntax, ERRID.ERR_RequiredConstExpr)
                            Return Nothing
                    End Select
                End If
            Loop

            Return Nothing
        End Function

        Private Shared Function IsNothingLiteralAllowedForAType(type As TypeSymbol) As Boolean
            If type.IsReferenceType Then
                Return True
            End If

            If type.IsEnumType Then
                Return True
            End If

            Select Case type.SpecialType
                Case SpecialType.System_Boolean,
                     SpecialType.System_Byte,
                     SpecialType.System_SByte,
                     SpecialType.System_Int16,
                     SpecialType.System_UInt16,
                     SpecialType.System_Int32,
                     SpecialType.System_UInt32,
                     SpecialType.System_Int64,
                     SpecialType.System_UInt64,
                     SpecialType.System_Single,
                     SpecialType.System_Double,
                     SpecialType.System_Char
                    Return True
            End Select
            Return False
        End Function

        Private Function CheckConversionForConstantExpression(conv As BoundExpression, operand As BoundExpression, diagnostics As BindingDiagnosticBag, context As ConstantContext) As ConstantValue
            If conv.HasErrors Then
                Return Nothing
            End If

            Dim conversionType As TypeSymbol = conv.Type
            Dim operandType As TypeSymbol = operand.Type

            ' First, special handling for the case when operand is a Nothing Literal
            If operand.IsNothingLiteral Then
                If context = ConstantContext.Default Then
                    ' In default context, Nothing literal can only be converted to either a reference type 
                    ' or an intrinsic value type allowing constant values, like int32, etc...
                    If Not IsNothingLiteralAllowedForAType(conversionType) Then
                        ReportDiagnostic(diagnostics,
                                         operand.Syntax,
                                         ERRID.ERR_RequiredConstConversion2,
                                         If(operandType, GetSpecialType(SpecialType.System_Object, operand.Syntax, diagnostics)),
                                         conversionType)
                        Return Nothing
                    End If

                Else
                    ' In ParameterDefaultValue constant context 'Nothing' literal can be 
                    ' converted to any type meaning the type's default value; 
                    Debug.Assert(context = ConstantContext.ParameterDefaultValue)
                End If

                Return operand.ConstantValueOpt
            End If

            ' Guess the constant value of the operand
            Dim nestedConstValue As ConstantValue = GetExpressionConstantValueIfAny(operand, diagnostics, context)
            If nestedConstValue Is Nothing Then
                ' Error should already be generated
                Return Nothing
            End If

            Debug.Assert(conversionType IsNot Nothing)
            Debug.Assert(operandType IsNot Nothing)

            If conversionType.IsObjectType Then
                ' Nothing constants of reference type can always be converted to System.Object
                If operandType.IsReferenceType AndAlso nestedConstValue.IsNothing Then
                    Return nestedConstValue
                End If

                ' In ParameterDefaultValue context we also allow constant of types DateTime, Decimal 
                ' and others mentioned as supported by IsNothingLiteralAllowedForAType() to be converted 
                ' to System.Object type; these constants will be emitted as constant values for appropriate 
                ' parameters and when used converted to target type if needed
                If context = ConstantContext.ParameterDefaultValue AndAlso
                        (IsNothingLiteralAllowedForAType(operandType) OrElse operandType.IsDateTimeType OrElse operandType.IsDecimalType) Then
                    Return nestedConstValue
                End If

                ReportDiagnostic(diagnostics,
                                 operand.Syntax,
                                 ERRID.ERR_RequiredConstConversion2,
                                 operandType,
                                 conversionType)
                Return Nothing
            End If

            If Not nestedConstValue.IsNothing Then
                ' Conversion of a non-nothing constant value to anything but System.Object is bad
                ' Note that all valid intrinsic conversions should have been folded and never reach this point

                ' In ParameterDefaultValue we also allow conversion of T --> S?
                If context = ConstantContext.ParameterDefaultValue AndAlso conversionType.IsNullableType Then
                    If IsSameTypeIgnoringAll(conversionType.GetNullableUnderlyingType, operandType) Then
                        ' A trivial case: T --> T?
                        Return nestedConstValue
                    Else
                        ' Let's convert to the underlying type of the Nullable.
                        ' All diagnostics about this conversion have already been reported, so we can treat it as an explicit conversion and ignore any errors/warnings.
                        Dim conversionToUnderlying As BoundExpression = ApplyConversion(operand.Syntax, conversionType.GetNullableUnderlyingType(), operand, isExplicit:=True, diagnostics:=BindingDiagnosticBag.Discarded)

                        nestedConstValue = conversionToUnderlying.ConstantValueOpt
                        If nestedConstValue IsNot Nothing Then
                            Return nestedConstValue
                        End If
                    End If
                End If

                ReportDiagnostic(diagnostics,
                                 operand.Syntax,
                                 ERRID.ERR_RequiredConstConversion2,
                                 operandType,
                                 conversionType)
                Return Nothing
            End If

            ' We have a 'Nothing' constant value which was converted to some 
            ' type by nested conversion(s) and now has type of the argument

            ' No actual conversion is done
            If IsSameTypeIgnoringAll(operandType, conversionType) Then
                Return nestedConstValue
            End If

            ' It is OK to convert Nothing literal of one reference type to another 
            ' reference type or (only in default parameter value context) Nothing 
            ' literal of reference type to a value type,
            ' if the conversion is not correct, errors should have been reported by now
            If operandType.IsReferenceType AndAlso
                    (conversionType.IsReferenceType OrElse context = ConstantContext.ParameterDefaultValue) Then
                Return nestedConstValue
            End If

            ' In ParameterDefaultValue we also allow conversion of 'Nothing' constants to nullable 
            If context = ConstantContext.ParameterDefaultValue Then
                If conversionType.IsNullableType Then
                    Return nestedConstValue
                End If
            End If

            ReportDiagnostic(diagnostics,
                             operand.Syntax,
                             ERRID.ERR_RequiredConstConversion2,
                             operandType,
                             conversionType)
            Return Nothing
        End Function

        ''' <summary>isWinMd says whether to mangle the name for winmdobj output. See the param tag for details.</summary>
        ''' <param name="isWinMd">isWinMd is only necessary for set properties, so any MethodKind which is definitely not
        ''' a set property can safely set this value to False.</param>
        Friend Shared Function GetAccessorName(name As String, kind As MethodKind, isWinMd As Boolean) As String
            Dim prefix As String
            Select Case kind
                Case MethodKind.PropertyGet
                    prefix = StringConstants.PropertyGetPrefix
                Case MethodKind.PropertySet
                    If isWinMd Then
                        prefix = StringConstants.WinMdPropertySetPrefix
                    Else
                        prefix = StringConstants.PropertySetPrefix
                    End If
                Case MethodKind.EventAdd
                    prefix = StringConstants.EventAddPrefix
                Case MethodKind.EventRemove
                    prefix = StringConstants.EventRemovePrefix
                Case MethodKind.EventRaise
                    prefix = StringConstants.EventRaisePrefix
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select

            Return prefix & name
        End Function
    End Class

    ''' <summary>
    ''' Holds information about a member in a compact form. Used for all non-type members for simplicity
    ''' </summary>
    ''' <remarks></remarks>
    <Flags()>
    Friend Enum SourceMemberFlags
        None = 0
        ' These are the actual accessibility level
        ' Bits 0,1,2
        AccessibilityPrivate = CUShort(Accessibility.Private)
        AccessibilityProtected = CUShort(Accessibility.Protected)
        AccessibilityFriend = CUShort(Accessibility.Friend)
        AccessibilityProtectedFriend = CUShort(Accessibility.ProtectedOrFriend)
        AccessibilityPrivateProtected = CUShort(Accessibility.ProtectedAndFriend)
        AccessibilityPublic = CUShort(Accessibility.Public)
        AccessibilityMask = &H7

        ' DeclarationModifierFlags
        ' Bits 3,4,5,...,27

        [Private] = CUInt(DeclarationModifiers.Private) << DeclarationModifierFlagShift
        [Protected] = CUInt(DeclarationModifiers.Protected) << DeclarationModifierFlagShift
        [Friend] = CUInt(DeclarationModifiers.Friend) << DeclarationModifierFlagShift
        [Public] = CUInt(DeclarationModifiers.Public) << DeclarationModifierFlagShift
        AllAccessibilityModifiers = [Private] Or [Friend] Or [Protected] Or [Public]

        [Shared] = CUInt(DeclarationModifiers.Shared) << DeclarationModifierFlagShift

        [ReadOnly] = CUInt(DeclarationModifiers.ReadOnly) << DeclarationModifierFlagShift
        [WriteOnly] = CUInt(DeclarationModifiers.WriteOnly) << DeclarationModifierFlagShift
        AllWriteabilityModifiers = [ReadOnly] Or [WriteOnly]

        [Overrides] = CUInt(DeclarationModifiers.Overrides) << DeclarationModifierFlagShift

        [Overridable] = CUInt(DeclarationModifiers.Overridable) << DeclarationModifierFlagShift
        [MustOverride] = CUInt(DeclarationModifiers.MustOverride) << DeclarationModifierFlagShift
        [NotOverridable] = CUInt(DeclarationModifiers.NotOverridable) << DeclarationModifierFlagShift
        AllOverrideModifiers = [Overridable] Or [MustOverride] Or [NotOverridable]

        PrivateOverridableModifiers = [Overridable] Or [Private]
        PrivateMustOverrideModifiers = [MustOverride] Or [Private]
        PrivateNotOverridableModifiers = [NotOverridable] Or [Private]

        [Overloads] = CUInt(DeclarationModifiers.Overloads) << DeclarationModifierFlagShift
        [Shadows] = CUInt(DeclarationModifiers.Shadows) << DeclarationModifierFlagShift
        AllShadowingModifiers = [Overloads] Or [Shadows]

        ShadowsAndOverrides = [Overrides] Or [Shadows]

        [Default] = CUInt(DeclarationModifiers.Default) << DeclarationModifierFlagShift
        [WithEvents] = CUInt(DeclarationModifiers.WithEvents) << DeclarationModifierFlagShift

        [Widening] = CUInt(DeclarationModifiers.Widening) << DeclarationModifierFlagShift
        [Narrowing] = CUInt(DeclarationModifiers.Narrowing) << DeclarationModifierFlagShift
        AllConversionModifiers = [Widening] Or [Narrowing]

        ' These should be kept in sync with the corresponding arrays in InvalidModifiers below.
        InvalidInNotInheritableClass = [Overridable] Or [NotOverridable] Or [MustOverride]
        InvalidInNotInheritableOtherPartialClass = [MustOverride] ' invalid if containing declaration doesn't have NotInheritable, but other partial does.
        InvalidInModule = [Protected] Or [Shared] Or [Default] Or [MustOverride] Or [Overridable] Or [Shadows] Or [Overrides] Or [NotOverridable]
        InvalidInInterface = AllAccessibilityModifiers Or [Shared] Or [Overrides] Or AllOverrideModifiers Or [Dim] Or [Const] Or
                             [Static] Or [WithEvents] Or AllConversionModifiers Or [Partial] Or [MustInherit] Or [NotInheritable] Or
                             Async Or Iterator
        InvalidIfShared = [Overrides] Or AllOverrideModifiers Or [Default]
        InvalidIfDefault = [Private]

        [Partial] = CUInt(DeclarationModifiers.Partial) << DeclarationModifierFlagShift
        [MustInherit] = CUInt(DeclarationModifiers.MustInherit) << DeclarationModifierFlagShift
        [NotInheritable] = CUInt(DeclarationModifiers.NotInheritable) << DeclarationModifierFlagShift
        TypeInheritModifiers = [MustInherit] Or [NotInheritable]

        Async = CUInt(DeclarationModifiers.Async) << DeclarationModifierFlagShift
        Iterator = CUInt(DeclarationModifiers.Iterator) << DeclarationModifierFlagShift

        [Dim] = CUInt(DeclarationModifiers.Dim) << DeclarationModifierFlagShift
        [Const] = CUInt(DeclarationModifiers.Const) << DeclarationModifierFlagShift
        [Static] = CUInt(DeclarationModifiers.Static) << DeclarationModifierFlagShift

        DeclarationModifierFlagMask = &HFFFFFF8
        DeclarationModifierFlagShift = 3

        ' Bits 25 and above are used for different things depending on the member type.
        ' 25 - [Dim]
        ' 26 - [Const]
        ' 27 - [Static]

        ' Fields only: Indicates that this const field has an inferred type.
        InferredFieldType = 1 << 30

        ' Fields and properties only: Indicates that this is the first field (or property representing a WithEvents field) of a particular type.
        ' In "Dim x, y, z As Integer, w As String, a, b as Decimal", set for "x", "w", and "a"
        FirstFieldDeclarationOfType = 1 << 31

        ' Source Methods only: Indicates this method has a void return type / is a Sub.
        MethodIsSub = 1 << 25

        ' Source methods only: Indicates that method syntax has "Handles"
        MethodHandlesEvents = 1 << 26

        ' Method symbols only: Bits 27,28,29,30,31
        MethodKindOrdinary = CUInt(MethodKind.Ordinary) << MethodKindShift
        MethodKindConstructor = CUInt(MethodKind.Constructor) << MethodKindShift
        MethodKindSharedConstructor = CUInt(MethodKind.SharedConstructor) << MethodKindShift
        MethodKindDelegateInvoke = CUInt(MethodKind.DelegateInvoke) << MethodKindShift
        MethodKindOperator = CUInt(MethodKind.UserDefinedOperator) << MethodKindShift
        MethodKindConversion = CUInt(MethodKind.Conversion) << MethodKindShift
        MethodKindPropertyGet = CUInt(MethodKind.PropertyGet) << MethodKindShift
        MethodKindPropertySet = CUInt(MethodKind.PropertySet) << MethodKindShift
        MethodKindEventAdd = CUInt(MethodKind.EventAdd) << MethodKindShift
        MethodKindEventRemove = CUInt(MethodKind.EventRemove) << MethodKindShift
        MethodKindEventRaise = CUInt(MethodKind.EventRaise) << MethodKindShift
        MethodKindDeclare = CUInt(MethodKind.DeclareMethod) << MethodKindShift

        MethodKindMask = &H1F
        MethodKindShift = 27
    End Enum

    Friend Module SourceMemberFlagsExtensions
        <Extension>
        Friend Function ToMethodKind(flags As SourceMemberFlags) As MethodKind
            Return CType((flags >> SourceMemberFlags.MethodKindShift) And SourceMemberFlags.MethodKindMask, MethodKind)
        End Function
    End Module

    ' These should be kept in sync with the corresponding bit fields in SourceMemberFlags above.
    Friend Module InvalidModifiers

        Public InvalidModifiersInNotInheritableClass() As SyntaxKind =
            {
                SyntaxKind.OverridableKeyword,
                SyntaxKind.NotOverridableKeyword,
                SyntaxKind.MustOverrideKeyword
            }

        Public InvalidModifiersInNotInheritableOtherPartialClass() As SyntaxKind =
            {
                SyntaxKind.MustOverrideKeyword
            }

        Public InvalidModifiersInModule() As SyntaxKind =
            {
                SyntaxKind.SharedKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.DefaultKeyword,
                SyntaxKind.MustOverrideKeyword,
                SyntaxKind.OverridableKeyword,
                SyntaxKind.ShadowsKeyword,
                SyntaxKind.OverridesKeyword,
                SyntaxKind.NotOverridableKeyword
            }

        Public InvalidModifiersInInterface() As SyntaxKind =
            {
                SyntaxKind.PublicKeyword,
                SyntaxKind.PrivateKeyword,
                SyntaxKind.ProtectedKeyword,
                SyntaxKind.FriendKeyword,
                SyntaxKind.StaticKeyword,
                SyntaxKind.SharedKeyword,
                SyntaxKind.MustInheritKeyword,
                SyntaxKind.NotInheritableKeyword,
                SyntaxKind.OverridesKeyword,
                SyntaxKind.PartialKeyword,
                SyntaxKind.NotOverridableKeyword,
                SyntaxKind.OverridableKeyword,
                SyntaxKind.MustOverrideKeyword,
                SyntaxKind.DimKeyword,
                SyntaxKind.ConstKeyword,
                SyntaxKind.WithEventsKeyword,
                SyntaxKind.WideningKeyword,
                SyntaxKind.NarrowingKeyword,
                SyntaxKind.CustomKeyword,
                SyntaxKind.AsyncKeyword,
                SyntaxKind.IteratorKeyword
            }

        Public InvalidModifiersIfShared() As SyntaxKind =
            {
                SyntaxKind.OverridesKeyword,
                SyntaxKind.OverridableKeyword,
                SyntaxKind.MustOverrideKeyword,
                SyntaxKind.NotOverridableKeyword,
                SyntaxKind.DefaultKeyword
            }

        Public InvalidModifiersIfDefault() As SyntaxKind =
            {
                SyntaxKind.PrivateKeyword
            }

        Public InvalidAsyncIterator() As SyntaxKind =
            {
                SyntaxKind.AsyncKeyword,
                SyntaxKind.IteratorKeyword
            }
    End Module

    Friend Structure MemberModifiers
        ''' <summary>
        ''' These are the flags that are found in the syntax.  They must correspond to the modifiers list.
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _foundFlags As SourceMemberFlags
        ''' <summary>
        ''' These are flags that are implied or computed
        ''' </summary>
        ''' <remarks></remarks>
        Private ReadOnly _computedFlags As SourceMemberFlags

        Public Sub New(foundFlags As SourceMemberFlags, computedFlags As SourceMemberFlags)
            _foundFlags = foundFlags
            _computedFlags = computedFlags
        End Sub

        Public ReadOnly Property FoundFlags As SourceMemberFlags
            Get
                Return _foundFlags
            End Get
        End Property

        Public ReadOnly Property ComputedFlags As SourceMemberFlags
            Get
                Return _computedFlags
            End Get
        End Property

        Public ReadOnly Property AllFlags As SourceMemberFlags
            Get
                Return _foundFlags Or _computedFlags
            End Get
        End Property

    End Structure
End Namespace
