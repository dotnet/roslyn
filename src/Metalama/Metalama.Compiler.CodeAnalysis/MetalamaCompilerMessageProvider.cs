using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using static Metalama.Compiler.MetalamaErrorCode;

#nullable enable

namespace Metalama.Compiler
{
    internal enum MetalamaErrorCode
    {
        // The diagnostics code ranges with LAMA prefix are reserved in Metalama\Metalama.Framework.Engine\Diagnostics\Ranges.md.
        ERR_TransformerFailed = 601,
        ERR_TransformerNotFound = 602,
        ERR_TransformerCycleFound = 603,
        ERR_TransformersNotOrdered = 604,
        WRN_NoTransformedOutputPathWhenDebuggingTransformed = 605,
        ERR_InvalidIntrinsicUse = 606,
        WRN_LicensingMessage = 607,
        ERR_InvalidLicenseOverall = 608,
        ERR_InvalidLicenseForProducingTransformedOutput = 609,
        ERR_LicensingMessage = 610,
    }

    internal sealed class MetalamaCompilerMessageProvider : CommonMessageProvider
    {
        public static MetalamaCompilerMessageProvider Instance { get; } = new MetalamaCompilerMessageProvider();

        public override string CodePrefix => "LAMA";

        public override Type ErrorCodeType => typeof(MetalamaErrorCode);

        #region Roslyn error codes

        public override int ERR_FailedToCreateTempFile => throw new NotImplementedException();

        public override int ERR_MultipleAnalyzerConfigsInSameDir => throw new NotImplementedException();

        public override int ERR_ExpectedSingleScript => throw new NotImplementedException();

        public override int ERR_OpenResponseFile => throw new NotImplementedException();

        public override int ERR_InvalidPathMap => throw new NotImplementedException();

        public override int FTL_InvalidInputFileName => throw new NotImplementedException();

        public override int ERR_FileNotFound => throw new NotImplementedException();

        public override int ERR_NoSourceFile => throw new NotImplementedException();

        public override int ERR_CantOpenFileWrite => throw new NotImplementedException();

        public override int ERR_OutputWriteFailed => throw new NotImplementedException();

        public override int WRN_NoConfigNotOnCommandLine => throw new NotImplementedException();

        public override int ERR_BinaryFile => throw new NotImplementedException();

        public override int WRN_UnableToLoadAnalyzer => throw new NotImplementedException();

        public override int INF_UnableToLoadSomeTypesInAnalyzer => throw new NotImplementedException();

        public override int WRN_AnalyzerCannotBeCreated => throw new NotImplementedException();

        public override int WRN_NoAnalyzerInAssembly => throw new NotImplementedException();

        public override int ERR_CantReadRulesetFile => throw new NotImplementedException();

        public override int ERR_CompileCancelled => throw new NotImplementedException();

        public override int ERR_BadSourceCodeKind => throw new NotImplementedException();

        public override int ERR_BadDocumentationMode => throw new NotImplementedException();

        public override int ERR_BadCompilationOptionValue => throw new NotImplementedException();

        public override int ERR_MutuallyExclusiveOptions => throw new NotImplementedException();

        public override int ERR_InvalidDebugInformationFormat => throw new NotImplementedException();

        public override int ERR_InvalidFileAlignment => throw new NotImplementedException();

        public override int ERR_InvalidSubsystemVersion => throw new NotImplementedException();

        public override int ERR_InvalidOutputName => throw new NotImplementedException();

        public override int ERR_InvalidInstrumentationKind => throw new NotImplementedException();

        public override int ERR_InvalidHashAlgorithmName => throw new NotImplementedException();

        public override int ERR_MetadataFileNotAssembly => throw new NotImplementedException();

        public override int ERR_MetadataFileNotModule => throw new NotImplementedException();

        public override int ERR_InvalidAssemblyMetadata => throw new NotImplementedException();

        public override int ERR_InvalidModuleMetadata => throw new NotImplementedException();

        public override int ERR_ErrorOpeningAssemblyFile => throw new NotImplementedException();

        public override int ERR_ErrorOpeningModuleFile => throw new NotImplementedException();

        public override int ERR_MetadataFileNotFound => throw new NotImplementedException();

        public override int ERR_MetadataReferencesNotSupported => throw new NotImplementedException();

        public override int ERR_LinkedNetmoduleMetadataMustProvideFullPEImage => throw new NotImplementedException();

        public override int ERR_PublicKeyFileFailure => throw new NotImplementedException();

        public override int ERR_PublicKeyContainerFailure => throw new NotImplementedException();

        public override int ERR_OptionMustBeAbsolutePath => throw new NotImplementedException();

        public override int ERR_CantReadResource => throw new NotImplementedException();

        public override int ERR_CantOpenWin32Resource => throw new NotImplementedException();

        public override int ERR_CantOpenWin32Manifest => throw new NotImplementedException();

        public override int ERR_CantOpenWin32Icon => throw new NotImplementedException();

        public override int ERR_BadWin32Resource => throw new NotImplementedException();

        public override int ERR_ErrorBuildingWin32Resource => throw new NotImplementedException();

        public override int ERR_ResourceNotUnique => throw new NotImplementedException();

        public override int ERR_ResourceFileNameNotUnique => throw new NotImplementedException();

        public override int ERR_ResourceInModule => throw new NotImplementedException();

        public override int ERR_PermissionSetAttributeFileReadError => throw new NotImplementedException();

        public override int ERR_EncodinglessSyntaxTree => throw new NotImplementedException();

        public override int WRN_PdbUsingNameTooLong => throw new NotImplementedException();

        public override int WRN_PdbLocalNameTooLong => throw new NotImplementedException();

        public override int ERR_PdbWritingFailed => throw new NotImplementedException();

        public override int ERR_MetadataNameTooLong => throw new NotImplementedException();

        public override int ERR_EncReferenceToAddedMember => throw new NotImplementedException();

        public override int ERR_TooManyUserStrings => throw new NotImplementedException();

        public override int ERR_PeWritingFailure => throw new NotImplementedException();

        public override int ERR_ModuleEmitFailure => throw new NotImplementedException();

        public override int ERR_EncUpdateFailedMissingAttribute => throw new NotImplementedException();

        public override int ERR_InvalidDebugInfo => throw new NotImplementedException();

        public override int WRN_GeneratorFailedDuringInitialization => throw new NotImplementedException();

        public override int WRN_GeneratorFailedDuringGeneration => throw new NotImplementedException();

        public override int ERR_BadAssemblyName => throw new NotImplementedException();

        public override int WRN_AnalyzerReferencesFramework => throw new NotImplementedException();

        #endregion

        public override Diagnostic CreateDiagnostic(DiagnosticInfo info) => Diagnostic.Create(info);

        public override Diagnostic CreateDiagnostic(int code, Location location, params object[] args)
        {
            var diagnosticInfo = new DiagnosticInfo(this, code, args);
            return new DiagnosticWithInfo(diagnosticInfo, location);
        }

        public override string GetCategory(int code) => Diagnostic.CompilerDiagnosticCategory;

        public override LocalizableString GetDescription(int code) => null!;

        public override ReportDiagnostic GetDiagnosticReport(DiagnosticInfo diagnosticInfo, CompilationOptions options)
        {
            throw new NotImplementedException();
        }

        public override string GetErrorDisplayString(ISymbol symbol) => symbol.ToString()!;

        public override string GetHelpLink(int code) => string.Empty;

        public override LocalizableString GetMessageFormat(int code) => LoadMessage(code, null);

        public override string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo? culture) =>
            string.Format(culture, "{0} {1}",
                severity == DiagnosticSeverity.Error || isWarningAsError ? "error" : "warning",
                id);

        public override DiagnosticSeverity GetSeverity(int code) => (MetalamaErrorCode)code switch
        {
            ERR_TransformerFailed or
            ERR_TransformerNotFound or
            ERR_TransformerCycleFound or
            ERR_TransformersNotOrdered or
            ERR_InvalidIntrinsicUse or
            ERR_InvalidLicenseOverall or
            ERR_InvalidLicenseForProducingTransformedOutput => DiagnosticSeverity.Error,
            WRN_NoTransformedOutputPathWhenDebuggingTransformed or
            WRN_LicensingMessage => DiagnosticSeverity.Warning,
            ERR_LicensingMessage => DiagnosticSeverity.Error,
             
            _ => throw new ArgumentOutOfRangeException(nameof(code))
        };

        public override LocalizableString GetTitle(int code) =>
            (MetalamaErrorCode)code switch
            {
                ERR_TransformerFailed => "Transformer failed.",
                ERR_TransformerNotFound => "Transformer was not found when resolving transformer order.",
                ERR_TransformerCycleFound => "Dependencies between transformers form a cycle.",
                ERR_TransformersNotOrdered => "Transformers are not strongly ordered. Their order of execution would not be deterministic.",
                WRN_NoTransformedOutputPathWhenDebuggingTransformed => "Output directory for transformed files is not set, even though debugging transformed code is enabled.",
                ERR_InvalidIntrinsicUse => "Argument is not valid for Metalama intrinsic method.",
                WRN_LicensingMessage => "Licensing warning.",
                ERR_LicensingMessage => "Licensing error.",
                ERR_InvalidLicenseOverall => "Cannot start Metalama: invalid license.",
                ERR_InvalidLicenseForProducingTransformedOutput => "Cannot generate the transformed code: this feature is not available in Metalama Essentials.",
                _ => throw new ArgumentOutOfRangeException(nameof(code))
            };

        public override int GetWarningLevel(int code) => GetSeverity(code) switch
        {
            DiagnosticSeverity.Error => 0,
            DiagnosticSeverity.Warning => 1,
            _ => throw new ArgumentOutOfRangeException()
        };

        public override string LoadMessage(int code, CultureInfo? language) =>
            (MetalamaErrorCode)code switch
            {
                ERR_TransformerFailed => "Transformer '{0}' failed: {1}",
                ERR_TransformerNotFound => "Transformer '{0}' was not found when resolving transformer order.",
                ERR_TransformerCycleFound => "Dependencies between transformers form a cycle. Members of this cycle are: {0}",
                ERR_TransformersNotOrdered => "Transformers '{0}' and '{1}' are not strongly ordered. Their order of execution would not be deterministic.",
                WRN_NoTransformedOutputPathWhenDebuggingTransformed => "Output directory for transformed files is not set, even though debugging transformed code is enabled. This will lead to warnings and errors that point to nonsensical file locations.",
                ERR_InvalidIntrinsicUse => "Argument '{0}' is not valid for Metalama intrinsic method '{1}'.",
                WRN_LicensingMessage => "{0}",
                ERR_LicensingMessage => "{0}",
                ERR_InvalidLicenseOverall => "Cannot start Metalama: invalid license.",
                ERR_InvalidLicenseForProducingTransformedOutput => "Cannot generate the transformed code: this feature is not available in Metalama Essentials.", 
                _ => throw new ArgumentOutOfRangeException(nameof(code))
            };

        #region Report Roslyn diagnostics

        protected override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2)
        {
            throw new NotImplementedException();
        }

        public override void ReportDuplicateMetadataReferenceStrong(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            throw new NotImplementedException();
        }

        public override void ReportDuplicateMetadataReferenceWeak(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            throw new NotImplementedException();
        }

        protected override void ReportInvalidAttributeArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        protected override void ReportInvalidNamedArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex, ITypeSymbol attributeClass, string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override void ReportMarshalUnmanagedTypeNotValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        protected override void ReportMarshalUnmanagedTypeOnlyValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        protected override void ReportParameterNotValidForType(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
