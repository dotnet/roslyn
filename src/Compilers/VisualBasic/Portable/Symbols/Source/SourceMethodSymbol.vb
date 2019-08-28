' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Reflection
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Microsoft.Cci
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports CallingConvention = Microsoft.Cci.CallingConvention ' to resolve ambiguity with System.Runtime.InteropServices.CallingConvention

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Base class for method symbols that are associated with some syntax and can receive custom attributes (directly or indirectly via another source symbol).
    ''' </summary>
    Friend MustInherit Class SourceMethodSymbol
        Inherits MethodSymbol
        Implements IAttributeTargetSymbol

        ' Flags about the method
        Protected ReadOnly m_flags As SourceMemberFlags

        ' Containing symbol
        Protected ReadOnly m_containingType As NamedTypeSymbol

        ' Me parameter.
        Private _lazyMeParameter As ParameterSymbol

        ' TODO (tomat): should be private
        ' Attributes on method. Set once after construction. IsNull means not set.  
        Protected m_lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ' TODO (tomat): should be private
        ' Return type attributes. IsNull means not set. 
        Protected m_lazyReturnTypeCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData)

        ' The syntax references for the primary (non-partial) declarations.
        ' Nothing if there are only partial declarations.
        Protected ReadOnly m_syntaxReferenceOpt As SyntaxReference

        ' Location(s)
        Private _lazyLocations As ImmutableArray(Of Location)

        Private _lazyDocComment As String
        Private _lazyExpandedDocComment As String

        'Nothing if diags have never been computed. Initial binding diagnostics
        'are stashed here to optimize API usage patterns
        'where method body diagnostics are requested multiple times.
        Private _cachedDiagnostics As ImmutableArray(Of Diagnostic)

        Protected Sub New(containingType As NamedTypeSymbol,
                          flags As SourceMemberFlags,
                          syntaxRef As SyntaxReference,
                          Optional locations As ImmutableArray(Of Location) = Nothing)

            Debug.Assert(TypeOf containingType Is SourceMemberContainerTypeSymbol OrElse
                         TypeOf containingType Is SynthesizedEventDelegateSymbol)

            m_containingType = containingType
            m_flags = flags
            m_syntaxReferenceOpt = syntaxRef

            ' calculated lazily if not initialized
            _lazyLocations = locations
        End Sub

#Region "Factories"
        ' Create a regular method, with the given, name, declarator, and declaration syntax.
        Friend Shared Function CreateRegularMethod(container As SourceMemberContainerTypeSymbol,
                                                   syntax As MethodStatementSyntax,
                                                   binder As Binder,
                                                   diagBag As DiagnosticBag) As SourceMethodSymbol
            ' Flags
            Dim methodModifiers = DecodeMethodModifiers(syntax.Modifiers, container, binder, diagBag)
            Dim flags = methodModifiers.AllFlags Or SourceMemberFlags.MethodKindOrdinary
            If syntax.Kind = SyntaxKind.SubStatement Then
                flags = flags Or SourceMemberFlags.MethodIsSub
            End If

            If syntax.HandlesClause IsNot Nothing Then
                flags = flags Or SourceMemberFlags.MethodHandlesEvents
            End If

            'Name
            Dim name As String = syntax.Identifier.ValueText

            Dim handledEvents As ImmutableArray(Of HandledEvent)

            If syntax.HandlesClause IsNot Nothing Then
                If container.TypeKind = TypeKind.Structure Then
                    ' Structures cannot handle events
                    binder.ReportDiagnostic(diagBag, syntax.Identifier, ERRID.ERR_StructsCannotHandleEvents)

                ElseIf container.IsInterface Then
                    ' Methods in interfaces cannot have an handles clause
                    binder.ReportDiagnostic(diagBag, syntax.HandlesClause, ERRID.ERR_BadInterfaceMethodFlags1, syntax.HandlesClause.HandlesKeyword.ToString)

                ElseIf GetTypeParameterListSyntax(syntax) IsNot Nothing Then
                    ' Generic methods cannot have 'handles' clause
                    binder.ReportDiagnostic(diagBag, syntax.Identifier, ERRID.ERR_HandlesInvalidOnGenericMethod)
                End If

                ' Operators methods cannot have Handles regardless of container. 
                'That error (ERR_InvalidHandles) is reported in parser.

                ' handled events will be lazily constructed:
                handledEvents = Nothing
            Else
                ' there is no handles clause, so it will be Empty anyways
                handledEvents = ImmutableArray(Of HandledEvent).Empty
            End If

            Dim arity = If(syntax.TypeParameterList Is Nothing, 0, syntax.TypeParameterList.Parameters.Count)
            Dim methodSym As New SourceMemberMethodSymbol(
                container, name, flags, binder, syntax, arity, handledEvents)

            If methodSym.IsPartial AndAlso methodSym.IsSub Then
                If methodSym.IsAsync Then
                    binder.ReportDiagnostic(diagBag, syntax.Identifier, ERRID.ERR_PartialMethodsMustNotBeAsync1, name)
                End If

                ReportPartialMethodErrors(syntax.Modifiers, binder, diagBag)
            End If

            Return methodSym
        End Function

        Friend Shared Function GetTypeParameterListSyntax(methodSyntax As MethodBaseSyntax) As TypeParameterListSyntax
            If methodSyntax.Kind = SyntaxKind.SubStatement OrElse methodSyntax.Kind = SyntaxKind.FunctionStatement Then
                Return DirectCast(methodSyntax, MethodStatementSyntax).TypeParameterList
            End If

            Return Nothing
        End Function


        Private Shared Sub ReportPartialMethodErrors(modifiers As SyntaxTokenList, binder As Binder, diagBag As DiagnosticBag)
            ' Handle partial methods related errors
            Dim reportPartialMethodsMustBePrivate As Boolean = True
            Dim partialToken As SyntaxToken = Nothing

            Dim modifierList = modifiers.ToList()

            For index = 0 To modifierList.Count - 1
                Dim token As SyntaxToken = modifierList(index)

                Select Case token.Kind
                    Case SyntaxKind.PublicKeyword,
                         SyntaxKind.MustOverrideKeyword,
                         SyntaxKind.NotOverridableKeyword,
                         SyntaxKind.OverridableKeyword,
                         SyntaxKind.OverridesKeyword,
                         SyntaxKind.MustInheritKeyword

lReportErrorOnSingleToken:
                        ' Report [Partial methods must be declared 'Private' instead of '...']
                        binder.ReportDiagnostic(diagBag, token,
                                                ERRID.ERR_OnlyPrivatePartialMethods1,
                                                SyntaxFacts.GetText(token.Kind))
                        reportPartialMethodsMustBePrivate = False

                    Case SyntaxKind.ProtectedKeyword

                        ' Check for 'Protected Friend'
                        If index >= modifierList.Count - 1 OrElse modifierList(index + 1).Kind <> SyntaxKind.FriendKeyword Then
                            GoTo lReportErrorOnSingleToken
                        End If

lReportErrorOnTwoTokens:
                        index += 1
                        Dim nextToken As SyntaxToken = modifierList(index)
                        Dim startLoc As Integer = Math.Min(token.SpanStart, nextToken.SpanStart)
                        Dim endLoc As Integer = Math.Max(token.Span.End, nextToken.Span.End)
                        Dim location = binder.SyntaxTree.GetLocation(New TextSpan(startLoc, endLoc - startLoc))

                        ' Report [Partial methods must be declared 'Private' instead of '...']
                        binder.ReportDiagnostic(diagBag, location,
                                                ERRID.ERR_OnlyPrivatePartialMethods1,
                                                token.Kind.GetText() & " " & nextToken.Kind.GetText())

                        reportPartialMethodsMustBePrivate = False

                    Case SyntaxKind.FriendKeyword

                        ' Check for 'Friend Protected'
                        If index >= modifierList.Count - 1 OrElse modifierList(index + 1).Kind <> SyntaxKind.ProtectedKeyword Then
                            GoTo lReportErrorOnSingleToken
                        End If

                        GoTo lReportErrorOnTwoTokens

                    Case SyntaxKind.PartialKeyword
                        partialToken = token

                    Case SyntaxKind.PrivateKeyword
                        reportPartialMethodsMustBePrivate = False

                End Select
            Next

            If reportPartialMethodsMustBePrivate Then
                ' Report [Partial methods must be declared 'Private']
                Debug.Assert(partialToken.Kind = SyntaxKind.PartialKeyword)
                binder.ReportDiagnostic(diagBag, partialToken, ERRID.ERR_PartialMethodsMustBePrivate)
            End If
        End Sub

        ''' <summary>
        ''' Creates a method symbol for Declare Sub or Function.
        ''' </summary>
        Friend Shared Function CreateDeclareMethod(container As SourceMemberContainerTypeSymbol,
                                                   syntax As DeclareStatementSyntax,
                                                   binder As Binder,
                                                   diagBag As DiagnosticBag) As SourceMethodSymbol

            Dim methodModifiers = binder.DecodeModifiers(
                syntax.Modifiers,
                SourceMemberFlags.AllAccessibilityModifiers Or SourceMemberFlags.Overloads Or SourceMemberFlags.Shadows,
                ERRID.ERR_BadDeclareFlags1,
                Accessibility.Public,
                diagBag)

            ' modifiers: Protected and Overloads in Modules and Structures:
            If container.TypeKind = TypeKind.Module Then
                If (methodModifiers.FoundFlags And SourceMemberFlags.Overloads) <> 0 Then
                    Dim keyword = syntax.Modifiers.First(Function(m) m.Kind = SyntaxKind.OverloadsKeyword)
                    diagBag.Add(ERRID.ERR_OverloadsModifierInModule, keyword.GetLocation(), keyword.ValueText)
                ElseIf (methodModifiers.FoundFlags And SourceMemberFlags.Protected) <> 0 Then
                    Dim keyword = syntax.Modifiers.First(Function(m) m.Kind = SyntaxKind.ProtectedKeyword)
                    diagBag.Add(ERRID.ERR_ModuleCantUseDLLDeclareSpecifier1, keyword.GetLocation(), keyword.ValueText)
                End If
            ElseIf container.TypeKind = TypeKind.Structure Then
                If (methodModifiers.FoundFlags And SourceMemberFlags.Protected) <> 0 Then
                    Dim keyword = syntax.Modifiers.First(Function(m) m.Kind = SyntaxKind.ProtectedKeyword)
                    diagBag.Add(ERRID.ERR_StructCantUseDLLDeclareSpecifier1, keyword.GetLocation(), keyword.ValueText)
                End If
            End If

            ' not allowed in generic context
            If container IsNot Nothing AndAlso container.IsGenericType Then
                diagBag.Add(ERRID.ERR_DeclaresCantBeInGeneric, syntax.Identifier.GetLocation())
            End If

            Dim flags = methodModifiers.AllFlags Or
                        SourceMemberFlags.MethodKindDeclare Or
                        SourceMemberFlags.Shared

            If syntax.Kind = SyntaxKind.DeclareSubStatement Then
                flags = flags Or SourceMemberFlags.MethodIsSub
            End If

            Dim name As String = syntax.Identifier.ValueText

            ' module name
            Dim moduleName As String = syntax.LibraryName.Token.ValueText
            If String.IsNullOrEmpty(moduleName) AndAlso Not syntax.LibraryName.IsMissing Then
                diagBag.Add(ERRID.ERR_BadAttribute1, syntax.LibraryName.GetLocation(), name)
                moduleName = Nothing
            End If

            ' entry point name
            Dim entryPointName As String
            If syntax.AliasName IsNot Nothing Then
                entryPointName = syntax.AliasName.Token.ValueText
                If String.IsNullOrEmpty(entryPointName) Then
                    diagBag.Add(ERRID.ERR_BadAttribute1, syntax.LibraryName.GetLocation(), name)
                    entryPointName = Nothing
                End If
            Else
                ' If alias syntax not specified use Nothing - the emitter will fill in the metadata method name and 
                ' the users can determine whether or not it was specified.
                entryPointName = Nothing
            End If

            Dim importData = New DllImportData(moduleName, entryPointName, GetPInvokeAttributes(syntax))
            Return New SourceDeclareMethodSymbol(container, name, flags, binder, syntax, importData)
        End Function

        Private Shared Function GetPInvokeAttributes(syntax As DeclareStatementSyntax) As MethodImportAttributes
            Dim result As MethodImportAttributes
            Select Case syntax.CharsetKeyword.Kind
                Case SyntaxKind.None, SyntaxKind.AnsiKeyword
                    result = MethodImportAttributes.CharSetAnsi Or MethodImportAttributes.ExactSpelling

                Case SyntaxKind.UnicodeKeyword
                    result = MethodImportAttributes.CharSetUnicode Or MethodImportAttributes.ExactSpelling

                Case SyntaxKind.AutoKeyword
                    result = MethodImportAttributes.CharSetAuto
            End Select

            Return result Or MethodImportAttributes.CallingConventionWinApi Or MethodImportAttributes.SetLastError
        End Function

        Friend Shared Function CreateOperator(
            container As SourceMemberContainerTypeSymbol,
            syntax As OperatorStatementSyntax,
            binder As Binder,
            diagBag As DiagnosticBag
        ) As SourceMethodSymbol

            ' Flags
            Dim methodModifiers = DecodeOperatorModifiers(syntax, binder, diagBag)
            Dim flags = methodModifiers.AllFlags

            Debug.Assert((flags And SourceMemberFlags.AccessibilityPublic) <> 0)
            Debug.Assert((flags And SourceMemberFlags.Shared) <> 0)

            'Name
            Dim name As String = GetMemberNameFromSyntax(syntax)

            Debug.Assert(name.Equals(WellKnownMemberNames.ImplicitConversionName) = ((flags And SourceMemberFlags.Widening) <> 0))
            Debug.Assert(name.Equals(WellKnownMemberNames.ExplicitConversionName) = ((flags And SourceMemberFlags.Narrowing) <> 0))

            Dim paramCountMismatchERRID As ERRID

            Select Case syntax.OperatorToken.Kind
                Case SyntaxKind.NotKeyword, SyntaxKind.IsTrueKeyword, SyntaxKind.IsFalseKeyword,
                     SyntaxKind.CTypeKeyword
                    paramCountMismatchERRID = ERRID.ERR_OneParameterRequired1

                Case SyntaxKind.PlusToken, SyntaxKind.MinusToken
                    paramCountMismatchERRID = ERRID.ERR_OneOrTwoParametersRequired1

                Case SyntaxKind.AsteriskToken, SyntaxKind.SlashToken, SyntaxKind.BackslashToken, SyntaxKind.ModKeyword, SyntaxKind.CaretToken,
                     SyntaxKind.EqualsToken, SyntaxKind.LessThanGreaterThanToken, SyntaxKind.LessThanToken, SyntaxKind.GreaterThanToken,
                     SyntaxKind.LessThanEqualsToken, SyntaxKind.GreaterThanEqualsToken, SyntaxKind.LikeKeyword,
                     SyntaxKind.AmpersandToken,
                     SyntaxKind.AndKeyword, SyntaxKind.OrKeyword, SyntaxKind.XorKeyword,
                     SyntaxKind.LessThanLessThanToken, SyntaxKind.GreaterThanGreaterThanToken
                    paramCountMismatchERRID = ERRID.ERR_TwoParametersRequired1

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(syntax.OperatorToken.Kind)
            End Select

            Select Case paramCountMismatchERRID
                Case ERRID.ERR_OneParameterRequired1
                    Debug.Assert(OverloadResolution.GetOperatorInfo(name).ParamCount = 1)
                    If syntax.ParameterList.Parameters.Count = 1 Then
                        paramCountMismatchERRID = 0
                    End If
                Case ERRID.ERR_TwoParametersRequired1
                    Debug.Assert(OverloadResolution.GetOperatorInfo(name).ParamCount = 2)
                    If syntax.ParameterList.Parameters.Count = 2 Then
                        paramCountMismatchERRID = 0
                    End If

                Case ERRID.ERR_OneOrTwoParametersRequired1
                    If syntax.ParameterList.Parameters.Count = 1 OrElse 2 = syntax.ParameterList.Parameters.Count Then
                        Debug.Assert(OverloadResolution.GetOperatorInfo(name).ParamCount = syntax.ParameterList.Parameters.Count)
                        paramCountMismatchERRID = 0
                    End If

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(paramCountMismatchERRID)
            End Select

            If paramCountMismatchERRID <> 0 Then
                binder.ReportDiagnostic(diagBag, syntax.OperatorToken, paramCountMismatchERRID, SyntaxFacts.GetText(syntax.OperatorToken.Kind))
            End If

            ' ERRID.ERR_OperatorDeclaredInModule is reported by the parser.

            flags = flags Or If(syntax.OperatorToken.Kind = SyntaxKind.CTypeKeyword, SourceMemberFlags.MethodKindConversion, SourceMemberFlags.MethodKindOperator)

            Return New SourceMemberMethodSymbol(
                container, name, flags, binder, syntax, arity:=0)
        End Function

        ' Create a constructor.
        Friend Shared Function CreateConstructor(container As SourceMemberContainerTypeSymbol,
                                                 syntax As SubNewStatementSyntax,
                                                 binder As Binder,
                                                 diagBag As DiagnosticBag) As SourceMethodSymbol

            ' Flags
            Dim modifiers = DecodeConstructorModifiers(syntax.Modifiers, container, binder, diagBag)

            Dim flags = modifiers.AllFlags Or SourceMemberFlags.MethodIsSub

            ' Name, Kind
            Dim name As String
            If (flags And SourceMemberFlags.Shared) <> 0 Then
                name = WellKnownMemberNames.StaticConstructorName
                flags = flags Or SourceMemberFlags.MethodKindSharedConstructor

                If (syntax.ParameterList IsNot Nothing AndAlso syntax.ParameterList.Parameters.Count > 0) Then
                    ' shared constructor cannot have parameters.
                    binder.ReportDiagnostic(diagBag, syntax.ParameterList, ERRID.ERR_SharedConstructorWithParams)
                End If
            Else
                name = WellKnownMemberNames.InstanceConstructorName
                flags = flags Or SourceMemberFlags.MethodKindConstructor
            End If

            Dim methodSym As New SourceMemberMethodSymbol(container, name, flags, binder, syntax, arity:=0)

            If (flags And SourceMemberFlags.Shared) = 0 Then
                If container.TypeKind = TypeKind.Structure AndAlso methodSym.ParameterCount = 0 Then
                    ' Instance constructor must have parameters.
                    Binder.ReportDiagnostic(diagBag, syntax.NewKeyword, ERRID.ERR_NewInStruct)
                End If
            End If

            Return methodSym
        End Function


        ' Decode the modifiers on the method, reporting errors where applicable.
        Private Shared Function DecodeMethodModifiers(modifiers As SyntaxTokenList,
                                                      container As SourceMemberContainerTypeSymbol,
                                                      binder As Binder,
                                                      diagBag As DiagnosticBag) As MemberModifiers
            ' Decode the flags.
            Dim methodModifiers = binder.DecodeModifiers(modifiers,
                SourceMemberFlags.AllAccessibilityModifiers Or SourceMemberFlags.Overloads Or SourceMemberFlags.Partial Or
                SourceMemberFlags.Shadows Or SourceMemberFlags.Shared Or
                SourceMemberFlags.Overridable Or SourceMemberFlags.NotOverridable Or
                SourceMemberFlags.Overrides Or SourceMemberFlags.MustOverride Or
                SourceMemberFlags.Async Or SourceMemberFlags.Iterator,
                ERRID.ERR_BadMethodFlags1,
                Accessibility.Public,
                diagBag)

            methodModifiers = binder.ValidateSharedPropertyAndMethodModifiers(modifiers, methodModifiers, False, container, diagBag)

            Const asyncIterator As SourceMemberFlags = SourceMemberFlags.Async Or SourceMemberFlags.Iterator
            If (methodModifiers.FoundFlags And asyncIterator) = asyncIterator Then
                binder.ReportModifierError(modifiers, ERRID.ERR_InvalidAsyncIteratorModifiers, diagBag, InvalidAsyncIterator)
            End If

            Return methodModifiers
        End Function

        ''' <summary>
        ''' Decode the modifiers on a user-defined operator, reporting errors where applicable. 
        ''' </summary>
        Private Shared Function DecodeOperatorModifiers(syntax As OperatorStatementSyntax,
                                                        binder As Binder,
                                                        diagBag As DiagnosticBag) As MemberModifiers
            ' Decode the flags.
            Dim allowModifiers As SourceMemberFlags = SourceMemberFlags.AllAccessibilityModifiers Or
                                                      SourceMemberFlags.Shared Or
                                                      SourceMemberFlags.Overloads Or
                                                      SourceMemberFlags.Shadows Or
                                                      SourceMemberFlags.Widening Or
                                                      SourceMemberFlags.Narrowing

            Dim operatorModifiers = binder.DecodeModifiers(syntax.Modifiers, allowModifiers, ERRID.ERR_BadOperatorFlags1, Accessibility.Public, diagBag)

            Dim foundFlags As SourceMemberFlags = operatorModifiers.FoundFlags
            Dim computedFlags As SourceMemberFlags = operatorModifiers.ComputedFlags

            ' It is OK to remove/add flags from the found list once an error is reported
            Dim foundAccessibility = foundFlags And SourceMemberFlags.AllAccessibilityModifiers
            If foundAccessibility <> 0 AndAlso foundAccessibility <> SourceMemberFlags.Public Then
                binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_OperatorMustBePublic, diagBag,
                                           SyntaxKind.PrivateKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.FriendKeyword)
                foundFlags = foundFlags And Not SourceMemberFlags.AllAccessibilityModifiers
                computedFlags = (computedFlags And Not SourceMemberFlags.AccessibilityMask) Or SourceMemberFlags.AccessibilityPublic
            End If

            If (foundFlags And SourceMemberFlags.Shared) = 0 Then
                binder.ReportDiagnostic(diagBag, syntax.OperatorToken, ERRID.ERR_OperatorMustBeShared)
                computedFlags = computedFlags Or SourceMemberFlags.Shared
            End If


            If syntax.OperatorToken.Kind = SyntaxKind.CTypeKeyword Then
                If (foundFlags And (SourceMemberFlags.Narrowing Or SourceMemberFlags.Widening)) = 0 Then
                    binder.ReportDiagnostic(diagBag, syntax.OperatorToken, ERRID.ERR_ConvMustBeWideningOrNarrowing)
                    computedFlags = computedFlags Or SourceMemberFlags.Narrowing
                End If
            ElseIf (foundFlags And (SourceMemberFlags.Narrowing Or SourceMemberFlags.Widening)) <> 0 Then
                binder.ReportModifierError(syntax.Modifiers, ERRID.ERR_InvalidSpecifierOnNonConversion1, diagBag,
                                           SyntaxKind.NarrowingKeyword, SyntaxKind.WideningKeyword)
                foundFlags = foundFlags And Not (SourceMemberFlags.Narrowing Or SourceMemberFlags.Widening)
            End If

            Return New MemberModifiers(foundFlags, computedFlags)
        End Function


        ' Decode the modifiers on a constructor, reporting errors where applicable. Constructors are more restrictive
        ' than regular methods, so they have more errors.
        Friend Shared Function DecodeConstructorModifiers(modifiers As SyntaxTokenList,
                                                          container As SourceMemberContainerTypeSymbol,
                                                          binder As Binder,
                                                          diagBag As DiagnosticBag) As MemberModifiers
            Dim constructorModifiers = DecodeMethodModifiers(modifiers, container, binder, diagBag)

            Dim flags = constructorModifiers.FoundFlags
            Dim computedFlags = constructorModifiers.ComputedFlags

            ' It is OK to remove flags from the found list once an error is reported

            If (flags And (SourceMemberFlags.MustOverride Or SourceMemberFlags.Overridable Or SourceMemberFlags.NotOverridable Or SourceMemberFlags.Shadows)) <> 0 Then
                binder.ReportModifierError(modifiers, ERRID.ERR_BadFlagsOnNew1, diagBag,
                                                SyntaxKind.OverridableKeyword, SyntaxKind.MustOverrideKeyword, SyntaxKind.NotOverridableKeyword, SyntaxKind.ShadowsKeyword)
                flags = flags And Not (SourceMemberFlags.MustOverride Or SourceMemberFlags.Overridable Or SourceMemberFlags.NotOverridable Or SourceMemberFlags.Shadows)
            End If

            If (flags And SourceMemberFlags.Overrides) <> 0 Then
                binder.ReportModifierError(modifiers, ERRID.ERR_CantOverrideConstructor, diagBag, SyntaxKind.OverridesKeyword)
                flags = flags And Not SourceMemberFlags.Overrides
            End If

            If (flags And SourceMemberFlags.Partial) <> 0 Then
                binder.ReportModifierError(modifiers, ERRID.ERR_ConstructorCannotBeDeclaredPartial, diagBag, SyntaxKind.PartialKeyword)
                flags = flags And Not SourceMemberFlags.Partial
            End If

            If (flags And SourceMemberFlags.Overloads) <> 0 Then
                binder.ReportModifierError(modifiers, ERRID.ERR_BadFlagsOnNewOverloads, diagBag, SyntaxKind.OverloadsKeyword)
                flags = flags And Not SourceMemberFlags.Overloads
            End If

            If (flags And SourceMemberFlags.Async) <> 0 Then
                binder.ReportModifierError(modifiers, ERRID.ERR_ConstructorAsync, diagBag, SyntaxKind.AsyncKeyword)
            End If

            If ((constructorModifiers.AllFlags And SourceMemberFlags.Shared) <> 0) Then
                If (flags And SourceMemberFlags.AllAccessibilityModifiers) <> 0 Then

                    ' Shared constructors can't be declared with accessibility modifiers
                    binder.ReportModifierError(modifiers, ERRID.ERR_SharedConstructorIllegalSpec1, diagBag,
                                                    SyntaxKind.PublicKeyword, SyntaxKind.PrivateKeyword, SyntaxKind.FriendKeyword, SyntaxKind.ProtectedKeyword)
                End If

                flags = (flags And Not SourceMemberFlags.AllAccessibilityModifiers) Or SourceMemberFlags.Private
                computedFlags = (computedFlags And Not SourceMemberFlags.AccessibilityMask) Or SourceMemberFlags.AccessibilityPrivate
            End If

            Return New MemberModifiers(flags, computedFlags)
        End Function

#End Region

        Friend Overrides Sub GenerateDeclarationErrors(cancellationToken As CancellationToken)
            MyBase.GenerateDeclarationErrors(cancellationToken)

            ' Force signature in methods.
            Dim unusedType = Me.ReturnType
            Dim unusedAttributes = Me.GetReturnTypeAttributes()
            For Each parameter In Me.Parameters
                unusedAttributes = parameter.GetAttributes()
                If parameter.HasExplicitDefaultValue Then
                    Dim defaultValue = parameter.ExplicitDefaultConstantValue()
                End If
            Next

            ' Ensure method type parameter constraints are resolved and checked.
            For Each typeParameter In Me.TypeParameters
                Dim unusedTypes = typeParameter.ConstraintTypesNoUseSiteDiagnostics
            Next

            ' Ensure Handles are resolved.
            Dim unusedHandles = Me.HandledEvents
        End Sub

        Friend ReadOnly Property Diagnostics As ImmutableArray(Of Diagnostic)
            Get
                Return _cachedDiagnostics
            End Get
        End Property

        ''' <summary>
        ''' Returns true if our diagnostics were used in the event that there were two threads racing.
        ''' </summary>
        Friend Function SetDiagnostics(diags As ImmutableArray(Of Diagnostic)) As Boolean
            Return ImmutableInterlocked.InterlockedInitialize(_cachedDiagnostics, diags)
        End Function

        Public Overrides ReadOnly Property IsImplicitlyDeclared As Boolean
            Get
                Return m_containingType.AreMembersImplicitlyDeclared
            End Get
        End Property

        Friend Overrides ReadOnly Property GenerateDebugInfoImpl As Boolean
            Get
                Return True
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ConstructedFrom As MethodSymbol
            Get
                Return Me
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ContainingSymbol As Symbol
            Get
                Return m_containingType
            End Get
        End Property

        Public Overrides ReadOnly Property ContainingType As NamedTypeSymbol
            Get
                Return m_containingType
            End Get
        End Property

        Public ReadOnly Property ContainingSourceModule As SourceModuleSymbol
            Get
                Return DirectCast(ContainingModule, SourceModuleSymbol)
            End Get
        End Property

        Public Overrides ReadOnly Property AssociatedSymbol As Symbol
            Get
                ' TODO: Associated property/event not implemented.
                Return Nothing
            End Get
        End Property

        Public Overrides ReadOnly Property ExplicitInterfaceImplementations As ImmutableArray(Of MethodSymbol)
            Get
                Return ImmutableArray(Of MethodSymbol).Empty
            End Get
        End Property

#Region "Flags"

        Public Overrides ReadOnly Property MethodKind As MethodKind
            Get
                Return m_flags.ToMethodKind()
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property IsMethodKindBasedOnSyntax As Boolean
            Get
                Return True
            End Get
        End Property

        ' TODO (tomat): NotOverridable?
        Public Overrides ReadOnly Property DeclaredAccessibility As Accessibility
            Get
                Return CType((m_flags And SourceMemberFlags.AccessibilityMask), Accessibility)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsMustOverride As Boolean
            Get
                Return (m_flags And SourceMemberFlags.MustOverride) <> 0
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsNotOverridable As Boolean
            Get
                Return (m_flags And SourceMemberFlags.NotOverridable) <> 0
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverloads As Boolean
            Get
                If (m_flags And SourceMemberFlags.Shadows) <> 0 Then
                    Return False
                ElseIf (m_flags And SourceMemberFlags.Overloads) <> 0 Then
                    Return True
                Else
                    Return (m_flags And SourceMemberFlags.Overrides) <> 0
                End If
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverridable As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Overridable) <> 0
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsOverrides As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Overrides) <> 0
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsShared As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Shared) <> 0
            End Get
        End Property

        Friend ReadOnly Property IsPartial As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Partial) <> 0
            End Get
        End Property

        ''' <summary>
        '''  True if 'Shadows' is explicitly specified in method's declaration.
        '''  </summary>
        Friend Overrides ReadOnly Property ShadowsExplicitly As Boolean
            ' TODO (tomat) : NotOverridable?
            Get
                Return (m_flags And SourceMemberFlags.Shadows) <> 0
            End Get
        End Property

        ''' <summary> 
        ''' True if 'Overloads' is explicitly specified in method's declaration.
        ''' </summary>
        Friend ReadOnly Property OverloadsExplicitly As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Overloads) <> 0
            End Get
        End Property

        ''' <summary>
        '''  True if 'Overrides' is explicitly specified in method's declaration.
        ''' </summary>
        Friend ReadOnly Property OverridesExplicitly As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Overrides) <> 0
            End Get
        End Property

        ''' <summary>
        ''' True if 'Handles' is specified in method's declaration
        ''' </summary>
        Friend ReadOnly Property HandlesEvents As Boolean
            Get
                Return (m_flags And SourceMemberFlags.MethodHandlesEvents) <> 0
            End Get
        End Property


        Friend NotOverridable Overrides ReadOnly Property CallingConvention As CallingConvention
            Get
                Return If(IsShared, CallingConvention.Default, CallingConvention.HasThis) Or
                       If(IsGenericMethod, CallingConvention.Generic, CallingConvention.Default)
            End Get
        End Property

#End Region

#Region "Syntax and Binding"

        ' Return the entire declaration block: Begin Statement + Body Statements + End Statement.
        Friend ReadOnly Property BlockSyntax As MethodBlockBaseSyntax
            Get
                If m_syntaxReferenceOpt Is Nothing Then
                    Return Nothing
                End If

                Dim decl = m_syntaxReferenceOpt.GetSyntax()
                Return TryCast(decl.Parent, MethodBlockBaseSyntax)
            End Get
        End Property

        Friend Overrides ReadOnly Property Syntax As SyntaxNode
            Get
                If m_syntaxReferenceOpt Is Nothing Then
                    Return Nothing
                End If

                ' usually the syntax of a source method symbol should be the block syntax
                Dim syntaxNode = Me.BlockSyntax
                If syntaxNode IsNot Nothing Then
                    Return syntaxNode
                End If

                ' in case of a method in an interface there is no block.
                ' just return the sub/function statement in this case.
                Return m_syntaxReferenceOpt.GetVisualBasicSyntax()
            End Get
        End Property

        ' Return the syntax tree that contains the method block.
        Public ReadOnly Property SyntaxTree As SyntaxTree
            Get
                If m_syntaxReferenceOpt IsNot Nothing Then
                    Return m_syntaxReferenceOpt.SyntaxTree
                End If
                Return Nothing
            End Get
        End Property

        Friend ReadOnly Property DeclarationSyntax As MethodBaseSyntax
            Get
                Return If(m_syntaxReferenceOpt IsNot Nothing, DirectCast(m_syntaxReferenceOpt.GetSyntax(), MethodBaseSyntax), Nothing)
            End Get
        End Property

        Friend Overridable ReadOnly Property HasEmptyBody As Boolean
            Get
                Dim blockSyntax = Me.BlockSyntax
                Return blockSyntax Is Nothing OrElse Not blockSyntax.Statements.Any
            End Get
        End Property

        Public Overrides ReadOnly Property DeclaringSyntaxReferences As ImmutableArray(Of SyntaxReference)
            Get
                Return GetDeclaringSyntaxReferenceHelper(m_syntaxReferenceOpt)
            End Get
        End Property

        Friend NotOverridable Overrides Function IsDefinedInSourceTree(tree As SyntaxTree, definedWithinSpan As TextSpan?, Optional cancellationToken As CancellationToken = Nothing) As Boolean
            Return IsDefinedInSourceTree(Me.Syntax, tree, definedWithinSpan, cancellationToken)
        End Function

        Public NotOverridable Overrides Function GetDocumentationCommentXml(Optional preferredCulture As CultureInfo = Nothing, Optional expandIncludes As Boolean = False, Optional cancellationToken As CancellationToken = Nothing) As String
            If expandIncludes Then
                Return GetAndCacheDocumentationComment(Me, preferredCulture, expandIncludes, _lazyExpandedDocComment, cancellationToken)
            Else
                Return GetAndCacheDocumentationComment(Me, preferredCulture, expandIncludes, _lazyDocComment, cancellationToken)
            End If
        End Function

        ''' <summary>
        '''  Return the location from syntax reference only.
        ''' </summary>
        Friend ReadOnly Property NonMergedLocation As Location
            Get
                Return If(m_syntaxReferenceOpt IsNot Nothing, GetSymbolLocation(m_syntaxReferenceOpt), Nothing)
            End Get
        End Property

        Friend Overrides Function GetLexicalSortKey() As LexicalSortKey
            ' WARNING: this should not allocate memory!
            Return If(m_syntaxReferenceOpt IsNot Nothing,
                    New LexicalSortKey(m_syntaxReferenceOpt, Me.DeclaringCompilation),
                    LexicalSortKey.NotInSource)
        End Function

        Public Overrides ReadOnly Property Locations As ImmutableArray(Of Location)
            Get
                ' NOTE: access to m_locations don't really need to be synchronized because 
                '       it is never being modified after the method symbol is published
                If _lazyLocations.IsDefault Then

                    ' This symbol location
                    Dim location As Location = Me.NonMergedLocation
                    ImmutableInterlocked.InterlockedCompareExchange(Me._lazyLocations,
                                                        If(location Is Nothing,
                                                           ImmutableArray(Of Location).Empty,
                                                           ImmutableArray.Create(location)),
                                                        Nothing)
                End If
                Return _lazyLocations
            End Get
        End Property

        ' Given a syntax ref, get the symbol location to return. We return the location of the name
        ' of the method.
        Private Function GetSymbolLocation(syntaxRef As SyntaxReference) As Location
            Dim syntaxNode = syntaxRef.GetVisualBasicSyntax()
            Dim syntaxTree = syntaxRef.SyntaxTree

            Return syntaxTree.GetLocation(GetMethodLocationFromSyntax(syntaxNode))
        End Function

        ' Get the location of a method given the syntax for its declaration. We use the location of the name
        ' of the method, or similar keywords.
        Private Shared Function GetMethodLocationFromSyntax(node As VisualBasicSyntaxNode) As TextSpan
            Select Case node.Kind
                Case SyntaxKind.MultiLineFunctionLambdaExpression,
                     SyntaxKind.MultiLineSubLambdaExpression,
                     SyntaxKind.SingleLineFunctionLambdaExpression,
                     SyntaxKind.SingleLineSubLambdaExpression
                    Return DirectCast(node, LambdaExpressionSyntax).SubOrFunctionHeader.Span

                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Return DirectCast(node, MethodStatementSyntax).Identifier.Span

                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(node, DeclareStatementSyntax).Identifier.Span

                Case SyntaxKind.SubNewStatement
                    Return DirectCast(node, SubNewStatementSyntax).NewKeyword.Span

                Case SyntaxKind.OperatorStatement
                    Return DirectCast(node, OperatorStatementSyntax).OperatorToken.Span

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Bind the constraint declarations for the given type parameter.
        ''' </summary>
        ''' <remarks>
        ''' The caller is expected to handle constraint checking and any caching of results.
        ''' </remarks>
        Friend Function BindTypeParameterConstraints(syntax As TypeParameterSyntax,
                                                     diagnostics As DiagnosticBag) As ImmutableArray(Of TypeParameterConstraint)

            Dim binder As Binder = BinderBuilder.CreateBinderForType(Me.ContainingSourceModule, Me.SyntaxTree, m_containingType)
            binder = BinderBuilder.CreateBinderForGenericMethodDeclaration(Me, binder)

            ' Handle type parameter variance.
            If syntax.VarianceKeyword.Kind <> SyntaxKind.None Then
                Binder.ReportDiagnostic(diagnostics, syntax.VarianceKeyword, ERRID.ERR_VarianceDisallowedHere)
            End If

            ' Wrap constraints binder in a location-specific binder to
            ' avoid checking constraints when binding type names.
            binder = New LocationSpecificBinder(BindingLocation.GenericConstraintsClause, Me, binder)
            Return binder.BindTypeParameterConstraintClause(Me, syntax.TypeParameterConstraintClause, diagnostics)
        End Function

        ' Get the symbol name that would be used for this method base syntax.
        Friend Shared Function GetMemberNameFromSyntax(node As MethodBaseSyntax) As String
            Select Case node.Kind
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Return DirectCast(node, MethodStatementSyntax).Identifier.ValueText

                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).Identifier.ValueText

                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(node, DeclareStatementSyntax).Identifier.ValueText

                Case SyntaxKind.OperatorStatement
                    Dim operatorStatement = DirectCast(node, OperatorStatementSyntax)

                    Select Case operatorStatement.OperatorToken.Kind
                        Case SyntaxKind.NotKeyword
                            Return WellKnownMemberNames.OnesComplementOperatorName

                        Case SyntaxKind.IsTrueKeyword
                            Return WellKnownMemberNames.TrueOperatorName

                        Case SyntaxKind.IsFalseKeyword
                            Return WellKnownMemberNames.FalseOperatorName

                        Case SyntaxKind.PlusToken
                            If operatorStatement.ParameterList.Parameters.Count <= 1 Then
                                Return WellKnownMemberNames.UnaryPlusOperatorName
                            Else
                                Return WellKnownMemberNames.AdditionOperatorName
                            End If

                        Case SyntaxKind.MinusToken
                            If operatorStatement.ParameterList.Parameters.Count <= 1 Then
                                Return WellKnownMemberNames.UnaryNegationOperatorName
                            Else
                                Return WellKnownMemberNames.SubtractionOperatorName
                            End If

                        Case SyntaxKind.AsteriskToken
                            Return WellKnownMemberNames.MultiplyOperatorName

                        Case SyntaxKind.SlashToken
                            Return WellKnownMemberNames.DivisionOperatorName

                        Case SyntaxKind.BackslashToken
                            Return WellKnownMemberNames.IntegerDivisionOperatorName

                        Case SyntaxKind.ModKeyword
                            Return WellKnownMemberNames.ModulusOperatorName

                        Case SyntaxKind.CaretToken
                            Return WellKnownMemberNames.ExponentOperatorName

                        Case SyntaxKind.EqualsToken
                            Return WellKnownMemberNames.EqualityOperatorName

                        Case SyntaxKind.LessThanGreaterThanToken
                            Return WellKnownMemberNames.InequalityOperatorName

                        Case SyntaxKind.LessThanToken
                            Return WellKnownMemberNames.LessThanOperatorName

                        Case SyntaxKind.GreaterThanToken
                            Return WellKnownMemberNames.GreaterThanOperatorName

                        Case SyntaxKind.LessThanEqualsToken
                            Return WellKnownMemberNames.LessThanOrEqualOperatorName

                        Case SyntaxKind.GreaterThanEqualsToken
                            Return WellKnownMemberNames.GreaterThanOrEqualOperatorName

                        Case SyntaxKind.LikeKeyword
                            Return WellKnownMemberNames.LikeOperatorName

                        Case SyntaxKind.AmpersandToken
                            Return WellKnownMemberNames.ConcatenateOperatorName

                        Case SyntaxKind.AndKeyword
                            Return WellKnownMemberNames.BitwiseAndOperatorName

                        Case SyntaxKind.OrKeyword
                            Return WellKnownMemberNames.BitwiseOrOperatorName

                        Case SyntaxKind.XorKeyword
                            Return WellKnownMemberNames.ExclusiveOrOperatorName

                        Case SyntaxKind.LessThanLessThanToken
                            Return WellKnownMemberNames.LeftShiftOperatorName

                        Case SyntaxKind.GreaterThanGreaterThanToken
                            Return WellKnownMemberNames.RightShiftOperatorName

                        Case SyntaxKind.CTypeKeyword

                            For Each keywordSyntax In operatorStatement.Modifiers
                                Dim currentModifier As SourceMemberFlags = Binder.MapKeywordToFlag(keywordSyntax)

                                If currentModifier = SourceMemberFlags.Widening Then
                                    Return WellKnownMemberNames.ImplicitConversionName
                                ElseIf currentModifier = SourceMemberFlags.Narrowing Then
                                    Return WellKnownMemberNames.ExplicitConversionName
                                End If
                            Next

                            Return WellKnownMemberNames.ExplicitConversionName

                        Case Else
                            Throw ExceptionUtilities.UnexpectedValue(operatorStatement.OperatorToken.Kind)
                    End Select

                Case SyntaxKind.SubNewStatement
                    ' Symbol name of a constructor depends on if it is shared. We ideally we like to just call
                    ' DecodeConstructorModifiers here, but we don't have a binder or container to pass. So we have
                    ' to duplicate some of the logic just to determine if it is shared.
                    Dim isShared As Boolean = False
                    For Each tok In node.Modifiers
                        If tok.Kind = SyntaxKind.SharedKeyword Then
                            isShared = True
                        End If
                    Next
                    ' inside a module are implicitly shared.
                    If node.Parent IsNot Nothing Then
                        If node.Parent.Kind = SyntaxKind.ModuleBlock OrElse
                            (node.Parent.Parent IsNot Nothing AndAlso node.Parent.Parent.Kind = SyntaxKind.ModuleBlock) Then
                            isShared = True
                        End If
                    End If

                    Return If(isShared, WellKnownMemberNames.StaticConstructorName, WellKnownMemberNames.InstanceConstructorName)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(node.Kind)
            End Select
        End Function

        ' Given the syntax declaration, and a container, get the source method symbol declared from that syntax.
        ' This is done by lookup up the name from the declaration in the container, handling duplicates and
        ' so forth correctly.
        Friend Shared Function FindSymbolFromSyntax(syntax As MethodBaseSyntax,
                                                    tree As SyntaxTree,
                                                    container As NamedTypeSymbol) As Symbol

            Select Case syntax.Kind
                Case SyntaxKind.GetAccessorStatement, SyntaxKind.SetAccessorStatement
                    Dim propertySyntax = TryCast(syntax.Parent.Parent, PropertyBlockSyntax)
                    If propertySyntax IsNot Nothing Then
                        Dim propertyIdentifier = propertySyntax.PropertyStatement.Identifier
                        Dim propertySymbol = DirectCast(
                            container.FindMember(propertyIdentifier.ValueText, SymbolKind.Property, propertyIdentifier.Span, tree),
                            PropertySymbol)

                        ' in case of ill formed syntax it can happen that the ContainingType of the actual binder does not directly contain
                        ' this property symbol. One example is e.g. a namespace nested in a class. Instead of a namespace binder, the containing
                        ' binder will be used in this error case and then member lookups will fail because the symbol of the containing binder does 
                        ' not contain these members.
                        If propertySymbol Is Nothing Then
                            Return Nothing
                        End If

                        Dim accessor = If(syntax.Kind = SyntaxKind.GetAccessorStatement, propertySymbol.GetMethod, propertySymbol.SetMethod)

                        ' symbol must have same syntax as the accessor's block
                        If accessor.Syntax Is syntax.Parent Then
                            Return accessor
                        Else
                            ' This can happen if property has multiple accessors. 
                            ' Parser allows multiple accessors, but binder will accept only one of a kind
                            Return Nothing
                        End If
                    Else
                        ' Did not find a property block. Can happen if syntax was ill-formed.
                        Return Nothing
                    End If

                Case SyntaxKind.AddHandlerAccessorStatement, SyntaxKind.RemoveHandlerAccessorStatement, SyntaxKind.RaiseEventAccessorStatement
                    Dim eventBlockSyntax = TryCast(syntax.Parent.Parent, EventBlockSyntax)
                    If eventBlockSyntax IsNot Nothing Then
                        Dim eventIdentifier = eventBlockSyntax.EventStatement.Identifier
                        Dim eventSymbol = DirectCast(
                            container.FindMember(eventIdentifier.ValueText, SymbolKind.Event, eventIdentifier.Span, tree),
                            EventSymbol)

                        ' in case of ill formed syntax it can happen that the ContainingType of the actual binder does not directly contain
                        ' this event symbol. One example is e.g. a namespace nested in a class. Instead of a namespace binder, the containing
                        ' binder will be used in this error case and then member lookups will fail because the symbol of the containing binder does 
                        ' not contain these members.
                        If eventSymbol Is Nothing Then
                            Return Nothing
                        End If

                        Dim accessor As MethodSymbol = Nothing
                        Select Case syntax.Kind
                            Case SyntaxKind.AddHandlerAccessorStatement
                                accessor = eventSymbol.AddMethod
                            Case SyntaxKind.RemoveHandlerAccessorStatement
                                accessor = eventSymbol.RemoveMethod
                            Case SyntaxKind.RaiseEventAccessorStatement
                                accessor = eventSymbol.RaiseMethod
                        End Select

                        ' symbol must have same syntax as the accessor's block
                        If accessor IsNot Nothing AndAlso accessor.Syntax Is syntax.Parent Then
                            Return accessor
                        Else
                            ' This can happen if event has multiple accessors. 
                            ' Parser allows multiple accessors, but binder will accept only one of a kind
                            Return Nothing
                        End If
                    Else
                        ' Did not find an event block. Can happen if syntax was ill-formed.
                        Return Nothing
                    End If

                Case SyntaxKind.PropertyStatement
                    Dim propertyIdentifier = DirectCast(syntax, PropertyStatementSyntax).Identifier
                    Return container.FindMember(propertyIdentifier.ValueText, SymbolKind.Property, propertyIdentifier.Span, tree)

                Case SyntaxKind.EventStatement
                    Dim eventIdentifier = DirectCast(syntax, EventStatementSyntax).Identifier
                    Return container.FindMember(eventIdentifier.ValueText, SymbolKind.Event, eventIdentifier.Span, tree)

                Case SyntaxKind.DelegateFunctionStatement, SyntaxKind.DelegateSubStatement
                    Dim delegateIdentifier = DirectCast(syntax, DelegateStatementSyntax).Identifier
                    Return container.FindMember(delegateIdentifier.ValueText, SymbolKind.NamedType, delegateIdentifier.Span, tree)

                Case Else
                    Dim methodSymbol = DirectCast(container.FindMember(GetMemberNameFromSyntax(syntax),
                                                                       SymbolKind.Method, GetMethodLocationFromSyntax(syntax), tree), MethodSymbol)

                    ' Substitute with partial method implementation?
                    If methodSymbol IsNot Nothing Then
                        Dim partialImpl = methodSymbol.PartialImplementationPart
                        If partialImpl IsNot Nothing AndAlso partialImpl.Syntax Is syntax.Parent Then
                            methodSymbol = partialImpl
                        End If
                    End If

                    Return methodSymbol
            End Select
        End Function

        ' Get the location of the implements name for an explicit implemented method, for later error reporting.
        Friend Function GetImplementingLocation(implementedMethod As MethodSymbol) As Location
            Debug.Assert(ExplicitInterfaceImplementations.Contains(implementedMethod))

            Dim methodSyntax As MethodStatementSyntax = Nothing
            Dim syntaxTree As SyntaxTree = Nothing
            Dim containingSourceType = TryCast(m_containingType, SourceMemberContainerTypeSymbol)

            If m_syntaxReferenceOpt IsNot Nothing Then
                methodSyntax = TryCast(m_syntaxReferenceOpt.GetSyntax(), MethodStatementSyntax)
                syntaxTree = m_syntaxReferenceOpt.SyntaxTree
            End If

            If methodSyntax IsNot Nothing AndAlso methodSyntax.ImplementsClause IsNot Nothing AndAlso containingSourceType IsNot Nothing Then
                Dim binder As Binder = BinderBuilder.CreateBinderForType(containingSourceType.ContainingSourceModule, syntaxTree, containingSourceType)
                Dim implementingSyntax = FindImplementingSyntax(methodSyntax.ImplementsClause,
                                                                Me,
                                                                implementedMethod,
                                                                containingSourceType,
                                                                binder)
                Return implementingSyntax.GetLocation()
            End If

            Return If(Locations.FirstOrDefault(), NoLocation.Singleton)
        End Function

        Friend Overrides Function GetBoundMethodBody(compilationState As TypeCompilationState, diagnostics As DiagnosticBag, Optional ByRef methodBodyBinder As Binder = Nothing) As BoundBlock

            Dim syntaxTree As SyntaxTree = Me.SyntaxTree

            ' All source method symbols must have block syntax.
            Dim methodBlock As MethodBlockBaseSyntax = Me.BlockSyntax
            Debug.Assert(methodBlock IsNot Nothing)

            ' Bind the method block
            methodBodyBinder = BinderBuilder.CreateBinderForMethodBody(ContainingSourceModule, syntaxTree, Me)

#If DEBUG Then
            ' Enable DEBUG check for ordering of simple name binding.
            methodBodyBinder.EnableSimpleNameBindingOrderChecks(True)
#End If
            Dim boundStatement = methodBodyBinder.BindStatement(methodBlock, diagnostics)
#If DEBUG Then
            methodBodyBinder.EnableSimpleNameBindingOrderChecks(False)
#End If
            If boundStatement.Kind = BoundKind.Block Then
                Return DirectCast(boundStatement, BoundBlock)
            End If

            Return New BoundBlock(methodBlock, methodBlock.Statements, ImmutableArray(Of LocalSymbol).Empty, ImmutableArray.Create(boundStatement))
        End Function

        Friend NotOverridable Overrides Function CalculateLocalSyntaxOffset(localPosition As Integer, localTree As SyntaxTree) As Integer
            Dim span As TextSpan

            Dim block = BlockSyntax
            If block IsNot Nothing AndAlso localTree Is block.SyntaxTree Then
                ' Assign all variables that are associated with the header -1.
                ' We can't assign >=0 since user-defined variables defined in the first statement of the body have 0
                ' and user-defined variables need to have a unique syntax offset.
                If localPosition = block.BlockStatement.SpanStart Then
                    Return -1
                End If

                span = block.Statements.Span

                If span.Contains(localPosition) Then
                    Return localPosition - span.Start
                End If
            End If

            ' Calculates a syntax offset of a syntax position which must be either a property or field initializer.
            Dim syntaxOffset As Integer
            Dim containingType = DirectCast(Me.ContainingType, SourceNamedTypeSymbol)
            If containingType.TryCalculateSyntaxOffsetOfPositionInInitializer(localPosition, localTree, Me.IsShared, syntaxOffset) Then
                Return syntaxOffset
            End If

            Throw ExceptionUtilities.Unreachable
        End Function
#End Region

#Region "Signature"

        Public NotOverridable Overrides ReadOnly Property IsVararg As Boolean
            Get
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsGenericMethod As Boolean
            Get
                Return Arity <> 0
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property TypeArguments As ImmutableArray(Of TypeSymbol)
            Get
                Debug.Assert(Not TypeParameters.IsDefault)
                Return StaticCast(Of TypeSymbol).From(TypeParameters)
            End Get
        End Property

        Public Overrides ReadOnly Property Arity As Integer
            Get
                Return TypeParameters.Length
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property ReturnsByRef As Boolean
            Get
                ' It is not possible to define ref-returning methods in source.
                Return False
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property RefCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Return ImmutableArray(Of CustomModifier).Empty
            End Get
        End Property

        Public Overrides ReadOnly Property IsSub As Boolean
            Get
                Debug.Assert(Me.MethodKind <> MethodKind.EventAdd,
                             "Can't trust the flag for event adders, because their signatures are different under WinRT")
                Return (m_flags And SourceMemberFlags.MethodIsSub) <> 0
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsAsync As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Async) <> 0
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property IsIterator As Boolean
            Get
                Return (m_flags And SourceMemberFlags.Iterator) <> 0
            End Get
        End Property

        Friend NotOverridable Overrides Function TryGetMeParameter(<Out> ByRef meParameter As ParameterSymbol) As Boolean
            If IsShared Then
                meParameter = Nothing
            Else
                If _lazyMeParameter Is Nothing Then
                    Interlocked.CompareExchange(_lazyMeParameter, New MeParameterSymbol(Me), Nothing)
                End If

                meParameter = _lazyMeParameter
            End If
            Return True
        End Function

        Public Overrides ReadOnly Property ReturnTypeCustomModifiers As ImmutableArray(Of CustomModifier)
            Get
                Dim overridden = Me.OverriddenMethod

                If overridden Is Nothing Then
                    Return ImmutableArray(Of CustomModifier).Empty
                Else
                    Return overridden.ConstructIfGeneric(TypeArguments).ReturnTypeCustomModifiers
                End If
            End Get
        End Property

#End Region

#Region "Attributes"

        ''' <summary>
        ''' Symbol to copy bound attributes from, or null if the attributes are not shared among multiple source method symbols.
        ''' </summary>
        ''' <remarks>
        ''' Used for example for event accessors. The "remove" method delegates attribute binding to the "add" method. 
        ''' The bound attribute data are then applied to both accessors.
        ''' </remarks>
        Protected Overridable ReadOnly Property BoundAttributesSource As SourceMethodSymbol
            Get
                Return Nothing
            End Get
        End Property

        ''' <summary>
        ''' Symbol to copy bound return type attributes from, or null if the attributes are not shared among multiple source symbols.
        ''' </summary>
        ''' <remarks>
        ''' Used for property accessors. Getter copies its return type attributes from the property return type attributes.
        ''' 
        ''' So far we only need to return <see cref="SourcePropertySymbol"/>. If we ever needed to return a <see cref="SourceMethodSymbol"/> 
        ''' we could implement an interface on those two types.
        ''' </remarks>
        Protected Overridable ReadOnly Property BoundReturnTypeAttributesSource As SourcePropertySymbol
            Get
                Return Nothing
            End Get
        End Property

        Protected ReadOnly Property AttributeDeclarationSyntaxList As SyntaxList(Of AttributeListSyntax)
            Get
                Return If(m_syntaxReferenceOpt IsNot Nothing, DeclarationSyntax.AttributeLists, Nothing)
            End Get
        End Property

        Protected ReadOnly Property ReturnTypeAttributeDeclarationSyntaxList As SyntaxList(Of AttributeListSyntax)
            Get
                Dim syntax = DeclarationSyntax
                If syntax IsNot Nothing Then
                    Dim asClauseOpt = syntax.AsClauseInternal
                    If asClauseOpt IsNot Nothing Then
                        Return asClauseOpt.Attributes
                    End If
                End If

                Return Nothing
            End Get
        End Property

        Protected Overridable Function GetAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Return OneOrMany.Create(AttributeDeclarationSyntaxList)
        End Function

        Protected Overridable Function GetReturnTypeAttributeDeclarations() As OneOrMany(Of SyntaxList(Of AttributeListSyntax))
            Return OneOrMany.Create(ReturnTypeAttributeDeclarationSyntaxList)
        End Function

        Public ReadOnly Property DefaultAttributeLocation As AttributeLocation Implements IAttributeTargetSymbol.DefaultAttributeLocation
            Get
                Return AttributeLocation.Method
            End Get
        End Property

        Private Function GetAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            Return GetAttributesBag(m_lazyCustomAttributesBag, forReturnType:=False)
        End Function

        Private Function GetReturnTypeAttributesBag() As CustomAttributesBag(Of VisualBasicAttributeData)
            Return GetAttributesBag(m_lazyReturnTypeCustomAttributesBag, forReturnType:=True)
        End Function

        Private Function GetAttributesBag(ByRef lazyCustomAttributesBag As CustomAttributesBag(Of VisualBasicAttributeData), forReturnType As Boolean) As CustomAttributesBag(Of VisualBasicAttributeData)
            If lazyCustomAttributesBag Is Nothing OrElse Not lazyCustomAttributesBag.IsSealed Then
                If forReturnType Then
                    Dim copyFrom = Me.BoundReturnTypeAttributesSource

                    ' prevent infinite recursion:
                    Debug.Assert(copyFrom IsNot Me)

                    If copyFrom IsNot Nothing Then
                        Dim attributesBag = copyFrom.GetReturnTypeAttributesBag()
                        Interlocked.CompareExchange(lazyCustomAttributesBag, attributesBag, Nothing)
                    Else
                        LoadAndValidateAttributes(Me.GetReturnTypeAttributeDeclarations(), lazyCustomAttributesBag, symbolPart:=AttributeLocation.Return)
                    End If
                Else
                    Dim copyFrom = Me.BoundAttributesSource

                    ' prevent infinite recursion:
                    Debug.Assert(copyFrom IsNot Me)

                    If copyFrom IsNot Nothing Then
                        Dim attributesBag = copyFrom.GetAttributesBag()
                        Interlocked.CompareExchange(lazyCustomAttributesBag, attributesBag, Nothing)
                    Else
                        LoadAndValidateAttributes(Me.GetAttributeDeclarations(), lazyCustomAttributesBag)
                    End If
                End If
            End If

            Return lazyCustomAttributesBag
        End Function

        ''' <summary>
        ''' Gets the attributes applied on this symbol.
        ''' Returns an empty array if there are no attributes.
        ''' </summary>
        Public NotOverridable Overrides Function GetAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetAttributesBag().Attributes
        End Function

        Friend Overrides Sub AddSynthesizedAttributes(compilationState As ModuleCompilationState, ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedAttributes(compilationState, attributes)

            ' Emit synthesized STAThreadAttribute for this method if both the following requirements are met:
            ' (a) This is the entry point method.
            ' (b) There is no applied STAThread or MTAThread attribute on this method.

            Dim compilation = Me.DeclaringCompilation
            Dim entryPointMethod As MethodSymbol = compilation.GetEntryPoint(CancellationToken.None)

            If Me Is entryPointMethod Then
                If Not Me.HasSTAThreadOrMTAThreadAttribute Then
                    ' UNDONE: UV Support: Do not emit if using the starlite libraries.

                    AddSynthesizedAttribute(attributes, compilation.TrySynthesizeAttribute(
                        WellKnownMember.System_STAThreadAttribute__ctor))
                End If
            End If
        End Sub

        Friend Overrides Sub AddSynthesizedReturnTypeAttributes(ByRef attributes As ArrayBuilder(Of SynthesizedAttributeData))
            MyBase.AddSynthesizedReturnTypeAttributes(attributes)

            If Me.ReturnType.ContainsTupleNames() Then
                AddSynthesizedAttribute(attributes, DeclaringCompilation.SynthesizeTupleNamesAttribute(Me.ReturnType))
            End If
        End Sub

        Protected Function GetDecodedWellKnownAttributeData() As MethodWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.m_lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, MethodWellKnownAttributeData)
        End Function

        ''' <summary>
        ''' Returns the list of attributes, if any, associated with the return type.
        ''' </summary>
        Public NotOverridable Overrides Function GetReturnTypeAttributes() As ImmutableArray(Of VisualBasicAttributeData)
            Return Me.GetReturnTypeAttributesBag().Attributes
        End Function

        Private Function GetDecodedReturnTypeWellKnownAttributeData() As CommonReturnTypeWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.m_lazyReturnTypeCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetReturnTypeAttributesBag()
            End If

            Return DirectCast(attributesBag.DecodedWellKnownAttributeData, CommonReturnTypeWellKnownAttributeData)
        End Function

        Friend Overrides Function EarlyDecodeWellKnownAttribute(ByRef arguments As EarlyDecodeWellKnownAttributeArguments(Of EarlyWellKnownAttributeBinder, NamedTypeSymbol, AttributeSyntax, AttributeLocation)) As VisualBasicAttributeData
            Debug.Assert(arguments.AttributeType IsNot Nothing)
            Debug.Assert(Not arguments.AttributeType.IsErrorType())
            Dim hasAnyDiagnostics As Boolean = False

            If arguments.SymbolPart <> AttributeLocation.Return Then
                If VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.CaseInsensitiveExtensionAttribute) Then
                    Dim isExtensionMethod As Boolean = False

                    If Not (Me.MethodKind <> MethodKind.Ordinary AndAlso Me.MethodKind <> MethodKind.DeclareMethod) AndAlso
                        m_containingType.AllowsExtensionMethods() AndAlso
                        Me.ParameterCount <> 0 Then

                        Debug.Assert(Me.IsShared)

                        Dim firstParam As ParameterSymbol = Me.Parameters(0)

                        If Not firstParam.IsOptional AndAlso
                           Not firstParam.IsParamArray AndAlso
                           ValidateGenericConstraintsOnExtensionMethodDefinition() Then
                            isExtensionMethod = m_containingType.MightContainExtensionMethods
                        End If
                    End If

                    If isExtensionMethod Then
                        Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                        If Not attrdata.HasErrors Then
                            arguments.GetOrCreateData(Of MethodEarlyWellKnownAttributeData)().IsExtensionMethod = True
                            Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                        End If
                    End If

                    Return Nothing
                ElseIf VisualBasicAttributeData.IsTargetEarlyAttribute(arguments.AttributeType, arguments.AttributeSyntax, AttributeDescription.ConditionalAttribute) Then
                    Dim attrdata = arguments.Binder.GetAttribute(arguments.AttributeSyntax, arguments.AttributeType, hasAnyDiagnostics)
                    If Not attrdata.HasErrors Then
                        Dim conditionalSymbol As String = attrdata.GetConstructorArgument(Of String)(0, SpecialType.System_String)
                        arguments.GetOrCreateData(Of MethodEarlyWellKnownAttributeData)().AddConditionalSymbol(conditionalSymbol)
                        Return If(Not hasAnyDiagnostics, attrdata, Nothing)
                    Else
                        Return Nothing
                    End If
                Else
                    Dim BoundAttribute As VisualBasicAttributeData = Nothing
                    Dim obsoleteData As ObsoleteAttributeData = Nothing

                    If EarlyDecodeDeprecatedOrExperimentalOrObsoleteAttribute(arguments, BoundAttribute, obsoleteData) Then
                        If obsoleteData IsNot Nothing Then
                            arguments.GetOrCreateData(Of MethodEarlyWellKnownAttributeData)().ObsoleteAttributeData = obsoleteData
                        End If

                        Return BoundAttribute
                    End If
                End If
            End If

            Return MyBase.EarlyDecodeWellKnownAttribute(arguments)
        End Function

        ''' <summary>
        ''' Returns data decoded from early bound well-known attributes applied to the symbol or null if there are no applied attributes.
        ''' </summary>
        ''' <remarks>
        ''' Forces binding and decoding of attributes.
        ''' </remarks>
        Private Function GetEarlyDecodedWellKnownAttributeData() As MethodEarlyWellKnownAttributeData
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.m_lazyCustomAttributesBag
            If attributesBag Is Nothing OrElse Not attributesBag.IsEarlyDecodedWellKnownAttributeDataComputed Then
                attributesBag = Me.GetAttributesBag()
            End If

            Return DirectCast(attributesBag.EarlyDecodedWellKnownAttributeData, MethodEarlyWellKnownAttributeData)
        End Function

        Friend Overrides Function GetAppliedConditionalSymbols() As ImmutableArray(Of String)
            Dim data As MethodEarlyWellKnownAttributeData = Me.GetEarlyDecodedWellKnownAttributeData()
            Return If(data IsNot Nothing, data.ConditionalSymbols, ImmutableArray(Of String).Empty)
        End Function

        Friend Overrides Sub DecodeWellKnownAttribute(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Dim attrData = arguments.Attribute
            Debug.Assert(Not attrData.HasErrors)

            If attrData.IsTargetAttribute(Me, AttributeDescription.TupleElementNamesAttribute) Then
                arguments.Diagnostics.Add(ERRID.ERR_ExplicitTupleElementNamesAttribute, arguments.AttributeSyntaxOpt.Location)
            End If

            If arguments.SymbolPart = AttributeLocation.Return Then
                ' Decode well-known attributes applied to return value

                DecodeWellKnownAttributeAppliedToReturnValue(arguments)
            Else
                Debug.Assert(arguments.SymbolPart = AttributeLocation.None)

                DecodeWellKnownAttributeAppliedToMethod(arguments)
            End If

            MyBase.DecodeWellKnownAttribute(arguments)
        End Sub

        Private Sub DecodeWellKnownAttributeAppliedToMethod(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            Debug.Assert(arguments.AttributeSyntaxOpt IsNot Nothing)

            ' Decode well-known attributes applied to method
            Dim attrData = arguments.Attribute

            If attrData.IsTargetAttribute(Me, AttributeDescription.CaseInsensitiveExtensionAttribute) Then
                ' Just report errors here. The extension attribute is decoded early.

                If Me.MethodKind <> MethodKind.Ordinary AndAlso Me.MethodKind <> MethodKind.DeclareMethod Then
                    arguments.Diagnostics.Add(ERRID.ERR_ExtensionOnlyAllowedOnModuleSubOrFunction, arguments.AttributeSyntaxOpt.GetLocation())

                ElseIf Not m_containingType.AllowsExtensionMethods() Then
                    arguments.Diagnostics.Add(ERRID.ERR_ExtensionMethodNotInModule, arguments.AttributeSyntaxOpt.GetLocation())

                ElseIf Me.ParameterCount = 0 Then
                    arguments.Diagnostics.Add(ERRID.ERR_ExtensionMethodNoParams, Me.Locations(0))

                Else
                    Debug.Assert(Me.IsShared)

                    Dim firstParam As ParameterSymbol = Me.Parameters(0)

                    If firstParam.IsOptional Then
                        arguments.Diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionMethodOptionalFirstArg), firstParam.Locations(0))

                    ElseIf firstParam.IsParamArray Then
                        arguments.Diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionMethodParamArrayFirstArg), firstParam.Locations(0))

                    ElseIf Not Me.ValidateGenericConstraintsOnExtensionMethodDefinition() Then
                        arguments.Diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_ExtensionMethodUncallable1, Me.Name), Me.Locations(0))

                    End If
                End If

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.WebMethodAttribute) Then

                ' Check for optional parameters
                For Each parameter In Me.Parameters
                    If parameter.IsOptional Then
                        arguments.Diagnostics.Add(ErrorFactory.ErrorInfo(ERRID.ERR_InvalidOptionalParameterUsage1, "WebMethod"), Me.Locations(0))
                    End If
                Next

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.PreserveSigAttribute) Then
                arguments.GetOrCreateData(Of MethodWellKnownAttributeData)().SetPreserveSignature(arguments.Index)

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.MethodImplAttribute) Then
                AttributeData.DecodeMethodImplAttribute(Of MethodWellKnownAttributeData, AttributeSyntax, VisualBasicAttributeData, AttributeLocation)(arguments, MessageProvider.Instance)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.DllImportAttribute) Then
                If Not IsDllImportAttributeAllowed(arguments.AttributeSyntaxOpt, arguments.Diagnostics) Then
                    Return
                End If

                Dim moduleName As String = TryCast(attrData.CommonConstructorArguments(0).Value, String)
                If Not MetadataHelpers.IsValidMetadataIdentifier(moduleName) Then
                    arguments.Diagnostics.Add(ERRID.ERR_BadAttribute1, arguments.AttributeSyntaxOpt.ArgumentList.Arguments(0).GetLocation(), attrData.AttributeClass)
                End If

                ' Default value of charset is inherited from the module (only if specified).
                ' This might be different from ContainingType.DefaultMarshallingCharSet. If the charset is not specified on module
                ' ContainingType.DefaultMarshallingCharSet would be Ansi (the class is emitted with "Ansi" charset metadata flag) 
                ' while the charset in P/Invoke metadata should be "None".
                Dim charSet As CharSet = If(Me.EffectiveDefaultMarshallingCharSet, Microsoft.Cci.Constants.CharSet_None)

                Dim importName As String = Nothing
                Dim preserveSig As Boolean = True
                Dim callingConvention As System.Runtime.InteropServices.CallingConvention = System.Runtime.InteropServices.CallingConvention.Winapi
                Dim setLastError As Boolean = False
                Dim exactSpelling As Boolean = False
                Dim bestFitMapping As Boolean? = Nothing
                Dim throwOnUnmappable As Boolean? = Nothing
                Dim position As Integer = 1

                For Each namedArg In attrData.CommonNamedArguments
                    Select Case namedArg.Key
                        Case "EntryPoint"
                            importName = TryCast(namedArg.Value.Value, String)
                            If Not MetadataHelpers.IsValidMetadataIdentifier(importName) Then
                                arguments.Diagnostics.Add(ERRID.ERR_BadAttribute1, arguments.AttributeSyntaxOpt.ArgumentList.Arguments(position).GetLocation(), attrData.AttributeClass)
                                Return
                            End If

                        Case "CharSet"
                            charSet = namedArg.Value.DecodeValue(Of CharSet)(SpecialType.System_Enum)

                        Case "SetLastError"
                            setLastError = namedArg.Value.DecodeValue(Of Boolean)(SpecialType.System_Boolean)

                        Case "ExactSpelling"
                            exactSpelling = namedArg.Value.DecodeValue(Of Boolean)(SpecialType.System_Boolean)

                        Case "PreserveSig"
                            preserveSig = namedArg.Value.DecodeValue(Of Boolean)(SpecialType.System_Boolean)

                        Case "CallingConvention"
                            callingConvention = namedArg.Value.DecodeValue(Of System.Runtime.InteropServices.CallingConvention)(SpecialType.System_Enum)

                        Case "BestFitMapping"
                            bestFitMapping = namedArg.Value.DecodeValue(Of Boolean)(SpecialType.System_Boolean)

                        Case "ThrowOnUnmappableChar"
                            throwOnUnmappable = namedArg.Value.DecodeValue(Of Boolean)(SpecialType.System_Boolean)

                    End Select

                    position = position + 1
                Next

                Dim data = arguments.GetOrCreateData(Of MethodWellKnownAttributeData)()
                data.SetDllImport(
                    arguments.Index,
                    moduleName,
                    importName,
                    DllImportData.MakeFlags(exactSpelling, charSet, setLastError, callingConvention, bestFitMapping, throwOnUnmappable),
                    preserveSig)

            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SpecialNameAttribute) Then
                arguments.GetOrCreateData(Of MethodWellKnownAttributeData)().HasSpecialNameAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.ExcludeFromCodeCoverageAttribute) Then
                arguments.GetOrCreateData(Of MethodWellKnownAttributeData)().HasExcludeFromCodeCoverageAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SuppressUnmanagedCodeSecurityAttribute) Then
                arguments.GetOrCreateData(Of MethodWellKnownAttributeData)().HasSuppressUnmanagedCodeSecurityAttribute = True
            ElseIf attrData.IsSecurityAttribute(Me.DeclaringCompilation) Then
                attrData.DecodeSecurityAttribute(Of MethodWellKnownAttributeData)(Me, Me.DeclaringCompilation, arguments)
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.STAThreadAttribute) Then
                arguments.GetOrCreateData(Of MethodWellKnownAttributeData)().HasSTAThreadAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.MTAThreadAttribute) Then
                arguments.GetOrCreateData(Of MethodWellKnownAttributeData)().HasMTAThreadAttribute = True
            ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.ConditionalAttribute) Then
                If Not Me.IsSub Then
                    ' BC41007: Attribute 'Conditional' is only valid on 'Sub' declarations.
                    arguments.Diagnostics.Add(ERRID.WRN_ConditionalNotValidOnFunction, Me.Locations(0))
                End If
            ElseIf VerifyObsoleteAttributeAppliedToMethod(arguments, AttributeDescription.ObsoleteAttribute) Then
            ElseIf VerifyObsoleteAttributeAppliedToMethod(arguments, AttributeDescription.DeprecatedAttribute) Then
            Else
                Dim methodImpl As MethodSymbol = If(Me.IsPartial, PartialImplementationPart, Me)

                If methodImpl IsNot Nothing AndAlso (methodImpl.IsAsync OrElse methodImpl.IsIterator) AndAlso Not methodImpl.ContainingType.IsInterfaceType() Then
                    If attrData.IsTargetAttribute(Me, AttributeDescription.SecurityCriticalAttribute) Then
                        Binder.ReportDiagnostic(arguments.Diagnostics, arguments.AttributeSyntaxOpt.GetLocation(), ERRID.ERR_SecurityCriticalAsync, "SecurityCritical")
                        Return
                    ElseIf attrData.IsTargetAttribute(Me, AttributeDescription.SecuritySafeCriticalAttribute) Then
                        Binder.ReportDiagnostic(arguments.Diagnostics, arguments.AttributeSyntaxOpt.GetLocation(), ERRID.ERR_SecurityCriticalAsync, "SecuritySafeCritical")
                        Return
                    End If
                End If
            End If
        End Sub

        Private Function VerifyObsoleteAttributeAppliedToMethod(
            ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation),
            description As AttributeDescription
        ) As Boolean
            If arguments.Attribute.IsTargetAttribute(Me, description) Then
                ' Obsolete Attribute is not allowed on event accessors.
                If Me.IsAccessor() AndAlso Me.AssociatedSymbol.Kind = SymbolKind.Event Then
                    arguments.Diagnostics.Add(ERRID.ERR_ObsoleteInvalidOnEventMember, Me.Locations(0), description.FullName)
                End If

                Return True
            End If

            Return False
        End Function

        Private Sub DecodeWellKnownAttributeAppliedToReturnValue(ByRef arguments As DecodeWellKnownAttributeArguments(Of AttributeSyntax, VisualBasicAttributeData, AttributeLocation))
            ' Decode well-known attributes applied to return value
            Dim attrData = arguments.Attribute
            Debug.Assert(Not attrData.HasErrors)

            If attrData.IsTargetAttribute(Me, AttributeDescription.MarshalAsAttribute) Then
                MarshalAsAttributeDecoder(Of CommonReturnTypeWellKnownAttributeData, AttributeSyntax, VisualBasicAttributeData, AttributeLocation).Decode(arguments, AttributeTargets.ReturnValue, MessageProvider.Instance)
            End If
        End Sub

        Private Function IsDllImportAttributeAllowed(syntax As AttributeSyntax, diagnostics As DiagnosticBag) As Boolean
            Select Case Me.MethodKind
                Case MethodKind.DeclareMethod
                    diagnostics.Add(ERRID.ERR_DllImportNotLegalOnDeclare, syntax.Name.GetLocation())
                    Return False

                Case MethodKind.PropertyGet, MethodKind.PropertySet
                    diagnostics.Add(ERRID.ERR_DllImportNotLegalOnGetOrSet, syntax.Name.GetLocation())
                    Return False

                Case MethodKind.EventAdd, MethodKind.EventRaise, MethodKind.EventRemove
                    diagnostics.Add(ERRID.ERR_DllImportNotLegalOnEventMethod, syntax.Name.GetLocation())
                    Return False
            End Select

            If Me.ContainingType IsNot Nothing AndAlso Me.ContainingType.IsInterface Then
                diagnostics.Add(ERRID.ERR_DllImportOnInterfaceMethod, syntax.Name.GetLocation())
                Return False
            End If

            If Me.IsGenericMethod OrElse (Me.ContainingType IsNot Nothing AndAlso Me.ContainingType.IsGenericType) Then
                diagnostics.Add(ERRID.ERR_DllImportOnGenericSubOrFunction, syntax.Name.GetLocation())
                Return False
            End If

            If Not Me.IsShared Then
                diagnostics.Add(ERRID.ERR_DllImportOnInstanceMethod, syntax.Name.GetLocation())
                Return False
            End If

            Dim methodImpl As SourceMethodSymbol = TryCast(If(Me.IsPartial, PartialImplementationPart, Me), SourceMethodSymbol)

            If methodImpl IsNot Nothing AndAlso
               (methodImpl.IsAsync OrElse methodImpl.IsIterator) AndAlso
               Not methodImpl.ContainingType.IsInterfaceType() Then

                Dim location As Location = methodImpl.NonMergedLocation

                If location IsNot Nothing Then
                    Binder.ReportDiagnostic(diagnostics, location, ERRID.ERR_DllImportOnResumableMethod)
                    Return False
                End If
            End If

            If Not HasEmptyBody Then
                diagnostics.Add(ERRID.ERR_DllImportOnNonEmptySubOrFunction, syntax.Name.GetLocation())
                Return False
            End If

            Return True
        End Function

        Friend Overrides Sub PostDecodeWellKnownAttributes(
            boundAttributes As ImmutableArray(Of VisualBasicAttributeData),
            allAttributeSyntaxNodes As ImmutableArray(Of AttributeSyntax),
            diagnostics As DiagnosticBag,
            symbolPart As AttributeLocation,
            decodedData As WellKnownAttributeData)

            Debug.Assert(Not boundAttributes.IsDefault)
            Debug.Assert(Not allAttributeSyntaxNodes.IsDefault)
            Debug.Assert(boundAttributes.Length = allAttributeSyntaxNodes.Length)
            Debug.Assert(symbolPart = AttributeLocation.Return OrElse symbolPart = AttributeLocation.None)

            If symbolPart <> AttributeLocation.Return Then
                Dim methodData = DirectCast(decodedData, MethodWellKnownAttributeData)
                If methodData IsNot Nothing AndAlso methodData.HasSTAThreadAttribute AndAlso methodData.HasMTAThreadAttribute Then
                    Debug.Assert(Me.NonMergedLocation IsNot Nothing)

                    ' BC31512: 'System.STAThreadAttribute' and 'System.MTAThreadAttribute' cannot both be applied to the same method.
                    diagnostics.Add(ERRID.ERR_STAThreadAndMTAThread0, Me.NonMergedLocation)
                End If
            End If

            MyBase.PostDecodeWellKnownAttributes(boundAttributes, allAttributeSyntaxNodes, diagnostics, symbolPart, decodedData)
        End Sub

        Public Overrides ReadOnly Property IsExtensionMethod As Boolean
            Get
                Dim data As MethodEarlyWellKnownAttributeData = Me.GetEarlyDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.IsExtensionMethod
            End Get
        End Property

        ' Force derived types to override this.
        Friend MustOverride Overrides ReadOnly Property MayBeReducibleExtensionMethod As Boolean

        Public Overrides ReadOnly Property IsExternalMethod As Boolean
            Get
                ' External methods are:
                ' 1) Declare Subs and Declare Functions: IsExternalMethod overridden in SourceDeclareMethodSymbol
                ' 2) methods marked by DllImportAttribute
                ' 3) methods marked by MethodImplAttribute: Runtime and InternalCall methods should not have a body emitted

                Debug.Assert(MethodKind <> MethodKind.DeclareMethod)

                Dim data As MethodWellKnownAttributeData = GetDecodedWellKnownAttributeData()
                If data Is Nothing Then
                    Return False
                End If

                ' p/invoke
                If data.DllImportPlatformInvokeData IsNot Nothing Then
                    Return True
                End If

                ' internal call
                If (data.MethodImplAttributes And Reflection.MethodImplAttributes.InternalCall) <> 0 Then
                    Return True
                End If

                ' runtime
                If (data.MethodImplAttributes And Reflection.MethodImplAttributes.CodeTypeMask) = Reflection.MethodImplAttributes.Runtime Then
                    Return True
                End If

                Return False
            End Get
        End Property

        Public Overrides Function GetDllImportData() As DllImportData
            Dim attributeData = GetDecodedWellKnownAttributeData()
            Return If(attributeData IsNot Nothing, attributeData.DllImportPlatformInvokeData, Nothing)
        End Function

        Friend NotOverridable Overrides ReadOnly Property ReturnTypeMarshallingInformation As MarshalPseudoCustomAttributeData
            Get
                Dim attributeData = GetDecodedReturnTypeWellKnownAttributeData()
                Return If(attributeData IsNot Nothing, attributeData.MarshallingInformation, Nothing)
            End Get
        End Property

        Friend Overrides ReadOnly Property ImplementationAttributes As Reflection.MethodImplAttributes
            Get
                ' Methods of ComImport types are marked as Runtime implemented and InternalCall
                If ContainingType.IsComImport AndAlso Not ContainingType.IsInterface Then
                    Return System.Reflection.MethodImplAttributes.Runtime Or Reflection.MethodImplAttributes.InternalCall
                End If

                Dim attributeData = GetDecodedWellKnownAttributeData()
                Return If(attributeData IsNot Nothing, attributeData.MethodImplAttributes, Nothing)
            End Get
        End Property

        Friend NotOverridable Overrides ReadOnly Property HasDeclarativeSecurity As Boolean
            Get
                Dim attributeData = GetDecodedWellKnownAttributeData()
                Return attributeData IsNot Nothing AndAlso attributeData.HasDeclarativeSecurity
            End Get
        End Property

        Friend NotOverridable Overrides Function GetSecurityInformation() As IEnumerable(Of SecurityAttribute)
            Dim attributesBag As CustomAttributesBag(Of VisualBasicAttributeData) = Me.GetAttributesBag()
            Dim wellKnownAttributeData = DirectCast(attributesBag.DecodedWellKnownAttributeData, MethodWellKnownAttributeData)
            If wellKnownAttributeData IsNot Nothing Then
                Dim securityData As SecurityWellKnownAttributeData = wellKnownAttributeData.SecurityInformation
                If securityData IsNot Nothing Then
                    Return securityData.GetSecurityAttributes(attributesBag.Attributes)
                End If
            End If

            Return SpecializedCollections.EmptyEnumerable(Of SecurityAttribute)()
        End Function

        Friend NotOverridable Overrides ReadOnly Property IsDirectlyExcludedFromCodeCoverage As Boolean
            Get
                Dim data = GetDecodedWellKnownAttributeData()
                Return data IsNot Nothing AndAlso data.HasExcludeFromCodeCoverageAttribute
            End Get
        End Property

        Friend Overrides ReadOnly Property HasRuntimeSpecialName As Boolean
            Get
                Return MyBase.HasRuntimeSpecialName OrElse IsVtableGapInterfaceMethod()
            End Get
        End Property

        Private Function IsVtableGapInterfaceMethod() As Boolean
            Return Me.ContainingType.IsInterface AndAlso
                   ModuleExtensions.GetVTableGapSize(Me.MetadataName) > 0
        End Function

        Friend Overrides ReadOnly Property HasSpecialName As Boolean
            Get
                Select Case Me.MethodKind
                    Case MethodKind.Constructor,
                         MethodKind.SharedConstructor,
                         MethodKind.PropertyGet,
                         MethodKind.PropertySet,
                         MethodKind.EventAdd,
                         MethodKind.EventRemove,
                         MethodKind.EventRaise,
                         MethodKind.Conversion,
                         MethodKind.UserDefinedOperator

                        Return True
                End Select

                If IsVtableGapInterfaceMethod() Then
                    Return True
                End If

                Dim attributeData = GetDecodedWellKnownAttributeData()
                Return attributeData IsNot Nothing AndAlso attributeData.HasSpecialNameAttribute
            End Get
        End Property

        Private ReadOnly Property HasSTAThreadOrMTAThreadAttribute As Boolean
            Get
                ' This property is only accessed during Emit, we must have already bound the attributes.
                Debug.Assert(m_lazyCustomAttributesBag IsNot Nothing AndAlso m_lazyCustomAttributesBag.IsSealed)

                Dim decodedData As MethodWellKnownAttributeData = Me.GetDecodedWellKnownAttributeData()
                Return decodedData IsNot Nothing AndAlso (decodedData.HasSTAThreadAttribute OrElse decodedData.HasMTAThreadAttribute)
            End Get
        End Property

        Friend Overrides ReadOnly Property ObsoleteAttributeData As ObsoleteAttributeData
            Get
                ' If there are no attributes then this symbol is not Obsolete.
                Dim container = TryCast(Me.m_containingType, SourceMemberContainerTypeSymbol)
                If container Is Nothing OrElse Not container.AnyMemberHasAttributes Then
                    Return Nothing
                End If

                Dim lazyCustomAttributesBag = Me.m_lazyCustomAttributesBag
                If (lazyCustomAttributesBag IsNot Nothing AndAlso lazyCustomAttributesBag.IsEarlyDecodedWellKnownAttributeDataComputed) Then
                    Dim data = DirectCast(m_lazyCustomAttributesBag.EarlyDecodedWellKnownAttributeData, MethodEarlyWellKnownAttributeData)
                    Return If(data IsNot Nothing, data.ObsoleteAttributeData, Nothing)
                End If

                Dim reference = Me.DeclaringSyntaxReferences
                If (reference.IsEmpty) Then
                    ' no references -> no attributes
                    Return Nothing
                End If

                Return ObsoleteAttributeData.Uninitialized
            End Get
        End Property
#End Region

        Public MustOverride Overrides ReadOnly Property ReturnType As TypeSymbol

        Public MustOverride Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)

        Friend MustOverride Overrides ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of MethodSymbol)
    End Class

    Friend MustInherit Class SourceNonPropertyAccessorMethodSymbol
        Inherits SourceMethodSymbol

        ' Parameters.
        Private _lazyParameters As ImmutableArray(Of ParameterSymbol)

        ' Return type. Void for a Sub.
        Private _lazyReturnType As TypeSymbol

        ' The overridden or hidden methods.
        Private _lazyOverriddenMethods As OverriddenMembersResult(Of MethodSymbol)

        Protected Sub New(containingType As NamedTypeSymbol,
                          flags As SourceMemberFlags,
                          syntaxRef As SyntaxReference,
                          Optional locations As ImmutableArray(Of Location) = Nothing)
            MyBase.New(containingType, flags, syntaxRef, locations)
        End Sub

        Friend NotOverridable Overrides ReadOnly Property ParameterCount As Integer
            Get
                If Not Me._lazyParameters.IsDefault Then
                    Return Me._lazyParameters.Length
                End If

                Dim decl = Me.DeclarationSyntax
                Dim paramListOpt As ParameterListSyntax

                Select Case decl.Kind
                    Case SyntaxKind.SubNewStatement
                        Dim methodStatement = DirectCast(decl, SubNewStatementSyntax)
                        paramListOpt = methodStatement.ParameterList

                    Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                        Dim methodStatement = DirectCast(decl, MethodStatementSyntax)
                        paramListOpt = methodStatement.ParameterList

                    Case Else
                        Return MyBase.ParameterCount

                End Select

                Return If(paramListOpt Is Nothing, 0, paramListOpt.Parameters.Count)
            End Get
        End Property

        Public NotOverridable Overrides ReadOnly Property Parameters As ImmutableArray(Of ParameterSymbol)
            Get
                EnsureSignature()
                Return _lazyParameters
            End Get
        End Property

        Private Sub EnsureSignature()
            If _lazyParameters.IsDefault Then

                Dim diagBag = DiagnosticBag.GetInstance()
                Dim sourceModule = ContainingSourceModule

                Dim params As ImmutableArray(Of ParameterSymbol) = GetParameters(sourceModule, diagBag)

                Dim errorLocation As SyntaxNodeOrToken = Nothing
                Dim retType As TypeSymbol = GetReturnType(sourceModule, errorLocation, diagBag)
                Debug.Assert(Me.IsAccessor OrElse retType.GetArity() = 0 OrElse Not (errorLocation.IsKind(SyntaxKind.None))) ' if we could have constraint errors, the location better exist.

                ' For an overriding method, we need to copy custom modifiers from the method we override.
                Dim overriddenMembers As OverriddenMembersResult(Of MethodSymbol)

                ' Do not support custom modifiers for properties at the moment.
                If Not Me.IsOverrides OrElse Not OverrideHidingHelper.CanOverrideOrHide(Me) Then
                    overriddenMembers = OverriddenMembersResult(Of MethodSymbol).Empty
                Else
                    ' Since we cannot expose parameters and return type to the outside world yet,
                    ' let's create a fake symbol to use for overriding resolution
                    Dim fakeTypeParameters As ImmutableArray(Of TypeParameterSymbol)
                    Dim replaceMethodTypeParametersWithFakeTypeParameters As TypeSubstitution

                    If Me.Arity > 0 Then
                        fakeTypeParameters = IndexedTypeParameterSymbol.Take(Me.Arity)
                        replaceMethodTypeParametersWithFakeTypeParameters = TypeSubstitution.Create(Me, Me.TypeParameters, StaticCast(Of TypeSymbol).From(fakeTypeParameters))
                    Else
                        fakeTypeParameters = ImmutableArray(Of TypeParameterSymbol).Empty
                        replaceMethodTypeParametersWithFakeTypeParameters = Nothing
                    End If

                    Dim fakeParamsBuilder = ArrayBuilder(Of ParameterSymbol).GetInstance(params.Length)
                    For Each param As ParameterSymbol In params
                        fakeParamsBuilder.Add(New SignatureOnlyParameterSymbol(
                                                param.Type.InternalSubstituteTypeParameters(replaceMethodTypeParametersWithFakeTypeParameters).AsTypeSymbolOnly(),
                                                ImmutableArray(Of CustomModifier).Empty,
                                                ImmutableArray(Of CustomModifier).Empty,
                                                defaultConstantValue:=Nothing,
                                                isParamArray:=False,
                                                isByRef:=param.IsByRef,
                                                isOut:=False,
                                                isOptional:=param.IsOptional))
                    Next

                    overriddenMembers = OverrideHidingHelper(Of MethodSymbol).
                        MakeOverriddenMembers(New SignatureOnlyMethodSymbol(Me.Name, m_containingType, Me.MethodKind,
                                                                            Me.CallingConvention,
                                                                            fakeTypeParameters,
                                                                            fakeParamsBuilder.ToImmutableAndFree(),
                                                                            returnsByRef:=False,
                                                                            returnType:=retType.InternalSubstituteTypeParameters(replaceMethodTypeParametersWithFakeTypeParameters).AsTypeSymbolOnly(),
                                                                            returnTypeCustomModifiers:=ImmutableArray(Of CustomModifier).Empty,
                                                                            refCustomModifiers:=ImmutableArray(Of CustomModifier).Empty,
                                                                            explicitInterfaceImplementations:=ImmutableArray(Of MethodSymbol).Empty,
                                                                            isOverrides:=True))
                End If

                Debug.Assert(IsDefinition)
                Dim overridden = overriddenMembers.OverriddenMember

                If overridden IsNot Nothing Then
                    CustomModifierUtils.CopyMethodCustomModifiers(overridden, Me.TypeArguments, retType, params)
                End If

                ' Unlike MethodSymbol, in SourceMethodSymbol we cache the result of MakeOverriddenOfHiddenMembers, because we use
                ' it heavily while validating methods and emitting.
                Interlocked.CompareExchange(_lazyOverriddenMethods, overriddenMembers, Nothing)

                Interlocked.CompareExchange(_lazyReturnType, retType, Nothing)
                retType = _lazyReturnType

                For Each param In params
                    ' TODO: The check for Locations is to rule out cases such as implicit parameters
                    ' from property accessors but it allows explicit accessor parameters. Is that correct?
                    If param.Locations.Length > 0 Then
                        ' Note: Errors are reported on the parameter name. Ideally, we should
                        ' match Dev10 and report errors on the parameter type syntax instead.
                        param.Type.CheckAllConstraints(param.Locations(0), diagBag)
                    End If
                Next

                If Not errorLocation.IsKind(SyntaxKind.None) Then
                    Dim diagnosticsBuilder = ArrayBuilder(Of TypeParameterDiagnosticInfo).GetInstance()
                    Dim useSiteDiagnosticsBuilder As ArrayBuilder(Of TypeParameterDiagnosticInfo) = Nothing

                    retType.CheckAllConstraints(diagnosticsBuilder, useSiteDiagnosticsBuilder)

                    If useSiteDiagnosticsBuilder IsNot Nothing Then
                        diagnosticsBuilder.AddRange(useSiteDiagnosticsBuilder)
                    End If

                    For Each diag In diagnosticsBuilder
                        diagBag.Add(diag.DiagnosticInfo, errorLocation.GetLocation())
                    Next
                    diagnosticsBuilder.Free()
                End If

                sourceModule.AtomicStoreArrayAndDiagnostics(
                        _lazyParameters,
                        params,
                        diagBag,
                        CompilationStage.Declare)

                diagBag.Free()

            End If
        End Sub

        Private Function CreateBinderForMethodDeclaration(sourceModule As SourceModuleSymbol) As Binder
            Dim binder As Binder = BinderBuilder.CreateBinderForMethodDeclaration(sourceModule, Me.SyntaxTree, Me)

            ' Constraint checking for parameter and return types must be delayed
            ' until the parameter and return type fields have been set since
            ' evaluating constraints may require comparing this method signature
            ' (to find the implemented method for instance).
            Return New LocationSpecificBinder(BindingLocation.MethodSignature, Me, binder)
        End Function

        Protected Overridable Function GetParameters(sourceModule As SourceModuleSymbol,
                                             diagBag As DiagnosticBag) As ImmutableArray(Of ParameterSymbol)

            Dim decl = Me.DeclarationSyntax
            Dim binder As Binder = CreateBinderForMethodDeclaration(sourceModule)

            Dim paramList As ParameterListSyntax

            Select Case decl.Kind
                Case SyntaxKind.SubNewStatement
                    paramList = DirectCast(decl, SubNewStatementSyntax).ParameterList
                Case SyntaxKind.OperatorStatement
                    paramList = DirectCast(decl, OperatorStatementSyntax).ParameterList
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    paramList = DirectCast(decl, MethodStatementSyntax).ParameterList
                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    paramList = DirectCast(decl, DeclareStatementSyntax).ParameterList
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(decl.Kind)
            End Select

            Return binder.DecodeParameterList(Me, False, m_flags, paramList, diagBag)
        End Function

        Public NotOverridable Overrides ReadOnly Property ReturnType As TypeSymbol
            Get
                EnsureSignature()
                Return _lazyReturnType
            End Get
        End Property

        Private Shared Function GetNameToken(methodStatement As MethodBaseSyntax) As SyntaxToken
            Select Case methodStatement.Kind
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(methodStatement, OperatorStatementSyntax).OperatorToken
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Return DirectCast(methodStatement, MethodStatementSyntax).Identifier
                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(methodStatement, DeclareStatementSyntax).Identifier
                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(methodStatement.Kind)
            End Select
        End Function

        Private Function GetReturnType(sourceModule As SourceModuleSymbol,
                                       ByRef errorLocation As SyntaxNodeOrToken,
                                       diagBag As DiagnosticBag) As TypeSymbol
            Dim binder As Binder = CreateBinderForMethodDeclaration(sourceModule)

            Select Case MethodKind
                Case MethodKind.Constructor,
                    MethodKind.SharedConstructor,
                    MethodKind.EventRemove,
                    MethodKind.EventRaise

                    Debug.Assert(Me.IsSub)
                    Return binder.GetSpecialType(SpecialType.System_Void, Syntax, diagBag)

                Case MethodKind.EventAdd
                    Dim isWindowsRuntimeEvent As Boolean = DirectCast(Me.AssociatedSymbol, EventSymbol).IsWindowsRuntimeEvent
                    Return If(isWindowsRuntimeEvent,
                        binder.GetWellKnownType(WellKnownType.System_Runtime_InteropServices_WindowsRuntime_EventRegistrationToken, Syntax, diagBag),
                        binder.GetSpecialType(SpecialType.System_Void, Syntax, diagBag))

                Case MethodKind.PropertyGet, MethodKind.PropertySet
                    Throw ExceptionUtilities.Unreachable

                Case Else
                    Dim methodStatement As MethodBaseSyntax = Me.DeclarationSyntax
                    Dim retType As TypeSymbol

                    Select Case methodStatement.Kind
                        Case SyntaxKind.SubStatement,
                            SyntaxKind.DeclareSubStatement

                            Debug.Assert(Me.IsSub)
                            Binder.DisallowTypeCharacter(GetNameToken(methodStatement), diagBag, ERRID.ERR_TypeCharOnSub)
                            retType = binder.GetSpecialType(SpecialType.System_Void, Syntax, diagBag)
                            errorLocation = methodStatement.DeclarationKeyword

                        Case Else
                            Dim getErrorInfo As Func(Of DiagnosticInfo) = Nothing

                            If binder.OptionStrict = OptionStrict.On Then
                                getErrorInfo = ErrorFactory.GetErrorInfo_ERR_StrictDisallowsImplicitProc
                            ElseIf binder.OptionStrict = OptionStrict.Custom Then
                                If Me.MethodKind = MethodKind.UserDefinedOperator Then
                                    getErrorInfo = ErrorFactory.GetErrorInfo_WRN_ObjectAssumed1_WRN_MissingAsClauseinOperator
                                Else
                                    getErrorInfo = ErrorFactory.GetErrorInfo_WRN_ObjectAssumed1_WRN_MissingAsClauseinFunction
                                End If
                            End If

                            Dim asClause As AsClauseSyntax = methodStatement.AsClauseInternal

                            retType = binder.DecodeIdentifierType(GetNameToken(methodStatement), asClause, getErrorInfo, diagBag)

                            If asClause IsNot Nothing Then
                                errorLocation = asClause.Type
                            Else
                                errorLocation = methodStatement.DeclarationKeyword
                            End If

                    End Select

                    If Not retType.IsErrorType() Then
                        AccessCheck.VerifyAccessExposureForMemberType(Me, errorLocation, retType, diagBag)

                        Dim restrictedType As TypeSymbol = Nothing
                        If retType.IsRestrictedArrayType(restrictedType) Then
                            Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_RestrictedType1, restrictedType)
                        End If

                        If Not (Me.IsAsync AndAlso Me.IsIterator) Then
                            If Me.IsSub Then
                                If Me.IsIterator Then
                                    Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_BadIteratorReturn)
                                End If

                            Else
                                If Me.IsAsync Then
                                    Dim compilation = Me.DeclaringCompilation

                                    If Not retType.OriginalDefinition.Equals(compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task_T)) AndAlso
                                       Not retType.Equals(compilation.GetWellKnownType(WellKnownType.System_Threading_Tasks_Task)) Then
                                        Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_BadAsyncReturn)
                                    End If
                                End If

                                If Me.IsIterator Then
                                    Dim originalRetTypeDef = retType.OriginalDefinition

                                    If originalRetTypeDef.SpecialType <> SpecialType.System_Collections_Generic_IEnumerable_T AndAlso
                                        originalRetTypeDef.SpecialType <> SpecialType.System_Collections_Generic_IEnumerator_T AndAlso
                                        retType.SpecialType <> SpecialType.System_Collections_IEnumerable AndAlso
                                        retType.SpecialType <> SpecialType.System_Collections_IEnumerator Then
                                        Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_BadIteratorReturn)
                                    End If
                                End If
                            End If
                        End If
                    End If

                    Return retType

            End Select
        End Function

        Friend NotOverridable Overrides ReadOnly Property OverriddenMembers As OverriddenMembersResult(Of MethodSymbol)
            Get
                EnsureSignature()
                Return Me._lazyOverriddenMethods
            End Get
        End Property

    End Class

End Namespace
