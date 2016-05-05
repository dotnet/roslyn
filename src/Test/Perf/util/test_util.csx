// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

#r "System.IO.Compression.FileSystem"
#r "../../Roslyn.Test.Performance.Utilities.dll"

using System.IO;
using System.IO.Compression;
using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Net;
using System.Globalization;
using Roslyn.Test.Performance.Utilities;

static PerfTest[] resultTests = null;

static void TestThisPlease(params PerfTest[] tests)
{
    if (IsRunFromRunner()) 
    {
        resultTests = tests;
    }
    else 
    {
        foreach (var test in tests) 
        {
            test.Setup();
            for (int i = 0; i < test.Iterations; i++) 
            {
                test.Test();
            }
        }
    }
}

