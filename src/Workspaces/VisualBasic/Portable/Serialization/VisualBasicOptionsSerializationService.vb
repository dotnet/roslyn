' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports Microsoft.CodeAnalysis.Serialization
Imports Microsoft.CodeAnalysis.Host.Mef

Namespace Microsoft.CodeAnalysis.VisualBasic.Serialization
    <ExportLanguageService(GetType(IOptionsSerializationService), LanguageNames.VisualBasic), [Shared]>
    Friend Class VisualBasicOptionsSerializationService
        Inherits AbstractOptionsSerializationService

        <ImportingConstructor>
        <Obsolete(MefConstruction.ImportingConstructorMessage, True)>
        Public Sub New()
        End Sub

        Public Overrides Sub WriteTo(options As CompilationOptions, writer As ObjectWriter, cancellationToken As CancellationToken)
            WriteCompilationOptionsTo(options, writer, cancellationToken)

            Dim vbOptions = DirectCast(options, VisualBasicCompilationOptions)

            writer.WriteArray(vbOptions.GlobalImports.SelectAsArray(Function(g) g.Name), Sub(w, n) w.WriteString(n))
            writer.WriteString(vbOptions.RootNamespace)
            writer.WriteInt32(vbOptions.OptionStrict)
            writer.WriteBoolean(vbOptions.OptionInfer)
            writer.WriteBoolean(vbOptions.OptionExplicit)
            writer.WriteBoolean(vbOptions.OptionCompareText)
            writer.WriteBoolean(vbOptions.EmbedVbCoreRuntime)

            ' save parse option for embedded types - My types
            writer.WriteBoolean(vbOptions.ParseOptions IsNot Nothing)
            If vbOptions.ParseOptions IsNot Nothing Then
                cancellationToken.ThrowIfCancellationRequested()
                WriteTo(vbOptions.ParseOptions, writer)
            End If
        End Sub

        Public Overrides Sub WriteTo(options As ParseOptions, writer As ObjectWriter)
            WriteParseOptionsTo(options, writer)

            Dim vbOptions = DirectCast(options, VisualBasicParseOptions)
            writer.WriteInt32(vbOptions.SpecifiedLanguageVersion)

            writer.WriteInt32(vbOptions.PreprocessorSymbols.Length)
            For Each kv In vbOptions.PreprocessorSymbols
                writer.WriteString(kv.Key)

                ' all value here should be primitive types
                writer.WriteScalarValue(kv.Value)
            Next
        End Sub

        Public Overrides Function ReadCompilationOptionsFrom(reader As ObjectReader, cancellationToken As CancellationToken) As CompilationOptions
            Dim tuple = ReadCompilationOptionsPieces(reader, cancellationToken)
            Dim outputKind = tuple.outputKind
            Dim reportSuppressedDiagnostics = tuple.reportSuppressedDiagnostics
            Dim moduleName = tuple.moduleName
            Dim mainTypeName = tuple.mainTypeName
            Dim scriptClassName = tuple.scriptClassName
            Dim optimizationLevel = tuple.optimizationLevel
            Dim checkOverflow = tuple.checkOverflow
            Dim cryptoKeyContainer = tuple.cryptoKeyContainer
            Dim cryptoKeyFile = tuple.cryptoKeyFile
            Dim cryptoPublicKey = tuple.cryptoPublicKey
            Dim delaySign = tuple.delaySign
            Dim platform = tuple.platform
            Dim generalDiagnosticOption = tuple.generalDiagnosticOption
            Dim warningLevel = tuple.warningLevel
            Dim specificDiagnosticOptions = tuple.specificDiagnosticOptions
            Dim concurrentBuild = tuple.concurrentBuild
            Dim deterministic = tuple.deterministic
            Dim publicSign = tuple.publicSign
            Dim metadataImportOptions = tuple.metadataImportOptions
            Dim xmlReferenceResolver = tuple.xmlReferenceResolver
            Dim sourceReferenceResolver = tuple.sourceReferenceResolver
            Dim metadataReferenceResolver = tuple.metadataReferenceResolver
            Dim assemblyIdentityComparer = tuple.assemblyIdentityComparer
            Dim strongNameProvider = tuple.strongNameProvider

            Dim globalImports = GlobalImport.Parse(reader.ReadArray(Function(r) r.ReadString()))
            Dim rootNamespace = reader.ReadString()
            Dim optionStrict = CType(reader.ReadInt32(), OptionStrict)
            Dim optionInfer = reader.ReadBoolean()
            Dim optionExplicit = reader.ReadBoolean()
            Dim optionCompareText = reader.ReadBoolean()
            Dim embedVbCoreRuntime = reader.ReadBoolean()

            Dim hasParseOptions = reader.ReadBoolean()
            Dim parseOption = If(hasParseOptions, DirectCast(ReadParseOptionsFrom(reader, cancellationToken), VisualBasicParseOptions), Nothing)

            Return New VisualBasicCompilationOptions(outputKind, moduleName, mainTypeName, scriptClassName,
                                                     globalImports, rootNamespace, optionStrict, optionInfer, optionExplicit,
                                                     optionCompareText, parseOption,
                                                     embedVbCoreRuntime, optimizationLevel, checkOverflow,
                                                     cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign,
                                                     platform, generalDiagnosticOption, specificDiagnosticOptions, concurrentBuild, deterministic,
                                                     xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver, assemblyIdentityComparer, strongNameProvider,
                                                     publicSign, reportSuppressedDiagnostics, metadataImportOptions)
        End Function

        Public Overrides Function ReadParseOptionsFrom(reader As ObjectReader, cancellationToken As CancellationToken) As ParseOptions
            Dim tuple = ReadParseOptionsPieces(reader, cancellationToken)
            Dim kind = tuple.kind
            Dim documentationMode = tuple.documentationMode
            Dim features = tuple.features

            Dim languageVersion = DirectCast(reader.ReadInt32(), LanguageVersion)

            Dim count = reader.ReadInt32()
            Dim builder = ImmutableArray.CreateBuilder(Of KeyValuePair(Of String, Object))(count)
            For i = 0 To count - 1
                Dim key = reader.ReadString()
                Dim value = reader.ReadScalarValue()
                builder.Add(KeyValuePairUtil.Create(key, value))
            Next

            Dim options = New VisualBasicParseOptions(languageVersion, documentationMode, kind, builder.MoveToImmutable())
            Return options.WithFeatures(features)
        End Function
    End Class
End Namespace
