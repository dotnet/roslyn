using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.Formatting
{
    [ExportOptionProvider, Shared]
    internal class FormattingOptionsProvider : IOptionProvider
    {
        private IEnumerable<IOption> _options = new List<IOption>
        {
            FormattingOptions.UseTabs,
            FormattingOptions.TabSize,
            FormattingOptions.IndentationSize,
            FormattingOptions.SmartIndent
        }.ToImmutableArray();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
