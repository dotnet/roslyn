// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;
using TestResources.NetFX;

namespace Test.Utilities
{
    internal static class AdditionalMetadataReferences
    {
        internal static readonly MetadataReference SystemXmlReference = MetadataReference.CreateFromFile(typeof(System.Xml.XmlDocument).Assembly.Location);
        internal static readonly MetadataReference SystemXmlDataReference = MetadataReference.CreateFromFile(typeof(System.Data.Rule).Assembly.Location);
        internal static readonly MetadataReference CSharpSymbolsReference = MetadataReference.CreateFromFile(typeof(CSharpCompilation).Assembly.Location);
        internal static readonly MetadataReference VisualBasicSymbolsReference = MetadataReference.CreateFromFile(typeof(VisualBasicCompilation).Assembly.Location);
        internal static readonly MetadataReference WorkspacesReference = MetadataReference.CreateFromFile(typeof(Workspace).Assembly.Location);
        internal static readonly MetadataReference SystemDiagnosticsDebugReference = MetadataReference.CreateFromFile(typeof(Debug).Assembly.Location);
        internal static readonly MetadataReference SystemDataReference = MetadataReference.CreateFromFile(typeof(System.Data.DataSet).Assembly.Location);


        private static MetadataReference s_systemRuntimeFacadeRef;
        public static MetadataReference SystemRuntimeFacadeRef
        {
            get
            {
                if (s_systemRuntimeFacadeRef == null)
                {
                    s_systemRuntimeFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Runtime).GetReference(display: "System.Runtime.dll");
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
                    s_systemThreadingFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Threading).GetReference(display: "System.Threading.dll");
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
                    s_systemThreadingTasksFacadeRef = AssemblyMetadata.CreateFromImage(ReferenceAssemblies_V45_Facades.System_Threading_Tasks).GetReference(display: "System.Threading.Tasks.dll");
                }

                return s_systemThreadingTasksFacadeRef;
            }
        }
    }
}
