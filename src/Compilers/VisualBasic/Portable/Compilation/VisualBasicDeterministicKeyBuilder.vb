' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Namespace Microsoft.CodeAnalysis.VisualBasic

    Friend NotInheritable Class VisualBasicDeterministicKeyBuilder
        Inherits DeterministicKeyBuilder

        Protected Overrides Sub AppendParseOptionsCore(parseOptions As ParseOptions)
            Dim basicOptions = TryCast(parseOptions, VisualBasicParseOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            MyBase.AppendParseOptionsCore(parseOptions)

            WriteEnum("languageVersion", basicOptions.LanguageVersion)
            WriteEnum("specifiedLanguageVersion", basicOptions.SpecifiedLanguageVersion)

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

        Protected Overrides Sub AppendCompilationOptionsCore(options As CompilationOptions)
            Dim basicOptions = TryCast(options, VisualBasicCompilationOptions)
            If basicOptions Is Nothing Then
                Throw New InvalidOperationException()
            End If

            WriteString("rootNamespace", basicOptions.RootNamespace)
            WriteEnum("optionStrict", basicOptions.OptionStrict)
            WriteBool("optionInfer", basicOptions.OptionInfer)
            WriteBool("optionExplicit", basicOptions.OptionExplicit)
            WriteBool("optionCompareText", basicOptions.OptionCompareText)
            WriteBool("embedVbCoreRuntime", basicOptions.EmbedVbCoreRuntime)

            If basicOptions.GlobalImports.Length > 0 Then
                Writer.WriteKey("globalImports")
                Writer.WriteArrayStart()
                For Each import In basicOptions.GlobalImports
                    Writer.WriteObjectStart()
                    WriteString("name", import.Name)
                    WriteBool("isXml", import.IsXmlClause)
                    Writer.WriteObjectEnd()
                Next
            End If

            AppendParseOptions(basicOptions.ParseOptions)
        End Sub

    End Class

End Namespace
