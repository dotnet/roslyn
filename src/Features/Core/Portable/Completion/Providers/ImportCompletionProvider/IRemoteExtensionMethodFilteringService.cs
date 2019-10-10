// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#nullable enable

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Completion.Providers
{
    internal interface IRemoteExtensionMethodFilteringService
    {
        /// <summary>
        /// Returns a mapping of "fully qualified name of containing type" => "contained extension method names"
        /// </summary>
        Task<IEnumerable<(string, IEnumerable<string>)>> GetPossibleExtensionMethodMatchesAsync(
            ProjectId projectId,
            string[] targetTypeNames,
            bool loadOnly,
            CancellationToken cancellationToken);
    }
}
