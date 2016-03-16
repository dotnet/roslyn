// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.VisualStudio
{
    // Resolves failed assembly resolves to assemblies that live alongside the dll that contains this test 
    // framework.
    //
    // To avoid running into the .NET EventListener bug https://github.com/dotnet/roslyn/issues/6358, we
    // turn off AppDomain isolation in xUnit. Unfortunately, this changes binding behavior in two ways;
    // 
    // 1) AppBase becomes the location of xunit.console.[arch].exe executable, instead of the test library.
    //
    // 2) Binding redirect and other binding settings are not respected because AppDomain 0 uses whatever
    //    settings are in xunit.console.[arch].exe.config.
    //
    // This is problematic for a variety of tests that expect this to work. For example, the project system 
    // tests have an indirect reference on System.Collections.Immutable, 1.1.36 however, we only deploy 
    // System.Collections.Immutable, 1.1.37, which, without a binding redirects, causes a version mismatch on
    // .NET Framework.
    //
    // The aim of this class is avoid the need of binding redirects, by always resolving to whatever assembly 
    // is deployed alongside the test framework, we avoid the need
    public class ResolveDependenciesLocallyTestFramework : ITestFramework
    {
        private readonly XunitTestFramework _underlyingFramework;

        private ResolveDependenciesLocallyTestFramework(IMessageSink messageSink)
        {
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
            _underlyingFramework = new XunitTestFramework(messageSink);
        }

        public ISourceInformationProvider SourceInformationProvider
        {
            set { _underlyingFramework.SourceInformationProvider = value; }
        }

        public ITestFrameworkDiscoverer GetDiscoverer(IAssemblyInfo assembly)
        {
            return _underlyingFramework.GetDiscoverer(assembly);
        }

        public ITestFrameworkExecutor GetExecutor(AssemblyName assemblyName)
        {
            return _underlyingFramework.GetExecutor(assemblyName);
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyResolve -= OnAssemblyResolve;
            _underlyingFramework.Dispose();
        }

        private Assembly OnAssemblyResolve(object sender, ResolveEventArgs args)
        {
            string directoryPath = Path.GetDirectoryName(GetType().Assembly.Location);

            AssemblyName name = new AssemblyName(args.Name);

            return Assembly.LoadFrom(Path.Combine(directoryPath, name.Name + ".dll"));
        }
    }
}
