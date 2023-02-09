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
        public static class ResourcesNet35
        {
            private static byte[] _SystemCore;
            public static byte[] SystemCore => ResourceLoader.GetOrCreateResource(ref _SystemCore, "net35.System.Core.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("System.Core.dll", SystemCore),
            };
        }
        public static class Net35
        {
            public static PortableExecutableReference SystemCore { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet35.SystemCore).GetReference(display: "System.Core.dll (net35)", filePath: "System.Core.dll");
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
        public static class ResourcesNet451
        {
            private static byte[] _mscorlib;
            public static byte[] mscorlib => ResourceLoader.GetOrCreateResource(ref _mscorlib, "net451.mscorlib.dll");
            private static byte[] _System;
            public static byte[] System => ResourceLoader.GetOrCreateResource(ref _System, "net451.System.dll");
            private static byte[] _SystemConfiguration;
            public static byte[] SystemConfiguration => ResourceLoader.GetOrCreateResource(ref _SystemConfiguration, "net451.System.Configuration.dll");
            private static byte[] _SystemCore;
            public static byte[] SystemCore => ResourceLoader.GetOrCreateResource(ref _SystemCore, "net451.System.Core.dll");
            private static byte[] _SystemData;
            public static byte[] SystemData => ResourceLoader.GetOrCreateResource(ref _SystemData, "net451.System.Data.dll");
            private static byte[] _SystemDrawing;
            public static byte[] SystemDrawing => ResourceLoader.GetOrCreateResource(ref _SystemDrawing, "net451.System.Drawing.dll");
            private static byte[] _SystemEnterpriseServices;
            public static byte[] SystemEnterpriseServices => ResourceLoader.GetOrCreateResource(ref _SystemEnterpriseServices, "net451.System.EnterpriseServices.dll");
            private static byte[] _SystemRuntimeSerialization;
            public static byte[] SystemRuntimeSerialization => ResourceLoader.GetOrCreateResource(ref _SystemRuntimeSerialization, "net451.System.Runtime.Serialization.dll");
            private static byte[] _SystemWindowsForms;
            public static byte[] SystemWindowsForms => ResourceLoader.GetOrCreateResource(ref _SystemWindowsForms, "net451.System.Windows.Forms.dll");
            private static byte[] _SystemWebServices;
            public static byte[] SystemWebServices => ResourceLoader.GetOrCreateResource(ref _SystemWebServices, "net451.System.Web.Services.dll");
            private static byte[] _SystemXml;
            public static byte[] SystemXml => ResourceLoader.GetOrCreateResource(ref _SystemXml, "net451.System.Xml.dll");
            private static byte[] _SystemXmlLinq;
            public static byte[] SystemXmlLinq => ResourceLoader.GetOrCreateResource(ref _SystemXmlLinq, "net451.System.Xml.Linq.dll");
            private static byte[] _MicrosoftCSharp;
            public static byte[] MicrosoftCSharp => ResourceLoader.GetOrCreateResource(ref _MicrosoftCSharp, "net451.Microsoft.CSharp.dll");
            private static byte[] _MicrosoftVisualBasic;
            public static byte[] MicrosoftVisualBasic => ResourceLoader.GetOrCreateResource(ref _MicrosoftVisualBasic, "net451.Microsoft.VisualBasic.dll");
            private static byte[] _SystemObjectModel;
            public static byte[] SystemObjectModel => ResourceLoader.GetOrCreateResource(ref _SystemObjectModel, "net451.System.ObjectModel.dll");
            private static byte[] _SystemRuntime;
            public static byte[] SystemRuntime => ResourceLoader.GetOrCreateResource(ref _SystemRuntime, "net451.System.Runtime.dll");
            private static byte[] _SystemRuntimeInteropServicesWindowsRuntime;
            public static byte[] SystemRuntimeInteropServicesWindowsRuntime => ResourceLoader.GetOrCreateResource(ref _SystemRuntimeInteropServicesWindowsRuntime, "net451.System.Runtime.InteropServices.WindowsRuntime.dll");
            private static byte[] _SystemThreading;
            public static byte[] SystemThreading => ResourceLoader.GetOrCreateResource(ref _SystemThreading, "net451.System.Threading.dll");
            private static byte[] _SystemThreadingTasks;
            public static byte[] SystemThreadingTasks => ResourceLoader.GetOrCreateResource(ref _SystemThreadingTasks, "net451.System.Threading.Tasks.dll");
            public static ReferenceInfo[] All => new[]
            {
                new ReferenceInfo("mscorlib.dll", mscorlib),
                new ReferenceInfo("System.dll", System),
                new ReferenceInfo("System.Configuration.dll", SystemConfiguration),
                new ReferenceInfo("System.Core.dll", SystemCore),
                new ReferenceInfo("System.Data.dll", SystemData),
                new ReferenceInfo("System.Drawing.dll", SystemDrawing),
                new ReferenceInfo("System.EnterpriseServices.dll", SystemEnterpriseServices),
                new ReferenceInfo("System.Runtime.Serialization.dll", SystemRuntimeSerialization),
                new ReferenceInfo("System.Windows.Forms.dll", SystemWindowsForms),
                new ReferenceInfo("System.Web.Services.dll", SystemWebServices),
                new ReferenceInfo("System.Xml.dll", SystemXml),
                new ReferenceInfo("System.Xml.Linq.dll", SystemXmlLinq),
                new ReferenceInfo("Microsoft.CSharp.dll", MicrosoftCSharp),
                new ReferenceInfo("Microsoft.VisualBasic.dll", MicrosoftVisualBasic),
                new ReferenceInfo("System.ObjectModel.dll", SystemObjectModel),
                new ReferenceInfo("System.Runtime.dll", SystemRuntime),
                new ReferenceInfo("System.Runtime.InteropServices.WindowsRuntime.dll", SystemRuntimeInteropServicesWindowsRuntime),
                new ReferenceInfo("System.Threading.dll", SystemThreading),
                new ReferenceInfo("System.Threading.Tasks.dll", SystemThreadingTasks),
            };
        }
        public static class Net451
        {
            public static PortableExecutableReference mscorlib { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.mscorlib).GetReference(display: "mscorlib.dll (net451)", filePath: "mscorlib.dll");
            public static PortableExecutableReference System { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.System).GetReference(display: "System.dll (net451)", filePath: "System.dll");
            public static PortableExecutableReference SystemConfiguration { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemConfiguration).GetReference(display: "System.Configuration.dll (net451)", filePath: "System.Configuration.dll");
            public static PortableExecutableReference SystemCore { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemCore).GetReference(display: "System.Core.dll (net451)", filePath: "System.Core.dll");
            public static PortableExecutableReference SystemData { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemData).GetReference(display: "System.Data.dll (net451)", filePath: "System.Data.dll");
            public static PortableExecutableReference SystemDrawing { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemDrawing).GetReference(display: "System.Drawing.dll (net451)", filePath: "System.Drawing.dll");
            public static PortableExecutableReference SystemEnterpriseServices { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemEnterpriseServices).GetReference(display: "System.EnterpriseServices.dll (net451)", filePath: "System.EnterpriseServices.dll");
            public static PortableExecutableReference SystemRuntimeSerialization { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemRuntimeSerialization).GetReference(display: "System.Runtime.Serialization.dll (net451)", filePath: "System.Runtime.Serialization.dll");
            public static PortableExecutableReference SystemWindowsForms { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemWindowsForms).GetReference(display: "System.Windows.Forms.dll (net451)", filePath: "System.Windows.Forms.dll");
            public static PortableExecutableReference SystemWebServices { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemWebServices).GetReference(display: "System.Web.Services.dll (net451)", filePath: "System.Web.Services.dll");
            public static PortableExecutableReference SystemXml { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemXml).GetReference(display: "System.Xml.dll (net451)", filePath: "System.Xml.dll");
            public static PortableExecutableReference SystemXmlLinq { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemXmlLinq).GetReference(display: "System.Xml.Linq.dll (net451)", filePath: "System.Xml.Linq.dll");
            public static PortableExecutableReference MicrosoftCSharp { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.MicrosoftCSharp).GetReference(display: "Microsoft.CSharp.dll (net451)", filePath: "Microsoft.CSharp.dll");
            public static PortableExecutableReference MicrosoftVisualBasic { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.MicrosoftVisualBasic).GetReference(display: "Microsoft.VisualBasic.dll (net451)", filePath: "Microsoft.VisualBasic.dll");
            public static PortableExecutableReference SystemObjectModel { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemObjectModel).GetReference(display: "System.ObjectModel.dll (net451)", filePath: "System.ObjectModel.dll");
            public static PortableExecutableReference SystemRuntime { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemRuntime).GetReference(display: "System.Runtime.dll (net451)", filePath: "System.Runtime.dll");
            public static PortableExecutableReference SystemRuntimeInteropServicesWindowsRuntime { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemRuntimeInteropServicesWindowsRuntime).GetReference(display: "System.Runtime.InteropServices.WindowsRuntime.dll (net451)", filePath: "System.Runtime.InteropServices.WindowsRuntime.dll");
            public static PortableExecutableReference SystemThreading { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemThreading).GetReference(display: "System.Threading.dll (net451)", filePath: "System.Threading.dll");
            public static PortableExecutableReference SystemThreadingTasks { get; } = AssemblyMetadata.CreateFromImage(ResourcesNet451.SystemThreadingTasks).GetReference(display: "System.Threading.Tasks.dll (net451)", filePath: "System.Threading.Tasks.dll");
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
