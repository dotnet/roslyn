// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// A factory for creating SourceText instances.
    /// </summary>
    public interface ITextFactoryService : IWorkspaceService
    {
        SourceText CreateText(Stream stream, CancellationToken cancellationToken = default(CancellationToken));
        SourceText CreateText(Stream stream, Encoding encoding, CancellationToken cancellationToken = default(CancellationToken));
    }
}