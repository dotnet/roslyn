// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    public class IdeFactDiscoverer : IXunitTestCaseDiscoverer
    {
        public ValueTask<IReadOnlyCollection<IXunitTestCase>> Discover(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            IFactAttribute factAttribute)
        {
            if (discoveryOptions is null)
                throw new ArgumentNullException(nameof(discoveryOptions));
            if (testMethod is null)
                throw new ArgumentNullException(nameof(testMethod));
            if (factAttribute is null)
                throw new ArgumentNullException(nameof(factAttribute));

            var testCases = new List<IXunitTestCase>();

            if (testMethod.Parameters.Count != 0)
            {
                testCases.Add(CreateErrorTestCase(discoveryOptions, testMethod, factAttribute, "[IdeFact] methods are not allowed to have parameters. Did you mean to use [IdeTheory]?"));
            }
            else if (testMethod.IsGenericMethodDefinition)
            {
                testCases.Add(CreateErrorTestCase(discoveryOptions, testMethod, factAttribute, "[IdeFact] methods are not allowed to be generic."));
            }
            else
            {
                var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);
                var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow: null);
                foreach (var supportedInstance in GetSupportedInstances(testMethod, factAttribute))
                {
                    testCases.Add(CreateTestCase(details, supportedInstance, traits));
                    AddInstanceTestCase(discoveryOptions, supportedInstance, testCases);
                }
            }

            return new ValueTask<IReadOnlyCollection<IXunitTestCase>>(testCases);
        }

        internal static IXunitTestMethod CreateVisualStudioTestMethod()
        {
            var testAssembly = new XunitTestAssembly(typeof(Instances).Assembly, configFilePath: null);
            var testCollection = new XunitTestCollection(testAssembly, collectionDefinition: null, disableParallelization: true, nameof(Instances));
            var testClass = new XunitTestClass(typeof(Instances), testCollection);
            var testMethod = typeof(Instances).GetMethod(nameof(Instances.VisualStudio))!;
            return new XunitTestMethod(testClass, testMethod, testMethodArguments: Array.Empty<object?>());
        }

        internal static IEnumerable<VisualStudioInstanceKey> GetSupportedInstances(IXunitTestMethod testMethod, IFactAttribute factAttribute)
        {
            var settingsAttribute = GetSettingsAttribute(factAttribute);
            var settingsAttributes = GetSettingsAttributes(testMethod).ToArray();
            var rootSuffix = GetRootSuffix(settingsAttribute, settingsAttributes);
            var maxAttempts = GetMaxAttempts(settingsAttribute, settingsAttributes);
            var environmentVariables = GetEnvironmentVariables(settingsAttribute, settingsAttributes);
            return GetSupportedVersions(settingsAttribute, settingsAttributes)
                .Select(version => new VisualStudioInstanceKey(version, rootSuffix, maxAttempts, environmentVariables));
        }

        internal static void AddInstanceTestCase(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            VisualStudioInstanceKey supportedInstance,
            List<IXunitTestCase> testCases)
        {
            if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, supportedInstance) is { } instanceTestCase)
            {
                testCases.Add(instanceTestCase);
            }
        }

        internal static IdeTestCase CreateTestCase(
            (string TestCaseDisplayName, bool Explicit, Type[]? SkipExceptions, string? SkipReason, Type? SkipType, string? SkipUnless, string? SkipWhen, string? SourceFilePath, int? SourceLineNumber, int Timeout, string UniqueID, IXunitTestMethod ResolvedTestMethod) details,
            VisualStudioInstanceKey supportedInstance,
            Dictionary<string, HashSet<string>>? traits = null,
            object?[]? testMethodArguments = null)
        {
            return new IdeTestCase(
                details.ResolvedTestMethod,
                details.TestCaseDisplayName,
                details.UniqueID,
                details.Explicit,
                details.SkipExceptions,
                details.SkipReason,
                details.SkipType,
                details.SkipUnless,
                details.SkipWhen,
                traits,
                testMethodArguments,
                details.SourceFilePath,
                details.SourceLineNumber,
                details.Timeout,
                supportedInstance);
        }

        private static ExecutionErrorTestCase CreateErrorTestCase(
            ITestFrameworkDiscoveryOptions discoveryOptions,
            IXunitTestMethod testMethod,
            IFactAttribute factAttribute,
            string message)
        {
            var details = TestIntrospectionHelper.GetTestCaseDetails(discoveryOptions, testMethod, factAttribute);
            return new ExecutionErrorTestCase(details.ResolvedTestMethod, details.TestCaseDisplayName, details.UniqueID, details.SourceFilePath, details.SourceLineNumber, message);
        }

        private static IIdeSettingsAttribute GetSettingsAttribute(IFactAttribute factAttribute)
        {
            if (factAttribute is not IIdeSettingsAttribute settingsAttribute)
                throw new ArgumentException("The fact attribute must implement IIdeSettingsAttribute.", nameof(factAttribute));

            return settingsAttribute;
        }

        private static IEnumerable<IIdeSettingsAttribute> GetSettingsAttributes(IXunitTestMethod testMethod)
        {
            return testMethod.Method.GetCustomAttributes(typeof(IdeSettingsAttribute), inherit: true)
                .Concat(testMethod.TestClass.Class.GetCustomAttributes(typeof(IdeSettingsAttribute), inherit: true))
                .Cast<IIdeSettingsAttribute>();
        }

        private static IEnumerable<VisualStudioVersion> GetSupportedVersions(IIdeSettingsAttribute factAttribute, IIdeSettingsAttribute[] settingsAttributes)
        {
            var minVersion = GetValue(
                factAttribute,
                settingsAttributes,
                static attribute => attribute.MinVersion,
                static value => value is not VisualStudioVersion.Unspecified,
                VisualStudioVersion.VS2012);

            var maxVersion = GetValue(
                factAttribute,
                settingsAttributes,
                static attribute => attribute.MaxVersion,
                static value => value is not VisualStudioVersion.Unspecified,
                VisualStudioVersion.VS18);

            for (var version = minVersion; version <= maxVersion; version++)
            {
#if MERGED_PIA
                if (version >= VisualStudioVersion.VS2012 && version < VisualStudioVersion.VS2022)
                {
                    continue;
                }
#else
                if (version >= VisualStudioVersion.VS2022)
                {
                    continue;
                }
#endif

                yield return version;
            }
        }

        private static string GetRootSuffix(IIdeSettingsAttribute factAttribute, IIdeSettingsAttribute[] settingsAttributes)
        {
            return GetValue(
                factAttribute,
                settingsAttributes,
                static attribute => attribute.RootSuffix,
                static value => value is not null,
                "Exp");
        }

        private static int GetMaxAttempts(IIdeSettingsAttribute factAttribute, IIdeSettingsAttribute[] settingsAttributes)
        {
            return GetValue(
                factAttribute,
                settingsAttributes,
                static attribute => attribute.MaxAttempts,
                static value => value > 0,
                1);
        }

        private static string[] GetEnvironmentVariables(IIdeSettingsAttribute factAttribute, IIdeSettingsAttribute[] settingsAttributes)
        {
            var result = Array.Empty<string>();
            for (var i = settingsAttributes.Length - 1; i >= 0; i--)
            {
                if (settingsAttributes[i].EnvironmentVariables.Length > 0)
                    result = MergeEnvironmentVariables(result, settingsAttributes[i].EnvironmentVariables);
            }

            return factAttribute.EnvironmentVariables.Length > 0
                ? MergeEnvironmentVariables(result, factAttribute.EnvironmentVariables)
                : result;
        }

        private static string[] MergeEnvironmentVariables(string[] inherited, string[] current)
        {
            if (inherited.Length == 0)
            {
                return current;
            }
            else if (current.Length == 0)
            {
                return inherited;
            }

            var set = new HashSet<string>(KeyOnlyComparerIgnoreCase.Instance);
            foreach (var value in current)
            {
                set.Add(value);
            }

            foreach (var value in inherited)
            {
                set.Add(value);
            }

            return set.ToArray();
        }

        private static TValue GetValue<TValue>(IIdeSettingsAttribute factAttribute, IIdeSettingsAttribute[] settingsAttributes, Func<IIdeSettingsAttribute, TValue> getValue, Func<TValue, bool> isValidValue, TValue defaultValue)
        {
            var value = getValue(factAttribute);
            if (isValidValue(value))
                return value;

            foreach (var attribute in settingsAttributes)
            {
                value = getValue(attribute);
                if (isValidValue(value))
                    return value;
            }

            return defaultValue;
        }

        private sealed class KeyOnlyComparerIgnoreCase : IEqualityComparer<string?>
        {
            public static readonly KeyOnlyComparerIgnoreCase Instance = new();

            private KeyOnlyComparerIgnoreCase()
            {
            }

            public bool Equals(string? x, string? y)
            {
                if (x is null)
                {
                    return y is null;
                }
                else if (y is null)
                {
                    return false;
                }

                return StringComparer.OrdinalIgnoreCase.Equals(GetKey(x), GetKey(y));
            }

            public int GetHashCode(string? obj)
            {
                if (obj is null)
                {
                    return 0;
                }

                return StringComparer.OrdinalIgnoreCase.GetHashCode(GetKey(obj));
            }

            private static string GetKey(string s)
            {
                var keyEnd = s.IndexOf('=');
                if (keyEnd < 0)
                {
                    return s;
                }

                return s.Substring(0, keyEnd);
            }
        }
    }
}
