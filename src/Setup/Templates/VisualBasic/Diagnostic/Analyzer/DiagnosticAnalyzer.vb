<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class $saferootidentifiername$Analyzer
    Inherits DiagnosticAnalyzer

    Public Const DiagnosticId = "$saferootidentifiername$"

    ' You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
    ' See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
    Private Shared ReadOnly Title As LocalizableString = New LocalizableResourceString(NameOf(My.Resources.AnalyzerTitle), My.Resources.ResourceManager, GetType(My.Resources.Resources))
    Private Shared ReadOnly MessageFormat As LocalizableString = New LocalizableResourceString(NameOf(My.Resources.AnalyzerMessageFormat), My.Resources.ResourceManager, GetType(My.Resources.Resources))
    Private Shared ReadOnly Description As LocalizableString = New LocalizableResourceString(NameOf(My.Resources.AnalyzerDescription), My.Resources.ResourceManager, GetType(My.Resources.Resources))
    Private Const Category = "Naming"

    Private Shared Rule As New DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault:=True, description:=Description)

    Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
        Get
            Return ImmutableArray.Create(Rule)
        End Get
    End Property

    Public Overrides Sub Initialize(context As AnalysisContext)
        ' TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
        ' See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
        context.RegisterSymbolAction(AddressOf AnalyzeSymbol, SymbolKind.NamedType)
    End Sub

    Private Sub AnalyzeSymbol(context As SymbolAnalysisContext)
        ' TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find

        Dim namedTypeSymbol = CType(context.Symbol, INamedTypeSymbol)

        ' Find just those named type symbols with names containing lowercase letters.
        If namedTypeSymbol.Name.ToCharArray.Any(AddressOf Char.IsLower) Then
            ' For all such symbols, produce a diagnostic.
            Dim diag = Diagnostic.Create(Rule, namedTypeSymbol.Locations(0), namedTypeSymbol.Name)

            context.ReportDiagnostic(diag)
        End If
    End Sub
End Class