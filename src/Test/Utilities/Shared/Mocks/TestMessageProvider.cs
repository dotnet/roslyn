// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Globalization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Roslyn.Test.Utilities
{
    internal abstract class TestMessageProvider : CommonMessageProvider
    {
        public override Type ErrorCodeType
        {
            get { throw new NotImplementedException(); }
        }

        public override Diagnostic CreateDiagnostic(int code, Location location, params object[] args)
        {
            throw new NotImplementedException();
        }

        public override string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override ReportDiagnostic GetDiagnosticReport(DiagnosticInfo diagnosticInfo, CompilationOptions options)
        {
            throw new NotImplementedException();
        }

        public override int ERR_InvalidPathMap
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_FailedToCreateTempFile
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_ExpectedSingleScript
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_OpenResponseFile
        {
            get { throw new NotImplementedException(); }
        }

        public override int FTL_InputFileNameTooLong
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_FileNotFound
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_NoSourceFile
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_CantOpenFileWrite
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_OutputWriteFailed
        {
            get { throw new NotImplementedException(); }
        }

        public override int WRN_NoConfigNotOnCommandLine
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_BinaryFile
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_MetadataFileNotAssembly
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_MetadataFileNotModule
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_InvalidAssemblyMetadata
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_InvalidModuleMetadata
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_ErrorOpeningAssemblyFile
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_ErrorOpeningModuleFile
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_MetadataFileNotFound
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_MetadataReferencesNotSupported
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_PublicKeyFileFailure
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_PublicKeyContainerFailure
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_CantReadResource
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_CantOpenWin32Resource
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_CantOpenWin32Manifest
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_CantOpenWin32Icon
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_BadWin32Resource
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_ErrorBuildingWin32Resource
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_ResourceNotUnique
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_ResourceFileNameNotUnique
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_ResourceInModule
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_PermissionSetAttributeFileReadError
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_PdbWritingFailed
        {
            get { throw new NotImplementedException(); }
        }

        public override int WRN_PdbUsingNameTooLong
        {
            get { throw new NotImplementedException(); }
        }

        public override int WRN_PdbLocalNameTooLong
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_MetadataNameTooLong
        {
            get { throw new NotImplementedException(); }
        }

        public override int WRN_AnalyzerCannotBeCreated
        {
            get { throw new NotImplementedException(); }
        }

        public override int WRN_NoAnalyzerInAssembly
        {
            get { throw new NotImplementedException(); }
        }

        public override int WRN_UnableToLoadAnalyzer
        {
            get { throw new NotImplementedException(); }
        }

        public override int INF_UnableToLoadSomeTypesInAnalyzer
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_CantReadRulesetFile
        {
            get { throw new NotImplementedException(); }
        }

        public override int ERR_CompileCancelled
        {
            get { throw new NotImplementedException(); }
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

        public override void ReportParameterNotValidForType(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex)
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

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName)
        {
            throw new NotImplementedException();
        }

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2)
        {
            throw new NotImplementedException();
        }

        public override DiagnosticSeverity GetSeverity(int code)
        {
            throw new NotImplementedException();
        }

        public override string LoadMessage(int code, CultureInfo language)
        {
            throw new NotImplementedException();
        }

        public override string CodePrefix
        {
            get { throw new NotImplementedException(); }
        }

        public override int GetWarningLevel(int code)
        {
            throw new NotImplementedException();
        }

        public override int ERR_LinkedNetmoduleMetadataMustProvideFullPEImage
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidDebugInformationFormat
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidFileAlignment
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidSubsystemVersion
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_InvalidOutputName
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_EncReferenceToAddedMember
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int ERR_BadCompilationOptionValue
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}
