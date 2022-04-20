// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Generator;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

internal class OptionsInfo : ILogMessage<MessageData>
{
    private readonly ImmutableArray<(string feature, ImmutableArray<IOption> options)> _groupedOptions;
    private readonly OptionSet _optionSet;

    public OptionsInfo(ImmutableArray<(string feature, ImmutableArray<IOption> options)> groupedOptions, OptionSet optionSet)
    {
        _groupedOptions = groupedOptions;
        _optionSet = optionSet;
    }

    public ImmutableArray<MessageData> GetMessageData()
    {
        var builder = ImmutableArray.CreateBuilder<MessageData>();
        foreach (var option in _groupedOptions.SelectMany(x => x.options))
        {
            if(RoslynEditorConfigFileGenerator.CanGetEditorConfigString(option))
            {
                builder.Add(new MessageData(
                    getName: () => RoslynEditorConfigFileGenerator.GetEditorConfigOptionString(option, _optionSet),
                    getMessage: () => RoslynEditorConfigFileGenerator.GetEditorConfigValueString(option, _optionSet)));
            }
        }
        return builder.ToImmutable();
    }
}
