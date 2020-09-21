// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests
{
    internal partial class Program
    {
        internal sealed class TestRunData
        {
            public int ElapsedSeconds { get; set; }
            public bool Succeeded { get; set; }
            public bool IsJenkins { get; set; }
            public bool Is32Bit { get; set; }
            public int AssemblyCount { get; set; }
            public int CacheCount { get; set; }
            public int ChunkCount { get; set; }
            public string JenkinsUrl { get; set; }
            public bool HasErrors { get; set; }
        }
    }
}
