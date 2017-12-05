// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.VisualStudio.Text.UI.Commanding;
using Microsoft.VisualStudio.Text.UI.Commanding.Commands;
using Microsoft.VisualStudio.Utilities;

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ILegacyCommandHandlerService
    {
        CommandState GetCommandState<T>(IContentType contentType, T args, Func<CommandState> lastHandler = null) where T : CommandArgs;
        void Execute<T>(IContentType contentType, T args, Action lastHandler = null) where T : CommandArgs;
    }
}
