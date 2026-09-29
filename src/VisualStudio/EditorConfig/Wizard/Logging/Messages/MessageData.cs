// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
