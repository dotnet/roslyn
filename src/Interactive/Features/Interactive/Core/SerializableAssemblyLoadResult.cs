// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Microsoft.CodeAnalysis.Scripting.Hosting;

namespace Microsoft.CodeAnalysis.Interactive
{
    [Serializable]
    public struct SerializableAssemblyLoadResult
    {
        public bool IsSuccessful { get; }
        public string Path { get; }
        public string OriginalPath { get; }

        private SerializableAssemblyLoadResult(AssemblyLoadResult result)
        {
            Path = result.Path;
            OriginalPath = result.OriginalPath;
            IsSuccessful = result.IsSuccessful;
        }

        public static implicit operator SerializableAssemblyLoadResult(AssemblyLoadResult result)
        {
            return new SerializableAssemblyLoadResult(result);
        }

        public static implicit operator AssemblyLoadResult(SerializableAssemblyLoadResult result)
        {
            return new AssemblyLoadResult(result.Path, result.OriginalPath, result.IsSuccessful);
        }
    }
}
