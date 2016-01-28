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

        Public Overrides ReadOnly Property CodePrefix As String
            Get
                ' VB uses "BC" (for Basic Compiler) to identifier its error messages.
                Return "BC"
            End Get
        End Property

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

        Public Overrides ReadOnly Property ErrorCodeType As Type
            Get
                Return GetType(ERRID)
            End Get
        End Property

        Public Overrides Function CreateDiagnostic(code As Integer, location As Location, ParamArray args() As Object) As Diagnostic
            Return New VBDiagnostic(ErrorFactory.ErrorInfo(CType(code, ERRID), args), location)
        End Function

        Public Overrides Function ConvertSymbolToString(errorCode As Integer, symbol As ISymbol) As String
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


        Public Overrides ReadOnly Property ERR_FailedToCreateTempFile As Integer
            Get
                Return ERRID.ERR_UnableToCreateTempFile
            End Get
        End Property

        ' command line:
        Public Overrides ReadOnly Property ERR_ExpectedSingleScript As Integer
            Get
                Return ERRID.ERR_ExpectedSingleScript
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_OpenResponseFile As Integer
            Get
                Return ERRID.ERR_NoResponseFile
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_InvalidPathMap As Integer
            Get
                Return ERRID.ERR_InvalidPathMap
            End Get
        End Property

        Public Overrides ReadOnly Property FTL_InputFileNameTooLong As Integer
            Get
                Return ERRID.FTL_InputFileNameTooLong
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_FileNotFound As Integer
            Get
                Return ERRID.ERR_FileNotFound
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_NoSourceFile As Integer
            Get
                Return ERRID.ERR_BadModuleFile1
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_CantOpenFileWrite As Integer
            Get
                Return ERRID.ERR_CantOpenFileWrite
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_OutputWriteFailed As Integer
            Get
                Return ERRID.ERR_CantOpenFileWrite
            End Get
        End Property

        Public Overrides ReadOnly Property WRN_NoConfigNotOnCommandLine As Integer
            Get
                Return ERRID.WRN_NoConfigInResponseFile
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_BinaryFile As Integer
            Get
                Return ERRID.ERR_BinaryFile
            End Get
        End Property

        Public Overrides ReadOnly Property WRN_AnalyzerCannotBeCreated As Integer
            Get
                Return ERRID.WRN_AnalyzerCannotBeCreated
            End Get
        End Property

        Public Overrides ReadOnly Property WRN_NoAnalyzerInAssembly As Integer
            Get
                Return ERRID.WRN_NoAnalyzerInAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property WRN_UnableToLoadAnalyzer As Integer
            Get
                Return ERRID.WRN_UnableToLoadAnalyzer
            End Get
        End Property

        Public Overrides ReadOnly Property INF_UnableToLoadSomeTypesInAnalyzer As Integer
            Get
                Return ERRID.INF_UnableToLoadSomeTypesInAnalyzer
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_CantReadRulesetFile As Integer
            Get
                Return ERRID.ERR_CantReadRulesetFile
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_CompileCancelled As Integer
            Get
                ' TODO: Add an error code for CompileCancelled
                Return ERRID.ERR_None
            End Get
        End Property

        ' compilation options:

        Public Overrides ReadOnly Property ERR_BadCompilationOptionValue As Integer
            Get
                Return ERRID.ERR_InvalidSwitchValue
            End Get
        End Property

        ' emit options:

        Public Overrides ReadOnly Property ERR_InvalidDebugInformationFormat As Integer
            Get
                Return ERRID.ERR_InvalidDebugInformationFormat
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_InvalidOutputName As Integer
            Get
                Return ERRID.ERR_InvalidOutputName
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_InvalidFileAlignment As Integer
            Get
                Return ERRID.ERR_InvalidFileAlignment
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_InvalidSubsystemVersion As Integer
            Get
                Return ERRID.ERR_InvalidSubsystemVersion
            End Get
        End Property


        ' reference manager:
        Public Overrides ReadOnly Property ERR_MetadataFileNotAssembly As Integer
            Get
                Return ERRID.ERR_MetaDataIsNotAssembly
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_MetadataFileNotModule As Integer
            Get
                Return ERRID.ERR_MetaDataIsNotModule
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_InvalidAssemblyMetadata As Integer
            Get
                Return ERRID.ERR_BadMetaDataReference1
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_InvalidModuleMetadata As Integer
            Get
                Return ERRID.ERR_BadModuleFile1
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_ErrorOpeningAssemblyFile As Integer
            Get
                Return ERRID.ERR_BadRefLib1
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_ErrorOpeningModuleFile As Integer
            Get
                Return ERRID.ERR_BadModuleFile1
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_MetadataFileNotFound As Integer
            Get
                Return ERRID.ERR_LibNotFound
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_MetadataReferencesNotSupported As Integer
            Get
                Return ERRID.ERR_MetadataReferencesNotSupported
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_LinkedNetmoduleMetadataMustProvideFullPEImage As Integer
            Get
                Return ERRID.ERR_LinkedNetmoduleMetadataMustProvideFullPEImage
            End Get
        End Property

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

        ' signing:

        Public Overrides ReadOnly Property ERR_CantReadResource As Integer
            Get
                Return ERRID.ERR_UnableToOpenResourceFile1
            End Get
        End Property
        Public Overrides ReadOnly Property ERR_PublicKeyFileFailure As Integer
            Get
                Return ERRID.ERR_PublicKeyFileFailure
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_PublicKeyContainerFailure As Integer
            Get
                Return ERRID.ERR_PublicKeyContainerFailure
            End Get
        End Property

        ' resources:
        Public Overrides ReadOnly Property ERR_CantOpenWin32Resource As Integer
            Get
                Return ERRID.ERR_UnableToOpenResourceFile1 'TODO: refine (DevDiv #12914)
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_CantOpenWin32Manifest As Integer
            Get
                Return ERRID.ERR_UnableToReadUacManifest2
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_CantOpenWin32Icon As Integer
            Get
                Return ERRID.ERR_UnableToOpenResourceFile1 'TODO: refine (DevDiv #12914)
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_ErrorBuildingWin32Resource As Integer
            Get
                Return ERRID.ERR_ErrorCreatingWin32ResourceFile
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_BadWin32Resource As Integer
            Get
                Return ERRID.ERR_ErrorCreatingWin32ResourceFile
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_ResourceFileNameNotUnique As Integer
            Get
                Return ERRID.ERR_DuplicateResourceFileName1
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_ResourceNotUnique As Integer
            Get
                Return ERRID.ERR_DuplicateResourceName1
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_ResourceInModule As Integer
            Get
                Return ERRID.ERR_ResourceInModule
            End Get
        End Property

        ' pseudo-custom attributes

        Public Overrides ReadOnly Property ERR_PermissionSetAttributeFileReadError As Integer
            Get
                Return ERRID.ERR_PermissionSetAttributeFileReadError
            End Get
        End Property

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

        ' PDB Writer
        Public Overrides ReadOnly Property WRN_PdbUsingNameTooLong As Integer
            Get
                Return ERRID.WRN_PdbUsingNameTooLong
            End Get
        End Property

        Public Overrides ReadOnly Property WRN_PdbLocalNameTooLong As Integer
            Get
                Return ERRID.WRN_PdbLocalNameTooLong
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_PdbWritingFailed As Integer
            Get
                Return ERRID.ERR_PDBWritingFailed
            End Get
        End Property

        ' PE Writer
        Public Overrides ReadOnly Property ERR_MetadataNameTooLong As Integer
            Get
                Return ERRID.ERR_TooLongMetadataName
            End Get
        End Property

        Public Overrides ReadOnly Property ERR_EncReferenceToAddedMember As Integer
            Get
                Return ERRID.ERR_EncReferenceToAddedMember
            End Get
        End Property
    End Class

End Namespace
