// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Options
{
    /// <summary>
    /// Exportable by a host to specify the save and restore behavior for a particular set of
    /// values.
    /// </summary>
    internal interface IOptionPersister
    {
        Task PrefetchAsync(CancellationToken cancelaltionToken);
        bool TryFetch(OptionKey optionKey, out object value);
        bool TryPersist(OptionKey optionKey, object value);
    }
}
