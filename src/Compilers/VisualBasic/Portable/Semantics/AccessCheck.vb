' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Runtime.InteropServices

Imports TypeKind = Microsoft.CodeAnalysis.TypeKind

Namespace Microsoft.CodeAnalysis.VisualBasic
    ''' <summary>
    ''' The possible reasons a symbol may be inaccessible
    ''' </summary>
    ''' <remarks></remarks>
    Friend Enum AccessCheckResult
        ' Is accessible
        Accessible

        ' Regular inaccessibility
        Inaccessible

        ' A Protected member is inaccessible because its "through type" isn't right
        InaccessibleViaThroughType
    End Enum

    ''' <summary>
    ''' Contains the code for determining VB accessibility rules.
    ''' </summary>
    Friend NotInheritable Class AccessCheck

        Private Sub New()
        End Sub

        ''' <summary>
        ''' Checks if 'symbol' is accessible from within assembly 'within'.  
        ''' </summary>
        Public Shared Function IsSymbolAccessible(symbol As Symbol,
                                                  within As AssemblySymbol,
                                                  <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                  Optional basesBeingResolved As ConsList(Of Symbol) = Nothing) As Boolean
            Return CheckSymbolAccessibilityCore(symbol, within, Nothing, basesBeingResolved, useSiteDiagnostics) = AccessCheckResult.Accessible
        End Function

        ''' <summary>
        ''' Checks if 'symbol' is accessible from within assembly 'within'.  
        ''' </summary>
        Public Shared Function CheckSymbolAccessibility(symbol As Symbol,
                                                        within As AssemblySymbol,
                                                        <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                        Optional basesBeingResolved As ConsList(Of Symbol) = Nothing) As AccessCheckResult
            Return CheckSymbolAccessibilityCore(symbol, within, Nothing, basesBeingResolved, useSiteDiagnostics)
        End Function

        ''' <summary>
        ''' Checks if 'symbol' is accessible from within type 'within', with
        ''' an optional qualifier of type "throughTypeOpt".
        ''' </summary>
        Public Shared Function IsSymbolAccessible(symbol As Symbol,
                                                  within As NamedTypeSymbol,
                                                  throughTypeOpt As TypeSymbol,
                                                  <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                  Optional basesBeingResolved As ConsList(Of Symbol) = Nothing) As Boolean
            Return CheckSymbolAccessibilityCore(symbol, within, throughTypeOpt, basesBeingResolved, useSiteDiagnostics) = AccessCheckResult.Accessible
        End Function

        ''' <summary>
        ''' Checks if 'symbol' is accessible from within type 'within', with
        ''' an qualifier of type "throughTypeOpt". Sets "failedThroughTypeCheck" to true
        ''' if it failed the "through type" check.
        ''' </summary>
        Public Shared Function CheckSymbolAccessibility(symbol As Symbol,
                                                        within As NamedTypeSymbol,
                                                        throughTypeOpt As TypeSymbol,
                                                        <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo),
                                                        Optional basesBeingResolved As ConsList(Of Symbol) = Nothing) As AccessCheckResult
            Return CheckSymbolAccessibilityCore(symbol, within, throughTypeOpt, basesBeingResolved, useSiteDiagnostics)
        End Function

        ''' <summary>
        ''' Checks if 'symbol' is accessible from within 'within', which must be a NamedTypeSymbol or 
        ''' an AssemblySymbol.  If 'symbol' is accessed off
        ''' of an expression then 'throughTypeOpt' is the type of that expression. This is needed to
        ''' properly do protected access checks. Sets "failedThroughTypeCheck" to true if this protected
        ''' check failed.
        ''' </summary>
        Private Shared Function CheckSymbolAccessibilityCore(symbol As Symbol,
                                                             within As Symbol,
                                                             throughTypeOpt As TypeSymbol,
                                                             basesBeingResolved As ConsList(Of Symbol),
                                                             <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As AccessCheckResult
            Debug.Assert(symbol IsNot Nothing)
            Debug.Assert(within IsNot Nothing)
            Debug.Assert(TypeOf within Is NamedTypeSymbol OrElse TypeOf within Is AssemblySymbol)
            Debug.Assert(within.IsDefinition)

            Dim withinAssembly = If(TryCast(within, AssemblySymbol), (DirectCast(within, NamedTypeSymbol)).ContainingAssembly)

            Select Case symbol.Kind
                Case SymbolKind.ArrayType
                    Return CheckSymbolAccessibilityCore((DirectCast(symbol, ArrayTypeSymbol)).ElementType, within, Nothing, basesBeingResolved, useSiteDiagnostics)

                Case SymbolKind.NamedType
                    Return CheckNamedTypeAccessibility(DirectCast(symbol, NamedTypeSymbol), within, basesBeingResolved, useSiteDiagnostics)

                Case SymbolKind.ErrorType
                    ' Always assume that error types are accessible.
                    Return AccessCheckResult.Accessible

                Case SymbolKind.TypeParameter, SymbolKind.Parameter, SymbolKind.Local, SymbolKind.RangeVariable,
                     SymbolKind.Label, SymbolKind.Namespace, SymbolKind.Assembly, SymbolKind.NetModule
                    ' These types of symbols are always accessible (if visible).
                    Return AccessCheckResult.Accessible

                Case SymbolKind.Method, SymbolKind.Property, SymbolKind.Event, SymbolKind.Field
                    Exit Select

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(symbol.Kind)
            End Select

            If symbol.IsShared Then
                ' shared members aren't accessed "through" an "instance" of any type.  So we null
                ' out the "through" instance here.  This ensures that we'll understand accessing
                ' protected shared members properly.
                throughTypeOpt = Nothing
            End If

            Return CheckMemberAccessibility(symbol.ContainingType, symbol.DeclaredAccessibility, within, throughTypeOpt, basesBeingResolved, useSiteDiagnostics)
        End Function

        ' Is the named type "typeSym" accessible from within "within", which must
        ' be a named type or an assembly.
        Private Shared Function CheckNamedTypeAccessibility(typeSym As NamedTypeSymbol,
                                                            within As Symbol,
                                                            basesBeingResolved As ConsList(Of Symbol),
                                                            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As AccessCheckResult
            Debug.Assert(TypeOf within Is NamedTypeSymbol OrElse TypeOf within Is AssemblySymbol)
            Debug.Assert(typeSym IsNot Nothing)

            If Not typeSym.IsDefinition Then
                ' All type argument must be accessible.
                Dim typeArgs = typeSym.TypeArgumentsWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)

                For i As Integer = 0 To typeArgs.Length - 1
                    ' type parameters are always accessible, so don't check those (so common it's
                    ' worth optimizing this).
                    If typeArgs(i).Kind <> SymbolKind.TypeParameter Then
                        Dim result = CheckSymbolAccessibilityCore(typeArgs(i), within, Nothing, basesBeingResolved, useSiteDiagnostics)
                        If result <> AccessCheckResult.Accessible Then
                            Return result
                        End If
                    End If
                Next
            End If

            Dim containingType As NamedTypeSymbol = typeSym.ContainingType

            If containingType Is Nothing Then
                Return CheckNonNestedTypeAccessibility(typeSym.ContainingAssembly, typeSym.DeclaredAccessibility, within)
            Else
                Return CheckMemberAccessibility(typeSym.ContainingType, typeSym.DeclaredAccessibility, within, Nothing, basesBeingResolved, useSiteDiagnostics)
            End If
        End Function

        ' Is a top-level type with accessibility "declaredAccessibility" inside assembly "assembly"
        ' accessible from "within", which must be a named type or an assembly.
        Private Shared Function CheckNonNestedTypeAccessibility(assembly As AssemblySymbol, declaredAccessibility As Accessibility, within As Symbol) As AccessCheckResult
            Debug.Assert(TypeOf within Is NamedTypeSymbol OrElse TypeOf within Is AssemblySymbol)
            Debug.Assert(assembly IsNot Nothing)

            Dim withinAssembly As AssemblySymbol = If(TryCast(within, AssemblySymbol), DirectCast(within, NamedTypeSymbol).ContainingAssembly)

            Select Case declaredAccessibility
                Case Accessibility.NotApplicable, Accessibility.Public
                    ' Public symbols always accessible
                    Return AccessCheckResult.Accessible

                Case Accessibility.Private, Accessibility.Protected, Accessibility.ProtectedAndFriend
                    ' Shouldn't happen except in error cases, but those do happen.
                    Return AccessCheckResult.Accessible

                Case Accessibility.Friend, Accessibility.ProtectedOrFriend
                    ' An internal type is accessible if we're in the same assembly or we have
                    ' friend access to the assembly it was defined in.
                    Return If(HasFriendAccessTo(withinAssembly, assembly), AccessCheckResult.Accessible, AccessCheckResult.Inaccessible)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(declaredAccessibility)
            End Select
        End Function

        ' Is a member with declared accessibility "declaredAccessibility" accessible from within "within", which must
        ' be a named type or an assembly.
        Private Shared Function CheckMemberAccessibility(containingType As NamedTypeSymbol,
                                                         declaredAccessibility As Accessibility,
                                                         within As Symbol,
                                                         throughTypeOpt As TypeSymbol,
                                                         basesBeingResolved As ConsList(Of Symbol),
                                                         <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As AccessCheckResult
            Debug.Assert(TypeOf within Is NamedTypeSymbol OrElse TypeOf within Is AssemblySymbol)
            Debug.Assert(containingType IsNot Nothing)

            Dim originalContainingType As NamedTypeSymbol = containingType.OriginalDefinition
            Dim withinNamedType As NamedTypeSymbol = TryCast(within, NamedTypeSymbol)
            Dim withinAssembly As AssemblySymbol = If(TryCast(within, AssemblySymbol), withinNamedType.ContainingAssembly)

            ' A member is only accessible to us if its containing type is accessible as well.
            Dim result = CheckNamedTypeAccessibility(containingType, within, basesBeingResolved, useSiteDiagnostics)
            If result <> AccessCheckResult.Accessible Then
                Return result
            End If

            Select Case declaredAccessibility
                Case Accessibility.NotApplicable
                    Return AccessCheckResult.Accessible

                Case Accessibility.Public
                    Return AccessCheckResult.Accessible

                Case Accessibility.Private
                    ' All expressions in the current submission (top-level or nested in a method or
                    ' type) can access previous submission's private top-level members. Previous
                    ' submissions are treated like outer classes for the current submission - the
                    ' inner class can access private members of the outer class.
                    If containingType.TypeKind = TypeKind.Submission Then
                        Return AccessCheckResult.Accessible
                    End If

                    ' private members never accessible from outside a type.
                    Return If(withinNamedType Is Nothing,
                              AccessCheckResult.Inaccessible,
                              CheckPrivateSymbolAccessibility(withinNamedType, originalContainingType))

                Case Accessibility.Friend
                    ' A friend type is accessible if we're in the same assembly or we have
                    ' friend access to the assembly it was defined in.
                    Return If(HasFriendAccessTo(withinAssembly, containingType.ContainingAssembly), AccessCheckResult.Accessible, AccessCheckResult.Inaccessible)

                Case Accessibility.ProtectedAndFriend
                    If Not HasFriendAccessTo(withinAssembly, containingType.ContainingAssembly) Then
                        ' We require friend access.  If we don't have it, then this symbol is
                        ' definitely not accessible to us.
                        Return AccessCheckResult.Inaccessible
                    End If

                    ' We had friend access.  Also have to make sure we have protected access.
                    Return CheckProtectedSymbolAccessibility(within, throughTypeOpt, originalContainingType, basesBeingResolved, useSiteDiagnostics)

                Case Accessibility.ProtectedOrFriend
                    If HasFriendAccessTo(withinAssembly, containingType.ContainingAssembly) Then
                        ' If we have friend access to this symbol, then that's sufficient.  no
                        ' need to do the complicated protected case.
                        Return AccessCheckResult.Accessible
                    End If

                    ' We don't have friend access.  But if we have protected access then that's
                    ' sufficient.
                    Return CheckProtectedSymbolAccessibility(within, throughTypeOpt, originalContainingType, basesBeingResolved, useSiteDiagnostics)

                Case Accessibility.Protected
                    Return CheckProtectedSymbolAccessibility(within, throughTypeOpt, originalContainingType, basesBeingResolved, useSiteDiagnostics)

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(declaredAccessibility)
            End Select
        End Function

        ' Is a protected symbol inside "originalContainingType" accessible from within "within",
        ' which must be a named type or an assembly.
        Private Shared Function CheckProtectedSymbolAccessibility(within As Symbol,
                                                                  throughTypeOpt As TypeSymbol,
                                                                  originalContainingType As NamedTypeSymbol,
                                                                  basesBeingResolved As ConsList(Of Symbol),
                                                                  <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As AccessCheckResult
            Debug.Assert(TypeOf within Is NamedTypeSymbol OrElse TypeOf within Is AssemblySymbol)

            ' It is not an error to define protected member in a sealed Script class, it's just a
            ' warning. The member behaves like a private one - it is visible in all subsequent
            ' submissions.
            If originalContainingType.TypeKind = TypeKind.Submission Then
                Return AccessCheckResult.Accessible
            End If

            Dim withinType = TryCast(within, NamedTypeSymbol)
            If withinType Is Nothing Then
                ' If we're not within a type, we can't access a protected symbol
                Return AccessCheckResult.Inaccessible
            End If

            ' A protected symbol is accessible if we're (optionally nested) inside the type that it
            ' was defined in.  I.e., protected is a superset of private.
            ' We do this check up front as it is very fast and easy to do.
            If IsNestedWithinOriginalContainingType(withinType, originalContainingType) Then
                Return AccessCheckResult.Accessible
            End If

            ' Protected is quite confusing. It's a two-fold test:
            '    1) The class access is from ("withinType") is, or is inside, 
            '        a class derived from "originalContainingType"
            '    2) If there is a qualifier, and the member is not shared, 
            '       the qualifier must be an instance of the derived
            '       class in which the access occurred (any construction thereof)
            '
            ' The VB language spec describes a third test:
            '    3) If there is a qualifier, and the member is not shared, the qualifier
            '       must be of the exact construction of the derived class from which access occurred.
            ' But Dev10 actually implements something rather different:
            '    3) If there is a qualifier, for any member, shared or not, the construction of "originalContainingType" that
            '       the qualifier type inherits from is the same as the construction of "originalContainingType" that the 
            '       class through which access occurred.
            ' This rule (either the spec'd or Dev10 version) is intentionally not implemented here. See bug 4107.

            withinType = withinType.OriginalDefinition

            ' Determine whether accessible through inheritance
            Dim containedWithinDerived As Boolean = False
            Dim current = withinType

            While current IsNot Nothing
                Debug.Assert(current.IsDefinition)

                If InheritsFromIgnoringConstruction(current, originalContainingType, basesBeingResolved, useSiteDiagnostics) Then
                    containedWithinDerived = True
                    Exit While
                End If

                ' Note that the container of an original type is always original.
                current = current.ContainingType
            End While

            If Not containedWithinDerived Then
                Return AccessCheckResult.Inaccessible
            End If

            Dim originalThroughTypeOpt = If(throughTypeOpt Is Nothing, Nothing, throughTypeOpt.OriginalDefinition)

            If originalThroughTypeOpt Is Nothing Then
                ' Assuming the target member is Shared or is accessed through 'withinType'.
                Return AccessCheckResult.Accessible
            End If

            ' Any protected instance members in or visible in the current context
            ' through inheritance are accessible in the current context through an
            ' instance of the current context or any type derived from the current
            ' context.
            '
            ' eg:
            ' Class Cls1
            '    Protected Sub foo()
            '    End Sub
            ' End Class
            '
            ' Class Cls2
            '   Inherits Cls1
            '
            '    Sub Test()
            '        Dim obj1 as New Cls1
            '        Obj1.foo    'Not accessible
            '
            '        Dim obj2 as New Cls2
            '        Obj2.foo    'Accessible
            '    End Sub
            ' End Class
            If InheritsFromIgnoringConstruction(originalThroughTypeOpt, withinType, basesBeingResolved, useSiteDiagnostics) Then
                Return AccessCheckResult.Accessible
            End If


            ' Any protected instance members in or visible in an enclosing type through
            ' inheritance are accessible in the current context through an instance of
            ' that enclosing type.
            '
            ' eg:
            ' Class Cls1
            '    Protected Sub foo()
            '    End Sub
            ' End Class
            '
            ' Class Cls2
            '   Inherits Cls1
            '
            '    Protected Sub goo()
            '    End Sub
            '
            '    Class Cls2_1
            '      Sub Test()
            '        Dim obj2 as New Cls2
            '        Obj2.foo    'Accessible
            '
            '        Obj2.goo    'Accessible
            '      End Sub
            '    End Class
            ' End Class
            current = withinType

            While current IsNot Nothing
                Debug.Assert(current.IsDefinition)

                If current.Equals(originalThroughTypeOpt) Then
                    Return AccessCheckResult.Accessible
                End If

                ' Note that the container of an original type is always original.
                current = current.ContainingType
            End While

            Return AccessCheckResult.InaccessibleViaThroughType
        End Function

        ' Is a private symbol access OK.
        Private Shared Function CheckPrivateSymbolAccessibility(within As Symbol, originalContainingType As NamedTypeSymbol) As AccessCheckResult
            Debug.Assert(TypeOf within Is NamedTypeSymbol OrElse TypeOf within Is AssemblySymbol)

            Dim withinType = TryCast(within, NamedTypeSymbol)
            If withinType Is Nothing Then
                ' If we're not within a type, we can't access a private symbol
                Return AccessCheckResult.Inaccessible
            End If

            ' A private symbol is accessible if we're (optionally nested) inside the type that it
            ' was defined in.
            Return If(IsNestedWithinOriginalContainingType(withinType, originalContainingType), AccessCheckResult.Accessible, AccessCheckResult.Inaccessible)
        End Function

        ' Is the type "withinType" nested within the original type "originalContainingType".
        Private Shared Function IsNestedWithinOriginalContainingType(withinType As NamedTypeSymbol,
                                                                     originalContainingType As NamedTypeSymbol) As Boolean
            Debug.Assert(withinType IsNot Nothing)
            Debug.Assert(originalContainingType IsNot Nothing)

            ' Walk up my parent chain and see if I eventually hit the owner.  If so then I'm a
            ' nested type of that owner and I'm allowed access to everything inside of it.
            Dim current = withinType.OriginalDefinition

            While current IsNot Nothing
                Debug.Assert(current.IsDefinition)
                If current Is originalContainingType Then
                    Return True
                End If

                current = current.ContainingType
            End While

            Return False
        End Function

        ' Determine if "derivedType" inherits from "baseType", ignoring constructed types, and dealing
        ' only with original types.
        Private Shared Function InheritsFromIgnoringConstruction(derivedType As TypeSymbol,
                                                                 baseType As TypeSymbol,
                                                                 basesBeingResolved As ConsList(Of Symbol),
                                                                 <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)) As Boolean
            Debug.Assert(derivedType.IsDefinition)
            Debug.Assert(baseType.IsDefinition)

            Dim current As TypeSymbol = derivedType

            While current IsNot Nothing
                If current.Equals(baseType) Then
                    Return True
                End If

                If basesBeingResolved IsNot Nothing AndAlso basesBeingResolved.Contains(current) Then
                    ' We can't obtain BaseType if we're currently resolving the base type of current.
                    current = Nothing ' can't go up the base type chain.
                Else
                    ' NOTE: The base type of an 'original' type may not be 'original'. i.e. 
                    ' "class Foo : Inherits IBar(Of Integer)".  We must map it back to the 'original' when as we walk up
                    ' the base type hierarchy.
                    current = current.BaseTypeOriginalDefinition(useSiteDiagnostics)
                End If
            End While

            Return False
        End Function

        ' Does "fromAssembly" have friend accessibility to "toAssembly"?
        ' I.e., either 
        '   1. They are the same assembly
        '   2. toAssembly has an InternalsVisibleTo attribute that names fromAssembly
        '   3. They are both interactive assemblies.
        Public Shared Function HasFriendAccessTo(fromAssembly As AssemblySymbol, toAssembly As AssemblySymbol) As Boolean
            ' TODO: Implement by checking attributes, and also that interactive assemblies have access to each other.
            Return _
                IsSameAssembly(fromAssembly, toAssembly) OrElse
                InternalsAccessibleTo(toAssembly, fromAssembly)
        End Function

        ' Does "toAssembly" give access to assemblyWantingAccess via InternalVisibleTo?
        Private Shared Function InternalsAccessibleTo(toAssembly As AssemblySymbol, assemblyWantingAccess As AssemblySymbol) As Boolean
            ' checks if fromAssembly has friend assembly access to the internals in toAssembly
            If assemblyWantingAccess.AreInternalsVisibleToThisAssembly(toAssembly) Then
                Return True
            End If

            ' all interactive assemblies are friends of each other:
            If assemblyWantingAccess.IsInteractive AndAlso toAssembly.IsInteractive Then
                Return True
            End If

            Return False
        End Function

        Private Shared Function IsSameAssembly(fromAssembly As AssemblySymbol, toAssembly As AssemblySymbol) As Boolean
            Return Equals(fromAssembly, toAssembly)
        End Function

        ' Get the accessibility modifier for a symbol to put into an error message.
        Public Shared Function GetAccessibilityForErrorMessage(sym As Symbol, fromAssembly As AssemblySymbol) As String
            Dim access = sym.DeclaredAccessibility
            If access = Accessibility.ProtectedAndFriend Then
                ' No exact VB equivalent for this. If its in an accessible assembly, treat as protected,
                ' otherwise treat as friend.
                If AccessCheck.IsSymbolAccessible(sym.ContainingAssembly, fromAssembly, useSiteDiagnostics:=Nothing) Then
                    access = Accessibility.Protected
                Else
                    access = Accessibility.Friend
                End If
            End If

            Return access.ToDisplay()
        End Function

        ''' <summary>
        ''' Captures information about illegal access exposure.
        ''' </summary>
        ''' <remarks></remarks>
        Private Structure AccessExposure
            ''' <summary>
            ''' The exposed type.
            ''' </summary>
            Public ExposedType As TypeSymbol

            ''' <summary>
            ''' Namespace or type that "gains" access to the type.
            ''' </summary>
            Public ExposedTo As NamespaceOrTypeSymbol
        End Structure

        ''' <summary>
        ''' Returns true if there is no illegal access exposure, false otherwise.
        ''' </summary>
        ''' <param name="exposedThrough">
        ''' Type or member exposing the type.
        ''' </param>
        ''' <param name="exposedType">
        ''' The exposed type.
        ''' </param>
        ''' <param name="illegalExposure">
        ''' If function returns false, it requests an instance of ArrayBuilder from the pool and populates
        ''' it with information about illegal exposure. The caller is responsible for returning the ArrayBuilder
        ''' to the pool.
        ''' </param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Private Shared Function VerifyAccessExposure(
            exposedThrough As Symbol,
            exposedType As TypeSymbol,
            ByRef illegalExposure As ArrayBuilder(Of AccessExposure),
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
            Dim typeArgumentsExposureIsLegal As Boolean = True

            Dim exposedNamedType As NamedTypeSymbol

            ' Dig through to a NamedTypeSymbol
            Do
                Select Case exposedType.Kind
                    Case SymbolKind.TypeParameter, SymbolKind.ErrorType
                        Return True
                    Case SymbolKind.ArrayType
                        exposedType = DirectCast(exposedType, ArrayTypeSymbol).ElementType
                    Case SymbolKind.NamedType
                        exposedNamedType = DirectCast(exposedType, NamedTypeSymbol)
                        Exit Do
                End Select
            Loop

            ' For a generic type, verify exposure of each of the type arguments.
            Dim possiblyGeneric As NamedTypeSymbol = exposedNamedType

            Do
                If possiblyGeneric.Arity > 0 Then
                    For Each typeArgument In possiblyGeneric.TypeArgumentsNoUseSiteDiagnostics
                        If Not VerifyAccessExposure(exposedThrough, typeArgument, illegalExposure, useSiteDiagnostics) Then
                            typeArgumentsExposureIsLegal = False
                        End If
                    Next
                End If

                possiblyGeneric = possiblyGeneric.ContainingType
            Loop While possiblyGeneric IsNot Nothing

            ' Now, verify exposure of the type itself. Since the type arguments have been checked already,
            ' check the original definition of the type.
            Dim containerWithAccessError As NamespaceOrTypeSymbol = Nothing

            If VerifyAccessExposure(exposedThrough, exposedNamedType.OriginalDefinition, containerWithAccessError, useSiteDiagnostics) Then
                Return typeArgumentsExposureIsLegal
            End If

            If illegalExposure Is Nothing Then
                illegalExposure = ArrayBuilder(Of AccessExposure).GetInstance()
            End If

            illegalExposure.Add(New AccessExposure With {.ExposedType = exposedNamedType, .ExposedTo = containerWithAccessError})

            Return False
        End Function

        ''' <summary>
        ''' Returns true if there is no illegal access exposure, false otherwise.
        ''' </summary>
        Private Shared Function VerifyAccessExposure(
            exposedThrough As Symbol,
            exposedType As NamedTypeSymbol,
            ByRef containerWithAccessError As NamespaceOrTypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean

            containerWithAccessError = Nothing

            ' Do a quick check for public, top level types, all intrinsic types are like that.
            If exposedType.DeclaredAccessibility = Accessibility.Public Then
                Dim container = exposedType.ContainingSymbol

                If container IsNot Nothing AndAlso container.Kind = SymbolKind.Namespace Then
                    Return True
                End If
            End If

            If MemberIsOrNestedInType(exposedThrough, exposedType) Then
                Return True
            End If

            If Not VerifyAccessExposureWithinAssembly(exposedThrough, exposedType, containerWithAccessError, useSiteDiagnostics) Then
                Return False
            End If

            Return VerifyAccessExposureOutsideAssembly(exposedThrough, exposedType, useSiteDiagnostics)
        End Function

        ''' <summary>
        ''' Determine if member is the definition of the type, or 
        ''' is contained (directly or indirectly) in the definition of the type.
        ''' </summary>
        Private Shared Function MemberIsOrNestedInType(
            member As Symbol,
            type As NamedTypeSymbol
        ) As Boolean
            Debug.Assert(member.IsDefinition)
            type = type.OriginalDefinition

            If member.Equals(type) Then
                Return True
            End If

            Dim containingType As NamedTypeSymbol = member.ContainingType

            While containingType IsNot Nothing
                If containingType.Equals(type) Then
                    Return True
                End If

                containingType = containingType.ContainingType
            End While

            Return False
        End Function

        Private Shared Function VerifyAccessExposureWithinAssembly(
            exposedThrough As Symbol,
            exposedType As NamedTypeSymbol,
            ByRef containerWithAccessError As NamespaceOrTypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
            Return VerifyAccessExposureHelper(
                        exposedThrough,
                        exposedType,
                        containerWithAccessError,
                        Nothing,
                        isOutsideAssembly:=False,
                        useSiteDiagnostics:=useSiteDiagnostics)
        End Function

        Private Shared Function VerifyAccessExposureOutsideAssembly(
            exposedThrough As Symbol,
            exposedType As NamedTypeSymbol,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
            Dim memberAccessOutsideAssembly As Accessibility = GetEffectiveAccessOutsideAssembly(exposedThrough)

            Debug.Assert(memberAccessOutsideAssembly <> Accessibility.Friend, "How can the access be friend outside the assembly ?")

            If memberAccessOutsideAssembly = Accessibility.Private Then
                Return True
            End If

            Dim typeAccessOutsideAssembly As Accessibility = GetEffectiveAccessOutsideAssembly(exposedType)

            If typeAccessOutsideAssembly = Accessibility.Private Then
                Return False
            End If

            If typeAccessOutsideAssembly = Accessibility.Public Then
                Return True
            End If

            Debug.Assert(typeAccessOutsideAssembly = Accessibility.Protected, "What else can the Type access be outside the assembly ?")

            If memberAccessOutsideAssembly = Accessibility.Public Then
                Return False
            End If

            Debug.Assert(memberAccessOutsideAssembly = Accessibility.Protected, "What else can the Member access be outside the assembly ?")

            Dim typeSeenThroughInheritance As Boolean = False

            VerifyAccessExposureHelper(
                exposedThrough,
                exposedType,
                Nothing,
                typeSeenThroughInheritance,
                isOutsideAssembly:=True,
                useSiteDiagnostics:=useSiteDiagnostics)

            Return typeSeenThroughInheritance
        End Function


        Private Shared ReadOnly s_mapAccessToAccessOutsideAssembly() As Accessibility

        Shared Sub New()
            s_mapAccessToAccessOutsideAssembly = New Accessibility(Accessibility.Public) {}

            s_mapAccessToAccessOutsideAssembly(Accessibility.NotApplicable) = Accessibility.NotApplicable
            s_mapAccessToAccessOutsideAssembly(Accessibility.Private) = Accessibility.Private
            s_mapAccessToAccessOutsideAssembly(Accessibility.ProtectedAndFriend) = Accessibility.Private
            s_mapAccessToAccessOutsideAssembly(Accessibility.Protected) = Accessibility.Protected
            s_mapAccessToAccessOutsideAssembly(Accessibility.Friend) = Accessibility.Private
            s_mapAccessToAccessOutsideAssembly(Accessibility.ProtectedOrFriend) = Accessibility.Protected
            s_mapAccessToAccessOutsideAssembly(Accessibility.Public) = Accessibility.Public

        End Sub

        Private Shared Function GetEffectiveAccessOutsideAssembly(
            symbol As Symbol
        ) As Accessibility
            Dim effectiveAccess As Accessibility = s_mapAccessToAccessOutsideAssembly(symbol.DeclaredAccessibility)

            If effectiveAccess = Accessibility.Private Then
                Return effectiveAccess
            End If

            Dim enclosingType As NamedTypeSymbol = symbol.ContainingType

            Do While enclosingType IsNot Nothing

                Dim accessOfContainer As Accessibility = s_mapAccessToAccessOutsideAssembly(enclosingType.DeclaredAccessibility)

                If accessOfContainer < effectiveAccess Then
                    effectiveAccess = accessOfContainer
                End If

                If effectiveAccess = Accessibility.Private Then
                    Return effectiveAccess
                End If

                ' Increment For loop
                enclosingType = enclosingType.ContainingType
            Loop ' End For

            Return effectiveAccess
        End Function

        Private Shared Function GetAccessInAssemblyContext(
            symbol As Symbol,
            isOutsideAssembly As Boolean
        ) As Accessibility
            Dim accessOfMember As Accessibility = symbol.DeclaredAccessibility

            If isOutsideAssembly Then
                accessOfMember = s_mapAccessToAccessOutsideAssembly(accessOfMember)
            End If

            Return accessOfMember
        End Function


        Private Shared Function IsTypeNestedIn(
            probablyNestedType As NamedTypeSymbol,
            probablyEnclosingType As NamedTypeSymbol
        ) As Boolean
            Debug.Assert(probablyEnclosingType.IsDefinition)
            probablyNestedType = probablyNestedType.OriginalDefinition

            Dim containingType As NamedTypeSymbol = probablyNestedType.ContainingType

            While containingType IsNot Nothing
                If containingType.Equals(probablyEnclosingType) Then
                    Return True
                End If

                containingType = containingType.ContainingType
            End While

            Return False
        End Function

        ''' <summary>
        ''' Returns true if there is no illegal access exposure, false otherwise.
        ''' 
        ''' Four cases:
        ''' 1: Member is not protected, non of its enclosing scopes are protected
        ''' 2: Member is not protected, but some of its enclosing scopes are protected
        ''' 3: Member is protected, non of its enclosing scopes are protected
        ''' 4: Member is protected, some of its enclosing scopes are also protected
        ''' </summary>
        Private Shared Function VerifyAccessExposureHelper(
            exposingMember As Symbol,
            exposedType As NamedTypeSymbol,
            ByRef containerWithAccessError As NamespaceOrTypeSymbol,
            ByRef seenThroughInheritance As Boolean,
            isOutsideAssembly As Boolean,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
            seenThroughInheritance = False
            Dim exposingType As NamedTypeSymbol = Nothing
            Dim membersAccessibilityInAssemblyContext As Accessibility = GetAccessInAssemblyContext(exposingMember, isOutsideAssembly)

            If membersAccessibilityInAssemblyContext = Accessibility.Private Then

                ' Continue checking for nested types because the fact that the enclosing type is private 
                ' doesn't mean that it is OK to expose the nested type.
                If Not (exposingMember.Kind = SymbolKind.NamedType AndAlso IsTypeNestedIn(exposedType, DirectCast(exposingMember, NamedTypeSymbol))) Then
                    Return True
                End If

                Debug.Assert(exposingMember.Kind = SymbolKind.NamedType)
                exposingType = DirectCast(exposingMember, NamedTypeSymbol)
            Else
                Dim StopAtAccess As Accessibility = Accessibility.Protected

                exposingType = FindEnclosingTypeWithGivenAccess(exposingMember, StopAtAccess, isOutsideAssembly)
            End If

            Dim exposingTypeAccessibilityInAssemblyContext As Accessibility = GetAccessInAssemblyContext(exposingType, isOutsideAssembly)
            Dim parentOfExposingType As NamespaceOrTypeSymbol

            If membersAccessibilityInAssemblyContext <= Accessibility.Protected Then
                If CanBeAccessedThroughInheritance(exposedType, exposingMember.ContainingType, isOutsideAssembly, useSiteDiagnostics) Then
                    seenThroughInheritance = True
                    Return True
                End If
            End If

            parentOfExposingType = exposingType.ContainingNamespaceOrType

            If CheckNamedTypeAccessibility(exposedType,
                                         If(parentOfExposingType.IsNamespace,
                                            DirectCast(parentOfExposingType.ContainingAssembly, Symbol),
                                            parentOfExposingType), Nothing,
                                        useSiteDiagnostics) <> AccessCheckResult.Accessible Then
                containerWithAccessError = parentOfExposingType
                Return False
            End If

            If exposingTypeAccessibilityInAssemblyContext <> Accessibility.Protected Then
                ' Case 1, 3
                Return True

            Else
                Debug.Assert(exposingTypeAccessibilityInAssemblyContext = Accessibility.Protected)

                ' Case 2, 4
                Return VerifyAccessExposureHelper(
                    exposingType,
                    exposedType,
                    containerWithAccessError,
                    seenThroughInheritance,
                    isOutsideAssembly,
                    useSiteDiagnostics)
            End If
        End Function

        ''' <summary>
        ''' Can type be accessed through container's inheritance?
        ''' </summary>
        Private Shared Function CanBeAccessedThroughInheritance(
            type As NamedTypeSymbol,
            container As NamedTypeSymbol,
            isOutsideAssembly As Boolean,
            <[In], Out> ByRef useSiteDiagnostics As HashSet(Of DiagnosticInfo)
        ) As Boolean
            If GetAccessInAssemblyContext(type, isOutsideAssembly) = Accessibility.Private Then
                Return False
            End If

            'EDMAURER This assert is incorrect. It fires in the shipping Dev10 compiler in a simple InternalsVisibleScenario
            'in which a Shared type in an assembly with IVT is used as a parameter of a method in the assembly being compiled. 
            'See Roslyn bug 6174.
            'Debug.Assert(Not (GetAccessInAssemblyContext(type, isOutsideAssembly) = Accessibility.Friend AndAlso
            '                  type.ContainingAssembly IsNot container.ContainingAssembly), _
            '             "This should have been caught when checking for inaccessibility during type resolution!!!")

            Dim containerOfType As NamedTypeSymbol = type.ContainingType

            If containerOfType Is Nothing Then
                Return False
            End If

            ' Protected Access in VB ignores type arguments, so do all comparisons on
            ' original definitions (see bug 12219, for example).
            Dim containerOfTypeDefinition = containerOfType.OriginalDefinition

            If container.OriginalDefinition.Equals(containerOfTypeDefinition) Then
                Return True
            ElseIf container.IsInterfaceType() Then
                If containerOfType.IsInterfaceType() Then
                    For Each iface In container.AllInterfacesWithDefinitionUseSiteDiagnostics(useSiteDiagnostics)
                        If iface.OriginalDefinition.Equals(containerOfTypeDefinition) Then
                            Return True
                        End If
                    Next
                End If
            ElseIf Not containerOfType.IsInterfaceType() Then
                Dim baseDefinition = container.BaseTypeOriginalDefinition(useSiteDiagnostics)

                While baseDefinition IsNot Nothing
                    If baseDefinition.Equals(containerOfTypeDefinition) Then
                        Return True
                    End If

                    baseDefinition = baseDefinition.BaseTypeOriginalDefinition(useSiteDiagnostics)
                End While
            End If

            If GetAccessInAssemblyContext(type, isOutsideAssembly) <> Accessibility.Protected Then
                Return CanBeAccessedThroughInheritance(
                                                        containerOfType,
                                                        container,
                                                        isOutsideAssembly,
                                                        useSiteDiagnostics)
            End If

            Return False
        End Function


        ''' <summary>
        ''' This function finds the inner most enclosing scope whose Access
        ''' is lesser than or equal to the given access "StopAtAccess".
        ''' </summary>
        ''' <param name="member">Member - for which the enclosing scope has to be found</param>
        ''' <param name="stopAtAccess">the enclosing scope's access has to be lesser than</param>
        ''' <param name="isOutsideAssembly"></param>
        Private Shared Function FindEnclosingTypeWithGivenAccess(
            member As Symbol,
            stopAtAccess As Accessibility,
            isOutsideAssembly As Boolean
        ) As NamedTypeSymbol
            Debug.Assert(member.Kind <> SymbolKind.Namespace, "How can a Member be a namespace ?")
            Debug.Assert(member.IsDefinition)

            Dim enclosingType As NamedTypeSymbol = member.ContainingType

            If member.Kind = SymbolKind.NamedType Then
                ' Do not bubble up to a namespace.
                If enclosingType Is Nothing Then
                    enclosingType = DirectCast(member, NamedTypeSymbol)
                End If
            End If

            Debug.Assert(enclosingType IsNot Nothing)

            Do
                Dim nextEnclosingType = enclosingType.ContainingType

                If nextEnclosingType Is Nothing Then
                    ' Do not bubble up to a namespace.
                    Exit Do
                End If

                Dim EnclosingContainerAccess As Accessibility = GetAccessInAssemblyContext(enclosingType, isOutsideAssembly)

                If EnclosingContainerAccess <= stopAtAccess Then
                    Exit Do
                End If

                enclosingType = nextEnclosingType
            Loop

            Return enclosingType
        End Function

        ''' <summary>
        ''' Returns false if there were errors reported due to access exposure, true otherwise.
        ''' </summary>
        Public Shared Function VerifyAccessExposureOfBaseClassOrInterface(
            classOrInterface As NamedTypeSymbol,
            baseClassSyntax As TypeSyntax,
            base As TypeSymbol,
            diagBag As DiagnosticBag
        ) As Boolean
            Debug.Assert(base.IsClassType() OrElse base.IsInterfaceType(), "Expected class or interface!!!")

            Dim illegalExposure As ArrayBuilder(Of AccessExposure) = Nothing
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            VerifyAccessExposure(classOrInterface, base, illegalExposure, useSiteDiagnostics)

            diagBag.Add(baseClassSyntax, useSiteDiagnostics)

            If illegalExposure IsNot Nothing Then

                Debug.Assert(illegalExposure.Count > 0)

                For Each accessExposure In illegalExposure
                    Dim containerAtWhichAccessErrorOccurs As NamespaceOrTypeSymbol = accessExposure.ExposedTo

                    Dim exposedType As TypeSymbol = accessExposure.ExposedType.DigThroughArrayType()

                    If containerAtWhichAccessErrorOccurs IsNot Nothing Then
                        If exposedType.Equals(base) Then
                            ' "'|1' cannot inherit from |2 '|3' because it expands the access of the base |2 to |4 '|5'."
                            Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritanceAccessMismatch5,
                                                    classOrInterface.Name,
                                                    base.GetKindText(),
                                                    base.ToErrorMessageArgument(),
                                                    containerAtWhichAccessErrorOccurs.GetKindText(),
                                                    containerAtWhichAccessErrorOccurs.ToErrorMessageArgument())
                        Else
                            ' generic type argument is being exposed

                            ' "'|1' cannot inherit from |2 '|3' because it expands the access of type '|4' to |5 '|6'."
                            Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritsTypeArgAccessMismatch7,
                                                    classOrInterface.Name,
                                                    base.GetKindText(),
                                                    base.ToErrorMessageArgument(),
                                                    exposedType,
                                                    containerAtWhichAccessErrorOccurs.GetKindText(),
                                                    containerAtWhichAccessErrorOccurs.ToErrorMessageArgument())
                        End If

                    Else
                        If exposedType.Equals(base) Then
                            ' "'|1' cannot inherit from |2 '|3' because it expands the access of the base |2 outside the assembly."
                            Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritanceAccessMismatchOutside3,
                                                    classOrInterface.Name,
                                                    base.GetKindText(),
                                                    base.ToErrorMessageArgument())

                        Else
                            ' generic type argument is being exposed

                            ' "'|1' cannot inherit from |2 '|3' because it expands the access of type '|4' outside the assembly."
                            Binder.ReportDiagnostic(diagBag, baseClassSyntax, ERRID.ERR_InheritsTypeArgAccessMismatchOutside5,
                                                    classOrInterface.Name,
                                                    base.GetKindText(),
                                                    base.ToErrorMessageArgument(),
                                                    exposedType)
                        End If
                    End If
                Next

                illegalExposure.Free()

                Return False
            End If

            Return True
        End Function


        Public Shared Sub VerifyAccessExposureForParameterType(
            member As Symbol,
            paramName As String,
            errorLocation As VisualBasicSyntaxNode,
            TypeBehindParam As TypeSymbol,
            diagBag As DiagnosticBag
        )
            Dim illegalExposure As ArrayBuilder(Of AccessExposure) = Nothing
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            VerifyAccessExposure(member, TypeBehindParam, illegalExposure, useSiteDiagnostics)

            diagBag.Add(errorLocation, useSiteDiagnostics)

            If illegalExposure IsNot Nothing Then
                Debug.Assert(illegalExposure.Count > 0)

                For Each accessExposure In illegalExposure
                    Dim containerAtWhichAccessErrorOccurs As NamespaceOrTypeSymbol = accessExposure.ExposedTo
                    Dim exposedType As TypeSymbol = accessExposure.ExposedType.DigThroughArrayType()

                    Dim membersContainer As NamedTypeSymbol = member.ContainingType

                    If containerAtWhichAccessErrorOccurs IsNot Nothing Then
                        ' "'|1' cannot expose type '|2' to the scope of |3 '|4' through |5 '|6'."
                        Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_AccessMismatch6,
                                                paramName,
                                                exposedType,
                                                containerAtWhichAccessErrorOccurs.GetKindText(),
                                                containerAtWhichAccessErrorOccurs.ToErrorMessageArgument(),
                                                membersContainer.GetKindText(),
                                                membersContainer.Name)

                    Else
                        ' "'|1' cannot expose type '|2' outside the project through |3 '|4'."
                        Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_AccessMismatchOutsideAssembly4,
                                                paramName,
                                                exposedType,
                                                membersContainer.GetKindText(),
                                                membersContainer.Name)
                    End If
                Next

                illegalExposure.Free()
            End If
        End Sub

        Public Shared Sub VerifyAccessExposureForMemberType(
            member As Symbol,
            errorLocation As SyntaxNodeOrToken,
            typeBehindMember As TypeSymbol,
            diagBag As DiagnosticBag,
            Optional isDelegateFromImplements As Boolean = False
        )
            Dim illegalExposure As ArrayBuilder(Of AccessExposure) = Nothing
            Dim useSiteDiagnostics As HashSet(Of DiagnosticInfo) = Nothing

            VerifyAccessExposure(member, typeBehindMember, illegalExposure, useSiteDiagnostics)

            diagBag.Add(errorLocation, useSiteDiagnostics)

            If illegalExposure IsNot Nothing Then
                Debug.Assert(illegalExposure.Count > 0)

                Dim membersContainer As NamedTypeSymbol

                If member.Kind = SymbolKind.NamedType Then
                    membersContainer = DirectCast(member, NamedTypeSymbol)
                Else
                    membersContainer = member.ContainingType
                End If

                ' This is needed when we find a error on the Delegate Invoke.
                ' When this happens, the error has to be report for the delegate
                ' and not the invoke which is not in user code.
                Dim nameToReportInError As String = If(membersContainer.IsDelegateType(), membersContainer.Name, member.Name)

                For Each accessExposure In illegalExposure
                    Dim containerAtWhichAccessErrorOccurs As NamespaceOrTypeSymbol = accessExposure.ExposedTo

                    Dim exposedType As TypeSymbol = accessExposure.ExposedType.DigThroughArrayType()

                    If containerAtWhichAccessErrorOccurs IsNot Nothing Then
                        If isDelegateFromImplements Then

                            '' // "'|1' cannot expose the underlying delegate type '|2' of the event it is implementing to |3 '|4' through |5 '|6'."
                            ' //
                            Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_AccessMismatchImplementedEvent6,
                                nameToReportInError,
                                exposedType,
                                containerAtWhichAccessErrorOccurs.GetKindText(),
                                containerAtWhichAccessErrorOccurs.ToErrorMessageArgument(),
                                membersContainer.GetKindText(),
                                membersContainer.Name)

                        Else
                            Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_AccessMismatch6,
                                                    nameToReportInError,
                                                    exposedType,
                                                    containerAtWhichAccessErrorOccurs.GetKindText(),
                                                    containerAtWhichAccessErrorOccurs.ToErrorMessageArgument(),
                                                    membersContainer.GetKindText(),
                                                    membersContainer.Name)
                        End If
                    Else
                        If isDelegateFromImplements Then
                            ' // "'|1' cannot expose the underlying delegate type '|2' of the event it is implementing outside the project through |3 '|4'."
                            ' //
                            Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_AccessMismatchImplementedEvent4,
                                                    nameToReportInError,
                                                    exposedType,
                                                    membersContainer.GetKindText(),
                                                    membersContainer.Name)
                        Else
                            ' "'|1' cannot expose type '|2' outside the project through |3 '|4'."
                            Binder.ReportDiagnostic(diagBag, errorLocation, ERRID.ERR_AccessMismatchOutsideAssembly4,
                                                    nameToReportInError,
                                                    exposedType,
                                                    membersContainer.GetKindText(),
                                                    membersContainer.Name)
                        End If
                    End If
                Next

                illegalExposure.Free()
            End If
        End Sub

    End Class

End Namespace
