// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Test.Utilities
{
    public class ConditionalWpfFactAttribute : WpfFactAttribute
    {
        public ConditionalWpfFactAttribute(Type skipCondition)
        {
            var condition = Activator.CreateInstance(skipCondition) as ExecutionCondition;
            if (condition.ShouldSkip)
            {
                Skip = condition.SkipReason;
            }
        }
    }
}
