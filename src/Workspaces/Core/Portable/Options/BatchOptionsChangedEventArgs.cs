// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class BatchOptionsChangedEventArgs : EventArgs
    {
        internal BatchOptionsChangedEventArgs(IEnumerable<OptionChangedEventArgs> optionChangedEventArgs, bool workspaceOptionsChanged)
        {
            OptionChangedEventArgs = optionChangedEventArgs;
            WorkspaceOptionsChanged = workspaceOptionsChanged;
        }

        public IEnumerable<OptionChangedEventArgs> OptionChangedEventArgs { get; }
        public bool WorkspaceOptionsChanged { get; }
    }
}
