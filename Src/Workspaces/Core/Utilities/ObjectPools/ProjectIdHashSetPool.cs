using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using Microsoft.CodeAnalysis;

namespace Roslyn.Utilities
{
    internal class ProjectIdHashSetPool : ObjectPool<HashSet<ProjectId>>
    {
        public static readonly ProjectIdHashSetPool Instance = new ProjectIdHashSetPool();

        private ProjectIdHashSetPool() : base(() => new HashSet<ProjectId>(), size: 1)
        {
        }
    }
}