' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class VisualBasicDeterministicKeyBuilder
        Inherits DeterministicKeyBuilder

        Public Sub New(options As DeterministicKeyOptions)
            MyBase.New(options)
        End Sub

        Protected Overrides Sub WriteParseOptionsCore(parseOptions As ParseOptions)
            Dim basicOptions = TryCast(parseOptions, VisualBasicParseOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            MyBase.WriteParseOptionsCore(parseOptions)

            Writer.Write("languageVersion", basicOptions.LanguageVersion)
            Writer.Write("specifiedLanguageVersion", basicOptions.SpecifiedLanguageVersion)

            If basicOptions.PreprocessorSymbols.Length > 0 Then
                Writer.WriteKey("preprocessorSymbols")
                Writer.WriteArrayStart()
                For Each pair In basicOptions.PreprocessorSymbols
                    Writer.WriteObjectStart()
                    Writer.WriteKey(pair.Key)
                    Writer.Write(pair.Value.ToString())
                Next
                Writer.WriteArrayEnd()
            End If

        End Sub

        Protected Overrides Sub WriteCompilationOptionsCore(options As CompilationOptions)
            Dim basicOptions = TryCast(options, VisualBasicCompilationOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            Writer.Write("rootNamespace", basicOptions.RootNamespace)
            Writer.Write("optionStrict", basicOptions.OptionStrict)
            Writer.Write("optionInfer", basicOptions.OptionInfer)
            Writer.Write("optionExplicit", basicOptions.OptionExplicit)
            Writer.Write("optionCompareText", basicOptions.OptionCompareText)
            Writer.Write("embedVbCoreRuntime", basicOptions.EmbedVbCoreRuntime)

            If basicOptions.GlobalImports.Length > 0 Then
                Writer.WriteKey("globalImports")
                Writer.WriteArrayStart()
                For Each import In basicOptions.GlobalImports
                    Writer.WriteObjectStart()
                    Writer.Write("name", import.Name)
                    Writer.Write("isXml", import.IsXmlClause)
                    Writer.WriteObjectEnd()
                Next
            End If

            WriteParseOptions(basicOptions.ParseOptions)
        End Sub

    End Class

End Namespace
