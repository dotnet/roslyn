// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.CodeAnalysis.Rename
{
    /// <summary>
    /// Represent the result of rename engine. It could either be a succesful <see cref="ConflictResolution"/> or failed <see cref="FailedConflictResolution"/>
    /// </summary>
    internal interface IConflictResolution
    {
        public Task<SerializableConflictResolution> DehydrateAsync(CancellationToken cancellationToken);
    }
}
