// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    [Serializable]
    internal sealed class MessageProvider : CommonMessageProvider, IObjectReference, IObjectWritable, IObjectReadable
    {
        public static readonly MessageProvider Instance = new MessageProvider();

        private MessageProvider()
        {
        }

        object IObjectReference.GetRealObject(StreamingContext context)
        {
            return Instance;
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            // write nothing, always read/deserialized as global Instance
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            return (r) => Instance;
        }

        public override DiagnosticSeverity GetSeverity(int code)
        {
            return ErrorFacts.GetSeverity((ErrorCode)code);
        }

        public override string LoadMessage(int code, CultureInfo language)
        {
            return ErrorFacts.GetMessage((ErrorCode)code, language);
        }

        public override string CodePrefix
        {
            get
            {
                return "CS";
            }
        }

        // Given a message identifier (e.g., CS0219), severity, warning as error and a culture, 
        // get the entire prefix (e.g., "error CS0219: Warning as Error:" for C#) used on error messages.
        public override string GetMessagePrefix(string id, DiagnosticSeverity severity, bool isWarningAsError, CultureInfo culture)
        {
            return String.Format(culture, "{0} {1}{2}",
                severity == DiagnosticSeverity.Error || isWarningAsError ? "error" : "warning",
                id,
                isWarningAsError ? ErrorFacts.GetMessage(MessageID.IDS_WarnAsError, culture) : "");
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

        public override int ERR_FailedToCreateTempFile { get { return (int)ErrorCode.ERR_CantMakeTempFile; } }


        // command line:
        public override int ERR_NoScriptsSpecified { get { return (int)ErrorCode.ERR_NoScriptsSpecified; } }
        public override int ERR_OpenResponseFile { get { return (int)ErrorCode.ERR_OpenResponseFile; } }
        public override int FTL_InputFileNameTooLong { get { return (int)ErrorCode.FTL_InputFileNameTooLong; } }
        public override int ERR_FileNotFound { get { return (int)ErrorCode.ERR_FileNotFound; } }
        public override int ERR_NoSourceFile { get { return (int)ErrorCode.ERR_NoSourceFile; } }
        public override int ERR_CantOpenFileWrite { get { return (int)ErrorCode.ERR_CantOpenFileWrite; } }
        public override int ERR_OutputWriteFailed { get { return (int)ErrorCode.ERR_OutputWriteFailed; } }
        public override int WRN_NoConfigNotOnCommandLine { get { return (int)ErrorCode.WRN_NoConfigNotOnCommandLine; } }
        public override int ERR_BinaryFile { get { return (int)ErrorCode.ERR_BinaryFile; } }
        public override int WRN_AnalyzerCannotBeCreated { get { return (int)ErrorCode.WRN_AnalyzerCannotBeCreated; } }
        public override int WRN_NoAnalyzerInAssembly { get { return (int)ErrorCode.WRN_NoAnalyzerInAssembly; } }
        public override int WRN_UnableToLoadAnalyzer { get { return (int)ErrorCode.WRN_UnableToLoadAnalyzer; } }
        public override int ERR_CantReadRulesetFile { get { return (int)ErrorCode.ERR_CantReadRulesetFile; } }

        // reference manager:
        public override int ERR_MetadataFileNotAssembly { get { return (int)ErrorCode.ERR_ImportNonAssembly; } }
        public override int ERR_MetadataFileNotModule { get { return (int)ErrorCode.ERR_AddModuleAssembly; } }
        public override int ERR_InvalidAssemblyMetadata { get { return (int)ErrorCode.FTL_MetadataCantOpenFile; } }
        public override int ERR_InvalidModuleMetadata { get { return (int)ErrorCode.FTL_MetadataCantOpenFile; } }
        public override int ERR_ErrorOpeningAssemblyFile { get { return (int)ErrorCode.FTL_MetadataCantOpenFile; } }
        public override int ERR_ErrorOpeningModuleFile { get { return (int)ErrorCode.FTL_MetadataCantOpenFile; } }
        public override int ERR_MetadataFileNotFound { get { return (int)ErrorCode.ERR_NoMetadataFile; } }
        public override int ERR_MetadataReferencesNotSupported { get { return (int)ErrorCode.ERR_MetadataReferencesNotSupported; } }
        public override int ERR_LinkedNetmoduleMetadataMustProvideFullPEImage { get { return (int)ErrorCode.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage; } }

        public override void ReportDuplicateMetadataReferenceStrong(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            diagnostics.Add(ErrorCode.ERR_DuplicateImport, (Location)location,
                reference.Display ?? identity.GetDisplayName(),
                equivalentReference.Display ?? equivalentIdentity.GetDisplayName());
        }

        public override void ReportDuplicateMetadataReferenceWeak(DiagnosticBag diagnostics, Location location, MetadataReference reference, AssemblyIdentity identity, MetadataReference equivalentReference, AssemblyIdentity equivalentIdentity)
        {
            diagnostics.Add(ErrorCode.ERR_DuplicateImportSimple, (Location)location,
                identity.Name,
                reference.Display ?? identity.GetDisplayName());
        }

        // signing:
        public override int ERR_PublicKeyFileFailure { get { return (int)ErrorCode.ERR_PublicKeyFileFailure; } }
        public override int ERR_PublicKeyContainerFailure { get { return (int)ErrorCode.ERR_PublicKeyContainerFailure; } }

        // resources:
        public override int ERR_CantReadResource { get { return (int)ErrorCode.ERR_CantReadResource; } }
        public override int ERR_CantOpenWin32Resource { get { return (int)ErrorCode.ERR_CantOpenWin32Res; } }
        public override int ERR_CantOpenWin32Manifest { get { return (int)ErrorCode.ERR_CantOpenWin32Manifest; } }
        public override int ERR_CantOpenWin32Icon { get { return (int)ErrorCode.ERR_CantOpenIcon; } }
        public override int ERR_ErrorBuildingWin32Resource { get { return (int)ErrorCode.ERR_ErrorBuildingWin32Resources; } }
        public override int ERR_BadWin32Resource { get { return (int)ErrorCode.ERR_BadWin32Res; } }
        public override int ERR_ResourceFileNameNotUnique { get { return (int)ErrorCode.ERR_ResourceFileNameNotUnique; } }
        public override int ERR_ResourceNotUnique { get { return (int)ErrorCode.ERR_ResourceNotUnique; } }
        public override int ERR_ResourceInModule { get { return (int)ErrorCode.ERR_CantRefResource; } }

        // pseudo-custom attributes:
        public override int ERR_PermissionSetAttributeFileReadError { get { return (int)ErrorCode.ERR_PermissionSetAttributeFileReadError; } }

        // PDB Writer:
        public override int WRN_PdbUsingNameTooLong { get { return (int)ErrorCode.WRN_DebugFullNameTooLong; } }
        public override int WRN_PdbLocalNameTooLong { get { return (int)ErrorCode.WRN_PdbLocalNameTooLong; } }
        public override int ERR_PdbWritingFailed { get { return (int)ErrorCode.FTL_DebugEmitFailure; } }

        // PE Writer:
        public override int ERR_MetadataNameTooLong { get { return (int)ErrorCode.ERR_MetadataNameTooLong; } }

        public override void ReportInvalidAttributeArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, AttributeData attribute)
        {
            var node = (AttributeSyntax)attributeSyntax;
            CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(parameterIndex, node);
            diagnostics.Add(ErrorCode.ERR_InvalidAttributeArgument, attributeArgumentSyntax.Location, node.GetErrorDisplayName());
        }

        public override void ReportInvalidNamedArgument(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex, ITypeSymbol attributeClass, string parameterName)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_InvalidNamedArgument, node.ArgumentList.Arguments[namedArgumentIndex].Location, parameterName);
        }

        public override void ReportParameterNotValidForType(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int namedArgumentIndex)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_ParameterNotValidForType, node.ArgumentList.Arguments[namedArgumentIndex].Location);
        }

        public override void ReportMarshalUnmanagedTypeNotValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            var node = (AttributeSyntax)attributeSyntax;
            CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(parameterIndex, node);
            diagnostics.Add(ErrorCode.ERR_MarshalUnmanagedTypeNotValidForFields, attributeArgumentSyntax.Location, unmanagedTypeName);
        }

        public override void ReportMarshalUnmanagedTypeOnlyValidForFields(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, int parameterIndex, string unmanagedTypeName, AttributeData attribute)
        {
            var node = (AttributeSyntax)attributeSyntax;
            CSharpSyntaxNode attributeArgumentSyntax = attribute.GetAttributeArgumentSyntax(parameterIndex, node);
            diagnostics.Add(ErrorCode.ERR_MarshalUnmanagedTypeOnlyValidForFields, attributeArgumentSyntax.Location, unmanagedTypeName);
        }

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_AttributeParameterRequired1, node.Name.Location, parameterName);
        }

        public override void ReportAttributeParameterRequired(DiagnosticBag diagnostics, SyntaxNode attributeSyntax, string parameterName1, string parameterName2)
        {
            var node = (AttributeSyntax)attributeSyntax;
            diagnostics.Add(ErrorCode.ERR_AttributeParameterRequired2, node.Name.Location, parameterName1, parameterName2);
        }
    }
}
