' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Concurrent
Imports System.Collections.Immutable
Imports System.Threading

Namespace Microsoft.CodeAnalysis.VisualBasic.Symbols

    Friend MustInherit Class MetadataOrSourceOrRetargetingAssemblySymbol
        Inherits NonMissingAssemblySymbol

        ''' <summary>
        ''' Determine whether this assembly has been granted access to <paramref name="potentialGiverOfAccess"></paramref>.
        ''' Assumes that the public key has been determined. The result will be cached.
        ''' </summary>
        ''' <param name="potentialGiverOfAccess"></param>
        ''' <returns></returns>
        ''' <remarks></remarks>
        Protected Function MakeFinalIVTDetermination(potentialGiverOfAccess As AssemblySymbol) As IVTConclusion
            Dim result As IVTConclusion = IVTConclusion.NoRelationshipClaimed
            If AssembliesToWhichInternalAccessHasBeenDetermined.TryGetValue(potentialGiverOfAccess, result) Then
                Return result
            End If

            result = IVTConclusion.NoRelationshipClaimed

            ' returns an empty list if there was no IVT attribute at all for the given name
            ' A name w/o a key is represented by a list with an entry that is empty
            Dim publicKeys As IEnumerable(Of ImmutableArray(Of Byte)) = potentialGiverOfAccess.GetInternalsVisibleToPublicKeys(Me.Name)

            ' We have an easy out here. Suppose the assembly wanting access is
            ' being compiled as a module. You can only strong-name an assembly. So we are going to optimistically
            ' assume that it Is going to be compiled into an assembly with a matching strong name, if necessary
            If publicKeys.Any() AndAlso IsNetModule Then
                Return IVTConclusion.Match
            End If

            ' look for one that works, if none work, then return the failure for the last one examined.
            For Each key In publicKeys
                ' We pass the public key of this assembly explicitly so PerformIVTCheck does not need
                ' to get it from this.Identity, which would trigger an infinite recursion.
                result = potentialGiverOfAccess.Identity.PerformIVTCheck(Me.PublicKey, key)

                If result = IVTConclusion.Match Then
                    ' Note that C# includes  OrElse result = IVTConclusion.OneSignedOneNot
                    Exit For
                End If
            Next

            AssembliesToWhichInternalAccessHasBeenDetermined.TryAdd(potentialGiverOfAccess, result)
            Return result
        End Function

        'EDMAURER This is a cache mapping from assemblies which we have analyzed whether or not they grant
        'internals access to us to the conclusion reached.
        Private _assembliesToWhichInternalAccessHasBeenAnalyzed As ConcurrentDictionary(Of AssemblySymbol, IVTConclusion)

        Private ReadOnly Property AssembliesToWhichInternalAccessHasBeenDetermined As ConcurrentDictionary(Of AssemblySymbol, IVTConclusion)
            Get
                If _assembliesToWhichInternalAccessHasBeenAnalyzed Is Nothing Then
                    Interlocked.CompareExchange(_assembliesToWhichInternalAccessHasBeenAnalyzed, New ConcurrentDictionary(Of AssemblySymbol, IVTConclusion), Nothing)
                End If
                Return _assembliesToWhichInternalAccessHasBeenAnalyzed
            End Get
        End Property

    End Class
End Namespace
