' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Collections.Immutable
Imports System.Runtime.Serialization
Imports Microsoft.CodeAnalysis

Namespace Microsoft.CodeAnalysis.VisualBasic
    <Serializable>
    Public NotInheritable Class VisualBasicSerializableCompilationOptions
        Inherits SerializableCompilationOptions

        Private _options As VisualBasicCompilationOptions

        Private Const s_globalImportsString = "GlobalImports"
        Private Const s_rootNamespaceString = "RootNamespace"
        Private Const s_optionStrictString = "OptionStrict"
        Private Const s_optionInferString = "OptionInfer"
        Private Const s_optionExplicitString = "OptionExplicit"
        Private Const s_optionCompareTextString = "OptionCompareText"
        Private Const s_embedVbCoreRuntimeString = "EmbedVbCoreRuntime"
        Private Const s_suppressEmbeddedDeclarationsString = "SuppressEmbeddedDeclarations"
        Private Const s_parseOptionsString = "ParseOptions"

        Public Sub New(options As VisualBasicCompilationOptions)
            If options Is Nothing Then
                Throw New ArgumentNullException("options")
            End If

            _options = options
        End Sub

        Friend Sub New(info As SerializationInfo, context As StreamingContext)
            Dim serializableOptions = DirectCast(info.GetValue(s_parseOptionsString, GetType(VisualBasicSerializableParseOptions)), VisualBasicSerializableParseOptions)

            _options = New VisualBasicCompilationOptions(
                outputKind:=DirectCast(info.GetInt32(OutputKindString), OutputKind),
                moduleName:=info.GetString(ModuleNameString),
                mainTypeName:=info.GetString(MainTypeNameString),
                scriptClassName:=info.GetString(ScriptClassNameString),
                cryptoKeyContainer:=info.GetString(CryptoKeyContainerString),
                cryptoKeyFile:=info.GetString(CryptoKeyFileString),
                cryptoPublicKey:=DirectCast(info.GetValue(CryptoPublicKeyString, GetType(Byte())), Byte()).AsImmutableOrNull(),
                delaySign:=DirectCast(info.GetValue(DelaySignString, GetType(Boolean?)), Boolean?),
                optimizationLevel:=DirectCast(info.GetInt32(OptimizeString), OptimizationLevel),
                checkOverflow:=info.GetBoolean(CheckOverflowString),
                platform:=DirectCast(info.GetInt32(PlatformString), Platform),
                generalDiagnosticOption:=DirectCast(info.GetInt32(GeneralDiagnosticOptionString), ReportDiagnostic),
                specificDiagnosticOptions:=DirectCast(info.GetValue(SpecificDiagnosticOptionsString, GetType(Dictionary(Of String, ReportDiagnostic))), Dictionary(Of String, ReportDiagnostic)).ToImmutableDictionary(),
                concurrentBuild:=info.GetBoolean(ConcurrentBuildString),
                extendedCustomDebugInformation:=info.GetBoolean(ExtendedCustomDebugInformationString),
                xmlReferenceResolver:=XmlFileResolver.Default,
                sourceReferenceResolver:=SourceFileResolver.Default,
                metadataReferenceResolver:=New AssemblyReferenceResolver(MetadataFileReferenceResolver.Default, MetadataFileReferenceProvider.Default),
                assemblyIdentityComparer:=DesktopAssemblyIdentityComparer.Default,
                strongNameProvider:=New DesktopStrongNameProvider(),
                metadataImportOptions:=DirectCast(info.GetByte(MetadataImportOptionsString), MetadataImportOptions),
                features:=DirectCast(info.GetValue(FeaturesString, GetType(String())), String()).AsImmutable(),
                globalImports:=DirectCast(info.GetValue(s_globalImportsString, GetType(String())), String()).Select(AddressOf GlobalImport.Parse),
                rootNamespace:=info.GetString(s_rootNamespaceString),
                optionStrict:=CType(info.GetInt32(s_optionStrictString), OptionStrict),
                optionInfer:=info.GetBoolean(s_optionInferString),
                optionExplicit:=info.GetBoolean(s_optionExplicitString),
                optionCompareText:=info.GetBoolean(s_optionCompareTextString),
                embedVbCoreRuntime:=info.GetBoolean(s_embedVbCoreRuntimeString),
                suppressEmbeddedDeclarations:=info.GetBoolean(s_suppressEmbeddedDeclarationsString),
                parseOptions:=If(serializableOptions IsNot Nothing, serializableOptions.Options, Nothing))
        End Sub

        Public Overrides Sub GetObjectData(info As SerializationInfo, context As StreamingContext)
            CommonGetObjectData(_options, info, context)

            info.AddValue(s_globalImportsString, _options.GlobalImports.Select(Function(g) g.Name).ToArray())
            info.AddValue(s_rootNamespaceString, _options.RootNamespace)
            info.AddValue(s_optionStrictString, _options.OptionStrict)
            info.AddValue(s_optionInferString, _options.OptionInfer)
            info.AddValue(s_optionExplicitString, _options.OptionExplicit)
            info.AddValue(s_optionCompareTextString, _options.OptionCompareText)
            info.AddValue(s_embedVbCoreRuntimeString, _options.EmbedVbCoreRuntime)
            info.AddValue(s_suppressEmbeddedDeclarationsString, _options.SuppressEmbeddedDeclarations)
            info.AddValue(s_parseOptionsString, If(_options.ParseOptions IsNot Nothing, New VisualBasicSerializableParseOptions(_options.ParseOptions), Nothing))
        End Sub

        Public Shadows ReadOnly Property Options As VisualBasicCompilationOptions
            Get
                Return _options
            End Get
        End Property

        Protected Overrides ReadOnly Property CommonOptions As CompilationOptions
            Get
                Return _options
            End Get
        End Property
    End Class
End Namespace
