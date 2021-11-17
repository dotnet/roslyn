' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class VisualBasicDeterministicKeyBuilder
        Inherits DeterministicKeyBuilder

        Public Sub New()

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
            If basicOptions.PreprocessorSymbols.Length > 0 Then
                writer.WriteObjectStart()
                For Each pair In basicOptions.PreprocessorSymbols.OrderBy(Function(x, y) StringComparer.Ordinal.Compare(x.Key, y.Key))
                    Dim value = pair.Value
                    If value Is Nothing Then
                        writer.WriteNull(pair.Key)
                        Continue For
                    End If

                    Dim type = value.GetType()
                    If type = GetType(String) Then
                        writer.Write(pair.Key, CType(value, String))
                    ElseIf type = GetType(Boolean) Then
                        writer.Write(pair.Key, CType(value, Boolean))
                    ElseIf type = GetType(DateTime) Then
                        writer.Write(pair.Key, CType(value, DateTime).ToString("G"))
                    ElseIf type = GetType(Char) Then
                        writer.Write(pair.Key, CType(value, Char).ToString())
                    ElseIf type = GetType(Int16) Then
                        writer.Write(pair.Key, CType(value, Int16).ToString("G"))
                    ElseIf type = GetType(Int32) Then
                        writer.Write(pair.Key, CType(value, Int32).ToString("G"))
                    ElseIf type = GetType(Int64) Then
                        writer.Write(pair.Key, CType(value, Int64).ToString("G"))
                    ElseIf type = GetType(UInt16) Then
                        writer.Write(pair.Key, CType(value, UInt16).ToString("G"))
                    ElseIf type = GetType(UInt32) Then
                        writer.Write(pair.Key, CType(value, UInt32).ToString("G"))
                    ElseIf type = GetType(UInt64) Then
                        writer.Write(pair.Key, CType(value, UInt64).ToString("G"))
                    ElseIf type = GetType(Decimal) Then
                        writer.Write(pair.Key, CType(value, Decimal).ToString("G"))
                    ElseIf type = GetType(Single) Then
                        writer.Write(pair.Key, CType(value, Single).ToString("G"))
                    ElseIf type = GetType(Double) Then
                        writer.Write(pair.Key, CType(value, Double).ToString("G"))
                    ElseIf type = GetType(SByte) Then
                        writer.Write(pair.Key, CType(value, SByte).ToString("G"))
                    Else
                        Throw New InvalidOperationException()
                    End If
                Next
                writer.WriteObjectEnd()
            Else
                writer.WriteNull()
            End If

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
                writer.Write("isXml", import.IsXmlClause)
                writer.WriteObjectEnd()
            Next
            writer.WriteArrayEnd()

            writer.WriteKey("parseOptions")
            WriteParseOptions(writer, basicOptions.ParseOptions)
        End Sub

    End Class

End Namespace
