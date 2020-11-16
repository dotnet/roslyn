using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using static RoslynEx.ErrorCode;

namespace RoslynEx
{
    internal enum ErrorCode
    {
        ERR_TransformerFailed = 1,
        ERR_TransformerNotFound = 2,
        ERR_TransformerCycleFound = 3,
        ERR_TransformersNotOrdered = 4,
        WRN_NoTransformedOutputPathWhenDebuggingTransformed = 5,
        ERR_InvalidIntrinsicUse = 6,
        ERR_TimeBombExploded = 7,
        WRN_TimeBombAboutToExplode = 8
    }

    internal sealed class RoslynExMessageProvider : CommonMessageProvider
    {
        public static RoslynExMessageProvider Instance { get; } = new RoslynExMessageProvider();

        public override string CodePrefix => "RE";

        public override Type ErrorCodeType => typeof(ErrorCode);

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

        public override DiagnosticSeverity GetSeverity(int code) => (ErrorCode)code switch
        {
            ERR_TransformerFailed or
            ERR_TransformerNotFound or
            ERR_TransformerCycleFound or
            ERR_TransformersNotOrdered or
            ERR_InvalidIntrinsicUse or
            ERR_TimeBombExploded => DiagnosticSeverity.Error,
            WRN_NoTransformedOutputPathWhenDebuggingTransformed or
            WRN_TimeBombAboutToExplode => DiagnosticSeverity.Warning,
            _ => throw new ArgumentOutOfRangeException(nameof(code))
        };

        public override LocalizableString GetTitle(int code) =>
            (ErrorCode)code switch
            {
                ERR_TransformerFailed => "Transformer failed.",
                ERR_TransformerNotFound => "Transformer was not found when resolving transformer order.",
                ERR_TransformerCycleFound => "Dependencies between transformers form a cycle.",
                ERR_TransformersNotOrdered => "Transformers are not strongly ordered. Their order of execution would not be deterministic.",
                WRN_NoTransformedOutputPathWhenDebuggingTransformed => "Output directory for transformed files is not set, even though debugging transformed code is enabled.",
                ERR_InvalidIntrinsicUse => "Argument is not valid for RoslynEx intrinsic method.",
                ERR_TimeBombExploded => "The current preview build of Caravela is out of date.",
                WRN_TimeBombAboutToExplode => "The current preview build of Caravela is going to be out of date soon.",
                _ => throw new ArgumentOutOfRangeException(nameof(code))
            };

        public override int GetWarningLevel(int code) => GetSeverity(code) switch
        {
            DiagnosticSeverity.Error => 0,
            DiagnosticSeverity.Warning => 1,
            _ => throw new ArgumentOutOfRangeException()
        };

        public override string LoadMessage(int code, CultureInfo? language) =>
            (ErrorCode)code switch
            {
                ERR_TransformerFailed => "Transformer '{0}' failed: {1}",
                ERR_TransformerNotFound => "Transformer '{0}' was not found when resolving transformer order.",
                ERR_TransformerCycleFound => "Dependencies between transformers form a cycle. Members of this cycle are: {0}",
                ERR_TransformersNotOrdered => "Transformers '{0}' and '{1}' are not strongly ordered. Their order of execution would not be deterministic.",
                WRN_NoTransformedOutputPathWhenDebuggingTransformed => "Output directory for transformed files is not set, even though debugging transformed code is enabled. This will lead to warnings and errors that point to nonsensical file locations.",
                ERR_InvalidIntrinsicUse => "Argument '{0}' is not valid for RoslynEx intrinsic method '{1}'.",
                ERR_TimeBombExploded => "The current preview build of Caravela is {0} days old, but is allowed to be used only for {1} days. Please update Caravela.",
                WRN_TimeBombAboutToExplode => "The current preview build of Caravela is {0} days old and will stop working soon, because it is allowed to be used only for {1} days. Please update Caravela soon.",
                _ => throw new ArgumentOutOfRangeException(nameof(code))
            };

        #region Report Roslyn diagnostics

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName)
        {
            throw new NotImplementedException();
        }

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2)
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

        public override void ReportInvalidAttributeArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        public override void ReportInvalidNamedArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex, ITypeSymbol attributeClass, string parameterName)
        {
            throw new NotImplementedException();
        }

        public override void ReportMarshalUnmanagedTypeNotValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        public override void ReportMarshalUnmanagedTypeOnlyValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            throw new NotImplementedException();
        }

        public override void ReportParameterNotValidForType(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
