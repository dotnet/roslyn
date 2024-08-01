// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Roslyn.Test.Utilities;
using Xunit;
using Xunit.Sdk;

namespace Roslyn.VisualStudio.NewIntegrationTests;

[XunitTestCaseDiscoverer("Xunit.Threading.IdeFactDiscoverer", "Microsoft.VisualStudio.Extensibility.Testing.Xunit")]
internal class ConditionalIdeFactAttribute : IdeFactAttribute
{
    /// <summary>
    /// This property exists to prevent users of ConditionalFact from accidentally putting documentation
    /// in the Skip property instead of Reason. Putting it into Skip would cause the test to be unconditionally
    /// skipped vs. conditionally skipped which is the entire point of this attribute.
    /// </summary>
    [Obsolete("ConditionalIdeFactAttribute should use Reason or AlwaysSkip", error: true)]
    public new string Skip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }

    public string AlwaysSkip
    {
        get { return base.Skip; }
        set { base.Skip = value; }
    }

    public required string Reason { get; set; }

    public ConditionalIdeFactAttribute(params Type[] skipConditions)
    {
        foreach (var skipCondition in skipConditions)
        {
            var condition = (ExecutionCondition)Activator.CreateInstance(skipCondition);
            if (condition.ShouldSkip)
            {
                base.Skip = Reason ?? condition.SkipReason;
                break;
            }
        }
    }
}

public class DartLabCIOnly : ExecutionCondition
{
    public override bool ShouldSkip => Environment.GetEnvironmentVariable("inDartLab").Equals("true", StringComparison.InvariantCultureIgnoreCase);

    public override string SkipReason => "Test should only run in DartLab CI";
}
