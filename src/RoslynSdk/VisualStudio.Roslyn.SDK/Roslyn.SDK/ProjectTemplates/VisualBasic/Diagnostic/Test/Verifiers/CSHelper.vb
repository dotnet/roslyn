Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CSharp

Friend Module CSharpVerifierHelper
    ''' <summary>
    ''' By default, the compiler reports diagnostics for nullable reference types at
    ''' <see cref="DiagnosticSeverity.Warning"/>, and the analyzer test framework defaults to only validating
    ''' diagnostics at <see cref="DiagnosticSeverity.Error"/>. This map contains all compiler diagnostic IDs
    ''' related to nullability mapped to <see cref="ReportDiagnostic.Error"/>, which is then used to enable all
    ''' of these warnings for default validation during analyzer and code fix tests.
    ''' </summary>
    Friend ReadOnly Property NullableWarnings As ImmutableDictionary(Of String, ReportDiagnostic) = GetNullableWarningsFromCompiler()

    Private Function GetNullableWarningsFromCompiler() As ImmutableDictionary(Of String, ReportDiagnostic)
        Dim args = {"/warnaserror:nullable"}
        Dim commandLineArguments = CSharpCommandLineParser.Default.Parse(args, baseDirectory:=Environment.CurrentDirectory, sdkDirectory:=Environment.CurrentDirectory)
        Dim nullableWarnings = commandLineArguments.CompilationOptions.SpecificDiagnosticOptions

        ' Workaround for https://github.com/dotnet/roslyn/issues/41610
        nullableWarnings = nullableWarnings _
            .SetItem("CS8632", ReportDiagnostic.Error) _
            .SetItem("CS8669", ReportDiagnostic.Error)

        Return nullableWarnings
    End Function
End Module
