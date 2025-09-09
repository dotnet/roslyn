// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed class MessageProvider : CommonMessageProvider
    {
        public static readonly MessageProvider Instance = new MessageProvider();

        private MessageProvider()
        {
        }

        public override DiagnosticSeverity GetSeverity(int code)
        {
            return ErrorFacts.GetSeverity((ErrorCode)code);
        }

        public override string LoadMessage(int code, CultureInfo language)
        {
            return ErrorFacts.GetMessage((ErrorCode)code, language);
        }

        public override LocalizableString GetMessageFormat(int code)
        {
            return ErrorFacts.GetMessageFormat((ErrorCode)code);
        }

        public override LocalizableString GetDescription(int code)
        {
            return ErrorFacts.GetDescription((ErrorCode)code);
        }

        public override LocalizableString GetTitle(int code)
        {
            return ErrorFacts.GetTitle((ErrorCode)code);
        }

        public override string GetHelpLink(int code)
        {
            return ErrorFacts.GetHelpLink((ErrorCode)code);
        }

        public override string GetCategory(int code)
        {
            return ErrorFacts.GetCategory((ErrorCode)code);
        }

        public override string CodePrefix
        {
            get
            {
                return "CS";
            }
        }

        // Given a message identifier (e.g., CS0219), severity, warning as error and a culture, 
        // get the entire prefix (e.g., "error CS0219:" for C#) used on error messages.
        public override string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo culture)
        {
            return String.Format(culture, "{0} {1}",
                severity == DiagnosticSeverity.Error || isWarningAsError ? "error" : "warning",
                id);
        }

        public override int GetWarningLevel(int code)
        {
            return ErrorFacts.GetWarningLevel((ErrorCode)code);
        }

        public override Type ErrorCodeType
        {
            get { return typeof(ErrorCode); }
        }

        public override Diagnostic CreateDiagnostic(int code, Location location, params object[] args)
        {
            var info = new CSDiagnosticInfo((ErrorCode)code, args, ImmutableArray<Symbol>.Empty, ImmutableArray<Location>.Empty);
            return new CSDiagnostic(info, location);
        }

        public override Diagnostic CreateDiagnostic(DiagnosticInfo info)
        {
            return new CSDiagnostic(info, Location.None);
        }

        public override string GetErrorDisplayString(ISymbol symbol)
        {
            // show extra info for assembly if possible such as version, public key token etc.
            if (symbol.Kind == SymbolKind.Assembly || symbol.Kind == SymbolKind.Namespace)
            {
                return symbol.ToString();
            }

            return SymbolDisplay.ToDisplayString(symbol, SymbolDisplayFormat.CSharpShortErrorMessageFormat);
        }

        public override bool GetIsEnabledByDefault(int code) =>
            (ErrorCode)code is not (
                ErrorCode.WRN_UseDefViolationPropertySupportedVersion
                or ErrorCode.WRN_UseDefViolationFieldSupportedVersion
                or ErrorCode.WRN_UseDefViolationThisSupportedVersion
                or ErrorCode.WRN_UnassignedThisAutoPropertySupportedVersion
                or ErrorCode.WRN_UnassignedThisSupportedVersion
                or ErrorCode.WRN_CollectionExpressionRefStructMayAllocate
                or ErrorCode.WRN_CollectionExpressionRefStructSpreadMayAllocate
            );

        public override ReportDiagnostic GetDiagnosticReport(DiagnosticInfo diagnosticInfo, CompilationOptions options)
        {
            bool hasPragmaSuppression;
            return CSharpDiagnosticFilter.GetDiagnosticReport(diagnosticInfo.Severity,
                                                              true,
                                                              diagnosticInfo.Code,
                                                              diagnosticInfo.MessageIdentifier,
                                                              diagnosticInfo.WarningLevel,
                                                              Location.None,
                                                              diagnosticInfo.CustomTags,
                                                              options.WarningLevel,
                                                              ((CSharpCompilationOptions)options).NullableContextOptions,
                                                              options.GeneralDiagnosticOption,
                                                              options.SpecificDiagnosticOptions,
                                                              options.SyntaxTreeOptionsProvider,
                                                              CancellationToken.None, // We don't have a tree so there's no need to pass cancellation to the SyntaxTreeOptionsProvider
                                                              out hasPragmaSuppression);
        }

#if DEBUG
        internal override bool ShouldAssertExpectedMessageArgumentsLength(int errorCode)
        {
            return (ErrorCode)errorCode switch
            {
                0 => false,
                ErrorCode.Unknown => false,
                ErrorCode.Void => false,
                ErrorCode.ERR_IdentifierExpectedKW => false, // message uses {1} rather than {0}
                ErrorCode.WRN_XMLParseError => false, // XmlSyntaxDiagnosticInfo.GetMessage() uses distinct error code 
                _ => true
            };
        }
#endif

        public override int ERR_FailedToCreateTempFile => (int)ErrorCode.ERR_CantMakeTempFile;
        public override int ERR_MultipleAnalyzerConfigsInSameDir => (int)ErrorCode.ERR_MultipleAnalyzerConfigsInSameDir;

        // command line:
        public override int ERR_ExpectedSingleScript => (int)ErrorCode.ERR_ExpectedSingleScript;
        public override int ERR_OpenResponseFile => (int)ErrorCode.ERR_OpenResponseFile;
        public override int ERR_InvalidPathMap => (int)ErrorCode.ERR_InvalidPathMap;
        public override int FTL_InvalidInputFileName => (int)ErrorCode.FTL_InvalidInputFileName;
        public override int ERR_FileNotFound => (int)ErrorCode.ERR_FileNotFound;
        public override int ERR_NoSourceFile => (int)ErrorCode.ERR_NoSourceFile;
        public override int ERR_CantOpenFileWrite => (int)ErrorCode.ERR_CantOpenFileWrite;
        public override int ERR_OutputWriteFailed => (int)ErrorCode.ERR_OutputWriteFailed;
        public override int WRN_NoConfigNotOnCommandLine => (int)ErrorCode.WRN_NoConfigNotOnCommandLine;
        public override int ERR_BinaryFile => (int)ErrorCode.ERR_BinaryFile;
        public override int WRN_AnalyzerCannotBeCreated => (int)ErrorCode.WRN_AnalyzerCannotBeCreated;
        public override int WRN_NoAnalyzerInAssembly => (int)ErrorCode.WRN_NoAnalyzerInAssembly;
        public override int WRN_UnableToLoadAnalyzer => (int)ErrorCode.WRN_UnableToLoadAnalyzer;
        public override int WRN_AnalyzerReferencesFramework => (int)ErrorCode.WRN_AnalyzerReferencesFramework;
        public override int WRN_AnalyzerReferencesNewerCompiler => (int)ErrorCode.WRN_AnalyzerReferencesNewerCompiler;
        public override int WRN_DuplicateAnalyzerReference => (int)ErrorCode.WRN_DuplicateAnalyzerReference;
        public override int INF_UnableToLoadSomeTypesInAnalyzer => (int)ErrorCode.INF_UnableToLoadSomeTypesInAnalyzer;
        public override int ERR_CantReadRulesetFile => (int)ErrorCode.ERR_CantReadRulesetFile;
        public override int ERR_CompileCancelled => (int)ErrorCode.ERR_CompileCancelled;

        // parse options:
        public override int ERR_BadSourceCodeKind => (int)ErrorCode.ERR_BadSourceCodeKind;
        public override int ERR_BadDocumentationMode => (int)ErrorCode.ERR_BadDocumentationMode;

        // compilation options:
        public override int ERR_BadCompilationOptionValue => (int)ErrorCode.ERR_BadCompilationOptionValue;
        public override int ERR_MutuallyExclusiveOptions => (int)ErrorCode.ERR_MutuallyExclusiveOptions;

        // emit options:
        public override int ERR_InvalidDebugInformationFormat => (int)ErrorCode.ERR_InvalidDebugInformationFormat;
        public override int ERR_InvalidOutputName => (int)ErrorCode.ERR_InvalidOutputName;
        public override int ERR_InvalidFileAlignment => (int)ErrorCode.ERR_InvalidFileAlignment;
        public override int ERR_InvalidSubsystemVersion => (int)ErrorCode.ERR_InvalidSubsystemVersion;
        public override int ERR_InvalidInstrumentationKind => (int)ErrorCode.ERR_InvalidInstrumentationKind;
        public override int ERR_InvalidHashAlgorithmName => (int)ErrorCode.ERR_InvalidHashAlgorithmName;

        // reference manager:
        public override int ERR_MetadataFileNotAssembly => (int)ErrorCode.ERR_ImportNonAssembly;
        public override int ERR_MetadataFileNotModule => (int)ErrorCode.ERR_AddModuleAssembly;
        public override int ERR_InvalidAssemblyMetadata => (int)ErrorCode.FTL_MetadataCantOpenFile;
        public override int ERR_InvalidModuleMetadata => (int)ErrorCode.FTL_MetadataCantOpenFile;
        public override int ERR_ErrorOpeningAssemblyFile => (int)ErrorCode.FTL_MetadataCantOpenFile;
        public override int ERR_ErrorOpeningModuleFile => (int)ErrorCode.FTL_MetadataCantOpenFile;
        public override int ERR_MetadataFileNotFound => (int)ErrorCode.ERR_NoMetadataFile;
        public override int ERR_MetadataReferencesNotSupported => (int)ErrorCode.ERR_MetadataReferencesNotSupported;
        public override int ERR_LinkedNetmoduleMetadataMustProvideFullPEImage => (int)ErrorCode.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage;

        public override void ReportDuplicateMetadataReferenceStrong(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            diagnostics.Add(ErrorCode.ERR_DuplicateImport, location,
                reference.Display ?? identity.GetDisplayName(),
                equivalentReference.Display ?? equivalentIdentity.GetDisplayName());
        }

        public override void ReportDuplicateMetadataReferenceWeak(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            diagnostics.Add(ErrorCode.ERR_DuplicateImportSimple, location,
                identity.Name,
                reference.Display ?? identity.GetDisplayName());
        }

        // signing:
        public override int ERR_PublicKeyFileFailure => (int)ErrorCode.ERR_PublicKeyFileFailure;
        public override int ERR_PublicKeyContainerFailure => (int)ErrorCode.ERR_PublicKeyContainerFailure;
        public override int ERR_OptionMustBeAbsolutePath => (int)ErrorCode.ERR_OptionMustBeAbsolutePath;

        // resources:
        public override int ERR_CantReadResource => (int)ErrorCode.ERR_CantReadResource;
        public override int ERR_CantOpenWin32Resource => (int)ErrorCode.ERR_CantOpenWin32Res;
        public override int ERR_CantOpenWin32Manifest => (int)ErrorCode.ERR_CantOpenWin32Manifest;
        public override int ERR_CantOpenWin32Icon => (int)ErrorCode.ERR_CantOpenIcon;
        public override int ERR_ErrorBuildingWin32Resource => (int)ErrorCode.ERR_ErrorBuildingWin32Resources;
        public override int ERR_BadWin32Resource => (int)ErrorCode.ERR_BadWin32Res;
        public override int ERR_ResourceFileNameNotUnique => (int)ErrorCode.ERR_ResourceFileNameNotUnique;
        public override int ERR_ResourceNotUnique => (int)ErrorCode.ERR_ResourceNotUnique;
        public override int ERR_ResourceInModule => (int)ErrorCode.ERR_CantRefResource;

        // pseudo-custom attributes:
        public override int ERR_PermissionSetAttributeFileReadError => (int)ErrorCode.ERR_PermissionSetAttributeFileReadError;
        public override int ERR_MethodImplAttributeAsyncCannotBeUsed => (int)ErrorCode.ERR_MethodImplAttributeAsyncCannotBeUsed;

        // PDB Writer:
        public override int ERR_EncodinglessSyntaxTree => (int)ErrorCode.ERR_EncodinglessSyntaxTree;
        public override int WRN_PdbUsingNameTooLong => (int)ErrorCode.WRN_DebugFullNameTooLong;
        public override int WRN_PdbLocalNameTooLong => (int)ErrorCode.WRN_PdbLocalNameTooLong;
        public override int ERR_PdbWritingFailed => (int)ErrorCode.FTL_DebugEmitFailure;

        // PE Writer:
        public override int ERR_MetadataNameTooLong => (int)ErrorCode.ERR_MetadataNameTooLong;
        public override int ERR_EncReferenceToAddedMember => (int)ErrorCode.ERR_EncReferenceToAddedMember;
        public override int ERR_TooManyUserStrings => (int)ErrorCode.ERR_TooManyUserStrings;
        public override int ERR_TooManyUserStrings_RestartRequired => (int)ErrorCode.ERR_TooManyUserStrings_RestartRequired;
        public override int ERR_PeWritingFailure => (int)ErrorCode.ERR_PeWritingFailure;
        public override int ERR_ModuleEmitFailure => (int)ErrorCode.ERR_ModuleEmitFailure;
        public override int ERR_EncUpdateFailedMissingSymbol => (int)ErrorCode.ERR_EncUpdateFailedMissingSymbol;
        public override int ERR_InvalidDebugInfo => (int)ErrorCode.ERR_InvalidDebugInfo;
        public override int ERR_FunctionPointerTypesInAttributeNotSupported => (int)ErrorCode.ERR_FunctionPointerTypesInAttributeNotSupported;
        public override int ERR_DataSectionStringLiteralHashCollision => (int)ErrorCode.ERR_DataSectionStringLiteralHashCollision;

        // Generators:
        public override int WRN_GeneratorFailedDuringInitialization => (int)ErrorCode.WRN_GeneratorFailedDuringInitialization;
        public override int WRN_GeneratorFailedDuringGeneration => (int)ErrorCode.WRN_GeneratorFailedDuringGeneration;

        protected override void ReportInvalidAttributeArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, AttributeData attribute)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, ((CSharpAttributeData)attribute).GetAttributeArgumentLocation(parameterIndex), node.GetErrorDisplayName());
        }

        protected override void ReportInvalidNamedArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex, ITypeSymbol attributeClass, string parameterName)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_InvalidNamedArgument, node.ArgumentList.Arguments[namedArgumentIndex].Location, parameterName);
        }

        protected override void ReportParameterNotValidForType(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_ParameterNotValidForType, node.ArgumentList.Arguments[namedArgumentIndex].Location);
        }

        protected override void ReportMarshalUnmanagedTypeNotValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            diagnostics.Add(ErrorCode.ERR_MarshalUnmanagedTypeNotValidForFields, ((CSharpAttributeData)attribute).GetAttributeArgumentLocation(parameterIndex), unmanagedTypeName);
        }

        protected override void ReportMarshalUnmanagedTypeOnlyValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            diagnostics.Add(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, ((CSharpAttributeData)attribute).GetAttributeArgumentLocation(parameterIndex), unmanagedTypeName);
        }

        protected override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_AttributeParameterRequired1, node.Name.Location, parameterName);
        }

        protected override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_AttributeParameterRequired2, node.Name.Location, parameterName1, parameterName2);
        }

        public override int ERR_BadAssemblyName => (int)ErrorCode.ERR_BadAssemblyName;

        public override int? WRN_ByValArraySizeConstRequired => (int)ErrorCode.WRN_ByValArraySizeConstRequired;
    }
}
