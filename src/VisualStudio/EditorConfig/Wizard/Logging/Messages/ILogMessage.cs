// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;

namespace Microsoft.VisualStudio.Templates.Editorconfig.Wizard.Logging.Messages;

public interface ILogMessage<T> where T : ILogMessageData
{
    ImmutableArray<T> GetMessageData();
}
