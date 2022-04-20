// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.VisualStudio.TemplateWizard;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

internal class TemplateInfo : ILogMessage<MessageData>
{
    private readonly WizardRunKind runKind;
    private readonly Dictionary<string, string> replacementsDictionary;

    public TemplateInfo(WizardRunKind runKind, Dictionary<string, string> replacementsDictionary)
    {
        this.runKind = runKind;
        this.replacementsDictionary = replacementsDictionary;
    }

    public ImmutableArray<MessageData> GetMessageData()
    {
        var builder = ImmutableArray.CreateBuilder<MessageData>();
        builder.Add(new MessageData(() => "WizardRunKind", () => Enum.GetName(runKind.GetType(), runKind)));
        foreach (var kvp in replacementsDictionary)
        {
            builder.Add(new MessageData(() => $"ReplacementsDictionary:{kvp.Key}", () => kvp.Value));
        }
        return builder.ToImmutable();
    }
}
