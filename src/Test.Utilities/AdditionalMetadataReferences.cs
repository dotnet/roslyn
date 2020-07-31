// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic;

namespace Test.Utilities
{
    public static class AdditionalMetadataReferences
    {
        public static ReferenceAssemblies Default { get; } = ReferenceAssemblies.NetFramework.Net46.Default
            .AddAssemblies(ImmutableArray.Create("System.Xml.Data"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "2.10.0")));

        public static ReferenceAssemblies Default472 { get; } = ReferenceAssemblies.NetFramework.Net472.Default
            .AddAssemblies(ImmutableArray.Create("System.Xml.Data"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", "2.10.0")));

        public static ReferenceAssemblies DefaultWithSystemWeb { get; } = Default
            .AddAssemblies(ImmutableArray.Create("System.Web", "System.Web.Extensions"));

        public static ReferenceAssemblies DefaultForTaintedDataAnalysis { get; } = Default472
            .AddAssemblies(ImmutableArray.Create("PresentationFramework", "System.DirectoryServices", "System.Web", "System.Web.Extensions", "System.Xaml"))
            .AddPackages(ImmutableArray.Create(
                new PackageIdentity("AntiXSS", "4.3.0"),
                new PackageIdentity("Microsoft.EntityFrameworkCore.Relational", "2.0.3")));

        public static ReferenceAssemblies DefaultWithSerialization { get; } = Default
            .AddAssemblies(ImmutableArray.Create("System.Runtime.Serialization"));

        public static ReferenceAssemblies DefaultWithAzureStorage { get; } = ReferenceAssemblies.Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("WindowsAzure.Storage", "9.0.0")));

        public static ReferenceAssemblies DefaultWithNewtonsoftJson { get; } = Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Newtonsoft.Json", "10.0.1")));

        public static ReferenceAssemblies DefaultWithWinForms { get; } = Default
            .AddAssemblies(ImmutableArray.Create("System.Windows.Forms"));

        public static ReferenceAssemblies DefaultWithWinHttpHandler { get; } = ReferenceAssemblies.NetStandard.NetStandard20
            .AddPackages(ImmutableArray.Create(new PackageIdentity("System.Net.Http.WinHttpHandler", "4.7.0")));

        public static ReferenceAssemblies DefaultWithAspNetCoreMvc { get; } = Default
            .AddPackages(ImmutableArray.Create(
                new PackageIdentity("Microsoft.AspNetCore", "1.1.7"),
                new PackageIdentity("Microsoft.AspNetCore.Mvc", "1.1.8"),
                new PackageIdentity("Microsoft.AspNetCore.Http", "1.1.2")));

        public static MetadataReference SystemCollectionsImmutableReference { get; } = MetadataReference.CreateFromFile(typeof(ImmutableHashSet<>).Assembly.Location);
        public static MetadataReference SystemComponentModelCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.ComponentModel.Composition.ExportAttribute).Assembly.Location);
        public static MetadataReference SystemCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.Composition.ExportAttribute).Assembly.Location);
        public static MetadataReference SystemXmlDataReference { get; } = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        public static MetadataReference CodeAnalysisReference { get; } = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        public static MetadataReference CSharpSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        public static MetadataReference VisualBasicSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(VisualBasicCompilation).Assembly.Location);
        public static MetadataReference WorkspacesReference { get; } = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
        public static MetadataReference SystemWebReference { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        public static MetadataReference SystemRuntimeSerialization { get; } = MetadataReference.CreateFromFile(typeof(System.Runtime.Serialization.NetDataContractSerializer).Assembly.Location);
        public static MetadataReference TestReferenceAssembly { get; } = MetadataReference.CreateFromFile(typeof(OtherDll.OtherDllStaticMethods).Assembly.Location);
        public static MetadataReference SystemDirectoryServices { get; } = MetadataReference.CreateFromFile(typeof(System.DirectoryServices.DirectoryEntry).Assembly.Location);
        public static MetadataReference SystemXaml { get; } = MetadataReference.CreateFromFile(typeof(System.Xaml.XamlReader).Assembly.Location);
        public static MetadataReference PresentationFramework { get; } = MetadataReference.CreateFromFile(typeof(System.Windows.Markup.XamlReader).Assembly.Location);
        public static MetadataReference SystemWebExtensions { get; } = MetadataReference.CreateFromFile(typeof(System.Web.Script.Serialization.JavaScriptSerializer).Assembly.Location);
    }
}
