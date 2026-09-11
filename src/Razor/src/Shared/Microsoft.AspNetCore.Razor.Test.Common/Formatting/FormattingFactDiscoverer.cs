// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

extern alias XunitV3;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using XunitV3::Xunit.Sdk;
using XunitV3::Xunit.v3;

namespace Microsoft.AspNetCore.Razor.Test.Common;

public sealed class FormattingFactDiscoverer : FactDiscoverer
{
    public override ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
        ITestFrameworkDiscoveryOptions discoveryOptions,
        IXunitTestMethod testMethod,
        IFactAttribute factAttribute)
    {
        var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);
        var traits = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var (name, values) in testMethod.Traits)
        {
            traits.Add(name, new HashSet<string>(values, StringComparer.OrdinalIgnoreCase));
        }

        return new(CreateTestCases(details, traits));
    }

    internal static IReadOnlyCollection<IXunitTestCase> CreateTestCases(
        (string TestCaseDisplayName, bool Explicit, Type[]? SkipExceptions, string? SkipReason, Type? SkipType, string? SkipUnless, string? SkipWhen, string? SourceFilePath, int? SourceLineNumber, int Timeout, string UniqueID, IXunitTestMethod ResolvedTestMethod) details,
        Dictionary<string, HashSet<string>> traits,
        object?[]? testMethodArguments = null)
    {
        return
        [
            CreateTestCase(shouldFlipLineEndings: false),
            CreateTestCase(shouldFlipLineEndings: true),
        ];

        FormattingTestCase CreateTestCase(bool shouldFlipLineEndings)
        {
            var suffix = shouldFlipLineEndings ? " (LF)" : " (CRLF)";
            var uniqueIdSuffix = shouldFlipLineEndings ? "lf" : "crlf";

            return new FormattingTestCase(
                shouldFlipLineEndings,
                details.ResolvedTestMethod,
                details.TestCaseDisplayName + suffix,
                details.UniqueID + uniqueIdSuffix,
                details.Explicit,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                new Dictionary<string, HashSet<string>>(traits, StringComparer.OrdinalIgnoreCase),
                testMethodArguments,
                details.SourceFilePath,
                details.SourceLineNumber,
                details.Timeout);
        }
    }
}
