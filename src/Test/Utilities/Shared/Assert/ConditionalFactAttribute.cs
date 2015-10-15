// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Xunit;

namespace Roslyn.Test.Utilities
{
    public class ConditionalFactAttribute : FactAttribute
    {
        public ConditionalFactAttribute(Type skipCondition)
        {
            ExecutionCondition condition = (ExecutionCondition)Activator.CreateInstance(skipCondition);
            if (condition.ShouldSkip)
            {
                Skip = condition.SkipReason;
            }
        }
    }

    public abstract class ExecutionCondition
    {
        public abstract bool ShouldSkip { get; }
        public abstract string SkipReason { get; }
    }

    public class x86 : ExecutionCondition
    {
        public override bool ShouldSkip { get { return IntPtr.Size != 4; } }

        public override string SkipReason { get { return "Target platform is not x86"; } }
    }
}
