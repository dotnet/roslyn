// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using Microsoft.CodeAnalysis.Test.Utilities;
using Microsoft.Win32;
using Xunit;
using Roslyn.Utilities;

namespace Roslyn.Test.Utilities
{
    public class WindowsOnly : ExecutionCondition
    {
        public override bool ShouldSkip { get { return Path.DirectorySeparatorChar != '\\'; } }

        public override string SkipReason { get { return "Test not supported on Mono"; } }
    }

    public class UnixLikeOnly : ExecutionCondition
    {
        public override bool ShouldSkip { get { return !PathUtilities.IsUnixLikePlatform; } }

        public override string SkipReason { get { return "Test not supported on Windows"; } }
    }

    public class NotMonoOnly : ExecutionCondition
    {
        public override bool ShouldSkip { get { return MonoHelpers.IsRunningOnMono(); } }

        public override string SkipReason { get { return "Test not supported on Mono"; } }
    }
}
