Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Imports System.Linq
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Utilities

Namespace Roslyn.Scripting.VisualBasic

    Friend NotInheritable Class SubmissionCompilationFactory
        Inherits CommonSubmissionCompilationFactory

        Private Shared ReadOnly DefaultInteractive As ParseOptions = New ParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Interactive)
        Private Shared ReadOnly DefaultScript As ParseOptions = New ParseOptions(languageVersion:=LanguageVersion.VisualBasic11, kind:=SourceCodeKind.Script)

        Public Sub New(importedNamespaces As IEnumerable(Of String), metadataFileProvider As MetadataFileProvider)
            MyBase.New(CheckNamespaces(importedNamespaces), metadataFileProvider)
        End Sub

        Private Shared Function CheckNamespaces(importedNamespaces As IEnumerable(Of String)) As IEnumerable(Of String)
            ' TODO (tomat):
            If importedNamespaces IsNot Nothing Then
                Dim invalidNamespace As String = importedNamespaces.FirstOrDefault(Function(u) Not u.IsValidClrNamespaceName())
                If invalidNamespace IsNot Nothing Then
                    Throw New ArgumentException(String.Format("Invalid namespace name: '{0}'", invalidNamespace), "importedNamespaces")
                End If
            End If

            Return importedNamespaces
        End Function

        Friend Overrides Function CreateCompilation(code As IText,
                                                    path As String,
                                                    isInteractive As Boolean,
                                                    session As Session,
                                                    returnType As Type,
                                                    diagnostics As DiagnosticBag) As CommonCompilation

            Debug.Assert(code IsNot Nothing AndAlso path IsNot Nothing AndAlso diagnostics IsNot Nothing)
            Dim previousSubmission As Compilation = If((session IsNot Nothing), DirectCast(session.LastSubmission, Compilation), Nothing)
            Dim references As IEnumerable(Of MetadataReference) = GetReferences(session)
            Dim globalImports As IEnumerable(Of GlobalImport) = System.Linq.Enumerable.Empty(Of GlobalImport)() ' TODO: GetImportedNamespaces(session)

            ' TODO (tomat): BaseDirectory should be a property on ScriptEngine?
            Dim fileResolver As FileResolver = session.GetFileResolver(session, Directory.GetCurrentDirectory())

            ' parse:
            Dim parseOptions = If(isInteractive, DefaultInteractive, DefaultScript)
            Dim tree = SyntaxTree.ParseText(code, path, parseOptions)
            diagnostics.Add(tree.GetDiagnostics())
            If diagnostics.HasAnyErrors() Then
                Return Nothing
            End If

            ' create compilation:
            Dim assemblyName As String = Nothing
            Dim submissionTypeName As String = Nothing
            GenerateSubmissionId(assemblyName, submissionTypeName)

            Dim submission = Compilation.CreateSubmission(assemblyName,
                New CompilationOptions(
                    OutputKind:=OutputKind.DynamicallyLinkedLibrary,
                    mainTypeName:=Nothing,
                    scriptClassName:=submissionTypeName,
                    globalImports:=globalImports,
                    rootNamespace:="",
                    OptionStrict:=OptionStrict.Off,
                    optionInfer:=True,
                    optionExplicit:=True,
                    optionCompareText:=False,
                    embedVbCoreRuntime:=False,
                    checkOverflow:=True,
                    cryptoKeyContainer:=Nothing,
                    cryptoKeyFile:=Nothing,
                    delaySign:=Nothing,
                    filealignment:=0,
                    baseaddress:=0,
                    Platform:=Platform.AnyCPU,
                    highEntropyVirtualAddressSpace:=False),
                tree,
                previousSubmission,
                references,
                fileResolver,
                Me.metadataFileProvider,
                returnType,
                If((session IsNot Nothing), session.HostObjectType, Nothing))

            ValidateReferences(submission, diagnostics)
            If diagnostics.HasAnyErrors() Then
                Return Nothing
            End If

            Return submission
        End Function

        ''' <summary> 
        ''' Checks that the compilation doesn't have any references whose name start with the reserved prefix. 
        ''' </summary>        
        Friend Sub ValidateReferences(compilation As CommonCompilation, diagnostics As DiagnosticBag)
            For Each reference In compilation.ReferencedAssemblyNames
                If IsReservedAssemblyName(reference) Then
                    diagnostics.Add(ERRID.ERR_ReservedAssemblyName, Nothing, reference.GetDisplayName())
                End If
            Next
        End Sub
    End Class
End Namespace

