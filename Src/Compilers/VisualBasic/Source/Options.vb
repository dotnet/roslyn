Imports System.Collections.Generic
Imports System.Globalization
Imports System.Runtime.InteropServices
Imports System.Threading
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Internal
Imports Roslyn.Compilers.Internal.Contract

Namespace Roslyn.Compilers.VisualBasic

    Public NotInheritable Class ParseOptions
        Implements IParseOptions

        Public Shared ReadOnly [Default] As ParseOptions = New ParseOptions()

        Private ReadOnly _preprocessorSymbols As ReadOnlyArray(Of KeyValuePair(Of String, Object))
        Private ReadOnly _suppressDocumentationCommentParse As Boolean
        Private ReadOnly _kind As SourceCodeKind

        Public Sub New(
             Optional preprocessorSymbols As IEnumerable(Of KeyValuePair(Of String, Object)) = Nothing,
             Optional suppressDocumentationCommentParse As Boolean = False,
             Optional kind As SourceCodeKind = SourceCodeKind.Regular)

            _preprocessorSymbols = preprocessorSymbols.AsReadOnlyOrEmpty
            _suppressDocumentationCommentParse = suppressDocumentationCommentParse
            _kind = kind
        End Sub

        Public Function Copy(
            Optional preprocessorSymbols As IEnumerable(Of KeyValuePair(Of String, Object)) = Nothing,
            Optional suppressDocumentationCommentParse As Boolean? = Nothing,
            Optional SourceCodeKind As SourceCodeKind? = Nothing) As ParseOptions

            Return New ParseOptions(
                preprocessorSymbols:=If(preprocessorSymbols, _preprocessorSymbols.AsEnumerable),
                suppressDocumentationCommentParse:=If(suppressDocumentationCommentParse, _suppressDocumentationCommentParse),
                Kind:=If(SourceCodeKind, _kind)
            )
        End Function

        ''' <summary>
        ''' The preprocessor symbols to parse with
        ''' </summary>
        Public ReadOnly Property PreprocessorSymbols As ReadOnlyArray(Of KeyValuePair(Of String, Object))
            Get
                Return _preprocessorSymbols
            End Get
        End Property

        Public ReadOnly Property SuppressDocumentationCommentParse As Boolean
            Get
                Return _suppressDocumentationCommentParse
            End Get
        End Property

        Public ReadOnly Property Kind As SourceCodeKind
            Get
                Return _kind
            End Get
        End Property

#Region "IParseOptions"
        Private ReadOnly Property IParseOptions_Kind As SourceCodeKind Implements IParseOptions.Kind
            Get
                Return Me.Kind
            End Get
        End Property
#End Region
    End Class

    Public Enum OptionStrict As Byte
        Off
        Custom
        [On]
    End Enum

    Public NotInheritable Class CompilationOptions
        Implements ICompilationOptions

        Public Shared ReadOnly [Default] As CompilationOptions = New CompilationOptions()

        Private ReadOnly _globalImports As ReadOnlyArray(Of GlobalImport)
        Private ReadOnly _rootNamespace As String
        Private ReadOnly _optionStrict As OptionStrict
        Private ReadOnly _optionInfer As Boolean
        Private ReadOnly _optionExplicit As Boolean
        Private ReadOnly _optionCompareText As Boolean
        Private ReadOnly _optionRemoveIntegerOverflowChecks As Boolean
        Private ReadOnly _isNetModule As Boolean
        Private ReadOnly _assemblyKind As AssemblyKind

        Public Sub New(
             Optional globalImports As IEnumerable(Of GlobalImport) = Nothing,
             Optional rootNamespace As String = "",
             Optional optionStrict As OptionStrict = OptionStrict.Off,
             Optional optionInfer As Boolean = False,
             Optional optionExplicit As Boolean = False,
             Optional optionCompareText As Boolean = False,
             Optional optionRemoveIntegerOverflowChecks As Boolean = False,
             Optional isNetModule As Boolean = False,
             Optional assemblyKind As AssemblyKind = AssemblyKind.ConsoleApplication)

            If rootNamespace Is Nothing Then
                Throw New ArgumentNullException("rootNamespace")
            End If

            If rootNamespace.Length > 0 And Not OptionsValidator.IsValidNamespaceName(rootNamespace) Then
                ' The command line parser should report error ERRID.ERR_BadNamespaceName1
                Throw New ArgumentException("Invalid namespace name", "rootNamespace")
            End If

            _globalImports = globalImports.AsReadOnlyOrEmpty
            _rootNamespace = rootNamespace
            _optionStrict = optionStrict
            _optionInfer = optionInfer
            _optionExplicit = optionExplicit
            _optionCompareText = optionCompareText
            _optionRemoveIntegerOverflowChecks = optionRemoveIntegerOverflowChecks
            _isNetModule = isNetModule
            _assemblyKind = assemblyKind
        End Sub

        Public Function Copy(
            Optional globalImports As IEnumerable(Of GlobalImport) = Nothing,
            Optional rootNamespace As String = Nothing,
            Optional optionStrict As OptionStrict? = Nothing,
            Optional optionInfer As Boolean? = Nothing,
            Optional optionExplicit As Boolean? = Nothing,
            Optional optionCompareText As Boolean? = Nothing,
            Optional optionRemoveIntegerOverflowChecks As Boolean? = Nothing,
            Optional isNetModule As Boolean? = Nothing,
            Optional assemblyKind As AssemblyKind? = Nothing) As CompilationOptions

            Return New CompilationOptions(
                globalImports:=If(globalImports, _globalImports.AsEnumerable),
                rootNamespace:=If(rootNamespace, _rootNamespace),
                optionStrict:=If(optionStrict, _optionStrict),
                optionInfer:=If(optionInfer, _optionInfer),
                optionExplicit:=If(optionExplicit, _optionExplicit),
                optionCompareText:=If(optionCompareText, _optionCompareText),
                optionRemoveIntegerOverflowChecks:=If(optionRemoveIntegerOverflowChecks, _optionRemoveIntegerOverflowChecks),
                isNetModule:=If(isNetModule, _isNetModule),
                assemblyKind:=If(assemblyKind, _assemblyKind)
            )
        End Function

        ''' <summary>
        ''' The list of global imports.
        ''' </summary>
        Public ReadOnly Property GlobalImports As ReadOnlyArray(Of GlobalImport)
            Get
                Return _globalImports
            End Get
        End Property

        ''' <summary>
        ''' The default namespace for all source code in the project. Corresponds to the 
        ''' "RootNamespace" project option or the "/rootnamespace" command line option.
        ''' </summary>
        Public ReadOnly Property RootNamespace As String
            Get
                Return _rootNamespace
            End Get
        End Property

        Friend Function GetRootNamespaceParts() As String()
            Return If(_rootNamespace.Length = 0, {}, _rootNamespace.Split("."c))
        End Function

        ''' <summary>
        ''' The imported namespaces for all source code in the project. Corresponds to the 
        ''' /imports command line options.
        ''' </summary>
        Public ReadOnly Property ImportedNamespaces As ReadOnlyArray(Of GlobalImport)
            Get
                Return _globalImports
            End Get
        End Property

        ''' <summary>
        ''' True if Option Strict On is in effect by default. False if Option Strict Off is in effect by default.
        ''' </summary>
        Public ReadOnly Property OptionStrict As OptionStrict
            Get
                Return _optionStrict
            End Get
        End Property

        ''' <summary>
        ''' True if Option Infer On is in effect by default. False if Option Infer Off is in effect by default.
        ''' </summary>
        Public ReadOnly Property OptionInfer As Boolean
            Get
                Return _optionInfer
            End Get
        End Property

        ''' <summary>
        ''' Retrieves the TargetValue setting of thie compilation
        ''' </summary>
        Public ReadOnly Property IsNetModule As Boolean
            Get
                Return _isNetModule
            End Get
        End Property

        ''' <summary>
        ''' Retrieves the AssemblyKind setting of thie compilation
        ''' </summary>
        Public ReadOnly Property AssemblyKind As AssemblyKind
            Get
                Return _assemblyKind
            End Get
        End Property

        ''' <summary>
        ''' True if Option Explicit On is in effect by default. False if Option Explicit Off is in effect by default.
        ''' </summary>
        Public ReadOnly Property OptionExplicit As Boolean
            Get
                Return _optionExplicit
            End Get
        End Property

        ''' <summary>
        ''' True if Option Compare Text is in effect by default. False if Option Compare Binary is in effect by default.
        ''' </summary>
        Public ReadOnly Property OptionCompareText As Boolean
            Get
                Return _optionCompareText
            End Get
        End Property

        ''' <summary>
        ''' True if integer overflow checking is off. 
        ''' </summary>
        Public ReadOnly Property OptionRemoveIntegerOverflowChecks As Boolean
            Get
                Return _optionRemoveIntegerOverflowChecks
            End Get
        End Property

#Region "ICompilationOptions"
        Private ReadOnly Property ICompilationOptions_AssemblyKind As AssemblyKind Implements ICompilationOptions.AssemblyKind
            Get
                Return Me.AssemblyKind
            End Get
        End Property

        Private ReadOnly Property ICompilationOPtions_IsNetModule As Boolean Implements ICompilationOptions.IsNetModule
            Get
                Return Me.IsNetModule
            End Get
        End Property
#End Region
    End Class

    Public NotInheritable Class GlobalImport
        Private ReadOnly _clause As ImportsClauseSyntax
        Private ReadOnly _importedName As String

        Friend Sub New(clause As ImportsClauseSyntax, importedName As String)
            Debug.Assert(clause IsNot Nothing)
            Debug.Assert(importedName IsNot Nothing)
            _clause = clause
            _importedName = importedName
        End Sub

        ''' <summary>
        ''' The import clause (a namespace name, an alias, or an XML namespace alias).
        ''' </summary>
        Public ReadOnly Property Clause As ImportsClauseSyntax
            Get
                Return _clause
            End Get
        End Property

        Public Shared Function Parse(importedNames As String) As GlobalImport
            Return Parse({importedNames})(0)
        End Function

        Public Shared Function Parse(importedNames As String, <Out()> ByRef diagnostics As IEnumerable(Of Diagnostic)) As GlobalImport
            Return Parse({importedNames}, diagnostics)(0)
        End Function

        Public Shared Function Parse(importedNames As IEnumerable(Of String)) As IEnumerable(Of GlobalImport)
            Dim errors As DiagnosticBag = Nothing
            Dim parsedImports = OptionsValidator.ParseImports(importedNames, errors)
            Dim firstError = If(errors Is Nothing, Nothing, errors.FirstOrDefault(Function(diag) diag.Info.Severity = DiagnosticSeverity.Error))
            If firstError IsNot Nothing Then
                Throw New ArgumentException(firstError.Info.GetMessage(CultureInfo.CurrentCulture))
            End If
            Return parsedImports
        End Function

        Public Shared Function Parse(importedNames As IEnumerable(Of String), <Out()> ByRef diagnostics As IEnumerable(Of Diagnostic)) As IEnumerable(Of GlobalImport)
            Dim errors As DiagnosticBag = Nothing
            Dim parsedImports = OptionsValidator.ParseImports(importedNames, errors)
            diagnostics = If(errors Is Nothing, Nothing, errors.Seal.Cast(Of Diagnostic))
            Return parsedImports
        End Function

        ' Map a diagnostic to the diagnostic we want to give.
        Friend Function MapDiagnostic(unmappedDiag As Diagnostic) As Diagnostic
            If unmappedDiag.Info.Code = ERRID.WRN_UndefinedOrEmptyNamespaceOrClass1 Then
                Return New Diagnostic(ErrorFactory.ErrorInfo(ERRID.WRN_UndefinedOrEmpyProjectNamespaceOrClass1, _importedName), NoLocation.Singleton)
            Else
                ' Determine the text of the import, plus the startIndex/length within that text
                ' that the error is.
                Dim unmappedSpan = unmappedDiag.Location.SourceSpan
                Dim startindex = unmappedSpan.Start - _clause.Span.Start
                Dim length = unmappedSpan.Length
                If (startindex < 0 OrElse length <= 0 OrElse startindex >= _importedName.Length) Then
                    ' startIndex, length are bad for some reason. Used the whole import text instead.
                    startindex = 0
                    length = _importedName.Length
                End If
                length = Math.Min(_importedName.Length - startindex, length)

                ' Create a diagnostic with no location that wrapped the actual parser diagnostic.
                Return New Diagnostic(New ImportDiagnosticInfo(unmappedDiag.Info, _importedName, startindex, length), NoLocation.Singleton)
            End If
        End Function

        ' A special Diagnostic info that wraps a particular diagnostic but customized the message with 
        ' the text of the import.
        Private Class ImportDiagnosticInfo
            Inherits DiagnosticInfo

            Private _importText As String
            Private _startIndex, _length As Integer
            Private _wrappedDiagnostic As DiagnosticInfo

            Public Overrides Function GetMessage(culture As System.Globalization.CultureInfo) As String
                Dim msg = ErrorFactory.IdToString(ERRID.ERR_GeneralProjectImportsError3, culture)
                Return String.Format(msg, _importText, _importText.Substring(_startIndex, _length), _wrappedDiagnostic.GetMessage(culture))
            End Function

            Public Sub New(wrappedDiagnostic As DiagnosticInfo,
                           importText As String,
                           startIndex As Integer,
                           length As Integer)
                MyBase.New(ErrorFactory.MessageProvider, wrappedDiagnostic.Code)
                _wrappedDiagnostic = wrappedDiagnostic
                _importText = importText
                _startIndex = startIndex
                _length = length
            End Sub
        End Class
    End Class

End Namespace
