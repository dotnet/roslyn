using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.CSharp.UpgradeProject
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal sealed class CSharpAsyncMainDiagnosticAnalyzer : DiagnosticAnalyzer, IBuiltInAnalyzer
    {
        private static readonly DiagnosticDescriptor s_diagnosticDescriptor =
            new DiagnosticDescriptor(
                IDEDiagnosticIds.AsyncMainNotAvailableDiagnosticId,
                new LocalizableResourceString(
                    nameof(CSharpFeaturesResources.Feature_async_main_is_not_available),
                    CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                new LocalizableResourceString(
                    nameof(CSharpFeaturesResources.Feature_async_main_is_not_available_in_CSharp_0_Please_use_language_version_7_1_or_greater),
                    CSharpFeaturesResources.ResourceManager, typeof(CSharpFeaturesResources)),
                DiagnosticCategory.Compiler,
                DiagnosticSeverity.Warning,
                isEnabledByDefault: true);

        private static readonly ImmutableDictionary<string, string> s_diagnosticProperties =
            ImmutableDictionary.Create<string, string>().Add(DiagnosticPropertyConstants.RequiredLanguageVersion, "7.1");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
            ImmutableArray.Create(s_diagnosticDescriptor);

        public DiagnosticAnalyzerCategory GetAnalyzerCategory() => DiagnosticAnalyzerCategory.SemanticSpanAnalysis;

        public bool OpenFileOnly(Workspace workspace) => false;

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterCompilationStartAction(CompilationStartAction);
        }

        private static void CompilationStartAction(CompilationStartAnalysisContext context)
        {
            var compilation = context.Compilation;
            if (compilation.Options.OutputKind != OutputKind.ConsoleApplication &&
                compilation.Options.OutputKind != OutputKind.WindowsApplication &&
                compilation.Options.OutputKind != OutputKind.WindowsRuntimeApplication)
            {
                return;
            }

            var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            if (taskType == null)
            {
                return;
            }

            var taskOfTType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            if (taskOfTType == null)
            {
                return;
            }

            var types = new AnalysisTypes(
                taskType,
                taskOfTType.Construct(compilation.GetSpecialType(SpecialType.System_Int32)),
                compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_String)));

            context.RegisterSymbolAction(symbolContext => AnalyzeSymbol(symbolContext, types), SymbolKind.Method);
        }

        private static void AnalyzeSymbol(SymbolAnalysisContext context, AnalysisTypes types)
        {
            var method = (IMethodSymbol)context.Symbol;

            if (IsValidMainMethodKind(method) &&
                IsValidAsyncMainParameterList(method.Parameters, types) &&
                IsValidAsyncMainReturnType(method.ReturnType, types) &&
                method.DeclaringSyntaxReferences.Length == 1)
            {
                var declarationReference = method.DeclaringSyntaxReferences[0];

                var parseOptions = (CSharpParseOptions)declarationReference.SyntaxTree.Options;
                if (parseOptions.Kind == SourceCodeKind.Regular &&
                    parseOptions.LanguageVersion < LanguageVersion.CSharp7_1)
                {
                    var entryPoint = context.Compilation.GetEntryPoint(context.CancellationToken);
                    if (entryPoint == null)
                    {
                        var methodDeclaration = (MethodDeclarationSyntax)declarationReference.GetSyntax(context.CancellationToken);
                        var location = methodDeclaration.ReturnType.GetLocation();

                        context.ReportDiagnostic(Diagnostic.Create(
                            s_diagnosticDescriptor,
                            location,
                            s_diagnosticProperties,
                            parseOptions.LanguageVersion.ToDisplayString()));
                    }
                }
            }
        }

        private static bool IsValidMainMethodKind(IMethodSymbol method)
            => method.MethodKind == MethodKind.Ordinary &&
               method.Name == "Main" &&
               method.IsStatic &&
               method.RefKind == RefKind.None &&
               !method.IsImplicitlyDeclared &&
               !method.IsGenericMethod && 
               !method.ContainingType.IsGenericType;

        private static bool IsValidAsyncMainParameterList(ImmutableArray<IParameterSymbol> parameters, AnalysisTypes types)
            => parameters.Length == 0 ||
               parameters.Length == 1 && parameters[0].RefKind == RefKind.None && parameters[0].Type.Equals(types.StringArray);

        private static bool IsValidAsyncMainReturnType(ITypeSymbol returnType, AnalysisTypes types)
            => returnType.Equals(types.Task) ||
               returnType.Equals(types.TaskOfInt);

        private readonly struct AnalysisTypes
        {
            public AnalysisTypes(INamedTypeSymbol task, INamedTypeSymbol taskOfInt, IArrayTypeSymbol stringArray)
            {
                Task = task;
                TaskOfInt = taskOfInt;
                StringArray = stringArray;
            }

            public INamedTypeSymbol Task { get; }
            public INamedTypeSymbol TaskOfInt { get; }
            public IArrayTypeSymbol StringArray { get; }
        }
    }
}
