// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
            if (!testMethod.Method.GetParameters().Any())
            {
                if (!testMethod.Method.IsGenericMethodDefinition)
                {
                    foreach (var supportedInstance in GetSupportedInstances(testMethod, factAttribute))
                    {
                        yield return new IdeTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), testMethod, supportedInstance);
                        yield return new IdeInstanceTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), discoveryOptions.MethodDisplayOptionsOrDefault(), CreateVisualStudioTestMethod(supportedInstance), supportedInstance);
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

        internal static ITestMethod CreateVisualStudioTestMethod(VisualStudioInstanceKey supportedInstance)
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
            return GetSupportedVersions(factAttribute, GetSettingsAttributes(testMethod).ToArray())
                .Select(version => new VisualStudioInstanceKey(version, rootSuffix));
        }

        private static string GetRootSuffix(ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            return GetRootSuffix(factAttribute, GetSettingsAttributes(testMethod).ToArray());
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
                defaultValue: VisualStudioVersion.VS2022);

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

        private static TValue GetNamedArgument<TValue>(IAttributeInfo factAttribute, IAttributeInfo[] settingsAttributes, string argumentName, Func<TValue, bool> isValidValue, TValue defaultValue)
        {
            if (TryGetNamedArgument(factAttribute, argumentName, isValidValue, out var value))
            {
                return value;
            }

            foreach (var attribute in settingsAttributes)
            {
                if (TryGetNamedArgument(attribute, argumentName, isValidValue, out value))
                {
                    return value;
                }
            }

            return defaultValue;

            static bool TryGetNamedArgument(IAttributeInfo attribute, string argumentName, Func<TValue, bool> isValidValue, out TValue value)
            {
                value = attribute.GetNamedArgument<TValue>(argumentName);
                return isValidValue(value);
            }
        }
    }
}
