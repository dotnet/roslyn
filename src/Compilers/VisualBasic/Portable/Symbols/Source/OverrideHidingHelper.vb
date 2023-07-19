' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Diagnostics
Imports System.Runtime.InteropServices
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    ''' <summary>
    ''' Methods, Properties, and Events all can override or hide members. 
    ''' This class has helper methods and extensions for sharing by multiple symbol types.
    ''' </summary>
    Friend Class OverrideHidingHelper
        ''' <summary>
        ''' Check for overriding and hiding errors in container and report them via diagnostics.
        ''' </summary>
        ''' <param name="container">Containing type to check. Should be an original definition.</param>
        ''' <param name="diagnostics">Place diagnostics here.</param>
        Public Shared Sub CheckHidingAndOverridingForType(container As SourceMemberContainerTypeSymbol, diagnostics As BindingDiagnosticBag)
            Debug.Assert(container.IsDefinition) ' Don't do this on constructed types

            Select Case container.TypeKind
                Case TypeKind.Class, TypeKind.Interface, TypeKind.Structure
                    CheckMembersAgainstBaseType(container, diagnostics)
                    CheckAllAbstractsAreOverriddenAndNotHidden(container, diagnostics)

                Case Else
                    ' Modules, Enums and Delegates have nothing to do.
            End Select
        End Sub

        ' Determine if two method or property signatures match, using the rules in 4.1.1, i.e., ByRef mismatches, 
        ' differences in optional parameters, custom modifiers, return type are not considered. An optional parameter
        ' matches if the corresponding parameter in the other signature is not there, optional of any type, 
        ' or non-optional of matching type.
        '
        ' Note that this sense of matching is not transitive. I.e.
        '   A)  f(x as integer)
        '   B)  f(x as integer, optional y as String = "")
        '   C)  f(x as integer, y As String)
        ' A matches B, B matches C, but A doesn't match C
        '
        ' Note that (A) and (B) above do match in terms of Dev10 behavior when we look for overridden 
        ' methods/properties. We still keep this behavior in Roslyn to be able to generate the same 
        ' error in the following case:
        '
        '   Class Base
        '       Public Overridable Sub f(x As Integer)
        '       End Sub
        '   End Class
        '
        '   Class Derived
        '       Inherits Base
        '       Public Overrides Sub f(x As Integer, Optional y As String = "")
        '       End Sub
        '   End Class
        '
        ' >>> error BC30308: 'Public Overrides Sub f(x As Integer, [y As String = ""])' cannot override 
        '                    'Public Overridable Sub f(x As Integer)' because they differ by optional parameters.
        '
        ' In this sense the method returns True if signatures match enough to be 
        ' considered a candidate of overridden member.
        '
        ' But for new overloading rules (overloading based on optional parameters) introduced in Dev11 
        ' we also need more detailed info on the two members being compared, namely do their signatures 
        ' also match taking into account total parameter count and parameter optionality (optional/required)? 
        ' We return this information in 'exactMatch' parameter.
        '
        ' So when searching for overridden members we prefer exactly matched candidates in case we could 
        ' find them. This helps properly find overridden members in the following case:
        '
        '   Class Base
        '       Public Overridable Sub f(x As Integer)
        '       End Sub
        '       Public Overridable Sub f(x As Integer, Optional y As String = "")
        '       End Sub
        '   End Class
        '
        '   Class Derived
        '       Inherits Base
        '       Public Overrides Sub f(x As Integer) 
        '       End Sub
        '       Public Overrides Sub f(x As Integer, Optional y As String = "")  ' << Dev11 Beta reports BC30308
        '       End Sub
        '   End Class
        '
        ' Note that Dev11 Beta wrongly reports BC30308 on the last Sub in this case.
        '
        Public Shared Function SignaturesMatch(sym1 As Symbol, sym2 As Symbol, <Out()> ByRef exactMatch As Boolean, <Out()> ByRef exactMatchIgnoringCustomModifiers As Boolean) As Boolean
            ' NOTE: we should NOT ignore extra required parameters as for overloading
            Const mismatchesForOverriding As SymbolComparisonResults =
                (SymbolComparisonResults.AllMismatches And (Not SymbolComparisonResults.MismatchesForConflictingMethods)) Or
                SymbolComparisonResults.CustomModifierMismatch

            ' 'Exact match' means that the number of parameters and 
            ' parameter 'optionality' match on two symbol candidates.
            Const exactMatchIgnoringCustomModifiersMask As SymbolComparisonResults =
                SymbolComparisonResults.TotalParameterCountMismatch Or SymbolComparisonResults.OptionalParameterTypeMismatch

            ' Note that exact match doesn't care about tuple element names.
            Const exactMatchMask As SymbolComparisonResults =
                exactMatchIgnoringCustomModifiersMask Or SymbolComparisonResults.CustomModifierMismatch

            Dim results As SymbolComparisonResults = DetailedSignatureCompare(sym1, sym2, mismatchesForOverriding)

            ' no match
            If (results And Not exactMatchMask) <> 0 Then
                exactMatch = False
                exactMatchIgnoringCustomModifiers = False
                Return False
            End If

            ' match
            exactMatch = (results And exactMatchMask) = 0
            exactMatchIgnoringCustomModifiers = (results And exactMatchIgnoringCustomModifiersMask) = 0

            Debug.Assert(Not exactMatch OrElse exactMatchIgnoringCustomModifiers)
            Return True
        End Function

        Friend Shared Function DetailedSignatureCompare(
            sym1 As Symbol,
            sym2 As Symbol,
            comparisons As SymbolComparisonResults,
            Optional stopIfAny As SymbolComparisonResults = 0
        ) As SymbolComparisonResults
            If sym1.Kind = SymbolKind.Property Then
                Return PropertySignatureComparer.DetailedCompare(DirectCast(sym1, PropertySymbol), DirectCast(sym2, PropertySymbol), comparisons, stopIfAny)
            Else
                Return MethodSignatureComparer.DetailedCompare(DirectCast(sym1, MethodSymbol), DirectCast(sym2, MethodSymbol), comparisons, stopIfAny)
            End If
        End Function

        ''' <summary>
        ''' Check each member of container for constraints against the base type. For methods and properties and events,
        ''' checking overriding and hiding constraints. For other members, just check for hiding issues.
        ''' </summary>
        ''' <param name="container">Containing type to check. Should be an original definition.</param>
        ''' <param name="diagnostics">Place diagnostics here.</param>
        ''' <remarks></remarks>
        Private Shared Sub CheckMembersAgainstBaseType(container As SourceMemberContainerTypeSymbol, diagnostics As BindingDiagnosticBag)
            For Each member In container.GetMembers()
                If CanOverrideOrHide(member) Then
                    Select Case member.Kind
                        Case SymbolKind.Method
                            Dim methodMember = DirectCast(member, MethodSymbol)
                            If Not methodMember.IsAccessor Then
                                If methodMember.IsOverrides Then
                                    OverrideHidingHelper(Of MethodSymbol).CheckOverrideMember(methodMember, methodMember.OverriddenMembers, diagnostics)
                                ElseIf methodMember.IsNotOverridable Then
                                    'Method is not marked as Overrides but is marked as Not Overridable
                                    diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_NotOverridableRequiresOverrides), methodMember.Locations(0)))
                                End If
                            End If
                        Case SymbolKind.Property
                            Dim propMember = DirectCast(member, PropertySymbol)
                            If propMember.IsOverrides Then
                                OverrideHidingHelper(Of PropertySymbol).CheckOverrideMember(propMember, propMember.OverriddenMembers, diagnostics)
                            ElseIf propMember.IsNotOverridable Then
                                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_NotOverridableRequiresOverrides), propMember.Locations(0)))
                            End If
                    End Select

                    ' TODO: only do this check if CheckOverrideMember didn't find an error?
                    CheckShadowing(container, member, diagnostics)
                End If
            Next
        End Sub

        ''' <summary>
        ''' If the "container" is a non-MustInherit, make sure it has no MustOverride Members
        ''' If "container" is a non-MustInherit inheriting from a MustInherit, make sure that all MustOverride members
        ''' have been overridden.
        ''' If "container" is a MustInherit inheriting from a MustInherit, make sure that no MustOverride members
        ''' have been shadowed.
        ''' </summary>
        Private Shared Sub CheckAllAbstractsAreOverriddenAndNotHidden(container As NamedTypeSymbol, diagnostics As BindingDiagnosticBag)

            ' Check that a non-MustInherit class doesn't have any MustOverride members
            If Not (container.IsMustInherit OrElse container.IsNotInheritable) Then
                For Each member In container.GetMembers()
                    If member.IsMustOverride Then
                        diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_MustOverridesInClass1, container.Name), container.Locations(0)))
                        Exit For
                    End If
                Next
            End If

            Dim baseType As NamedTypeSymbol = container.BaseTypeNoUseSiteDiagnostics

            If baseType IsNot Nothing AndAlso baseType.IsMustInherit Then
                ' Check that all MustOverride members in baseType or one of its bases are overridden/not shadowed somewhere along the chain.
                ' Do this by accumulating a set of all the methods that have been overridden, if we encounter a MustOverride
                ' method that is not in the set, then report it. We can do this in a single pass up the base chain.

                Dim overriddenMembers As HashSet(Of Symbol) = New HashSet(Of Symbol)()
                Dim unimplementedMembers As ArrayBuilder(Of Symbol) = ArrayBuilder(Of Symbol).GetInstance()

                Dim currType = container
                While currType IsNot Nothing
                    For Each member In currType.GetMembers()
                        If CanOverrideOrHide(member) AndAlso Not member.IsAccessor Then  ' accessors handled by their containing properties.
                            If member.IsOverrides Then
                                Dim overriddenMember = GetOverriddenMember(member)
                                If overriddenMember IsNot Nothing Then
                                    overriddenMembers.Add(GetOverriddenMember(member))
                                End If
                            End If

                            If member.IsMustOverride AndAlso currType IsNot container Then
                                If Not overriddenMembers.Contains(member) Then
                                    unimplementedMembers.Add(member)
                                End If
                            End If

                        End If
                    Next

                    currType = currType.BaseTypeNoUseSiteDiagnostics
                End While

                If unimplementedMembers.Any Then
                    If container.IsMustInherit Then
                        ' It is OK for a IsMustInherit type to have unimplemented abstract members. But, it is not allowed
                        ' to shadow them. Check each one to see if it is shadowed by a member of "container". Don't report for
                        ' accessor hiding accessor, because we'll report it on the property.
                        Dim hidingSymbols As New HashSet(Of Symbol) ' don't report more than once per hiding symbols

                        For Each mustOverrideMember In unimplementedMembers
                            For Each hidingMember In container.GetMembers(mustOverrideMember.Name)
                                If DoesHide(hidingMember, mustOverrideMember) AndAlso Not hidingSymbols.Contains(hidingMember) Then
                                    ReportShadowingMustOverrideError(hidingMember, mustOverrideMember, diagnostics)
                                    hidingSymbols.Add(hidingMember)
                                End If
                            Next
                        Next
                    Else
                        ' This is not a IsMustInherit type. Some members should be been overridden but weren't.
                        ' Create a single error that lists all of the unimplemented members.
                        Dim diagnosticInfos = ArrayBuilder(Of DiagnosticInfo).GetInstance(unimplementedMembers.Count)

                        For Each member In unimplementedMembers
                            If Not member.IsAccessor Then
                                If member.Kind = SymbolKind.Event Then
                                    diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_MustInheritEventNotOverridden,
                                                                                            member,
                                                                                            CustomSymbolDisplayFormatter.QualifiedName(member.ContainingType),
                                                                                            CustomSymbolDisplayFormatter.ShortErrorName(container)),
                                                    container.Locations(0)))
                                Else
                                    diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_UnimplementedMustOverride, member.ContainingType, member))
                                End If
                            Else
                                ' accessor is reported on the containing property.
                                Debug.Assert(unimplementedMembers.Contains(DirectCast(member, MethodSymbol).AssociatedSymbol))
                            End If
                        Next

                        If diagnosticInfos.Count > 0 Then
                            diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_BaseOnlyClassesMustBeExplicit2,
                                                                   CustomSymbolDisplayFormatter.ShortErrorName(container),
                                                                   New CompoundDiagnosticInfo(diagnosticInfos.ToArrayAndFree())),
                                                       container.Locations(0)))
                        Else
                            diagnosticInfos.Free()
                        End If
                    End If
                End If

                unimplementedMembers.Free()
            End If
        End Sub

        ' Compare two symbols of the same name to see if one actually does hide the other.
        Private Shared Function DoesHide(hidingMember As Symbol, hiddenMember As Symbol) As Boolean
            Debug.Assert(IdentifierComparison.Equals(hidingMember.Name, hiddenMember.Name))

            Select Case hidingMember.Kind
                Case SymbolKind.Method
                    If hidingMember.IsOverloads AndAlso hiddenMember.Kind = SymbolKind.Method Then
                        Dim hidingMethod = DirectCast(hidingMember, MethodSymbol)
                        If hidingMethod.IsOverrides Then
                            ' For Dev10 compatibility, an override is not considered as hiding (see bug 11728)
                            Return False
                        Else
                            Dim exactMatchIgnoringCustomModifiers As Boolean = False
                            Return OverrideHidingHelper(Of MethodSymbol).SignaturesMatch(hidingMethod, DirectCast(hiddenMember, MethodSymbol), Nothing, exactMatchIgnoringCustomModifiers) AndAlso exactMatchIgnoringCustomModifiers
                        End If
                    Else
                        Return True
                    End If

                Case SymbolKind.Property
                    If hidingMember.IsOverloads AndAlso hiddenMember.Kind = SymbolKind.Property Then
                        Dim hidingProperty = DirectCast(hidingMember, PropertySymbol)
                        If hidingProperty.IsOverrides Then
                            ' For Dev10 compatibility, an override is not considered as hiding (see bug 11728)
                            Return False
                        Else
                            Dim exactMatchIgnoringCustomModifiers As Boolean = False
                            Return OverrideHidingHelper(Of PropertySymbol).SignaturesMatch(hidingProperty, DirectCast(hiddenMember, PropertySymbol), Nothing, exactMatchIgnoringCustomModifiers) AndAlso exactMatchIgnoringCustomModifiers
                        End If
                    Else
                        Return True
                    End If

                Case Else
                    Return True
            End Select
        End Function

        ''' <summary>
        ''' Report any diagnostics related to shadowing for a member.
        ''' </summary>
        Protected Shared Sub CheckShadowing(container As SourceMemberContainerTypeSymbol,
                                            member As Symbol,
                                            diagnostics As BindingDiagnosticBag)
            Dim memberIsOverloads = member.IsOverloads()
            Dim warnForHiddenMember As Boolean = Not member.ShadowsExplicitly

            If Not warnForHiddenMember Then
                Return ' short circuit unnecessary checks.
            End If

            If container.IsInterfaceType() Then
                For Each currentBaseInterface In container.AllInterfacesNoUseSiteDiagnostics
                    CheckShadowingInBaseType(container, member, memberIsOverloads, currentBaseInterface, diagnostics, warnForHiddenMember)
                Next
            Else
                Dim currentBase As NamedTypeSymbol = container.BaseTypeNoUseSiteDiagnostics
                While currentBase IsNot Nothing
                    CheckShadowingInBaseType(container, member, memberIsOverloads, currentBase, diagnostics, warnForHiddenMember)

                    currentBase = currentBase.BaseTypeNoUseSiteDiagnostics
                End While
            End If
        End Sub

        ' Check shadowing against members in one base type.
        Private Shared Sub CheckShadowingInBaseType(container As SourceMemberContainerTypeSymbol,
                                                    member As Symbol,
                                                    memberIsOverloads As Boolean,
                                                    baseType As NamedTypeSymbol,
                                                    diagnostics As BindingDiagnosticBag,
                                                    ByRef warnForHiddenMember As Boolean)
            Debug.Assert(container.IsDefinition)

            If warnForHiddenMember Then
                For Each hiddenMember In baseType.GetMembers(member.Name)
                    If AccessCheck.IsSymbolAccessible(hiddenMember, container, Nothing, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded) AndAlso
                       (Not memberIsOverloads OrElse
                        hiddenMember.Kind <> member.Kind OrElse
                        hiddenMember.IsWithEventsProperty OrElse
                        (member.Kind = SymbolKind.Method AndAlso DirectCast(member, MethodSymbol).IsUserDefinedOperator() <> DirectCast(hiddenMember, MethodSymbol).IsUserDefinedOperator()) OrElse
                        member.IsAccessor() <> hiddenMember.IsAccessor) AndAlso
                       Not (member.IsAccessor() AndAlso hiddenMember.IsAccessor) Then

                        'special case for classes of different arity . Do not warn in such case
                        If member.Kind = SymbolKind.NamedType AndAlso
                            hiddenMember.Kind = SymbolKind.NamedType AndAlso
                            member.GetArity <> hiddenMember.GetArity Then

                            Continue For
                        End If

                        ' Found an accessible member we are hiding and not overloading.
                        ' We don't warn if accessor hides accessor, because we will warn on the containing properties instead.

                        ' Give warning for shadowing hidden member
                        ReportShadowingDiagnostic(member, hiddenMember, diagnostics)
                        warnForHiddenMember = False  ' don't warn for more than one hidden member.
                        Exit For
                    End If
                Next
            End If
        End Sub

        ' Report diagnostic for one member shadowing another, but no Shadows modifier was present.
        Private Shared Sub ReportShadowingDiagnostic(hidingMember As Symbol,
                                                     hiddenMember As Symbol,
                                                     diagnostics As BindingDiagnosticBag)

            Debug.Assert(Not (hidingMember.IsAccessor() AndAlso hiddenMember.IsAccessor))

            Dim associatedhiddenSymbol = hiddenMember.ImplicitlyDefinedBy
            If associatedhiddenSymbol Is Nothing AndAlso hiddenMember.IsUserDefinedOperator() AndAlso Not hidingMember.IsUserDefinedOperator() Then
                ' For the purpose of this check, operator methods are treated as implicitly defined by themselves.
                associatedhiddenSymbol = hiddenMember
            End If

            Dim associatedhidingSymbol = hidingMember.ImplicitlyDefinedBy
            If associatedhidingSymbol Is Nothing AndAlso hidingMember.IsUserDefinedOperator() AndAlso Not hiddenMember.IsUserDefinedOperator() Then
                ' For the purpose of this check, operator methods are treated as implicitly defined by themselves.
                associatedhidingSymbol = hidingMember
            End If

            If associatedhiddenSymbol IsNot Nothing Then
                If associatedhidingSymbol IsNot Nothing Then
                    If Not IdentifierComparison.Equals(associatedhiddenSymbol.Name,
                                                       associatedhidingSymbol.Name) Then
                        ' both members are defined implicitly by members of different names
                        diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_SynthMemberShadowsSynthMember7,
                                           associatedhidingSymbol.GetKindText(),
                                           AssociatedSymbolName(associatedhidingSymbol),
                                           hidingMember.Name,
                                           associatedhiddenSymbol.GetKindText(),
                                           AssociatedSymbolName(associatedhiddenSymbol),
                                           hiddenMember.ContainingType.GetKindText(),
                                           CustomSymbolDisplayFormatter.ShortErrorName(hiddenMember.ContainingType)),
                               hidingMember.Locations(0)))
                    End If

                    Return
                End If
                ' explicitly defined member hiding implicitly defined member
                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_MemberShadowsSynthMember6,
                                                           hidingMember.GetKindText(), hidingMember.Name,
                                                           associatedhiddenSymbol.GetKindText(), AssociatedSymbolName(associatedhiddenSymbol), hiddenMember.ContainingType.GetKindText(),
                                                           CustomSymbolDisplayFormatter.ShortErrorName(hiddenMember.ContainingType)),
                                               hidingMember.Locations(0)))
            ElseIf associatedhidingSymbol IsNot Nothing Then
                ' implicitly defined member hiding explicitly defined member
                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_SynthMemberShadowsMember5,
                                                           associatedhidingSymbol.GetKindText(), AssociatedSymbolName(associatedhidingSymbol),
                                                           hidingMember.Name, hiddenMember.ContainingType.GetKindText(),
                                                           CustomSymbolDisplayFormatter.ShortErrorName(hiddenMember.ContainingType)),
                                               associatedhidingSymbol.Locations(0)))
            ElseIf hidingMember.Kind = hiddenMember.Kind AndAlso
                (hidingMember.Kind = SymbolKind.Property OrElse hidingMember.Kind = SymbolKind.Method) AndAlso
                Not (hiddenMember.IsWithEventsProperty OrElse hidingMember.IsWithEventsProperty) Then

                ' method hiding method or property hiding property; message depends on if hidden symbol is overridable.
                Dim id As ERRID
                If hiddenMember.IsOverridable OrElse hiddenMember.IsOverrides OrElse (hiddenMember.IsMustOverride AndAlso Not hiddenMember.ContainingType.IsInterface) Then
                    id = ERRID.WRN_MustOverride2
                Else
                    id = ERRID.WRN_MustOverloadBase4
                End If

                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(id,
                                                           hidingMember.GetKindText(), hidingMember.Name, hiddenMember.ContainingType.GetKindText(),
                                                           CustomSymbolDisplayFormatter.ShortErrorName(hiddenMember.ContainingType)),
                                               hidingMember.Locations(0)))
            Else
                ' all other hiding scenarios.
                Debug.Assert(hidingMember.Locations(0).IsInSource)
                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_OverrideType5,
                                                           hidingMember.GetKindText(), hidingMember.Name, hiddenMember.GetKindText(), hiddenMember.ContainingType.GetKindText(),
                                                           CustomSymbolDisplayFormatter.ShortErrorName(hiddenMember.ContainingType)),
                                               hidingMember.Locations(0)))
            End If

        End Sub

        Public Shared Function AssociatedSymbolName(associatedSymbol As Symbol) As String
            Return If(associatedSymbol.IsUserDefinedOperator(),
                      SyntaxFacts.GetText(OverloadResolution.GetOperatorTokenKind(associatedSymbol.Name)),
                      associatedSymbol.Name)
        End Function

        ' Report diagnostic for a member shadowing a MustOverride.
        Private Shared Sub ReportShadowingMustOverrideError(hidingMember As Symbol,
                                                            hiddenMember As Symbol,
                                                            diagnostics As BindingDiagnosticBag)
            Debug.Assert(hidingMember.Locations(0).IsInSource)

            If hidingMember.IsAccessor() Then
                ' accessor hiding non-accessorTODO
                Dim associatedHidingSymbol = DirectCast(hidingMember, MethodSymbol).AssociatedSymbol
                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_SynthMemberShadowsMustOverride5,
                                                                      hidingMember,
                                                                      associatedHidingSymbol.GetKindText(), associatedHidingSymbol.Name,
                                                                      hiddenMember.ContainingType.GetKindText(),
                                                                      CustomSymbolDisplayFormatter.ShortErrorName(hiddenMember.ContainingType)),
                                               hidingMember.Locations(0)))
            Else
                ' Basic hiding case
                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_CantShadowAMustOverride1, hidingMember),
                                               hidingMember.Locations(0)))
            End If
        End Sub

        ''' <summary>
        ''' Some symbols do not participate in overriding/hiding (e.g. constructors). Accessors are consider
        ''' to override or hide.
        ''' </summary>
        Friend Shared Function CanOverrideOrHide(sym As Symbol) As Boolean
            If sym.Kind <> SymbolKind.Method Then
                Return True
            Else

                Select Case DirectCast(sym, MethodSymbol).MethodKind
                    Case MethodKind.LambdaMethod, MethodKind.Constructor, MethodKind.SharedConstructor
                        Return False
                    Case MethodKind.Conversion, MethodKind.DelegateInvoke, MethodKind.UserDefinedOperator, MethodKind.Ordinary, MethodKind.DeclareMethod,
                        MethodKind.EventAdd, MethodKind.EventRaise, MethodKind.EventRemove,
                        MethodKind.PropertyGet, MethodKind.PropertySet
                        Return True
                    Case Else
                        Debug.Assert(False, String.Format("Unexpected method kind '{0}'", DirectCast(sym, MethodSymbol).MethodKind))
                        Return False
                End Select
            End If
        End Function

        ' If this member overrides another member, return that overridden member, else return Nothing.
        Protected Shared Function GetOverriddenMember(sym As Symbol) As Symbol
            Select Case sym.Kind
                Case SymbolKind.Method
                    Return DirectCast(sym, MethodSymbol).OverriddenMethod
                Case SymbolKind.Property
                    Return DirectCast(sym, PropertySymbol).OverriddenProperty
                Case SymbolKind.Event
                    Return DirectCast(sym, EventSymbol).OverriddenEvent
            End Select

            Return Nothing
        End Function

        ''' <summary>
        ''' If a method had a virtual inaccessible override, then an explicit override in metadata is needed
        ''' to make it really override what it intends to override, and "skip" the inaccessible virtual
        ''' method.
        ''' </summary>
        Public Shared Function RequiresExplicitOverride(method As MethodSymbol) As Boolean
            If method.IsAccessor Then
                If TypeOf method.AssociatedSymbol Is EventSymbol Then
                    ' VB does not override events
                    Return False
                End If

                Return RequiresExplicitOverride(DirectCast(method.AssociatedSymbol, PropertySymbol))
            End If

            If method.OverriddenMethod IsNot Nothing Then
                For Each inaccessibleOverride In method.OverriddenMembers.InaccessibleMembers
                    If inaccessibleOverride.IsOverridable OrElse inaccessibleOverride.IsMustOverride OrElse inaccessibleOverride.IsOverrides Then
                        Return True
                    End If
                Next
            End If

            Return False
        End Function

        Private Shared Function RequiresExplicitOverride(prop As PropertySymbol) As Boolean
            If prop.OverriddenProperty IsNot Nothing Then
                For Each inaccessibleOverride In prop.OverriddenMembers.InaccessibleMembers
                    If inaccessibleOverride.IsOverridable OrElse inaccessibleOverride.IsMustOverride OrElse inaccessibleOverride.IsOverrides Then
                        Return True
                    End If
                Next
            End If

            Return False
        End Function
    End Class

    ''' <summary>
    ''' Many of the methods want to generically work on properties, methods (and maybe events) as TSymbol. We put all these
    ''' methods into a generic class for convenience.
    ''' </summary>
    Friend Class OverrideHidingHelper(Of TSymbol As Symbol)
        Inherits OverrideHidingHelper

        ' Comparer for comparing signatures of TSymbols in a runtime-equivalent way.
        ' It is not ReadOnly because it is initialized by a Shared Sub New of another instance of this class.
#Disable Warning IDE0044 ' Add readonly modifier - Adding readonly generates compile error in the constructor. - see https://github.com/dotnet/roslyn/issues/47197
        Private Shared s_runtimeSignatureComparer As IEqualityComparer(Of TSymbol)
#Enable Warning IDE0044 ' Add readonly modifier

        ' Initialize the various kinds of comparers.
        Shared Sub New()
            OverrideHidingHelper(Of MethodSymbol).s_runtimeSignatureComparer = MethodSignatureComparer.RuntimeMethodSignatureComparer
            OverrideHidingHelper(Of PropertySymbol).s_runtimeSignatureComparer = PropertySignatureComparer.RuntimePropertySignatureComparer
            OverrideHidingHelper(Of EventSymbol).s_runtimeSignatureComparer = EventSignatureComparer.RuntimeEventSignatureComparer
        End Sub

        ''' <summary>
        ''' Walk up the type hierarchy from ContainingType and list members that this
        ''' method overrides (accessible methods/properties with the same signature, if this
        ''' method is declared "override").
        ''' 
        ''' Methods in the overridden list may not be virtual or may have different
        ''' accessibilities, types, accessors, etc.  They are really candidates to be
        ''' overridden.
        ''' 
        ''' All found accessible candidates of overridden members are collected in two 
        ''' builders, those with 'exactly' matching signatures and those with 'generally'
        ''' or 'inexactly' matching signatures. 'Exact' signature match is a 'general' 
        ''' signature match which also does not have mismatches in total number of parameters
        ''' and/or types of optional parameters. See also comments on correspondent 
        ''' OverriddenMembersResult(Of TSymbol) properties.
        ''' 
        ''' 'Inexactly' matching candidates are only collected for reporting Dev10/Dev11
        ''' errors like BC30697 and others. We collect 'inexact' matching candidates until 
        ''' we find any 'exact' match.
        ''' 
        ''' Also remembers inaccessible members that are found, but these do not prevent
        ''' continuing to search for accessible members.
        ''' 
        ''' </summary>
        ''' <remarks>
        ''' In the presence of non-VB types, the meaning of "same signature" is rather
        ''' complicated.  If this method isn't from source, then it refers to the runtime's
        ''' notion of signature (i.e. including return type, custom modifiers, etc).
        ''' If this method is from source, use the VB version of signature. Note that 
        ''' Dev10 C# has a rule that prefers members with less custom modifiers. Dev 10 VB has no
        ''' such rule, so I'm not adding such a rule here.
        ''' </remarks>
        Friend Shared Function MakeOverriddenMembers(overridingSym As TSymbol) As OverriddenMembersResult(Of TSymbol)
            If Not overridingSym.IsOverrides OrElse Not CanOverrideOrHide(overridingSym) Then
                Return OverriddenMembersResult(Of TSymbol).Empty
            End If

            ' We should not be here for constructed methods, since overriding/hiding doesn't really make sense for them.
            Debug.Assert(Not (TypeOf overridingSym Is MethodSymbol AndAlso DirectCast(DirectCast(overridingSym, Symbol), MethodSymbol).ConstructedFrom <> overridingSym))

            ' We should not be here for property accessors (but ok for event accessors).
            ' TODO: When we support virtual events, that might change.
            Debug.Assert(Not (TypeOf overridingSym Is MethodSymbol AndAlso
                              (DirectCast(DirectCast(overridingSym, Symbol), MethodSymbol).MethodKind = MethodKind.PropertyGet OrElse
                                DirectCast(DirectCast(overridingSym, Symbol), MethodSymbol).MethodKind = MethodKind.PropertySet)))

            ' NOTE: If our goal is to make source references and metadata references indistinguishable, then we should really
            ' distinguish between the "current" compilation and other compilations, rather than between source and metadata.
            ' However, doing so would require adding a new parameter to the public API (i.e. which compilation to consider
            ' "current") and that extra complexity does not seem to provide significant benefit.  Our fallback goal is:
            ' if a source assembly builds successfully, then compilations referencing that assembly should build against
            ' both source and metadata or fail to build against both source and metadata.  Our expectation is that an exact
            ' match (which is required for successful compilation) should roundtrip through metadata, so this requirement
            ' should be met.
            Dim overridingIsFromSomeCompilation As Boolean = overridingSym.Dangerous_IsFromSomeCompilationIncludingRetargeting

            Dim containingType As NamedTypeSymbol = overridingSym.ContainingType
            Dim overriddenBuilder As ArrayBuilder(Of TSymbol) = ArrayBuilder(Of TSymbol).GetInstance()
            Dim inexactOverriddenMembers As ArrayBuilder(Of TSymbol) = ArrayBuilder(Of TSymbol).GetInstance()
            Dim inaccessibleBuilder As ArrayBuilder(Of TSymbol) = ArrayBuilder(Of TSymbol).GetInstance()

            Debug.Assert(Not containingType.IsInterface, "An interface member can't be marked overrides")

            Dim currType As NamedTypeSymbol = containingType.BaseTypeNoUseSiteDiagnostics

            While currType IsNot Nothing
                If FindOverriddenMembersInType(overridingSym, overridingIsFromSomeCompilation, containingType, currType, overriddenBuilder, inexactOverriddenMembers, inaccessibleBuilder) Then
                    Exit While ' Once we hit an overriding or hiding member, we're done.
                End If

                currType = currType.BaseTypeNoUseSiteDiagnostics
            End While

            Return OverriddenMembersResult(Of TSymbol).Create(overriddenBuilder.ToImmutableAndFree(),
                                                              inexactOverriddenMembers.ToImmutableAndFree(),
                                                              inaccessibleBuilder.ToImmutableAndFree())
        End Function

        ''' <summary>
        ''' Look for overridden members in a specific type. Return true if we find an overridden member candidate 
        ''' with 'exact' signature match, or we hit a member that hides. See comments on MakeOverriddenMembers(...)
        ''' for description of 'exact' and 'inexact' signature matches.
        ''' 
        ''' Also remember any inaccessible members that we see.
        ''' </summary>
        ''' <param name="overridingSym">Syntax that overriding or hiding.</param>
        ''' <param name="overridingIsFromSomeCompilation">True if "overridingSym" is from source (this.IsFromSomeCompilation).</param>
        ''' <param name="overridingContainingType">The type that contains this method (this.ContainingType).</param>
        ''' <param name="currType">The type to search.</param>
        ''' <param name="overriddenBuilder">Builder to place exactly-matched overridden member candidates in. </param>
        ''' <param name="inexactOverriddenMembers">Builder to place inexactly-matched overridden member candidates in. </param>
        ''' <param name="inaccessibleBuilder">Builder to place exactly-matched inaccessible overridden member candidates in. </param>
        Private Shared Function FindOverriddenMembersInType(overridingSym As TSymbol,
                                                            overridingIsFromSomeCompilation As Boolean,
                                                            overridingContainingType As NamedTypeSymbol,
                                                            currType As NamedTypeSymbol,
                                                            overriddenBuilder As ArrayBuilder(Of TSymbol),
                                                            inexactOverriddenMembers As ArrayBuilder(Of TSymbol),
                                                            inaccessibleBuilder As ArrayBuilder(Of TSymbol)) As Boolean
            ' Note that overriddenBuilder may contain some non-exact 
            ' matched symbols found in previous iterations

            ' We should not be here for property accessors (but ok for event accessors).
            ' TODO: When we support virtual events, that might change.
            Debug.Assert(Not (TypeOf overridingSym Is MethodSymbol AndAlso
                              (DirectCast(DirectCast(overridingSym, Symbol), MethodSymbol).MethodKind = MethodKind.PropertyGet OrElse
                                DirectCast(DirectCast(overridingSym, Symbol), MethodSymbol).MethodKind = MethodKind.PropertySet)))

            Dim stopLookup As Boolean = False
            Dim haveExactMatch As Boolean = False
            Dim overriddenInThisType As ArrayBuilder(Of TSymbol) = ArrayBuilder(Of TSymbol).GetInstance()

            For Each sym In currType.GetMembers(overridingSym.Name)
                ProcessMemberWithMatchingName(sym, overridingSym, overridingIsFromSomeCompilation, overridingContainingType, inexactOverriddenMembers,
                                              inaccessibleBuilder, overriddenInThisType, stopLookup, haveExactMatch)
            Next

            If overridingSym.Kind = SymbolKind.Property Then
                Dim prop = DirectCast(DirectCast(overridingSym, Object), PropertySymbol)

                If prop.IsImplicitlyDeclared AndAlso prop.IsWithEvents Then
                    For Each sym In currType.GetSynthesizedWithEventsOverrides()
                        If sym.Name.Equals(prop.Name) Then
                            ProcessMemberWithMatchingName(sym, overridingSym, overridingIsFromSomeCompilation, overridingContainingType, inexactOverriddenMembers,
                                              inaccessibleBuilder, overriddenInThisType, stopLookup, haveExactMatch)
                        End If
                    Next
                End If
            End If

            If overriddenInThisType.Count > 1 Then
                RemoveMembersWithConflictingAccessibility(overriddenInThisType)
            End If

            If overriddenInThisType.Count > 0 Then
                If haveExactMatch Then
                    Debug.Assert(stopLookup)
                    overriddenBuilder.Clear()
                End If

                If overriddenBuilder.Count = 0 Then
                    overriddenBuilder.AddRange(overriddenInThisType)
                End If
            End If

            overriddenInThisType.Free()
            Return stopLookup
        End Function

        Private Shared Sub ProcessMemberWithMatchingName(
            sym As Symbol,
            overridingSym As TSymbol,
            overridingIsFromSomeCompilation As Boolean,
            overridingContainingType As NamedTypeSymbol,
            inexactOverriddenMembers As ArrayBuilder(Of TSymbol),
            inaccessibleBuilder As ArrayBuilder(Of TSymbol),
            overriddenInThisType As ArrayBuilder(Of TSymbol),
            ByRef stopLookup As Boolean,
            ByRef haveExactMatch As Boolean
        )
            ' Use original definition for accessibility check, because substitutions can cause
            ' reductions in accessibility that aren't appropriate (see bug #12038 for example).
            Dim accessible = AccessCheck.IsSymbolAccessible(sym.OriginalDefinition, overridingContainingType.OriginalDefinition, Nothing, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)

            If sym.Kind = overridingSym.Kind AndAlso
                CanOverrideOrHide(sym) Then

                Dim member As TSymbol = DirectCast(sym, TSymbol)
                Dim exactMatch As Boolean = True ' considered to be True for all runtime signature comparisons
                Dim exactMatchIgnoringCustomModifiers As Boolean = True ' considered to be True for all runtime signature comparisons

                If If(overridingIsFromSomeCompilation,
                    sym.IsWithEventsProperty = overridingSym.IsWithEventsProperty AndAlso
                        SignaturesMatch(overridingSym, member, exactMatch, exactMatchIgnoringCustomModifiers),
                    s_runtimeSignatureComparer.Equals(overridingSym, member)) Then

                    If accessible Then
                        If exactMatchIgnoringCustomModifiers Then
                            If exactMatch Then
                                If Not haveExactMatch Then
                                    haveExactMatch = True
                                    stopLookup = True
                                    overriddenInThisType.Clear()
                                End If

                                overriddenInThisType.Add(member)
                            ElseIf Not haveExactMatch Then
                                overriddenInThisType.Add(member)
                            End If
                        Else
                            ' Add only if not hidden by signature
                            AddMemberToABuilder(member, inexactOverriddenMembers)
                        End If
                    Else
                        If exactMatchIgnoringCustomModifiers Then
                            ' only exact matched methods are to be added 
                            inaccessibleBuilder.Add(member)
                        End If
                    End If
                ElseIf Not member.IsOverloads() AndAlso accessible Then
                    ' hiding symbol by name
                    stopLookup = True
                End If
            ElseIf accessible Then
                ' Any accessible symbol of different kind stops further lookup
                stopLookup = True
            End If
        End Sub

        Private Shared Sub AddMemberToABuilder(member As TSymbol,
                                               builder As ArrayBuilder(Of TSymbol))

            ' We should only add a member to a builder if it does not match any 
            ' symbols from previously processed (derived) classes 

            ' This is supposed to help avoid adding multiple symbols one of 
            ' which overrides another one, in the following case
            '    C1
            '       Overridable Sub S(x As Integer, Optional y As Integer = 1)
            '
            '    C2: C1
            '       Overridable Sub S(x As Integer)
            '
            '    C3: C2
            '       Overrides Sub S(x As Integer)
            '
            '    C4: C3
            '       Overrides Sub S(x As Integer, Optional y As Integer = 1)

            ' In the case above we should not add 'S(x As Integer)' twice

            ' We don't use 'OverriddenMethod' property on MethodSymbol because
            ' right now it does not cache the result, so we want to avoid 
            ' unnecessary nested calls to 'MakeOverriddenMembers'

            Dim memberContainingType As NamedTypeSymbol = member.ContainingType
            For i = 0 To builder.Count - 1
                Dim exactMatchIgnoringCustomModifiers As Boolean = False
                If Not TypeSymbol.Equals(builder(i).ContainingType, memberContainingType, TypeCompareKind.ConsiderEverything) AndAlso
                        SignaturesMatch(builder(i), member, Nothing, exactMatchIgnoringCustomModifiers) AndAlso exactMatchIgnoringCustomModifiers Then
                    ' Do NOT add
                    Exit Sub
                End If
            Next
            builder.Add(member)
        End Sub

        ' Check a member that is marked Override against it's base and report any necessary diagnostics. The already computed
        ' overridden members are passed in.
        Friend Shared Sub CheckOverrideMember(member As TSymbol,
                                              overriddenMembersResult As OverriddenMembersResult(Of TSymbol),
                                              diagnostics As BindingDiagnosticBag)
            Debug.Assert(overriddenMembersResult IsNot Nothing)

            Dim memberIsShadows As Boolean = member.ShadowsExplicitly
            Dim memberIsOverloads As Boolean = member.IsOverloads()
            Dim overriddenMembers As ImmutableArray(Of TSymbol) = overriddenMembersResult.OverriddenMembers

            ' If there are no overridden members (those with 'exactly' matching signature)
            ' analyze overridden member candidates with 'generally' matching signature
            If overriddenMembers.IsEmpty Then
                overriddenMembers = overriddenMembersResult.InexactOverriddenMembers
            End If

            If overriddenMembers.Length = 0 Then
                ' Did not have member to override. But there might have been an inaccessible one.
                If overriddenMembersResult.InaccessibleMembers.Length > 0 Then
                    ReportBadOverriding(ERRID.ERR_CannotOverrideInAccessibleMember, member, overriddenMembersResult.InaccessibleMembers(0), diagnostics)
                Else
                    diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_OverrideNotNeeded3, member.GetKindText(), member.Name),
                                                   member.Locations(0)))
                End If
            ElseIf overriddenMembers.Length > 1 Then
                ' Multiple members we could be overriding. Create a single error message that lists them all.
                Dim diagnosticInfos = ArrayBuilder(Of DiagnosticInfo).GetInstance(overriddenMembers.Length)

                For Each overriddenMemb In overriddenMembers
                    diagnosticInfos.Add(ErrorFactory.ErrorInfo(ERRID.ERR_OverriddenCandidate1, overriddenMemb.OriginalDefinition))
                Next

                diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_AmbiguousOverrides3,
                                                           overriddenMembers(0),
                                                           CustomSymbolDisplayFormatter.ShortErrorName(overriddenMembers(0).ContainingType),
                                                           New CompoundDiagnosticInfo(diagnosticInfos.ToArrayAndFree())),
                                               member.Locations(0)))
            Else
                ' overriding exactly one member.
                Dim overriddenMember As TSymbol = overriddenMembers(0)
                Dim comparisonResults As SymbolComparisonResults = DetailedSignatureCompare(member, overriddenMember, SymbolComparisonResults.AllMismatches)
                Dim errorId As ERRID

                If overriddenMember.IsNotOverridable Then
                    ReportBadOverriding(ERRID.ERR_CantOverrideNotOverridable2, member, overriddenMember, diagnostics)
                ElseIf Not (overriddenMember.IsOverridable Or overriddenMember.IsMustOverride Or overriddenMember.IsOverrides) Then
                    ReportBadOverriding(ERRID.ERR_CantOverride4, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.ParameterByrefMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverrideWithByref2, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.OptionalParameterMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverrideWithOptional2, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.ReturnTypeMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_InvalidOverrideDueToReturn2, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.PropertyAccessorMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverridingPropertyKind2, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.PropertyInitOnlyMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverridingInitOnlyProperty, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.ParamArrayMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverrideWithArrayVsParamArray2, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.OptionalParameterTypeMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverrideWithOptionalTypes2, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.OptionalParameterValueMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverrideWithDefault2, member, overriddenMember, diagnostics)
                ElseIf (comparisonResults And SymbolComparisonResults.ConstraintMismatch) <> 0 Then
                    ReportBadOverriding(ERRID.ERR_OverrideWithConstraintMismatch2, member, overriddenMember, diagnostics)
                ElseIf Not ConsistentAccessibility(member, overriddenMember, errorId) Then
                    ReportBadOverriding(errorId, member, overriddenMember, diagnostics)
                ElseIf member.ContainsTupleNames() AndAlso (comparisonResults And SymbolComparisonResults.TupleNamesMismatch) <> 0 Then
                    ' it is ok to override with no tuple names, for compatibility with VB 14, but otherwise names should match
                    ReportBadOverriding(ERRID.WRN_InvalidOverrideDueToTupleNames2, member, overriddenMember, diagnostics)
                Else
                    For Each inaccessibleMember In overriddenMembersResult.InaccessibleMembers
                        If inaccessibleMember.DeclaredAccessibility = Accessibility.Friend AndAlso
                            inaccessibleMember.OverriddenMember = overriddenMember Then
                            ' We have an inaccessible friend member that overrides the member we're trying to override.
                            ' We can't do that, so issue an error.
                            diagnostics.Add(New VBDiagnostic(ErrorFactory.ErrorInfo(ERRID.ERR_InAccessibleOverridingMethod5,
                                                                                  member, member.ContainingType, overriddenMember, overriddenMember.ContainingType, inaccessibleMember.ContainingType),
                                                            member.Locations(0)))

                        End If

                    Next

                    Dim useSiteInfo = overriddenMember.GetUseSiteInfo()
                    If Not diagnostics.Add(useSiteInfo, member.Locations(0)) AndAlso
                       member.Kind = SymbolKind.Property Then

                        ' No overriding errors found in member. If its a property, its accessors might have issues.
                        Dim overridingProperty As PropertySymbol = DirectCast(DirectCast(member, Symbol), PropertySymbol)
                        Dim overriddenProperty As PropertySymbol = DirectCast(DirectCast(overriddenMember, Symbol), PropertySymbol)

                        CheckOverridePropertyAccessor(overridingProperty.GetMethod, overriddenProperty.GetMethod, diagnostics)
                        CheckOverridePropertyAccessor(overridingProperty.SetMethod, overriddenProperty.SetMethod, diagnostics)
                    End If
                End If
            End If
        End Sub

        ' Imported types can have multiple members with the same signature (case insensitive) and different accessibilities. VB prefers
        ' members that are "more accessible". This is a very rare code path if members has > 1 element so we don't worry about performance.
        Private Shared Sub RemoveMembersWithConflictingAccessibility(members As ArrayBuilder(Of TSymbol))
            If members.Count < 2 Then
                Return
            End If

            Const significantDifferences As SymbolComparisonResults = SymbolComparisonResults.AllMismatches And
                                                                      Not SymbolComparisonResults.MismatchesForConflictingMethods

            Dim nonConflicting As ArrayBuilder(Of TSymbol) = ArrayBuilder(Of TSymbol).GetInstance()

            For Each sym In members
                Dim isWorseThanAnother As Boolean = False
                For Each otherSym In members
                    If sym IsNot otherSym Then
                        Dim originalSym = sym.OriginalDefinition
                        Dim originalOther = otherSym.OriginalDefinition

                        ' Two original definitions with identical signatures in same containing types are compared by accessibility, and
                        ' more accessible wins.
                        If TypeSymbol.Equals(originalSym.ContainingType, originalOther.ContainingType, TypeCompareKind.ConsiderEverything) AndAlso
                           DetailedSignatureCompare(originalSym, originalOther, significantDifferences) = 0 AndAlso
                           LookupResult.CompareAccessibilityOfSymbolsConflictingInSameContainer(originalSym, originalOther) < 0 Then
                            ' sym is worse than otherSym
                            isWorseThanAnother = True
                            Exit For
                        End If
                    End If
                Next

                If Not isWorseThanAnother Then
                    nonConflicting.Add(sym)
                End If
            Next

            If nonConflicting.Count <> members.Count Then
                members.Clear()
                members.AddRange(nonConflicting)
            End If

            nonConflicting.Free()
        End Sub

        ' Check an accessor with respect to its overridden accessor and report any diagnostics
        Friend Shared Sub CheckOverridePropertyAccessor(overridingAccessor As MethodSymbol,
                                                        overriddenAccessor As MethodSymbol,
                                                        diagnostics As BindingDiagnosticBag)
            ' CONSIDER: it is possible for an accessor to have a use site error even when the property
            ' does not but, in general, we have not been handling cases where property and accessor
            ' signatures are mismatched (e.g. different modopts).
            If overridingAccessor IsNot Nothing AndAlso overriddenAccessor IsNot Nothing Then
                ' Use original definition for accessibility check, because substitutions can cause
                ' reductions in accessibility that aren't appropriate (see bug #12038 for example).
                If Not AccessCheck.IsSymbolAccessible(overriddenAccessor.OriginalDefinition, overridingAccessor.ContainingType, Nothing, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded) Then
                    ReportBadOverriding(ERRID.ERR_CannotOverrideInAccessibleMember, overridingAccessor, overriddenAccessor, diagnostics)
                Else
                    Dim errorId As ERRID

                    If Not ConsistentAccessibility(overridingAccessor, overriddenAccessor, errorId) Then
                        ReportBadOverriding(errorId, overridingAccessor, overriddenAccessor, diagnostics)
                    End If
                End If

                diagnostics.Add(overriddenAccessor.GetUseSiteInfo(), overridingAccessor.Locations(0))
            End If
        End Sub

        ' Report an error with overriding
        Private Shared Sub ReportBadOverriding(id As ERRID,
                                               overridingMember As Symbol,
                                               overriddenMember As Symbol,
                                               diagnostics As BindingDiagnosticBag)
            diagnostics.Add(New VBDiagnostic(New BadSymbolDiagnostic(overriddenMember, id, overridingMember, overriddenMember),
                                            overridingMember.Locations(0)))
        End Sub

        ' Are the declared accessibility of the two symbols consistent? If not, return the error code to use.
        Private Shared Function ConsistentAccessibility(overriding As Symbol, overridden As Symbol, ByRef errorId As ERRID) As Boolean
            If overridden.DeclaredAccessibility = Accessibility.ProtectedOrFriend And Not overriding.ContainingAssembly = overridden.ContainingAssembly Then
                errorId = ERRID.ERR_FriendAssemblyBadAccessOverride2
                Return overriding.DeclaredAccessibility = Accessibility.Protected
            Else
                errorId = ERRID.ERR_BadOverrideAccess2
                Return overridden.DeclaredAccessibility = overriding.DeclaredAccessibility
            End If
        End Function

    End Class
End Namespace
