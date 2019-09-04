// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;

public static class TestReferences
{
    public static class MetadataTests
    {
        public static class NetModule01
        {
            private static readonly Lazy<PortableExecutableReference> s_appCS = new Lazy<PortableExecutableReference>(
                () => AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.AppCS).GetReference(display: "AppCS"),
                LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference AppCS => s_appCS.Value;

            private static readonly Lazy<PortableExecutableReference> s_moduleCS00 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference(display: "ModuleCS00.mod"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ModuleCS00 => s_moduleCS00.Value;

            private static readonly Lazy<PortableExecutableReference> s_moduleCS01 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS01).GetReference(display: "ModuleCS01.mod"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ModuleCS01 => s_moduleCS01.Value;

            private static readonly Lazy<PortableExecutableReference> s_moduleVB01 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleVB01).GetReference(display: "ModuleVB01.mod"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ModuleVB01 => s_moduleVB01.Value;
        }

        public static class InterfaceAndClass
        {
            private static readonly Lazy<PortableExecutableReference> s_CSClasses01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).GetReference(display: "CSClasses01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CSClasses01 => s_CSClasses01.Value;

            private static readonly Lazy<PortableExecutableReference> s_CSInterfaces01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01).GetReference(display: "CSInterfaces01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CSInterfaces01 => s_CSInterfaces01.Value;

            private static readonly Lazy<PortableExecutableReference> s_VBClasses01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBClasses01).GetReference(display: "VBClasses01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference VBClasses01 => s_VBClasses01.Value;

            private static readonly Lazy<PortableExecutableReference> s_VBClasses02 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBClasses02).GetReference(display: "VBClasses02.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference VBClasses02 => s_VBClasses02.Value;

            private static readonly Lazy<PortableExecutableReference> s_VBInterfaces01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBInterfaces01).GetReference(display: "VBInterfaces01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference VBInterfaces01 => s_VBInterfaces01.Value;
        }
    }

    public static class NetFx
    {
        public static class Minimal
        {
            private static readonly Lazy<PortableExecutableReference> s_mincorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.Minimal.mincorlib).GetReference(display: "mincorlib.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference mincorlib => s_mincorlib.Value;

            private static readonly Lazy<PortableExecutableReference> s_minasync = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.Minimal.minasync).GetReference(display: "minasync.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference minasync => s_minasync.Value;

            private static readonly Lazy<PortableExecutableReference> s_minasynccorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.Minimal.minasynccorlib).GetReference(display: "minasynccorlib.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference minasynccorlib => s_minasynccorlib.Value;
        }

        public static class ValueTuple
        {
            private static readonly Lazy<PortableExecutableReference> s_tuplelib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.ValueTuple.tuplelib).GetReference(display: "System.ValueTuple.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference tuplelib => s_tuplelib.Value;
        }

        public static class silverlight_v5_0_5_0
        {
            private static readonly Lazy<PortableExecutableReference> s_system = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight).GetReference(display: "System.v5.0.5.0_silverlight.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System => s_system.Value;
        }

        public static class v4_0_21006
        {
            private static readonly Lazy<PortableExecutableReference> s_mscorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_21006.mscorlib).GetReference(display: "mscorlib.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference mscorlib => s_mscorlib.Value;
        }

        public static class v2_0_50727
        {
            private static readonly Lazy<PortableExecutableReference> s_mscorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.mscorlib).GetReference(display: "mscorlib, v2.0.50727"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference mscorlib => s_mscorlib.Value;

            private static readonly Lazy<PortableExecutableReference> s_system = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.System).GetReference(display: "System.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System => s_system.Value;

            private static readonly Lazy<PortableExecutableReference> s_microsoft_VisualBasic = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Microsoft_VisualBasic => s_microsoft_VisualBasic.Value;
        }

        public static class v3_5_30729
        {
            private static readonly Lazy<PortableExecutableReference> s_systemCore = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v3_5_30729.System_Core_v3_5_30729.AsImmutableOrNull()).GetReference(display: "System.Core, v3.5.30729"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference SystemCore => s_systemCore.Value;
        }

        /// <summary>
        /// References here map to net40 RTM
        /// </summary>
        public static class v4_0_30319
        {
            private static readonly Lazy<PortableExecutableReference> s_mscorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.mscorlib).GetReference(filePath: @"R:\v4_0_30319\mscorlib.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference mscorlib => s_mscorlib.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_Core = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Core).GetReference(filePath: @"R:\v4_0_30319\System.Core.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Core => s_system_Core.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_Configuration = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Configuration).GetReference(filePath: @"R:\v4_0_30319\System.Configuration.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Configuration => s_system_Configuration.Value;

            private static readonly Lazy<PortableExecutableReference> s_system = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System).GetReference(filePath: @"R:\v4_0_30319\System.dll", display: "System.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System => s_system.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_Data = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Data).GetReference(filePath: @"R:\v4_0_30319\System.Data.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Data => s_system_Data.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_Xml = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Xml).GetReference(filePath: @"R:\v4_0_30319\System.Xml.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Xml => s_system_Xml.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_Xml_Linq = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Xml_Linq).GetReference(filePath: @"R:\v4_0_30319\System.Xml.Linq.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Xml_Linq => s_system_Xml_Linq.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_Windows_Forms = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Windows_Forms).GetReference(filePath: @"R:\v4_0_30319\System.Windows.Forms.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Windows_Forms => s_system_Windows_Forms.Value;

            private static readonly Lazy<PortableExecutableReference> s_microsoft_CSharp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_CSharp).GetReference(filePath: @"R:\v4_0_30319\Microsoft.CSharp.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Microsoft_CSharp => s_microsoft_CSharp.Value;

            private static readonly Lazy<PortableExecutableReference> s_microsoft_VisualBasic = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_VisualBasic).GetReference(filePath: @"R:\v4_0_30319\Microsoft.VisualBasic.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Microsoft_VisualBasic => s_microsoft_VisualBasic.Value;

            private static readonly Lazy<PortableExecutableReference> s_microsoft_JScript = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_JScript).GetReference(display: "Microsoft.JScript.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Microsoft_JScript => s_microsoft_JScript.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_ComponentModel_Composition = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_ComponentModel_Composition).GetReference(display: "System.ComponentModel.Composition.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_ComponentModel_Composition => s_system_ComponentModel_Composition.Value;

            private static readonly Lazy<PortableExecutableReference> s_system_Web_Services = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Web_Services).GetReference(display: "System.Web.Services.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Web_Services => s_system_Web_Services.Value;

            public static class System_EnterpriseServices
            {
                private static readonly Lazy<PortableExecutableReference> s_system_EnterpriseServices = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_EnterpriseServices).GetReference(display: "System.EnterpriseServices.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_system_EnterpriseServices.Value;
            }

            private static readonly Lazy<PortableExecutableReference> s_system_Runtime_Serialization = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Runtime_Serialization).GetReference(display: "System.Runtime.Serialization.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System_Runtime_Serialization => s_system_Runtime_Serialization.Value;
        }

        /// <summary>
        /// References here map to net45 beta.
        /// </summary>
        public static class v4_0_30319_17626
        {
            private static readonly Lazy<PortableExecutableReference> s_mscorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17626.mscorlib).GetReference(display: @"mscorlib.v4_0_30319_17626.dll", filePath: @"Z:\FxReferenceAssembliesUri"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference mscorlib => s_mscorlib.Value;
        }
    }

    public static class NetStandard13
    {
        private static readonly Lazy<PortableExecutableReference> s_systemRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard13.System_Runtime).GetReference(display: @"System.Runtime.dll (netstandard13 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntime => s_systemRuntime.Value;
    }

    public static class NetStandard20
    {
        private static readonly Lazy<PortableExecutableReference> s_netstandard = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.netstandard).GetReference(display: "netstandard.dll (netstandard 2.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference NetStandard => s_netstandard.Value;

        private static readonly Lazy<PortableExecutableReference> s_mscorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.mscorlib).GetReference(display: "mscorlib.dll (netstandard 2.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference MscorlibRef => s_mscorlib.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.System_Runtime).GetReference(display: "System.Runtime.dll (netstandard 2.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeRef => s_systemRuntime.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCore = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.System_Core).GetReference(display: "System.Core.dll (netstandard 2.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCoreRef => s_systemCore.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDynamicRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.System_Dynamic_Runtime).GetReference(display: "System.Dynamic.Runtime.dll (netstandard 2.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDynamicRuntimeRef => s_systemDynamicRuntime.Value;

        private static readonly Lazy<PortableExecutableReference> s_microsoftCSharp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.Microsoft_CSharp).GetReference(display: "Microsoft.CSharp.dll (netstandard 2.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference MicrosoftCSharpRef => s_microsoftCSharp.Value;

        private static readonly Lazy<PortableExecutableReference> s_system_Threading_Tasks_Extensions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.System_Threading_Tasks_Extensions).GetReference(display: "System.Threading.Tasks.Extensions.dll (netstandard 2.0)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference TasksExtensionsRef => s_system_Threading_Tasks_Extensions.Value;

        private static readonly Lazy<PortableExecutableReference> s_system_Runtime_CompilerServices_Unsafe = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.System_Runtime_CompilerServices_Unsafe).GetReference(display: "System.Runtime.CompilerServices.Unsafe.dll (netstandard 2.0)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference UnsafeRef => s_system_Runtime_CompilerServices_Unsafe.Value;

        private static readonly Lazy<PortableExecutableReference> s_microsoftVisualBasic = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netstandard20.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.dll (netstandard 2.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference MicrosoftVisualBasicRef => s_microsoftVisualBasic.Value;
    }

    public static class NetCoreApp30
    {
        private static readonly Lazy<PortableExecutableReference> s_netstandard = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.netstandard).GetReference(display: "netstandard.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference NetStandard => s_netstandard.Value;

        private static readonly Lazy<PortableExecutableReference> s_mscorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.mscorlib).GetReference(display: "mscorlib.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference MscorlibRef => s_mscorlib.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Runtime).GetReference(display: "System.Runtime.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeRef => s_systemRuntime.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCore = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Core).GetReference(display: "System.Core.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCoreRef => s_systemCore.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDynamicRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Dynamic_Runtime).GetReference(display: "System.Dynamic.Runtime.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDynamicRuntimeRef => s_systemDynamicRuntime.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCollections = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Collections).GetReference(display: "System.Collections.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCollectionsRef => s_systemCollections.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemConsole = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Console).GetReference(display: "System.Console.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemConsoleRef => s_systemConsole.Value;

        private static readonly Lazy<PortableExecutableReference> s_system_Threading_Tasks = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Threading_Tasks).GetReference(display: "System.Threading.Tasks.dll (netcoreapp 3.0)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingTasksRef => s_system_Threading_Tasks.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemLinq = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Linq).GetReference(display: "System.Linq.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemLinqRef => s_systemLinq.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemLinqExpressions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Linq_Expressions).GetReference(display: "System.Linq.Expressions.dll (netcoreapp 3.0 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemLinqExpressionsRef => s_systemLinqExpressions.Value;

        private static readonly Lazy<PortableExecutableReference> s_system_Runtime_InteropServices_WindowsRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.netcoreapp30.System_Runtime_InteropServices_WindowsRuntime).GetReference(display: "System_Runtime_InteropServices_WindowsRuntime.dll (netcoreapp 3.0)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeInteropServicesWindowsRuntimeRef => s_system_Runtime_InteropServices_WindowsRuntime.Value;
    }

    public static class Net461
    {
        private static readonly Lazy<PortableExecutableReference> s_microsoftWin32Primitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.Microsoft_Win32_Primitives).GetReference(display: "Microsoft.Win32.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference MicrosoftWin32PrimitivesRef => s_microsoftWin32Primitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_mscorlib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.mscorlib).GetReference(display: "mscorlib.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference mscorlibRef => s_mscorlib.Value;

        private static readonly Lazy<PortableExecutableReference> s_netfxforceconflicts = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.netfx_force_conflicts).GetReference(display: "netfx.force.conflicts.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference netfxforceconflictsRef => s_netfxforceconflicts.Value;

        private static readonly Lazy<PortableExecutableReference> s_netstandard = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.netstandard).GetReference(display: "netstandard.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference netstandardRef => s_netstandard.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemAppContext = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_AppContext).GetReference(display: "System.AppContext.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemAppContextRef => s_systemAppContext.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCollectionsConcurrent = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Collections_Concurrent).GetReference(display: "System.Collections.Concurrent.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCollectionsConcurrentRef => s_systemCollectionsConcurrent.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCollections = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Collections).GetReference(display: "System.Collections.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCollectionsRef => s_systemCollections.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCollectionsNonGeneric = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Collections_NonGeneric).GetReference(display: "System.Collections.NonGeneric.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCollectionsNonGenericRef => s_systemCollectionsNonGeneric.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCollectionsSpecialized = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Collections_Specialized).GetReference(display: "System.Collections.Specialized.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCollectionsSpecializedRef => s_systemCollectionsSpecialized.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemComponentModelComposition = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ComponentModel_Composition).GetReference(display: "System.ComponentModel.Composition.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemComponentModelCompositionRef => s_systemComponentModelComposition.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemComponentModel = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ComponentModel).GetReference(display: "System.ComponentModel.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemComponentModelRef => s_systemComponentModel.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemComponentModelEventBasedAsync = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ComponentModel_EventBasedAsync).GetReference(display: "System.ComponentModel.EventBasedAsync.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemComponentModelEventBasedAsyncRef => s_systemComponentModelEventBasedAsync.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemComponentModelPrimitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ComponentModel_Primitives).GetReference(display: "System.ComponentModel.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemComponentModelPrimitivesRef => s_systemComponentModelPrimitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemComponentModelTypeConverter = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ComponentModel_TypeConverter).GetReference(display: "System.ComponentModel.TypeConverter.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemComponentModelTypeConverterRef => s_systemComponentModelTypeConverter.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemConsole = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Console).GetReference(display: "System.Console.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemConsoleRef => s_systemConsole.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemCore = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Core).GetReference(display: "System.Core.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemCoreRef => s_systemCore.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDataCommon = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Data_Common).GetReference(display: "System.Data.Common.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDataCommonRef => s_systemDataCommon.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemData = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Data).GetReference(display: "System.Data.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDataRef => s_systemData.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsContracts = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_Contracts).GetReference(display: "System.Diagnostics.Contracts.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsContractsRef => s_systemDiagnosticsContracts.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsDebug = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_Debug).GetReference(display: "System.Diagnostics.Debug.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsDebugRef => s_systemDiagnosticsDebug.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsFileVersionInfo = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_FileVersionInfo).GetReference(display: "System.Diagnostics.FileVersionInfo.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsFileVersionInfoRef => s_systemDiagnosticsFileVersionInfo.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsProcess = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_Process).GetReference(display: "System.Diagnostics.Process.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsProcessRef => s_systemDiagnosticsProcess.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsStackTrace = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_StackTrace).GetReference(display: "System.Diagnostics.StackTrace.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsStackTraceRef => s_systemDiagnosticsStackTrace.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsTextWriterTraceListener = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_TextWriterTraceListener).GetReference(display: "System.Diagnostics.TextWriterTraceListener.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsTextWriterTraceListenerRef => s_systemDiagnosticsTextWriterTraceListener.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsTools = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_Tools).GetReference(display: "System.Diagnostics.Tools.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsToolsRef => s_systemDiagnosticsTools.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsTraceSource = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_TraceSource).GetReference(display: "System.Diagnostics.TraceSource.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsTraceSourceRef => s_systemDiagnosticsTraceSource.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDiagnosticsTracing = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Diagnostics_Tracing).GetReference(display: "System.Diagnostics.Tracing.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDiagnosticsTracingRef => s_systemDiagnosticsTracing.Value;

        private static readonly Lazy<PortableExecutableReference> s_system = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System).GetReference(display: "System.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRef => s_system.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDrawing = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Drawing).GetReference(display: "System.Drawing.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDrawingRef => s_systemDrawing.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDrawingPrimitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Drawing_Primitives).GetReference(display: "System.Drawing.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDrawingPrimitivesRef => s_systemDrawingPrimitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemDynamicRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Dynamic_Runtime).GetReference(display: "System.Dynamic.Runtime.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemDynamicRuntimeRef => s_systemDynamicRuntime.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemGlobalizationCalendars = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Globalization_Calendars).GetReference(display: "System.Globalization.Calendars.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemGlobalizationCalendarsRef => s_systemGlobalizationCalendars.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemGlobalization = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Globalization).GetReference(display: "System.Globalization.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemGlobalizationRef => s_systemGlobalization.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemGlobalizationExtensions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Globalization_Extensions).GetReference(display: "System.Globalization.Extensions.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemGlobalizationExtensionsRef => s_systemGlobalizationExtensions.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOCompression = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_Compression).GetReference(display: "System.IO.Compression.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOCompressionRef => s_systemIOCompression.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOCompressionFileSystem = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_Compression_FileSystem).GetReference(display: "System.IO.Compression.FileSystem.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOCompressionFileSystemRef => s_systemIOCompressionFileSystem.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOCompressionZipFile = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_Compression_ZipFile).GetReference(display: "System.IO.Compression.ZipFile.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOCompressionZipFileRef => s_systemIOCompressionZipFile.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIO = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO).GetReference(display: "System.IO.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIORef => s_systemIO.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOFileSystem = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_FileSystem).GetReference(display: "System.IO.FileSystem.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOFileSystemRef => s_systemIOFileSystem.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOFileSystemDriveInfo = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_FileSystem_DriveInfo).GetReference(display: "System.IO.FileSystem.DriveInfo.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOFileSystemDriveInfoRef => s_systemIOFileSystemDriveInfo.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOFileSystemPrimitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_FileSystem_Primitives).GetReference(display: "System.IO.FileSystem.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOFileSystemPrimitivesRef => s_systemIOFileSystemPrimitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOFileSystemWatcher = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_FileSystem_Watcher).GetReference(display: "System.IO.FileSystem.Watcher.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOFileSystemWatcherRef => s_systemIOFileSystemWatcher.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOIsolatedStorage = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_IsolatedStorage).GetReference(display: "System.IO.IsolatedStorage.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOIsolatedStorageRef => s_systemIOIsolatedStorage.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOMemoryMappedFiles = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_MemoryMappedFiles).GetReference(display: "System.IO.MemoryMappedFiles.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOMemoryMappedFilesRef => s_systemIOMemoryMappedFiles.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOPipes = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_Pipes).GetReference(display: "System.IO.Pipes.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOPipesRef => s_systemIOPipes.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemIOUnmanagedMemoryStream = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_IO_UnmanagedMemoryStream).GetReference(display: "System.IO.UnmanagedMemoryStream.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemIOUnmanagedMemoryStreamRef => s_systemIOUnmanagedMemoryStream.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemLinq = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Linq).GetReference(display: "System.Linq.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemLinqRef => s_systemLinq.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemLinqExpressions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Linq_Expressions).GetReference(display: "System.Linq.Expressions.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemLinqExpressionsRef => s_systemLinqExpressions.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemLinqParallel = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Linq_Parallel).GetReference(display: "System.Linq.Parallel.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemLinqParallelRef => s_systemLinqParallel.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemLinqQueryable = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Linq_Queryable).GetReference(display: "System.Linq.Queryable.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemLinqQueryableRef => s_systemLinqQueryable.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNet = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net).GetReference(display: "System.Net.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetRef => s_systemNet.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetHttp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_Http).GetReference(display: "System.Net.Http.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetHttpRef => s_systemNetHttp.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetNameResolution = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_NameResolution).GetReference(display: "System.Net.NameResolution.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetNameResolutionRef => s_systemNetNameResolution.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetNetworkInformation = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_NetworkInformation).GetReference(display: "System.Net.NetworkInformation.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetNetworkInformationRef => s_systemNetNetworkInformation.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetPing = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_Ping).GetReference(display: "System.Net.Ping.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetPingRef => s_systemNetPing.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetPrimitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_Primitives).GetReference(display: "System.Net.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetPrimitivesRef => s_systemNetPrimitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetRequests = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_Requests).GetReference(display: "System.Net.Requests.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetRequestsRef => s_systemNetRequests.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetSecurity = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_Security).GetReference(display: "System.Net.Security.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetSecurityRef => s_systemNetSecurity.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetSockets = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_Sockets).GetReference(display: "System.Net.Sockets.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetSocketsRef => s_systemNetSockets.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetWebHeaderCollection = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_WebHeaderCollection).GetReference(display: "System.Net.WebHeaderCollection.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetWebHeaderCollectionRef => s_systemNetWebHeaderCollection.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetWebSocketsClient = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_WebSockets_Client).GetReference(display: "System.Net.WebSockets.Client.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetWebSocketsClientRef => s_systemNetWebSocketsClient.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNetWebSockets = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Net_WebSockets).GetReference(display: "System.Net.WebSockets.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNetWebSocketsRef => s_systemNetWebSockets.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemNumerics = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Numerics).GetReference(display: "System.Numerics.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemNumericsRef => s_systemNumerics.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemObjectModel = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ObjectModel).GetReference(display: "System.ObjectModel.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemObjectModelRef => s_systemObjectModel.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemReflection = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Reflection).GetReference(display: "System.Reflection.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemReflectionRef => s_systemReflection.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemReflectionExtensions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Reflection_Extensions).GetReference(display: "System.Reflection.Extensions.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemReflectionExtensionsRef => s_systemReflectionExtensions.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemReflectionPrimitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Reflection_Primitives).GetReference(display: "System.Reflection.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemReflectionPrimitivesRef => s_systemReflectionPrimitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemResourcesReader = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Resources_Reader).GetReference(display: "System.Resources.Reader.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemResourcesReaderRef => s_systemResourcesReader.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemResourcesResourceManager = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Resources_ResourceManager).GetReference(display: "System.Resources.ResourceManager.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemResourcesResourceManagerRef => s_systemResourcesResourceManager.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemResourcesWriter = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Resources_Writer).GetReference(display: "System.Resources.Writer.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemResourcesWriterRef => s_systemResourcesWriter.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeCompilerServicesVisualC = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_CompilerServices_VisualC).GetReference(display: "System.Runtime.CompilerServices.VisualC.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeCompilerServicesVisualCRef => s_systemRuntimeCompilerServicesVisualC.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntime = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime).GetReference(display: "System.Runtime.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeRef => s_systemRuntime.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeExtensions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Extensions).GetReference(display: "System.Runtime.Extensions.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeExtensionsRef => s_systemRuntimeExtensions.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeHandles = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Handles).GetReference(display: "System.Runtime.Handles.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeHandlesRef => s_systemRuntimeHandles.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeInteropServices = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_InteropServices).GetReference(display: "System.Runtime.InteropServices.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeInteropServicesRef => s_systemRuntimeInteropServices.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeInteropServicesRuntimeInformation = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_InteropServices_RuntimeInformation).GetReference(display: "System.Runtime.InteropServices.RuntimeInformation.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeInteropServicesRuntimeInformationRef => s_systemRuntimeInteropServicesRuntimeInformation.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeNumerics = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Numerics).GetReference(display: "System.Runtime.Numerics.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeNumericsRef => s_systemRuntimeNumerics.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeSerialization = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Serialization).GetReference(display: "System.Runtime.Serialization.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeSerializationRef => s_systemRuntimeSerialization.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeSerializationFormatters = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Serialization_Formatters).GetReference(display: "System.Runtime.Serialization.Formatters.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeSerializationFormattersRef => s_systemRuntimeSerializationFormatters.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeSerializationJson = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Serialization_Json).GetReference(display: "System.Runtime.Serialization.Json.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeSerializationJsonRef => s_systemRuntimeSerializationJson.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeSerializationPrimitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Serialization_Primitives).GetReference(display: "System.Runtime.Serialization.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeSerializationPrimitivesRef => s_systemRuntimeSerializationPrimitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemRuntimeSerializationXml = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Runtime_Serialization_Xml).GetReference(display: "System.Runtime.Serialization.Xml.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemRuntimeSerializationXmlRef => s_systemRuntimeSerializationXml.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecurityClaims = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_Claims).GetReference(display: "System.Security.Claims.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecurityClaimsRef => s_systemSecurityClaims.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecurityCryptographyAlgorithms = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_Cryptography_Algorithms).GetReference(display: "System.Security.Cryptography.Algorithms.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecurityCryptographyAlgorithmsRef => s_systemSecurityCryptographyAlgorithms.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecurityCryptographyCsp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_Cryptography_Csp).GetReference(display: "System.Security.Cryptography.Csp.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecurityCryptographyCspRef => s_systemSecurityCryptographyCsp.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecurityCryptographyEncoding = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_Cryptography_Encoding).GetReference(display: "System.Security.Cryptography.Encoding.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecurityCryptographyEncodingRef => s_systemSecurityCryptographyEncoding.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecurityCryptographyPrimitives = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_Cryptography_Primitives).GetReference(display: "System.Security.Cryptography.Primitives.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecurityCryptographyPrimitivesRef => s_systemSecurityCryptographyPrimitives.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecurityCryptographyX509Certificates = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_Cryptography_X509Certificates).GetReference(display: "System.Security.Cryptography.X509Certificates.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecurityCryptographyX509CertificatesRef => s_systemSecurityCryptographyX509Certificates.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecurityPrincipal = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_Principal).GetReference(display: "System.Security.Principal.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecurityPrincipalRef => s_systemSecurityPrincipal.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemSecuritySecureString = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Security_SecureString).GetReference(display: "System.Security.SecureString.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemSecuritySecureStringRef => s_systemSecuritySecureString.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemServiceModelWeb = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ServiceModel_Web).GetReference(display: "System.ServiceModel.Web.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemServiceModelWebRef => s_systemServiceModelWeb.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemTextEncoding = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Text_Encoding).GetReference(display: "System.Text.Encoding.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemTextEncodingRef => s_systemTextEncoding.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemTextEncodingExtensions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Text_Encoding_Extensions).GetReference(display: "System.Text.Encoding.Extensions.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemTextEncodingExtensionsRef => s_systemTextEncodingExtensions.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemTextRegularExpressions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Text_RegularExpressions).GetReference(display: "System.Text.RegularExpressions.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemTextRegularExpressionsRef => s_systemTextRegularExpressions.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemThreading = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Threading).GetReference(display: "System.Threading.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingRef => s_systemThreading.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemThreadingOverlapped = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Threading_Overlapped).GetReference(display: "System.Threading.Overlapped.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingOverlappedRef => s_systemThreadingOverlapped.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemThreadingTasks = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Threading_Tasks).GetReference(display: "System.Threading.Tasks.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingTasksRef => s_systemThreadingTasks.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemThreadingTasksParallel = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Threading_Tasks_Parallel).GetReference(display: "System.Threading.Tasks.Parallel.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingTasksParallelRef => s_systemThreadingTasksParallel.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemThreadingThread = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Threading_Thread).GetReference(display: "System.Threading.Thread.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingThreadRef => s_systemThreadingThread.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemThreadingThreadPool = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Threading_ThreadPool).GetReference(display: "System.Threading.ThreadPool.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingThreadPoolRef => s_systemThreadingThreadPool.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemThreadingTimer = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Threading_Timer).GetReference(display: "System.Threading.Timer.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemThreadingTimerRef => s_systemThreadingTimer.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemTransactions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Transactions).GetReference(display: "System.Transactions.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemTransactionsRef => s_systemTransactions.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemValueTuple = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_ValueTuple).GetReference(display: "System.ValueTuple.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemValueTupleRef => s_systemValueTuple.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemWeb = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Web).GetReference(display: "System.Web.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemWebRef => s_systemWeb.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemWindows = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Windows).GetReference(display: "System.Windows.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemWindowsRef => s_systemWindows.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXml = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml).GetReference(display: "System.Xml.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlRef => s_systemXml.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlLinq = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_Linq).GetReference(display: "System.Xml.Linq.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlLinqRef => s_systemXmlLinq.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlReaderWriter = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_ReaderWriter).GetReference(display: "System.Xml.ReaderWriter.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlReaderWriterRef => s_systemXmlReaderWriter.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlSerialization = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_Serialization).GetReference(display: "System.Xml.Serialization.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlSerializationRef => s_systemXmlSerialization.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlXDocument = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_XDocument).GetReference(display: "System.Xml.XDocument.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlXDocumentRef => s_systemXmlXDocument.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlXmlDocument = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_XmlDocument).GetReference(display: "System.Xml.XmlDocument.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlXmlDocumentRef => s_systemXmlXmlDocument.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlXmlSerializer = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_XmlSerializer).GetReference(display: "System.Xml.XmlSerializer.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlXmlSerializerRef => s_systemXmlXmlSerializer.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlXPath = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_XPath).GetReference(display: "System.Xml.XPath.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlXPathRef => s_systemXmlXPath.Value;

        private static readonly Lazy<PortableExecutableReference> s_systemXmlXPathXDocument = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.NetFX.net461.System_Xml_XPath_XDocument).GetReference(display: "System.Xml.XPath.XDocument.dll (net461 ref)"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference SystemXmlXPathXDocumentRef => s_systemXmlXPathXDocument.Value;
    }

    public static class DiagnosticTests
    {
        public static class ErrTestLib01
        {
            private static readonly Lazy<PortableExecutableReference> s_errTestLib01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestLib01).GetReference(display: "ErrTestLib01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference dll => s_errTestLib01.Value;
        }

        public static class ErrTestLib02
        {
            private static readonly Lazy<PortableExecutableReference> s_errTestLib02 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestLib02).GetReference(display: "ErrTestLib02.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference dll => s_errTestLib02.Value;
        }

        public static class ErrTestLib11
        {
            private static readonly Lazy<PortableExecutableReference> s_errTestLib11 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestLib11).GetReference(display: "ErrTestLib11.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference dll => s_errTestLib11.Value;
        }

        public static class ErrTestMod01
        {
            private static readonly Lazy<PortableExecutableReference> s_errTestMod01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestMod01).GetReference(display: "ErrTestMod01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference dll => s_errTestMod01.Value;
        }

        public static class ErrTestMod02
        {
            private static readonly Lazy<PortableExecutableReference> s_errTestMod02 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestMod02).GetReference(display: "ErrTestMod02.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference dll => s_errTestMod02.Value;
        }

        public static class badresfile
        {
            private static readonly Lazy<PortableExecutableReference> s_badresfile = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.badresfile).GetReference(display: "badresfile.res"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference res => s_badresfile.Value;
        }
    }

    public static class SymbolsTests
    {
        private static readonly Lazy<PortableExecutableReference> s_mdTestLib1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.MDTestLib1).GetReference(display: "MDTestLib1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference MDTestLib1 => s_mdTestLib1.Value;

        private static readonly Lazy<PortableExecutableReference> s_mdTestLib2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.MDTestLib2).GetReference(display: "MDTestLib2.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference MDTestLib2 => s_mdTestLib2.Value;

        private static readonly Lazy<PortableExecutableReference> s_VBConversions = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.VBConversions).GetReference(display: "VBConversions.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference VBConversions => s_VBConversions.Value;

        private static readonly Lazy<PortableExecutableReference> s_withSpaces = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.With_Spaces).GetReference(display: "With Spaces.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference WithSpaces => s_withSpaces.Value;

        private static readonly Lazy<PortableExecutableReference> s_withSpacesModule = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.General.With_SpacesModule).GetReference(display: "With Spaces.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference WithSpacesModule => s_withSpacesModule.Value;

        private static readonly Lazy<PortableExecutableReference> s_inheritIComparable = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.InheritIComparable).GetReference(display: "InheritIComparable.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference InheritIComparable => s_inheritIComparable.Value;

        private static readonly Lazy<PortableExecutableReference> s_bigVisitor = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.BigVisitor).GetReference(display: "BigVisitor.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference BigVisitor => s_bigVisitor.Value;

        private static readonly Lazy<PortableExecutableReference> s_properties = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.Properties).GetReference(display: "Properties.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference Properties => s_properties.Value;

        private static readonly Lazy<PortableExecutableReference> s_propertiesWithByRef = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.PropertiesWithByRef).GetReference(display: "PropertiesWithByRef.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference PropertiesWithByRef => s_propertiesWithByRef.Value;

        private static readonly Lazy<PortableExecutableReference> s_indexers = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.Indexers).GetReference(display: "Indexers.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference Indexers => s_indexers.Value;

        private static readonly Lazy<PortableExecutableReference> s_events = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.Events).GetReference(display: "Events.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference Events => s_events.Value;

        public static class netModule
        {
            private static readonly Lazy<PortableExecutableReference> s_netModule1 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "netModule1.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference netModule1 => s_netModule1.Value;

            private static readonly Lazy<PortableExecutableReference> s_netModule2 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule2).GetReference(display: "netModule2.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference netModule2 => s_netModule2.Value;

            private static readonly Lazy<PortableExecutableReference> s_crossRefModule1 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1).GetReference(display: "CrossRefModule1.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CrossRefModule1 => s_crossRefModule1.Value;

            private static readonly Lazy<PortableExecutableReference> s_crossRefModule2 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2).GetReference(display: "CrossRefModule2.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CrossRefModule2 => s_crossRefModule2.Value;

            private static readonly Lazy<PortableExecutableReference> s_crossRefLib = new Lazy<PortableExecutableReference>(
                () => AssemblyMetadata.Create(
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefLib),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2)).GetReference(display: "CrossRefLib.dll"),
                LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CrossRefLib => s_crossRefLib.Value;

            private static readonly Lazy<PortableExecutableReference> s_hash_module = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.hash_module).GetReference(display: "hash_module.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference hash_module => s_hash_module.Value;

            private static readonly Lazy<PortableExecutableReference> s_x64COFF = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.x64COFF).GetReference(display: "x64COFF.obj"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference x64COFF => s_x64COFF.Value;
        }

        public static class V1
        {
            public static class MTTestLib1
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestLib1).GetReference(display: "MTTestLib1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_v1MTTestLib1.Value;
            }

            public static class MTTestModule1
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule1).GetReference(display: "MTTestModule1.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference netmodule => s_v1MTTestLib1.Value;
            }

            public static class MTTestLib2
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestLib2).GetReference(display: "MTTestLib2.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_v1MTTestLib2.Value;
            }

            public static class MTTestModule2
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule2).GetReference(display: "MTTestModule2.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference netmodule => s_v1MTTestLib1.Value;
            }
        }

        public static class V2
        {
            public static class MTTestLib1
            {
                private static readonly Lazy<PortableExecutableReference> s_v2MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestLib1).GetReference(display: "MTTestLib1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_v2MTTestLib1.Value;
            }

            public static class MTTestModule1
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestModule1).GetReference(display: "MTTestModule1.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference netmodule => s_v1MTTestLib1.Value;
            }

            public static class MTTestLib3
            {
                private static readonly Lazy<PortableExecutableReference> s_v2MTTestLib3 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestLib3).GetReference(display: "MTTestLib3.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_v2MTTestLib3.Value;
            }

            public static class MTTestModule3
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestModule3).GetReference(display: "MTTestModule3.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference netmodule => s_v1MTTestLib1.Value;
            }
        }

        public static class V3
        {
            public static class MTTestLib1
            {
                private static readonly Lazy<PortableExecutableReference> s_v3MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestLib1).GetReference(display: "MTTestLib1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_v3MTTestLib1.Value;
            }

            public static class MTTestModule1
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestModule1).GetReference(display: "MTTestModule1.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference netmodule => s_v1MTTestLib1.Value;
            }

            public static class MTTestLib4
            {
                private static readonly Lazy<PortableExecutableReference> s_v3MTTestLib4 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestLib4).GetReference(display: "MTTestLib4.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_v3MTTestLib4.Value;
            }

            public static class MTTestModule4
            {
                private static readonly Lazy<PortableExecutableReference> s_v1MTTestLib1 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestModule4).GetReference(display: "MTTestModule4.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference netmodule => s_v1MTTestLib1.Value;
            }
        }

        public static class MultiModule
        {
            private static readonly Lazy<PortableExecutableReference> s_assembly = new Lazy<PortableExecutableReference>(
                () => AssemblyMetadata.Create(
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModuleDll),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3)).GetReference(display: "MultiModule.dll"),
                LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Assembly => s_assembly.Value;

            private static readonly Lazy<PortableExecutableReference> s_mod2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2).GetReference(display: "mod2.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference mod2 => s_mod2.Value;

            private static readonly Lazy<PortableExecutableReference> s_mod3 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3).GetReference(display: "mod3.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference mod3 => s_mod3.Value;

            private static readonly Lazy<PortableExecutableReference> s_consumer = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.Consumer).GetReference(display: "Consumer.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Consumer => s_consumer.Value;
        }

        public static class DifferByCase
        {
            private static readonly Lazy<PortableExecutableReference> s_typeAndNamespaceDifferByCase = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase).GetReference(display: "TypeAndNamespaceDifferByCase.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference TypeAndNamespaceDifferByCase => s_typeAndNamespaceDifferByCase.Value;

            private static readonly Lazy<PortableExecutableReference> s_differByCaseConsumer = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.Consumer).GetReference(display: "Consumer.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Consumer => s_differByCaseConsumer.Value;

            private static readonly Lazy<PortableExecutableReference> s_csharpCaseSen = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.Consumer).GetReference(display: "CsharpCaseSen.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CsharpCaseSen => s_csharpCaseSen.Value;

            private static readonly Lazy<PortableExecutableReference> s_csharpDifferCaseOverloads = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.CSharpDifferCaseOverloads).GetReference(display: "CSharpDifferCaseOverloads.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CsharpDifferCaseOverloads => s_csharpDifferCaseOverloads.Value;
        }

        public static class CorLibrary
        {
            public static class GuidTest2
            {
                private static readonly Lazy<PortableExecutableReference> s_exe = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.GuidTest2).GetReference(display: "GuidTest2.exe"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference exe => s_exe.Value;
            }

            private static readonly Lazy<PortableExecutableReference> s_noMsCorLibRef = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.NoMsCorLibRef).GetReference(display: "NoMsCorLibRef.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference NoMsCorLibRef => s_noMsCorLibRef.Value;

            public static class FakeMsCorLib
            {
                private static readonly Lazy<PortableExecutableReference> s_dll = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.FakeMsCorLib).GetReference(display: "FakeMsCorLib.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_dll.Value;
            }
        }

        public static class CustomModifiers
        {
            public static class Modifiers
            {
                private static readonly Lazy<PortableExecutableReference> s_dll = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.Modifiers).GetReference(display: "Modifiers.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_dll.Value;

                private static readonly Lazy<PortableExecutableReference> s_module = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModifiersModule).GetReference(display: "Modifiers.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference netmodule => s_module.Value;
            }

            private static readonly Lazy<PortableExecutableReference> s_modoptTests = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModoptTests).GetReference(display: "ModoptTests.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ModoptTests => s_modoptTests.Value;

            public static class CppCli
            {
                private static readonly Lazy<PortableExecutableReference> s_dll = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.CppCli).GetReference(display: "CppCli.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_dll.Value;
            }

            public static class GenericMethodWithModifiers
            {
                private static readonly Lazy<PortableExecutableReference> s_dll = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.GenericMethodWithModifiers).GetReference(display: "GenericMethodWithModifiers.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_dll.Value;
            }
        }

        public static class Cyclic
        {
            public static class Cyclic1
            {
                private static readonly Lazy<PortableExecutableReference> s_cyclic1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Cyclic.Cyclic1).GetReference(display: "Cyclic1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_cyclic1.Value;
            }

            public static class Cyclic2
            {
                private static readonly Lazy<PortableExecutableReference> s_cyclic2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Cyclic.Cyclic2).GetReference(display: "Cyclic2.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_cyclic2.Value;
            }
        }

        public static class CyclicInheritance
        {
            private static readonly Lazy<PortableExecutableReference> s_class1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class1).GetReference(display: "Class1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Class1 => s_class1.Value;

            private static readonly Lazy<PortableExecutableReference> s_class2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class2).GetReference(display: "Class2.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Class2 => s_class2.Value;

            private static readonly Lazy<PortableExecutableReference> s_class3 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class3).GetReference(display: "Class3.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Class3 => s_class3.Value;
        }

        private static readonly Lazy<PortableExecutableReference> s_cycledStructs = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicStructure.cycledstructs).GetReference(display: "cycledstructs.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference CycledStructs => s_cycledStructs.Value;

        public static class RetargetingCycle
        {
            public static class V1
            {
                public static class ClassA
                {
                    private static readonly Lazy<PortableExecutableReference> s_classA = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV1.ClassA).GetReference(display: "ClassA.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                    public static PortableExecutableReference dll => s_classA.Value;
                }

                public static class ClassB
                {
                    private static readonly Lazy<PortableExecutableReference> s_classB = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV1.ClassB).GetReference(display: "ClassB.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
                    public static PortableExecutableReference netmodule => s_classB.Value;
                }
            }

            public static class V2
            {
                public static class ClassA
                {
                    private static readonly Lazy<PortableExecutableReference> s_classA = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV2.ClassA).GetReference(display: "ClassA.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                    public static PortableExecutableReference dll => s_classA.Value;
                }

                public static class ClassB
                {
                    private static readonly Lazy<PortableExecutableReference> s_classB = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV2.ClassB).GetReference(display: "ClassB.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                    public static PortableExecutableReference dll => s_classB.Value;
                }
            }
        }

        public static class Methods
        {
            private static readonly Lazy<PortableExecutableReference> s_CSMethods = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.CSMethods).GetReference(display: "CSMethods.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CSMethods => s_CSMethods.Value;

            private static readonly Lazy<PortableExecutableReference> s_VBMethods = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.VBMethods).GetReference(display: "VBMethods.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference VBMethods => s_VBMethods.Value;

            private static readonly Lazy<PortableExecutableReference> s_ILMethods = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.ILMethods).GetReference(display: "ILMethods.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ILMethods => s_ILMethods.Value;

            private static readonly Lazy<PortableExecutableReference> s_byRefReturn = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.ByRefReturn).GetReference(display: "ByRefReturn.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ByRefReturn => s_byRefReturn.Value;
        }

        public static class Fields
        {
            public static class CSFields
            {
                private static readonly Lazy<PortableExecutableReference> s_CSFields = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.CSFields).GetReference(display: "CSFields.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_CSFields.Value;
            }

            public static class VBFields
            {
                private static readonly Lazy<PortableExecutableReference> s_VBFields = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.VBFields).GetReference(display: "VBFields.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_VBFields.Value;
            }

            private static readonly Lazy<PortableExecutableReference> s_constantFields = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.ConstantFields).GetReference(display: "ConstantFields.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ConstantFields => s_constantFields.Value;
        }

        public static class MissingTypes
        {
            private static readonly Lazy<PortableExecutableReference> s_MDMissingType = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MDMissingType).GetReference(display: "MDMissingType.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MDMissingType => s_MDMissingType.Value;

            private static readonly Lazy<PortableExecutableReference> s_MDMissingTypeLib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MDMissingTypeLib).GetReference(display: "MDMissingTypeLib.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MDMissingTypeLib => s_MDMissingTypeLib.Value;

            private static readonly Lazy<PortableExecutableReference> s_missingTypesEquality1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality1).GetReference(display: "MissingTypesEquality1.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MissingTypesEquality1 => s_missingTypesEquality1.Value;

            private static readonly Lazy<PortableExecutableReference> s_missingTypesEquality2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality2).GetReference(display: "MissingTypesEquality2.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MissingTypesEquality2 => s_missingTypesEquality2.Value;

            private static readonly Lazy<PortableExecutableReference> s_CL2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.CL2).GetReference(display: "CL2.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CL2 => s_CL2.Value;

            private static readonly Lazy<PortableExecutableReference> s_CL3 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.CL3).GetReference(display: "CL3.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CL3 => s_CL3.Value;
        }

        public static class TypeForwarders
        {
            public static class TypeForwarder
            {
                private static readonly Lazy<PortableExecutableReference> s_typeForwarder2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarder).GetReference(display: "TypeForwarder.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_typeForwarder2.Value;
            }

            public static class TypeForwarderLib
            {
                private static readonly Lazy<PortableExecutableReference> s_typeForwarderLib2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarderLib).GetReference(display: "TypeForwarderLib.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_typeForwarderLib2.Value;
            }

            public static class TypeForwarderBase
            {
                private static readonly Lazy<PortableExecutableReference> s_typeForwarderBase2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarderBase).GetReference(display: "TypeForwarderBase.Dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference dll => s_typeForwarderBase2.Value;
            }
        }

        public static class MultiTargeting
        {
            private static readonly Lazy<PortableExecutableReference> s_source1Module = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source1Module).GetReference(display: "Source1Module.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Source1Module => s_source1Module.Value;

            private static readonly Lazy<PortableExecutableReference> s_source3Module = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source3Module).GetReference(display: "Source3Module.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Source3Module => s_source3Module.Value;

            private static readonly Lazy<PortableExecutableReference> s_source4Module = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source4Module).GetReference(display: "Source4Module.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Source4Module => s_source4Module.Value;

            private static readonly Lazy<PortableExecutableReference> s_source5Module = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source5Module).GetReference(display: "Source5Module.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Source5Module => s_source5Module.Value;

            private static readonly Lazy<PortableExecutableReference> s_source7Module = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source7Module).GetReference(display: "Source7Module.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Source7Module => s_source7Module.Value;
        }

        public static class NoPia
        {
            private static readonly Lazy<PortableExecutableReference> s_stdOle = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.ProprietaryPias.stdole).GetReference(display: "stdole.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference StdOle => s_stdOle.Value;

            private static readonly Lazy<PortableExecutableReference> s_pia1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia1).GetReference(display: "Pia1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Pia1 => s_pia1.Value;

            private static readonly Lazy<PortableExecutableReference> s_pia1Copy = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia1Copy).GetReference(display: "Pia1Copy.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Pia1Copy => s_pia1Copy.Value;

            private static readonly Lazy<PortableExecutableReference> s_pia2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia2).GetReference(display: "Pia2.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Pia2 => s_pia2.Value;

            private static readonly Lazy<PortableExecutableReference> s_pia3 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia3).GetReference(display: "Pia3.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Pia3 => s_pia3.Value;

            private static readonly Lazy<PortableExecutableReference> s_pia4 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia4).GetReference(display: "Pia4.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Pia4 => s_pia4.Value;

            private static readonly Lazy<PortableExecutableReference> s_pia5 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia5).GetReference(display: "Pia5.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Pia5 => s_pia5.Value;

            private static readonly Lazy<PortableExecutableReference> s_generalPia = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.GeneralPia).GetReference(display: "GeneralPia.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference GeneralPia => s_generalPia.Value;

            private static readonly Lazy<PortableExecutableReference> s_generalPiaCopy = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.GeneralPiaCopy).GetReference(display: "GeneralPiaCopy.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference GeneralPiaCopy => s_generalPiaCopy.Value;

            private static readonly Lazy<PortableExecutableReference> s_noPIAGenericsAsm1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.NoPIAGenerics1_Asm1).GetReference(display: "NoPIAGenerics1-Asm1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference NoPIAGenericsAsm1 => s_noPIAGenericsAsm1.Value;

            private static readonly Lazy<PortableExecutableReference> s_externalAsm1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.ExternalAsm1).GetReference(display: "ExternalAsm1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference ExternalAsm1 => s_externalAsm1.Value;

            private static readonly Lazy<PortableExecutableReference> s_library1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Library1).GetReference(display: "Library1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Library1 => s_library1.Value;

            private static readonly Lazy<PortableExecutableReference> s_library2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Library2).GetReference(display: "Library2.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Library2 => s_library2.Value;

            private static readonly Lazy<PortableExecutableReference> s_localTypes1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes1).GetReference(display: "LocalTypes1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference LocalTypes1 => s_localTypes1.Value;

            private static readonly Lazy<PortableExecutableReference> s_localTypes2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes2).GetReference(display: "LocalTypes2.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference LocalTypes2 => s_localTypes2.Value;

            private static readonly Lazy<PortableExecutableReference> s_localTypes3 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes3).GetReference(display: "LocalTypes3.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference LocalTypes3 => s_localTypes3.Value;

            private static readonly Lazy<PortableExecutableReference> s_A = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.A).GetReference(display: "A.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference A => s_A.Value;

            private static readonly Lazy<PortableExecutableReference> s_B = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.B).GetReference(display: "B.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference B => s_B.Value;

            private static readonly Lazy<PortableExecutableReference> s_C = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.C).GetReference(display: "C.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference C => s_C.Value;

            private static readonly Lazy<PortableExecutableReference> s_D = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.D).GetReference(display: "D.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference D => s_D.Value;

            public static class Microsoft
            {
                public static class VisualStudio
                {
                    private static readonly Lazy<PortableExecutableReference> s_missingPIAAttributes = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.MissingPIAAttributes).GetReference(display: "MicrosoftPIAAttributes.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                    public static PortableExecutableReference MissingPIAAttributes => s_missingPIAAttributes.Value;
                }
            }
        }

        public static class Interface
        {
            private static readonly Lazy<PortableExecutableReference> s_staticMethodInInterface = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Interface.StaticMethodInInterface).GetReference(display: "StaticMethodInInterface.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference StaticMethodInInterface => s_staticMethodInInterface.Value;

            private static readonly Lazy<PortableExecutableReference> s_MDInterfaceMapping = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Interface.MDInterfaceMapping).GetReference(display: "MDInterfaceMapping.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MDInterfaceMapping => s_MDInterfaceMapping.Value;
        }

        public static class MetadataCache
        {
            private static readonly Lazy<PortableExecutableReference> s_MDTestLib1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.MDTestLib1).GetReference(display: "MDTestLib1.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MDTestLib1 => s_MDTestLib1.Value;

            private static readonly Lazy<PortableExecutableReference> s_netModule1 = new Lazy<PortableExecutableReference>(
        () => ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "netModule1.netmodule"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference netModule1 => s_netModule1.Value;
        }

        public static class ExplicitInterfaceImplementation
        {
            public static class Methods
            {
                private static readonly Lazy<PortableExecutableReference> s_CSharp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.CSharpExplicitInterfaceImplementation).GetReference(display: "CSharpExplicitInterfaceImplementation.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference CSharp => s_CSharp.Value;

                private static readonly Lazy<PortableExecutableReference> s_IL = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.ILExplicitInterfaceImplementation).GetReference(display: "ILExplicitInterfaceImplementation.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference IL => s_IL.Value;
            }

            public static class Properties
            {
                private static readonly Lazy<PortableExecutableReference> s_CSharp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.CSharpExplicitInterfaceImplementationProperties).GetReference(display: "CSharpExplicitInterfaceImplementationProperties.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference CSharp => s_CSharp.Value;

                private static readonly Lazy<PortableExecutableReference> s_IL = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.ILExplicitInterfaceImplementationProperties).GetReference(display: "ILExplicitInterfaceImplementationProperties.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference IL => s_IL.Value;
            }

            public static class Events
            {
                private static readonly Lazy<PortableExecutableReference> s_CSharp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.CSharpExplicitInterfaceImplementationEvents).GetReference(display: "CSharpExplicitInterfaceImplementationEvents.dll"),
        LazyThreadSafetyMode.PublicationOnly);
                public static PortableExecutableReference CSharp => s_CSharp.Value;
            }
        }

        private static readonly Lazy<PortableExecutableReference> s_regress40025 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.Regress40025DLL).GetReference(display: "Regress40025DLL.dll"),
        LazyThreadSafetyMode.PublicationOnly);
        public static PortableExecutableReference Regress40025 => s_regress40025.Value;

        public static class WithEvents
        {
            private static readonly Lazy<PortableExecutableReference> s_simpleWithEvents = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.WithEvents.SimpleWithEvents).GetReference(display: "SimpleWithEvents.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference SimpleWithEvents => s_simpleWithEvents.Value;
        }

        public static class DelegateImplementation
        {
            private static readonly Lazy<PortableExecutableReference> s_delegatesWithoutInvoke = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.DelegatesWithoutInvoke).GetReference(display: "DelegatesWithoutInvoke.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference DelegatesWithoutInvoke => s_delegatesWithoutInvoke.Value;

            private static readonly Lazy<PortableExecutableReference> s_delegateByRefParamArray = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.DelegateByRefParamArray).GetReference(display: "DelegateByRefParamArray.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference DelegateByRefParamArray => s_delegateByRefParamArray.Value;
        }

        public static class Metadata
        {
            private static readonly Lazy<PortableExecutableReference> s_invalidCharactersInAssemblyName2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.InvalidCharactersInAssemblyName).GetReference(display: "InvalidCharactersInAssemblyName.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference InvalidCharactersInAssemblyName => s_invalidCharactersInAssemblyName2.Value;

            private static readonly Lazy<PortableExecutableReference> s_MDTestAttributeDefLib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib).GetReference(display: "MDTestAttributeDefLib.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MDTestAttributeDefLib => s_MDTestAttributeDefLib.Value;

            private static readonly Lazy<PortableExecutableReference> s_MDTestAttributeApplicationLib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib).GetReference(display: "MDTestAttributeApplicationLib.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference MDTestAttributeApplicationLib => s_MDTestAttributeApplicationLib.Value;

            private static readonly Lazy<PortableExecutableReference> s_attributeInterop01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeInterop01).GetReference(display: "AttributeInterop01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference AttributeInterop01 => s_attributeInterop01.Value;

            private static readonly Lazy<PortableExecutableReference> s_attributeInterop02 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeInterop02).GetReference(display: "AttributeInterop02.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference AttributeInterop02 => s_attributeInterop02.Value;

            private static readonly Lazy<PortableExecutableReference> s_attributeTestLib01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestLib01).GetReference(display: "AttributeTestLib01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference AttributeTestLib01 => s_attributeTestLib01.Value;

            private static readonly Lazy<PortableExecutableReference> s_attributeTestDef01 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01).GetReference(display: "AttributeTestDef01.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference AttributeTestDef01 => s_attributeTestDef01.Value;

            private static readonly Lazy<PortableExecutableReference> s_dynamicAttributeLib = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.DynamicAttribute).GetReference(display: "DynamicAttribute.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference DynamicAttributeLib => s_dynamicAttributeLib.Value;
        }

        public static class UseSiteErrors
        {
            private static readonly Lazy<PortableExecutableReference> s_unavailable = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.Unavailable).GetReference(display: "Unavailable.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference Unavailable => s_unavailable.Value;

            private static readonly Lazy<PortableExecutableReference> s_CSharp = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.CSharpErrors).GetReference(display: "CSharpErrors.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference CSharp => s_CSharp.Value;

            private static readonly Lazy<PortableExecutableReference> s_IL = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.ILErrors).GetReference(display: "ILErrors.dll"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference IL => s_IL.Value;
        }

        public static class Versioning
        {
            private static readonly Lazy<PortableExecutableReference> s_AR_SA = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.Culture_AR_SA).GetReference(display: "AR-SA"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference AR_SA => s_AR_SA.Value;

            private static readonly Lazy<PortableExecutableReference> s_EN_US = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.Culture_EN_US).GetReference(display: "EN-US"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference EN_US => s_EN_US.Value;

            private static readonly Lazy<PortableExecutableReference> s_C1 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.C1).GetReference(display: "C1"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference C1 => s_C1.Value;

            private static readonly Lazy<PortableExecutableReference> s_C2 = new Lazy<PortableExecutableReference>(
        () => AssemblyMetadata.CreateFromImage(TestResources.General.C2).GetReference(display: "C2"),
        LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference C2 => s_C2.Value;
        }
    }
}
