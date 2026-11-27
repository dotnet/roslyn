// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Remote.Razor;
using Nerdbank.Streams;
using Xunit;

namespace Microsoft.CodeAnalysis.Razor.Remote;

public sealed class RazorAssemblyLoadContextTest
{
    [Theory]
    [InlineData(RazorAssemblyLoadContext.RemoteServiceHubAssemblyName)]
    [InlineData(RazorAssemblyLoadContext.RemoteWorkspacesAssemblyName)]
    [InlineData(RazorAssemblyLoadContext.MessagePackAssemblyName)]
    [InlineData(RazorAssemblyLoadContext.NerdbankStreamsAssemblyName)]
    [InlineData(RazorAssemblyLoadContext.NewtonsoftJsonAssemblyName)]
    [InlineData(RazorAssemblyLoadContext.StreamJsonRpcAssemblyName)]
    public void SharedRemoteAssemblyUsesParentLoadContext(string assemblyName)
    {
        var baseDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(baseDirectory);
        var sourceAssemblyPath = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.dll");
        var copiedAssemblyPath = Path.Combine(baseDirectory, $"{assemblyName}.dll");
        File.Copy(sourceAssemblyPath, copiedAssemblyPath);

        var parentAssembly = AssemblyLoadContext.Default.Assemblies.SingleOrDefault(assembly => assembly.GetName().Name == assemblyName)
            ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(sourceAssemblyPath);

        var razorLoadContext = RazorAssemblyLoadContext.TestAccessor.Create(AssemblyLoadContext.Default, baseDirectory);
        try
        {
            var assembly = razorLoadContext.LoadFromAssemblyName(new AssemblyName(assemblyName));

            Assert.Same(parentAssembly, assembly);
            Assert.Same(AssemblyLoadContext.Default, AssemblyLoadContext.GetLoadContext(assembly));
            Assert.DoesNotContain(razorLoadContext.Assemblies, assembly => assembly.GetName().Name == assemblyName);
        }
        finally
        {
            razorLoadContext.Unload();
            Directory.Delete(baseDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task RemoteMefInitializationFactoryCanCreateServiceInRazorAlc()
    {
        var (clientStream, serverStream) = FullDuplexStream.CreatePair();
        using var _1 = clientStream;
        using var _2 = serverStream;

        var parentLoadContext = new TestAssemblyLoadContext(AppContext.BaseDirectory);
        var remoteRazorAssembly = parentLoadContext.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, "Microsoft.CodeAnalysis.Remote.Razor.dll"));
        var factoryType = remoteRazorAssembly.GetType("Microsoft.CodeAnalysis.Remote.Razor.RemoteMEFInitializationService+Factory", throwOnError: true)!;
        var factory = Activator.CreateInstance(factoryType)!;
        var createAsync = factoryType.GetMethods(BindingFlags.Instance | BindingFlags.Public).Single(static m => m.Name == "CreateAsync" && m.GetParameters().Length == 5);
        var serviceActivationOptions = Activator.CreateInstance(createAsync.GetParameters()[2].ParameterType)!;

        var task = (Task<object>)createAsync.Invoke(factory, [serverStream, new EmptyServiceProvider(), serviceActivationOptions, null, null])!;
        var service = await task;

        Assert.NotNull(service);
        Assert.NotSame(parentLoadContext, AssemblyLoadContext.GetLoadContext(service.GetType().Assembly));
    }

    private sealed class EmptyServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private sealed class TestAssemblyLoadContext(string baseDirectory) : AssemblyLoadContext(isCollectible: false)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            var assemblyPath = Path.Combine(baseDirectory, $"{assemblyName.Name}.dll");
            return File.Exists(assemblyPath)
                ? LoadFromAssemblyPath(assemblyPath)
                : null;
        }
    }
}
#endif
