// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    internal sealed class InteractiveAssemblyLoaderException : NotSupportedException
    {
        internal InteractiveAssemblyLoaderException(string message)
            : base(message)
        {
        }
    }
}
