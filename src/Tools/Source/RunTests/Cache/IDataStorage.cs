// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunTests.Cache
{
    internal interface IDataStorage
    {
        bool TryGetTestResult(string checksum, out TestResult testResult);

        void AddTestResult(ContentFile conentFile, TestResult testResult);
    }
}
