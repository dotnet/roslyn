// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
using System.Runtime.CompilerServices;

namespace Roslyn.Test.Performance.Utilities
{
    public abstract class PerfTest : RelativeDirectory
    {
        public PerfTest([CallerFilePath] string workingFile = "") : base(workingFile) { }

        public abstract void Setup();
        public abstract void Test();
        public abstract int Iterations { get; }
        public abstract string Name { get; }
        public abstract string MeasuredProc { get; }

        public abstract bool ProvidesScenarios { get; }
        public abstract string[] GetScenarios();
    }
}
