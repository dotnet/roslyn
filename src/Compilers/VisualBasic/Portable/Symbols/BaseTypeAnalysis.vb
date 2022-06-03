' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Text
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Metadata.PE
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend Module BaseTypeAnalysis

        ''' <summary>
        ''' a link in a dependency chain
        ''' it means that "dependent" is dependent on the rest of the chain.
        ''' "kind" tells what kind of dependency this is.
        ''' </summary>
        Private Structure DependencyDesc
            Public ReadOnly kind As DependencyKind
            Public ReadOnly dependent As TypeSymbol

            Friend Sub New(kind As DependencyKind, dependent As TypeSymbol)
                Me.kind = kind
                Me.dependent = dependent
            End Sub

        End Structure

        ''' <summary>
        ''' Source types may have dependencies via inheritance or containment
        ''' The diagnostics is different in those cases.
        ''' </summary>
        Private Enum DependencyKind
            Inheritance
            Containment
        End Enum

        ''' <summary>
        ''' Given base being resolved chain and current type produce the diagnostics 
        ''' or Nothing if there is no cycle detected
        ''' </summary>
        Friend Function GetDependenceDiagnosticForBase(this As SourceNamedTypeSymbol, basesBeingResolved As BasesBeingResolved) As DiagnosticInfo
            Dim hasContainment As Boolean = False
            Dim current As ConsList(Of TypeSymbol) = basesBeingResolved.InheritsBeingResolvedOpt
            Dim previous As NamedTypeSymbol = this
            Dim dependency As ConsList(Of DependencyDesc) = ConsList(Of DependencyDesc).Empty.Prepend(New DependencyDesc(DependencyKind.Inheritance, this))
            Dim count As Integer = 1

            While current.Any
                Debug.Assert(current.Head.Kind = SymbolKind.NamedType)
                Dim head = DirectCast(current.Head, NamedTypeSymbol)

                If head Is this Then
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_IllegalBaseTypeReferences3, this.GetKindText(), this, GetBaseTypeReferenceDetails(dependency))
                End If

                If Not this.DetectTypeCircularity_ShouldStepIntoType(head) Then
                    ' Effectively break cycle detection if 'this' is not the 'smallest'
                    ' type of the cycle ensuring that the cycle will only be detected 
                    ' and reported on the smallest type in the cycle
                    Return Nothing
                End If

                ' What is the relationship between 'head' and the next type in 'basesBeingResolved'?
                Dim isContainment As Boolean = previous IsNot Nothing AndAlso head.ContainingSymbol Is previous
                If isContainment Then
                    hasContainment = True
                End If

                dependency = dependency.Prepend(New DependencyDesc(If(hasContainment, DependencyKind.Containment, DependencyKind.Inheritance), head))
                count += 1

                previous = head
                current = current.Tail
            End While

            ' No cycle detected
            Return Nothing
        End Function

        ''' <summary>
        ''' Detects situations when a type participates in a dependency loop
        ''' And generates appropriate diagnostics.
        ''' No diagnostics means there was no loop
        ''' </summary>
        Friend Function GetDependenceDiagnosticForBase(this As SourceNamedTypeSymbol,
                                                       base As TypeSymbol) As DiagnosticInfo

            ' we should not call this for Implements.
            Debug.Assert(this.IsInterface OrElse Not base.IsInterfaceType())

            Dim dependency = GetDependenceChain(
                New HashSet(Of Symbol),
                DirectCast(this.OriginalDefinition, SourceNamedTypeSymbol),
                base)

            ' common case - not dependent
            If dependency Is Nothing Then
                Return Nothing
            End If

            ' we know that "this" inherits "base".
            dependency = dependency.Prepend(New DependencyDesc(DependencyKind.Inheritance, this))

            ' figure if there are containment links in the chain and how long is the chain
            ' it results in different error IDs
            Dim count = 0
            Dim hasContainment As Boolean = False
            For Each d In dependency
                If d.kind = DependencyKind.Containment Then
                    hasContainment = True
                End If

                count += 1
            Next

            ' loop via inheritance only
            If Not hasContainment Then
                If this.TypeKind = TypeKind.Class Then
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_InheritanceCycle1, this, GetInheritanceDetails(dependency))
                Else
                    Debug.Assert(this.IsInterface)
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_InterfaceCycle1, this, GetInheritanceDetails(dependency))
                End If
            End If

            ' deriving from container or a more complex case
            If count > 2 Then
                Return ErrorFactory.ErrorInfo(ERRID.ERR_CircularBaseDependencies4,
                                              this.GetKindText(),
                                              this,
                                              GetInheritanceDetails(dependency))
            Else
                Return ErrorFactory.ErrorInfo(ERRID.ERR_NestedBase2, this.GetKindText(), this)
            End If
        End Function

        Private Function GetInheritanceDetails(chain As ConsList(Of DependencyDesc)) As DiagnosticInfo
            Return GetInheritanceOrDependenceDetails(chain, ERRID.ERR_InheritsFrom2)
        End Function

        Private Function GetBaseTypeReferenceDetails(chain As ConsList(Of DependencyDesc)) As DiagnosticInfo
            Return GetInheritanceOrDependenceDetails(chain, ERRID.ERR_BaseTypeReferences2)
        End Function

        Private Function GetInheritanceOrDependenceDetails(chain As ConsList(Of DependencyDesc), inheritsOrDepends As ERRID) As DiagnosticInfo
            Dim details = ArrayBuilder(Of DiagnosticInfo).GetInstance()

            Dim dependent As DependencyDesc = chain.Head
            Dim detailErrId As ERRID

            For Each current In chain.Tail
                Select Case dependent.kind
                    Case DependencyKind.Containment
                        'Build string for:  " 'current' is nested in 'dependent'."
                        detailErrId = ERRID.ERR_IsNestedIn2
                    Case Else
                        'Build string for:  " 'dependent' inherits from 'current'."
                        detailErrId = inheritsOrDepends
                End Select
                details.Add(ErrorFactory.ErrorInfo(detailErrId, dependent.dependent, current.dependent))
                dependent = current
            Next

            ' deal with last dependent - it depends on the head of the chain        
            Select Case dependent.kind
                Case DependencyKind.Containment
                    'Build string for:  " 'current' is nested in 'dependent'."
                    detailErrId = ERRID.ERR_IsNestedIn2
                Case Else
                    'Build string for:  " 'dependent' inherits from 'current'."
                    detailErrId = inheritsOrDepends
            End Select
            details.Add(ErrorFactory.ErrorInfo(detailErrId, dependent.dependent, chain.Head.dependent))

            Return New CompoundDiagnosticInfo(details.ToArrayAndFree())
        End Function

        ''' <summary>
        ''' if there is a dependency chain from "current" to the "root"
        ''' Returning Nothing, means that there is no dependency
        ''' Returning Empty, means that root and current are the same and we have a 0-length dependency
        ''' Otherwise a dependence chain is formed.
        ''' </summary>
        Private Function GetDependenceChain(visited As HashSet(Of Symbol),
                                               root As SourceNamedTypeSymbol,
                                               current As TypeSymbol) As ConsList(Of DependencyDesc)
            Debug.Assert(TypeSymbol.Equals(root.OriginalDefinition, root, TypeCompareKind.ConsiderEverything), "root must not be a substitution")

            If current Is Nothing OrElse current.Kind = SymbolKind.ErrorType Then
                Return Nothing
            End If

            Dim currentDef = current.OriginalDefinition

            If root Is currentDef Then
                ' root and current are the same symbol 
                ' it means there is a 0-length dependency
                Return ConsList(Of DependencyDesc).Empty
            End If

            If Not visited.Add(current) Then
                ' we have seen this already
                Return Nothing
            End If

            Dim currentNamedType = TryCast(currentDef, NamedTypeSymbol)
            If currentNamedType Is Nothing Then
                ' if current is not a named type, we can assume it does not depend on other types
                Return Nothing
            End If

            Dim chain As ConsList(Of DependencyDesc) = Nothing

            ' try getting to the root via containing type
            chain = GetDependenceChain(visited, root, currentNamedType.ContainingType)
            If chain IsNot Nothing Then
                Return chain.Prepend(New DependencyDesc(DependencyKind.Containment, current))
            End If

            If Not root.DetectTypeCircularity_ShouldStepIntoType(currentNamedType) Then
                ' we don't step into 'smaller' types to ensure we report 
                ' circularities only on the smallest ones
                Return Nothing
            End If

            ' try getting to the root via base
            If currentNamedType.TypeKind = TypeKind.Class Then
                Dim declaredBase = currentNamedType.GetBestKnownBaseType()
                chain = GetDependenceChain(visited, root, declaredBase)
                If chain IsNot Nothing Then
                    Return chain.Prepend(New DependencyDesc(DependencyKind.Inheritance, current))
                End If
            End If

            ' try getting to the root via Interfaces
            If currentNamedType.IsInterface Then
                Dim declaredInterfaces = currentNamedType.GetBestKnownInterfacesNoUseSiteDiagnostics()
                For Each declaredInterface In declaredInterfaces
                    chain = GetDependenceChain(visited, root, declaredInterface)
                    If chain IsNot Nothing Then
                        Return chain.Prepend(New DependencyDesc(DependencyKind.Inheritance, current))
                    End If
                Next
            End If

            Return Nothing
        End Function

        ''' <summary>
        ''' Verifies that given symbol does not have loops in its inheritance chain
        ''' and reports appropriate diagnostics.
        ''' </summary>
        Friend Function GetDependencyDiagnosticsForImportedClass(this As NamedTypeSymbol) As DiagnosticInfo
            ' dependency analysis for PE types is different because it reports errors not only when a type is 
            ' involved in a loop itself, but also if any of its bases does.
            ' on the other hand we do not need to produce the chain, just need to tell if there was a loop
            ' so we use a simple hare/tortoise here.

            Dim slow = this.OriginalDefinition
            If (slow Is Nothing) Then
                Return Nothing
            End If

            'fast moves up the chain 2x faster than slow. If they meet before fast hits Nothing, we have a reachable cycle
            Dim fast = this.GetDeclaredBase(Nothing)

            While fast IsNot Nothing

                fast = TryCast(fast.OriginalDefinition, NamedTypeSymbol)
                If slow Is fast Then
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_InheritanceCycleInImportedType1, this)
                End If

                'move fast
                fast = fast.GetDeclaredBase(Nothing)
                If fast Is Nothing Then
                    Exit While
                End If

                fast = TryCast(fast.OriginalDefinition, NamedTypeSymbol)
                If slow Is fast Then
                    Return ErrorFactory.ErrorInfo(ERRID.ERR_InheritanceCycleInImportedType1, this)
                End If

                'move fast again
                fast = fast.GetDeclaredBase(Nothing)

                'move slow
                slow = slow.GetDeclaredBase(Nothing).OriginalDefinition
            End While

            Return Nothing
        End Function

        ''' <summary>
        ''' Verifies that given symbol does not have loops in its inheritance hierarchy
        ''' and reports appropriate diagnostics.
        ''' </summary>
        Friend Function GetDependencyDiagnosticsForImportedBaseInterface(this As NamedTypeSymbol, base As NamedTypeSymbol) As DiagnosticInfo
            ' dependency analysis for PE interfaces is similar to what we do for classes
            ' however multiple inheritance makes things a bit more complicated 
            ' we use DFS to search for loops in the hierarchy

            base = TryCast(base.OriginalDefinition, NamedTypeSymbol)
            If base Is Nothing Then
                Return Nothing
            End If

            Dim derived As New HashSet(Of TypeSymbol)
            derived.Add(base)

            ' interface inheritance trees often contain joins so we record all verified nodes 
            ' so that we do not check them again and go exponential.
            Dim verified As New HashSet(Of TypeSymbol)

            If HasCycles(derived, verified, base) Then
                Return ErrorFactory.ErrorInfo(ERRID.ERR_InheritanceCycleInImportedType1, this)
            End If

            Return Nothing
        End Function

        ' DFS traverse that checks that interface does not have bases that form dependency loops
        ' interface may have a dependency loop by 
        '     depending on anything in the derived set or 
        '     by recursively having a base with a dependency loop.
        Private Function HasCycles(derived As HashSet(Of TypeSymbol), verified As HashSet(Of TypeSymbol), [interface] As NamedTypeSymbol) As Boolean
            Dim bases = [interface].GetDeclaredInterfacesNoUseSiteDiagnostics(Nothing)
            If Not bases.IsEmpty Then
                For Each base In bases
                    base = TryCast(base.OriginalDefinition, NamedTypeSymbol)

                    If (base Is Nothing) Then
                        Continue For   ' not a named type
                    End If

                    If verified.Contains(base) Then
                        Continue For ' seen this one. no need to check it again.
                    End If

                    If Not derived.Add(base) Then
                        ' woa, having a loop right here!
                        Return True
                    Else
                        If HasCycles(derived, verified, base) Then
                            Return True ' bubble up. something in bases has a loop
                        End If
                    End If
                Next
            End If

            ' this interface is verified to not have inheritance loops in its hierarchy
            verified.Add([interface])
            derived.Remove([interface])
            Return Nothing
        End Function
    End Module

End Namespace
