// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

internal class SingleLogMessage<T> : ILogMessage<MessageData>
{
    private readonly T _value;

    public SingleLogMessage(T value)
    {
        _value = value;
    }

    public ImmutableArray<MessageData> GetMessageData()
    {
        return ImmutableArray.Create(new MessageData(() => "value", () => _value.ToString()));
    }
}
