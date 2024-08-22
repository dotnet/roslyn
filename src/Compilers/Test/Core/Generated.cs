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
    }
}
