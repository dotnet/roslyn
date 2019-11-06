// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic;
using NuGet.Packaging.Core;
using NuGet.Versioning;

namespace Test.Utilities
{
    public static class AdditionalMetadataReferences
    {
        public static ReferenceAssemblies Default { get; } = ReferenceAssemblies.Default
            .AddAssemblies(ImmutableArray.Create("System.Xml.Data"))
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", NuGetVersion.Parse("3.0.0"))));

        public static MetadataReference SystemCollectionsImmutableReference { get; } = MetadataReference.CreateFromFile(typeof(ImmutableHashSet<>).Assembly.Location);
        public static MetadataReference SystemComponentModelCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.ComponentModel.Composition.ExportAttribute).Assembly.Location);
        public static MetadataReference SystemCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.Composition.ExportAttribute).Assembly.Location);
        internal static MetadataReference SystemXmlDataReference { get; } = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        internal static MetadataReference CodeAnalysisReference { get; } = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        internal static MetadataReference CSharpSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        internal static MetadataReference VisualBasicSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(VisualBasicCompilation).Assembly.Location);
        internal static MetadataReference WorkspacesReference { get; } = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
        internal static MetadataReference SystemWebReference { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        internal static MetadataReference SystemRuntimeSerialization { get; } = MetadataReference.CreateFromFile(typeof(System.Runtime.Serialization.NetDataContractSerializer).Assembly.Location);
        internal static MetadataReference TestReferenceAssembly { get; } = MetadataReference.CreateFromFile(typeof(OtherDll.OtherDllStaticMethods).Assembly.Location);
        internal static MetadataReference SystemDirectoryServices { get; } = MetadataReference.CreateFromFile(typeof(System.DirectoryServices.DirectoryEntry).Assembly.Location);
        internal static MetadataReference SystemXaml { get; } = MetadataReference.CreateFromFile(typeof(System.Xaml.XamlReader).Assembly.Location);
        internal static MetadataReference PresentationFramework { get; } = MetadataReference.CreateFromFile(typeof(System.Windows.Markup.XamlReader).Assembly.Location);
        public static MetadataReference SystemWeb { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        internal static MetadataReference SystemWebExtensions { get; } = MetadataReference.CreateFromFile(typeof(System.Web.Script.Serialization.JavaScriptSerializer).Assembly.Location);
    }
}
