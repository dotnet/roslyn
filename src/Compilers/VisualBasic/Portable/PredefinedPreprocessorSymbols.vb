' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Globalization

Namespace Microsoft.CodeAnalysis.VisualBasic
    Public Module PredefinedPreprocessorSymbols

        Friend ReadOnly Property CurrentVersionNumber As Double
            Get
                Return Double.Parse(LanguageVersion.Latest.MapSpecifiedToEffectiveVersion().GetErrorName(), CultureInfo.InvariantCulture)
            End Get
        End Property

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
