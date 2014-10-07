Imports System
Imports System.ComponentModel.Composition
Imports System.Diagnostics.CodeAnalysis
Imports System.Linq
Imports System.Text.RegularExpressions
Imports Roslyn.Compilers
Imports Roslyn.Compilers.Common
Imports Roslyn.Compilers.VisualBasic
Imports Roslyn.Services
Imports Roslyn.Services.Host
Imports Roslyn.Services.MSBuild
Imports MSB = Microsoft.Build.Evaluation

Namespace Roslyn.Services.VisualBasic

    <ExportMSBuildLanguageService(GetType(IMSBuildLanguageService), LanguageNames.VisualBasic, "F184B08F-C81C-45F6-A57F-5ABD9991F28F", "vbproj")>
    Partial Friend Class VisualBasicMSBuildLanguageService
        Inherits AbstractMSBuildLanguageService

        <ImportingConstructor()>
        Friend Sub New()
        End Sub

        Public Overrides Function GetCompilationOptions(project As MSB.Project) As ICompilationOptions
            Dim options = CompilationOptions.Default

            Dim outputType = project.GetPropertyValue("OutputType")
            Select Case outputType
                Case "Library"
                    options = options.Copy(assemblyKind:=AssemblyKind.DynamicallyLinkedLibrary)
                Case "Exe"
                    options = options.Copy(assemblyKind:=AssemblyKind.ConsoleApplication)
                Case "WinExe"
                    options = options.Copy(assemblyKind:=AssemblyKind.WindowsApplication)
            End Select

            Dim rootNamespaceProperty = project.GetProperty("RootNamespace")
            If rootNamespaceProperty IsNot Nothing Then
                Dim rootNamespace = rootNamespaceProperty.EvaluatedValue
                If String.Compare(options.RootNamespace, rootNamespace) <> 0 Then
                    options = options.Copy(rootNamespace:=rootNamespace)
                End If
            End If

            Dim optionStrictProperty = project.GetProperty("OptionStrict")
            If optionStrictProperty IsNot Nothing Then
                Dim optStrict As OptionStrict
                If [Enum].TryParse(Of OptionStrict)(optionStrictProperty.EvaluatedValue, optStrict) AndAlso options.OptionStrict <> optStrict Then
                    options = options.Copy(optionStrict:=optStrict)
                End If
            End If

            Dim optionInferProperty = project.GetProperty("OptionInfer")
            If optionInferProperty IsNot Nothing Then
                Dim optionInfer = optionInferProperty.EvaluatedValue
                Dim optInfer As Boolean = optionInfer = "On"
                If options.OptionInfer <> optInfer Then
                    options = options.Copy(optionInfer:=optInfer)
                End If
            End If

            Dim optionExplicitProperty = project.GetProperty("OptionExplicit")
            If optionExplicitProperty IsNot Nothing Then
                Dim optionExplicit = optionExplicitProperty.EvaluatedValue
                Dim optExplicit As Boolean = optionExplicit = "On"
                If options.OptionExplicit <> optExplicit Then
                    options = options.Copy(optionExplicit:=optExplicit)
                End If
            End If

            Dim optionCompareProperty = project.GetProperty("OptionCompare")
            If optionCompareProperty IsNot Nothing Then
                Dim optionCompare = optionCompareProperty.EvaluatedValue
                Dim optCompareText As Boolean = optionCompare = "Text"
                If options.OptionCompareText <> optCompareText Then
                    options = options.Copy(optionCompareText:=optCompareText)
                End If
            End If

            Dim removeIntegerChecksProperty = project.GetProperty("RemoveIntegerChecks")
            If removeIntegerChecksProperty IsNot Nothing Then
                Dim removeIntegerChecks As Boolean
                If Boolean.TryParse(removeIntegerChecksProperty.EvaluatedValue, removeIntegerChecks) AndAlso options.OptionRemoveIntegerOverflowChecks <> removeIntegerChecks Then
                    options = options.Copy(optionRemoveIntegerOverflowChecks:=removeIntegerChecks)
                End If
            End If

            Dim globalImports As String = String.Join(", ", project.GetItems("Import").Select(Function(item) item.EvaluatedInclude))
            options = DirectCast(options.SetOption("GlobalImports", globalImports), CompilationOptions)

            Return options
        End Function

        Public Overrides Function GetSourceCodeKind(documentFileName As String) As SourceCodeKind
            If documentFileName.EndsWith(".vbx") Then
                Return SourceCodeKind.Script
            End If

            Return SourceCodeKind.Regular
        End Function

        Public Overrides Function GetDocumentExtension(sourceCodeKind As SourceCodeKind) As String
            Select Case sourceCodeKind
                Case SourceCodeKind.Script
                    Return ".vbx"
                Case Else
                    Return ".vb"
            End Select
        End Function

        Public Overrides Function GetParseOptions(project As MSB.Project) As IParseOptions
            Return Me.GetParseOptions(project, ParseOptions.Default)
        End Function

        Private Overloads Function GetParseOptions(project As MSB.Project, options As ParseOptions) As ParseOptions

            Dim defineConstantsProperty = project.GetProperty("DefineConstants")
            If defineConstantsProperty IsNot Nothing Then
                Dim defineConstantsValue = defineConstantsProperty.EvaluatedValue
                options = options.Copy(preprocessorSymbols:=CommandLineArguments.ParseConditionalCompilationSymbols(defineConstantsValue))
            End If

            Dim documentationFileProperty = project.GetProperty("DocumentationFile")
            Dim suppress = (documentationFileProperty Is Nothing) OrElse String.IsNullOrEmpty(documentationFileProperty.EvaluatedValue)
            If options.SuppressDocumentationCommentParse <> suppress Then
                options = options.Copy(suppressDocumentationCommentParse:=suppress)
            End If

            Return options
        End Function
    End Class

End Namespace
