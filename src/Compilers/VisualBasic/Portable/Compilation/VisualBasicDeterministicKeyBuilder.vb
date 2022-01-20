' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Globalization

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class VisualBasicDeterministicKeyBuilder
        Inherits DeterministicKeyBuilder

        Friend Shared ReadOnly Instance As New VisualBasicDeterministicKeyBuilder()

        Private Sub New()

        End Sub

        Protected Overrides Sub WriteParseOptionsCore(writer As JsonWriter, parseOptions As ParseOptions)

            ' This can happen for SyntaxTree that are constructed via the API. 
            If parseOptions Is Nothing Then
                Return
            End If

            Dim basicOptions = TryCast(parseOptions, VisualBasicParseOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            MyBase.WriteParseOptionsCore(writer, parseOptions)

            writer.Write("languageVersion", basicOptions.LanguageVersion)
            writer.Write("specifiedLanguageVersion", basicOptions.SpecifiedLanguageVersion)

            writer.WriteKey("preprocessorSymbols")
            writer.WriteObjectStart()
            For Each pair In basicOptions.PreprocessorSymbols.OrderBy(Function(x, y) StringComparer.Ordinal.Compare(x.Key, y.Key))
                Dim value = pair.Value
                If value Is Nothing Then
                    writer.WriteNull(pair.Key)
                    Continue For
                End If

                writer.WriteKey(pair.Key)

                Dim type = value.GetType()
                If type = GetType(String) Then
                    writer.Write(CType(value, String))
                ElseIf type = GetType(Boolean) Then
                    writer.Write(CType(value, Boolean))
                Else
                    Dim formattable = TryCast(value, IFormattable)
                    If formattable IsNot Nothing Then
                        writer.Write(formattable.ToString(Nothing, CultureInfo.InvariantCulture))
                    Else
                        Throw ExceptionUtilities.UnexpectedValue(value)
                    End If
                End If
            Next
            writer.WriteObjectEnd()

        End Sub

        Protected Overrides Sub WriteCompilationOptionsCore(writer As JsonWriter, options As CompilationOptions)
            Dim basicOptions = TryCast(options, VisualBasicCompilationOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            MyBase.WriteCompilationOptionsCore(writer, options)

            writer.Write("rootNamespace", basicOptions.RootNamespace)
            writer.Write("optionStrict", basicOptions.OptionStrict)
            writer.Write("optionInfer", basicOptions.OptionInfer)
            writer.Write("optionExplicit", basicOptions.OptionExplicit)
            writer.Write("optionCompareText", basicOptions.OptionCompareText)
            writer.Write("embedVbCoreRuntime", basicOptions.EmbedVbCoreRuntime)

            writer.WriteKey("globalImports")
            writer.WriteArrayStart()
            For Each import In basicOptions.GlobalImports
                writer.WriteObjectStart()
                writer.Write("name", import.Name)
                writer.Write("isXmlClause", import.IsXmlClause)
                writer.WriteObjectEnd()
            Next
            writer.WriteArrayEnd()

            writer.WriteKey("parseOptions")
            If basicOptions.ParseOptions IsNot Nothing Then
                WriteParseOptions(writer, basicOptions.ParseOptions)
            Else
                writer.WriteNull()
            End If
        End Sub

    End Class

End Namespace
