// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.IO;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Test.Resources.Proprietary;
using Roslyn.Test.Utilities;

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
                () => AssemblyMetadata.CreateFromImage(ProprietaryTestResources.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight).GetReference(display: "System.v5.0.5.0_silverlight.dll"),
                LazyThreadSafetyMode.PublicationOnly);
            public static PortableExecutableReference System => s_system.Value;
        }
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

        public static class @badresfile
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
        () => AssemblyMetadata.CreateFromImage(ProprietaryTestResources.ProprietaryPias.stdole).GetReference(display: "stdole.dll"),
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
