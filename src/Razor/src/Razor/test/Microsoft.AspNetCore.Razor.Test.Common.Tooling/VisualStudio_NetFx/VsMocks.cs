// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.Primitives;
using System.Threading;
using Microsoft.CodeAnalysis.Razor;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Razor;
using Microsoft.VisualStudio.Razor.LanguageClient;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Utilities;
using Moq;

namespace Microsoft.AspNetCore.Razor.Test.Common.VisualStudio;

internal static class VsMocks
{
    public static ITextBuffer CreateTextBuffer(bool core)
        => CreateTextBuffer(core ? ContentTypes.RazorCore : ContentTypes.NonRazor);

    public static ITextBuffer CreateTextBuffer(PropertyCollection? properties = null)
    {
        properties ??= new PropertyCollection();

        return StrictMock.Of<ITextBuffer>(b =>
            b.Properties == properties);
    }

    public static ITextBuffer CreateTextBuffer(IContentType contentType, PropertyCollection? properties = null)
    {
        var buffer = CreateTextBuffer(properties);

        Mock.Get(buffer)
            .SetupGet(x => x.ContentType)
            .Returns(contentType);

        return buffer;
    }

    internal static class ContentTypes
    {
        public static readonly IContentType LegacyRazorCore = Create(RazorConstants.LegacyCoreContentType);
        public static readonly IContentType RazorCore = Create(RazorLanguage.CoreContentType);
        public static readonly IContentType RazorLSP = Create(RazorConstants.RazorLSPContentTypeName);
        public static readonly IContentType NonRazor = StrictMock.Of<IContentType>(c => c.IsOfType(It.IsAny<string>()) == false);
        public static readonly IContentType CSharp = CreateCSharp();

        public static IContentType Create(params string[] types)
        {
            var mock = new StrictMock<IContentType>();
            mock.Setup(x => x.IsOfType(It.IsAny<string>()))
                .Returns((string type) => Array.IndexOf(types, type) >= 0);

            return mock.Object;
        }

        private static IContentType CreateCSharp()
        {
            var contentType = Create(RazorLSPConstants.CSharpContentTypeName);
            var mock = Mock.Get(contentType);

            mock.SetupGet(x => x.TypeName)
                .Returns(RazorLSPConstants.CSharpContentTypeName);
            mock.SetupGet(x => x.DisplayName)
                .Returns(RazorLSPConstants.CSharpContentTypeName);

            return contentType;
        }
    }

    public static IServiceProvider CreateServiceProvider(Action<IServiceProviderBuilder>? configure = null)
    {
        var builder = new ServiceProviderBuilder();
        configure?.Invoke(builder);
        return builder.Mock.Object;
    }

    public interface IServiceProviderBuilder
    {
        void AddService<T>(T? serviceInstance)
            where T : class;
        void AddService<T>(Func<T?> getServiceCallback)
            where T : class;
        void AddService<T>(object? serviceInstance)
            where T : class;
        void AddService<T>(Func<object?> getServiceCallback)
            where T : class;
        void AddService(Type serviceType, object? serviceInstance);
        void AddService(Type serviceType, Func<object?> getServiceCallback);

        void AddComponentModel(Action<IComponentModelBuilder>? configure = null);
    }

    private class ServiceProviderBuilder : IServiceProviderBuilder
    {
        public StrictMock<IServiceProvider> Mock { get; } = new();

        public void AddService<T>(T? serviceInstance)
            where T : class
        {
            AddService(typeof(T), serviceInstance);
        }

        public void AddService<T>(Func<T?> getServiceCallback)
            where T : class
        {
            AddService(typeof(T), getServiceCallback);
        }

        public void AddService<T>(object? serviceInstance)
            where T : class
        {
            AddService(typeof(T), serviceInstance);
        }

        public void AddService<T>(Func<object?> getServiceCallback)
            where T : class
        {
            AddService(typeof(T), getServiceCallback);
        }

        public void AddService(Type serviceType, object? serviceInstance)
        {
            Mock.Setup(x => x.GetService(serviceType))
                .Returns(serviceInstance!);
        }

        public void AddService(Type serviceType, Func<object?> getServiceCallback)
        {
            Mock.Setup<object?>(x => x.GetService(serviceType))
                .Returns(() => getServiceCallback());
        }

        public void AddComponentModel(Action<IComponentModelBuilder>? configure = null)
        {
            AddService<SComponentModel>(CreateComponentModel(configure));
        }
    }

    public static IAsyncServiceProvider CreateAsyncServiceProvider(Action<IAsyncServiceProviderBuilder>? configure = null)
    {
        var builder = new AsyncServiceProviderBuilder();
        configure?.Invoke(builder);
        return builder.Mock.Object;
    }

    public interface IAsyncServiceProviderBuilder
    {
        void AddService<TService, TInterface>(TInterface service)
            where TInterface : class;
        void AddService<TService, TInterface>(Func<TInterface> service)
            where TInterface : class;
    }

    private class AsyncServiceProviderBuilder : IAsyncServiceProviderBuilder
    {
        private readonly StrictMock<IAsyncServiceProvider> _mock1;
        private readonly Mock<IAsyncServiceProvider2> _mock2;
        private readonly Mock<IAsyncServiceProvider3> _mock3;

        public StrictMock<IAsyncServiceProvider> Mock => _mock1;

        public AsyncServiceProviderBuilder()
        {
            _mock1 = new();
            _mock2 = Mock.As<IAsyncServiceProvider2>();
            _mock3 = Mock.As<IAsyncServiceProvider3>();
        }

        public void AddService<TService, TInterface>(TInterface service)
            where TInterface : class
        {
            _mock1.Setup(x => x.GetServiceAsync(typeof(TService)))
                  .ReturnsAsync(service);
            _mock2.Setup(x => x.GetServiceAsync(typeof(TService), /*swallowException*/ false))
                  .ReturnsAsync(service);
            _mock3.Setup(x => x.GetServiceAsync<TService, TInterface>(/*throwOnFailure*/ true, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(service);
        }

        public void AddService<TService, TInterface>(Func<TInterface> getServiceCallback)
            where TInterface : class
        {
            _mock1.Setup(x => x.GetServiceAsync(typeof(TService)))
                  .ReturnsAsync(() => getServiceCallback());
            _mock2.Setup(x => x.GetServiceAsync(typeof(TService), /*swallowException*/ false))
                  .ReturnsAsync(() => getServiceCallback());
            _mock3.Setup(x => x.GetServiceAsync<TService, TInterface>(/*throwOnFailure*/ true, It.IsAny<CancellationToken>()))
                  .ReturnsAsync(() => getServiceCallback());
        }
    }

    public static IComponentModel CreateComponentModel(Action<IComponentModelBuilder>? configure = null)
    {
        var builder = new ComponentModelBuilder();
        configure?.Invoke(builder);
        return builder.Mock.Object;
    }

    public interface IComponentModelBuilder
    {
        void AddExport<T>(T instance)
            where T : class;
        void AddExport<T>(Func<T> getInstanceCallback)
            where T : class;
    }

    private class ComponentModelBuilder : IComponentModelBuilder
    {
        private readonly Dictionary<string, Func<object>> _contractNameToExportMap = new();

        public void AddExport<T>(T instance)
            where T : class
        {
            _contractNameToExportMap.Add(typeof(T).FullName, () => instance);
        }

        public void AddExport<T>(Func<T> getInstanceCallback)
            where T : class
        {
            _contractNameToExportMap.Add(typeof(T).FullName, () => getInstanceCallback());
        }

        public StrictMock<IComponentModel> Mock
        {
            get
            {
                var mock = new StrictMock<IComponentModel>();

                mock.SetupGet(x => x.DefaultExportProvider)
                    .Returns(new SimpleExportProvider(_contractNameToExportMap));

                return mock;
            }
        }

        private class SimpleExportProvider(Dictionary<string, Func<object>> contractNameToExportMap) : ExportProvider
        {
            protected override IEnumerable<Export> GetExportsCore(ImportDefinition definition, AtomicComposition atomicComposition)
            {
                var contractName = definition.ContractName;

                if (!contractNameToExportMap.TryGetValue(contractName, out var exportValueGetter))
                {
                    throw new InvalidOperationException($"Failed to find export with contract name, '{contractName}'");
                }

                yield return new Export(contractName, exportValueGetter);
            }
        }
    }

    public static IVsService<TService, TInterface> CreateVsService<TService, TInterface>(Mock<TInterface> serviceMock)
        where TService : class
        where TInterface : class
        => CreateVsService<TService, TInterface>(serviceMock.Object);

    public static IVsService<TService, TInterface> CreateVsService<TService, TInterface>(TInterface service)
        where TService : class
        where TInterface : class
    {
        var mock = new StrictMock<IVsService<TService, TInterface>>();

        mock.Setup(x => x.GetValueAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);
        mock.Setup(x => x.GetValueOrNullAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(service);

        return mock.Object;
    }
}
