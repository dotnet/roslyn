// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.Sdk;

    public class IdeFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private readonly IMessageSink _diagnosticMessageSink;

        public IdeFactDiscoverer(IMessageSink diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            if (testMethod is null)
            {
                throw new ArgumentNullException(nameof(testMethod));
            }

            if (!testMethod.Method.GetParameters().Any())
            {
                if (!testMethod.Method.IsGenericMethodDefinition)
                {
                    foreach (var supportedInstance in GetSupportedInstances(testMethod, factAttribute))
                    {
                        yield return new IdeTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance);
                        if (IdeInstanceTestCase.TryCreateNewInstanceForFramework(discoveryOptions, _diagnosticMessageSink, supportedInstance) is { } instanceTestCase)
                        {
                            yield return instanceTestCase;
                        }
                    }
                }
                else
                {
                    yield return new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, "[IdeFact] methods are not allowed to be generic.");
                }
            }
            else
            {
                yield return new ExecutionErrorTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, "[IdeFact] methods are not allowed to have parameters. Did you mean to use [IdeTheory]?");
            }
        }

        internal static ITestMethod CreateVisualStudioTestMethod()
        {
            var testAssembly = new TestAssembly(new ReflectionAssemblyInfo(typeof(Instances).Assembly));
            var testCollection = new TestCollection(testAssembly, collectionDefinition: null, nameof(Instances));
            var testClass = new TestClass(testCollection, new ReflectionTypeInfo(typeof(Instances)));
            var testMethod = testClass.Class.GetMethods(false).Single(method => method.Name == nameof(Instances.VisualStudio));
            return new TestMethod(testClass, testMethod);
        }

        internal static IEnumerable<VisualStudioInstanceKey> GetSupportedInstances(ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var rootSuffix = GetRootSuffix(testMethod, factAttribute);
            var maxAttempts = GetMaxAttempts(testMethod, factAttribute);
            var environmentVariables = GetEnvironmentVariables(testMethod, factAttribute);
            return GetSupportedVersions(factAttribute, GetSettingsAttributes(testMethod).ToArray())
                .Select(version => new VisualStudioInstanceKey(version, rootSuffix, maxAttempts, environmentVariables));
        }

        private static string GetRootSuffix(ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            return GetRootSuffix(factAttribute, GetSettingsAttributes(testMethod).ToArray());
        }

        private static int GetMaxAttempts(ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            return GetMaxAttempts(factAttribute, GetSettingsAttributes(testMethod).ToArray());
        }

        private static string[] GetEnvironmentVariables(ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            return GetEnvironmentVariables(factAttribute, GetSettingsAttributes(testMethod).ToArray());
        }

        private static IEnumerable<IAttributeInfo> GetSettingsAttributes(ITestMethod testMethod)
        {
            foreach (var attributeData in testMethod.Method.GetCustomAttributes(typeof(IdeSettingsAttribute)))
            {
                yield return attributeData;
            }

            foreach (var attributeData in testMethod.TestClass.Class.GetCustomAttributes(typeof(IdeSettingsAttribute)))
            {
                yield return attributeData;
            }
        }

        private static IEnumerable<VisualStudioVersion> GetSupportedVersions(IAttributeInfo factAttribute, IAttributeInfo[] settingsAttributes)
        {
            var minVersion = GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.MinVersion),
                static value => value is not VisualStudioVersion.Unspecified,
                defaultValue: VisualStudioVersion.VS2012);

            var maxVersion = GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.MaxVersion),
                static value => value is not VisualStudioVersion.Unspecified,
                defaultValue: VisualStudioVersion.VS18);

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

        private static string GetRootSuffix(IAttributeInfo factAttribute, IAttributeInfo[] settingsAttributes)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.RootSuffix),
                static value => value is not null,
                defaultValue: "Exp");
        }

        private static int GetMaxAttempts(IAttributeInfo factAttribute, IAttributeInfo[] settingsAttributes)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.MaxAttempts),
                static value => value > 0,
                defaultValue: 1);
        }

        private static string[] GetEnvironmentVariables(IAttributeInfo factAttribute, IAttributeInfo[] settingsAttributes)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                nameof(IIdeSettingsAttribute.EnvironmentVariables),
                static value => value != null,
                (inherited, current) => MergeEnvironmentVariables(inherited, current),
                defaultValue: new string[0]);
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

        private static TValue GetNamedArgument<TValue>(IAttributeInfo factAttribute, IAttributeInfo[] settingsAttributes, string argumentName, Func<TValue, bool> isValidValue, TValue defaultValue)
        {
            return GetNamedArgument(
                factAttribute,
                settingsAttributes,
                argumentName,
                isValidValue,
                merge: null,
                defaultValue);
        }

        private static TValue GetNamedArgument<TValue>(IAttributeInfo factAttribute, IAttributeInfo[] settingsAttributes, string argumentName, Func<TValue, bool> isValidValue, Func<TValue, TValue, TValue>? merge, TValue defaultValue)
        {
            StrongBox<TValue>? result = null;
            if (TryGetNamedArgument(factAttribute, argumentName, isValidValue, out var value))
            {
                if (merge is null)
                {
                    return value;
                }

                result = new StrongBox<TValue>(value);
            }

            foreach (var attribute in settingsAttributes)
            {
                if (TryGetNamedArgument(attribute, argumentName, isValidValue, out value))
                {
                    if (merge is null)
                    {
                        return value;
                    }
                    else if (result is null)
                    {
                        result = new StrongBox<TValue>(value);
                    }
                    else
                    {
                        result.Value = merge(value, result.Value);
                    }

                    return value;
                }
            }

            if (result is not null)
            {
                return result.Value;
            }

            return defaultValue;

            static bool TryGetNamedArgument(IAttributeInfo attribute, string argumentName, Func<TValue, bool> isValidValue, out TValue value)
            {
                value = attribute.GetNamedArgument<TValue>(argumentName);
                return isValidValue(value);
            }
        }

        private class KeyOnlyComparerIgnoreCase : IEqualityComparer<string?>
        {
            public static readonly KeyOnlyComparerIgnoreCase Instance = new KeyOnlyComparerIgnoreCase();

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
