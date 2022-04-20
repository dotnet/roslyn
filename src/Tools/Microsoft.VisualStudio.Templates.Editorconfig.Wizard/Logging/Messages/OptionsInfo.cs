// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Generator;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

internal class OptionsInfo : IEnumerable<ILogMessage<MessageData>>
{
    private readonly ImmutableArray<(string feature, ImmutableArray<IOption> options)> _groupedOptions;
    private readonly OptionSet _optionSet;

    public OptionsInfo(ImmutableArray<(string feature, ImmutableArray<IOption> options)> groupedOptions, OptionSet optionSet)
    {
        _groupedOptions = groupedOptions;
        _optionSet = optionSet;
    }

    public IEnumerator<ILogMessage<MessageData>> GetEnumerator()
    {
        foreach (var option in _groupedOptions.SelectMany(x => x.options))
        {
            if (RoslynEditorConfigFileGenerator.CanGetEditorConfigString(option))
            {
                yield return new OptionInfo(() =>
                {
                    var optionMessage = new MessageData(
                        "option", getMessage: () => RoslynEditorConfigFileGenerator.GetEditorConfigOptionString(option, _optionSet));
                    var valueMessage = new MessageData(
                        "value", getMessage: () => RoslynEditorConfigFileGenerator.GetEditorConfigValueString(option, _optionSet));
                    return ImmutableArray.Create(optionMessage, valueMessage);
                });
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private class OptionInfo : ILogMessage<MessageData>
    {
        private readonly Func<ImmutableArray<MessageData>> _generateMessageData;

        public OptionInfo(Func<ImmutableArray<MessageData>> generateMessageData)
        {
            _generateMessageData = generateMessageData;
        }

        public ImmutableArray<MessageData> GetMessageData()
        {
            return _generateMessageData();
        }
    }
}
