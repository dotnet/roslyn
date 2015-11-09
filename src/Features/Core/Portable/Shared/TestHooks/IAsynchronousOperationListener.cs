// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;

namespace Microsoft.CodeAnalysis.Shared.TestHooks
{
    internal interface IAsynchronousOperationListener
    {
        // TODO(cyrusn): Remove this overload when we can simply recompile TypeScript and move them
        // to the better version below.  Right now this version is needed for binary compatibility.
        IAsyncToken BeginAsyncOperation(string name, object tag);

        IAsyncToken BeginAsyncOperation(string name, object tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
    }
}
