// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

internal class MessageData : ILogMessageData
{
    private readonly Func<string> _getName;
    private readonly Func<string> _getMessage;

    public MessageData(Func<string> getName, Func<string> getMessage)
    {
        _getName = getName;
        _getMessage = getMessage;
    }

    public string GetName() => _getName();

    public string GetMessage() => _getMessage();
}
