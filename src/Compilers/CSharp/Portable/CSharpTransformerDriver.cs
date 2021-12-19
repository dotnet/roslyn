// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// <Metalama /> This code is used by Try.Metalama.

using System;
using System.Collections.Immutable;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using PostSharp.Backstage.Extensibility;
using PostSharp.Backstage.Licensing;
using PostSharp.Backstage.Licensing.Consumption;

namespace Metalama.Compiler
{
    public static class CSharpTransformerDriver
    {
        public static (Compilation, ImmutableArray<Diagnostic>) RunTransformers(
            Compilation input, ImmutableArray<ISourceTransformer> transformers, ImmutableArray<object> plugins, AnalyzerConfigOptionsProvider analyzerConfigProvider,
            ImmutableArray<ResourceDescription> manifestResources, IAnalyzerAssemblyLoader assemblyLoader)
        {
            var services = new ServiceCollection();

            var serviceProviderBuilder = new ServiceProviderBuilder(
                (type, instance) => services.AddService(type, instance),
                () => services.GetServiceProvider());

            serviceProviderBuilder.AddSingleton<IBackstageDiagnosticSink>(new CaravelaTryBackstageDiagnosticsSink());
            serviceProviderBuilder.AddSingleton<ILicenseConsumptionManager>(new CaravelaTryLicenseConsumptionManager());

            var diagnostics = DiagnosticBag.GetInstance();
            var results = CSharpCompiler.RunTransformers(input, transformers, null, plugins, analyzerConfigProvider, diagnostics, manifestResources, assemblyLoader, serviceProviderBuilder.ServiceProvider, CancellationToken.None);
            return (results.TransformedCompilation, diagnostics.ToReadOnlyAndFree());
        }

        private class CaravelaTryLicenseConsumptionManager : ILicenseConsumptionManager
        {
            public bool CanConsumeFeatures(ILicenseConsumer consumer, LicensedFeatures requiredFeatures) => true;

            public void ConsumeFeatures(ILicenseConsumer consumer, LicensedFeatures requiredFeatures) { }
        }

        private class CaravelaTryBackstageDiagnosticsSink : IBackstageDiagnosticSink
        {
            public void ReportError(string message, IDiagnosticsLocation? location = null)
            {
                throw new InvalidOperationException(message);
            }

            public void ReportWarning(string message, IDiagnosticsLocation? location = null)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
