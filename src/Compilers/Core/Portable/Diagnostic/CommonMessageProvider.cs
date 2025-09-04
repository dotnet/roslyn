// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Concurrent;
using System.Globalization;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Abstracts the ability to classify and load messages for error codes. Allows the error
    /// infrastructure to be reused between C# and VB.
    /// </summary>
    internal abstract class CommonMessageProvider
    {
        /// <summary>
        /// Caches the return values for <see cref="GetIdForErrorCode(int)"/>.
        /// </summary>
        private static readonly ConcurrentDictionary<(string prefix, int code), string> s_errorIdCache = new ConcurrentDictionary<(string prefix, int code), string>();

        /// <summary>
        /// Given an error code, get the severity (warning or error) of the code.
        /// </summary>
        public abstract DiagnosticSeverity GetSeverity(int code);

        /// <summary>
        /// Load the message for the given error code. If the message contains
        /// "fill-in" placeholders, those should be expressed in standard string.Format notation
        /// and be in the string.
        /// </summary>
        public abstract string LoadMessage(int code, CultureInfo? language);

        /// <summary>
        /// Get an optional localizable title for the given diagnostic code.
        /// </summary>
        public abstract LocalizableString GetTitle(int code);

        /// <summary>
        /// Get an optional localizable description for the given diagnostic code.
        /// </summary>
        public abstract LocalizableString GetDescription(int code);

        /// <summary>
        /// Get a localizable message format string for the given diagnostic code.
        /// </summary>
        public abstract LocalizableString GetMessageFormat(int code);

        /// <summary>
        /// Get an optional help link for the given diagnostic code.
        /// </summary>
        public abstract string GetHelpLink(int code);

        /// <summary>
        /// Get the diagnostic category for the given diagnostic code.
        /// Default category is <see cref="Diagnostic.CompilerDiagnosticCategory"/>.
        /// </summary>
        public abstract string GetCategory(int code);

        /// <summary>
        /// Get the text prefix (e.g., "CS" for C#) used on error messages.
        /// </summary>
        public abstract string CodePrefix { get; }

        /// <summary>
        /// Get the warning level for warnings (e.g., 1 or greater for C#). VB does not have warning
        /// levels and always uses 1. Errors should return 0.
        /// </summary>
        public abstract int GetWarningLevel(int code);

        /// <summary>
        /// Type that defines error codes. For testing purposes only.
        /// </summary>
        public abstract Type ErrorCodeType { get; }

        /// <summary>
        /// Create a simple language specific diagnostic for given error code.
        /// </summary>
        public Diagnostic CreateDiagnostic(int code, Location location)
        {
            return CreateDiagnostic(code, location, Array.Empty<object>());
        }

        /// <summary>
        /// Create a simple language specific diagnostic with no location for given info.
        /// </summary>
        public abstract Diagnostic CreateDiagnostic(DiagnosticInfo info);

        /// <summary>
        /// Create a simple language specific diagnostic for given error code.
        /// </summary>
        public abstract Diagnostic CreateDiagnostic(int code, Location location, params object[] args);

        /// <summary>
        /// Given a message identifier (e.g., CS0219), severity, warning as error and a culture, 
        /// get the entire prefix (e.g., "error CS0219: Warning as Error:" for C# or "error BC42024:" for VB) used on error messages.
        /// </summary>
        public abstract string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo? culture);

        /// <summary>
        /// Convert given symbol to string representation.
        /// </summary>
        public abstract string GetErrorDisplayString(ISymbol symbol);

        public abstract bool GetIsEnabledByDefault(int code);

        /// <summary>
        /// Given an error code (like 1234) return the identifier (CS1234 or BC1234).
        /// </summary>
        [PerformanceSensitive(
            "https://github.com/dotnet/roslyn/issues/31964",
            AllowCaptures = false,
            Constraint = "Frequently called by error list filtering; avoid allocations")]
        public string GetIdForErrorCode(int errorCode)
        {
            return s_errorIdCache.GetOrAdd((CodePrefix, errorCode), key => key.prefix + key.code.ToString("0000"));
        }

        /// <summary>
        /// Produces the filtering action for the diagnostic based on the options passed in.
        /// </summary>
        /// <returns>
        /// A new <see cref="DiagnosticInfo"/> with new effective severity based on the options or null if the
        /// diagnostic has been suppressed.
        /// </returns>
        public abstract ReportDiagnostic GetDiagnosticReport(DiagnosticInfo diagnosticInfo, CompilationOptions options);

        /// <summary>
        /// Filter a <see cref="DiagnosticInfo"/> based on the compilation options so that /nowarn and /warnaserror etc. take effect.options
        /// </summary>
        /// <returns>A <see cref="DiagnosticInfo"/> with effective severity based on option or null if suppressed.</returns>
        public DiagnosticInfo? FilterDiagnosticInfo(DiagnosticInfo diagnosticInfo, CompilationOptions options)
        {
            var report = this.GetDiagnosticReport(diagnosticInfo, options);
            switch (report)
            {
                case ReportDiagnostic.Error:
                    return diagnosticInfo.GetInstanceWithSeverity(DiagnosticSeverity.Error);
                case ReportDiagnostic.Warn:
                    return diagnosticInfo.GetInstanceWithSeverity(DiagnosticSeverity.Warning);
                case ReportDiagnostic.Info:
                    return diagnosticInfo.GetInstanceWithSeverity(DiagnosticSeverity.Info);
                case ReportDiagnostic.Hidden:
                    return diagnosticInfo.GetInstanceWithSeverity(DiagnosticSeverity.Hidden);
                case ReportDiagnostic.Suppress:
                    return null;
                default:
                    return diagnosticInfo;
            }
        }

#if DEBUG
        internal abstract bool ShouldAssertExpectedMessageArgumentsLength(int errorCode);
#endif

        // Common error messages 

        public abstract int ERR_FailedToCreateTempFile { get; }
        public abstract int ERR_MultipleAnalyzerConfigsInSameDir { get; }

        // command line:
        public abstract int ERR_ExpectedSingleScript { get; }
        public abstract int ERR_OpenResponseFile { get; }
        public abstract int ERR_InvalidPathMap { get; }
        public abstract int FTL_InvalidInputFileName { get; }
        public abstract int ERR_FileNotFound { get; }
        public abstract int ERR_NoSourceFile { get; }
        public abstract int ERR_CantOpenFileWrite { get; }
        public abstract int ERR_OutputWriteFailed { get; }
        public abstract int WRN_NoConfigNotOnCommandLine { get; }
        public abstract int ERR_BinaryFile { get; }
        public abstract int WRN_UnableToLoadAnalyzer { get; }
        public abstract int INF_UnableToLoadSomeTypesInAnalyzer { get; }
        public abstract int WRN_AnalyzerCannotBeCreated { get; }
        public abstract int WRN_NoAnalyzerInAssembly { get; }
        public abstract int WRN_AnalyzerReferencesFramework { get; }
        public abstract int WRN_AnalyzerReferencesNewerCompiler { get; }
        public abstract int WRN_DuplicateAnalyzerReference { get; }
        public abstract int ERR_CantReadRulesetFile { get; }
        public abstract int ERR_CompileCancelled { get; }

        // parse options:
        public abstract int ERR_BadSourceCodeKind { get; }
        public abstract int ERR_BadDocumentationMode { get; }

        // compilation options:
        public abstract int ERR_BadCompilationOptionValue { get; }
        public abstract int ERR_MutuallyExclusiveOptions { get; }

        // emit options:
        public abstract int ERR_InvalidDebugInformationFormat { get; }
        public abstract int ERR_InvalidFileAlignment { get; }
        public abstract int ERR_InvalidSubsystemVersion { get; }
        public abstract int ERR_InvalidOutputName { get; }
        public abstract int ERR_InvalidInstrumentationKind { get; }
        public abstract int ERR_InvalidHashAlgorithmName { get; }

        // reference manager:
        public abstract int ERR_MetadataFileNotAssembly { get; }
        public abstract int ERR_MetadataFileNotModule { get; }
        public abstract int ERR_InvalidAssemblyMetadata { get; }
        public abstract int ERR_InvalidModuleMetadata { get; }
        public abstract int ERR_ErrorOpeningAssemblyFile { get; }
        public abstract int ERR_ErrorOpeningModuleFile { get; }
        public abstract int ERR_MetadataFileNotFound { get; }
        public abstract int ERR_MetadataReferencesNotSupported { get; }
        public abstract int ERR_LinkedNetmoduleMetadataMustProvideFullPEImage { get; }

        public abstract void ReportDuplicateMetadataReferenceStrong(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity);
        public abstract void ReportDuplicateMetadataReferenceWeak(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity);

        // signing:
        public abstract int ERR_PublicKeyFileFailure { get; }
        public abstract int ERR_PublicKeyContainerFailure { get; }
        public abstract int ERR_OptionMustBeAbsolutePath { get; }

        // resources:
        public abstract int ERR_CantReadResource { get; }
        public abstract int ERR_CantOpenWin32Resource { get; }
        public abstract int ERR_CantOpenWin32Manifest { get; }
        public abstract int ERR_CantOpenWin32Icon { get; }
        public abstract int ERR_BadWin32Resource { get; }
        public abstract int ERR_ErrorBuildingWin32Resource { get; }
        public abstract int ERR_ResourceNotUnique { get; }
        public abstract int ERR_ResourceFileNameNotUnique { get; }
        public abstract int ERR_ResourceInModule { get; }

        // pseudo-custom attributes:
        public abstract int ERR_PermissionSetAttributeFileReadError { get; }
        public abstract int ERR_MethodImplAttributeAsyncCannotBeUsed { get; }

        // PDB writing:
        public abstract int ERR_EncodinglessSyntaxTree { get; }
        public abstract int WRN_PdbUsingNameTooLong { get; }
        public abstract int WRN_PdbLocalNameTooLong { get; }
        public abstract int ERR_PdbWritingFailed { get; }

        // PE writing:
        public abstract int ERR_MetadataNameTooLong { get; }
        public abstract int ERR_EncReferenceToAddedMember { get; }
        public abstract int ERR_TooManyUserStrings { get; }
        public abstract int ERR_TooManyUserStrings_RestartRequired { get; }
        public abstract int ERR_PeWritingFailure { get; }
        public abstract int ERR_ModuleEmitFailure { get; }
        public abstract int ERR_EncUpdateFailedMissingSymbol { get; }
        public abstract int ERR_InvalidDebugInfo { get; }
        public abstract int ERR_FunctionPointerTypesInAttributeNotSupported { get; }
        public abstract int ERR_DataSectionStringLiteralHashCollision { get; }

        // Generators:
        public abstract int WRN_GeneratorFailedDuringInitialization { get; }
        public abstract int WRN_GeneratorFailedDuringGeneration { get; }

        /// <summary>
        /// Takes an exception produced while writing to a file stream and produces a diagnostic.
        /// </summary>
        public void ReportStreamWriteException(Exception e, string filePath, DiagnosticBag diagnostics)
        {
            diagnostics.Add(CreateDiagnostic(ERR_OutputWriteFailed, Location.None, filePath, e.Message));
        }

        protected abstract void ReportInvalidAttributeArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, AttributeData attribute);

        public void ReportInvalidAttributeArgument(BindingDiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, AttributeData attribute)
        {
            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                ReportInvalidAttributeArgument(diagnosticBag, attributeSyntax, parameterIndex, attribute);
            }
        }

        protected abstract void ReportInvalidNamedArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex, ITypeSymbol attributeClass, string parameterName);

        public void ReportInvalidNamedArgument(BindingDiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex, ITypeSymbol attributeClass, string parameterName)
        {
            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                ReportInvalidNamedArgument(diagnosticBag, attributeSyntax, namedArgumentIndex, attributeClass, parameterName);
            }
        }

        protected abstract void ReportParameterNotValidForType(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex);

        public void ReportParameterNotValidForType(BindingDiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex)
        {
            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                ReportParameterNotValidForType(diagnosticBag, attributeSyntax, namedArgumentIndex);
            }
        }

        protected abstract void ReportMarshalUnmanagedTypeNotValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute);

        public void ReportMarshalUnmanagedTypeNotValidForFields(BindingDiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                ReportMarshalUnmanagedTypeNotValidForFields(diagnosticBag, attributeSyntax, parameterIndex, unmanagedTypeName, attribute);
            }
        }

        protected abstract void ReportMarshalUnmanagedTypeOnlyValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute);

        public void ReportMarshalUnmanagedTypeOnlyValidForFields(BindingDiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                ReportMarshalUnmanagedTypeOnlyValidForFields(diagnosticBag, attributeSyntax, parameterIndex, unmanagedTypeName, attribute);
            }
        }

        protected abstract void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName);

        public void ReportAttributeParameterRequired(BindingDiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName)
        {
            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                ReportAttributeParameterRequired(diagnosticBag, attributeSyntax, parameterName);
            }
        }

        protected abstract void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2);

        public void ReportAttributeParameterRequired(BindingDiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2)
        {
            if (diagnostics.DiagnosticBag is DiagnosticBag diagnosticBag)
            {
                ReportAttributeParameterRequired(diagnosticBag, attributeSyntax, parameterName1, parameterName2);
            }
        }

        public abstract int ERR_BadAssemblyName { get; }

        public abstract int? WRN_ByValArraySizeConstRequired { get; }
    }
}
