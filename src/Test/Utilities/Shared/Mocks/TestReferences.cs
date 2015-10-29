// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;

public static class TestReferences
{
    public static class MetadataTests
    {
        public static class NetModule01
        {
            private static PortableExecutableReference s_appCS;
            public static PortableExecutableReference AppCS
            {
                get
                {
                    if (s_appCS == null)
                    {
                        s_appCS = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.AppCS).GetReference(display: "AppCS");
                    }

                    return s_appCS;
                }
            }

            private static PortableExecutableReference s_moduleCS00;
            public static PortableExecutableReference ModuleCS00
            {
                get
                {
                    if (s_moduleCS00 == null)
                    {
                        s_moduleCS00 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference(display: "ModuleCS00.mod");
                    }

                    return s_moduleCS00;
                }
            }

            private static PortableExecutableReference s_moduleCS01;
            public static PortableExecutableReference ModuleCS01
            {
                get
                {
                    if (s_moduleCS01 == null)
                    {
                        s_moduleCS01 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS01).GetReference(display: "ModuleCS01.mod");
                    }

                    return s_moduleCS01;
                }
            }

            private static PortableExecutableReference s_moduleVB01;
            public static PortableExecutableReference ModuleVB01
            {
                get
                {
                    if (s_moduleVB01 == null)
                    {
                        s_moduleVB01 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleVB01).GetReference(display: "ModuleVB01.mod");
                    }

                    return s_moduleVB01;
                }
            }
        }

        public static class InterfaceAndClass
        {
            private static PortableExecutableReference s_CSClasses01;
            public static PortableExecutableReference CSClasses01
            {
                get
                {
                    if (s_CSClasses01 == null)
                    {
                        s_CSClasses01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).GetReference(display: "CSClasses01.dll");
                    }

                    return s_CSClasses01;
                }
            }

            private static PortableExecutableReference s_CSInterfaces01;
            public static PortableExecutableReference CSInterfaces01
            {
                get
                {
                    if (s_CSInterfaces01 == null)
                    {
                        s_CSInterfaces01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01).GetReference(display: "CSInterfaces01.dll");
                    }

                    return s_CSInterfaces01;
                }
            }

            private static PortableExecutableReference s_VBClasses01;
            public static PortableExecutableReference VBClasses01
            {
                get
                {
                    if (s_VBClasses01 == null)
                    {
                        s_VBClasses01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBClasses01).GetReference(display: "VBClasses01.dll");
                    }

                    return s_VBClasses01;
                }
            }

            private static PortableExecutableReference s_VBClasses02;
            public static PortableExecutableReference VBClasses02
            {
                get
                {
                    if (s_VBClasses02 == null)
                    {
                        s_VBClasses02 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBClasses02).GetReference(display: "VBClasses02.dll");
                    }

                    return s_VBClasses02;
                }
            }

            private static PortableExecutableReference s_VBInterfaces01;
            public static PortableExecutableReference VBInterfaces01
            {
                get
                {
                    if (s_VBInterfaces01 == null)
                    {
                        s_VBInterfaces01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBInterfaces01).GetReference(display: "VBInterfaces01.dll");
                    }

                    return s_VBInterfaces01;
                }
            }
        }
    }

    public static class NetFx
    {
        public static class silverlight_v5_0_5_0
        {
            private static PortableExecutableReference s_system;
            public static PortableExecutableReference System
            {
                get
                {
                    if (s_system == null)
                    {
                        s_system = AssemblyMetadata.CreateFromImage(TestResources.NetFX.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight).GetReference(display: "System.v5.0.5.0_silverlight.dll");
                    }

                    return s_system;
                }
            }
        }

        public static class v4_0_21006
        {
            private static PortableExecutableReference s_mscorlib;
            public static PortableExecutableReference mscorlib
            {
                get
                {
                    if (s_mscorlib == null)
                    {
                        s_mscorlib = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_21006.mscorlib).GetReference(display: "mscorlib.dll");
                    }

                    return s_mscorlib;
                }
            }
        }

        public static class v2_0_50727
        {
            private static PortableExecutableReference s_mscorlib;
            public static PortableExecutableReference mscorlib
            {
                get
                {
                    if (s_mscorlib == null)
                    {
                        s_mscorlib = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.mscorlib).GetReference(display: "mscorlib, v2.0.50727");
                    }

                    return s_mscorlib;
                }
            }

            private static PortableExecutableReference s_system;
            public static PortableExecutableReference System
            {
                get
                {
                    if (s_system == null)
                    {
                        s_system = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.System).GetReference(display: "System.dll");
                    }

                    return s_system;
                }
            }

            private static PortableExecutableReference s_microsoft_VisualBasic;
            public static PortableExecutableReference Microsoft_VisualBasic
            {
                get
                {
                    if (s_microsoft_VisualBasic == null)
                    {
                        s_microsoft_VisualBasic = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v2_0_50727.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.dll");
                    }

                    return s_microsoft_VisualBasic;
                }
            }
        }

        public static class v3_5_30729
        {
            private static PortableExecutableReference s_systemCore;
            public static PortableExecutableReference SystemCore
            {
                get
                {
                    if (s_systemCore == null)
                    {
                        s_systemCore = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v3_5_30729.System_Core_v3_5_30729.AsImmutableOrNull()).GetReference(display: "System.Core, v3.5.30729");
                    }

                    return s_systemCore;
                }
            }
        }

        public static class v4_0_30319
        {
            private static PortableExecutableReference s_mscorlib;
            public static PortableExecutableReference mscorlib
            {
                get
                {
                    if (s_mscorlib == null)
                    {
                        s_mscorlib = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_21006.mscorlib).GetReference(filePath: @"R:\v4_0_30319\mscorlib.dll");
                    }

                    return s_mscorlib;
                }
            }

            private static PortableExecutableReference s_system_Core;
            public static PortableExecutableReference System_Core
            {
                get
                {
                    if (s_system_Core == null)
                    {
                        s_system_Core = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Core).GetReference(filePath: @"R:\v4_0_30319\System.Core.dll");
                    }

                    return s_system_Core;
                }
            }

            private static PortableExecutableReference s_system_Configuration;
            public static PortableExecutableReference System_Configuration
            {
                get
                {
                    if (s_system_Configuration == null)
                    {
                        s_system_Configuration = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Configuration).GetReference(filePath: @"R:\v4_0_30319\System.Configuration.dll");
                    }

                    return s_system_Configuration;
                }
            }

            private static PortableExecutableReference s_system;
            public static PortableExecutableReference System
            {
                get
                {
                    if (s_system == null)
                    {
                        s_system = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System).GetReference(filePath: @"R:\v4_0_30319\System.dll", display: "System.dll");
                    }

                    return s_system;
                }
            }

            private static PortableExecutableReference s_system_Data;
            public static PortableExecutableReference System_Data
            {
                get
                {
                    if (s_system_Data == null)
                    {
                        s_system_Data = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Data).GetReference(filePath: @"R:\v4_0_30319\System.Data.dll");
                    }

                    return s_system_Data;
                }
            }

            private static PortableExecutableReference s_system_Xml;
            public static PortableExecutableReference System_Xml
            {
                get
                {
                    if (s_system_Xml == null)
                    {
                        s_system_Xml = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Xml).GetReference(filePath: @"R:\v4_0_30319\System.Xml.dll");
                    }

                    return s_system_Xml;
                }
            }

            private static PortableExecutableReference s_system_Xml_Linq;
            public static PortableExecutableReference System_Xml_Linq
            {
                get
                {
                    if (s_system_Xml_Linq == null)
                    {
                        s_system_Xml_Linq = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Xml_Linq).GetReference(filePath: @"R:\v4_0_30319\System.Xml.Linq.dll");
                    }

                    return s_system_Xml_Linq;
                }
            }

            private static PortableExecutableReference s_system_Windows_Forms;
            public static PortableExecutableReference System_Windows_Forms
            {
                get
                {
                    if (s_system_Windows_Forms == null)
                    {
                        s_system_Windows_Forms = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Windows_Forms).GetReference(filePath: @"R:\v4_0_30319\System.Windows.Forms.dll");
                    }

                    return s_system_Windows_Forms;
                }
            }

            private static PortableExecutableReference s_microsoft_CSharp;
            public static PortableExecutableReference Microsoft_CSharp
            {
                get
                {
                    if (s_microsoft_CSharp == null)
                    {
                        s_microsoft_CSharp = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_CSharp).GetReference(filePath: @"R:\v4_0_30319\Microsoft.CSharp.dll");
                    }

                    return s_microsoft_CSharp;
                }
            }

            private static PortableExecutableReference s_microsoft_VisualBasic;
            public static PortableExecutableReference Microsoft_VisualBasic
            {
                get
                {
                    if (s_microsoft_VisualBasic == null)
                    {
                        s_microsoft_VisualBasic = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_VisualBasic).GetReference(filePath: @"R:\v4_0_30319\Microsoft.VisualBasic.dll");
                    }

                    return s_microsoft_VisualBasic;
                }
            }

            private static PortableExecutableReference s_microsoft_JScript;
            public static PortableExecutableReference Microsoft_JScript
            {
                get
                {
                    if (s_microsoft_JScript == null)
                    {
                        s_microsoft_JScript = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.Microsoft_JScript).GetReference(display: "Microsoft.JScript.dll");
                    }

                    return s_microsoft_JScript;
                }
            }

            private static PortableExecutableReference s_system_ComponentModel_Composition;
            public static PortableExecutableReference System_ComponentModel_Composition
            {
                get
                {
                    if (s_system_ComponentModel_Composition == null)
                    {
                        s_system_ComponentModel_Composition = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_ComponentModel_Composition).GetReference(display: "System.ComponentModel.Composition.dll");
                    }

                    return s_system_ComponentModel_Composition;
                }
            }

            private static PortableExecutableReference s_system_Web_Services;
            public static PortableExecutableReference System_Web_Services
            {
                get
                {
                    if (s_system_Web_Services == null)
                    {
                        s_system_Web_Services = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_Web_Services).GetReference(display: "System.Web.Services.dll");
                    }

                    return s_system_Web_Services;
                }
            }

            public static class System_EnterpriseServices
            {
                private static PortableExecutableReference s_system_EnterpriseServices;

                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_system_EnterpriseServices == null)
                        {
                            s_system_EnterpriseServices = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319.System_EnterpriseServices).GetReference(display: "System.EnterpriseServices.dll");
                        }

                        return s_system_EnterpriseServices;
                    }
                }
            }

            private static PortableExecutableReference s_system_Runtime_Serialization;
            public static PortableExecutableReference System_Runtime_Serialization
            {
                get
                {
                    if (s_system_Runtime_Serialization == null)
                    {
                        s_system_Runtime_Serialization = AssemblyMetadata.CreateFromImage(TestResources.NetFX.v4_0_30319_17929.System_Runtime_Serialization).GetReference(display: "System.Runtime.Serialization.dll");
                    }

                    return s_system_Runtime_Serialization;
                }
            }
        }
    }

    public static class DiagnosticTests
    {
        public static class ErrTestLib01
        {
            private static PortableExecutableReference s_errTestLib01;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (s_errTestLib01 == null)
                    {
                        s_errTestLib01 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestLib01).GetReference(display: "ErrTestLib01.dll");
                    }

                    return s_errTestLib01;
                }
            }
        }

        public static class ErrTestLib02
        {
            private static PortableExecutableReference s_errTestLib02;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (s_errTestLib02 == null)
                    {
                        s_errTestLib02 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestLib02).GetReference(display: "ErrTestLib02.dll");
                    }

                    return s_errTestLib02;
                }
            }
        }

        public static class ErrTestLib11
        {
            private static PortableExecutableReference s_errTestLib11;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (s_errTestLib11 == null)
                    {
                        s_errTestLib11 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestLib11).GetReference(display: "ErrTestLib11.dll");
                    }

                    return s_errTestLib11;
                }
            }
        }

        public static class ErrTestMod01
        {
            private static PortableExecutableReference s_errTestMod01;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (s_errTestMod01 == null)
                    {
                        s_errTestMod01 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestMod01).GetReference(display: "ErrTestMod01.dll");
                    }

                    return s_errTestMod01;
                }
            }
        }

        public static class ErrTestMod02
        {
            private static PortableExecutableReference s_errTestMod02;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (s_errTestMod02 == null)
                    {
                        s_errTestMod02 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.ErrTestMod02).GetReference(display: "ErrTestMod02.dll");
                    }

                    return s_errTestMod02;
                }
            }
        }

        public static class badresfile
        {
            private static PortableExecutableReference s_badresfile;
            public static PortableExecutableReference res
            {
                get
                {
                    if (s_badresfile == null)
                    {
                        s_badresfile = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.badresfile).GetReference(display: "badresfile.res");
                    }

                    return s_badresfile;
                }
            }
        }
    }

    public static class SymbolsTests
    {
        private static PortableExecutableReference s_mdTestLib1;
        public static PortableExecutableReference MDTestLib1
        {
            get
            {
                if (s_mdTestLib1 == null)
                {
                    s_mdTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.General.MDTestLib1).GetReference(display: "MDTestLib1.dll");
                }

                return s_mdTestLib1;
            }
        }

        private static PortableExecutableReference s_mdTestLib2;
        public static PortableExecutableReference MDTestLib2
        {
            get
            {
                if (s_mdTestLib2 == null)
                {
                    s_mdTestLib2 = AssemblyMetadata.CreateFromImage(TestResources.General.MDTestLib2).GetReference(display: "MDTestLib2.dll");
                }

                return s_mdTestLib2;
            }
        }

        private static PortableExecutableReference s_VBConversions;
        public static PortableExecutableReference VBConversions
        {
            get
            {
                if (s_VBConversions == null)
                {
                    s_VBConversions = AssemblyMetadata.CreateFromImage(TestResources.General.VBConversions).GetReference(display: "VBConversions.dll");
                }

                return s_VBConversions;
            }
        }

        private static PortableExecutableReference s_withSpaces;
        public static PortableExecutableReference WithSpaces
        {
            get
            {
                if (s_withSpaces == null)
                {
                    s_withSpaces = AssemblyMetadata.CreateFromImage(TestResources.General.With_Spaces).GetReference(display: "With Spaces.dll");
                }

                return s_withSpaces;
            }
        }

        private static PortableExecutableReference s_withSpacesModule;
        public static PortableExecutableReference WithSpacesModule
        {
            get
            {
                if (s_withSpacesModule == null)
                {
                    s_withSpacesModule = ModuleMetadata.CreateFromImage(TestResources.General.With_SpacesModule).GetReference(display: "With Spaces.netmodule");
                }

                return s_withSpacesModule;
            }
        }

        private static PortableExecutableReference s_inheritIComparable;
        public static PortableExecutableReference InheritIComparable
        {
            get
            {
                if (s_inheritIComparable == null)
                {
                    s_inheritIComparable = AssemblyMetadata.CreateFromImage(TestResources.General.InheritIComparable).GetReference(display: "InheritIComparable.dll");
                }

                return s_inheritIComparable;
            }
        }

        private static PortableExecutableReference s_bigVisitor;
        public static PortableExecutableReference BigVisitor
        {
            get
            {
                if (s_bigVisitor == null)
                {
                    s_bigVisitor = AssemblyMetadata.CreateFromImage(TestResources.General.BigVisitor).GetReference(display: "BigVisitor.dll");
                }

                return s_bigVisitor;
            }
        }

        private static PortableExecutableReference s_properties;
        public static PortableExecutableReference Properties
        {
            get
            {
                if (s_properties == null)
                {
                    s_properties = AssemblyMetadata.CreateFromImage(TestResources.General.Properties).GetReference(display: "Properties.dll");
                }

                return s_properties;
            }
        }

        private static PortableExecutableReference s_propertiesWithByRef;
        public static PortableExecutableReference PropertiesWithByRef
        {
            get
            {
                if (s_propertiesWithByRef == null)
                {
                    s_propertiesWithByRef = AssemblyMetadata.CreateFromImage(TestResources.General.PropertiesWithByRef).GetReference(display: "PropertiesWithByRef.dll");
                }

                return s_propertiesWithByRef;
            }
        }

        private static PortableExecutableReference s_indexers;
        public static PortableExecutableReference Indexers
        {
            get
            {
                if (s_indexers == null)
                {
                    s_indexers = AssemblyMetadata.CreateFromImage(TestResources.General.Indexers).GetReference(display: "Indexers.dll");
                }

                return s_indexers;
            }
        }

        private static PortableExecutableReference s_events;
        public static PortableExecutableReference Events
        {
            get
            {
                if (s_events == null)
                {
                    s_events = AssemblyMetadata.CreateFromImage(TestResources.General.Events).GetReference(display: "Events.dll");
                }

                return s_events;
            }
        }

        public static class netModule
        {
            private static PortableExecutableReference s_netModule1;
            public static PortableExecutableReference netModule1
            {
                get
                {
                    if (s_netModule1 == null)
                    {
                        s_netModule1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "netModule1.netmodule");
                    }

                    return s_netModule1;
                }
            }

            private static PortableExecutableReference s_netModule2;
            public static PortableExecutableReference netModule2
            {
                get
                {
                    if (s_netModule2 == null)
                    {
                        s_netModule2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule2).GetReference(display: "netModule2.netmodule");
                    }

                    return s_netModule2;
                }
            }

            private static PortableExecutableReference s_crossRefModule1;
            public static PortableExecutableReference CrossRefModule1
            {
                get
                {
                    if (s_crossRefModule1 == null)
                    {
                        s_crossRefModule1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1).GetReference(display: "CrossRefModule1.netmodule");
                    }

                    return s_crossRefModule1;
                }
            }

            private static PortableExecutableReference s_crossRefModule2;
            public static PortableExecutableReference CrossRefModule2
            {
                get
                {
                    if (s_crossRefModule2 == null)
                    {
                        s_crossRefModule2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2).GetReference(display: "CrossRefModule2.netmodule");
                    }

                    return s_crossRefModule2;
                }
            }

            private static PortableExecutableReference s_crossRefLib;
            public static PortableExecutableReference CrossRefLib
            {
                get
                {
                    if (s_crossRefLib == null)
                    {
                        s_crossRefLib = AssemblyMetadata.Create(
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefLib),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2)).GetReference(display: "CrossRefLib.dll");
                    }

                    return s_crossRefLib;
                }
            }

            private static PortableExecutableReference s_hash_module;
            public static PortableExecutableReference hash_module
            {
                get
                {
                    if (s_hash_module == null)
                    {
                        s_hash_module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.hash_module).GetReference(display: "hash_module.netmodule");
                    }

                    return s_hash_module;
                }
            }

            private static PortableExecutableReference s_x64COFF;
            public static PortableExecutableReference x64COFF
            {
                get
                {
                    if (s_x64COFF == null)
                    {
                        s_x64COFF = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.x64COFF).GetReference(display: "x64COFF.obj");
                    }

                    return s_x64COFF;
                }
            }
        }

        public static class V1
        {
            public static class MTTestLib1
            {
                private static PortableExecutableReference s_v1MTTestLib1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_v1MTTestLib1 == null)
                        {
                            s_v1MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestLib1).GetReference(display: "MTTestLib1.dll");
                        }

                        return s_v1MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static PortableExecutableReference s_v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (s_v1MTTestLib1 == null)
                        {
                            s_v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule1).GetReference(display: "MTTestModule1.netmodule");
                        }

                        return s_v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib2
            {
                private static PortableExecutableReference s_v1MTTestLib2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_v1MTTestLib2 == null)
                        {
                            s_v1MTTestLib2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestLib2).GetReference(display: "MTTestLib2.dll");
                        }

                        return s_v1MTTestLib2;
                    }
                }
            }

            public static class MTTestModule2
            {
                private static PortableExecutableReference s_v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (s_v1MTTestLib1 == null)
                        {
                            s_v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule2).GetReference(display: "MTTestModule2.netmodule");
                        }

                        return s_v1MTTestLib1;
                    }
                }
            }
        }

        public static class V2
        {
            public static class MTTestLib1
            {
                private static PortableExecutableReference s_v2MTTestLib1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_v2MTTestLib1 == null)
                        {
                            s_v2MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestLib1).GetReference(display: "MTTestLib1.dll");
                        }

                        return s_v2MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static PortableExecutableReference s_v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (s_v1MTTestLib1 == null)
                        {
                            s_v1MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestModule1).GetReference(display: "MTTestModule1.netmodule");
                        }

                        return s_v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib3
            {
                private static PortableExecutableReference s_v2MTTestLib3;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_v2MTTestLib3 == null)
                        {
                            s_v2MTTestLib3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestLib3).GetReference(display: "MTTestLib3.dll");
                        }

                        return s_v2MTTestLib3;
                    }
                }
            }

            public static class MTTestModule3
            {
                private static PortableExecutableReference s_v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (s_v1MTTestLib1 == null)
                        {
                            s_v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestModule3).GetReference(display: "MTTestModule3.netmodule");
                        }

                        return s_v1MTTestLib1;
                    }
                }
            }
        }

        public static class V3
        {
            public static class MTTestLib1
            {
                private static PortableExecutableReference s_v3MTTestLib1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_v3MTTestLib1 == null)
                        {
                            s_v3MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestLib1).GetReference(display: "MTTestLib1.dll");
                        }

                        return s_v3MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static PortableExecutableReference s_v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (s_v1MTTestLib1 == null)
                        {
                            s_v1MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestModule1).GetReference(display: "MTTestModule1.netmodule");
                        }

                        return s_v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib4
            {
                private static PortableExecutableReference s_v3MTTestLib4;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_v3MTTestLib4 == null)
                        {
                            s_v3MTTestLib4 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestLib4).GetReference(display: "MTTestLib4.dll");
                        }

                        return s_v3MTTestLib4;
                    }
                }
            }

            public static class MTTestModule4
            {
                private static PortableExecutableReference s_v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (s_v1MTTestLib1 == null)
                        {
                            s_v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestModule4).GetReference(display: "MTTestModule4.netmodule");
                        }

                        return s_v1MTTestLib1;
                    }
                }
            }
        }

        public static class MultiModule
        {
            private static PortableExecutableReference s_assembly;
            public static PortableExecutableReference Assembly
            {
                get
                {
                    if (s_assembly == null)
                    {
                        s_assembly = AssemblyMetadata.Create(
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModuleDll),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3)).GetReference(display: "MultiModule.dll");
                    }

                    return s_assembly;
                }
            }

            private static PortableExecutableReference s_mod2;
            public static PortableExecutableReference mod2
            {
                get
                {
                    if (s_mod2 == null)
                    {
                        s_mod2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2).GetReference(display: "mod2.netmodule");
                    }

                    return s_mod2;
                }
            }

            private static PortableExecutableReference s_mod3;
            public static PortableExecutableReference mod3
            {
                get
                {
                    if (s_mod3 == null)
                    {
                        s_mod3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3).GetReference(display: "mod3.netmodule");
                    }

                    return s_mod3;
                }
            }

            private static PortableExecutableReference s_consumer;
            public static PortableExecutableReference Consumer
            {
                get
                {
                    if (s_consumer == null)
                    {
                        s_consumer = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.Consumer).GetReference(display: "Consumer.dll");
                    }

                    return s_consumer;
                }
            }
        }

        public static class DifferByCase
        {
            private static PortableExecutableReference s_typeAndNamespaceDifferByCase;
            public static PortableExecutableReference TypeAndNamespaceDifferByCase
            {
                get
                {
                    if (s_typeAndNamespaceDifferByCase == null)
                    {
                        s_typeAndNamespaceDifferByCase = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase).GetReference(display: "TypeAndNamespaceDifferByCase.dll");
                    }

                    return s_typeAndNamespaceDifferByCase;
                }
            }

            private static PortableExecutableReference s_differByCaseConsumer;
            public static PortableExecutableReference Consumer
            {
                get
                {
                    if (s_differByCaseConsumer == null)
                    {
                        s_differByCaseConsumer = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.Consumer).GetReference(display: "Consumer.dll");
                    }

                    return s_differByCaseConsumer;
                }
            }

            private static PortableExecutableReference s_csharpCaseSen;
            public static PortableExecutableReference CsharpCaseSen
            {
                get
                {
                    if (s_csharpCaseSen == null)
                    {
                        s_csharpCaseSen = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.Consumer).GetReference(display: "CsharpCaseSen.dll");
                    }

                    return s_csharpCaseSen;
                }
            }

            private static PortableExecutableReference s_csharpDifferCaseOverloads;
            public static PortableExecutableReference CsharpDifferCaseOverloads
            {
                get
                {
                    if (s_csharpDifferCaseOverloads == null)
                    {
                        s_csharpDifferCaseOverloads = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.CSharpDifferCaseOverloads).GetReference(display: "CSharpDifferCaseOverloads.dll");
                    }

                    return s_csharpDifferCaseOverloads;
                }
            }
        }

        public static class CorLibrary
        {
            public static class GuidTest2
            {
                private static PortableExecutableReference s_exe;
                public static PortableExecutableReference exe
                {
                    get
                    {
                        if (s_exe == null)
                        {
                            s_exe = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.GuidTest2).GetReference(display: "GuidTest2.exe");
                        }

                        return s_exe;
                    }
                }
            }

            private static PortableExecutableReference s_noMsCorLibRef;
            public static PortableExecutableReference NoMsCorLibRef
            {
                get
                {
                    if (s_noMsCorLibRef == null)
                    {
                        s_noMsCorLibRef = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.NoMsCorLibRef).GetReference(display: "NoMsCorLibRef.dll");
                    }

                    return s_noMsCorLibRef;
                }
            }

            public static class FakeMsCorLib
            {
                private static PortableExecutableReference s_dll;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_dll == null)
                        {
                            s_dll = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.FakeMsCorLib).GetReference(display: "FakeMsCorLib.dll");
                        }

                        return s_dll;
                    }
                }
            }
        }

        public static class CustomModifiers
        {
            public static class Modifiers
            {
                private static PortableExecutableReference s_dll;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_dll == null)
                        {
                            s_dll = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.Modifiers).GetReference(display: "Modifiers.dll");
                        }

                        return s_dll;
                    }
                }

                private static PortableExecutableReference s_module;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (s_module == null)
                        {
                            s_module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModifiersModule).GetReference(display: "Modifiers.netmodule");
                        }

                        return s_module;
                    }
                }
            }

            private static PortableExecutableReference s_modoptTests;
            public static PortableExecutableReference ModoptTests
            {
                get
                {
                    if (s_modoptTests == null)
                    {
                        s_modoptTests = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModoptTests).GetReference(display: "ModoptTests.dll");
                    }

                    return s_modoptTests;
                }
            }

            public static class CppCli
            {
                private static PortableExecutableReference s_dll;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_dll == null)
                        {
                            s_dll = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.CppCli).GetReference(display: "CppCli.dll");
                        }

                        return s_dll;
                    }
                }
            }
        }

        public static class Cyclic
        {
            public static class Cyclic1
            {
                private static PortableExecutableReference s_cyclic1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_cyclic1 == null)
                        {
                            s_cyclic1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Cyclic.Cyclic1).GetReference(display: "Cyclic1.dll");
                        }

                        return s_cyclic1;
                    }
                }
            }

            public static class Cyclic2
            {
                private static PortableExecutableReference s_cyclic2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_cyclic2 == null)
                        {
                            s_cyclic2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Cyclic.Cyclic2).GetReference(display: "Cyclic2.dll");
                        }

                        return s_cyclic2;
                    }
                }
            }
        }

        public static class CyclicInheritance
        {
            private static PortableExecutableReference s_class1;
            public static PortableExecutableReference Class1
            {
                get
                {
                    if (s_class1 == null)
                    {
                        s_class1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class1).GetReference(display: "Class1.dll");
                    }

                    return s_class1;
                }
            }

            private static PortableExecutableReference s_class2;
            public static PortableExecutableReference Class2
            {
                get
                {
                    if (s_class2 == null)
                    {
                        s_class2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class2).GetReference(display: "Class2.dll");
                    }

                    return s_class2;
                }
            }

            private static PortableExecutableReference s_class3;
            public static PortableExecutableReference Class3
            {
                get
                {
                    if (s_class3 == null)
                    {
                        s_class3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class3).GetReference(display: "Class3.dll");
                    }

                    return s_class3;
                }
            }
        }

        private static PortableExecutableReference s_cycledStructs;
        public static PortableExecutableReference CycledStructs
        {
            get
            {
                if (s_cycledStructs == null)
                {
                    s_cycledStructs = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicStructure.cycledstructs).GetReference(display: "cycledstructs.dll");
                }

                return s_cycledStructs;
            }
        }

        public static class RetargetingCycle
        {
            public static class V1
            {
                public static class ClassA
                {
                    private static PortableExecutableReference s_classA;
                    public static PortableExecutableReference dll
                    {
                        get
                        {
                            if (s_classA == null)
                            {
                                s_classA = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV1.ClassA).GetReference(display: "ClassA.dll");
                            }

                            return s_classA;
                        }
                    }
                }

                public static class ClassB
                {
                    private static PortableExecutableReference s_classB;
                    public static PortableExecutableReference netmodule
                    {
                        get
                        {
                            if (s_classB == null)
                            {
                                s_classB = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV1.ClassB).GetReference(display: "ClassB.netmodule");
                            }

                            return s_classB;
                        }
                    }
                }
            }

            public static class V2
            {
                public static class ClassA
                {
                    private static PortableExecutableReference s_classA;
                    public static PortableExecutableReference dll
                    {
                        get
                        {
                            if (s_classA == null)
                            {
                                s_classA = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV2.ClassA).GetReference(display: "ClassA.dll");
                            }

                            return s_classA;
                        }
                    }
                }

                public static class ClassB
                {
                    private static PortableExecutableReference s_classB;
                    public static PortableExecutableReference dll
                    {
                        get
                        {
                            if (s_classB == null)
                            {
                                s_classB = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetargetingCycle.RetV2.ClassB).GetReference(display: "ClassB.dll");
                            }

                            return s_classB;
                        }
                    }
                }
            }
        }

        public static class Methods
        {
            private static PortableExecutableReference s_CSMethods;
            public static PortableExecutableReference CSMethods
            {
                get
                {
                    if (s_CSMethods == null)
                    {
                        s_CSMethods = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.CSMethods).GetReference(display: "CSMethods.Dll");
                    }

                    return s_CSMethods;
                }
            }

            private static PortableExecutableReference s_VBMethods;
            public static PortableExecutableReference VBMethods
            {
                get
                {
                    if (s_VBMethods == null)
                    {
                        s_VBMethods = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.VBMethods).GetReference(display: "VBMethods.Dll");
                    }

                    return s_VBMethods;
                }
            }

            private static PortableExecutableReference s_ILMethods;
            public static PortableExecutableReference ILMethods
            {
                get
                {
                    if (s_ILMethods == null)
                    {
                        s_ILMethods = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.ILMethods).GetReference(display: "ILMethods.Dll");
                    }

                    return s_ILMethods;
                }
            }

            private static PortableExecutableReference s_byRefReturn;
            public static PortableExecutableReference ByRefReturn
            {
                get
                {
                    if (s_byRefReturn == null)
                    {
                        s_byRefReturn = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.ByRefReturn).GetReference(display: "ByRefReturn.Dll");
                    }

                    return s_byRefReturn;
                }
            }
        }

        public static class Fields
        {
            public static class CSFields
            {
                private static PortableExecutableReference s_CSFields;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_CSFields == null)
                        {
                            s_CSFields = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.CSFields).GetReference(display: "CSFields.Dll");
                        }

                        return s_CSFields;
                    }
                }
            }

            public static class VBFields
            {
                private static PortableExecutableReference s_VBFields;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_VBFields == null)
                        {
                            s_VBFields = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.VBFields).GetReference(display: "VBFields.Dll");
                        }

                        return s_VBFields;
                    }
                }
            }

            private static PortableExecutableReference s_constantFields;
            public static PortableExecutableReference ConstantFields
            {
                get
                {
                    if (s_constantFields == null)
                    {
                        s_constantFields = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.ConstantFields).GetReference(display: "ConstantFields.Dll");
                    }

                    return s_constantFields;
                }
            }
        }

        public static class MissingTypes
        {
            private static PortableExecutableReference s_MDMissingType;
            public static PortableExecutableReference MDMissingType
            {
                get
                {
                    if (s_MDMissingType == null)
                    {
                        s_MDMissingType = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MDMissingType).GetReference(display: "MDMissingType.Dll");
                    }

                    return s_MDMissingType;
                }
            }

            private static PortableExecutableReference s_MDMissingTypeLib;
            public static PortableExecutableReference MDMissingTypeLib
            {
                get
                {
                    if (s_MDMissingTypeLib == null)
                    {
                        s_MDMissingTypeLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MDMissingTypeLib).GetReference(display: "MDMissingTypeLib.Dll");
                    }

                    return s_MDMissingTypeLib;
                }
            }

            private static PortableExecutableReference s_missingTypesEquality1;
            public static PortableExecutableReference MissingTypesEquality1
            {
                get
                {
                    if (s_missingTypesEquality1 == null)
                    {
                        s_missingTypesEquality1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality1).GetReference(display: "MissingTypesEquality1.Dll");
                    }

                    return s_missingTypesEquality1;
                }
            }

            private static PortableExecutableReference s_missingTypesEquality2;
            public static PortableExecutableReference MissingTypesEquality2
            {
                get
                {
                    if (s_missingTypesEquality2 == null)
                    {
                        s_missingTypesEquality2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality2).GetReference(display: "MissingTypesEquality2.Dll");
                    }

                    return s_missingTypesEquality2;
                }
            }

            private static PortableExecutableReference s_CL2;
            public static PortableExecutableReference CL2
            {
                get
                {
                    if (s_CL2 == null)
                    {
                        s_CL2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.CL2).GetReference(display: "CL2.Dll");
                    }

                    return s_CL2;
                }
            }

            private static PortableExecutableReference s_CL3;
            public static PortableExecutableReference CL3
            {
                get
                {
                    if (s_CL3 == null)
                    {
                        s_CL3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.CL3).GetReference(display: "CL3.Dll");
                    }

                    return s_CL3;
                }
            }
        }

        public static class TypeForwarders
        {
            public static class TypeForwarder
            {
                private static PortableExecutableReference s_typeForwarder2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_typeForwarder2 == null)
                        {
                            s_typeForwarder2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarder).GetReference(display: "TypeForwarder.Dll");
                        }

                        return s_typeForwarder2;
                    }
                }
            }

            public static class TypeForwarderLib
            {
                private static PortableExecutableReference s_typeForwarderLib2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_typeForwarderLib2 == null)
                        {
                            s_typeForwarderLib2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarderLib).GetReference(display: "TypeForwarderLib.Dll");
                        }

                        return s_typeForwarderLib2;
                    }
                }
            }

            public static class TypeForwarderBase
            {
                private static PortableExecutableReference s_typeForwarderBase2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (s_typeForwarderBase2 == null)
                        {
                            s_typeForwarderBase2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarderBase).GetReference(display: "TypeForwarderBase.Dll");
                        }

                        return s_typeForwarderBase2;
                    }
                }
            }
        }

        public static class MultiTargeting
        {
            private static PortableExecutableReference s_source1Module;
            public static PortableExecutableReference Source1Module
            {
                get
                {
                    if (s_source1Module == null)
                    {
                        s_source1Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source1Module).GetReference(display: "Source1Module.netmodule");
                    }

                    return s_source1Module;
                }
            }

            private static PortableExecutableReference s_source3Module;
            public static PortableExecutableReference Source3Module
            {
                get
                {
                    if (s_source3Module == null)
                    {
                        s_source3Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source3Module).GetReference(display: "Source3Module.netmodule");
                    }

                    return s_source3Module;
                }
            }

            private static PortableExecutableReference s_source4Module;
            public static PortableExecutableReference Source4Module
            {
                get
                {
                    if (s_source4Module == null)
                    {
                        s_source4Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source4Module).GetReference(display: "Source4Module.netmodule");
                    }

                    return s_source4Module;
                }
            }

            private static PortableExecutableReference s_source5Module;
            public static PortableExecutableReference Source5Module
            {
                get
                {
                    if (s_source5Module == null)
                    {
                        s_source5Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source5Module).GetReference(display: "Source5Module.netmodule");
                    }

                    return s_source5Module;
                }
            }

            private static PortableExecutableReference s_source7Module;
            public static PortableExecutableReference Source7Module
            {
                get
                {
                    if (s_source7Module == null)
                    {
                        s_source7Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source7Module).GetReference(display: "Source7Module.netmodule");
                    }

                    return s_source7Module;
                }
            }
        }

        public static class NoPia
        {
            private static PortableExecutableReference s_stdOle;
            public static PortableExecutableReference StdOle
            {
                get
                {
                    if (s_stdOle == null)
                    {
                        s_stdOle = AssemblyMetadata.CreateFromImage(TestResources.ProprietaryPias.stdole).GetReference(display: "stdole.dll");
                    }

                    return s_stdOle;
                }
            }

            private static PortableExecutableReference s_pia1;
            public static PortableExecutableReference Pia1
            {
                get
                {
                    if (s_pia1 == null)
                    {
                        s_pia1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia1).GetReference(display: "Pia1.dll");
                    }

                    return s_pia1;
                }
            }

            private static PortableExecutableReference s_pia1Copy;
            public static PortableExecutableReference Pia1Copy
            {
                get
                {
                    if (s_pia1Copy == null)
                    {
                        s_pia1Copy = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia1Copy).GetReference(display: "Pia1Copy.dll");
                    }

                    return s_pia1Copy;
                }
            }

            private static PortableExecutableReference s_pia2;
            public static PortableExecutableReference Pia2
            {
                get
                {
                    if (s_pia2 == null)
                    {
                        s_pia2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia2).GetReference(display: "Pia2.dll");
                    }

                    return s_pia2;
                }
            }

            private static PortableExecutableReference s_pia3;
            public static PortableExecutableReference Pia3
            {
                get
                {
                    if (s_pia3 == null)
                    {
                        s_pia3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia3).GetReference(display: "Pia3.dll");
                    }

                    return s_pia3;
                }
            }

            private static PortableExecutableReference s_pia4;
            public static PortableExecutableReference Pia4
            {
                get
                {
                    if (s_pia4 == null)
                    {
                        s_pia4 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia4).GetReference(display: "Pia4.dll");
                    }

                    return s_pia4;
                }
            }

            private static PortableExecutableReference s_pia5;
            public static PortableExecutableReference Pia5
            {
                get
                {
                    if (s_pia5 == null)
                    {
                        s_pia5 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia5).GetReference(display: "Pia5.dll");
                    }

                    return s_pia5;
                }
            }

            private static PortableExecutableReference s_generalPia;
            public static PortableExecutableReference GeneralPia
            {
                get
                {
                    if (s_generalPia == null)
                    {
                        s_generalPia = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.GeneralPia).GetReference(display: "GeneralPia.dll");
                    }

                    return s_generalPia;
                }
            }

            private static PortableExecutableReference s_generalPiaCopy;
            public static PortableExecutableReference GeneralPiaCopy
            {
                get
                {
                    if (s_generalPiaCopy == null)
                    {
                        s_generalPiaCopy = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.GeneralPiaCopy).GetReference(display: "GeneralPiaCopy.dll");
                    }

                    return s_generalPiaCopy;
                }
            }

            private static PortableExecutableReference s_noPIAGenericsAsm1;
            public static PortableExecutableReference NoPIAGenericsAsm1
            {
                get
                {
                    if (s_noPIAGenericsAsm1 == null)
                    {
                        s_noPIAGenericsAsm1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.NoPIAGenerics1_Asm1).GetReference(display: "NoPIAGenerics1-Asm1.dll");
                    }

                    return s_noPIAGenericsAsm1;
                }
            }

            private static PortableExecutableReference s_externalAsm1;
            public static PortableExecutableReference ExternalAsm1
            {
                get
                {
                    if (s_externalAsm1 == null)
                    {
                        s_externalAsm1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.ExternalAsm1).GetReference(display: "ExternalAsm1.dll");
                    }

                    return s_externalAsm1;
                }
            }

            private static PortableExecutableReference s_library1;
            public static PortableExecutableReference Library1
            {
                get
                {
                    if (s_library1 == null)
                    {
                        s_library1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Library1).GetReference(display: "Library1.dll");
                    }

                    return s_library1;
                }
            }

            private static PortableExecutableReference s_library2;
            public static PortableExecutableReference Library2
            {
                get
                {
                    if (s_library2 == null)
                    {
                        s_library2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Library2).GetReference(display: "Library2.dll");
                    }

                    return s_library2;
                }
            }

            private static PortableExecutableReference s_localTypes1;
            public static PortableExecutableReference LocalTypes1
            {
                get
                {
                    if (s_localTypes1 == null)
                    {
                        s_localTypes1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes1).GetReference(display: "LocalTypes1.dll");
                    }

                    return s_localTypes1;
                }
            }

            private static PortableExecutableReference s_localTypes2;
            public static PortableExecutableReference LocalTypes2
            {
                get
                {
                    if (s_localTypes2 == null)
                    {
                        s_localTypes2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes2).GetReference(display: "LocalTypes2.dll");
                    }

                    return s_localTypes2;
                }
            }

            private static PortableExecutableReference s_localTypes3;
            public static PortableExecutableReference LocalTypes3
            {
                get
                {
                    if (s_localTypes3 == null)
                    {
                        s_localTypes3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes3).GetReference(display: "LocalTypes3.dll");
                    }

                    return s_localTypes3;
                }
            }

            private static PortableExecutableReference s_A;
            public static PortableExecutableReference A
            {
                get
                {
                    if (s_A == null)
                    {
                        s_A = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.A).GetReference(display: "A.dll");
                    }

                    return s_A;
                }
            }

            private static PortableExecutableReference s_B;
            public static PortableExecutableReference B
            {
                get
                {
                    if (s_B == null)
                    {
                        s_B = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.B).GetReference(display: "B.dll");
                    }

                    return s_B;
                }
            }

            private static PortableExecutableReference s_C;
            public static PortableExecutableReference C
            {
                get
                {
                    if (s_C == null)
                    {
                        s_C = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.C).GetReference(display: "C.dll");
                    }

                    return s_C;
                }
            }

            private static PortableExecutableReference s_D;
            public static PortableExecutableReference D
            {
                get
                {
                    if (s_D == null)
                    {
                        s_D = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.D).GetReference(display: "D.dll");
                    }

                    return s_D;
                }
            }

            public static class Microsoft
            {
                public static class VisualStudio
                {
                    private static PortableExecutableReference s_missingPIAAttributes;
                    public static PortableExecutableReference MissingPIAAttributes
                    {
                        get
                        {
                            if (s_missingPIAAttributes == null)
                            {
                                s_missingPIAAttributes = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.MissingPIAAttributes).GetReference(display: "MicrosoftPIAAttributes.dll");
                            }

                            return s_missingPIAAttributes;
                        }
                    }
                }
            }
        }

        public static class Interface
        {
            private static PortableExecutableReference s_staticMethodInInterface;
            public static PortableExecutableReference StaticMethodInInterface
            {
                get
                {
                    if (s_staticMethodInInterface == null)
                    {
                        s_staticMethodInInterface = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Interface.StaticMethodInInterface).GetReference(display: "StaticMethodInInterface.dll");
                    }

                    return s_staticMethodInInterface;
                }
            }

            private static PortableExecutableReference s_MDInterfaceMapping;
            public static PortableExecutableReference MDInterfaceMapping
            {
                get
                {
                    if (s_MDInterfaceMapping == null)
                    {
                        s_MDInterfaceMapping = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Interface.MDInterfaceMapping).GetReference(display: "MDInterfaceMapping.dll");
                    }

                    return s_MDInterfaceMapping;
                }
            }
        }

        public static class MetadataCache
        {
            private static PortableExecutableReference s_MDTestLib1;
            public static PortableExecutableReference MDTestLib1
            {
                get
                {
                    if (s_MDTestLib1 == null)
                    {
                        s_MDTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.General.MDTestLib1).GetReference(display: "MDTestLib1.dll");
                    }

                    return s_MDTestLib1;
                }
            }

            private static PortableExecutableReference s_netModule1;
            public static PortableExecutableReference netModule1
            {
                get
                {
                    if (s_netModule1 == null)
                    {
                        s_netModule1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "netModule1.netmodule");
                    }

                    return s_netModule1;
                }
            }
        }

        public static class ExplicitInterfaceImplementation
        {
            public static class Methods
            {
                private static PortableExecutableReference s_CSharp;
                public static PortableExecutableReference CSharp
                {
                    get
                    {
                        if (s_CSharp == null)
                        {
                            s_CSharp = AssemblyMetadata.CreateFromImage(TestResources.General.CSharpExplicitInterfaceImplementation).GetReference(display: "CSharpExplicitInterfaceImplementation.dll");
                        }

                        return s_CSharp;
                    }
                }

                private static PortableExecutableReference s_IL;
                public static PortableExecutableReference IL
                {
                    get
                    {
                        if (s_IL == null)
                        {
                            s_IL = AssemblyMetadata.CreateFromImage(TestResources.General.ILExplicitInterfaceImplementation).GetReference(display: "ILExplicitInterfaceImplementation.dll");
                        }

                        return s_IL;
                    }
                }
            }

            public static class Properties
            {
                private static PortableExecutableReference s_CSharp;
                public static PortableExecutableReference CSharp
                {
                    get
                    {
                        if (s_CSharp == null)
                        {
                            s_CSharp = AssemblyMetadata.CreateFromImage(TestResources.General.CSharpExplicitInterfaceImplementationProperties).GetReference(display: "CSharpExplicitInterfaceImplementationProperties.dll");
                        }

                        return s_CSharp;
                    }
                }

                private static PortableExecutableReference s_IL;
                public static PortableExecutableReference IL
                {
                    get
                    {
                        if (s_IL == null)
                        {
                            s_IL = AssemblyMetadata.CreateFromImage(TestResources.General.ILExplicitInterfaceImplementationProperties).GetReference(display: "ILExplicitInterfaceImplementationProperties.dll");
                        }

                        return s_IL;
                    }
                }
            }

            public static class Events
            {
                private static PortableExecutableReference s_CSharp;
                public static PortableExecutableReference CSharp
                {
                    get
                    {
                        if (s_CSharp == null)
                        {
                            s_CSharp = AssemblyMetadata.CreateFromImage(TestResources.General.CSharpExplicitInterfaceImplementationEvents).GetReference(display: "CSharpExplicitInterfaceImplementationEvents.dll");
                        }

                        return s_CSharp;
                    }
                }
            }
        }

        private static PortableExecutableReference s_regress40025;
        public static PortableExecutableReference Regress40025
        {
            get
            {
                if (s_regress40025 == null)
                {
                    s_regress40025 = AssemblyMetadata.CreateFromImage(TestResources.General.Regress40025DLL).GetReference(display: "Regress40025DLL.dll");
                }

                return s_regress40025;
            }
        }

        public static class WithEvents
        {
            private static PortableExecutableReference s_simpleWithEvents;
            public static PortableExecutableReference SimpleWithEvents
            {
                get
                {
                    if (s_simpleWithEvents == null)
                    {
                        s_simpleWithEvents = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.WithEvents.SimpleWithEvents).GetReference(display: "SimpleWithEvents.dll");
                    }

                    return s_simpleWithEvents;
                }
            }
        }

        public static class DelegateImplementation
        {
            private static PortableExecutableReference s_delegatesWithoutInvoke;
            public static PortableExecutableReference DelegatesWithoutInvoke
            {
                get
                {
                    if (s_delegatesWithoutInvoke == null)
                    {
                        s_delegatesWithoutInvoke = AssemblyMetadata.CreateFromImage(TestResources.General.DelegatesWithoutInvoke).GetReference(display: "DelegatesWithoutInvoke.dll");
                    }

                    return s_delegatesWithoutInvoke;
                }
            }

            private static PortableExecutableReference s_delegateByRefParamArray;
            public static PortableExecutableReference DelegateByRefParamArray
            {
                get
                {
                    if (s_delegateByRefParamArray == null)
                    {
                        s_delegateByRefParamArray = AssemblyMetadata.CreateFromImage(TestResources.General.DelegateByRefParamArray).GetReference(display: "DelegateByRefParamArray.dll");
                    }

                    return s_delegateByRefParamArray;
                }
            }
        }

        public static class Metadata
        {
            private static PortableExecutableReference s_invalidCharactersInAssemblyName2;
            public static PortableExecutableReference InvalidCharactersInAssemblyName
            {
                get
                {
                    if (s_invalidCharactersInAssemblyName2 == null)
                    {
                        s_invalidCharactersInAssemblyName2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.InvalidCharactersInAssemblyName).GetReference(display: "InvalidCharactersInAssemblyName.dll");
                    }

                    return s_invalidCharactersInAssemblyName2;
                }
            }

            private static PortableExecutableReference s_MDTestAttributeDefLib;
            public static PortableExecutableReference MDTestAttributeDefLib
            {
                get
                {
                    if (s_MDTestAttributeDefLib == null)
                    {
                        s_MDTestAttributeDefLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib).GetReference(display: "MDTestAttributeDefLib.dll");
                    }

                    return s_MDTestAttributeDefLib;
                }
            }

            private static PortableExecutableReference s_MDTestAttributeApplicationLib;
            public static PortableExecutableReference MDTestAttributeApplicationLib
            {
                get
                {
                    if (s_MDTestAttributeApplicationLib == null)
                    {
                        s_MDTestAttributeApplicationLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib).GetReference(display: "MDTestAttributeApplicationLib.dll");
                    }

                    return s_MDTestAttributeApplicationLib;
                }
            }

            private static PortableExecutableReference s_attributeInterop01;
            public static PortableExecutableReference AttributeInterop01
            {
                get
                {
                    if (s_attributeInterop01 == null)
                    {
                        s_attributeInterop01 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeInterop01).GetReference(display: "AttributeInterop01.dll");
                    }

                    return s_attributeInterop01;
                }
            }

            private static PortableExecutableReference s_attributeInterop02;
            public static PortableExecutableReference AttributeInterop02
            {
                get
                {
                    if (s_attributeInterop02 == null)
                    {
                        s_attributeInterop02 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeInterop02).GetReference(display: "AttributeInterop02.dll");
                    }

                    return s_attributeInterop02;
                }
            }

            private static PortableExecutableReference s_attributeTestLib01;
            public static PortableExecutableReference AttributeTestLib01
            {
                get
                {
                    if (s_attributeTestLib01 == null)
                    {
                        s_attributeTestLib01 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestLib01).GetReference(display: "AttributeTestLib01.dll");
                    }

                    return s_attributeTestLib01;
                }
            }

            private static PortableExecutableReference s_attributeTestDef01;
            public static PortableExecutableReference AttributeTestDef01
            {
                get
                {
                    if (s_attributeTestDef01 == null)
                    {
                        s_attributeTestDef01 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01).GetReference(display: "AttributeTestDef01.dll");
                    }

                    return s_attributeTestDef01;
                }
            }

            private static PortableExecutableReference s_dynamicAttributeLib;
            public static PortableExecutableReference DynamicAttributeLib
            {
                get
                {
                    if (s_dynamicAttributeLib == null)
                    {
                        s_dynamicAttributeLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.DynamicAttribute).GetReference(display: "DynamicAttribute.dll");
                    }

                    return s_dynamicAttributeLib;
                }
            }
        }

        public static class UseSiteErrors
        {
            private static PortableExecutableReference s_unavailable;
            public static PortableExecutableReference Unavailable
            {
                get
                {
                    if (s_unavailable == null)
                    {
                        s_unavailable = AssemblyMetadata.CreateFromImage(TestResources.General.Unavailable).GetReference(display: "Unavailable.dll");
                    }

                    return s_unavailable;
                }
            }

            private static PortableExecutableReference s_CSharp;
            public static PortableExecutableReference CSharp
            {
                get
                {
                    if (s_CSharp == null)
                    {
                        s_CSharp = AssemblyMetadata.CreateFromImage(TestResources.General.CSharpErrors).GetReference(display: "CSharpErrors.dll");
                    }

                    return s_CSharp;
                }
            }

            private static PortableExecutableReference s_IL;
            public static PortableExecutableReference IL
            {
                get
                {
                    if (s_IL == null)
                    {
                        s_IL = AssemblyMetadata.CreateFromImage(TestResources.General.ILErrors).GetReference(display: "ILErrors.dll");
                    }

                    return s_IL;
                }
            }
        }

        public static class Versioning
        {
            private static PortableExecutableReference s_AR_SA;
            public static PortableExecutableReference AR_SA
            {
                get
                {
                    if (s_AR_SA == null)
                    {
                        s_AR_SA = AssemblyMetadata.CreateFromImage(TestResources.General.Culture_AR_SA).GetReference(display: "AR-SA");
                    }

                    return s_AR_SA;
                }
            }

            private static PortableExecutableReference s_EN_US;
            public static PortableExecutableReference EN_US
            {
                get
                {
                    if (s_EN_US == null)
                    {
                        s_EN_US = AssemblyMetadata.CreateFromImage(TestResources.General.Culture_EN_US).GetReference(display: "EN-US");
                    }

                    return s_EN_US;
                }
            }

            private static PortableExecutableReference s_C1;
            public static PortableExecutableReference C1
            {
                get
                {
                    if (s_C1 == null)
                    {
                        s_C1 = AssemblyMetadata.CreateFromImage(TestResources.General.C1).GetReference(display: "C1");
                    }

                    return s_C1;
                }
            }

            private static PortableExecutableReference s_C2;
            public static PortableExecutableReference C2
            {
                get
                {
                    if (s_C2 == null)
                    {
                        s_C2 = AssemblyMetadata.CreateFromImage(TestResources.General.C2).GetReference(display: "C2");
                    }

                    return s_C2;
                }
            }
        }
    }
}
