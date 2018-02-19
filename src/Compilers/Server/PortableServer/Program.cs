// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Net;
using System.Threading;

namespace Microsoft.CodeAnalysis.CompilerServer
{
    public class Program
    {
        public static void Main(string[] args) => new PortableBuildServerController().Run(args);
    }
}
