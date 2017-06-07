using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Options.Providers;

namespace Microsoft.CodeAnalysis.ValidateFormatString
{
    internal class ValidateFormatStringOption
    {
        public static PerLanguageOption<bool> ReportInvalidPlaceholdersInStringDotFormatExpression =
            new PerLanguageOption<bool>(nameof(ValidateFormatStringOption), nameof(ReportInvalidPlaceholdersInStringDotFormatExpression), defaultValue: true,
                storageLocations: new RoamingProfileStorageLocation("TextEditor.%LANGUAGE%.Specific.WarnOnInvalidStringDotFormatCalls"));
    }

    [ExportOptionProvider, Shared]
    internal class SplitStringLiteralOptionsProvider : IOptionProvider
    {
        public ImmutableArray<IOption> Options { get; } = ImmutableArray.Create<IOption>(
            ValidateFormatStringOption.ReportInvalidPlaceholdersInStringDotFormatExpression);
    }
}
