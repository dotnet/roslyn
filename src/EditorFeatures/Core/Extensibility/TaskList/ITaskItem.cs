// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor
{
    internal interface ITaskItem
    {
        string Message { get; }
        Workspace Workspace { get; }
        DocumentId DocumentId { get; }

        /// <summary>
        /// Null if path is not mapped and <see cref="OriginalFilePath"/> contains the actual path.
        /// Note that the value might be a relative path. In that case <see cref="OriginalFilePath"/> should be used
        /// as a base path for path resolution.
        /// </summary>
        string MappedFilePath { get; }

        string OriginalFilePath { get; }
        int MappedLine { get; }
        int MappedColumn { get; }
        int OriginalLine { get; }
        int OriginalColumn { get; }
    }
}
