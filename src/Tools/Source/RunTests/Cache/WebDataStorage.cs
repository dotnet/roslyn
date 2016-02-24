// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal sealed class WebDataStorage : IDataStorage
    {
        public Task AddCachedTestResult(ContentFile conentFile, CachedTestResult testResult)
        {
            throw new NotImplementedException();
        }

        public Task<CachedTestResult?> TryGetCachedTestResult(string checksum)
        {
            throw new NotImplementedException();
        }
    }
}
