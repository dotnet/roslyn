// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
