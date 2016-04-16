// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.CompilerServer.UnitTests
{
    internal class RunKeepAliveTests : ExecutionCondition
    {
        public override bool ShouldSkip => Environment.GetEnvironmentVariable("RunKeepAliveTests") == null;

        public override string SkipReason { get; } = "RunKeepAliveTests environment variable not set";
    }
}
