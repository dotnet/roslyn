using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Roslyn.Services.OptionService;

namespace Roslyn.Services.Formatting.Options
{
    [ExportOptionProvider(FormattingOptionDefinitions.FeatureName, "1.0.0.1")]
    [ExcludeFromCodeCoverage]
    internal class FormattingDefaultOptionsProvider : AbstractOptionProvider
    {
        protected override IEnumerable<string> AllOptionNames
        {
            get
            {
                yield return FormattingOptionDefinitions.UseDebugMode.Name;
            }
        }

        protected override object GetOptionDefaultValue(string optionName)
        {
            if (FormattingOptionDefinitions.UseDebugMode.Name == optionName)
            {
                return false;
            }

            throw new ArgumentException("invalid option name" + optionName);
        }
    }
}