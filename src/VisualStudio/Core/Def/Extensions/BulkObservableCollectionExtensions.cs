// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.VisualStudio.Language.Intellisense;

namespace Microsoft.VisualStudio.LanguageServices.Extensions;

internal static class BulkObservableCollectionExtensions
{
    public static BulkOperationDisposable<T> GetBulkOperation<T>(this BulkObservableCollection<T> collection)
    {
        collection.BeginBulkOperation();
        return new BulkOperationDisposable<T>(collection);
    }

    public readonly struct BulkOperationDisposable<T>(BulkObservableCollection<T> collection)
        : IDisposable
    {
        public void Dispose()
            => collection.EndBulkOperation();
    }
}
