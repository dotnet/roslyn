// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace Test.Utilities
{
    public static class AdditionalMetadataReferences
    {
        public static ReferenceAssemblies Default { get; } = CreateDefaultReferenceAssemblies();

        public static ReferenceAssemblies DefaultWithoutRoslynSymbols { get; } = ReferenceAssemblies.Default
            .AddAssemblies(ImmutableArray.Create("System.Xml.Data"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis.Workspaces.Common", "3.0.0")));

        public static ReferenceAssemblies DefaultWithSystemWeb { get; } = ReferenceAssemblies.NetFramework.Net472.Default
            .AddAssemblies(ImmutableArray.Create("System.Web", "System.Web.Extensions"));

        public static ReferenceAssemblies DefaultForTaintedDataAnalysis { get; } = ReferenceAssemblies.NetFramework.Net472.Default
            .AddAssemblies(ImmutableArray.Create("PresentationFramework", "System.DirectoryServices", "System.Web", "System.Web.Extensions", "System.Xaml"))
            .AddPackages(ImmutableArray.Create(
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
        public static MetadataReference SystemCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.Composition.ExportAttribute).Assembly.Location);
        public static MetadataReference SystemXmlDataReference { get; } = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        public static MetadataReference CodeAnalysisReference { get; } = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        public static MetadataReference CSharpSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        public static MetadataReference WorkspacesReference { get; } = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
#if !NETCOREAPP
        public static MetadataReference SystemWebReference { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        public static MetadataReference SystemRuntimeSerialization { get; } = MetadataReference.CreateFromFile(typeof(System.Runtime.Serialization.NetDataContractSerializer).Assembly.Location);
#endif
        public static MetadataReference TestReferenceAssembly { get; } = MetadataReference.CreateFromFile(typeof(OtherDll.OtherDllStaticMethods).Assembly.Location);
        public static MetadataReference SystemDirectoryServices { get; } = MetadataReference.CreateFromFile(typeof(System.DirectoryServices.DirectoryEntry).Assembly.Location);
#if !NETCOREAPP
        public static MetadataReference SystemXaml { get; } = MetadataReference.CreateFromFile(typeof(System.Xaml.XamlReader).Assembly.Location);
        public static MetadataReference PresentationFramework { get; } = MetadataReference.CreateFromFile(typeof(System.Windows.Markup.XamlReader).Assembly.Location);
        public static MetadataReference SystemWeb { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        public static MetadataReference SystemWebExtensions { get; } = MetadataReference.CreateFromFile(typeof(System.Web.Script.Serialization.JavaScriptSerializer).Assembly.Location);
        public static MetadataReference SystemServiceModel { get; } = MetadataReference.CreateFromFile(typeof(System.ServiceModel.OperationContractAttribute).Assembly.Location);
#endif

        private static readonly Lazy<ReferenceAssemblies> _lazyNet60 =
            new(() =>
            {
                return new ReferenceAssemblies(
                    "net6.0",
                    new PackageIdentity(
                        "Microsoft.NETCore.App.Ref",
                        "6.0.0-preview.6.21352.12"),
                    Path.Combine("ref", "net6.0"));
            });

        public static ReferenceAssemblies Net60 => _lazyNet60.Value;

        private static ReferenceAssemblies CreateDefaultReferenceAssemblies()
        {
            var referenceAssemblies = ReferenceAssemblies.Default;

#if !NETCOREAPP
            referenceAssemblies = referenceAssemblies.AddAssemblies(ImmutableArray.Create("System.Xml.Data"));
#endif

            referenceAssemblies = referenceAssemblies.AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "3.0.0")));

#if NETCOREAPP
            referenceAssemblies = referenceAssemblies.AddPackages(ImmutableArray.Create(
                new PackageIdentity("System.Runtime.Serialization.Formatters", "4.3.0"),
                new PackageIdentity("System.Configuration.ConfigurationManager", "4.7.0"),
                new PackageIdentity("System.Security.Cryptography.Cng", "4.7.0"),
                new PackageIdentity("System.Security.Permissions", "4.7.0"),
                new PackageIdentity("Microsoft.VisualBasic", "10.3.0")));
#endif

            return referenceAssemblies;
        }
    }
}
