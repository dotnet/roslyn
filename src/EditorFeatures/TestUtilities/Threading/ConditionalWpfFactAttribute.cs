// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

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
