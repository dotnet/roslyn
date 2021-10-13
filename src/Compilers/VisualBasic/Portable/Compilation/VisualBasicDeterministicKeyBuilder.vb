' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class VisualBasicDeterministicKeyBuilder
        Inherits DeterministicKeyBuilder

        Public Sub New()

        End Sub

        Protected Overrides Sub WriteParseOptionsCore(writer As JsonWriter, parseOptions As ParseOptions)
            Dim basicOptions = TryCast(parseOptions, VisualBasicParseOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            MyBase.WriteParseOptionsCore(writer, parseOptions)

            writer.Write("languageVersion", basicOptions.LanguageVersion)
            writer.Write("specifiedLanguageVersion", basicOptions.SpecifiedLanguageVersion)

            If basicOptions.PreprocessorSymbols.Length > 0 Then
                writer.WriteKey("preprocessorSymbols")
                writer.WriteArrayStart()
                For Each pair In basicOptions.PreprocessorSymbols
                    writer.WriteObjectStart()
                    writer.WriteKey(pair.Key)
                    writer.Write(pair.Value.ToString())
                Next
                writer.WriteArrayEnd()
            End If

        End Sub

        Protected Overrides Sub WriteCompilationOptionsCore(writer As JsonWriter, options As CompilationOptions)
            Dim basicOptions = TryCast(options, VisualBasicCompilationOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            writer.Write("rootNamespace", basicOptions.RootNamespace)
            writer.Write("optionStrict", basicOptions.OptionStrict)
            writer.Write("optionInfer", basicOptions.OptionInfer)
            writer.Write("optionExplicit", basicOptions.OptionExplicit)
            writer.Write("optionCompareText", basicOptions.OptionCompareText)
            writer.Write("embedVbCoreRuntime", basicOptions.EmbedVbCoreRuntime)

            If basicOptions.GlobalImports.Length > 0 Then
                writer.WriteKey("globalImports")
                writer.WriteArrayStart()
                For Each import In basicOptions.GlobalImports
                    writer.WriteObjectStart()
                    writer.Write("name", import.Name)
                    writer.Write("isXml", import.IsXmlClause)
                    writer.WriteObjectEnd()
                Next
            End If

            writer.WriteKey("parseOptions")
            WriteParseOptions(writer, basicOptions.ParseOptions)
        End Sub

    End Class

End Namespace
