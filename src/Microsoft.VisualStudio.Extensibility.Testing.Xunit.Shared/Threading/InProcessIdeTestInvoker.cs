// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for more information.

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
                                await asyncLifetime.InitializeAsync();
                            }
                            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                            {
                                throw ExceptionUtilities.Unreachable;
                            }
                        }

                        if (!CancellationTokenSource.IsCancellationRequested)
                        {
                            await BeforeTestMethodInvokedAsync();
                            if (!CancellationTokenSource.IsCancellationRequested && !Aggregator.HasExceptions)
                            {
                                await InvokeTestMethodAsync(testClassInstance);
                            }

                            await AfterTestMethodInvokedAsync();
                        }

                        if (asyncLifetime != null)
                        {
                            await Aggregator.RunAsync(async () =>
                            {
                                try
                                {
                                    await asyncLifetime.DisposeAsync();
                                }
                                catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
                                {
                                    throw ExceptionUtilities.Unreachable;
                                }
                            });
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

        protected override Task BeforeTestMethodInvokedAsync()
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
            return Task.CompletedTask;
#else
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
#endif
        }

        protected override async Task<decimal> InvokeTestMethodAsync(object testClassInstance)
        {
            try
            {
                return await base.InvokeTestMethodAsync(testClassInstance);
            }
            catch (Exception ex) when (DataCollectionService.LogAndPropagate(ex))
            {
                throw ExceptionUtilities.Unreachable;
            }
        }

        protected override Task AfterTestMethodInvokedAsync()
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
            return Task.CompletedTask;
#else
            var tcs = new TaskCompletionSource<bool>();
            tcs.SetResult(true);
            return tcs.Task;
#endif
        }
    }
}
