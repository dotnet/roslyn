// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.Options;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.LanguageServices.Implementation.Options;
using Microsoft.VisualStudio.LanguageServices.Options;
using Microsoft.VisualStudio.Shell;
using Moq;
using Roslyn.Test.Utilities;
using Xunit;

namespace Roslyn.VisualStudio.Next.UnitTests.UnifiedSettings;

public sealed class UnifiedSettingsPackageTests
{
    [WpfTheory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task StartupQueriesTheShellAsynchronouslyAndComposesMefOffTheUIThread(bool completedQuery)
    {
        var uiThread = Environment.CurrentManagedThreadId;
        var componentModel = new Mock<IComponentModel>(MockBehavior.Strict);
        var namingService = new Mock<INamingSettingsService>(MockBehavior.Strict);
        var globalOptions = new Mock<IGlobalOptionService>(MockBehavior.Strict);
        var threadingContext = new Mock<IThreadingContext>(MockBehavior.Strict);
        componentModel.Setup(model => model.GetService<INamingSettingsService>())
            .Callback(() => Assert.NotEqual(uiThread, Environment.CurrentManagedThreadId)).Returns(namingService.Object);
        componentModel.Setup(model => model.GetService<IGlobalOptionService>())
            .Callback(() => Assert.NotEqual(uiThread, Environment.CurrentManagedThreadId)).Returns(globalOptions.Object);
        componentModel.Setup(model => model.GetService<IThreadingContext>())
            .Callback(() => Assert.NotEqual(uiThread, Environment.CurrentManagedThreadId)).Returns(threadingContext.Object);
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var serviceProvider = new Mock<IAsyncServiceProvider>(MockBehavior.Strict);
        serviceProvider.As<IServiceProvider>();
        serviceProvider.Setup(provider => provider.GetServiceAsync(typeof(SComponentModel)))
            .Returns(completedQuery ? Task.FromResult<object?>(componentModel.Object) : completion.Task);

        var initialization = UnifiedSettingsPackage.GetMefServicesAsync(serviceProvider.Object, CancellationToken.None);
        if (!completedQuery)
        {
            Assert.False(initialization.IsCompleted);
            componentModel.VerifyNoOtherCalls();
            completion.SetResult(componentModel.Object);
        }

        var services = await initialization;
        Assert.Same(componentModel.Object, services.ComponentModel);
        Assert.Same(namingService.Object, services.NamingService);
        Assert.Same(globalOptions.Object, services.GlobalOptions);
        Assert.Same(threadingContext.Object, services.ThreadingContext);
        serviceProvider.Verify(provider => provider.GetServiceAsync(typeof(SComponentModel)), Times.Once);
        serviceProvider.As<IServiceProvider>().Verify(provider => provider.GetService(It.IsAny<Type>()), Times.Never);
        componentModel.VerifyAll();
        namingService.VerifyNoOtherCalls();
        globalOptions.VerifyNoOtherCalls();
    }

    [WpfFact]
    public async Task CancellationBeforeInitializationDoesNotQueryTheShell()
    {
        var serviceProvider = new Mock<IAsyncServiceProvider>(MockBehavior.Strict);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            UnifiedSettingsPackage.GetMefServicesAsync(serviceProvider.Object, cancellation.Token));

        serviceProvider.VerifyNoOtherCalls();
    }

    [WpfFact]
    public async Task CancellationDuringTheShellQueryDoesNotComposeMef()
    {
        using var cancellation = new CancellationTokenSource();
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var componentModel = new Mock<IComponentModel>(MockBehavior.Strict);
        var serviceProvider = new Mock<IAsyncServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(provider => provider.GetServiceAsync(typeof(SComponentModel))).Returns(completion.Task);
        var initialization = UnifiedSettingsPackage.GetMefServicesAsync(serviceProvider.Object, cancellation.Token);

        cancellation.Cancel();
        completion.SetResult(componentModel.Object);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => initialization);
        componentModel.VerifyNoOtherCalls();
    }

    [WpfFact]
    public async Task ComponentModelQueryFailureIsPropagated()
    {
        var failure = new InvalidOperationException("Component model unavailable.");
        var serviceProvider = new Mock<IAsyncServiceProvider>(MockBehavior.Strict);
        serviceProvider.Setup(provider => provider.GetServiceAsync(typeof(SComponentModel)))
            .Returns(Task.FromException<object?>(failure));

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            UnifiedSettingsPackage.GetMefServicesAsync(serviceProvider.Object, CancellationToken.None));

        Assert.Same(failure, actual);
    }
}
