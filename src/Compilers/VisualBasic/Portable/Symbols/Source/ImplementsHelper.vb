' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Roslyn.Utilities

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Methods, Properties, and Events all have implements clauses and need to handle interface
    ''' implementation. This module has helper methods and extensions for sharing by multiple
    ''' symbol types.
    ''' </summary>
    ''' <remarks></remarks>
    Friend Module ImplementsHelper

        ' Given a property, method, or event symbol, get the explicitly implemented symbols
        Public Function GetExplicitInterfaceImplementations(member As Symbol) As ImmutableArray(Of Symbol)
            Select Case member.Kind
                Case SymbolKind.Method
                    Return StaticCast(Of Symbol).From(DirectCast(member, MethodSymbol).ExplicitInterfaceImplementations)
                Case SymbolKind.Property
                    Return StaticCast(Of Symbol).From(DirectCast(member, PropertySymbol).ExplicitInterfaceImplementations)
                Case SymbolKind.Event
                    Return StaticCast(Of Symbol).From(DirectCast(member, EventSymbol).ExplicitInterfaceImplementations)
                Case Else
                    Return ImmutableArray(Of Symbol).Empty
            End Select
        End Function

        ' Given an implementing symbol, and an implemented symbol, get the location of the 
        ' syntax in the implements clause that matches that implemented symbol. Should only use for 
        ' symbols from source.
        '
        ' Used for error reporting.
        Public Function GetImplementingLocation(sourceSym As Symbol, implementedSym As Symbol) As Location
            Debug.Assert(GetExplicitInterfaceImplementations(sourceSym).Contains(implementedSym))

            Dim sourceMethod = TryCast(sourceSym, SourceMethodSymbol)
            If sourceMethod IsNot Nothing Then
                Return sourceMethod.GetImplementingLocation(DirectCast(implementedSym, MethodSymbol))
            End If

            Dim sourceProperty = TryCast(sourceSym, SourcePropertySymbol)
            If sourceProperty IsNot Nothing Then
                Return sourceProperty.GetImplementingLocation(DirectCast(implementedSym, PropertySymbol))
            End If

            Dim sourceEvent = TryCast(sourceSym, SourceEventSymbol)
            If sourceEvent IsNot Nothing Then
                Return sourceEvent.GetImplementingLocation(DirectCast(implementedSym, EventSymbol))
            End If

            ' Should always pass source symbol into this function
            Throw ExceptionUtilities.Unreachable
        End Function

        ' Given an implements clause syntax on an implementing symbol, and an implemented symbol, find and return the particular name
        ' syntax in the implements clause that matches that implemented symbol, or Nothing if none match.
        '
        ' Used for error reporting.
        Public Function FindImplementingSyntax(Of TSymbol As Symbol)(implementsClause As ImplementsClauseSyntax,
                                                                     implementingSym As TSymbol,
                                                                     implementedSym As TSymbol,
                                                                     container As SourceMemberContainerTypeSymbol,
                                                                     binder As Binder) As QualifiedNameSyntax
            Debug.Assert(implementedSym IsNot Nothing)

            Dim dummyDiagnostics = DiagnosticBag.GetInstance() ' don't care about diagnostics
            Dim dummyResultKind As LookupResultKind

            Try
                ' Bind each syntax again and compare them.
                For Each implementedMethodSyntax As QualifiedNameSyntax In implementsClause.InterfaceMembers
                    Dim implementedMethod As TSymbol = FindExplicitlyImplementedMember(implementingSym, container, implementedMethodSyntax, binder, dummyDiagnostics, Nothing, dummyResultKind)
                    If implementedMethod = implementedSym Then
                        Return implementedMethodSyntax
                    End If
                Next

                Return Nothing
            Finally
                dummyDiagnostics.Free()
            End Try
        End Function

        ' Given a symbol in the process of being constructed, bind the Implements clause
        ' on it and diagnose any errors. Returns the list of implemented members.
        Public Function ProcessImplementsClause(Of TSymbol As Symbol)(implementsClause As ImplementsClauseSyntax,
                                                                      implementingSym As TSymbol,
                                                                      container As SourceMemberContainerTypeSymbol,
                                                                      binder As Binder,
                                                                      diagBag As DiagnosticBag) As ImmutableArray(Of TSymbol)
            Debug.Assert(implementsClause IsNot Nothing)

            If container.IsInterface Then
                ' Members in interfaces cannot have an implements clause (each member has its own error code)
                Dim errorid As ERRID
                If implementingSym.Kind = SymbolKind.Method Then
                    errorid = ERRID.ERR_BadInterfaceMethodFlags1
                ElseIf implementingSym.Kind = SymbolKind.Property Then
                    errorid = ERRID.ERR_BadInterfacePropertyFlags1
                Else
                    errorid = ERRID.ERR_InterfaceCantUseEventSpecifier1
                End If
                Binder.ReportDiagnostic(diagBag, implementsClause, errorid, implementsClause.ImplementsKeyword.ToString())

                Return ImmutableArray(Of TSymbol).Empty
            ElseIf container.IsModuleType Then
                ' Methods in Std Modules can't implement interfaces
                Binder.ReportDiagnostic(diagBag,
                                        implementsClause.ImplementsKeyword,
                                        ERRID.ERR_ModuleMemberCantImplement)

                Return ImmutableArray(Of TSymbol).Empty
            Else
                ' Process the IMPLEMENTS lists
                Dim implementedMembers As ArrayBuilder(Of TSymbol) = ArrayBuilder(Of TSymbol).GetInstance()
                Dim dummyResultKind As LookupResultKind

                Dim firstImplementedMemberIsWindowsRuntimeEvent As ThreeState = ThreeState.Unknown
                Dim implementingSymIsEvent = (implementingSym.Kind = SymbolKind.Event)
                For Each implementedMemberSyntax As QualifiedNameSyntax In implementsClause.InterfaceMembers
                    Dim implementedMember As TSymbol = FindExplicitlyImplementedMember(implementingSym, container, implementedMemberSyntax, binder, diagBag, Nothing, dummyResultKind)
                    If implementedMember IsNot Nothing Then
                        implementedMembers.Add(implementedMember)

                        ' Process Obsolete attribute on implements clause
                        Binder.ReportDiagnosticsIfObsolete(diagBag, implementingSym, implementedMember, implementsClause)

                        If implementingSymIsEvent Then
                            Debug.Assert(implementedMember.Kind = SymbolKind.Event)

                            If Not firstImplementedMemberIsWindowsRuntimeEvent.HasValue() Then
                                firstImplementedMemberIsWindowsRuntimeEvent = TryCast(implementedMember, EventSymbol).IsWindowsRuntimeEvent.ToThreeState()
                            Else
                                Dim currIsWinRT As Boolean = TryCast(implementedMember, EventSymbol).IsWindowsRuntimeEvent
                                Dim firstIsWinRT As Boolean = firstImplementedMemberIsWindowsRuntimeEvent.Value()

                                If currIsWinRT <> firstIsWinRT Then
                                    Binder.ReportDiagnostic(diagBag,
                                                            implementedMemberSyntax,
                                                            ERRID.ERR_MixingWinRTAndNETEvents,
                                                            CustomSymbolDisplayFormatter.ShortErrorName(implementingSym),
                                                            CustomSymbolDisplayFormatter.QualifiedName(If(firstIsWinRT, implementedMembers(0), implementedMember)),
                                                            CustomSymbolDisplayFormatter.QualifiedName(If(firstIsWinRT, implementedMember, implementedMembers(0))))
                                End If
                            End If
                        End If
                    End If
                Next

                Return implementedMembers.ToImmutableAndFree()
            End If
        End Function

        ''' <summary>
        ''' Find the implemented method denoted by "implementedMemberSyntax" that matches implementingSym.
        ''' Returns the implemented method, or Nothing if none.
        ''' 
        ''' Also stores into "candidateSymbols" (if not Nothing) and resultKind the symbols and result kind that
        ''' should be used for semantic model purposes.
        ''' </summary>
        Public Function FindExplicitlyImplementedMember(Of TSymbol As Symbol)(implementingSym As TSymbol,
                                                                              containingType As NamedTypeSymbol,
                                                                              implementedMemberSyntax As QualifiedNameSyntax,
                                                                              binder As Binder,
                                                                              diagBag As DiagnosticBag,
                                                                              candidateSymbols As ArrayBuilder(Of Symbol),
                                                                              ByRef resultKind As LookupResultKind) As TSymbol
            resultKind = LookupResultKind.Good
            Dim interfaceName As NameSyntax = implementedMemberSyntax.Left
            Dim implementedMethodName As String = implementedMemberSyntax.Right.Identifier.ValueText

            Dim interfaceType As TypeSymbol = binder.BindTypeSyntax(interfaceName, diagBag)

            If interfaceType.IsInterfaceType() Then
                Dim errorReported As Boolean = False        ' was an error already reported?
                Dim interfaceNamedType As NamedTypeSymbol = DirectCast(interfaceType, NamedTypeSymbol)

                If Not containingType.InterfacesAndTheirBaseInterfacesNoUseSiteDiagnostics(interfaceNamedType).Contains(interfaceNamedType) Then
                    ' Class doesn't implement the interface that was named
                    Binder.ReportDiagnostic(diagBag, interfaceName, ERRID.ERR_InterfaceNotImplemented1,
                                            interfaceType)
                    resultKind = LookupResultKind.NotReferencable
                    errorReported = True
                    ' continue on...
                End If

                ' Do lookup of the specified name in the interface (note it could be in a base interface thereof)
                Dim lookup As LookupResult = LookupResult.GetInstance()
                Dim foundMember As TSymbol = Nothing   ' the correctly matching method we found

                ' NOTE(cyrusn): We pass 'IgnoreAccessibility' here to provide a better experience
                ' for the IDE.  For correct code it won't matter (as interface members are always
                ' public in correct code).  However, in incorrect code it makes sure we can hook up
                ' the implements clause to a private member.
                Dim options As LookupOptions = LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreAccessibility Or LookupOptions.IgnoreExtensionMethods
                If implementingSym.Kind = SymbolKind.Event Then
                    options = CType(options Or LookupOptions.EventsOnly, LookupOptions)
                End If

                Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing
                binder.LookupMember(lookup, interfaceType, implementedMethodName, -1, options, useSiteDiagnostics)

                If lookup.IsAmbiguous Then
                    Binder.ReportDiagnostic(diagBag, implementedMemberSyntax, ERRID.ERR_AmbiguousImplementsMember3,
                                            implementedMethodName,
                                            implementedMethodName)

                    If candidateSymbols IsNot Nothing Then
                        candidateSymbols.AddRange(DirectCast(lookup.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols)
                    End If
                    resultKind = LookupResult.WorseResultKind(lookup.Kind, LookupResultKind.Ambiguous)

                    errorReported = True
                ElseIf lookup.IsGood Then
                    ' Check each method found to see if it matches signature of methodSym
                    Dim candidates As ArrayBuilder(Of TSymbol) = Nothing

                    For Each possibleMatch In lookup.Symbols
                        Dim possibleMatchMember = TryCast(possibleMatch, TSymbol)
                        If possibleMatchMember IsNot Nothing AndAlso
                           possibleMatchMember.ContainingType.IsInterface AndAlso
                           MembersAreMatchingForPurposesOfInterfaceImplementation(implementingSym, possibleMatchMember) Then
                            If candidates Is Nothing Then
                                candidates = ArrayBuilder(Of TSymbol).GetInstance()
                            End If

                            candidates.Add(possibleMatchMember)
                        End If
                    Next

                    Dim candidatesCount As Integer = If(candidates IsNot Nothing, candidates.Count, 0)

                    ' If we have more than one candidate, eliminate candidates from least derived interfaces
                    If candidatesCount > 1 Then
                        For i As Integer = 0 To candidates.Count - 2
                            Dim first As TSymbol = candidates(i)

                            If first Is Nothing Then
                                Continue For ' has been eliminated already
                            End If

                            For j As Integer = i + 1 To candidates.Count - 1
                                Dim second As TSymbol = candidates(j)

                                If second Is Nothing Then
                                    Continue For ' has been eliminated already
                                End If

                                If second.ContainingType.ImplementsInterface(first.ContainingType, comparer:=Nothing, useSiteDiagnostics:=Nothing) Then
                                    candidates(i) = Nothing
                                    candidatesCount -= 1
                                    GoTo Next_i
                                ElseIf first.ContainingType.ImplementsInterface(second.ContainingType, comparer:=Nothing, useSiteDiagnostics:=Nothing) Then
                                    candidates(j) = Nothing
                                    candidatesCount -= 1
                                End If
                            Next
Next_i:
                        Next
                    End If

                    ' If we still have more than one candidate, they are either from the same type (type substitution can create two methods with same signature),
                    ' or from unrelated base interfaces
                    If candidatesCount > 1 Then
                        For i As Integer = 0 To candidates.Count - 2
                            Dim first As TSymbol = candidates(i)

                            If first Is Nothing Then
                                Continue For ' has been eliminated already
                            End If

                            If foundMember Is Nothing Then
                                foundMember = first
                            End If

                            For j As Integer = i + 1 To candidates.Count - 1
                                Dim second As TSymbol = candidates(j)

                                If second Is Nothing Then
                                    Continue For ' has been eliminated already
                                End If

                                If TypeSymbol.Equals(first.ContainingType, second.ContainingType, TypeCompareKind.ConsiderEverything) Then
                                    ' type substitution can create two methods with same signature in the same type
                                    ' report ambiguity
                                    Binder.ReportDiagnostic(diagBag, implementedMemberSyntax, ERRID.ERR_AmbiguousImplements3,
                                                                    CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(first.ContainingType),
                                                                    implementedMethodName,
                                                                    CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(first.ContainingType),
                                                                    first,
                                                                    second)
                                    errorReported = True
                                    resultKind = LookupResult.WorseResultKind(lookup.Kind, LookupResultKind.OverloadResolutionFailure)

                                    GoTo DoneWithErrorReporting
                                End If
                            Next
                        Next

                        Binder.ReportDiagnostic(diagBag, implementedMemberSyntax, ERRID.ERR_AmbiguousImplementsMember3,
                                    implementedMethodName,
                                    implementedMethodName)

                        resultKind = LookupResult.WorseResultKind(lookup.Kind, LookupResultKind.Ambiguous)
                        errorReported = True
DoneWithErrorReporting:
                        If candidateSymbols IsNot Nothing Then
                            candidateSymbols.AddRange(lookup.Symbols)
                        End If

                    ElseIf candidatesCount = 1 Then

                        For i As Integer = 0 To candidates.Count - 1
                            Dim first As TSymbol = candidates(i)

                            If first Is Nothing Then
                                Continue For ' has been eliminated already
                            End If

                            foundMember = first
                            Exit For
                        Next

                    Else
                        Debug.Assert(candidatesCount = 0)
                        ' No matching members. Remember non-matching members for semantic model questions.
                        If candidateSymbols IsNot Nothing Then
                            candidateSymbols.AddRange(lookup.Symbols)
                        End If
                        resultKind = LookupResult.WorseResultKind(lookup.Kind, LookupResultKind.OverloadResolutionFailure)
                    End If

                    If candidates IsNot Nothing Then
                        candidates.Free()
                    End If

                    If foundMember IsNot Nothing Then
                        Dim coClassContext As Boolean = interfaceNamedType.CoClassType IsNot Nothing
                        If coClassContext AndAlso (implementingSym.Kind = SymbolKind.Event) <> (foundMember.Kind = SymbolKind.Event) Then
                            ' Following Dev11 implementation: in COM Interface context if the implementing symbol 
                            ' is an event and the found candidate is not (or vice versa) we just pretend we didn't 
                            ' find anything and fall back to the default error
                            foundMember = Nothing
                        End If

                        If Not errorReported Then
                            ' Further verification of found method.
                            foundMember = ValidateImplementedMember(implementingSym, foundMember, implementedMemberSyntax, binder, diagBag, interfaceType, implementedMethodName, errorReported)
                        End If

                        If foundMember IsNot Nothing Then
                            ' Record found member for semantic model questions.
                            If candidateSymbols IsNot Nothing Then
                                candidateSymbols.Add(foundMember)
                            End If
                            resultKind = LookupResult.WorseResultKind(resultKind, lookup.Kind)
                            If Not binder.IsAccessible(foundMember, useSiteDiagnostics) Then
                                resultKind = LookupResult.WorseResultKind(resultKind, LookupResultKind.Inaccessible) ' we specified IgnoreAccessibility above.
                                Binder.ReportDiagnostic(diagBag, implementedMemberSyntax, binder.GetInaccessibleErrorInfo(foundMember))
                            End If
                        End If
                    End If
                End If

                diagBag.Add(interfaceName, useSiteDiagnostics)
                lookup.Free()

                If foundMember Is Nothing And Not errorReported Then
                    ' Didn't find a method (or it was otherwise bad in some way)
                    Binder.ReportDiagnostic(diagBag, implementedMemberSyntax, ERRID.ERR_IdentNotMemberOfInterface4,
                                            CustomSymbolDisplayFormatter.ShortErrorName(implementingSym), implementedMethodName,
                                            implementingSym.GetKindText(),
                                            CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(interfaceType))
                End If

                Return foundMember
            ElseIf interfaceType.TypeKind = TypeKind.Error Then
                ' BindType already reported an error, so don't report another one
                Return Nothing
            Else
                ' type is some other type rather than an interface
                Binder.ReportDiagnostic(diagBag, interfaceName, ERRID.ERR_BadImplementsType)
                Return Nothing
            End If
        End Function

        ''' <summary>
        ''' Does 'implementingSym' match 'implementedSym' well enough to be considered a match for interface implementation?
        ''' </summary>
        Private Function MembersAreMatchingForPurposesOfInterfaceImplementation(implementingSym As Symbol,
                                                                                implementedSym As Symbol) As Boolean
            Return MembersAreMatching(implementingSym, implementedSym, Not SymbolComparisonResults.MismatchesForExplicitInterfaceImplementations, EventSignatureComparer.ExplicitEventImplementationComparer)

        End Function

        Private Function MembersHaveMatchingTupleNames(implementingSym As Symbol,
                                                        implementedSym As Symbol) As Boolean

            Return MembersAreMatching(implementingSym, implementedSym, SymbolComparisonResults.TupleNamesMismatch, EventSignatureComparer.ExplicitEventImplementationWithTupleNamesComparer)
        End Function

        Private Function MembersAreMatching(implementingSym As Symbol,
                                            implementedSym As Symbol,
                                            comparisons As SymbolComparisonResults,
                                            eventComparer As EventSignatureComparer) As Boolean
            Debug.Assert(implementingSym.Kind = implementedSym.Kind)

            Select Case implementingSym.Kind
                Case SymbolKind.Method
                    Dim results = MethodSignatureComparer.DetailedCompare(DirectCast(implementedSym, MethodSymbol), DirectCast(implementingSym, MethodSymbol),
                                                                          comparisons,
                                                                          comparisons)
                    Return (results = 0)

                Case SymbolKind.Property
                    Dim results = PropertySignatureComparer.DetailedCompare(DirectCast(implementedSym, PropertySymbol), DirectCast(implementingSym, PropertySymbol),
                                                                            comparisons,
                                                                            comparisons)
                    Return (results = 0)

                Case SymbolKind.Event
                    Return eventComparer.Equals(DirectCast(implementedSym, EventSymbol), DirectCast(implementingSym, EventSymbol))

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(implementingSym.Kind)
            End Select
        End Function

        ''' <summary>
        ''' Perform additional validate of implementedSym and issue diagnostics.
        ''' Return "implementedSym" if the symbol table should record implementedSym as the implemented
        ''' symbol (even if diagnostics were issues). Returns Nothing if the code should not treat
        ''' implementedSym as the implemented symbol.
        ''' </summary>
        Private Function ValidateImplementedMember(Of TSymbol As Symbol)(implementingSym As TSymbol,
                                                                        implementedSym As TSymbol,
                                                                        implementedMemberSyntax As QualifiedNameSyntax,
                                                                        binder As Binder,
                                                                        diagBag As DiagnosticBag,
                                                                        interfaceType As TypeSymbol,
                                                                        implementedMethodName As String,
                                                                        ByRef errorReported As Boolean) As TSymbol

            If Not implementedSym.RequiresImplementation() Then
                ' TODO: Perhaps give ERR_CantImplementNonVirtual3 like Dev10. But, this message seems more
                ' TODO: confusing than useful, so for now, just treat it like a method that doesn't exist.
                Return Nothing
            End If

            ' Validate that implementing property implements all accessors of the implemented property
            If implementedSym.Kind = SymbolKind.Property Then
                Dim implementedProperty As PropertySymbol = TryCast(implementedSym, PropertySymbol)

                Dim implementedPropertyGetMethod As MethodSymbol = implementedProperty.GetMethod
                If Not implementedPropertyGetMethod?.RequiresImplementation() Then
                    implementedPropertyGetMethod = Nothing
                End If

                Dim implementedPropertySetMethod As MethodSymbol = implementedProperty.SetMethod
                If Not implementedPropertySetMethod?.RequiresImplementation() Then
                    implementedPropertySetMethod = Nothing
                End If

                Dim implementingProperty As PropertySymbol = TryCast(implementingSym, PropertySymbol)

                If (implementedPropertyGetMethod IsNot Nothing AndAlso implementingProperty.GetMethod Is Nothing) OrElse
                    (implementedPropertySetMethod IsNot Nothing AndAlso implementingProperty.SetMethod Is Nothing) Then
                    ' "'{0}' cannot be implemented by a {1} property."
                    Binder.ReportDiagnostic(diagBag, implementedMemberSyntax, ERRID.ERR_PropertyDoesntImplementAllAccessors,
                                            implementedProperty,
                                            implementingProperty.GetPropertyKindText())
                    errorReported = True

                ElseIf ((implementedPropertyGetMethod Is Nothing) Xor (implementedPropertySetMethod Is Nothing)) AndAlso
                       implementingProperty.GetMethod IsNot Nothing AndAlso implementingProperty.SetMethod IsNot Nothing Then

                    errorReported = errorReported Or
                                    Not InternalSyntax.Parser.CheckFeatureAvailability(diagBag, implementedMemberSyntax.GetLocation(),
                                        DirectCast(implementedMemberSyntax.SyntaxTree, VisualBasicSyntaxTree).Options.LanguageVersion,
                                        InternalSyntax.Feature.ImplementingReadonlyOrWriteonlyPropertyWithReadwrite)
                End If
            End If

            If implementedSym IsNot Nothing AndAlso implementingSym.ContainsTupleNames() AndAlso
                Not MembersHaveMatchingTupleNames(implementingSym, implementedSym) Then

                ' it is ok to implement with no tuple names, for compatibility with VB 14, but otherwise names should match
                Binder.ReportDiagnostic(diagBag, implementedMemberSyntax, ERRID.ERR_ImplementingInterfaceWithDifferentTupleNames5,
                                        CustomSymbolDisplayFormatter.ShortErrorName(implementingSym),
                                        implementingSym.GetKindText(),
                                        implementedMethodName,
                                        CustomSymbolDisplayFormatter.ShortNameWithTypeArgs(interfaceType),
                                        implementingSym,
                                        implementedSym)

                errorReported = True
            End If

            ' TODO: If implementing event, check that delegate types are consistent, or maybe set the delegate type.  See Dev10 compiler
            ' TODO: in ImplementsSemantics.cpp, Bindable::BindImplements.

            ' Method type parameter constraints are validated later, in ValidateImplementedMethodConstraints,
            ' after the ExplicitInterfaceImplementations property has been set on the implementing method.

            Return implementedSym
        End Function

        ''' <summary>
        ''' Validate method type parameter constraints. This is handled outside
        ''' of ValidateImplementedMember because that method is invoked
        ''' while computing the ExplicitInterfaceImplementations value on the
        ''' implementing method, but method type parameters rely on the value
        ''' of ExplicitInterfaceImplementations to determine constraints correctly.
        ''' </summary>
        Public Sub ValidateImplementedMethodConstraints(implementingMethod As SourceMethodSymbol,
                                                        implementedMethod As MethodSymbol,
                                                        diagBag As DiagnosticBag)
            If Not MethodSignatureComparer.HaveSameConstraints(implementedMethod, implementingMethod) Then
                ' "'{0}' cannot implement '{1}.{2}' because they differ by type parameter constraints."
                Dim loc = implementingMethod.GetImplementingLocation(implementedMethod)
                diagBag.Add(
                    ErrorFactory.ErrorInfo(ERRID.ERR_ImplementsWithConstraintMismatch3, implementingMethod, implementedMethod.ContainingType, implementedMethod),
                    loc)
            End If
        End Sub

        ''' <summary>
        ''' Performs interface mapping to determine which symbol in this type or a base type
        ''' actually implements a particular interface member.
        ''' </summary>
        ''' <typeparam name="TSymbol">MethodSymbol or PropertySymbol or EventSymbol (an interface member).</typeparam>
        ''' <param name="interfaceMember">A non-null member on an interface type.</param>
        ''' <param name="implementingType">The type implementing the interface member.</param>
        ''' <param name="comparer">A comparer for comparing signatures of TSymbol according to metadata implementation rules.</param>
        ''' <returns>The implementing member or Nothing, if there isn't one.</returns>
        Public Function ComputeImplementationForInterfaceMember(Of TSymbol As Symbol)(interfaceMember As TSymbol,
                                                                                      implementingType As TypeSymbol,
                                                                                      comparer As IEqualityComparer(Of TSymbol)) As TSymbol
            Debug.Assert(TypeOf interfaceMember Is PropertySymbol OrElse
                         TypeOf interfaceMember Is MethodSymbol OrElse
                         TypeOf interfaceMember Is EventSymbol)

            Dim interfaceType As NamedTypeSymbol = interfaceMember.ContainingType
            Debug.Assert(interfaceType IsNot Nothing AndAlso interfaceType.IsInterface)
            Dim seenMDTypeDeclaringInterface As Boolean = False

            Dim currType As TypeSymbol = implementingType

            ' Go up the inheritance chain, looking for an implementation of the member.

            While currType IsNot Nothing
                ' First, check for explicit interface implementation.
                Dim currTypeExplicitImpl As MultiDictionary(Of Symbol, Symbol).ValueSet = currType.ExplicitInterfaceImplementationMap(interfaceMember)
                If currTypeExplicitImpl.Count = 1 Then
                    Return DirectCast(currTypeExplicitImpl.Single(), TSymbol)
                ElseIf currTypeExplicitImpl.Count > 1 Then
                    Return Nothing
                End If

                ' VB only supports explicit interface implementation, but for the purpose of finding implementation, we must
                ' check implicit implementation for members from metadata. We only want to consider metadata implementations 
                ' if a metadata implementation (or a derived metadata implementation) actually implements the given interface 
                ' (not a derived interface), since this is the metadata rule from Partition II, section 12.2.
                '
                ' Consider:
                '     Interface IGoo ' from metadata
                '         Sub Goo()
                '     Class A ' from metadata
                '         Public Sub Goo()
                '     Class B: Inherits A: Implements IGoo ' from metadata
                '     Class C: Inherits B ' from metadata
                '         Public Shadows Sub Goo()
                '     Class D: Inherits C: Implements IGoo  ' from source
                ' In this case, A.Goo is the correct implementation of IGoo.Goo within D.

                ' NOTE: Ideally, we'd like to distinguish between the "current" compilation and other assemblies 
                ' (including other compilations), rather than source and metadata, but there are two reasons that
                ' that won't work in this case:
                '   1) We really don't want consumers of the API to have to pass in the current compilation when
                '   they ask questions about interface implementation.
                '   2) NamedTypeSymbol.Interfaces does not round-trip in the presence of implicit interface
                '   implementations.  As in dev11, we drop interfaces from the interface list if any of their
                '   members are implemented in a base type (so that CLR implicit implementation will pick the
                '   same method as the VB language).
                If Not currType.Dangerous_IsFromSomeCompilationIncludingRetargeting AndAlso
                   currType.InterfacesNoUseSiteDiagnostics.Contains(interfaceType, EqualsIgnoringComparer.InstanceCLRSignatureCompare) Then
                    seenMDTypeDeclaringInterface = True
                End If

                If seenMDTypeDeclaringInterface Then
                    'check for implicit impls (name must match)
                    Dim currTypeImplicitImpl As TSymbol
                    currTypeImplicitImpl = FindImplicitImplementationDeclaredInType(interfaceMember, currType, comparer)
                    If currTypeImplicitImpl IsNot Nothing Then
                        Return currTypeImplicitImpl
                    End If
                End If

                currType = currType.BaseTypeNoUseSiteDiagnostics
            End While

            Return Nothing
        End Function

        ''' <summary>
        ''' Search the declared methods of a type for one that could be an implicit implementation
        ''' of a given interface method (depending on interface declarations). It is assumed that the implementing
        ''' type is not a source type.
        ''' </summary>
        ''' <typeparam name="TSymbol">MethodSymbol or PropertySymbol or EventSymbol (an interface member).</typeparam>
        ''' <param name="interfaceMember">The interface member being implemented.</param>
        ''' <param name="currType">The type on which we are looking for a declared implementation of the interface method.</param>
        ''' <param name="comparer">A comparer for comparing signatures of TSymbol according to metadata implementation rules.</param>
        Private Function FindImplicitImplementationDeclaredInType(Of TSymbol As Symbol)(interfaceMember As TSymbol,
                                                                                        currType As TypeSymbol,
                                                                                        comparer As IEqualityComparer(Of TSymbol)) As TSymbol '
            Debug.Assert(Not currType.Dangerous_IsFromSomeCompilationIncludingRetargeting)

            For Each member In currType.GetMembers(interfaceMember.Name)
                If member.DeclaredAccessibility = Accessibility.Public AndAlso
                   Not member.IsShared AndAlso
                   TypeOf member Is TSymbol AndAlso
                   comparer.Equals(interfaceMember, DirectCast(member, TSymbol)) Then

                    Return DirectCast(member, TSymbol)
                End If
            Next

            Return Nothing
        End Function

        ''' <summary>
        ''' Given a set of explicit interface implementations that are undergoing substitution, return the substituted versions.
        ''' </summary>
        ''' <typeparam name="TSymbol">Type of the interface members (Method, Property, Event)</typeparam>
        ''' <param name="unsubstitutedImplementations">The ROA of members that are being implemented</param>
        ''' <param name="substitution">The type substitution</param>
        ''' <returns>The substituted members.</returns>
        Public Function SubstituteExplicitInterfaceImplementations(Of TSymbol As Symbol)(unsubstitutedImplementations As ImmutableArray(Of TSymbol),
                                                                                         substitution As TypeSubstitution) As ImmutableArray(Of TSymbol)
            If unsubstitutedImplementations.Length = 0 Then
                Return ImmutableArray(Of TSymbol).Empty
            Else
                Dim substitutedImplementations(0 To unsubstitutedImplementations.Length - 1) As TSymbol
                For i As Integer = 0 To unsubstitutedImplementations.Length - 1
                    Dim unsubstitutedMember As TSymbol = unsubstitutedImplementations(i)
                    Dim unsubstitutedInterfaceType = unsubstitutedMember.ContainingType
                    substitutedImplementations(i) = unsubstitutedImplementations(i) ' default: no substitution necessary

                    If unsubstitutedInterfaceType.IsGenericType Then
                        Dim substitutedInterfaceType = TryCast(unsubstitutedInterfaceType.InternalSubstituteTypeParameters(substitution).AsTypeSymbolOnly(), SubstitutedNamedType)

                        If substitutedInterfaceType IsNot Nothing Then
                            ' Get the substituted version of the member
                            substitutedImplementations(i) = DirectCast(substitutedInterfaceType.GetMemberForDefinition(unsubstitutedMember.OriginalDefinition), TSymbol)
                        End If
                    End If
                Next

                Return ImmutableArray.Create(Of TSymbol)(substitutedImplementations)
            End If
        End Function


    End Module
End Namespace

