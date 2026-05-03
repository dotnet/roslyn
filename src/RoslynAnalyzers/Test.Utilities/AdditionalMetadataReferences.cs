// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Test.Utilities
{
    public static class AdditionalMetadataReferences
    {
        public static ReferenceAssemblies DefaultNetCore { get; } = ReferenceAssemblies.Net.Net100
                .AddPackages(ImmutableArray.Create(
                    new PackageIdentity("Microsoft.CodeAnalysis", "3.0.0"),
                    new PackageIdentity("System.Runtime.Serialization.Formatters", "4.3.0"),
                    new PackageIdentity("System.Configuration.ConfigurationManager", "4.7.0"),
                    new PackageIdentity("System.Security.Cryptography.Cng", "4.7.0"),
                    new PackageIdentity("System.Security.Permissions", "4.7.0"),
                    new PackageIdentity("Microsoft.VisualBasic", "10.3.0")));

        public static ReferenceAssemblies DefaultNetFramework { get; } = ReferenceAssemblies.Default
            .AddAssemblies(ImmutableArray.Create("System.Xml.Data"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "3.0.0")));

        public static ReferenceAssemblies Default { get; } =
#if NETCOREAPP
            DefaultNetCore;
#else
            DefaultNetFramework;
#endif

        public static ReferenceAssemblies DefaultWithoutRoslynSymbols { get; } = ReferenceAssemblies.Default
            .AddAssemblies(ImmutableArray.Create("System.Xml.Data"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis.Workspaces.Common", "3.0.0")));

        public static ReferenceAssemblies DefaultWithSystemWeb { get; } = ReferenceAssemblies.NetFramework.Net472.Default
            .AddAssemblies(ImmutableArray.Create("System.Web", "System.Web.Extensions"));

        public static ReferenceAssemblies DefaultForTaintedDataAnalysis { get; } = ReferenceAssemblies.NetFramework.Net472.Default
            .AddAssemblies(ImmutableArray.Create("PresentationFramework", "System.Web", "System.Web.Extensions", "System.Xaml"))
            .AddPackages(ImmutableArray.Create(
                new PackageIdentity("System.DirectoryServices", "6.0.1"),
                new PackageIdentity("AntiXSS", "4.3.0"),
                new PackageIdentity("Microsoft.AspNetCore.Mvc", "2.2.0"),
                new PackageIdentity("Microsoft.EntityFrameworkCore.Relational", "2.0.3")));

        public static ReferenceAssemblies DefaultWithSerialization { get; } = ReferenceAssemblies.NetFramework.Net472.Default
            .AddAssemblies(ImmutableArray.Create("System.Runtime.Serialization"));

        public static ReferenceAssemblies DefaultWithAzureStorage { get; } = ReferenceAssemblies.Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("WindowsAzure.Storage", "9.0.0")));

        public static ReferenceAssemblies DefaultWithNewtonsoftJson10 { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Newtonsoft.Json", "10.0.1")));

        public static ReferenceAssemblies DefaultWithNewtonsoftJson12 { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Newtonsoft.Json", "12.0.1")));

        public static ReferenceAssemblies DefaultWithMELogging { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.Extensions.Logging", "5.0.0")));

        public static ReferenceAssemblies DefaultWithWilson { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.IdentityModel.Tokens", "6.12.0")));

        public static ReferenceAssemblies DefaultWithWinForms { get; } = ReferenceAssemblies.NetFramework.Net472.WindowsForms;

        public static ReferenceAssemblies DefaultWithWinHttpHandler { get; } = ReferenceAssemblies.NetStandard.NetStandard20
            .AddPackages(ImmutableArray.Create(new PackageIdentity("System.Net.Http.WinHttpHandler", "4.7.0")));

        public static ReferenceAssemblies DefaultWithAspNetCoreMvc { get; } = Default
            .AddPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.AspNetCore", "1.1.7"),
                new PackageIdentity("Microsoft.AspNetCore.Mvc", "1.1.8"),
                new PackageIdentity("Microsoft.AspNetCore.Http", "1.1.2")));

        public static ReferenceAssemblies DefaultWithNUnit { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("NUnit", "3.12.0")));

        public static ReferenceAssemblies DefaultWithXUnit { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("xunit", "2.4.1")));

        public static ReferenceAssemblies DefaultWithMSTest { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("MSTest.TestFramework", "2.1.0")));

        public static ReferenceAssemblies DefaultWithAsyncInterfaces { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.Bcl.AsyncInterfaces", "1.1.0")));

        public static MetadataReference SystemCollectionsImmutableReference { get; } = MetadataReference.CreateFromFile(typeof(ImmutableHashSet<>).Assembly.Location);
        public static MetadataReference SystemComponentModelCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.ComponentModel.Composition.ExportAttribute).Assembly.Location);
        public static MetadataReference SystemXmlDataReference { get; } = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        public static MetadataReference CodeAnalysisReference { get; } = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        public static MetadataReference CSharpSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        public static MetadataReference WorkspacesReference { get; } = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
#if !NETCOREAPP
        public static MetadataReference SystemWebReference { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        public static MetadataReference SystemRuntimeSerialization { get; } = MetadataReference.CreateFromFile(typeof(System.Runtime.Serialization.NetDataContractSerializer).Assembly.Location);
#endif
        public static MetadataReference TestReferenceAssembly { get; } = MetadataReference.CreateFromFile(typeof(OtherDll.OtherDllStaticMethods).Assembly.Location);
#if !NETCOREAPP
        public static MetadataReference SystemXaml { get; } = MetadataReference.CreateFromFile(typeof(System.Xaml.XamlReader).Assembly.Location);
        public static MetadataReference PresentationFramework { get; } = MetadataReference.CreateFromFile(typeof(System.Windows.Markup.XamlReader).Assembly.Location);
        public static MetadataReference SystemWeb { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        public static MetadataReference SystemWebExtensions { get; } = MetadataReference.CreateFromFile(typeof(System.Web.Script.Serialization.JavaScriptSerializer).Assembly.Location);
        public static MetadataReference SystemServiceModel { get; } = MetadataReference.CreateFromFile(typeof(System.ServiceModel.OperationContractAttribute).Assembly.Location);
#endif
    }
}
