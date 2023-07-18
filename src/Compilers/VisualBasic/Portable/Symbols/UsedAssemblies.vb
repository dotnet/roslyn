' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis.PooledObjects
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols.Retargeting

Namespace Microsoft.CodeAnalysis.VisualBasic

    Partial Public Class VisualBasicCompilation

        Private _lazyUsedAssemblyReferences As ConcurrentSet(Of AssemblySymbol)
        Private _usedAssemblyReferencesFrozen As Boolean

        Public Overrides Function GetUsedAssemblyReferences(Optional cancellationToken As CancellationToken = Nothing) As ImmutableArray(Of MetadataReference)
            Dim usedAssemblies As ConcurrentSet(Of AssemblySymbol) = GetCompleteSetOfUsedAssemblies(cancellationToken)

            If usedAssemblies Is Nothing Then
                Return ImmutableArray(Of MetadataReference).Empty
            End If

            ' Use stable ordering for the result, matching the order in References.
            Dim builder = ArrayBuilder(Of MetadataReference).GetInstance(usedAssemblies.Count)

            For Each reference In References
                If reference.Properties.Kind = MetadataImageKind.Assembly Then
                    Dim symbol As Symbol = GetBoundReferenceManager().GetReferencedAssemblySymbol(reference)

                    If symbol IsNot Nothing AndAlso usedAssemblies.Contains(DirectCast(symbol, AssemblySymbol)) Then
                        builder.Add(reference)
                    End If
                End If
            Next

            Return builder.ToImmutableAndFree()
        End Function

        Private Function GetCompleteSetOfUsedAssemblies(cancellationToken As CancellationToken) As ConcurrentSet(Of AssemblySymbol)

            If Not _usedAssemblyReferencesFrozen AndAlso Not Volatile.Read(_usedAssemblyReferencesFrozen) Then

                Dim diagnostics = BindingDiagnosticBag.GetConcurrentInstance()
                RoslynDebug.Assert(diagnostics.AccumulatesDiagnostics)

                GetDiagnosticsWithoutFiltering(CompilationStage.Declare, includeEarlierStages:=True, diagnostics, cancellationToken)

                Dim seenErrors As Boolean = diagnostics.HasAnyErrors()

                If Not seenErrors Then
                    diagnostics.DiagnosticBag.Clear()
                    GetDiagnosticsForAllMethodBodies(hasDeclarationErrors:=False, diagnostics, doLowering:=True, cancellationToken)
                    seenErrors = diagnostics.HasAnyErrors()

                    If Not seenErrors Then
                        AddUsedAssemblies(diagnostics.DependenciesBag)
                    End If
                End If

                CompleteTheSetOfUsedAssemblies(seenErrors, cancellationToken)

                diagnostics.Free()
            End If

            Return _lazyUsedAssemblyReferences
        End Function

        Private Sub AddUsedAssembly(dependency As AssemblySymbol, stack As ArrayBuilder(Of AssemblySymbol))
            If AddUsedAssembly(dependency) Then
                stack.Push(dependency)
            End If
        End Sub

        Private Sub AddReferencedAssemblies(assembly As AssemblySymbol, includeMainModule As Boolean, stack As ArrayBuilder(Of AssemblySymbol))
            For i As Integer = If(includeMainModule, 0, 1) To assembly.Modules.Length - 1
                For Each dependency In assembly.Modules(i).ReferencedAssemblySymbols
                    AddUsedAssembly(dependency, stack)
                Next
            Next
        End Sub

        Private Sub CompleteTheSetOfUsedAssemblies(seenErrors As Boolean, cancellationToken As CancellationToken)

            If _usedAssemblyReferencesFrozen OrElse Volatile.Read(_usedAssemblyReferencesFrozen) Then
                Return
            End If

            If seenErrors Then
                ' Add all referenced assemblies
                For Each assembly As AssemblySymbol In SourceModule.ReferencedAssemblySymbols
                    AddUsedAssembly(assembly)
                Next
            Else
                ' Assume that all assemblies used by the added modules are also used
                For i As Integer = 1 To SourceAssembly.Modules.Length - 1
                    For Each dependency In SourceAssembly.Modules(i).ReferencedAssemblySymbols
                        AddUsedAssembly(dependency)
                    Next
                Next

                If _usedAssemblyReferencesFrozen OrElse Volatile.Read(_usedAssemblyReferencesFrozen) Then
                    Return
                End If

                ' Assume that all assemblies used by the used assemblies are also used
                ' This, for example, takes care of including facade assemblies that forward types around.
                If _lazyUsedAssemblyReferences IsNot Nothing Then
                    SyncLock _lazyUsedAssemblyReferences
                        If _usedAssemblyReferencesFrozen OrElse Volatile.Read(_usedAssemblyReferencesFrozen) Then
                            Return
                        End If

                        Dim stack = ArrayBuilder(Of AssemblySymbol).GetInstance(_lazyUsedAssemblyReferences.Count)
                        stack.AddRange(_lazyUsedAssemblyReferences)

                        While stack.Count <> 0
                            Dim current As AssemblySymbol = stack.Pop()
                            Dim usedAssemblies As ConcurrentSet(Of AssemblySymbol)

                            Dim sourceAssembly = TryCast(current, SourceAssemblySymbol)

                            If sourceAssembly IsNot Nothing Then
                                ' The set of assemblies used by the referenced compilation feels Like
                                ' a reasonable approximation to the set of assembly references that would
                                ' be emitted into the resulting binary for that compilation. An alternative
                                ' would be to attempt to emit and get the exact set of emitted references
                                ' in case of success. This might be too slow though.
                                usedAssemblies = sourceAssembly.DeclaringCompilation.GetCompleteSetOfUsedAssemblies(cancellationToken)
                                If usedAssemblies IsNot Nothing Then
                                    For Each dependency As AssemblySymbol In usedAssemblies
                                        Debug.Assert(Not dependency.IsLinked)
                                        AddUsedAssembly(dependency, stack)
                                    Next
                                End If

                                Continue While
                            End If

                            Dim retargetingAssembly = TryCast(current, RetargetingAssemblySymbol)

                            If retargetingAssembly IsNot Nothing Then
                                usedAssemblies = retargetingAssembly.UnderlyingAssembly.DeclaringCompilation.GetCompleteSetOfUsedAssemblies(cancellationToken)
                                If usedAssemblies IsNot Nothing Then
                                    For Each underlyingDependency As AssemblySymbol In retargetingAssembly.UnderlyingAssembly.SourceModule.ReferencedAssemblySymbols
                                        If Not underlyingDependency.IsLinked AndAlso usedAssemblies.Contains(underlyingDependency) Then
                                            Dim dependency As AssemblySymbol = Nothing

                                            If Not DirectCast(retargetingAssembly.Modules(0), RetargetingModuleSymbol).RetargetingDefinitions(underlyingDependency, dependency) Then
                                                Debug.Assert(retargetingAssembly.Modules(0).ReferencedAssemblySymbols.Contains(underlyingDependency))
                                                dependency = underlyingDependency
                                            End If

                                            AddUsedAssembly(dependency, stack)
                                        End If
                                    Next
                                End If

                                AddReferencedAssemblies(retargetingAssembly, includeMainModule:=False, stack)

                                Continue While
                            End If

                            AddReferencedAssemblies(current, includeMainModule:=True, stack)
                        End While

                        stack.Free()
                    End SyncLock
                End If

                If SourceAssembly.CorLibrary IsNot Nothing Then
                    ' Add core library
                    AddUsedAssembly(sourceAssembly.CorLibrary)
                End If
            End If

            _usedAssemblyReferencesFrozen = True
        End Sub

        Friend Sub AddUsedAssemblies(assemblies As ICollection(Of AssemblySymbol))
            If Not assemblies.IsNullOrEmpty() Then
                For Each candidate In assemblies
                    AddUsedAssembly(candidate)
                Next
            End If
        End Sub

        Friend Function AddUsedAssembly(assembly As AssemblySymbol) As Boolean

            If assembly Is Nothing OrElse assembly Is SourceAssembly OrElse assembly.IsMissing Then
                Return False
            End If

            If _lazyUsedAssemblyReferences Is Nothing Then
                Interlocked.CompareExchange(_lazyUsedAssemblyReferences, New ConcurrentSet(Of AssemblySymbol)(), Nothing)
            End If

#If DEBUG Then
            Dim wasFrozen As Boolean = _usedAssemblyReferencesFrozen
#End If
            Dim added As Boolean = _lazyUsedAssemblyReferences.Add(assembly)

#If DEBUG Then
            Debug.Assert(Not added OrElse Not wasFrozen)
#End If
            Return added
        End Function

    End Class
End Namespace
