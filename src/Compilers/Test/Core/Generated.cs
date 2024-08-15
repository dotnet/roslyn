// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// This is a generated file, please edit Generate.ps1 to change the contents

#nullable disable

using Microsoft.CodeAnalysis;

namespace Roslyn.Test.Utilities
{
    public static class TestMetadata
    {
        public readonly struct ReferenceInfo
        {
            public string FileName { get; }
            public byte[] ImageBytes { get; }
            public ReferenceInfo(string fileName, byte[] imageBytes)
            {
                FileName = fileName;
                ImageBytes = imageBytes;
            }
        }
        public static class ResourcesNet20
        {
            private static byte[] _mscorlib;
            public static byte[] mscorlib => ResourceLoader.GetOrCreateResource(ref _mscorlib, "net20.mscorlib.dll");
            private static byte[] _System;
            public static byte[] System => ResourceLoader.GetOrCreateResource(ref _System, "net20.System.dll");
            private static byte[] _MicrosoftVisualBasic;
            public static byte[] MicrosoftVisualBasic => ResourceLoader.GetOrCreateResource(ref _MicrosoftVisualBasic, "net20.Microsoft.VisualBasic.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("mscorlib.dll", mscorlib),
                new ReferenceInfo("System.dll", System),
                new ReferenceInfo("Microsoft.VisualBasic.dll", MicrosoftVisualBasic),
            };
        }
        public static class Net20
        {
            public static PortableExecutableReference mscorlib { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet20.mscorlib).GetReference(display: "mscorlib.dll (net20)", filePath: "mscorlib.dll");
            public static PortableExecutableReference System { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet20.System).GetReference(display: "System.dll (net20)", filePath: "System.dll");
            public static PortableExecutableReference MicrosoftVisualBasic { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet20.MicrosoftVisualBasic).GetReference(display: "Microsoft.VisualBasic.dll (net20)", filePath: "Microsoft.VisualBasic.dll");
        }
        public static class ResourcesNet40
        {
            private static byte[] _mscorlib;
            public static byte[] mscorlib => ResourceLoader.GetOrCreateResource(ref _mscorlib, "net40.mscorlib.dll");
            private static byte[] _System;
            public static byte[] System => ResourceLoader.GetOrCreateResource(ref _System, "net40.System.dll");
            private static byte[] _SystemCore;
            public static byte[] SystemCore => ResourceLoader.GetOrCreateResource(ref _SystemCore, "net40.System.Core.dll");
            private static byte[] _SystemData;
            public static byte[] SystemData => ResourceLoader.GetOrCreateResource(ref _SystemData, "net40.System.Data.dll");
            private static byte[] _SystemXml;
            public static byte[] SystemXml => ResourceLoader.GetOrCreateResource(ref _SystemXml, "net40.System.Xml.dll");
            private static byte[] _SystemXmlLinq;
            public static byte[] SystemXmlLinq => ResourceLoader.GetOrCreateResource(ref _SystemXmlLinq, "net40.System.Xml.Linq.dll");
            private static byte[] _MicrosoftVisualBasic;
            public static byte[] MicrosoftVisualBasic => ResourceLoader.GetOrCreateResource(ref _MicrosoftVisualBasic, "net40.Microsoft.VisualBasic.dll");
            private static byte[] _MicrosoftCSharp;
            public static byte[] MicrosoftCSharp => ResourceLoader.GetOrCreateResource(ref _MicrosoftCSharp, "net40.Microsoft.CSharp.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("mscorlib.dll", mscorlib),
                new ReferenceInfo("System.dll", System),
                new ReferenceInfo("System.Core.dll", SystemCore),
                new ReferenceInfo("System.Data.dll", SystemData),
                new ReferenceInfo("System.Xml.dll", SystemXml),
                new ReferenceInfo("System.Xml.Linq.dll", SystemXmlLinq),
                new ReferenceInfo("Microsoft.VisualBasic.dll", MicrosoftVisualBasic),
                new ReferenceInfo("Microsoft.CSharp.dll", MicrosoftCSharp),
            };
        }
        public static class Net40
        {
            public static PortableExecutableReference mscorlib { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.mscorlib).GetReference(display: "mscorlib.dll (net40)", filePath: "mscorlib.dll");
            public static PortableExecutableReference System { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.System).GetReference(display: "System.dll (net40)", filePath: "System.dll");
            public static PortableExecutableReference SystemCore { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.SystemCore).GetReference(display: "System.Core.dll (net40)", filePath: "System.Core.dll");
            public static PortableExecutableReference SystemData { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.SystemData).GetReference(display: "System.Data.dll (net40)", filePath: "System.Data.dll");
            public static PortableExecutableReference SystemXml { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.SystemXml).GetReference(display: "System.Xml.dll (net40)", filePath: "System.Xml.dll");
            public static PortableExecutableReference SystemXmlLinq { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.SystemXmlLinq).GetReference(display: "System.Xml.Linq.dll (net40)", filePath: "System.Xml.Linq.dll");
            public static PortableExecutableReference MicrosoftVisualBasic { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.MicrosoftVisualBasic).GetReference(display: "Microsoft.VisualBasic.dll (net40)", filePath: "Microsoft.VisualBasic.dll");
            public static PortableExecutableReference MicrosoftCSharp { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet40.MicrosoftCSharp).GetReference(display: "Microsoft.CSharp.dll (net40)", filePath: "Microsoft.CSharp.dll");
        }
        public static class ResourcesMicrosoftCSharp
        {
            private static byte[] _Netstandard10;
            public static byte[] Netstandard10 => ResourceLoader.GetOrCreateResource(ref _Netstandard10, "netstandard10.microsoftcsharp.Microsoft.CSharp.dll");
            private static byte[] _Netstandard13Lib;
            public static byte[] Netstandard13Lib => ResourceLoader.GetOrCreateResource(ref _Netstandard13Lib, "netstandard13lib.microsoftcsharp.Microsoft.CSharp.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("Netstandard10.dll", Netstandard10),
                new ReferenceInfo("Netstandard13Lib.dll", Netstandard13Lib),
            };
        }
        public static class MicrosoftCSharp
        {
            public static PortableExecutableReference Netstandard10 { get; } = AssemblyMetadata.CreateFromImage(ResourcesMicrosoftCSharp.Netstandard10).GetReference(display: "Microsoft.CSharp.dll (microsoftcsharp)", filePath: "Netstandard10.dll");
            public static PortableExecutableReference Netstandard13Lib { get; } = AssemblyMetadata.CreateFromImage(ResourcesMicrosoftCSharp.Netstandard13Lib).GetReference(display: "Microsoft.CSharp.dll (microsoftcsharp)", filePath: "Netstandard13Lib.dll");
        }
        public static class ResourcesMicrosoftVisualBasic
        {
            private static byte[] _Netstandard11;
            public static byte[] Netstandard11 => ResourceLoader.GetOrCreateResource(ref _Netstandard11, "netstandard11.microsoftvisualbasic.Microsoft.VisualBasic.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("Netstandard11.dll", Netstandard11),
            };
        }
        public static class MicrosoftVisualBasic
        {
            public static PortableExecutableReference Netstandard11 { get; } = AssemblyMetadata.CreateFromImage(ResourcesMicrosoftVisualBasic.Netstandard11).GetReference(display: "Microsoft.VisualBasic.dll (microsoftvisualbasic)", filePath: "Netstandard11.dll");
        }
        public static class ResourcesSystemThreadingTasksExtensions
        {
            private static byte[] _PortableLib;
            public static byte[] PortableLib => ResourceLoader.GetOrCreateResource(ref _PortableLib, "portablelib.systemthreadingtasksextensions.System.Threading.Tasks.Extensions.dll");
            private static byte[] _NetStandard20Lib;
            public static byte[] NetStandard20Lib => ResourceLoader.GetOrCreateResource(ref _NetStandard20Lib, "netstandard20lib.systemthreadingtasksextensions.System.Threading.Tasks.Extensions.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("PortableLib.dll", PortableLib),
                new ReferenceInfo("NetStandard20Lib.dll", NetStandard20Lib),
            };
        }
        public static class SystemThreadingTasksExtensions
        {
            public static PortableExecutableReference PortableLib { get; } = AssemblyMetadata.CreateFromImage(ResourcesSystemThreadingTasksExtensions.PortableLib).GetReference(display: "System.Threading.Tasks.Extensions.dll (systemthreadingtasksextensions)", filePath: "PortableLib.dll");
            public static PortableExecutableReference NetStandard20Lib { get; } = AssemblyMetadata.CreateFromImage(ResourcesSystemThreadingTasksExtensions.NetStandard20Lib).GetReference(display: "System.Threading.Tasks.Extensions.dll (systemthreadingtasksextensions)", filePath: "NetStandard20Lib.dll");
        }
        public static class ResourcesBuildExtensions
        {
            private static byte[] _NetStandardToNet461;
            public static byte[] NetStandardToNet461 => ResourceLoader.GetOrCreateResource(ref _NetStandardToNet461, "netstandardtonet461.buildextensions.netstandard.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("NetStandardToNet461.dll", NetStandardToNet461),
            };
        }
        public static class BuildExtensions
        {
            public static PortableExecutableReference NetStandardToNet461 { get; } = AssemblyMetadata.CreateFromImage(ResourcesBuildExtensions.NetStandardToNet461).GetReference(display: "netstandard.dll (buildextensions)", filePath: "NetStandardToNet461.dll");
        }
    }
}
