using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    [ExportOptionProvider, Shared]
    internal class InternalDiagnosticsOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = new List<IOption>
            {
                InternalDiagnosticsOptions.BlueSquiggleForBuildDiagnostic,
                InternalDiagnosticsOptions.UseDiagnosticEngineV2,
                InternalDiagnosticsOptions.CompilationEndCodeFix,
                InternalDiagnosticsOptions.UseCompilationEndCodeFixHeuristic,
                InternalDiagnosticsOptions.BuildErrorIsTheGod,
                InternalDiagnosticsOptions.ClearLiveErrorsForProjectBuilt,
                InternalDiagnosticsOptions.PreferLiveErrorsOnOpenedFiles,
                InternalDiagnosticsOptions.PreferBuildErrorsOverLiveErrors
            }.ToImmutableArray();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
