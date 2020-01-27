' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Diagnostics
Imports Microsoft.CodeAnalysis.PooledObjects

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols
    ''' <summary>
    ''' Function that help implement the overloading rules for VB, in particular the rules
    ''' for recasing method and property names.
    ''' </summary>
    Friend Module OverloadingHelper

        ''' <summary>
        ''' Set the correct metadata name for all overloads of a particular name and symbol kind
        ''' (must be method or property) inside a container.
        ''' 
        ''' The rules are as follows:
        '''    1) If a method or property overrides one from its base class, its metadata name
        '''       must match that.
        '''    2) If method overloads those in the base (because the Overloads keyword is used), and
        '''       all metadata names in the base are consistent in case, use that name.
        '''    3) All overloads with a class should match, except possibly for overrides. If there is
        '''       an override or overload from base, use that. Otherwise, use casing of first member in
        '''       class.
        ''' </summary>
        Public Sub SetMetadataNameForAllOverloads(name As String, kind As SymbolKind, container As NamedTypeSymbol)
            Debug.Assert(kind = SymbolKind.Method OrElse kind = SymbolKind.Property)

            Dim compilation = container.DeclaringCompilation

            Dim overloadedMembers = ArrayBuilder(Of Symbol).GetInstance()    ' the set of overloaded symbols.
            Dim hasOverloadSpecifier As Boolean = False                      ' do any overloads have "Overloads" (not counting Overrides)
            Dim hasOverrideSpecifier As Boolean = False                      ' do any overloads have "Overrides" 

            Dim metadataName As String = Nothing                             ' the metadata name we have chosen

            Try
                ' Check for overloads or overrides.
                ' Find all the overloads that we are processing, and if any have "Overloads" or "Overrides"
                FindOverloads(name, kind, container, overloadedMembers, hasOverloadSpecifier, hasOverrideSpecifier)

                If overloadedMembers.Count = 1 AndAlso Not hasOverloadSpecifier AndAlso Not hasOverrideSpecifier Then
                    ' Quick, common case: one symbol of name in type, no "Overrides" or "Overloads".
                    ' Just use the current name.
                    overloadedMembers(0).SetMetadataName(overloadedMembers(0).Name)
                    Return
                ElseIf hasOverrideSpecifier Then
                    ' Note: in error conditions (an override didn't exist), this could return Nothing.
                    ' That is dealt with below. 
                    metadataName = SetMetadataNamesOfOverrides(overloadedMembers, compilation)
                ElseIf hasOverloadSpecifier Then
                    metadataName = GetBaseMemberMetadataName(name, kind, container)
                End If

                If metadataName Is Nothing Then
                    ' We did not get a name from the overrides or base class. Pick the name of the first member
                    metadataName = NameOfFirstMember(overloadedMembers, compilation)
                End If

                ' We now have the metadata name we want to apply to each non-override member
                ' (override member names have already been applied)
                For Each member In overloadedMembers
                    If Not (member.IsOverrides AndAlso member.OverriddenMember() IsNot Nothing) Then
                        member.SetMetadataName(metadataName)
                    End If
                Next
            Finally
                overloadedMembers.Free()
            End Try
        End Sub

        ''' <summary>
        ''' Collect all overloads in "container" of the given name and kind.
        ''' Also determine if any have "Overloads" or "Overrides" specifiers.
        ''' </summary>
        Private Sub FindOverloads(name As String,
                                  kind As SymbolKind,
                                  container As NamedTypeSymbol,
                                  overloadsMembers As ArrayBuilder(Of Symbol),
                                  ByRef hasOverloadSpecifier As Boolean,
                                  ByRef hasOverrideSpecifier As Boolean)
            For Each member In container.GetMembers(name)
                If IsCandidateMember(member, kind) Then
                    overloadsMembers.Add(member)

                    If member.IsOverrides Then
                        hasOverrideSpecifier = True
                    ElseIf member.IsOverloads Then
                        hasOverloadSpecifier = True
                    End If
                End If
            Next
        End Sub

        ''' <summary>
        ''' For each member in "overloadedMembers" that is marked Overrides, set its
        ''' metadata name to be the metadata name of its overridden member. Return the
        ''' first such name, lexically.
        ''' 
        ''' Note: can return null if no override member with an actual overridden member was found.
        ''' </summary>
        Private Function SetMetadataNamesOfOverrides(overloadedMembers As ArrayBuilder(Of Symbol), compilation As VisualBasicCompilation) As String
            Dim locationOfFirstOverride As Location = Nothing
            Dim firstOverrideName As String = Nothing

            For Each member In overloadedMembers
                If member.IsOverrides Then
                    Dim overriddenMember As Symbol = member.OverriddenMember()
                    If overriddenMember IsNot Nothing Then
                        Dim metadataName As String = overriddenMember.MetadataName
                        member.SetMetadataName(metadataName)

                        ' Remember the metadata name of the lexically first override
                        If firstOverrideName Is Nothing OrElse compilation.CompareSourceLocations(member.Locations(0), locationOfFirstOverride) < 0 Then
                            firstOverrideName = metadataName
                            locationOfFirstOverride = member.Locations(0)
                        End If
                    End If
                End If
            Next

            Return firstOverrideName
        End Function

        ''' <summary>
        ''' Return the name of the lexically first symbol in "overloadedMembers".
        ''' </summary>
        Private Function NameOfFirstMember(overloadedMembers As ArrayBuilder(Of Symbol), compilation As VisualBasicCompilation) As String
            Dim firstName As String = Nothing
            Dim locationOfFirstName As Location = Nothing
            For Each member In overloadedMembers
                Dim memberLocation = member.Locations(0)
                If firstName Is Nothing OrElse compilation.CompareSourceLocations(memberLocation, locationOfFirstName) < 0 Then
                    firstName = member.Name
                    locationOfFirstName = memberLocation
                End If
            Next

            Return firstName
        End Function

        ''' <summary>
        ''' Check all accessible, visible members of the base types of container for the given name and kind. If they
        ''' all have the same case-sensitive metadata name, return that name. Otherwise, return Nothing.
        ''' </summary>
        Private Function GetBaseMemberMetadataName(name As String, kind As SymbolKind, container As NamedTypeSymbol) As String
            Dim metadataName As String = Nothing
            Dim metadataLocation As Location = Nothing

            ' We are creating a binder for the first partial declaration, so we can use member lookup to find accessible & visible
            ' members. For the lookup we are doing, it doesn't matter which partial we use because Imports and Options can't
            ' affect a lookup that ignores extension methods.
            Dim binder = BinderBuilder.CreateBinderForType(DirectCast(container.ContainingModule, SourceModuleSymbol),
                                                           container.Locations(0).PossiblyEmbeddedOrMySourceTree(),
                                                           container)

            Dim result = LookupResult.GetInstance()
            binder.LookupMember(result, container, name, 0, LookupOptions.AllMethodsOfAnyArity Or LookupOptions.IgnoreExtensionMethods, useSiteInfo:=CompoundUseSiteInfo(Of AssemblySymbol).Discarded)
            If result.IsGoodOrAmbiguous Then
                Dim lookupSymbols As ArrayBuilder(Of Symbol) = result.Symbols
                If result.Kind = LookupResultKind.Ambiguous AndAlso result.HasDiagnostic AndAlso TypeOf result.Diagnostic Is AmbiguousSymbolDiagnostic Then
                    lookupSymbols.AddRange(DirectCast(result.Diagnostic, AmbiguousSymbolDiagnostic).AmbiguousSymbols)
                End If

                For Each foundMember In lookupSymbols
                    ' Go through each member found in a base class or interface
                    If IsCandidateMember(foundMember, kind) AndAlso foundMember.ContainingType IsNot container Then
                        If metadataName Is Nothing Then
                            metadataName = foundMember.MetadataName
                        Else
                            ' Intentionally using case-sensitive comparison here.
                            If Not String.Equals(metadataName, foundMember.MetadataName, StringComparison.Ordinal) Then
                                ' We have found two members with conflicting casing of metadata names.
                                metadataName = Nothing
                                Exit For
                            End If
                        End If
                    End If
                Next
            End If

            result.Free()

            Return metadataName
        End Function

        Private Function IsCandidateMember(member As Symbol, kind As SymbolKind) As Boolean
            Return member.Kind = kind AndAlso Not member.IsAccessor()
        End Function

    End Module
End Namespace
