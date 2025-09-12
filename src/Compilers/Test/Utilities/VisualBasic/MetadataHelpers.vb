' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Runtime.CompilerServices

Friend Module MetadataTestHelpers

    <Extension>
    Friend Function GetCorLibType(this As ModuleSymbol, typeId As SpecialType) As NamedTypeSymbol
        Return this.ContainingAssembly.GetSpecialType(typeId)
    End Function

    <Extension>
    Friend Function CorLibrary(this As ModuleSymbol) As AssemblySymbol
        Return this.ContainingAssembly.CorLibrary
    End Function

    Friend Function LoadFromBytes(bytes() As Byte) As AssemblySymbol
        Dim retval = GetSymbolsForReferences({bytes})(0)
        Return retval
    End Function

    Friend Function GetSymbolsForReferences(references As Object(), Optional importInternals As Boolean = False) As AssemblySymbol()
        Dim refs As New List(Of MetadataReference)

        For Each r In references
            Dim bytes = TryCast(r, Byte())
            If bytes IsNot Nothing Then
                refs.Add(MetadataReference.CreateFromImage(bytes))
                Continue For
            End If

            If TypeOf r Is ImmutableArray(Of Byte) Then
                refs.Add(MetadataReference.CreateFromImage(CType(r, ImmutableArray(Of Byte))))
                Continue For
            End If

            Dim c = TryCast(r, VisualBasicCompilation)
            If c IsNot Nothing Then
                refs.Add(New VisualBasicCompilationReference(c))
                Continue For
            End If

            Dim m = TryCast(r, MetadataReference)
            If m IsNot Nothing Then
                refs.Add(m)
                Continue For
            End If

            Throw New InvalidOperationException()
        Next

        Dim options = If(importInternals, TestOptions.ReleaseDll.WithMetadataImportOptions(MetadataImportOptions.Internal), TestOptions.ReleaseDll)
        Dim tc1 = VisualBasicCompilation.Create("Dummy", references:=refs, options:=options)

        Return (From ref In refs Select tc1.GetReferencedAssemblySymbol(ref)).ToArray()
    End Function

End Module
