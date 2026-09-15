// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Runtime.CompilerServices;

namespace Roslyn.Test.Utilities;

public class ConditionalWpfTheoryAttribute : WpfTheoryAttribute
{
    public ConditionalWpfTheoryAttribute(
        Type skipCondition,
        [CallerFilePath] string sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        var condition = Activator.CreateInstance(skipCondition) as ExecutionCondition;
        if (condition.ShouldSkip)
        {
            Skip = condition.SkipReason;
        }
    }
}
