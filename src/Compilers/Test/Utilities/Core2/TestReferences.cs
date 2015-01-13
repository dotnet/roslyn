// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;

public static class TestReferences
{
    public static class MetadataTests
    {
        public static class NetModule01
        {
            private static PortableExecutableReference _AppCS;
            public static PortableExecutableReference AppCS
            {
                get
                {
                    if (_AppCS == null)
                    {
                        _AppCS = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.AppCS).GetReference(display: "AppCS");
                    }

                    return _AppCS;
                }
            }

            private static PortableExecutableReference _ModuleCS00;
            public static PortableExecutableReference ModuleCS00
            {
                get
                {
                    if (_ModuleCS00 == null)
                    {
                        _ModuleCS00 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00).GetReference(display: "ModuleCS00.mod");
                    }

                    return _ModuleCS00;
                }
            }

            private static PortableExecutableReference _ModuleCS01;
            public static PortableExecutableReference ModuleCS01
            {
                get
                {
                    if (_ModuleCS01 == null)
                    {
                        _ModuleCS01 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS01).GetReference(display: "ModuleCS01.mod");
                    }

                    return _ModuleCS01;
                }
            }

            private static PortableExecutableReference _ModuleVB01;
            public static PortableExecutableReference ModuleVB01
            {
                get
                {
                    if (_ModuleVB01 == null)
                    {
                        _ModuleVB01 = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleVB01).GetReference(display: "ModuleVB01.mod");
                    }

                    return _ModuleVB01;
                }
            }
        }

        public static class InterfaceAndClass
        {
            private static PortableExecutableReference _CSClasses01;
            public static PortableExecutableReference CSClasses01
            {
                get
                {
                    if (_CSClasses01 == null)
                    {
                        _CSClasses01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.CSClasses01).GetReference(display: "CSClasses01.dll");
                    }

                    return _CSClasses01;
                }
            }

            private static PortableExecutableReference _CSInterfaces01;
            public static PortableExecutableReference CSInterfaces01
            {
                get
                {
                    if (_CSInterfaces01 == null)
                    {
                        _CSInterfaces01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01).GetReference(display: "CSInterfaces01.dll");
                    }

                    return _CSInterfaces01;
                }
            }

            private static PortableExecutableReference _VBClasses01;
            public static PortableExecutableReference VBClasses01
            {
                get
                {
                    if (_VBClasses01 == null)
                    {
                        _VBClasses01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBClasses01).GetReference(display: "VBClasses01.dll");
                    }

                    return _VBClasses01;
                }
            }

            private static PortableExecutableReference _VBClasses02;
            public static PortableExecutableReference VBClasses02
            {
                get
                {
                    if (_VBClasses02 == null)
                    {
                        _VBClasses02 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBClasses02).GetReference(display: "VBClasses02.dll");
                    }

                    return _VBClasses02;
                }
            }

            private static PortableExecutableReference _VBInterfaces01;
            public static PortableExecutableReference VBInterfaces01
            {
                get
                {
                    if (_VBInterfaces01 == null)
                    {
                        _VBInterfaces01 = AssemblyMetadata.CreateFromImage(TestResources.MetadataTests.InterfaceAndClass.VBInterfaces01).GetReference(display: "VBInterfaces01.dll");
                    }

                    return _VBInterfaces01;
                }
            }
        }
    }

    public static class NetFx
    {
        public static class silverlight_v5_0_5_0
        {
            private static PortableExecutableReference _System;
            public static PortableExecutableReference System
            {
                get
                {
                    if (_System == null)
                    {
                        _System = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight).GetReference(display: "System.v5.0.5.0_silverlight.dll");
                    }

                    return _System;
                }
            }
        }

        public static class v4_0_21006
        {
            private static PortableExecutableReference _mscorlib;
            public static PortableExecutableReference mscorlib
            {
                get
                {
                    if (_mscorlib == null)
                    {
                        _mscorlib = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).GetReference(display: "mscorlib.dll");
                    }

                    return _mscorlib;
                }
            }
        }

        public static class v2_0_50727
        {
            private static PortableExecutableReference _mscorlib;
            public static PortableExecutableReference mscorlib
            {
                get
                {
                    if (_mscorlib == null)
                    {
                        _mscorlib = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v2_0_50727.mscorlib).GetReference(display: "mscorlib, v2.0.50727");
                    }

                    return _mscorlib;
                }
            }

            private static PortableExecutableReference _System;
            public static PortableExecutableReference System
            {
                get
                {
                    if (_System == null)
                    {
                        _System = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v2_0_50727.System).GetReference(display: "System.dll");
                    }

                    return _System;
                }
            }

            private static PortableExecutableReference _Microsoft_VisualBasic;
            public static PortableExecutableReference Microsoft_VisualBasic
            {
                get
                {
                    if (_Microsoft_VisualBasic == null)
                    {
                        _Microsoft_VisualBasic = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v2_0_50727.Microsoft_VisualBasic).GetReference(display: "Microsoft.VisualBasic.dll");
                    }

                    return _Microsoft_VisualBasic;
                }
            }
        }

        public static class v3_5_30729
        {
            private static PortableExecutableReference _SystemCore;
            public static PortableExecutableReference SystemCore
            {
                get
                {
                    if (_SystemCore == null)
                    {
                        _SystemCore = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v3_5_30729.System_Core_v3_5_30729.AsImmutableOrNull()).GetReference(display: "System.Core, v3.5.30729");
                    }

                    return _SystemCore;
                }
            }
        }

        public static class v4_0_30319
        {
            private static PortableExecutableReference _mscorlib;
            public static PortableExecutableReference mscorlib
            {
                get
                {
                    if (_mscorlib == null)
                    {
                        _mscorlib = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib).GetReference(filePath: @"R:\v4_0_30319\mscorlib.dll");
                    }

                    return _mscorlib;
                }
            }

            private static PortableExecutableReference _System_Core;
            public static PortableExecutableReference System_Core
            {
                get
                {
                    if (_System_Core == null)
                    {
                        _System_Core = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Core).GetReference(filePath: @"R:\v4_0_30319\System.Core.dll");
                    }

                    return _System_Core;
                }
            }

            private static PortableExecutableReference _System_Configuration;
            public static PortableExecutableReference System_Configuration
            {
                get
                {
                    if (_System_Configuration == null)
                    {
                        _System_Configuration = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Configuration).GetReference(filePath: @"R:\v4_0_30319\System.Configuration.dll");
                    }

                    return _System_Configuration;
                }
            }

            private static PortableExecutableReference _System;
            public static PortableExecutableReference System
            {
                get
                {
                    if (_System == null)
                    {
                        _System = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System).GetReference(filePath: @"R:\v4_0_30319\System.dll", display: "System.dll");
                    }

                    return _System;
                }
            }

            private static PortableExecutableReference _System_Data;
            public static PortableExecutableReference System_Data
            {
                get
                {
                    if (_System_Data == null)
                    {
                        _System_Data = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Data).GetReference(filePath: @"R:\v4_0_30319\System.Data.dll");
                    }

                    return _System_Data;
                }
            }

            private static PortableExecutableReference _System_Xml;
            public static PortableExecutableReference System_Xml
            {
                get
                {
                    if (_System_Xml == null)
                    {
                        _System_Xml = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Xml).GetReference(filePath: @"R:\v4_0_30319\System.Xml.dll");
                    }

                    return _System_Xml;
                }
            }

            private static PortableExecutableReference _System_Xml_Linq;
            public static PortableExecutableReference System_Xml_Linq
            {
                get
                {
                    if (_System_Xml_Linq == null)
                    {
                        _System_Xml_Linq = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Xml_Linq).GetReference(filePath: @"R:\v4_0_30319\System.Xml.Linq.dll");
                    }

                    return _System_Xml_Linq;
                }
            }

            private static PortableExecutableReference _System_Windows_Forms;
            public static PortableExecutableReference System_Windows_Forms
            {
                get
                {
                    if (_System_Windows_Forms == null)
                    {
                        _System_Windows_Forms = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Windows_Forms).GetReference(filePath: @"R:\v4_0_30319\System.Windows.Forms.dll");
                    }

                    return _System_Windows_Forms;
                }
            }

            private static PortableExecutableReference _Microsoft_CSharp;
            public static PortableExecutableReference Microsoft_CSharp
            {
                get
                {
                    if (_Microsoft_CSharp == null)
                    {
                        _Microsoft_CSharp = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_CSharp).GetReference(filePath: @"R:\v4_0_30319\Microsoft.CSharp.dll");
                    }

                    return _Microsoft_CSharp;
                }
            }

            private static PortableExecutableReference _Microsoft_VisualBasic;
            public static PortableExecutableReference Microsoft_VisualBasic
            {
                get
                {
                    if (_Microsoft_VisualBasic == null)
                    {
                        _Microsoft_VisualBasic = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_VisualBasic).GetReference(filePath: @"R:\v4_0_30319\Microsoft.VisualBasic.dll");
                    }

                    return _Microsoft_VisualBasic;
                }
            }

            private static PortableExecutableReference _Microsoft_JScript;
            public static PortableExecutableReference Microsoft_JScript
            {
                get
                {
                    if (_Microsoft_JScript == null)
                    {
                        _Microsoft_JScript = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_JScript).GetReference(display: "Microsoft.JScript.dll");
                    }

                    return _Microsoft_JScript;
                }
            }

            private static PortableExecutableReference _System_ComponentModel_Composition;
            public static PortableExecutableReference System_ComponentModel_Composition
            {
                get
                {
                    if (_System_ComponentModel_Composition == null)
                    {
                        _System_ComponentModel_Composition = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_ComponentModel_Composition).GetReference(display: "System.ComponentModel.Composition.dll");
                    }

                    return _System_ComponentModel_Composition;
                }
            }

            private static PortableExecutableReference _System_Web_Services;
            public static PortableExecutableReference System_Web_Services
            {
                get
                {
                    if (_System_Web_Services == null)
                    {
                        _System_Web_Services = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_Web_Services).GetReference(display: "System.Web.Services.dll");
                    }

                    return _System_Web_Services;
                }
            }

            public static class System_EnterpriseServices
            {
                private static PortableExecutableReference _System_EnterpriseServices;

                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_System_EnterpriseServices == null)
                        {
                            _System_EnterpriseServices = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319.System_EnterpriseServices).GetReference(display: "System.EnterpriseServices.dll");
                        }

                        return _System_EnterpriseServices;
                    }
                }
            }

            private static PortableExecutableReference _System_Runtime_Serialization;
            public static PortableExecutableReference System_Runtime_Serialization
            {
                get
                {
                    if (_System_Runtime_Serialization == null)
                    {
                        _System_Runtime_Serialization = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.NetFX.v4_0_30319_17929.System_Runtime_Serialization).GetReference(display: "System.Runtime.Serialization.dll");
                    }

                    return _System_Runtime_Serialization;
                }
            }
        }
    }

    public static class DiagnosticTests
    {
        public static class ErrTestLib01
        {
            private static PortableExecutableReference _ErrTestLib01;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (_ErrTestLib01 == null)
                    {
                        _ErrTestLib01 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.DiagnosticTests.ErrTestLib01).GetReference(display: "ErrTestLib01.dll");
                    }

                    return _ErrTestLib01;
                }
            }
        }

        public static class ErrTestLib02
        {
            private static PortableExecutableReference _ErrTestLib02;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (_ErrTestLib02 == null)
                    {
                        _ErrTestLib02 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.DiagnosticTests.ErrTestLib02).GetReference(display: "ErrTestLib02.dll");
                    }

                    return _ErrTestLib02;
                }
            }
        }

        public static class ErrTestLib11
        {
            private static PortableExecutableReference _ErrTestLib11;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (_ErrTestLib11 == null)
                    {
                        _ErrTestLib11 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.DiagnosticTests.ErrTestLib11).GetReference(display: "ErrTestLib11.dll");
                    }

                    return _ErrTestLib11;
                }
            }
        }

        public static class ErrTestMod01
        {
            private static PortableExecutableReference _ErrTestMod01;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (_ErrTestMod01 == null)
                    {
                        _ErrTestMod01 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.DiagnosticTests.ErrTestMod01).GetReference(display: "ErrTestMod01.dll");
                    }

                    return _ErrTestMod01;
                }
            }
        }

        public static class ErrTestMod02
        {
            private static PortableExecutableReference _ErrTestMod02;
            public static PortableExecutableReference dll
            {
                get
                {
                    if (_ErrTestMod02 == null)
                    {
                        _ErrTestMod02 = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.DiagnosticTests.ErrTestMod02).GetReference(display: "ErrTestMod02.dll");
                    }

                    return _ErrTestMod02;
                }
            }
        }

        public static class badresfile
        {
            private static PortableExecutableReference _badresfile;
            public static PortableExecutableReference res
            {
                get
                {
                    if (_badresfile == null)
                    {
                        _badresfile = AssemblyMetadata.CreateFromImage(TestResources.DiagnosticTests.DiagnosticTests.badresfile).GetReference(display: "badresfile.res");
                    }

                    return _badresfile;
                }
            }
        }
    }

    public static class SymbolsTests
    {
        private static PortableExecutableReference _mdTestLib1;
        public static PortableExecutableReference MDTestLib1
        {
            get
            {
                if (_mdTestLib1 == null)
                {
                    _mdTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.MDTestLib1).GetReference(display: "MDTestLib1.dll");
                }

                return _mdTestLib1;
            }
        }

        private static PortableExecutableReference _mdTestLib2;
        public static PortableExecutableReference MDTestLib2
        {
            get
            {
                if (_mdTestLib2 == null)
                {
                    _mdTestLib2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.MDTestLib2).GetReference(display: "MDTestLib2.dll");
                }

                return _mdTestLib2;
            }
        }

        private static PortableExecutableReference _VBConversions;
        public static PortableExecutableReference VBConversions
        {
            get
            {
                if (_VBConversions == null)
                {
                    _VBConversions = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.VBConversions).GetReference(display: "VBConversions.dll");
                }

                return _VBConversions;
            }
        }

        private static PortableExecutableReference _WithSpaces;
        public static PortableExecutableReference WithSpaces
        {
            get
            {
                if (_WithSpaces == null)
                {
                    _WithSpaces = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.With_Spaces).GetReference(display: "With Spaces.dll");
                }

                return _WithSpaces;
            }
        }

        private static PortableExecutableReference _WithSpacesModule;
        public static PortableExecutableReference WithSpacesModule
        {
            get
            {
                if (_WithSpacesModule == null)
                {
                    _WithSpacesModule = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.With_SpacesModule).GetReference(display: "With Spaces.netmodule");
                }

                return _WithSpacesModule;
            }
        }

        private static PortableExecutableReference _InheritIComparable;
        public static PortableExecutableReference InheritIComparable
        {
            get
            {
                if (_InheritIComparable == null)
                {
                    _InheritIComparable = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.InheritIComparable).GetReference(display: "InheritIComparable.dll");
                }

                return _InheritIComparable;
            }
        }

        private static PortableExecutableReference _BigVisitor;
        public static PortableExecutableReference BigVisitor
        {
            get
            {
                if (_BigVisitor == null)
                {
                    _BigVisitor = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.BigVisitor).GetReference(display: "BigVisitor.dll");
                }

                return _BigVisitor;
            }
        }

        private static PortableExecutableReference _Properties;
        public static PortableExecutableReference Properties
        {
            get
            {
                if (_Properties == null)
                {
                    _Properties = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.Properties).GetReference(display: "Properties.dll");
                }

                return _Properties;
            }
        }

        private static PortableExecutableReference _PropertiesWithByRef;
        public static PortableExecutableReference PropertiesWithByRef
        {
            get
            {
                if (_PropertiesWithByRef == null)
                {
                    _PropertiesWithByRef = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.PropertiesWithByRef).GetReference(display: "PropertiesWithByRef.dll");
                }

                return _PropertiesWithByRef;
            }
        }

        private static PortableExecutableReference _Indexers;
        public static PortableExecutableReference Indexers
        {
            get
            {
                if (_Indexers == null)
                {
                    _Indexers = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.Indexers).GetReference(display: "Indexers.dll");
                }

                return _Indexers;
            }
        }

        private static PortableExecutableReference _Events;
        public static PortableExecutableReference Events
        {
            get
            {
                if (_Events == null)
                {
                    _Events = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.Events).GetReference(display: "Events.dll");
                }

                return _Events;
            }
        }

        public static class netModule
        {
            private static PortableExecutableReference _netModule1;
            public static PortableExecutableReference netModule1
            {
                get
                {
                    if (_netModule1 == null)
                    {
                        _netModule1 =ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "netModule1.netmodule");
                    }

                    return _netModule1;
                }
            }

            private static PortableExecutableReference _netModule2;
            public static PortableExecutableReference netModule2
            {
                get
                {
                    if (_netModule2 == null)
                    {
                        _netModule2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule2).GetReference(display: "netModule2.netmodule");
                    }

                    return _netModule2;
                }
            }

            private static PortableExecutableReference _CrossRefModule1;
            public static PortableExecutableReference CrossRefModule1
            {
                get
                {
                    if (_CrossRefModule1 == null)
                    {
                        _CrossRefModule1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1).GetReference(display: "CrossRefModule1.netmodule");
                    }

                    return _CrossRefModule1;
                }
            }

            private static PortableExecutableReference _CrossRefModule2;
            public static PortableExecutableReference CrossRefModule2
            {
                get
                {
                    if (_CrossRefModule2 == null)
                    {
                        _CrossRefModule2 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2).GetReference(display: "CrossRefModule2.netmodule");
                    }

                    return _CrossRefModule2;
                }
            }

            private static PortableExecutableReference _CrossRefLib;
            public static PortableExecutableReference CrossRefLib
            {
                get
                {
                    if (_CrossRefLib == null)
                    {
                        _CrossRefLib = AssemblyMetadata.Create(
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefLib),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2)).GetReference(display: "CrossRefLib.dll");
                    }

                    return _CrossRefLib;
                }
            }

            private static PortableExecutableReference _hash_module;
            public static PortableExecutableReference hash_module
            {
                get
                {
                    if (_hash_module == null)
                    {
                        _hash_module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.hash_module).GetReference(display: "hash_module.netmodule");
                    }

                    return _hash_module;
                }
            }

            private static PortableExecutableReference _x64COFF;
            public static PortableExecutableReference x64COFF
            {
                get
                {
                    if (_x64COFF == null)
                    {
                        _x64COFF = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.x64COFF).GetReference(display: "x64COFF.obj");
                    }

                    return _x64COFF;
                }
            }
        }

        public static class V1
        {
            public static class MTTestLib1
            {
                private static PortableExecutableReference _v1MTTestLib1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestLib1).GetReference(display: "MTTestLib1.dll");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static PortableExecutableReference _v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule1).GetReference(display: "MTTestModule1.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib2
            {
                private static PortableExecutableReference _v1MTTestLib2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_v1MTTestLib2 == null)
                        {
                            _v1MTTestLib2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestLib2).GetReference(display: "MTTestLib2.dll");
                        }

                        return _v1MTTestLib2;
                    }
                }
            }

            public static class MTTestModule2
            {
                private static PortableExecutableReference _v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule2).GetReference(display: "MTTestModule2.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }
        }

        public static class V2
        {
            public static class MTTestLib1
            {
                private static PortableExecutableReference _v2MTTestLib1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_v2MTTestLib1 == null)
                        {
                            _v2MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestLib1).GetReference(display: "MTTestLib1.dll");
                        }

                        return _v2MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static PortableExecutableReference _v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestModule1).GetReference(display: "MTTestModule1.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib3
            {
                private static PortableExecutableReference _v2MTTestLib3;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_v2MTTestLib3 == null)
                        {
                            _v2MTTestLib3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestLib3).GetReference(display: "MTTestLib3.dll");
                        }

                        return _v2MTTestLib3;
                    }
                }
            }

            public static class MTTestModule3
            {
                private static PortableExecutableReference _v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestModule3).GetReference(display: "MTTestModule3.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }
        }

        public static class V3
        {
            public static class MTTestLib1
            {
                private static PortableExecutableReference _v3MTTestLib1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_v3MTTestLib1 == null)
                        {
                            _v3MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestLib1).GetReference(display: "MTTestLib1.dll");
                        }

                        return _v3MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static PortableExecutableReference _v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestModule1).GetReference(display: "MTTestModule1.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib4
            {
                private static PortableExecutableReference _v3MTTestLib4;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_v3MTTestLib4 == null)
                        {
                            _v3MTTestLib4 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestLib4).GetReference(display: "MTTestLib4.dll");
                        }

                        return _v3MTTestLib4;
                    }
                }
            }

            public static class MTTestModule4
            {
                private static PortableExecutableReference _v1MTTestLib1;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestModule4).GetReference(display: "MTTestModule4.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }
        }

        public static class MultiModule
        {
            private static PortableExecutableReference _Assembly;
            public static PortableExecutableReference Assembly
            {
                get
                {
                    if (_Assembly == null)
                    {
                        _Assembly = AssemblyMetadata.Create(
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModule),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2),
                            ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3)).GetReference(display: "MultiModule.dll");
                    }

                    return _Assembly;
                }
            }

            private static PortableExecutableReference _mod2;
            public static PortableExecutableReference mod2
            {
                get
                {
                    if (_mod2 == null)
                    {
                        _mod2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2).GetReference(display: "mod2.netmodule");
                    }

                    return _mod2;
                }
            }

            private static PortableExecutableReference _mod3;
            public static PortableExecutableReference mod3
            {
                get
                {
                    if (_mod3 == null)
                    {
                        _mod3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3).GetReference(display: "mod3.netmodule");
                    }

                    return _mod3;
                }
            }

            private static PortableExecutableReference _Consumer;
            public static PortableExecutableReference Consumer
            {
                get
                {
                    if (_Consumer == null)
                    {
                        _Consumer = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.Consumer).GetReference(display: "Consumer.dll");
                    }

                    return _Consumer;
                }
            }
        }

        public static class DifferByCase
        {
            private static PortableExecutableReference _typeAndNamespaceDifferByCase;
            public static PortableExecutableReference TypeAndNamespaceDifferByCase
            {
                get
                {
                    if (_typeAndNamespaceDifferByCase == null)
                    {
                        _typeAndNamespaceDifferByCase = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase).GetReference(display: "TypeAndNamespaceDifferByCase.dll");
                    }

                    return _typeAndNamespaceDifferByCase;
                }
            }

            private static PortableExecutableReference _DifferByCaseConsumer;
            public static PortableExecutableReference Consumer
            {
                get
                {
                    if (_DifferByCaseConsumer == null)
                    {
                        _DifferByCaseConsumer = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.Consumer).GetReference(display: "Consumer.dll");
                    }

                    return _DifferByCaseConsumer;
                }
            }

            private static PortableExecutableReference _CsharpCaseSen;
            public static PortableExecutableReference CsharpCaseSen
            {
                get
                {
                    if (_CsharpCaseSen == null)
                    {
                        _CsharpCaseSen = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.Consumer).GetReference(display: "CsharpCaseSen.dll");
                    }

                    return _CsharpCaseSen;
                }
            }

            private static PortableExecutableReference _CsharpDifferCaseOverloads;
            public static PortableExecutableReference CsharpDifferCaseOverloads
            {
                get
                {
                    if (_CsharpDifferCaseOverloads == null)
                    {
                        _CsharpDifferCaseOverloads = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.DifferByCase.CSharpDifferCaseOverloads).GetReference(display: "CSharpDifferCaseOverloads.dll");
                    }

                    return _CsharpDifferCaseOverloads;
                }
            }
        }

        public static class CorLibrary
        {
            public static class GuidTest2
            {
                private static PortableExecutableReference _exe;
                public static PortableExecutableReference exe
                {
                    get
                    {
                        if (_exe == null)
                        {
                            _exe = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.GuidTest2).GetReference(display: "GuidTest2.exe");
                        }

                        return _exe;
                    }
                }
            }

            private static PortableExecutableReference _NoMsCorLibRef;
            public static PortableExecutableReference NoMsCorLibRef
            {
                get
                {
                    if (_NoMsCorLibRef == null)
                    {
                        _NoMsCorLibRef = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.NoMsCorLibRef).GetReference(display: "NoMsCorLibRef.dll");
                    }

                    return _NoMsCorLibRef;
                }
            }

            public static class FakeMsCorLib
            {
                private static PortableExecutableReference _dll;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_dll == null)
                        {
                            _dll = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CorLibrary.FakeMsCorLib).GetReference(display: "FakeMsCorLib.dll");
                        }

                        return _dll;
                    }
                }
            }
        }

        public static class CustomModifiers
        {
            public static class Modifiers
            {
                private static PortableExecutableReference _Dll;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_Dll == null)
                        {
                            _Dll = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.Modifiers).GetReference(display: "Modifiers.dll");
                        }

                        return _Dll;
                    }
                }

                private static PortableExecutableReference _Module;
                public static PortableExecutableReference netmodule
                {
                    get
                    {
                        if (_Module == null)
                        {
                            _Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModifiersModule).GetReference(display: "Modifiers.netmodule");
                        }

                        return _Module;
                    }
                }
            }

            private static PortableExecutableReference _ModoptTests;
            public static PortableExecutableReference ModoptTests
            {
                get
                {
                    if (_ModoptTests == null)
                    {
                        _ModoptTests = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModoptTests).GetReference(display: "ModoptTests.dll");
                    }

                    return _ModoptTests;
                }
            }

            public static class CppCli
            {
                private static PortableExecutableReference _Dll;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_Dll == null)
                        {
                            _Dll = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.CppCli).GetReference(display: "CppCli.dll");
                        }

                        return _Dll;
                    }
                }
            }
        }

        public static class Cyclic
        {
            public static class Cyclic1
            {
                private static PortableExecutableReference _Cyclic1;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_Cyclic1 == null)
                        {
                            _Cyclic1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Cyclic.Cyclic1).GetReference(display: "Cyclic1.dll");
                        }

                        return _Cyclic1;
                    }
                }
            }

            public static class Cyclic2
            {
                private static PortableExecutableReference _Cyclic2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_Cyclic2 == null)
                        {
                            _Cyclic2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Cyclic.Cyclic2).GetReference(display: "Cyclic2.dll");
                        }

                        return _Cyclic2;
                    }
                }
            }
        }

        public static class CyclicInheritance
        {
            private static PortableExecutableReference _Class1;
            public static PortableExecutableReference Class1
            {
                get
                {
                    if (_Class1 == null)
                    {
                        _Class1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class1).GetReference(display: "Class1.dll");
                    }

                    return _Class1;
                }
            }

            private static PortableExecutableReference _Class2;
            public static PortableExecutableReference Class2
            {
                get
                {
                    if (_Class2 == null)
                    {
                        _Class2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class2).GetReference(display: "Class2.dll");
                    }

                    return _Class2;
                }
            }

            private static PortableExecutableReference _Class3;
            public static PortableExecutableReference Class3
            {
                get
                {
                    if (_Class3 == null)
                    {
                        _Class3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicInheritance.Class3).GetReference(display: "Class3.dll");
                    }

                    return _Class3;
                }
            }
        }

        private static PortableExecutableReference _CycledStructs;
        public static PortableExecutableReference CycledStructs
        {
            get
            {
                if (_CycledStructs == null)
                {
                    _CycledStructs = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.CyclicStructure.cycledstructs).GetReference(display: "cycledstructs.dll");
                }

                return _CycledStructs;
            }
        }

        public static class RetargetingCycle
        {
            public static class V1
            {
                public static class ClassA
                {
                    private static PortableExecutableReference _ClassA;
                    public static PortableExecutableReference dll
                    {
                        get
                        {
                            if (_ClassA == null)
                            {
                                _ClassA = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetV1.ClassA).GetReference(display: "ClassA.dll");
                            }

                            return _ClassA;
                        }
                    }
                }

                public static class ClassB
                {
                    private static PortableExecutableReference _ClassB;
                    public static PortableExecutableReference netmodule
                    {
                        get
                        {
                            if (_ClassB == null)
                            {
                                _ClassB = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.RetV1.ClassB).GetReference(display: "ClassB.netmodule");
                            }

                            return _ClassB;
                        }
                    }
                }
            }

            public static class V2
            {
                public static class ClassA
                {
                    private static PortableExecutableReference _ClassA;
                    public static PortableExecutableReference dll
                    {
                        get
                        {
                            if (_ClassA == null)
                            {
                                _ClassA = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetV2.ClassA).GetReference(display: "ClassA.dll");
                            }

                            return _ClassA;
                        }
                    }
                }

                public static class ClassB
                {
                    private static PortableExecutableReference _ClassB;
                    public static PortableExecutableReference dll
                    {
                        get
                        {
                            if (_ClassB == null)
                            {
                                _ClassB = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.RetV2.ClassB).GetReference(display: "ClassB.dll");
                            }

                            return _ClassB;
                        }
                    }
                }
            }
        }

        public static class Methods
        {
            private static PortableExecutableReference _CSMethods;
            public static PortableExecutableReference CSMethods
            {
                get
                {
                    if (_CSMethods == null)
                    {
                        _CSMethods = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.CSMethods).GetReference(display: "CSMethods.Dll");
                    }

                    return _CSMethods;
                }
            }

            private static PortableExecutableReference _VBMethods;
            public static PortableExecutableReference VBMethods
            {
                get
                {
                    if (_VBMethods == null)
                    {
                        _VBMethods = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.VBMethods).GetReference(display: "VBMethods.Dll");
                    }

                    return _VBMethods;
                }
            }

            private static PortableExecutableReference _ILMethods;
            public static PortableExecutableReference ILMethods
            {
                get
                {
                    if (_ILMethods == null)
                    {
                        _ILMethods = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.ILMethods).GetReference(display: "ILMethods.Dll");
                    }

                    return _ILMethods;
                }
            }

            private static PortableExecutableReference _ByRefReturn;
            public static PortableExecutableReference ByRefReturn
            {
                get
                {
                    if (_ByRefReturn == null)
                    {
                        _ByRefReturn = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Methods.ByRefReturn).GetReference(display: "ByRefReturn.Dll");
                    }

                    return _ByRefReturn;
                }
            }
        }

        public static class Fields
        {
            public static class CSFields
            {
                private static PortableExecutableReference _CSFields;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_CSFields == null)
                        {
                            _CSFields = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.CSFields).GetReference(display: "CSFields.Dll");
                        }

                        return _CSFields;
                    }
                }
            }

            public static class VBFields
            {
                private static PortableExecutableReference _VBFields;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_VBFields == null)
                        {
                            _VBFields = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.VBFields).GetReference(display: "VBFields.Dll");
                        }

                        return _VBFields;
                    }
                }
            }

            private static PortableExecutableReference _ConstantFields;
            public static PortableExecutableReference ConstantFields
            {
                get
                {
                    if (_ConstantFields == null)
                    {
                        _ConstantFields = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Fields.ConstantFields).GetReference(display: "ConstantFields.Dll");
                    }

                    return _ConstantFields;
                }
            }
        }

        public static class MissingTypes
        {
            private static PortableExecutableReference _MDMissingType;
            public static PortableExecutableReference MDMissingType
            {
                get
                {
                    if (_MDMissingType == null)
                    {
                        _MDMissingType = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MDMissingType).GetReference(display: "MDMissingType.Dll");
                    }

                    return _MDMissingType;
                }
            }

            private static PortableExecutableReference _MDMissingTypeLib;
            public static PortableExecutableReference MDMissingTypeLib
            {
                get
                {
                    if (_MDMissingTypeLib == null)
                    {
                        _MDMissingTypeLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MDMissingTypeLib).GetReference(display: "MDMissingTypeLib.Dll");
                    }

                    return _MDMissingTypeLib;
                }
            }

            private static PortableExecutableReference _MissingTypesEquality1;
            public static PortableExecutableReference MissingTypesEquality1
            {
                get
                {
                    if (_MissingTypesEquality1 == null)
                    {
                        _MissingTypesEquality1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality1).GetReference(display: "MissingTypesEquality1.Dll");
                    }

                    return _MissingTypesEquality1;
                }
            }

            private static PortableExecutableReference _MissingTypesEquality2;
            public static PortableExecutableReference MissingTypesEquality2
            {
                get
                {
                    if (_MissingTypesEquality2 == null)
                    {
                        _MissingTypesEquality2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality2).GetReference(display: "MissingTypesEquality2.Dll");
                    }

                    return _MissingTypesEquality2;
                }
            }

            private static PortableExecutableReference _CL2;
            public static PortableExecutableReference CL2
            {
                get
                {
                    if (_CL2 == null)
                    {
                        _CL2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.CL2).GetReference(display: "CL2.Dll");
                    }

                    return _CL2;
                }
            }

            private static PortableExecutableReference _CL3;
            public static PortableExecutableReference CL3
            {
                get
                {
                    if (_CL3 == null)
                    {
                        _CL3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.MissingTypes.CL3).GetReference(display: "CL3.Dll");
                    }

                    return _CL3;
                }
            }
        }

        public static class TypeForwarders
        {
            public static class TypeForwarder
            {
                private static PortableExecutableReference _TypeForwarder2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_TypeForwarder2 == null)
                        {
                            _TypeForwarder2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarder).GetReference(display: "TypeForwarder.Dll");
                        }

                        return _TypeForwarder2;
                    }
                }
            }

            public static class TypeForwarderLib
            {
                private static PortableExecutableReference _TypeForwarderLib2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_TypeForwarderLib2 == null)
                        {
                            _TypeForwarderLib2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarderLib).GetReference(display: "TypeForwarderLib.Dll");
                        }

                        return _TypeForwarderLib2;
                    }
                }
            }

            public static class TypeForwarderBase
            {
                private static PortableExecutableReference _TypeForwarderBase2;
                public static PortableExecutableReference dll
                {
                    get
                    {
                        if (_TypeForwarderBase2 == null)
                        {
                            _TypeForwarderBase2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.TypeForwarders.TypeForwarderBase).GetReference(display: "TypeForwarderBase.Dll");
                        }

                        return _TypeForwarderBase2;
                    }
                }
            }
        }

        public static class MultiTargeting
        {
            private static PortableExecutableReference _Source1Module;
            public static PortableExecutableReference Source1Module
            {
                get
                {
                    if (_Source1Module == null)
                    {
                        _Source1Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source1Module).GetReference(display: "Source1Module.netmodule");
                    }

                    return _Source1Module;
                }
            }

            private static PortableExecutableReference _Source3Module;
            public static PortableExecutableReference Source3Module
            {
                get
                {
                    if (_Source3Module == null)
                    {
                        _Source3Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source3Module).GetReference(display: "Source3Module.netmodule");
                    }

                    return _Source3Module;
                }
            }

            private static PortableExecutableReference _Source4Module;
            public static PortableExecutableReference Source4Module
            {
                get
                {
                    if (_Source4Module == null)
                    {
                        _Source4Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source4Module).GetReference(display: "Source4Module.netmodule");
                    }

                    return _Source4Module;
                }
            }

            private static PortableExecutableReference _Source5Module;
            public static PortableExecutableReference Source5Module
            {
                get
                {
                    if (_Source5Module == null)
                    {
                        _Source5Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source5Module).GetReference(display: "Source5Module.netmodule");
                    }

                    return _Source5Module;
                }
            }

            private static PortableExecutableReference _Source7Module;
            public static PortableExecutableReference Source7Module
            {
                get
                {
                    if (_Source7Module == null)
                    {
                        _Source7Module = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source7Module).GetReference(display: "Source7Module.netmodule");
                    }

                    return _Source7Module;
                }
            }
        }

        public static class NoPia
        {
            private static PortableExecutableReference _StdOle;
            public static PortableExecutableReference StdOle
            {
                get
                {
                    if (_StdOle == null)
                    {
                        _StdOle = AssemblyMetadata.CreateFromImage(ProprietaryTestResources.SymbolsTests.NoPia.stdole).GetReference(display: "stdole.dll");
                    }

                    return _StdOle;
                }
            }

            private static PortableExecutableReference _Pia1;
            public static PortableExecutableReference Pia1
            {
                get
                {
                    if (_Pia1 == null)
                    {
                        _Pia1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia1).GetReference(display: "Pia1.dll");
                    }

                    return _Pia1;
                }
            }

            private static PortableExecutableReference _Pia1Copy;
            public static PortableExecutableReference Pia1Copy
            {
                get
                {
                    if (_Pia1Copy == null)
                    {
                        _Pia1Copy = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia1Copy).GetReference(display: "Pia1Copy.dll");
                    }

                    return _Pia1Copy;
                }
            }

            private static PortableExecutableReference _Pia2;
            public static PortableExecutableReference Pia2
            {
                get
                {
                    if (_Pia2 == null)
                    {
                        _Pia2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia2).GetReference(display: "Pia2.dll");
                    }

                    return _Pia2;
                }
            }

            private static PortableExecutableReference _Pia3;
            public static PortableExecutableReference Pia3
            {
                get
                {
                    if (_Pia3 == null)
                    {
                        _Pia3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia3).GetReference(display: "Pia3.dll");
                    }

                    return _Pia3;
                }
            }

            private static PortableExecutableReference _Pia4;
            public static PortableExecutableReference Pia4
            {
                get
                {
                    if (_Pia4 == null)
                    {
                        _Pia4 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia4).GetReference(display: "Pia4.dll");
                    }

                    return _Pia4;
                }
            }

            private static PortableExecutableReference _Pia5;
            public static PortableExecutableReference Pia5
            {
                get
                {
                    if (_Pia5 == null)
                    {
                        _Pia5 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Pia5).GetReference(display: "Pia5.dll");
                    }

                    return _Pia5;
                }
            }

            private static PortableExecutableReference _GeneralPia;
            public static PortableExecutableReference GeneralPia
            {
                get
                {
                    if (_GeneralPia == null)
                    {
                        _GeneralPia = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.GeneralPia).GetReference(display: "GeneralPia.dll");
                    }

                    return _GeneralPia;
                }
            }

            private static PortableExecutableReference _GeneralPiaCopy;
            public static PortableExecutableReference GeneralPiaCopy
            {
                get
                {
                    if (_GeneralPiaCopy == null)
                    {
                        _GeneralPiaCopy = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.GeneralPiaCopy).GetReference(display: "GeneralPiaCopy.dll");
                    }

                    return _GeneralPiaCopy;
                }
            }

            private static PortableExecutableReference _NoPIAGenericsAsm1;
            public static PortableExecutableReference NoPIAGenericsAsm1
            {
                get
                {
                    if (_NoPIAGenericsAsm1 == null)
                    {
                        _NoPIAGenericsAsm1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.NoPIAGenerics1_Asm1).GetReference(display: "NoPIAGenerics1-Asm1.dll");
                    }

                    return _NoPIAGenericsAsm1;
                }
            }

            private static PortableExecutableReference _ExternalAsm1;
            public static PortableExecutableReference ExternalAsm1
            {
                get
                {
                    if (_ExternalAsm1 == null)
                    {
                        _ExternalAsm1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.ExternalAsm1).GetReference(display: "ExternalAsm1.dll");
                    }

                    return _ExternalAsm1;
                }
            }

            private static PortableExecutableReference _Library1;
            public static PortableExecutableReference Library1
            {
                get
                {
                    if (_Library1 == null)
                    {
                        _Library1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Library1).GetReference(display: "Library1.dll");
                    }

                    return _Library1;
                }
            }

            private static PortableExecutableReference _Library2;
            public static PortableExecutableReference Library2
            {
                get
                {
                    if (_Library2 == null)
                    {
                        _Library2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.Library2).GetReference(display: "Library2.dll");
                    }

                    return _Library2;
                }
            }

            private static PortableExecutableReference _LocalTypes1;
            public static PortableExecutableReference LocalTypes1
            {
                get
                {
                    if (_LocalTypes1 == null)
                    {
                        _LocalTypes1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes1).GetReference(display: "LocalTypes1.dll");
                    }

                    return _LocalTypes1;
                }
            }

            private static PortableExecutableReference _LocalTypes2;
            public static PortableExecutableReference LocalTypes2
            {
                get
                {
                    if (_LocalTypes2 == null)
                    {
                        _LocalTypes2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes2).GetReference(display: "LocalTypes2.dll");
                    }

                    return _LocalTypes2;
                }
            }

            private static PortableExecutableReference _LocalTypes3;
            public static PortableExecutableReference LocalTypes3
            {
                get
                {
                    if (_LocalTypes3 == null)
                    {
                        _LocalTypes3 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.LocalTypes3).GetReference(display: "LocalTypes3.dll");
                    }

                    return _LocalTypes3;
                }
            }

            private static PortableExecutableReference _A;
            public static PortableExecutableReference A
            {
                get
                {
                    if (_A == null)
                    {
                        _A = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.A).GetReference(display: "A.dll");
                    }

                    return _A;
                }
            }

            private static PortableExecutableReference _B;
            public static PortableExecutableReference B
            {
                get
                {
                    if (_B == null)
                    {
                        _B = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.B).GetReference(display: "B.dll");
                    }

                    return _B;
                }
            }

            private static PortableExecutableReference _C;
            public static PortableExecutableReference C
            {
                get
                {
                    if (_C == null)
                    {
                        _C = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.C).GetReference(display: "C.dll");
                    }

                    return _C;
                }
            }

            private static PortableExecutableReference _D;
            public static PortableExecutableReference D
            {
                get
                {
                    if (_D == null)
                    {
                        _D = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.D).GetReference(display: "D.dll");
                    }

                    return _D;
                }
            }

            public static class Microsoft
            {
                public static class VisualStudio
                {
                    private static PortableExecutableReference _MissingPIAAttributes;
                    public static PortableExecutableReference MissingPIAAttributes
                    {
                        get
                        {
                            if (_MissingPIAAttributes == null)
                            {
                                _MissingPIAAttributes = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.NoPia.MissingPIAAttributes).GetReference(display: "MicrosoftPIAAttributes.dll");
                            }

                            return _MissingPIAAttributes;
                        }
                    }
                }
            }
        }

        public static class Interface
        {
            private static PortableExecutableReference _StaticMethodInInterface;
            public static PortableExecutableReference StaticMethodInInterface
            {
                get
                {
                    if (_StaticMethodInInterface == null)
                    {
                        _StaticMethodInInterface = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests._Interface.StaticMethodInInterface).GetReference(display: "StaticMethodInInterface.dll");
                    }

                    return _StaticMethodInInterface;
                }
            }

            private static PortableExecutableReference _MDInterfaceMapping;
            public static PortableExecutableReference MDInterfaceMapping
            {
                get
                {
                    if (_MDInterfaceMapping == null)
                    {
                        _MDInterfaceMapping = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests._Interface.MDInterfaceMapping).GetReference(display: "MDInterfaceMapping.dll");
                    }

                    return _MDInterfaceMapping;
                }
            }
        }

        public static class MetadataCache
        {
            private static PortableExecutableReference _MDTestLib1;
            public static PortableExecutableReference MDTestLib1
            {
                get
                {
                    if (_MDTestLib1 == null)
                    {
                        _MDTestLib1 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.MDTestLib1).GetReference(display: "MDTestLib1.dll");
                    }

                    return _MDTestLib1;
                }
            }

            private static PortableExecutableReference _netModule1;
            public static PortableExecutableReference netModule1
            {
                get
                {
                    if (_netModule1 == null)
                    {
                        _netModule1 = ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1).GetReference(display: "netModule1.netmodule");
                    }

                    return _netModule1;
                }
            }
        }

        public static class ExplicitInterfaceImplementation
        {
            public static class Methods
            {
                private static PortableExecutableReference _CSharp;
                public static PortableExecutableReference CSharp
                {
                    get
                    {
                        if (_CSharp == null)
                        {
                            _CSharp = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.CSharpExplicitInterfaceImplementation).GetReference(display: "CSharpExplicitInterfaceImplementation.dll");
                        }

                        return _CSharp;
                    }
                }

                private static PortableExecutableReference _IL;
                public static PortableExecutableReference IL
                {
                    get
                    {
                        if (_IL == null)
                        {
                            _IL = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.ILExplicitInterfaceImplementation).GetReference(display: "ILExplicitInterfaceImplementation.dll");
                        }

                        return _IL;
                    }
                }
            }

            public static class Properties
            {
                private static PortableExecutableReference _CSharp;
                public static PortableExecutableReference CSharp
                {
                    get
                    {
                        if (_CSharp == null)
                        {
                            _CSharp = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.CSharpExplicitInterfaceImplementationProperties).GetReference(display: "CSharpExplicitInterfaceImplementationProperties.dll");
                        }

                        return _CSharp;
                    }
                }

                private static PortableExecutableReference _IL;
                public static PortableExecutableReference IL
                {
                    get
                    {
                        if (_IL == null)
                        {
                            _IL = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.ILExplicitInterfaceImplementationProperties).GetReference(display: "ILExplicitInterfaceImplementationProperties.dll");
                        }

                        return _IL;
                    }
                }
            }

            public static class Events
            {
                private static PortableExecutableReference _CSharp;
                public static PortableExecutableReference CSharp
                {
                    get
                    {
                        if (_CSharp == null)
                        {
                            _CSharp = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.CSharpExplicitInterfaceImplementationEvents).GetReference(display: "CSharpExplicitInterfaceImplementationEvents.dll");
                        }

                        return _CSharp;
                    }
                }
            }
        }

        private static PortableExecutableReference _Regress40025;
        public static PortableExecutableReference Regress40025
        {
            get
            {
                if (_Regress40025 == null)
                {
                    _Regress40025 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.Regress40025DLL).GetReference(display: "Regress40025DLL.dll");
                }

                return _Regress40025;
            }
        }

        public static class WithEvents
        {
            private static PortableExecutableReference _SimpleWithEvents;
            public static PortableExecutableReference SimpleWithEvents
            {
                get
                {
                    if (_SimpleWithEvents == null)
                    {
                        _SimpleWithEvents = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests._WithEvents.SimpleWithEvents).GetReference(display: "SimpleWithEvents.dll");
                    }

                    return _SimpleWithEvents;
                }
            }
        }

        public static class DelegateImplementation
        {
            private static PortableExecutableReference _DelegatesWithoutInvoke;
            public static PortableExecutableReference DelegatesWithoutInvoke
            {
                get
                {
                    if (_DelegatesWithoutInvoke == null)
                    {
                        _DelegatesWithoutInvoke = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.DelegatesWithoutInvoke).GetReference(display: "DelegatesWithoutInvoke.dll");
                    }

                    return _DelegatesWithoutInvoke;
                }
            }

            private static PortableExecutableReference _DelegateByRefParamArray;
            public static PortableExecutableReference DelegateByRefParamArray
            {
                get
                {
                    if (_DelegateByRefParamArray == null)
                    {
                        _DelegateByRefParamArray = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.DelegateByRefParamArray).GetReference(display: "DelegateByRefParamArray.dll");
                    }

                    return _DelegateByRefParamArray;
                }
            }
        }

        public static class Metadata
        {
            private static PortableExecutableReference _InvalidCharactersInAssemblyName2;
            public static PortableExecutableReference InvalidCharactersInAssemblyName
            {
                get
                {
                    if (_InvalidCharactersInAssemblyName2 == null)
                    {
                        _InvalidCharactersInAssemblyName2 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.InvalidCharactersInAssemblyName).GetReference(display: "InvalidCharactersInAssemblyName.dll");
                    }

                    return _InvalidCharactersInAssemblyName2;
                }
            }

            private static PortableExecutableReference _MDTestAttributeDefLib;
            public static PortableExecutableReference MDTestAttributeDefLib
            {
                get
                {
                    if (_MDTestAttributeDefLib == null)
                    {
                        _MDTestAttributeDefLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib).GetReference(display: "MDTestAttributeDefLib.dll");
                    }

                    return _MDTestAttributeDefLib;
                }
            }

            private static PortableExecutableReference _MDTestAttributeApplicationLib;
            public static PortableExecutableReference MDTestAttributeApplicationLib
            {
                get
                {
                    if (_MDTestAttributeApplicationLib == null)
                    {
                        _MDTestAttributeApplicationLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib).GetReference(display: "MDTestAttributeApplicationLib.dll");
                    }

                    return _MDTestAttributeApplicationLib;
                }
            }

            private static PortableExecutableReference _AttributeInterop01;
            public static PortableExecutableReference AttributeInterop01
            {
                get
                {
                    if (_AttributeInterop01 == null)
                    {
                        _AttributeInterop01 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeInterop01).GetReference(display: "AttributeInterop01.dll");
                    }

                    return _AttributeInterop01;
                }
            }

            private static PortableExecutableReference _AttributeInterop02;
            public static PortableExecutableReference AttributeInterop02
            {
                get
                {
                    if (_AttributeInterop02 == null)
                    {
                        _AttributeInterop02 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeInterop02).GetReference(display: "AttributeInterop02.dll");
                    }

                    return _AttributeInterop02;
                }
            }

            private static PortableExecutableReference _AttributeTestLib01;
            public static PortableExecutableReference AttributeTestLib01
            {
                get
                {
                    if (_AttributeTestLib01 == null)
                    {
                        _AttributeTestLib01 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestLib01).GetReference(display: "AttributeTestLib01.dll");
                    }

                    return _AttributeTestLib01;
                }
            }

            private static PortableExecutableReference _AttributeTestDef01;
            public static PortableExecutableReference AttributeTestDef01
            {
                get
                {
                    if (_AttributeTestDef01 == null)
                    {
                        _AttributeTestDef01 = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.AttributeTestDef01).GetReference(display: "AttributeTestDef01.dll");
                    }

                    return _AttributeTestDef01;
                }
            }

            private static PortableExecutableReference _DynamicAttributeLib;
            public static PortableExecutableReference DynamicAttributeLib
            {
                get
                {
                    if (_DynamicAttributeLib == null)
                    {
                        _DynamicAttributeLib = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.Metadata.DynamicAttribute).GetReference(display: "DynamicAttribute.dll");
                    }

                    return _DynamicAttributeLib;
                }
            }
        }

        public static class UseSiteErrors
        {
            private static PortableExecutableReference _Unavailable;
            public static PortableExecutableReference Unavailable
            {
                get
                {
                    if (_Unavailable == null)
                    {
                        _Unavailable = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.Unavailable).GetReference(display: "Unavailable.dll");
                    }

                    return _Unavailable;
                }
            }

            private static PortableExecutableReference _CSharp;
            public static PortableExecutableReference CSharp
            {
                get
                {
                    if (_CSharp == null)
                    {
                        _CSharp = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.CSharpErrors).GetReference(display: "CSharpErrors.dll");
                    }

                    return _CSharp;
                }
            }

            private static PortableExecutableReference _IL;
            public static PortableExecutableReference IL
            {
                get
                {
                    if (_IL == null)
                    {
                        _IL = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.ILErrors).GetReference(display: "ILErrors.dll");
                    }

                    return _IL;
                }
            }
        }

        public static class Versioning
        {
            private static PortableExecutableReference _AR_SA;
            public static PortableExecutableReference AR_SA
            {
                get
                {
                    if (_AR_SA == null)
                    {
                        _AR_SA = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.Culture_AR_SA).GetReference(display: "AR-SA");
                    }

                    return _AR_SA;
                }
            }

            private static PortableExecutableReference _EN_US;
            public static PortableExecutableReference EN_US
            {
                get
                {
                    if (_EN_US == null)
                    {
                        _EN_US = AssemblyMetadata.CreateFromImage(TestResources.SymbolsTests.General.Culture_EN_US).GetReference(display: "EN-US");
                    }

                    return _EN_US;
                }
            }
        }
    }
}
