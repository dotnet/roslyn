// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

public interface ILogMessage<T> where T : ILogMessageData
{
    ImmutableArray<T> GetMessageData();
}

