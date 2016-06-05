﻿' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Public Module PredefinedPreprocessorSymbols

        Friend Const CurrentVersionNumber = 15.0

        ''' <summary>
        ''' Adds predefined preprocessor symbols VBC_VER and TARGET to given list of preprocessor symbols if not included yet.
        ''' </summary>
        ''' <param name="kind">The Output kind to derive the value of TARGET symbol from.</param>
        ''' <param name="symbols">A collection of KeyValue pairs representing existing symbols.</param>
        ''' <returns>Array of symbols that include VBC_VER and TARGET.</returns>
        Public Function AddPredefinedPreprocessorSymbols(kind As OutputKind, symbols As IEnumerable(Of KeyValuePair(Of String, Object))) As ImmutableArray(Of KeyValuePair(Of String, Object))
            Return AddPredefinedPreprocessorSymbols(kind, symbols.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Adds predefined preprocessor symbols VBC_VER and TARGET to given list of preprocessor symbols if not included yet.
        ''' </summary>
        ''' <param name="kind">The Output kind to derive the value of TARGET symbol from.</param>
        ''' <param name="symbols">An parameter array of KeyValue pairs representing existing symbols.</param>
        ''' <returns>Array of symbols that include VBC_VER and TARGET.</returns>
        Public Function AddPredefinedPreprocessorSymbols(kind As OutputKind, ParamArray symbols As KeyValuePair(Of String, Object)()) As ImmutableArray(Of KeyValuePair(Of String, Object))
            Return AddPredefinedPreprocessorSymbols(kind, symbols.AsImmutableOrNull())
        End Function

        ''' <summary>
        ''' Adds predefined preprocessor symbols VBC_VER and TARGET to given list of preprocessor symbols if not included yet.
        ''' </summary>
        ''' <param name="kind">The Output kind to derive the value of TARGET symbol from.</param>
        ''' <param name="symbols">An ImmutableArray of KeyValue pairs representing existing symbols.</param>
        ''' <returns>Array of symbols that include VBC_VER and TARGET.</returns>
        Public Function AddPredefinedPreprocessorSymbols(kind As OutputKind, symbols As ImmutableArray(Of KeyValuePair(Of String, Object))) As ImmutableArray(Of KeyValuePair(Of String, Object))
            If Not kind.IsValid Then
                Throw New ArgumentOutOfRangeException(NameOf(kind))
            End If

            Const CompilerVersionSymbol = "VBC_VER"
            Const TargetSymbol = "TARGET"

            If symbols.IsDefault Then
                symbols = ImmutableArray(Of KeyValuePair(Of String, Object)).Empty
            End If

            If symbols.FirstOrDefault(Function(entry) IdentifierComparison.Equals(entry.Key, CompilerVersionSymbol)).Key Is Nothing Then
                ' This number should always line up with the current version of the compilerString
                symbols = symbols.Add(New KeyValuePair(Of String, Object)(CompilerVersionSymbol, CurrentVersionNumber))
            End If

            If symbols.FirstOrDefault(Function(entry) IdentifierComparison.Equals(entry.Key, TargetSymbol)).Key Is Nothing Then
                symbols = symbols.Add(New KeyValuePair(Of String, Object)(TargetSymbol, GetTargetString(kind)))
            End If

            Return symbols
        End Function

        Friend Function GetTargetString(kind As OutputKind) As String
            Select Case kind
                Case OutputKind.ConsoleApplication
                    Return "exe"

                Case OutputKind.DynamicallyLinkedLibrary
                    Return "library"

                Case OutputKind.NetModule
                    Return "module"

                Case OutputKind.WindowsApplication
                    Return "winexe"

                Case OutputKind.WindowsRuntimeApplication
                    Return "appcontainerexe"

                Case OutputKind.WindowsRuntimeMetadata
                    Return "winmdobj"

                Case Else
                    Throw ExceptionUtilities.UnexpectedValue(kind)
            End Select
        End Function
    End Module
End Namespace
