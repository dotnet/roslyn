// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

internal class MessageData : ILogMessageData
{
    private readonly Func<string?> _getMessage;

    public MessageData(string name, Func<string?> getMessage)
    {
        Name = name;
        _getMessage = getMessage;
    }

    public string Name { get; }

    public string? GetMessage() => _getMessage();
}
