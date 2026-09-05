// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.VisualStudio.TemplateWizard;

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
        builder.Add(new MessageData("WizardRunKind", () => Enum.GetName(runKind.GetType(), runKind)));
        foreach (var kvp in replacementsDictionary)
        {
            builder.Add(new MessageData("ReplacementsDictionaryValue", () => kvp.Value));
        }
        return builder.ToImmutable();
    }
}
