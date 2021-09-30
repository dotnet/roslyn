// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.Shared.TestHooks;

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense
{
    internal interface IController<TModel>
    {
        void OnModelUpdated(TModel result, bool updateController);
        IAsyncToken BeginAsyncOperation(string name = "", object tag = null, [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);
        void StopModelComputation();
    }
}
