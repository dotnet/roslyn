﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// This service allows you to access temporary storage.
    /// </summary>
    internal interface ITemporaryStorageService2 : ITemporaryStorageService
    {
        /// <summary>
        /// Attach to existing <see cref="ITemporaryStreamStorage"/> with given name.
        /// </summary>
        ITemporaryStreamStorage AttachTemporaryStreamStorage(string storageName, long offset, long size, CancellationToken cancellationToken = default);

        /// <summary>
        /// Attach to existing <see cref="ITemporaryTextStorage"/> with given name.
        /// </summary>
        ITemporaryTextStorage AttachTemporaryTextStorage(string storageName, long offset, long size, Encoding encoding, CancellationToken cancellationToken = default);
    }
}
