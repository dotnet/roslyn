' Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Symbols
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Public Module PredefinedPreprocessorSymbols
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
                Throw New ArgumentOutOfRangeException("kind")
            End If

            Const CompilerVersionSymbol = "VBC_VER"
            Const TargetSymbol = "TARGET"

            If symbols.IsDefault Then
                symbols = ImmutableArray(Of KeyValuePair(Of String, Object)).Empty
            End If

            If symbols.FirstOrDefault(Function(entry) IdentifierComparison.Equals(entry.Key, CompilerVersionSymbol)).Key Is Nothing Then
                ' This number is hardcoded to Dev11 compiler. 
                ' It's a bad practice to use the symbol so we should just keep the value as is and not ever rev it again.
                ' Incorrect usages include conditionally using Framework APIs or language features based upon this number.
                symbols = symbols.Add(New KeyValuePair(Of String, Object)(CompilerVersionSymbol, 11.0))
            End If

            If symbols.FirstOrDefault(Function(entry) IdentifierComparison.Equals(entry.Key, TargetSymbol)).Key Is Nothing Then
                symbols = symbols.Add(New KeyValuePair(Of String, Object)(TargetSymbol, VisualBasicCommandLineParser.GetTargetString(kind)))
            End If

            Return symbols
        End Function
    End Module
End Namespace
