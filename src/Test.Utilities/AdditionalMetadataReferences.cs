// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.VisualBasic;
using NuGet.Packaging.Core;
using NuGet.Versioning;
using TestResources.NetFX;

namespace Test.Utilities
{
    public static class AdditionalMetadataReferences
    {
        public static ReferenceAssemblies Default { get; } = ReferenceAssemblies.Default
            .AddPackages(ImmutableArray.Create(new PackageIdentity("Microsoft.CodeAnalysis", NuGetVersion.Parse("2.10.0"))));

        public static MetadataReference CorlibReference { get; } = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        public static MetadataReference SystemCoreReference { get; } = MetadataReference.CreateFromFile(typeof(HashSet<>).Assembly.Location);
        public static MetadataReference SystemCollectionsImmutableReference { get; } = MetadataReference.CreateFromFile(typeof(ImmutableHashSet<>).Assembly.Location);
        public static MetadataReference SystemComponentModelCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.ComponentModel.Composition.ExportAttribute).Assembly.Location);
        public static MetadataReference SystemCompositionReference { get; } = MetadataReference.CreateFromFile(typeof(System.Composition.ExportAttribute).Assembly.Location);
        internal static MetadataReference SystemXmlReference { get; } = MetadataReference.CreateFromFile(typeof(System.Xml.XmlDocument).Assembly.Location);
        internal static MetadataReference SystemXmlDataReference { get; } = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        internal static MetadataReference CodeAnalysisReference { get; } = MetadataReference.CreateFromFile(typeof(Compilation).Assembly.Location);
        internal static MetadataReference CSharpSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        internal static MetadataReference VisualBasicSymbolsReference { get; } = MetadataReference.CreateFromFile(typeof(VisualBasicCompilation).Assembly.Location);
        internal static MetadataReference WorkspacesReference { get; } = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
        internal static MetadataReference SystemDiagnosticsDebugReference { get; } = MetadataReference.CreateFromFile(typeof(Debug).Assembly.Location);
        internal static MetadataReference SystemDataReference { get; } = MetadataReference.CreateFromFile(typeof(System.Data.DataSet).Assembly.Location);
        internal static MetadataReference SystemWebReference { get; } = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        internal static MetadataReference SystemRuntimeSerialization { get; } = MetadataReference.CreateFromFile(typeof(System.Runtime.Serialization.NetDataContractSerializer).Assembly.Location);
        internal static MetadataReference SystemXmlLinq { get; } = MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XAttribute).Assembly.Location);
        internal static MetadataReference TestReferenceAssembly { get; } = MetadataReference.CreateFromFile(typeof(OtherDll.OtherDllStaticMethods).Assembly.Location);
        internal static MetadataReference SystemDirectoryServices { get; } = MetadataReference.CreateFromFile(typeof(System.DirectoryServices.DirectoryEntry).Assembly.Location);
        internal static MetadataReference SystemXaml { get; } = MetadataReference.CreateFromFile(typeof(System.Xaml.XamlReader).Assembly.Location);
        internal static MetadataReference PresentationFramework { get; } = MetadataReference.CreateFromFile(typeof(System.Windows.Markup.XamlReader).Assembly.Location);
        internal static MetadataReference SystemWebExtensions { get; } = MetadataReference.CreateFromFile(typeof(System.Web.Script.Serialization.JavaScriptSerializer).Assembly.Location);
        internal static MetadataReference SystemGlobalization { get; } = MetadataReference.CreateFromFile(Assembly.Load("System.Globalization, Version=4.0.10.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a").Location);

        private static MetadataReference? s_systemRuntimeFacadeRef;
        public static MetadataReference SystemRuntimeFacadeRef
        {
            get
            {
                if (s_systemRuntimeFacadeRef == null)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope - Dispose ownership transfer at 'AssemblyMetadata.GetReference'
                    s_systemRuntimeFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Runtime).GetReference(display: "System.Runtime.dll");
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                return s_systemRuntimeFacadeRef;
            }
        }

        private static MetadataReference? s_systemThreadingFacadeRef;
        public static MetadataReference SystemThreadingFacadeRef
        {
            get
            {
                if (s_systemThreadingFacadeRef == null)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope - Dispose ownership transfer at 'AssemblyMetadata.GetReference'
                    s_systemThreadingFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Threading).GetReference(display: "System.Threading.dll");
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                return s_systemThreadingFacadeRef;
            }
        }

        private static MetadataReference? s_systemThreadingTasksFacadeRef;
        public static MetadataReference SystemThreadingTaskFacadeRef
        {
            get
            {
                if (s_systemThreadingTasksFacadeRef == null)
                {
#pragma warning disable CA2000 // Dispose objects before losing scope - Dispose ownership transfer at 'AssemblyMetadata.GetReference'
                    s_systemThreadingTasksFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Threading_Tasks).GetReference(display: "System.Threading.Tasks.dll");
#pragma warning restore CA2000 // Dispose objects before losing scope
                }

                return s_systemThreadingTasksFacadeRef;
            }
        }
    }
}
