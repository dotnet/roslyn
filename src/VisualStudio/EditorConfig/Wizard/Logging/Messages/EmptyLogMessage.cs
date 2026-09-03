// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

internal class EmptyLogMessage : ILogMessage<MessageData>
{
    public static EmptyLogMessage Instance { get; } = new EmptyLogMessage();

    public ImmutableArray<MessageData> GetMessageData()
    {
        return ImmutableArray<MessageData>.Empty;
    }
}
