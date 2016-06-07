using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    [ExportOptionProvider, Shared]
    internal class AddImportOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = ImmutableArray.Create<IOption>(
            AddImportOptions.SuggestForTypesInReferenceAssemblies,
            AddImportOptions.SuggestForTypesInNuGetPackages);

        public IEnumerable<IOption> GetOptions() => _options;
    }
}
