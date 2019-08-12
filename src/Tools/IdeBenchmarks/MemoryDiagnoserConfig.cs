// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;

namespace IdeBenchmarks
{
    public class MemoryDiagnoserConfig : ManualConfig
    {
        public MemoryDiagnoserConfig()
        {
            Add(MemoryDiagnoser.Default);
        }
    }
}
