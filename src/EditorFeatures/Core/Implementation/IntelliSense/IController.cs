// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal interface IController<TModel>
    {
        void OnModelUpdated(TModel result);
        IAsyncToken BeginAsyncOperation(string name= "", object tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber]int lineNumber = 0);
        void StopModelComputation();
    }
}
