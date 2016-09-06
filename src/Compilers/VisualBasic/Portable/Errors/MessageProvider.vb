' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports System.Globalization
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Microsoft.CodeAnalysis.VisualBasic
    Friend NotInheritable Class MessageProvider
        Inherits CommonMessageProvider
        Implements IObjectWritable, IObjectReadable

        Public Shared ReadOnly Instance As MessageProvider = New MessageProvider()

        Private Sub New()
        End Sub

        Private Sub WriteTo(writer As ObjectWriter) Implements IObjectWritable.WriteTo
            ' don't write anything since we always return the shared 'Instance' when read.
        End Sub

        Private Function GetReader() As Func(Of ObjectReader, Object) Implements IObjectReadable.GetReader
            Return Function(r) Instance
        End Function

        Public Overrides ReadOnly Property CodePrefix As String = "BC" ' VB uses "BC" (for Basic Compiler) to identifier its error messages.

        Public Overrides Function LoadMessage(code As Integer, language As CultureInfo) As String
            Return ErrorFactory.IdToString(DirectCast(code, ERRID), language)
        End Function

        Public Overrides Function GetMessageFormat(code As Integer) As LocalizableString
            Return ErrorFactory.GetMessageFormat(DirectCast(code, ERRID))
        End Function

        Public Overrides Function GetDescription(code As Integer) As LocalizableString
            Return ErrorFactory.GetDescription(DirectCast(code, ERRID))
        End Function

        Public Overrides Function GetTitle(code As Integer) As LocalizableString
            Return ErrorFactory.GetTitle(DirectCast(code, ERRID))
        End Function

        Public Overrides Function GetHelpLink(code As Integer) As String
            Return ErrorFactory.GetHelpLink(DirectCast(code, ERRID))
        End Function

        Public Overrides Function GetCategory(code As Integer) As String
            Return ErrorFactory.GetCategory(DirectCast(code, ERRID))
        End Function

        Public Overrides Function GetSeverity(code As Integer) As DiagnosticSeverity
            Dim errid = DirectCast(code, ERRID)
            If errid = ERRID.Void Then
                Return InternalDiagnosticSeverity.Void
            ElseIf errid = ERRID.Unknown Then
                Return InternalDiagnosticSeverity.Unknown
            ElseIf ErrorFacts.IsWarning(errid) Then
                Return DiagnosticSeverity.Warning
            ElseIf ErrorFacts.IsInfo(errid) Then
                Return DiagnosticSeverity.Info
            ElseIf ErrorFacts.IsHidden(errid) Then
                Return DiagnosticSeverity.Hidden
            Else
                Return DiagnosticSeverity.Error
            End If
        End Function

        Public Overrides Function GetWarningLevel(code As Integer) As Integer
            Dim errorId = DirectCast(code, ERRID)
            If ErrorFacts.IsWarning(errorId) AndAlso
               code <> ERRID.WRN_BadSwitch AndAlso
               code <> ERRID.WRN_NoConfigInResponseFile AndAlso
               code <> ERRID.WRN_IgnoreModuleManifest Then
                Return 1
            ElseIf ErrorFacts.IsInfo(errorId) OrElse ErrorFacts.IsHidden(errorId)
                ' Info and hidden diagnostics
                Return 1
            Else
                Return 0
            End If
        End Function

        Public Overrides ReadOnly Property ErrorCodeType As Type = GetType(ERRID)

        Public Overrides Function CreateDiagnostic(code As Integer, location As Location, ParamArray args() As Object) As Diagnostic
            Return New VBDiagnostic(ErrorFactory.ErrorInfo(CType(code, ERRID), args), location)
        End Function

        Public Overrides Function CreateDiagnostic(info As DiagnosticInfo) As Diagnostic
            Return New VBDiagnostic(info, Location.None)
        End Function

        Public Overrides Function GetErrorDisplayString(symbol As ISymbol) As String
            ' show extra info for assembly if possible such as version, public key token etc.
            If symbol.Kind = SymbolKind.Assembly OrElse symbol.Kind = SymbolKind.Namespace Then
                Return symbol.ToString()
            End If

            Return SymbolDisplay.ToDisplayString(symbol, SymbolDisplayFormat.VisualBasicShortErrorMessageFormat)
        End Function

        ' Given a message identifier (e.g., CS0219), severity, warning as error and a culture, 
        ' get the entire prefix (e.g., "error BC42024:" for VB) used on error messages.
        Public Overrides Function GetMessagePrefix(id As String, severity As DiagnosticSeverity, isWarningAsError As Boolean, culture As CultureInfo) As String
            Return String.Format(culture, "{0} {1}",
                    If(severity = DiagnosticSeverity.Error OrElse isWarningAsError, "error", "warning"), id)
        End Function

        Public Overrides Function GetDiagnosticReport(diagnosticInfo As DiagnosticInfo, options As CompilationOptions) As ReportDiagnostic
            Dim hasSourceSuppression = False
            Return VisualBasicDiagnosticFilter.GetDiagnosticReport(diagnosticInfo.Severity,
                                                                   True,
                                                                   diagnosticInfo.MessageIdentifier,
                                                                   Location.None,
                                                                   diagnosticInfo.Category,
                                                                   options.GeneralDiagnosticOption,
                                                                   options.SpecificDiagnosticOptions,
                                                                   hasSourceSuppression)
        End Function

        Public Overrides ReadOnly Property ERR_FailedToCreateTempFile As Integer = ERRID.ERR_UnableToCreateTempFile

#Region " command line:"
        Public Overrides ReadOnly Property ERR_ExpectedSingleScript As Integer = ERRID.ERR_ExpectedSingleScript
        Public Overrides ReadOnly Property ERR_OpenResponseFile As Integer = ERRID.ERR_NoResponseFile
        Public Overrides ReadOnly Property ERR_InvalidPathMap As Integer = ERRID.ERR_InvalidPathMap
        Public Overrides ReadOnly Property FTL_InputFileNameTooLong As Integer = ERRID.FTL_InputFileNameTooLong
        Public Overrides ReadOnly Property ERR_FileNotFound As Integer = ERRID.ERR_FileNotFound
        Public Overrides ReadOnly Property ERR_NoSourceFile As Integer = ERRID.ERR_BadModuleFile1
        Public Overrides ReadOnly Property ERR_CantOpenFileWrite As Integer = ERRID.ERR_CantOpenFileWrite
        Public Overrides ReadOnly Property ERR_OutputWriteFailed As Integer = ERRID.ERR_CantOpenFileWrite
        Public Overrides ReadOnly Property WRN_NoConfigNotOnCommandLine As Integer = ERRID.WRN_NoConfigInResponseFile
        Public Overrides ReadOnly Property ERR_BinaryFile As Integer = ERRID.ERR_BinaryFile
        Public Overrides ReadOnly Property WRN_AnalyzerCannotBeCreated As Integer = ERRID.WRN_AnalyzerCannotBeCreated
        Public Overrides ReadOnly Property WRN_NoAnalyzerInAssembly As Integer = ERRID.WRN_NoAnalyzerInAssembly
        Public Overrides ReadOnly Property WRN_UnableToLoadAnalyzer As Integer = ERRID.WRN_UnableToLoadAnalyzer
        Public Overrides ReadOnly Property INF_UnableToLoadSomeTypesInAnalyzer As Integer = ERRID.INF_UnableToLoadSomeTypesInAnalyzer
        Public Overrides ReadOnly Property ERR_CantReadRulesetFile As Integer = ERRID.ERR_CantReadRulesetFile
        Public Overrides ReadOnly Property ERR_CompileCancelled As Integer = ERRID.ERR_None  ' TODO: Add an error code for CompileCancelled
#End Region
#Region " compilation options:"
        Public Overrides ReadOnly Property ERR_BadCompilationOptionValue As Integer = ERRID.ERR_InvalidSwitchValue
        Public Overrides ReadOnly Property ERR_MutuallyExclusiveOptions As Integer = ERRID.ERR_MutuallyExclusiveOptions
#End Region
#Region " emit options:"
        Public Overrides ReadOnly Property ERR_InvalidDebugInformationFormat As Integer = ERRID.ERR_InvalidDebugInformationFormat
        Public Overrides ReadOnly Property ERR_InvalidOutputName As Integer = ERRID.ERR_InvalidOutputName
        Public Overrides ReadOnly Property ERR_InvalidFileAlignment As Integer = ERRID.ERR_InvalidFileAlignment
        Public Overrides ReadOnly Property ERR_InvalidSubsystemVersion As Integer = ERRID.ERR_InvalidSubsystemVersion
#End Region

#Region "reference manager:"
        Public Overrides ReadOnly Property ERR_MetadataFileNotAssembly As Integer = ERRID.ERR_MetaDataIsNotAssembly
        Public Overrides ReadOnly Property ERR_MetadataFileNotModule As Integer = ERRID.ERR_MetaDataIsNotModule
        Public Overrides ReadOnly Property ERR_InvalidAssemblyMetadata As Integer = ERRID.ERR_BadMetaDataReference1
        Public Overrides ReadOnly Property ERR_InvalidModuleMetadata As Integer = ERRID.ERR_BadModuleFile1
        Public Overrides ReadOnly Property ERR_ErrorOpeningAssemblyFile As Integer = ERRID.ERR_BadRefLib1
        Public Overrides ReadOnly Property ERR_ErrorOpeningModuleFile As Integer = ERRID.ERR_BadModuleFile1
        Public Overrides ReadOnly Property ERR_MetadataFileNotFound As Integer = ERRID.ERR_LibNotFound
        Public Overrides ReadOnly Property ERR_MetadataReferencesNotSupported As Integer = ERRID.ERR_MetadataReferencesNotSupported
        Public Overrides ReadOnly Property ERR_LinkedNetmoduleMetadataMustProvideFullPEImage As Integer = ERRID.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage
#End Region

        Public Overrides Sub ReportDuplicateMetadataReferenceStrong(diagnostics As DiagnosticBag, location As Location, reference As MetadataReference, identity As AssemblyIdentity, equivalentReference As MetadataReference, equivalentIdentity As AssemblyIdentity)
            diagnostics.Add(ERRID.ERR_DuplicateReferenceStrong,
                            DirectCast(location, Location),
                            If(reference.Display, identity.GetDisplayName()),
                            If(equivalentReference.Display, equivalentIdentity.GetDisplayName()))
        End Sub

        Public Overrides Sub ReportDuplicateMetadataReferenceWeak(diagnostics As DiagnosticBag, location As Location, reference As MetadataReference, identity As AssemblyIdentity, equivalentReference As MetadataReference, equivalentIdentity As AssemblyIdentity)
            diagnostics.Add(ERRID.ERR_DuplicateReference2,
                            DirectCast(location, Location),
                            identity.Name,
                            If(equivalentReference.Display, equivalentIdentity.GetDisplayName()))
        End Sub

#Region " signing:"
        Public Overrides ReadOnly Property ERR_CantReadResource As Integer = ERRID.ERR_UnableToOpenResourceFile1
        Public Overrides ReadOnly Property ERR_PublicKeyFileFailure As Integer = ERRID.ERR_PublicKeyFileFailure
        Public Overrides ReadOnly Property ERR_PublicKeyContainerFailure As Integer = ERRID.ERR_PublicKeyContainerFailure
        Public Overrides ReadOnly Property ERR_OptionMustBeAbsolutePath As Integer = ERRID.ERR_OptionMustBeAbsolutePath
#End Region

#Region " resources:"
        Public Overrides ReadOnly Property ERR_CantOpenWin32Resource As Integer = ERRID.ERR_UnableToOpenResourceFile1 'TODO: refine (DevDiv #12914)
        Public Overrides ReadOnly Property ERR_CantOpenWin32Manifest As Integer = ERRID.ERR_UnableToReadUacManifest2
        Public Overrides ReadOnly Property ERR_CantOpenWin32Icon As Integer = ERRID.ERR_UnableToOpenResourceFile1 'TODO: refine (DevDiv #12914)
        Public Overrides ReadOnly Property ERR_ErrorBuildingWin32Resource As Integer = ERRID.ERR_ErrorCreatingWin32ResourceFile
        Public Overrides ReadOnly Property ERR_BadWin32Resource As Integer = ERRID.ERR_ErrorCreatingWin32ResourceFile
        Public Overrides ReadOnly Property ERR_ResourceFileNameNotUnique As Integer = ERRID.ERR_DuplicateResourceFileName1
        Public Overrides ReadOnly Property ERR_ResourceNotUnique As Integer = ERRID.ERR_DuplicateResourceName1
        Public Overrides ReadOnly Property ERR_ResourceInModule As Integer = ERRID.ERR_ResourceInModule
#End Region

#Region " pseudo-custom attributes"
        Public Overrides ReadOnly Property ERR_PermissionSetAttributeFileReadError As Integer = ERRID.ERR_PermissionSetAttributeFileReadError
#End Region

        Public Overrides Sub ReportInvalidAttributeArgument(diagnostics As DiagnosticBag, attributeSyntax As SyntaxNode, parameterIndex As Integer, attribute As AttributeData)
            Dim node = DirectCast(attributeSyntax, AttributeSyntax)
            diagnostics.Add(ERRID.ERR_BadAttribute1, node.ArgumentList.Arguments(parameterIndex).GetLocation(), attribute.AttributeClass)
        End Sub

        Public Overrides Sub ReportInvalidNamedArgument(diagnostics As DiagnosticBag, attributeSyntax As SyntaxNode, namedArgumentIndex As Integer, attributeClass As ITypeSymbol, parameterName As String)
            Dim node = DirectCast(attributeSyntax, AttributeSyntax)
            diagnostics.Add(ERRID.ERR_BadAttribute1, node.ArgumentList.Arguments(namedArgumentIndex).GetLocation(), attributeClass)
        End Sub

        Public Overrides Sub ReportParameterNotValidForType(diagnostics As DiagnosticBag, attributeSyntax As SyntaxNode, namedArgumentIndex As Integer)
            Dim node = DirectCast(attributeSyntax, AttributeSyntax)
            diagnostics.Add(ERRID.ERR_ParameterNotValidForType, node.ArgumentList.Arguments(namedArgumentIndex).GetLocation())
        End Sub

        Public Overrides Sub ReportMarshalUnmanagedTypeNotValidForFields(diagnostics As DiagnosticBag, attributeSyntax As SyntaxNode, parameterIndex As Integer, unmanagedTypeName As String, attribute As AttributeData)
            Dim node = DirectCast(attributeSyntax, AttributeSyntax)
            diagnostics.Add(ERRID.ERR_MarshalUnmanagedTypeNotValidForFields, node.ArgumentList.Arguments(parameterIndex).GetLocation(), unmanagedTypeName)
        End Sub

        Public Overrides Sub ReportMarshalUnmanagedTypeOnlyValidForFields(diagnostics As DiagnosticBag, attributeSyntax As SyntaxNode, parameterIndex As Integer, unmanagedTypeName As String, attribute As AttributeData)
            Dim node = DirectCast(attributeSyntax, AttributeSyntax)
            diagnostics.Add(ERRID.ERR_MarshalUnmanagedTypeOnlyValidForFields, node.ArgumentList.Arguments(parameterIndex).GetLocation(), unmanagedTypeName)
        End Sub

        Public Overrides Sub ReportAttributeParameterRequired(diagnostics As DiagnosticBag, attributeSyntax As SyntaxNode, parameterName As String)
            Dim node = DirectCast(attributeSyntax, AttributeSyntax)
            diagnostics.Add(ERRID.ERR_AttributeParameterRequired1, node.Name.GetLocation(), parameterName)
        End Sub

        Public Overrides Sub ReportAttributeParameterRequired(diagnostics As DiagnosticBag, attributeSyntax As SyntaxNode, parameterName1 As String, parameterName2 As String)
            Dim node = DirectCast(attributeSyntax, AttributeSyntax)
            diagnostics.Add(ERRID.ERR_AttributeParameterRequired2, node.Name.GetLocation(), parameterName1, parameterName2)
        End Sub

#Region " PDB Writer"
        Public Overrides ReadOnly Property ERR_EncodinglessSyntaxTree As Integer = ERRID.ERR_EncodinglessSyntaxTree
        Public Overrides ReadOnly Property WRN_PdbUsingNameTooLong As Integer = ERRID.WRN_PdbUsingNameTooLong
        Public Overrides ReadOnly Property WRN_PdbLocalNameTooLong As Integer = ERRID.WRN_PdbLocalNameTooLong
        Public Overrides ReadOnly Property ERR_PdbWritingFailed As Integer = ERRID.ERR_PDBWritingFailed
#End Region

#Region " PE Writer"
        Public Overrides ReadOnly Property ERR_MetadataNameTooLong As Integer = ERRID.ERR_TooLongMetadataName
        Public Overrides ReadOnly Property ERR_EncReferenceToAddedMember As Integer = ERRID.ERR_EncReferenceToAddedMember
        Public Overrides ReadOnly Property ERR_TooManyUserStrings As Integer = ERRID.ERR_TooManyUserStrings
        Public Overrides ReadOnly Property ERR_PeWritingFailure As Integer = ERRID.ERR_PeWritingFailure
        Public Overrides ReadOnly Property ERR_ModuleEmitFailure As Integer = ERRID.ERR_ModuleEmitFailure
        Public Overrides ReadOnly Property ERR_EncUpdateFailedMissingAttribute As Integer = ERRID.ERR_EncUpdateFailedMissingAttribute
#End Region

    End Class

End Namespace
