// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.Options
{
    internal sealed class BatchOptionsChangedEventArgs : EventArgs
    {
        public BatchOptionsChangedEventArgs(IEnumerable<OptionChangedEventArgs> optionChangedEventArgs, Workspace? sourceWorkspace)
        {
            OptionChangedEventArgs = optionChangedEventArgs;
            SourceWorkspace = sourceWorkspace;
        }

        public IEnumerable<OptionChangedEventArgs> OptionChangedEventArgs { get; }
        public Workspace? SourceWorkspace { get; }
    }
}
