using System;
using Roslyn.Test.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks.UnitTests
{
    internal class RunKeepAliveTests : ExecutionCondition
    {
        public override bool ShouldSkip => Environment.GetEnvironmentVariable("RunKeepAliveTests") == null;

        public override string SkipReason { get; } = "RunKeepAliveTests environment variable not set";
    }
}
