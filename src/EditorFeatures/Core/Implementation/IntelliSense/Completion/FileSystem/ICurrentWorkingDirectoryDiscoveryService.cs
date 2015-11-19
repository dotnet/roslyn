// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.Implementation.IntelliSense.Completion.FileSystem
{
    internal interface ICurrentWorkingDirectoryDiscoveryService
    {
        /// <summary>
        /// Gets the full path of the current directory.
        /// </summary>
        string WorkingDirectory { get; }
    }
}
