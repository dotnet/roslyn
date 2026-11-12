// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
        return ImmutableArray.Create(new MessageData("value", () => _value?.ToString()));
    }
}
