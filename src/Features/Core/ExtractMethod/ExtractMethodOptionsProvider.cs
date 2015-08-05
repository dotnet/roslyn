using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ExtractMethod
{
    [ExportOptionProvider, Shared]
    internal class ExtractMethodOptionsProvider : IOptionProvider
    {
        private readonly IEnumerable<IOption> _options = new List<IOption>
            {
                ExtractMethodOptions.AllowBestEffort,
                ExtractMethodOptions.DontPutOutOrRefOnStruct,
                ExtractMethodOptions.AllowMovingDeclaration
            }.ToImmutableArray();

        public IEnumerable<IOption> GetOptions()
        {
            return _options;
        }
    }
}
