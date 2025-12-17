// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Xunit.Threading
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit.Abstractions;
    using Xunit.Harness;
    using Xunit.Sdk;

    public class InProcessIdeTestInvoker : XunitTestInvoker
    {
        private readonly Stack<BeforeAfterTestAttribute> _beforeAfterAttributesRun = new();
        private readonly IReadOnlyList<BeforeAfterTestAttribute> _beforeAfterAttributes;

        public InProcessIdeTestInvoker(ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
        {
            _beforeAfterAttributes = beforeAfterAttributes;
        }

        public new Task<decimal> RunAsync()
        {
            return Aggregator.RunAsync(async delegate
            {
                if (!CancellationTokenSource.IsCancellationRequested)
                {
                    var testClassInstance = CreateTestClass();
                    try
                    {
                        var asyncLifetime = testClassInstance as IAsyncLifetime;
                        if (asyncLifetime != null)
                        {
                            try
                            {
                                await asyncLifetime.InitializeAsync().ConfigureAwait(true);
                            }
                            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                            {
                                throw ExceptionUtilities.Unreachable;
                            }
                        }

                        if (!CancellationTokenSource.IsCancellationRequested)
                        {
                            await BeforeTestMethodInvokedAsync().ConfigureAwait(true);
                            if (!CancellationTokenSource.IsCancellationRequested && !Aggregator.HasExceptions)
                            {
                                await InvokeTestMethodAsync(testClassInstance).ConfigureAwait(true);
                            }

                            await AfterTestMethodInvokedAsync().ConfigureAwait(true);
                        }

                        if (asyncLifetime != null)
                        {
                            await Aggregator.RunAsync(async () =>
                            {
                                try
                                {
                                    await asyncLifetime.DisposeAsync().ConfigureAwait(true);
                                }
                                catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                                {
                                    throw ExceptionUtilities.Unreachable;
                                }
                            }).ConfigureAwait(true);
                        }
                    }
                    finally
                    {
                        Aggregator.Run(delegate
                        {
                            Test.DisposeTestClass(testClassInstance, MessageBus, Timer, CancellationTokenSource);
                        });
                    }
                }

                return Timer.Total;
            });
        }

        protected override object CreateTestClass()
        {
            try
            {
                return base.CreateTestClass();
            }
            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected override async Task BeforeTestMethodInvokedAsync()
        {
            foreach (var beforeAfterAttribute in _beforeAfterAttributes)
            {
                var attributeName = beforeAfterAttribute.GetType().Name;
                if (!MessageBus.QueueMessage(new BeforeTestStarting(Test, attributeName)))
                {
                    CancellationTokenSource.Cancel();
                }
                else
                {
                    try
                    {
                        Timer.Aggregate(() => beforeAfterAttribute.Before(TestMethod));
                        _beforeAfterAttributesRun.Push(beforeAfterAttribute);
                    }
                    catch (Exception ex) when (DataCollectionService.LogAndCatch(ex))
                    {
                        Aggregator.Add(ex);
                        break;
                    }
                    finally
                    {
                        if (!MessageBus.QueueMessage(new BeforeTestFinished(Test, attributeName)))
                        {
                            CancellationTokenSource.Cancel();
                        }
                    }
                }

                if (CancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }
            }

#if NET472
#else
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
#endif
        }

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            var oldSyncContext = SynchronizationContext.Current;

            try
            {
                var asyncSyncContext = new AsyncTestSyncContext(oldSyncContext);
                SynchronizationContext.SetSynchronizationContext(asyncSyncContext);

                await Aggregator.RunAsync(
                    () => Timer.AggregateAsync(
                        async () =>
                        {
                            var parameterCount = TestMethod.GetParameters().Length;
                            var valueCount = TestMethodArguments == null ? 0 : TestMethodArguments.Length;
                            if (parameterCount != valueCount)
                            {
                                Aggregator.Add(
                                    new InvalidOperationException(
                                        $"The test method expected {parameterCount} parameter value{(parameterCount == 1 ? string.Empty : "s")}, but {valueCount} parameter value{(valueCount == 1 ? string.Empty : "s")} {(valueCount == 1 ? "was" : "were")} provided."));
                            }
                            else
                            {
                                var result = CallTestMethod(testClassInstance);
                                var task = GetTaskFromResult(result);
                                if (task != null)
                                {
                                    if (task.Status == TaskStatus.Created)
                                    {
                                        throw new InvalidOperationException("Test method returned a non-started Task (tasks must be started before being returned)");
                                    }

                                    try
                                    {
                                        await task.ConfigureAwait(true);
                                    }
                                    catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                                    {
                                        throw ExceptionUtilities.Unreachable;
                                    }
                                }
                                else
                                {
                                    var ex = await asyncSyncContext.WaitForCompletionAsync().ConfigureAwait(true);
                                    if (ex != null)
                                    {
                                        DataCollectionService.TryLog(ex);
                                        Aggregator.Add(ex);
                                    }
                                }
                            }
                        })).ConfigureAwait(true);
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldSyncContext);
            }

            return Timer.Total;
        }

        protected override object CallTestMethod(object testClassInstance)
        {
            try
            {
                return base.CallTestMethod(testClassInstance);
            }
            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected override async Task AfterTestMethodInvokedAsync()
        {
            foreach (var beforeAfterAttribute in _beforeAfterAttributesRun)
            {
                var attributeName = beforeAfterAttribute.GetType().Name;
                if (!MessageBus.QueueMessage(new AfterTestStarting(Test, attributeName)))
                {
                    CancellationTokenSource.Cancel();
                }

                Aggregator.Run(() =>
                {
                    Timer.Aggregate(() =>
                    {
                        try
                        {
                            beforeAfterAttribute.After(TestMethod);
                        }
                        catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                    });
                });

                if (!MessageBus.QueueMessage(new AfterTestFinished(Test, attributeName)))
                {
                    CancellationTokenSource.Cancel();
                }
            }

#if NET472
#else
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
#endif
        }
    }
}
