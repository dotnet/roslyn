// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using ProprietaryTestResources = Microsoft.CodeAnalysis.Test.Resources.Proprietary;

public static class TestReferences
{
    public static class MetadataTests
    {
        public static class NetModule01
        {
            private static MetadataImageReference _AppCS;
            public static MetadataImageReference AppCS
            {
                get
                {
                    if (_AppCS == null)
                    {
                        _AppCS = new MetadataImageReference(TestResources.MetadataTests.NetModule01.AppCS, display: "AppCS");
                    }

                    return _AppCS;
                }
            }

            private static MetadataImageReference _ModuleCS00;
            public static MetadataImageReference ModuleCS00
            {
                get
                {
                    if (_ModuleCS00 == null)
                    {
                        _ModuleCS00 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS00), display: "ModuleCS00.mod");
                    }

                    return _ModuleCS00;
                }
            }

            private static MetadataImageReference _ModuleCS01;
            public static MetadataImageReference ModuleCS01
            {
                get
                {
                    if (_ModuleCS01 == null)
                    {
                        _ModuleCS01 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleCS01), display: "ModuleCS01.mod");
                    }

                    return _ModuleCS01;
                }
            }

            private static MetadataImageReference _ModuleVB01;
            public static MetadataImageReference ModuleVB01
            {
                get
                {
                    if (_ModuleVB01 == null)
                    {
                        _ModuleVB01 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.MetadataTests.NetModule01.ModuleVB01), display: "ModuleVB01.mod");
                    }

                    return _ModuleVB01;
                }
            }
        }

        public static class InterfaceAndClass
        {
            private static MetadataImageReference _CSClasses01;
            public static MetadataImageReference CSClasses01
            {
                get
                {
                    if (_CSClasses01 == null)
                    {
                        _CSClasses01 = new MetadataImageReference(TestResources.MetadataTests.InterfaceAndClass.CSClasses01, display: "CSClasses01.dll");
                    }

                    return _CSClasses01;
                }
            }

            private static MetadataImageReference _CSInterfaces01;
            public static MetadataImageReference CSInterfaces01
            {
                get
                {
                    if (_CSInterfaces01 == null)
                    {
                        _CSInterfaces01 = new MetadataImageReference(TestResources.MetadataTests.InterfaceAndClass.CSInterfaces01, display: "CSInterfaces01.dll");
                    }

                    return _CSInterfaces01;
                }
            }

            private static MetadataImageReference _VBClasses01;
            public static MetadataImageReference VBClasses01
            {
                get
                {
                    if (_VBClasses01 == null)
                    {
                        _VBClasses01 = new MetadataImageReference(TestResources.MetadataTests.InterfaceAndClass.VBClasses01, display: "VBClasses01.dll");
                    }

                    return _VBClasses01;
                }
            }

            private static MetadataImageReference _VBClasses02;
            public static MetadataImageReference VBClasses02
            {
                get
                {
                    if (_VBClasses02 == null)
                    {
                        _VBClasses02 = new MetadataImageReference(TestResources.MetadataTests.InterfaceAndClass.VBClasses02, display: "VBClasses02.dll");
                    }

                    return _VBClasses02;
                }
            }

            private static MetadataImageReference _VBInterfaces01;
            public static MetadataImageReference VBInterfaces01
            {
                get
                {
                    if (_VBInterfaces01 == null)
                    {
                        _VBInterfaces01 = new MetadataImageReference(TestResources.MetadataTests.InterfaceAndClass.VBInterfaces01, display: "VBInterfaces01.dll");
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
            private static MetadataImageReference _System;
            public static MetadataImageReference System
            {
                get
                {
                    if (_System == null)
                    {
                        _System = new MetadataImageReference(ProprietaryTestResources.NetFX.silverlight_v5_0_5_0.System_v5_0_5_0_silverlight, display: "System.v5.0.5.0_silverlight.dll");
                    }

                    return _System;
                }
            }
        }

        public static class v4_0_21006
        {
            private static MetadataImageReference _mscorlib;
            public static MetadataImageReference mscorlib
            {
                get
                {
                    if (_mscorlib == null)
                    {
                        _mscorlib = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib, display: "mscorlib.dll");
                    }

                    return _mscorlib;
                }
            }
        }

        public static class v2_0_50727
        {
            private static MetadataImageReference _mscorlib;
            public static MetadataImageReference mscorlib
            {
                get
                {
                    if (_mscorlib == null)
                    {
                        _mscorlib = new MetadataImageReference(ProprietaryTestResources.NetFX.v2_0_50727.mscorlib, display: "mscorlib, v2.0.50727");
                    }

                    return _mscorlib;
                }
            }

            private static MetadataImageReference _System;
            public static MetadataImageReference System
            {
                get
                {
                    if (_System == null)
                    {
                        _System = new MetadataImageReference(ProprietaryTestResources.NetFX.v2_0_50727.System, display: "System.dll");
                    }

                    return _System;
                }
            }

            private static MetadataImageReference _Microsoft_VisualBasic;
            public static MetadataImageReference Microsoft_VisualBasic
            {
                get
                {
                    if (_Microsoft_VisualBasic == null)
                    {
                        _Microsoft_VisualBasic = new MetadataImageReference(ProprietaryTestResources.NetFX.v2_0_50727.Microsoft_VisualBasic, display: "Microsoft.VisualBasic.dll");
                    }

                    return _Microsoft_VisualBasic;
                }
            }
        }

        public static class v3_5_30729
        {
            private static MetadataImageReference _SystemCore;
            public static MetadataImageReference SystemCore
            {
                get
                {
                    if (_SystemCore == null)
                    {
                        _SystemCore = new MetadataImageReference(ProprietaryTestResources.NetFX.v3_5_30729.System_Core_v3_5_30729.AsImmutableOrNull(), display: "System.Core, v3.5.30729");
                    }

                    return _SystemCore;
                }
            }
        }

        public static class v4_0_30319
        {
            private static MetadataImageReference _mscorlib;
            public static MetadataImageReference mscorlib
            {
                get
                {
                    if (_mscorlib == null)
                    {
                        _mscorlib = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_21006.mscorlib, fullPath: @"R:\v4_0_30319\mscorlib.dll", display: "mscorlib.dll");
                    }

                    return _mscorlib;
                }
            }

            private static MetadataImageReference _System_Core;
            public static MetadataImageReference System_Core
            {
                get
                {
                    if (_System_Core == null)
                    {
                        _System_Core = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_Core, fullPath: @"R:\v4_0_30319\System.Core.dll", display: "System.Core.dll");
                    }

                    return _System_Core;
                }
            }

            private static MetadataImageReference _System_Configuration;
            public static MetadataImageReference System_Configuration
            {
                get
                {
                    if (_System_Configuration == null)
                    {
                        _System_Configuration = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_Configuration, fullPath: @"R:\v4_0_30319\System.Configuration.dll", display: "System.Configuration.dll");
                    }

                    return _System_Configuration;
                }
            }

            private static MetadataImageReference _System;
            public static MetadataImageReference System
            {
                get
                {
                    if (_System == null)
                    {
                        _System = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System, fullPath: @"R:\v4_0_30319\System.dll", display: "System.dll");
                    }

                    return _System;
                }
            }

            private static MetadataImageReference _System_Data;
            public static MetadataImageReference System_Data
            {
                get
                {
                    if (_System_Data == null)
                    {
                        _System_Data = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_Data, fullPath: @"R:\v4_0_30319\System.Data.dll", display: "System.Data.dll");
                    }

                    return _System_Data;
                }
            }

            private static MetadataImageReference _System_Xml;
            public static MetadataImageReference System_Xml
            {
                get
                {
                    if (_System_Xml == null)
                    {
                        _System_Xml = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_Xml, fullPath: @"R:\v4_0_30319\System.Xml.dll", display: "System.Xml.dll");
                    }

                    return _System_Xml;
                }
            }

            private static MetadataImageReference _System_Xml_Linq;
            public static MetadataImageReference System_Xml_Linq
            {
                get
                {
                    if (_System_Xml_Linq == null)
                    {
                        _System_Xml_Linq = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_Xml_Linq, fullPath: @"R:\v4_0_30319\System.Xml.Linq.dll", display: "System.Xml.Linq.dll");
                    }

                    return _System_Xml_Linq;
                }
            }

            private static MetadataImageReference _System_Windows_Forms;
            public static MetadataImageReference System_Windows_Forms
            {
                get
                {
                    if (_System_Windows_Forms == null)
                    {
                        _System_Windows_Forms = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_Windows_Forms, fullPath: @"R:\v4_0_30319\System.Windows.Forms.dll", display: "System.Windows.Forms.dll");
                    }

                    return _System_Windows_Forms;
                }
            }

            private static MetadataImageReference _Microsoft_CSharp;
            public static MetadataImageReference Microsoft_CSharp
            {
                get
                {
                    if (_Microsoft_CSharp == null)
                    {
                        _Microsoft_CSharp = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_CSharp, fullPath: @"R:\v4_0_30319\Microsoft.CSharp.dll", display: "Microsoft.CSharp.dll");
                    }

                    return _Microsoft_CSharp;
                }
            }

            private static MetadataImageReference _Microsoft_VisualBasic;
            public static MetadataImageReference Microsoft_VisualBasic
            {
                get
                {
                    if (_Microsoft_VisualBasic == null)
                    {
                        _Microsoft_VisualBasic = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_VisualBasic, fullPath: @"R:\v4_0_30319\Microsoft.VisualBasic.dll", display: "Microsoft.VisualBasic.dll");
                    }

                    return _Microsoft_VisualBasic;
                }
            }

            private static MetadataImageReference _Microsoft_JScript;
            public static MetadataImageReference Microsoft_JScript
            {
                get
                {
                    if (_Microsoft_JScript == null)
                    {
                        _Microsoft_JScript = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.Microsoft_JScript, display: "Microsoft.JScript.dll");
                    }

                    return _Microsoft_JScript;
                }
            }

            private static MetadataImageReference _System_ComponentModel_Composition;
            public static MetadataImageReference System_ComponentModel_Composition
            {
                get
                {
                    if (_System_ComponentModel_Composition == null)
                    {
                        _System_ComponentModel_Composition = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_ComponentModel_Composition, display: "System.ComponentModel.Composition.dll");
                    }

                    return _System_ComponentModel_Composition;
                }
            }

            private static MetadataImageReference _System_Web_Services;
            public static MetadataImageReference System_Web_Services
            {
                get
                {
                    if (_System_Web_Services == null)
                    {
                        _System_Web_Services = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_Web_Services, display: "System.Web.Services.dll");
                    }

                    return _System_Web_Services;
                }
            }

            public static class System_EnterpriseServices
            {
                private static MetadataImageReference _System_EnterpriseServices;

                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_System_EnterpriseServices == null)
                        {
                            _System_EnterpriseServices = new MetadataImageReference(ProprietaryTestResources.NetFX.v4_0_30319.System_EnterpriseServices, display: "System.EnterpriseServices.dll");
                        }

                        return _System_EnterpriseServices;
                    }
                }
            }
        }
    }

    public static class DiagnosticTests
    {
        public static class ErrTestLib01
        {
            private static MetadataImageReference _ErrTestLib01;
            public static MetadataImageReference dll
            {
                get
                {
                    if (_ErrTestLib01 == null)
                    {
                        _ErrTestLib01 = new MetadataImageReference(TestResources.DiagnosticTests.DiagnosticTests.ErrTestLib01, display: "ErrTestLib01.dll");
                    }

                    return _ErrTestLib01;
                }
            }
        }

        public static class ErrTestLib02
        {
            private static MetadataImageReference _ErrTestLib02;
            public static MetadataImageReference dll
            {
                get
                {
                    if (_ErrTestLib02 == null)
                    {
                        _ErrTestLib02 = new MetadataImageReference(TestResources.DiagnosticTests.DiagnosticTests.ErrTestLib02, display: "ErrTestLib02.dll");
                    }

                    return _ErrTestLib02;
                }
            }
        }

        public static class ErrTestLib11
        {
            private static MetadataImageReference _ErrTestLib11;
            public static MetadataImageReference dll
            {
                get
                {
                    if (_ErrTestLib11 == null)
                    {
                        _ErrTestLib11 = new MetadataImageReference(TestResources.DiagnosticTests.DiagnosticTests.ErrTestLib11, display: "ErrTestLib11.dll");
                    }

                    return _ErrTestLib11;
                }
            }
        }

        public static class ErrTestMod01
        {
            private static MetadataImageReference _ErrTestMod01;
            public static MetadataImageReference dll
            {
                get
                {
                    if (_ErrTestMod01 == null)
                    {
                        _ErrTestMod01 = new MetadataImageReference(TestResources.DiagnosticTests.DiagnosticTests.ErrTestMod01, display: "ErrTestMod01.dll");
                    }

                    return _ErrTestMod01;
                }
            }
        }

        public static class ErrTestMod02
        {
            private static MetadataImageReference _ErrTestMod02;
            public static MetadataImageReference dll
            {
                get
                {
                    if (_ErrTestMod02 == null)
                    {
                        _ErrTestMod02 = new MetadataImageReference(TestResources.DiagnosticTests.DiagnosticTests.ErrTestMod02, display: "ErrTestMod02.dll");
                    }

                    return _ErrTestMod02;
                }
            }
        }

        public static class badresfile
        {
            private static MetadataImageReference _badresfile;
            public static MetadataImageReference res
            {
                get
                {
                    if (_badresfile == null)
                    {
                        _badresfile = new MetadataImageReference(TestResources.DiagnosticTests.DiagnosticTests.badresfile, display: "badresfile.res");
                    }

                    return _badresfile;
                }
            }
        }
    }

    public static class SymbolsTests
    {
        private static MetadataImageReference _mdTestLib1;
        public static MetadataImageReference MDTestLib1
        {
            get
            {
                if (_mdTestLib1 == null)
                {
                    _mdTestLib1 = new MetadataImageReference(TestResources.SymbolsTests.General.MDTestLib1, display: "MDTestLib1.dll");
                }

                return _mdTestLib1;
            }
        }

        private static MetadataImageReference _mdTestLib2;
        public static MetadataImageReference MDTestLib2
        {
            get
            {
                if (_mdTestLib2 == null)
                {
                    _mdTestLib2 = new MetadataImageReference(TestResources.SymbolsTests.General.MDTestLib2, display: "MDTestLib2.dll");
                }

                return _mdTestLib2;
            }
        }

        private static MetadataImageReference _VBConversions;
        public static MetadataImageReference VBConversions
        {
            get
            {
                if (_VBConversions == null)
                {
                    _VBConversions = new MetadataImageReference(TestResources.SymbolsTests.General.VBConversions, display: "VBConversions.dll");
                }

                return _VBConversions;
            }
        }

        private static MetadataImageReference _WithSpaces;
        public static MetadataImageReference WithSpaces
        {
            get
            {
                if (_WithSpaces == null)
                {
                    _WithSpaces = new MetadataImageReference(TestResources.SymbolsTests.General.With_Spaces, display: "With Spaces.dll");
                }

                return _WithSpaces;
            }
        }

        private static MetadataImageReference _WithSpacesModule;
        public static MetadataImageReference WithSpacesModule
        {
            get
            {
                if (_WithSpacesModule == null)
                {
                    _WithSpacesModule = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.General.With_SpacesModule), display: "With Spaces.netmodule");
                }

                return _WithSpacesModule;
            }
        }

        private static MetadataImageReference _InheritIComparable;
        public static MetadataImageReference InheritIComparable
        {
            get
            {
                if (_InheritIComparable == null)
                {
                    _InheritIComparable = new MetadataImageReference(TestResources.SymbolsTests.General.InheritIComparable, display: "InheritIComparable.dll");
                }

                return _InheritIComparable;
            }
        }

        private static MetadataImageReference _BigVisitor;
        public static MetadataImageReference BigVisitor
        {
            get
            {
                if (_BigVisitor == null)
                {
                    _BigVisitor = new MetadataImageReference(TestResources.SymbolsTests.General.BigVisitor, display: "BigVisitor.dll");
                }

                return _BigVisitor;
            }
        }

        private static MetadataImageReference _Properties;
        public static MetadataImageReference Properties
        {
            get
            {
                if (_Properties == null)
                {
                    _Properties = new MetadataImageReference(TestResources.SymbolsTests.General.Properties, display: "Properties.dll");
                }

                return _Properties;
            }
        }

        private static MetadataImageReference _PropertiesWithByRef;
        public static MetadataImageReference PropertiesWithByRef
        {
            get
            {
                if (_PropertiesWithByRef == null)
                {
                    _PropertiesWithByRef = new MetadataImageReference(TestResources.SymbolsTests.General.PropertiesWithByRef, display: "PropertiesWithByRef.dll");
                }

                return _PropertiesWithByRef;
            }
        }

        private static MetadataImageReference _Indexers;
        public static MetadataImageReference Indexers
        {
            get
            {
                if (_Indexers == null)
                {
                    _Indexers = new MetadataImageReference(TestResources.SymbolsTests.General.Indexers, display: "Indexers.dll");
                }

                return _Indexers;
            }
        }

        private static MetadataImageReference _Events;
        public static MetadataImageReference Events
        {
            get
            {
                if (_Events == null)
                {
                    _Events = new MetadataImageReference(TestResources.SymbolsTests.General.Events, display: "Events.dll");
                }

                return _Events;
            }
        }

        public static class netModule
        {
            private static MetadataImageReference _netModule1;
            public static MetadataImageReference netModule1
            {
                get
                {
                    if (_netModule1 == null)
                    {
                        _netModule1 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1), display: "netModule1.netmodule");
                    }

                    return _netModule1;
                }
            }

            private static MetadataImageReference _netModule2;
            public static MetadataImageReference netModule2
            {
                get
                {
                    if (_netModule2 == null)
                    {
                        _netModule2 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule2), display: "netModule2.netmodule");
                    }

                    return _netModule2;
                }
            }

            private static MetadataImageReference _CrossRefModule1;
            public static MetadataImageReference CrossRefModule1
            {
                get
                {
                    if (_CrossRefModule1 == null)
                    {
                        _CrossRefModule1 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1), display: "CrossRefModule1.netmodule");
                    }

                    return _CrossRefModule1;
                }
            }

            private static MetadataImageReference _CrossRefModule2;
            public static MetadataImageReference CrossRefModule2
            {
                get
                {
                    if (_CrossRefModule2 == null)
                    {
                        _CrossRefModule2 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2), display: "CrossRefModule2.netmodule");
                    }

                    return _CrossRefModule2;
                }
            }

            private static MetadataImageReference _CrossRefLib;
            public static MetadataImageReference CrossRefLib
            {
                get
                {
                    if (_CrossRefLib == null)
                    {
                        _CrossRefLib = new MetadataImageReference(
                            AssemblyMetadata.Create(
                                ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefLib),
                                ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule1),
                                ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.CrossRefModule2)), 
                            display: "CrossRefLib.dll");
                    }

                    return _CrossRefLib;
                }
            }

            private static MetadataImageReference _hash_module;
            public static MetadataImageReference hash_module
            {
                get
                {
                    if (_hash_module == null)
                    {
                        _hash_module = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.hash_module), display: "hash_module.netmodule");
                    }

                    return _hash_module;
                }
            }

            private static MetadataImageReference _x64COFF;
            public static MetadataImageReference x64COFF
            {
                get
                {
                    if (_x64COFF == null)
                    {
                        _x64COFF = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.x64COFF), display: "x64COFF.obj");
                    }

                    return _x64COFF;
                }
            }
        }

        public static class V1
        {
            public static class MTTestLib1
            {
                private static MetadataImageReference _v1MTTestLib1;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = new MetadataImageReference(TestResources.SymbolsTests.V1.MTTestLib1, display: "MTTestLib1.dll");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static MetadataImageReference _v1MTTestLib1;
                public static MetadataImageReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule1), display: "MTTestModule1.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib2
            {
                private static MetadataImageReference _v1MTTestLib2;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_v1MTTestLib2 == null)
                        {
                            _v1MTTestLib2 = new MetadataImageReference(TestResources.SymbolsTests.V1.MTTestLib2, display: "MTTestLib2.dll");
                        }

                        return _v1MTTestLib2;
                    }
                }
            }

            public static class MTTestModule2
            {
                private static MetadataImageReference _v1MTTestLib1;
                public static MetadataImageReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V1.MTTestModule2), display: "MTTestModule2.netmodule");
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
                private static MetadataImageReference _v2MTTestLib1;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_v2MTTestLib1 == null)
                        {
                            _v2MTTestLib1 = new MetadataImageReference(TestResources.SymbolsTests.V2.MTTestLib1, display: "MTTestLib1.dll");
                        }

                        return _v2MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static MetadataImageReference _v1MTTestLib1;
                public static MetadataImageReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = new MetadataImageReference(TestResources.SymbolsTests.V2.MTTestModule1, display: "MTTestModule1.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib3
            {
                private static MetadataImageReference _v2MTTestLib3;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_v2MTTestLib3 == null)
                        {
                            _v2MTTestLib3 = new MetadataImageReference(TestResources.SymbolsTests.V2.MTTestLib3, display: "MTTestLib3.dll");
                        }

                        return _v2MTTestLib3;
                    }
                }
            }

            public static class MTTestModule3
            {
                private static MetadataImageReference _v1MTTestLib1;
                public static MetadataImageReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V2.MTTestModule3), display: "MTTestModule3.netmodule");
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
                private static MetadataImageReference _v3MTTestLib1;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_v3MTTestLib1 == null)
                        {
                            _v3MTTestLib1 = new MetadataImageReference(TestResources.SymbolsTests.V3.MTTestLib1, display: "MTTestLib1.dll");
                        }

                        return _v3MTTestLib1;
                    }
                }
            }

            public static class MTTestModule1
            {
                private static MetadataImageReference _v1MTTestLib1;
                public static MetadataImageReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = new MetadataImageReference(TestResources.SymbolsTests.V3.MTTestModule1, display: "MTTestModule1.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }

            public static class MTTestLib4
            {
                private static MetadataImageReference _v3MTTestLib4;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_v3MTTestLib4 == null)
                        {
                            _v3MTTestLib4 = new MetadataImageReference(TestResources.SymbolsTests.V3.MTTestLib4, display: "MTTestLib4.dll");
                        }

                        return _v3MTTestLib4;
                    }
                }
            }

            public static class MTTestModule4
            {
                private static MetadataImageReference _v1MTTestLib1;
                public static MetadataImageReference netmodule
                {
                    get
                    {
                        if (_v1MTTestLib1 == null)
                        {
                            _v1MTTestLib1 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.V3.MTTestModule4), display: "MTTestModule4.netmodule");
                        }

                        return _v1MTTestLib1;
                    }
                }
            }
        }

        public static class MultiModule
        {
            private static MetadataImageReference _Assembly;
            public static MetadataImageReference Assembly
            {
                get
                {
                    if (_Assembly == null)
                    {
                        _Assembly = new MetadataImageReference(
                            AssemblyMetadata.Create(
                                ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.MultiModule),
                                ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod2),
                                ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiModule.mod3)),
                            display: "MultiModule.dll");
                    }

                    return _Assembly;
                }
            }

            private static MetadataImageReference _mod2;
            public static MetadataImageReference mod2
            {
                get
                {
                    if (_mod2 == null)
                    {
                        _mod2 = new MetadataImageReference(TestResources.SymbolsTests.MultiModule.mod2, display: "mod2.netmodule");
                    }

                    return _mod2;
                }
            }

            private static MetadataImageReference _mod3;
            public static MetadataImageReference mod3
            {
                get
                {
                    if (_mod3 == null)
                    {
                        _mod3 = new MetadataImageReference(TestResources.SymbolsTests.MultiModule.mod3, display: "mod3.netmodule");
                    }

                    return _mod3;
                }
            }

            private static MetadataImageReference _Consumer;
            public static MetadataImageReference Consumer
            {
                get
                {
                    if (_Consumer == null)
                    {
                        _Consumer = new MetadataImageReference(TestResources.SymbolsTests.MultiModule.Consumer, display: "Consumer.dll");
                    }

                    return _Consumer;
                }
            }
        }

        public static class DifferByCase
        {
            private static MetadataImageReference _typeAndNamespaceDifferByCase;
            public static MetadataImageReference TypeAndNamespaceDifferByCase
            {
                get
                {
                    if (_typeAndNamespaceDifferByCase == null)
                    {
                        _typeAndNamespaceDifferByCase = new MetadataImageReference(TestResources.SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase, display: "TypeAndNamespaceDifferByCase.dll");
                    }

                    return _typeAndNamespaceDifferByCase;
                }
            }

            private static MetadataImageReference _DifferByCaseConsumer;
            public static MetadataImageReference Consumer
            {
                get
                {
                    if (_DifferByCaseConsumer == null)
                    {
                        _DifferByCaseConsumer = new MetadataImageReference(TestResources.SymbolsTests.DifferByCase.Consumer, display: "Consumer.dll");
                    }

                    return _DifferByCaseConsumer;
                }
            }

            private static MetadataImageReference _CsharpCaseSen;
            public static MetadataImageReference CsharpCaseSen
            {
                get
                {
                    if (_CsharpCaseSen == null)
                    {
                        _CsharpCaseSen = new MetadataImageReference(TestResources.SymbolsTests.DifferByCase.Consumer, display: "CsharpCaseSen.dll");
                    }

                    return _CsharpCaseSen;
                }
            }

            private static MetadataImageReference _CsharpDifferCaseOverloads;
            public static MetadataImageReference CsharpDifferCaseOverloads
            {
                get
                {
                    if (_CsharpDifferCaseOverloads == null)
                    {
                        _CsharpDifferCaseOverloads = new MetadataImageReference(TestResources.SymbolsTests.DifferByCase.CSharpDifferCaseOverloads, display: "CSharpDifferCaseOverloads.dll");
                    }

                    return _CsharpDifferCaseOverloads;
                }
            }
        }

        public static class CorLibrary
        {
            public static class GuidTest2
            {
                private static MetadataImageReference _exe;
                public static MetadataImageReference exe
                {
                    get
                    {
                        if (_exe == null)
                        {
                            _exe = new MetadataImageReference(TestResources.SymbolsTests.CorLibrary.GuidTest2, display: "GuidTest2.exe");
                        }

                        return _exe;
                    }
                }
            }

            private static MetadataImageReference _NoMsCorLibRef;
            public static MetadataImageReference NoMsCorLibRef
            {
                get
                {
                    if (_NoMsCorLibRef == null)
                    {
                        _NoMsCorLibRef = new MetadataImageReference(TestResources.SymbolsTests.CorLibrary.NoMsCorLibRef, display: "NoMsCorLibRef.dll");
                    }

                    return _NoMsCorLibRef;
                }
            }

            public static class FakeMsCorLib
            {
                private static MetadataImageReference _dll;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_dll == null)
                        {
                            _dll = new MetadataImageReference(TestResources.SymbolsTests.CorLibrary.FakeMsCorLib, display: "FakeMsCorLib.dll");
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
                private static MetadataImageReference _Dll;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_Dll == null)
                        {
                            _Dll = new MetadataImageReference(TestResources.SymbolsTests.CustomModifiers.Modifiers, display: "Modifiers.dll");
                        }

                        return _Dll;
                    }
                }

                private static MetadataImageReference _Module;
                public static MetadataImageReference netmodule
                {
                    get
                    {
                        if (_Module == null)
                        {
                            _Module = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.CustomModifiers.ModifiersModule), display: "Modifiers.netmodule");
                        }

                        return _Module;
                    }
                }
            }

            private static MetadataImageReference _ModoptTests;
            public static MetadataImageReference ModoptTests
            {
                get
                {
                    if (_ModoptTests == null)
                    {
                        _ModoptTests = new MetadataImageReference(TestResources.SymbolsTests.CustomModifiers.ModoptTests, display: "ModoptTests.dll");
                    }

                    return _ModoptTests;
                }
            }

            public static class CppCli
            {
                private static MetadataImageReference _Dll;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_Dll == null)
                        {
                            _Dll = new MetadataImageReference(TestResources.SymbolsTests.CustomModifiers.CppCli, display: "CppCli.dll");
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
                private static MetadataImageReference _Cyclic1;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_Cyclic1 == null)
                        {
                            _Cyclic1 = new MetadataImageReference(TestResources.SymbolsTests.Cyclic.Cyclic1, display: "Cyclic1.dll");
                        }

                        return _Cyclic1;
                    }
                }
            }

            public static class Cyclic2
            {
                private static MetadataImageReference _Cyclic2;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_Cyclic2 == null)
                        {
                            _Cyclic2 = new MetadataImageReference(TestResources.SymbolsTests.Cyclic.Cyclic2, display: "Cyclic2.dll");
                        }

                        return _Cyclic2;
                    }
                }
            }
        }

        public static class CyclicInheritance
        {
            private static MetadataImageReference _Class1;
            public static MetadataImageReference Class1
            {
                get
                {
                    if (_Class1 == null)
                    {
                        _Class1 = new MetadataImageReference(TestResources.SymbolsTests.CyclicInheritance.Class1, display: "Class1.dll");
                    }

                    return _Class1;
                }
            }

            private static MetadataImageReference _Class2;
            public static MetadataImageReference Class2
            {
                get
                {
                    if (_Class2 == null)
                    {
                        _Class2 = new MetadataImageReference(TestResources.SymbolsTests.CyclicInheritance.Class2, display: "Class2.dll");
                    }

                    return _Class2;
                }
            }

            private static MetadataImageReference _Class3;
            public static MetadataImageReference Class3
            {
                get
                {
                    if (_Class3 == null)
                    {
                        _Class3 = new MetadataImageReference(TestResources.SymbolsTests.CyclicInheritance.Class3, display: "Class3.dll");
                    }

                    return _Class3;
                }
            }
        }

        private static MetadataImageReference _CycledStructs;
        public static MetadataImageReference CycledStructs
        {
            get
            {
                if (_CycledStructs == null)
                {
                    _CycledStructs = new MetadataImageReference(TestResources.SymbolsTests.CyclicStructure.cycledstructs, display: "cycledstructs.dll");
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
                    private static MetadataImageReference _ClassA;
                    public static MetadataImageReference dll
                    {
                        get
                        {
                            if (_ClassA == null)
                            {
                                _ClassA = new MetadataImageReference(TestResources.SymbolsTests.RetV1.ClassA, display: "ClassA.dll");
                            }

                            return _ClassA;
                        }
                    }
                }

                public static class ClassB
                {
                    private static MetadataImageReference _ClassB;
                    public static MetadataImageReference netmodule
                    {
                        get
                        {
                            if (_ClassB == null)
                            {
                                _ClassB = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.RetV1.ClassB), display: "ClassB.netmodule");
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
                    private static MetadataImageReference _ClassA;
                    public static MetadataImageReference dll
                    {
                        get
                        {
                            if (_ClassA == null)
                            {
                                _ClassA = new MetadataImageReference(TestResources.SymbolsTests.RetV2.ClassA, display: "ClassA.dll");
                            }

                            return _ClassA;
                        }
                    }
                }

                public static class ClassB
                {
                    private static MetadataImageReference _ClassB;
                    public static MetadataImageReference dll
                    {
                        get
                        {
                            if (_ClassB == null)
                            {
                                _ClassB = new MetadataImageReference(TestResources.SymbolsTests.RetV2.ClassB, display: "ClassB.dll");
                            }

                            return _ClassB;
                        }
                    }
                }
            }
        }

        public static class Methods
        {
            private static MetadataImageReference _CSMethods;
            public static MetadataImageReference CSMethods
            {
                get
                {
                    if (_CSMethods == null)
                    {
                        _CSMethods = new MetadataImageReference(TestResources.SymbolsTests.Methods.CSMethods, display: "CSMethods.Dll");
                    }

                    return _CSMethods;
                }
            }

            private static MetadataImageReference _VBMethods;
            public static MetadataImageReference VBMethods
            {
                get
                {
                    if (_VBMethods == null)
                    {
                        _VBMethods = new MetadataImageReference(TestResources.SymbolsTests.Methods.VBMethods, display: "VBMethods.Dll");
                    }

                    return _VBMethods;
                }
            }

            private static MetadataImageReference _ILMethods;
            public static MetadataImageReference ILMethods
            {
                get
                {
                    if (_ILMethods == null)
                    {
                        _ILMethods = new MetadataImageReference(TestResources.SymbolsTests.Methods.ILMethods, display: "ILMethods.Dll");
                    }

                    return _ILMethods;
                }
            }

            private static MetadataImageReference _ByRefReturn;
            public static MetadataImageReference ByRefReturn
            {
                get
                {
                    if (_ByRefReturn == null)
                    {
                        _ByRefReturn = new MetadataImageReference(TestResources.SymbolsTests.Methods.ByRefReturn, display: "ByRefReturn.Dll");
                    }

                    return _ByRefReturn;
                }
            }
        }

        public static class Fields
        {
            public static class CSFields
            {
                private static MetadataImageReference _CSFields;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_CSFields == null)
                        {
                            _CSFields = new MetadataImageReference(TestResources.SymbolsTests.Fields.CSFields, display: "CSFields.Dll");
                        }

                        return _CSFields;
                    }
                }
            }

            public static class VBFields
            {
                private static MetadataImageReference _VBFields;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_VBFields == null)
                        {
                            _VBFields = new MetadataImageReference(TestResources.SymbolsTests.Fields.VBFields, display: "VBFields.Dll");
                        }

                        return _VBFields;
                    }
                }
            }

            private static MetadataImageReference _ConstantFields;
            public static MetadataImageReference ConstantFields
            {
                get
                {
                    if (_ConstantFields == null)
                    {
                        _ConstantFields = new MetadataImageReference(TestResources.SymbolsTests.Fields.ConstantFields, display: "ConstantFields.Dll");
                    }

                    return _ConstantFields;
                }
            }
        }

        public static class MissingTypes
        {
            private static MetadataImageReference _MDMissingType;
            public static MetadataImageReference MDMissingType
            {
                get
                {
                    if (_MDMissingType == null)
                    {
                        _MDMissingType = new MetadataImageReference(TestResources.SymbolsTests.MissingTypes.MDMissingType, display: "MDMissingType.Dll");
                    }

                    return _MDMissingType;
                }
            }

            private static MetadataImageReference _MDMissingTypeLib;
            public static MetadataImageReference MDMissingTypeLib
            {
                get
                {
                    if (_MDMissingTypeLib == null)
                    {
                        _MDMissingTypeLib = new MetadataImageReference(TestResources.SymbolsTests.MissingTypes.MDMissingTypeLib, display: "MDMissingTypeLib.Dll");
                    }

                    return _MDMissingTypeLib;
                }
            }

            private static MetadataImageReference _MissingTypesEquality1;
            public static MetadataImageReference MissingTypesEquality1
            {
                get
                {
                    if (_MissingTypesEquality1 == null)
                    {
                        _MissingTypesEquality1 = new MetadataImageReference(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality1, display: "MissingTypesEquality1.Dll");
                    }

                    return _MissingTypesEquality1;
                }
            }

            private static MetadataImageReference _MissingTypesEquality2;
            public static MetadataImageReference MissingTypesEquality2
            {
                get
                {
                    if (_MissingTypesEquality2 == null)
                    {
                        _MissingTypesEquality2 = new MetadataImageReference(TestResources.SymbolsTests.MissingTypes.MissingTypesEquality2, display: "MissingTypesEquality2.Dll");
                    }

                    return _MissingTypesEquality2;
                }
            }

            private static MetadataImageReference _CL2;
            public static MetadataImageReference CL2
            {
                get
                {
                    if (_CL2 == null)
                    {
                        _CL2 = new MetadataImageReference(TestResources.SymbolsTests.MissingTypes.CL2, display: "CL2.Dll");
                    }

                    return _CL2;
                }
            }

            private static MetadataImageReference _CL3;
            public static MetadataImageReference CL3
            {
                get
                {
                    if (_CL3 == null)
                    {
                        _CL3 = new MetadataImageReference(TestResources.SymbolsTests.MissingTypes.CL3, display: "CL3.Dll");
                    }

                    return _CL3;
                }
            }
        }

        public static class TypeForwarders
        {
            public static class TypeForwarder
            {
                private static MetadataImageReference _TypeForwarder2;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_TypeForwarder2 == null)
                        {
                            _TypeForwarder2 = new MetadataImageReference(TestResources.SymbolsTests.TypeForwarders.TypeForwarder, display: "TypeForwarder.Dll");
                        }

                        return _TypeForwarder2;
                    }
                }
            }

            public static class TypeForwarderLib
            {
                private static MetadataImageReference _TypeForwarderLib2;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_TypeForwarderLib2 == null)
                        {
                            _TypeForwarderLib2 = new MetadataImageReference(TestResources.SymbolsTests.TypeForwarders.TypeForwarderLib, display: "TypeForwarderLib.Dll");
                        }

                        return _TypeForwarderLib2;
                    }
                }
            }

            public static class TypeForwarderBase
            {
                private static MetadataImageReference _TypeForwarderBase2;
                public static MetadataImageReference dll
                {
                    get
                    {
                        if (_TypeForwarderBase2 == null)
                        {
                            _TypeForwarderBase2 = new MetadataImageReference(TestResources.SymbolsTests.TypeForwarders.TypeForwarderBase, display: "TypeForwarderBase.Dll");
                        }

                        return _TypeForwarderBase2;
                    }
                }
            }
        }

        public static class MultiTargeting
        {
            private static MetadataImageReference _Source1Module;
            public static MetadataImageReference Source1Module
            {
                get
                {
                    if (_Source1Module == null)
                    {
                        _Source1Module = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source1Module), display: "Source1Module.netmodule");
                    }

                    return _Source1Module;
                }
            }

            private static MetadataImageReference _Source3Module;
            public static MetadataImageReference Source3Module
            {
                get
                {
                    if (_Source3Module == null)
                    {
                        _Source3Module = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source3Module), display: "Source3Module.netmodule");
                    }

                    return _Source3Module;
                }
            }

            private static MetadataImageReference _Source4Module;
            public static MetadataImageReference Source4Module
            {
                get
                {
                    if (_Source4Module == null)
                    {
                        _Source4Module = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source4Module), display: "Source4Module.netmodule");
                    }

                    return _Source4Module;
                }
            }

            private static MetadataImageReference _Source5Module;
            public static MetadataImageReference Source5Module
            {
                get
                {
                    if (_Source5Module == null)
                    {
                        _Source5Module = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source5Module), display: "Source5Module.netmodule");
                    }

                    return _Source5Module;
                }
            }

            private static MetadataImageReference _Source7Module;
            public static MetadataImageReference Source7Module
            {
                get
                {
                    if (_Source7Module == null)
                    {
                        _Source7Module = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.MultiTargeting.Source7Module), display: "Source7Module.netmodule");
                    }

                    return _Source7Module;
                }
            }
        }

        public static class NoPia
        {
            private static MetadataImageReference _StdOle;
            public static MetadataImageReference StdOle
            {
                get
                {
                    if (_StdOle == null)
                    {
                        _StdOle = new MetadataImageReference(ProprietaryTestResources.SymbolsTests.NoPia.stdole, display: "stdole.dll");
                    }

                    return _StdOle;
                }
            }

            private static MetadataImageReference _Pia1;
            public static MetadataImageReference Pia1
            {
                get
                {
                    if (_Pia1 == null)
                    {
                        _Pia1 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Pia1, display: "Pia1.dll");
                    }

                    return _Pia1;
                }
            }

            private static MetadataImageReference _Pia1Copy;
            public static MetadataImageReference Pia1Copy
            {
                get
                {
                    if (_Pia1Copy == null)
                    {
                        _Pia1Copy = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Pia1Copy, display: "Pia1Copy.dll");
                    }

                    return _Pia1Copy;
                }
            }

            private static MetadataImageReference _Pia2;
            public static MetadataImageReference Pia2
            {
                get
                {
                    if (_Pia2 == null)
                    {
                        _Pia2 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Pia2, display: "Pia2.dll");
                    }

                    return _Pia2;
                }
            }

            private static MetadataImageReference _Pia3;
            public static MetadataImageReference Pia3
            {
                get
                {
                    if (_Pia3 == null)
                    {
                        _Pia3 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Pia3, display: "Pia3.dll");
                    }

                    return _Pia3;
                }
            }

            private static MetadataImageReference _Pia4;
            public static MetadataImageReference Pia4
            {
                get
                {
                    if (_Pia4 == null)
                    {
                        _Pia4 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Pia4, display: "Pia4.dll");
                    }

                    return _Pia4;
                }
            }

            private static MetadataImageReference _Pia5;
            public static MetadataImageReference Pia5
            {
                get
                {
                    if (_Pia5 == null)
                    {
                        _Pia5 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Pia5, display: "Pia5.dll");
                    }

                    return _Pia5;
                }
            }

            private static MetadataImageReference _GeneralPia;
            public static MetadataImageReference GeneralPia
            {
                get
                {
                    if (_GeneralPia == null)
                    {
                        _GeneralPia = new MetadataImageReference(TestResources.SymbolsTests.NoPia.GeneralPia, display: "GeneralPia.dll");
                    }

                    return _GeneralPia;
                }
            }

            private static MetadataImageReference _GeneralPiaCopy;
            public static MetadataImageReference GeneralPiaCopy
            {
                get
                {
                    if (_GeneralPiaCopy == null)
                    {
                        _GeneralPiaCopy = new MetadataImageReference(TestResources.SymbolsTests.NoPia.GeneralPiaCopy, display: "GeneralPiaCopy.dll");
                    }

                    return _GeneralPiaCopy;
                }
            }

            private static MetadataImageReference _NoPIAGenericsAsm1;
            public static MetadataImageReference NoPIAGenericsAsm1
            {
                get
                {
                    if (_NoPIAGenericsAsm1 == null)
                    {
                        _NoPIAGenericsAsm1 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.NoPIAGenerics1_Asm1, display: "NoPIAGenerics1-Asm1.dll");
                    }

                    return _NoPIAGenericsAsm1;
                }
            }

            private static MetadataImageReference _ExternalAsm1;
            public static MetadataImageReference ExternalAsm1
            {
                get
                {
                    if (_ExternalAsm1 == null)
                    {
                        _ExternalAsm1 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.ExternalAsm1, display: "ExternalAsm1.dll");
                    }

                    return _ExternalAsm1;
                }
            }

            private static MetadataImageReference _Library1;
            public static MetadataImageReference Library1
            {
                get
                {
                    if (_Library1 == null)
                    {
                        _Library1 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Library1, display: "Library1.dll");
                    }

                    return _Library1;
                }
            }

            private static MetadataImageReference _Library2;
            public static MetadataImageReference Library2
            {
                get
                {
                    if (_Library2 == null)
                    {
                        _Library2 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.Library2, display: "Library2.dll");
                    }

                    return _Library2;
                }
            }

            private static MetadataImageReference _LocalTypes1;
            public static MetadataImageReference LocalTypes1
            {
                get
                {
                    if (_LocalTypes1 == null)
                    {
                        _LocalTypes1 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.LocalTypes1, display: "LocalTypes1.dll");
                    }

                    return _LocalTypes1;
                }
            }

            private static MetadataImageReference _LocalTypes2;
            public static MetadataImageReference LocalTypes2
            {
                get
                {
                    if (_LocalTypes2 == null)
                    {
                        _LocalTypes2 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.LocalTypes2, display: "LocalTypes2.dll");
                    }

                    return _LocalTypes2;
                }
            }

            private static MetadataImageReference _LocalTypes3;
            public static MetadataImageReference LocalTypes3
            {
                get
                {
                    if (_LocalTypes3 == null)
                    {
                        _LocalTypes3 = new MetadataImageReference(TestResources.SymbolsTests.NoPia.LocalTypes3, display: "LocalTypes3.dll");
                    }

                    return _LocalTypes3;
                }
            }

            private static MetadataImageReference _A;
            public static MetadataImageReference A
            {
                get
                {
                    if (_A == null)
                    {
                        _A = new MetadataImageReference(TestResources.SymbolsTests.NoPia.A, display: "A.dll");
                    }

                    return _A;
                }
            }

            private static MetadataImageReference _B;
            public static MetadataImageReference B
            {
                get
                {
                    if (_B == null)
                    {
                        _B = new MetadataImageReference(TestResources.SymbolsTests.NoPia.B, display: "B.dll");
                    }

                    return _B;
                }
            }

            private static MetadataImageReference _C;
            public static MetadataImageReference C
            {
                get
                {
                    if (_C == null)
                    {
                        _C = new MetadataImageReference(TestResources.SymbolsTests.NoPia.C, display: "C.dll");
                    }

                    return _C;
                }
            }

            private static MetadataImageReference _D;
            public static MetadataImageReference D
            {
                get
                {
                    if (_D == null)
                    {
                        _D = new MetadataImageReference(TestResources.SymbolsTests.NoPia.D, display: "D.dll");
                    }

                    return _D;
                }
            }

            public static class Microsoft
            {
                public static class VisualStudio
                {
                    private static MetadataImageReference _MissingPIAAttributes;
                    public static MetadataImageReference MissingPIAAttributes
                    {
                        get
                        {
                            if (_MissingPIAAttributes == null)
                            {
                                _MissingPIAAttributes = new MetadataImageReference(TestResources.SymbolsTests.NoPia.MissingPIAAttributes, display: "MicrosoftPIAAttributes.dll");
                            }

                            return _MissingPIAAttributes;
                        }
                    }
                }
            }
        }

        public static class Interface
        {
            private static MetadataImageReference _StaticMethodInInterface;
            public static MetadataImageReference StaticMethodInInterface
            {
                get
                {
                    if (_StaticMethodInInterface == null)
                    {
                        _StaticMethodInInterface = new MetadataImageReference(TestResources.SymbolsTests._Interface.StaticMethodInInterface, display: "StaticMethodInInterface.dll");
                    }

                    return _StaticMethodInInterface;
                }
            }

            private static MetadataImageReference _MDInterfaceMapping;
            public static MetadataImageReference MDInterfaceMapping
            {
                get
                {
                    if (_MDInterfaceMapping == null)
                    {
                        _MDInterfaceMapping = new MetadataImageReference(TestResources.SymbolsTests._Interface.MDInterfaceMapping, display: "MDInterfaceMapping.dll");
                    }

                    return _MDInterfaceMapping;
                }
            }
        }

        public static class MetadataCache
        {
            private static MetadataImageReference _MDTestLib1;
            public static MetadataImageReference MDTestLib1
            {
                get
                {
                    if (_MDTestLib1 == null)
                    {
                        _MDTestLib1 = new MetadataImageReference(TestResources.SymbolsTests.General.MDTestLib1, display: "MDTestLib1.dll");
                    }

                    return _MDTestLib1;
                }
            }

            private static MetadataImageReference _netModule1;
            public static MetadataImageReference netModule1
            {
                get
                {
                    if (_netModule1 == null)
                    {
                        _netModule1 = new MetadataImageReference(ModuleMetadata.CreateFromImage(TestResources.SymbolsTests.netModule.netModule1), display: "netModule1.netmodule");
                    }

                    return _netModule1;
                }
            }
        }

        public static class ExplicitInterfaceImplementation
        {
            public static class Methods
            {
                private static MetadataImageReference _CSharp;
                public static MetadataImageReference CSharp
                {
                    get
                    {
                        if (_CSharp == null)
                        {
                            _CSharp = new MetadataImageReference(TestResources.SymbolsTests.General.CSharpExplicitInterfaceImplementation, display: "CSharpExplicitInterfaceImplementation.dll");
                        }

                        return _CSharp;
                    }
                }

                private static MetadataImageReference _IL;
                public static MetadataImageReference IL
                {
                    get
                    {
                        if (_IL == null)
                        {
                            _IL = new MetadataImageReference(TestResources.SymbolsTests.General.ILExplicitInterfaceImplementation, display: "ILExplicitInterfaceImplementation.dll");
                        }

                        return _IL;
                    }
                }
            }

            public static class Properties
            {
                private static MetadataImageReference _CSharp;
                public static MetadataImageReference CSharp
                {
                    get
                    {
                        if (_CSharp == null)
                        {
                            _CSharp = new MetadataImageReference(TestResources.SymbolsTests.General.CSharpExplicitInterfaceImplementationProperties, display: "CSharpExplicitInterfaceImplementationProperties.dll");
                        }

                        return _CSharp;
                    }
                }

                private static MetadataImageReference _IL;
                public static MetadataImageReference IL
                {
                    get
                    {
                        if (_IL == null)
                        {
                            _IL = new MetadataImageReference(TestResources.SymbolsTests.General.ILExplicitInterfaceImplementationProperties, display: "ILExplicitInterfaceImplementationProperties.dll");
                        }

                        return _IL;
                    }
                }
            }

            public static class Events
            {
                private static MetadataImageReference _CSharp;
                public static MetadataImageReference CSharp
                {
                    get
                    {
                        if (_CSharp == null)
                        {
                            _CSharp = new MetadataImageReference(TestResources.SymbolsTests.General.CSharpExplicitInterfaceImplementationEvents, display: "CSharpExplicitInterfaceImplementationEvents.dll");
                        }

                        return _CSharp;
                    }
                }
            }
        }

        private static MetadataImageReference _Regress40025;
        public static MetadataImageReference Regress40025
        {
            get
            {
                if (_Regress40025 == null)
                {
                    _Regress40025 = new MetadataImageReference(TestResources.SymbolsTests.General.Regress40025DLL, display: "Regress40025DLL.dll");
                }

                return _Regress40025;
            }
        }

        public static class WithEvents
        {
            private static MetadataImageReference _SimpleWithEvents;
            public static MetadataImageReference SimpleWithEvents
            {
                get
                {
                    if (_SimpleWithEvents == null)
                    {
                        _SimpleWithEvents = new MetadataImageReference(TestResources.SymbolsTests._WithEvents.SimpleWithEvents, display: "SimpleWithEvents.dll");
                    }

                    return _SimpleWithEvents;
                }
            }
        }

        public static class DelegateImplementation
        {
            private static MetadataImageReference _DelegatesWithoutInvoke;
            public static MetadataImageReference DelegatesWithoutInvoke
            {
                get
                {
                    if (_DelegatesWithoutInvoke == null)
                    {
                        _DelegatesWithoutInvoke = new MetadataImageReference(TestResources.SymbolsTests.General.DelegatesWithoutInvoke, display: "DelegatesWithoutInvoke.dll");
                    }

                    return _DelegatesWithoutInvoke;
                }
            }

            private static MetadataImageReference _DelegateByRefParamArray;
            public static MetadataImageReference DelegateByRefParamArray
            {
                get
                {
                    if (_DelegateByRefParamArray == null)
                    {
                        _DelegateByRefParamArray = new MetadataImageReference(TestResources.SymbolsTests.General.DelegateByRefParamArray, display: "DelegateByRefParamArray.dll");
                    }

                    return _DelegateByRefParamArray;
                }
            }
        }

        public static class Metadata
        {
            private static MetadataImageReference _InvalidCharactersInAssemblyName2;
            public static MetadataImageReference InvalidCharactersInAssemblyName
            {
                get
                {
                    if (_InvalidCharactersInAssemblyName2 == null)
                    {
                        _InvalidCharactersInAssemblyName2 = new MetadataImageReference(TestResources.SymbolsTests.Metadata.InvalidCharactersInAssemblyName, display: "InvalidCharactersInAssemblyName.dll");
                    }

                    return _InvalidCharactersInAssemblyName2;
                }
            }

            private static MetadataImageReference _MDTestAttributeDefLib;
            public static MetadataImageReference MDTestAttributeDefLib
            {
                get
                {
                    if (_MDTestAttributeDefLib == null)
                    {
                        _MDTestAttributeDefLib = new MetadataImageReference(TestResources.SymbolsTests.Metadata.MDTestAttributeDefLib, display: "MDTestAttributeDefLib.dll");
                    }

                    return _MDTestAttributeDefLib;
                }
            }

            private static MetadataImageReference _MDTestAttributeApplicationLib;
            public static MetadataImageReference MDTestAttributeApplicationLib
            {
                get
                {
                    if (_MDTestAttributeApplicationLib == null)
                    {
                        _MDTestAttributeApplicationLib = new MetadataImageReference(TestResources.SymbolsTests.Metadata.MDTestAttributeApplicationLib, display: "MDTestAttributeApplicationLib.dll");
                    }

                    return _MDTestAttributeApplicationLib;
                }
            }

            private static MetadataImageReference _AttributeInterop01;
            public static MetadataImageReference AttributeInterop01
            {
                get
                {
                    if (_AttributeInterop01 == null)
                    {
                        _AttributeInterop01 = new MetadataImageReference(TestResources.SymbolsTests.Metadata.AttributeInterop01, display: "AttributeInterop01.dll");
                    }

                    return _AttributeInterop01;
                }
            }

            private static MetadataImageReference _AttributeInterop02;
            public static MetadataImageReference AttributeInterop02
            {
                get
                {
                    if (_AttributeInterop02 == null)
                    {
                        _AttributeInterop02 = new MetadataImageReference(TestResources.SymbolsTests.Metadata.AttributeInterop02, display: "AttributeInterop02.dll");
                    }

                    return _AttributeInterop02;
                }
            }

            private static MetadataImageReference _AttributeTestLib01;
            public static MetadataImageReference AttributeTestLib01
            {
                get
                {
                    if (_AttributeTestLib01 == null)
                    {
                        _AttributeTestLib01 = new MetadataImageReference(TestResources.SymbolsTests.Metadata.AttributeTestLib01, display: "AttributeTestLib01.dll");
                    }

                    return _AttributeTestLib01;
                }
            }

            private static MetadataImageReference _AttributeTestDef01;
            public static MetadataImageReference AttributeTestDef01
            {
                get
                {
                    if (_AttributeTestDef01 == null)
                    {
                        _AttributeTestDef01 = new MetadataImageReference(TestResources.SymbolsTests.Metadata.AttributeTestDef01, display: "AttributeTestDef01.dll");
                    }

                    return _AttributeTestDef01;
                }
            }

            private static MetadataImageReference _DynamicAttributeLib;
            public static MetadataImageReference DynamicAttributeLib
            {
                get
                {
                    if (_DynamicAttributeLib == null)
                    {
                        _DynamicAttributeLib = new MetadataImageReference(TestResources.SymbolsTests.Metadata.DynamicAttribute, display: "DynamicAttribute.dll");
                    }

                    return _DynamicAttributeLib;
                }
            }
        }

        public static class UseSiteErrors
        {
            private static MetadataImageReference _Unavailable;
            public static MetadataImageReference Unavailable
            {
                get
                {
                    if (_Unavailable == null)
                    {
                        _Unavailable = new MetadataImageReference(TestResources.SymbolsTests.General.Unavailable, display: "Unavailable.dll");
                    }

                    return _Unavailable;
                }
            }

            private static MetadataImageReference _CSharp;
            public static MetadataImageReference CSharp
            {
                get
                {
                    if (_CSharp == null)
                    {
                        _CSharp = new MetadataImageReference(TestResources.SymbolsTests.General.CSharpErrors, display: "CSharpErrors.dll");
                    }

                    return _CSharp;
                }
            }

            private static MetadataImageReference _IL;
            public static MetadataImageReference IL
            {
                get
                {
                    if (_IL == null)
                    {
                        _IL = new MetadataImageReference(TestResources.SymbolsTests.General.ILErrors, display: "ILErrors.dll");
                    }

                    return _IL;
                }
            }
        }

        public static class Versioning
        {
            private static MetadataImageReference _AR_SA;
            public static MetadataImageReference AR_SA
            {
                get
                {
                    if (_AR_SA == null)
                    {
                        _AR_SA = new MetadataImageReference(TestResources.SymbolsTests.General.Culture_AR_SA, display: "AR-SA");
                    }

                    return _AR_SA;
                }
            }

            private static MetadataImageReference _EN_US;
            public static MetadataImageReference EN_US
            {
                get
                {
                    if (_EN_US == null)
                    {
                        _EN_US = new MetadataImageReference(TestResources.SymbolsTests.General.Culture_EN_US, display: "EN-US");
                    }

                    return _EN_US;
                }
            }
        }
    }
}
