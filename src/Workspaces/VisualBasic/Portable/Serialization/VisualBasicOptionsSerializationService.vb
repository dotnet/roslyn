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

            writer.WriteValue(vbOptions.GlobalImports.Select(Function(g) g.Name).ToArray())
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
                writer.WriteValue(kv.Value)
            Next
        End Sub

        Public Overrides Function ReadCompilationOptionsFrom(reader As ObjectReader, cancellationToken As CancellationToken) As CompilationOptions
            Dim outputKind As OutputKind
            Dim reportSuppressedDiagnostics As Boolean
            Dim moduleName As String = Nothing
            Dim mainTypeName As String = Nothing
            Dim scriptClassName As String = Nothing
            Dim optimizationLevel As OptimizationLevel
            Dim checkOverflow As Boolean
            Dim cryptoKeyContainer As String = Nothing
            Dim cryptoKeyFile As String = Nothing
            Dim cryptoPublicKey As ImmutableArray(Of Byte) = ImmutableArray(Of Byte).Empty
            Dim delaySign As Boolean?
            Dim platform As Platform
            Dim generalDiagnosticOption As ReportDiagnostic
            Dim warningLevel As Integer
            Dim specificDiagnosticOptions As IEnumerable(Of KeyValuePair(Of String, ReportDiagnostic)) = Nothing
            Dim concurrentBuild As Boolean
            Dim deterministic As Boolean
            Dim publicSign As Boolean
            Dim metadataImportOptions As MetadataImportOptions = Nothing
            Dim xmlReferenceResolver As XmlReferenceResolver = Nothing
            Dim sourceReferenceResolver As SourceReferenceResolver = Nothing
            Dim metadataReferenceResolver As MetadataReferenceResolver = Nothing
            Dim assemblyIdentityComparer As AssemblyIdentityComparer = Nothing
            Dim strongNameProvider As StrongNameProvider = Nothing

            ReadCompilationOptionsFrom(reader, outputKind, reportSuppressedDiagnostics, moduleName, mainTypeName, scriptClassName,
                optimizationLevel, checkOverflow, cryptoKeyContainer, cryptoKeyFile, cryptoPublicKey, delaySign,
                platform, generalDiagnosticOption, warningLevel, specificDiagnosticOptions, concurrentBuild, deterministic,
                publicSign, metadataImportOptions, xmlReferenceResolver, sourceReferenceResolver, metadataReferenceResolver,
                assemblyIdentityComparer, strongNameProvider, cancellationToken)

            Dim globalImports = GlobalImport.Parse(reader.ReadArray(Of String)())
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
            Dim kind As SourceCodeKind
            Dim documentationMode As DocumentationMode
            Dim features As IEnumerable(Of KeyValuePair(Of String, String)) = Nothing
            ReadParseOptionsFrom(reader, kind, documentationMode, features, cancellationToken)

            Dim languageVersion = DirectCast(reader.ReadInt32(), LanguageVersion)

            Dim count = reader.ReadInt32()
            Dim builder = ImmutableArray.CreateBuilder(Of KeyValuePair(Of String, Object))(count)
            For i = 0 To count - 1
                Dim key = reader.ReadString()
                Dim value = reader.ReadValue()
                builder.Add(KeyValuePairUtil.Create(key, value))
            Next

            Dim options = New VisualBasicParseOptions(languageVersion, documentationMode, kind, builder.MoveToImmutable())
            Return options.WithFeatures(features)
        End Function
    End Class
End Namespace
