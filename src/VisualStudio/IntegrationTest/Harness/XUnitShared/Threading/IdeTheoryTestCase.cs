// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Harness;
    using Xunit.Sdk;
    using Xunit.v3;

    public sealed class IdeTheoryTestCase : IdeTestCaseBase
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Called by the deserializer; should only be called by deriving classes for deserialization purposes", error: true)]
        public IdeTheoryTestCase()
        {
        }

        public IdeTheoryTestCase(
            IXunitTestMethod testMethod,
            string testCaseDisplayName,
            string uniqueID,
            bool @explicit,
            Type[]? skipExceptions,
            string? skipReason,
            Type? skipType,
            string? skipUnless,
            string? skipWhen,
            Dictionary<string, HashSet<string>> traits,
            bool skipTestWithoutData,
            string? sourceFilePath,
            int? sourceLineNumber,
            int? timeout,
            VisualStudioInstanceKey visualStudioInstanceKey)
            : base(testMethod, testCaseDisplayName, uniqueID, @explicit, skipExceptions, skipReason, skipType, skipUnless, skipWhen, traits, testMethodArguments: null, sourceFilePath, sourceLineNumber, timeout, visualStudioInstanceKey)
        {
            SkipTestWithoutData = skipTestWithoutData;
        }

        public bool SkipTestWithoutData { get; private set; }

        public override async ValueTask<IReadOnlyCollection<IXunitTest>> CreateTests()
        {
            var testIndex = 0;
            var result = new List<IXunitTest>();

            foreach (var dataAttribute in TestMethod.DataAttributes)
            {
                var data = await dataAttribute.GetData(TestMethod.Method, DisposalTracker);
                if (data is null)
                {
                    throw new InvalidOperationException(
                        string.Format(
                            CultureInfo.CurrentCulture,
                            "Test data returned null for {0}.{1}. Make sure it is statically initialized before this test method is called.",
                            TestMethod.TestClass.TestClassName,
                            TestMethod.MethodName));
                }

                foreach (var dataRow in data)
                {
                    var dataRowData = dataRow.GetData();
                    DisposalTracker.AddRange(dataRowData);

                    var testMethod = TestMethod;
                    var resolvedTypes = testMethod.ResolveGenericTypes(dataRowData);
                    if (resolvedTypes is not null)
                        testMethod = new XunitTestMethod(testMethod.TestClass, testMethod.MakeGenericMethod(resolvedTypes), dataRowData);

                    var convertedDataRow = testMethod.ResolveMethodArguments(dataRowData);
                    var baseDisplayName = dataRow.TestDisplayName ?? dataAttribute.TestDisplayName ?? TestCaseDisplayName;
                    var theoryDisplayName = testMethod.GetDisplayName(baseDisplayName, dataRow.Label, convertedDataRow, resolvedTypes);
                    var traits = TestIntrospectionHelper.GetTraits(testMethod, dataRow)
                        .ToDictionary(static pair => pair.Key, static pair => (IReadOnlyCollection<string>)pair.Value, StringComparer.OrdinalIgnoreCase);
                    var timeout = dataRow.Timeout ?? dataAttribute.Timeout ?? Timeout;
                    var (skipReason, skipType, skipUnless, skipWhen) = (dataRow.Skip, dataAttribute.Skip) switch
                    {
                        (null, null) => (SkipReason, SkipType, SkipUnless, SkipWhen),
                        (null, _) => (dataAttribute.Skip, dataAttribute.SkipType, dataAttribute.SkipUnless, dataAttribute.SkipWhen),
                        _ => (dataRow.Skip, dataRow.SkipType, dataRow.SkipUnless, dataRow.SkipWhen),
                    };

                    result.Add(new XunitTest(
                        this,
                        testMethod,
                        dataRow.Explicit,
                        skipReason,
                        skipType,
                        skipUnless,
                        skipWhen,
                        theoryDisplayName,
                        testIndex++,
                        traits,
                        timeout,
                        convertedDataRow,
                        dataRow.Label,
                        dataRow.DisableParallelization ?? dataAttribute.DisableParallelization));
                }
            }

            if (result.Count == 0)
            {
                var message = string.Format(CultureInfo.CurrentCulture, "No data found for {0}.{1}", TestMethod.TestClass.TestClassName, TestMethod.MethodName);
                if (SkipTestWithoutData)
                    throw new InvalidOperationException(DynamicSkipToken.Value + message);

                throw new InvalidOperationException(message);
            }

            return result;
        }

        public override Task<RunSummary> RunAsync(IMessageSink diagnosticMessageSink, IMessageBus messageBus, object[] constructorArguments, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
        {
            TestCaseRunner<IXunitTestCase> runner;
            if (!string.IsNullOrEmpty(SkipReason))
            {
                // Use XunitTheoryTestCaseRunner so the skip gets reported without trying to open VS
                runner = new XunitTheoryTestCaseRunner(this, DisplayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource);
            }
            else
            {
                runner = new IdeTheoryTestCaseRunner(SharedData, VisualStudioInstanceKey, this, DisplayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource);
            }

            return runner.RunAsync();
        }

        protected override void Serialize(IXunitSerializationInfo data)
        {
            base.Serialize(data);
            data.AddValue(nameof(SkipTestWithoutData), SkipTestWithoutData);
        }

        protected override void Deserialize(IXunitSerializationInfo data)
        {
            base.Deserialize(data);
            SkipTestWithoutData = data.GetValue<bool>(nameof(SkipTestWithoutData));
        }
    }
}
