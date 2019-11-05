// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using TestResources.NetFX;

namespace Test.Utilities
{
    public static class AdditionalMetadataReferences
    {
        private static readonly Lazy<Assembly> s_netstandardAssembly = new Lazy<Assembly>(() => Assembly.Load("netstandard, Version=2.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51"));
        private static readonly Lazy<MetadataReference> s_netstandardReference = new Lazy<MetadataReference>(() => MetadataReference.CreateFromFile(s_netstandardAssembly.Value.Location));

        public static readonly MetadataReference SystemComponentModelCompositionReference = MetadataReference.CreateFromFile(typeof(System.ComponentModel.Composition.ExportAttribute).Assembly.Location);
        public static readonly MetadataReference SystemCompositionReference = MetadataReference.CreateFromFile(typeof(System.Composition.ExportAttribute).Assembly.Location);
        internal static readonly MetadataReference SystemXmlReference = MetadataReference.CreateFromFile(typeof(System.Xml.XmlDocument).Assembly.Location);
        internal static readonly MetadataReference SystemXmlDataReference = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        internal static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        internal static readonly MetadataReference VisualBasicSymbolsReference = MetadataReference.CreateFromFile(typeof(VisualBasicCompilation).Assembly.Location);
        internal static readonly MetadataReference WorkspacesReference = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
        internal static readonly MetadataReference SystemDiagnosticsDebugReference = MetadataReference.CreateFromFile(typeof(Debug).Assembly.Location);
        internal static readonly MetadataReference SystemDataReference = MetadataReference.CreateFromFile(typeof(System.Data.DataSet).Assembly.Location);
        internal static readonly MetadataReference SystemWebReference = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        internal static readonly MetadataReference SystemRuntimeSerialization = MetadataReference.CreateFromFile(typeof(System.Runtime.Serialization.NetDataContractSerializer).Assembly.Location);
        public static readonly MetadataReference SystemXmlLinq = MetadataReference.CreateFromFile(typeof(System.Xml.Linq.XAttribute).Assembly.Location);
        internal static readonly MetadataReference TestReferenceAssembly = MetadataReference.CreateFromFile(typeof(OtherDll.OtherDllStaticMethods).Assembly.Location);
        internal static readonly MetadataReference SystemDirectoryServices = MetadataReference.CreateFromFile(typeof(System.DirectoryServices.DirectoryEntry).Assembly.Location);
        internal static readonly MetadataReference SystemXaml = MetadataReference.CreateFromFile(typeof(System.Xaml.XamlReader).Assembly.Location);
        internal static readonly MetadataReference PresentationFramework = MetadataReference.CreateFromFile(typeof(System.Windows.Markup.XamlReader).Assembly.Location);
        public static readonly MetadataReference SystemWeb = MetadataReference.CreateFromFile(typeof(System.Web.HttpRequest).Assembly.Location);
        internal static readonly MetadataReference SystemWebExtensions = MetadataReference.CreateFromFile(typeof(System.Web.Script.Serialization.JavaScriptSerializer).Assembly.Location);
        private static MetadataReference s_systemRuntimeFacadeRef;
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

        private static MetadataReference s_systemThreadingFacadeRef;
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

        private static MetadataReference s_systemThreadingTasksFacadeRef;
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

        public static MetadataReference Netstandard => s_netstandardReference.Value;
    }
}
