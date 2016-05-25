using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Editor.CSharp.SplitStringLiteral
{
    internal class SplitStringLiteralOptions
    {
        public const string FeatureName = "SplitStringLiteral";

        public static PerLanguageOption<bool> Enabled =
            new PerLanguageOption<bool>(FeatureName, nameof(Enabled), defaultValue: true);
    }

    [ExportOptionProvider, Shared]
    internal class SplitStringLiteralOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = ImmutableArray.Create<IOption>(
            SplitStringLiteralOptions.Enabled);

        public IEnumerable<IOption> GetOptions() => _options;
    }
}
