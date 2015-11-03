// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace TestResources
{
    public static class AnalyzerTests
    {
        private static byte[] _FaultyAnalyzer;
        public static byte[] FaultyAnalyzer => ResourceLoader.GetOrCreateResource(ref _FaultyAnalyzer, "Analyzers.FaultyAnalyzer.dll");
    }

    public static class AssemblyLoadTests
    {
        private static byte[] _Alpha;
        public static byte[] Alpha => ResourceLoader.GetOrCreateResource(ref _Alpha, "AssemblyLoadTests.Alpha.dll");

        private static byte[] _Beta;
        public static byte[] Beta => ResourceLoader.GetOrCreateResource(ref _Beta, "AssemblyLoadTests.Beta.dll");

        private static byte[] _Delta;
        public static byte[] Delta => ResourceLoader.GetOrCreateResource(ref _Delta, "AssemblyLoadTests.Delta.dll");

        private static byte[] _Gamma;
        public static byte[] Gamma => ResourceLoader.GetOrCreateResource(ref _Gamma, "AssemblyLoadTests.Gamma.dll");
    }

    public static class DiagnosticTests
    {
        private static byte[] _badresfile;
        public static byte[] badresfile => ResourceLoader.GetOrCreateResource(ref _badresfile, "DiagnosticTests.badresfile.res");

        private static byte[] _ErrTestLib01;
        public static byte[] ErrTestLib01 => ResourceLoader.GetOrCreateResource(ref _ErrTestLib01, "DiagnosticTests.ErrTestLib01.dll");

        private static byte[] _ErrTestLib02;
        public static byte[] ErrTestLib02 => ResourceLoader.GetOrCreateResource(ref _ErrTestLib02, "DiagnosticTests.ErrTestLib02.dll");

        private static byte[] _ErrTestLib11;
        public static byte[] ErrTestLib11 => ResourceLoader.GetOrCreateResource(ref _ErrTestLib11, "DiagnosticTests.ErrTestLib11.dll");

        private static byte[] _ErrTestMod01;
        public static byte[] ErrTestMod01 => ResourceLoader.GetOrCreateResource(ref _ErrTestMod01, "DiagnosticTests.ErrTestMod01.netmodule");

        private static byte[] _ErrTestMod02;
        public static byte[] ErrTestMod02 => ResourceLoader.GetOrCreateResource(ref _ErrTestMod02, "DiagnosticTests.ErrTestMod02.netmodule");
    }

    public static class Basic
    {
        private static byte[] _Members;
        public static byte[] Members => ResourceLoader.GetOrCreateResource(ref _Members, "MetadataTests.Members.dll");

        private static byte[] _NativeApp;
        public static byte[] NativeApp => ResourceLoader.GetOrCreateResource(ref _NativeApp, "MetadataTests.NativeApp.exe");
    }
}

namespace TestResources.MetadataTests
{
    public static class InterfaceAndClass
    {
        private static byte[] _CSClasses01;
        public static byte[] CSClasses01 => ResourceLoader.GetOrCreateResource(ref _CSClasses01, "MetadataTests.InterfaceAndClass.CSClasses01.dll");

        private static byte[] _CSInterfaces01;
        public static byte[] CSInterfaces01 => ResourceLoader.GetOrCreateResource(ref _CSInterfaces01, "MetadataTests.InterfaceAndClass.CSInterfaces01.dll");

        private static byte[] _VBClasses01;
        public static byte[] VBClasses01 => ResourceLoader.GetOrCreateResource(ref _VBClasses01, "MetadataTests.InterfaceAndClass.VBClasses01.dll");

        private static byte[] _VBClasses02;
        public static byte[] VBClasses02 => ResourceLoader.GetOrCreateResource(ref _VBClasses02, "MetadataTests.InterfaceAndClass.VBClasses02.dll");

        private static byte[] _VBInterfaces01;
        public static byte[] VBInterfaces01 => ResourceLoader.GetOrCreateResource(ref _VBInterfaces01, "MetadataTests.InterfaceAndClass.VBInterfaces01.dll");
    }

    public static class Interop
    {
        private static byte[] _IndexerWithByRefParam;
        public static byte[] IndexerWithByRefParam => ResourceLoader.GetOrCreateResource(ref _IndexerWithByRefParam, "MetadataTests.Interop.IndexerWithByRefParam.dll");

        private static byte[] _Interop_Mock01;
        public static byte[] Interop_Mock01 => ResourceLoader.GetOrCreateResource(ref _Interop_Mock01, "MetadataTests.Interop.Interop.Mock01.dll");

        private static byte[] _Interop_Mock01_Impl;
        public static byte[] Interop_Mock01_Impl => ResourceLoader.GetOrCreateResource(ref _Interop_Mock01_Impl, "MetadataTests.Interop.Interop.Mock01.Impl.dll");
    }

    public static class Invalid
    {
        private static byte[] _ClassLayout;
        public static byte[] ClassLayout => ResourceLoader.GetOrCreateResource(ref _ClassLayout, "MetadataTests.Invalid.ClassLayout.dll");

        private static byte[] _CustomAttributeTableUnsorted;
        public static byte[] CustomAttributeTableUnsorted => ResourceLoader.GetOrCreateResource(ref _CustomAttributeTableUnsorted, "MetadataTests.Invalid.CustomAttributeTableUnsorted.dll");

        private static byte[] _EmptyModuleTable;
        public static byte[] EmptyModuleTable => ResourceLoader.GetOrCreateResource(ref _EmptyModuleTable, "MetadataTests.Invalid.EmptyModuleTable.netmodule");

        private static byte[] _IncorrectCustomAssemblyTableSize_TooManyMethodSpecs;
        public static byte[] IncorrectCustomAssemblyTableSize_TooManyMethodSpecs => ResourceLoader.GetOrCreateResource(ref _IncorrectCustomAssemblyTableSize_TooManyMethodSpecs, "MetadataTests.Invalid.IncorrectCustomAssemblyTableSize_TooManyMethodSpecs.dll");

        private static byte[] _InvalidDynamicAttributeArgs;
        public static byte[] InvalidDynamicAttributeArgs => ResourceLoader.GetOrCreateResource(ref _InvalidDynamicAttributeArgs, "MetadataTests.Invalid.InvalidDynamicAttributeArgs.dll");

        private static byte[] _InvalidFuncDelegateName;
        public static byte[] InvalidFuncDelegateName => ResourceLoader.GetOrCreateResource(ref _InvalidFuncDelegateName, "MetadataTests.Invalid.InvalidFuncDelegateName.dll");

        private static byte[] _InvalidGenericType;
        public static byte[] InvalidGenericType => ResourceLoader.GetOrCreateResource(ref _InvalidGenericType, "MetadataTests.Invalid.InvalidGenericType.dll");

        private static byte[] _InvalidModuleName;
        public static byte[] InvalidModuleName => ResourceLoader.GetOrCreateResource(ref _InvalidModuleName, "MetadataTests.Invalid.InvalidModuleName.dll");

        private static byte[] _LongTypeFormInSignature;
        public static byte[] LongTypeFormInSignature => ResourceLoader.GetOrCreateResource(ref _LongTypeFormInSignature, "MetadataTests.Invalid.LongTypeFormInSignature.dll");

        private static string _ManyMethodSpecs;
        public static string ManyMethodSpecs => ResourceLoader.GetOrCreateResource(ref _ManyMethodSpecs, "MetadataTests.Invalid.ManyMethodSpecs.vb");

        private static byte[] _Obfuscated;
        public static byte[] Obfuscated => ResourceLoader.GetOrCreateResource(ref _Obfuscated, "MetadataTests.Invalid.Obfuscated.dll");

        private static byte[] _Obfuscated2;
        public static byte[] Obfuscated2 => ResourceLoader.GetOrCreateResource(ref _Obfuscated2, "MetadataTests.Invalid.Obfuscated2.dll");
    }

    public static class NetModule01
    {
        private static byte[] _AppCS;
        public static byte[] AppCS => ResourceLoader.GetOrCreateResource(ref _AppCS, "MetadataTests.NetModule01.AppCS.exe");

        private static byte[] _ModuleCS00;
        public static byte[] ModuleCS00 => ResourceLoader.GetOrCreateResource(ref _ModuleCS00, "MetadataTests.NetModule01.ModuleCS00.mod");

        private static byte[] _ModuleCS01;
        public static byte[] ModuleCS01 => ResourceLoader.GetOrCreateResource(ref _ModuleCS01, "MetadataTests.NetModule01.ModuleCS01.mod");

        private static byte[] _ModuleVB01;
        public static byte[] ModuleVB01 => ResourceLoader.GetOrCreateResource(ref _ModuleVB01, "MetadataTests.NetModule01.ModuleVB01.mod");
    }
}

namespace TestResources.NetFX
{
    public static class aacorlib_v15_0_3928
    {
        private static string _aacorlib_v15_0_3928_cs;
        public static string aacorlib_v15_0_3928_cs => ResourceLoader.GetOrCreateResource(ref _aacorlib_v15_0_3928_cs, "NetFX.aacorlib.aacorlib.v15.0.3928.cs");
    }

    public static class Minimal
    {
        private static byte[] _mincorlib;
        public static byte[] mincorlib => ResourceLoader.GetOrCreateResource(ref _mincorlib, "NetFX.Minimal.mincorlib.dll");
    }
}

namespace TestResources
{
    public static class PerfTests
    {
        private static string _CSPerfTest;
        public static string CSPerfTest => ResourceLoader.GetOrCreateResource(ref _CSPerfTest, "PerfTests.CSPerfTest.cs");

        private static string _VBPerfTest;
        public static string VBPerfTest => ResourceLoader.GetOrCreateResource(ref _VBPerfTest, "PerfTests.VBPerfTest.vb");
    }

    public static class General
    {
        private static byte[] _BigVisitor;
        public static byte[] BigVisitor => ResourceLoader.GetOrCreateResource(ref _BigVisitor, "SymbolsTests.BigVisitor.dll");

        private static byte[] _DelegateByRefParamArray;
        public static byte[] DelegateByRefParamArray => ResourceLoader.GetOrCreateResource(ref _DelegateByRefParamArray, "SymbolsTests.Delegates.DelegateByRefParamArray.dll");

        private static byte[] _DelegatesWithoutInvoke;
        public static byte[] DelegatesWithoutInvoke => ResourceLoader.GetOrCreateResource(ref _DelegatesWithoutInvoke, "SymbolsTests.Delegates.DelegatesWithoutInvoke.dll");

        private static byte[] _shiftJisSource;
        public static byte[] ShiftJisSource => ResourceLoader.GetOrCreateResource(ref _shiftJisSource, "Encoding.sjis.cs");

        private static byte[] _Events;
        public static byte[] Events => ResourceLoader.GetOrCreateResource(ref _Events, "SymbolsTests.Events.dll");

        private static byte[] _CSharpExplicitInterfaceImplementation;
        public static byte[] CSharpExplicitInterfaceImplementation => ResourceLoader.GetOrCreateResource(ref _CSharpExplicitInterfaceImplementation, "SymbolsTests.ExplicitInterfaceImplementation.CSharpExplicitInterfaceImplementation.dll");

        private static byte[] _CSharpExplicitInterfaceImplementationEvents;
        public static byte[] CSharpExplicitInterfaceImplementationEvents => ResourceLoader.GetOrCreateResource(ref _CSharpExplicitInterfaceImplementationEvents, "SymbolsTests.ExplicitInterfaceImplementation.CSharpExplicitInterfaceImplementationEvents.dll");

        private static byte[] _CSharpExplicitInterfaceImplementationProperties;
        public static byte[] CSharpExplicitInterfaceImplementationProperties => ResourceLoader.GetOrCreateResource(ref _CSharpExplicitInterfaceImplementationProperties, "SymbolsTests.ExplicitInterfaceImplementation.CSharpExplicitInterfaceImplementationProperties.dll");

        private static byte[] _ILExplicitInterfaceImplementation;
        public static byte[] ILExplicitInterfaceImplementation => ResourceLoader.GetOrCreateResource(ref _ILExplicitInterfaceImplementation, "SymbolsTests.ExplicitInterfaceImplementation.ILExplicitInterfaceImplementation.dll");

        private static byte[] _ILExplicitInterfaceImplementationProperties;
        public static byte[] ILExplicitInterfaceImplementationProperties => ResourceLoader.GetOrCreateResource(ref _ILExplicitInterfaceImplementationProperties, "SymbolsTests.ExplicitInterfaceImplementation.ILExplicitInterfaceImplementationProperties.dll");

        private static byte[] _FSharpTestLibrary;
        public static byte[] FSharpTestLibrary => ResourceLoader.GetOrCreateResource(ref _FSharpTestLibrary, "SymbolsTests.FSharpTestLibrary.dll");

        private static byte[] _Indexers;
        public static byte[] Indexers => ResourceLoader.GetOrCreateResource(ref _Indexers, "SymbolsTests.Indexers.dll");

        private static byte[] _InheritIComparable;
        public static byte[] InheritIComparable => ResourceLoader.GetOrCreateResource(ref _InheritIComparable, "SymbolsTests.InheritIComparable.dll");

        private static byte[] _MDTestLib1;
        public static byte[] MDTestLib1 => ResourceLoader.GetOrCreateResource(ref _MDTestLib1, "SymbolsTests.MDTestLib1.dll");

        private static byte[] _MDTestLib2;
        public static byte[] MDTestLib2 => ResourceLoader.GetOrCreateResource(ref _MDTestLib2, "SymbolsTests.MDTestLib2.dll");

        private static byte[] _nativeCOFFResources;
        public static byte[] nativeCOFFResources => ResourceLoader.GetOrCreateResource(ref _nativeCOFFResources, "SymbolsTests.nativeCOFFResources.obj");

        private static byte[] _Properties;
        public static byte[] Properties => ResourceLoader.GetOrCreateResource(ref _Properties, "SymbolsTests.Properties.dll");

        private static byte[] _PropertiesWithByRef;
        public static byte[] PropertiesWithByRef => ResourceLoader.GetOrCreateResource(ref _PropertiesWithByRef, "SymbolsTests.PropertiesWithByRef.dll");

        private static byte[] _Regress40025DLL;
        public static byte[] Regress40025DLL => ResourceLoader.GetOrCreateResource(ref _Regress40025DLL, "SymbolsTests.Regress40025DLL.dll");

        private static byte[] _snKey;
        public static byte[] snKey => ResourceLoader.GetOrCreateResource(ref _snKey, "SymbolsTests.snKey.snk");

        private static byte[] _snKey2;
        public static byte[] snKey2 => ResourceLoader.GetOrCreateResource(ref _snKey2, "SymbolsTests.snKey2.snk");

        private static byte[] _snPublicKey;
        public static byte[] snPublicKey => ResourceLoader.GetOrCreateResource(ref _snPublicKey, "SymbolsTests.snPublicKey.snk");

        private static byte[] _snPublicKey2;
        public static byte[] snPublicKey2 => ResourceLoader.GetOrCreateResource(ref _snPublicKey2, "SymbolsTests.snPublicKey2.snk");

        private static byte[] _CSharpErrors;
        public static byte[] CSharpErrors => ResourceLoader.GetOrCreateResource(ref _CSharpErrors, "SymbolsTests.UseSiteErrors.CSharpErrors.dll");

        private static byte[] _ILErrors;
        public static byte[] ILErrors => ResourceLoader.GetOrCreateResource(ref _ILErrors, "SymbolsTests.UseSiteErrors.ILErrors.dll");

        private static byte[] _Unavailable;
        public static byte[] Unavailable => ResourceLoader.GetOrCreateResource(ref _Unavailable, "SymbolsTests.UseSiteErrors.Unavailable.dll");

        private static byte[] _VBConversions;
        public static byte[] VBConversions => ResourceLoader.GetOrCreateResource(ref _VBConversions, "SymbolsTests.VBConversions.dll");

        private static byte[] _Culture_AR_SA;
        public static byte[] Culture_AR_SA => ResourceLoader.GetOrCreateResource(ref _Culture_AR_SA, "SymbolsTests.Versioning.AR_SA.Culture.dll");

        private static byte[] _Culture_EN_US;
        public static byte[] Culture_EN_US => ResourceLoader.GetOrCreateResource(ref _Culture_EN_US, "SymbolsTests.Versioning.EN_US.Culture.dll");

        private static byte[] _C1;
        public static byte[] C1 => ResourceLoader.GetOrCreateResource(ref _C1, "SymbolsTests.Versioning.V1.C.dll");

        private static byte[] _C2;
        public static byte[] C2 => ResourceLoader.GetOrCreateResource(ref _C2, "SymbolsTests.Versioning.V2.C.dll");

        private static byte[] _With_Spaces;
        public static byte[] With_Spaces => ResourceLoader.GetOrCreateResource(ref _With_Spaces, "SymbolsTests.With Spaces.dll");

        private static byte[] _With_SpacesModule;
        public static byte[] With_SpacesModule => ResourceLoader.GetOrCreateResource(ref _With_SpacesModule, "SymbolsTests.With Spaces.netmodule");
    }
}

namespace TestResources.SymbolsTests
{
    public static class CorLibrary
    {
        private static byte[] _FakeMsCorLib;
        public static byte[] FakeMsCorLib => ResourceLoader.GetOrCreateResource(ref _FakeMsCorLib, "SymbolsTests.CorLibrary.FakeMsCorLib.dll");

        private static byte[] _GuidTest2;
        public static byte[] GuidTest2 => ResourceLoader.GetOrCreateResource(ref _GuidTest2, "SymbolsTests.CorLibrary.GuidTest2.exe");

        private static byte[] _NoMsCorLibRef;
        public static byte[] NoMsCorLibRef => ResourceLoader.GetOrCreateResource(ref _NoMsCorLibRef, "SymbolsTests.CorLibrary.NoMsCorLibRef.dll");
    }

    public static class CustomModifiers
    {
        private static byte[] _CppCli;
        public static byte[] CppCli => ResourceLoader.GetOrCreateResource(ref _CppCli, "SymbolsTests.CustomModifiers.CppCli.dll");

        private static byte[] _Modifiers;
        public static byte[] Modifiers => ResourceLoader.GetOrCreateResource(ref _Modifiers, "SymbolsTests.CustomModifiers.Modifiers.dll");

        private static byte[] _ModifiersModule;
        public static byte[] ModifiersModule => ResourceLoader.GetOrCreateResource(ref _ModifiersModule, "SymbolsTests.CustomModifiers.Modifiers.netmodule");

        private static byte[] _ModoptTests;
        public static byte[] ModoptTests => ResourceLoader.GetOrCreateResource(ref _ModoptTests, "SymbolsTests.CustomModifiers.ModoptTests.dll");
    }

    public static class Cyclic
    {
        private static byte[] _Cyclic1;
        public static byte[] Cyclic1 => ResourceLoader.GetOrCreateResource(ref _Cyclic1, "SymbolsTests.Cyclic.Cyclic1.dll");

        private static byte[] _Cyclic2;
        public static byte[] Cyclic2 => ResourceLoader.GetOrCreateResource(ref _Cyclic2, "SymbolsTests.Cyclic.Cyclic2.dll");
    }

    public static class CyclicInheritance
    {
        private static byte[] _Class1;
        public static byte[] Class1 => ResourceLoader.GetOrCreateResource(ref _Class1, "SymbolsTests.CyclicInheritance.Class1.dll");

        private static byte[] _Class2;
        public static byte[] Class2 => ResourceLoader.GetOrCreateResource(ref _Class2, "SymbolsTests.CyclicInheritance.Class2.dll");

        private static byte[] _Class3;
        public static byte[] Class3 => ResourceLoader.GetOrCreateResource(ref _Class3, "SymbolsTests.CyclicInheritance.Class3.dll");
    }

    public static class CyclicStructure
    {
        private static byte[] _cycledstructs;
        public static byte[] cycledstructs => ResourceLoader.GetOrCreateResource(ref _cycledstructs, "SymbolsTests.CyclicStructure.cycledstructs.dll");
    }

    public static class DifferByCase
    {
        private static byte[] _Consumer;
        public static byte[] Consumer => ResourceLoader.GetOrCreateResource(ref _Consumer, "SymbolsTests.DifferByCase.Consumer.dll");

        private static byte[] _CsharpCaseSen;
        public static byte[] CsharpCaseSen => ResourceLoader.GetOrCreateResource(ref _CsharpCaseSen, "SymbolsTests.DifferByCase.CsharpCaseSen.dll");

        private static byte[] _CSharpDifferCaseOverloads;
        public static byte[] CSharpDifferCaseOverloads => ResourceLoader.GetOrCreateResource(ref _CSharpDifferCaseOverloads, "SymbolsTests.DifferByCase.CSharpDifferCaseOverloads.dll");

        private static byte[] _TypeAndNamespaceDifferByCase;
        public static byte[] TypeAndNamespaceDifferByCase => ResourceLoader.GetOrCreateResource(ref _TypeAndNamespaceDifferByCase, "SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase.dll");
    }

    public static class Fields
    {
        private static byte[] _ConstantFields;
        public static byte[] ConstantFields => ResourceLoader.GetOrCreateResource(ref _ConstantFields, "SymbolsTests.Fields.ConstantFields.dll");

        private static byte[] _CSFields;
        public static byte[] CSFields => ResourceLoader.GetOrCreateResource(ref _CSFields, "SymbolsTests.Fields.CSFields.dll");

        private static byte[] _VBFields;
        public static byte[] VBFields => ResourceLoader.GetOrCreateResource(ref _VBFields, "SymbolsTests.Fields.VBFields.dll");
    }

    public static class Interface
    {
        private static byte[] _MDInterfaceMapping;
        public static byte[] MDInterfaceMapping => ResourceLoader.GetOrCreateResource(ref _MDInterfaceMapping, "SymbolsTests.Interface.MDInterfaceMapping.dll");

        private static byte[] _StaticMethodInInterface;
        public static byte[] StaticMethodInInterface => ResourceLoader.GetOrCreateResource(ref _StaticMethodInInterface, "SymbolsTests.Interface.StaticMethodInInterface.dll");
    }

    public static class Metadata
    {
        private static byte[] _AttributeInterop01;
        public static byte[] AttributeInterop01 => ResourceLoader.GetOrCreateResource(ref _AttributeInterop01, "SymbolsTests.Metadata.AttributeInterop01.dll");

        private static byte[] _AttributeInterop02;
        public static byte[] AttributeInterop02 => ResourceLoader.GetOrCreateResource(ref _AttributeInterop02, "SymbolsTests.Metadata.AttributeInterop02.dll");

        private static byte[] _AttributeTestDef01;
        public static byte[] AttributeTestDef01 => ResourceLoader.GetOrCreateResource(ref _AttributeTestDef01, "SymbolsTests.Metadata.AttributeTestDef01.dll");

        private static byte[] _AttributeTestLib01;
        public static byte[] AttributeTestLib01 => ResourceLoader.GetOrCreateResource(ref _AttributeTestLib01, "SymbolsTests.Metadata.AttributeTestLib01.dll");

        private static byte[] _DynamicAttribute;
        public static byte[] DynamicAttribute => ResourceLoader.GetOrCreateResource(ref _DynamicAttribute, "SymbolsTests.Metadata.DynamicAttribute.dll");

        private static byte[] _InvalidCharactersInAssemblyName;
        public static byte[] InvalidCharactersInAssemblyName => ResourceLoader.GetOrCreateResource(ref _InvalidCharactersInAssemblyName, "SymbolsTests.Metadata.InvalidCharactersInAssemblyName.dll");

        private static byte[] _InvalidPublicKey;
        public static byte[] InvalidPublicKey => ResourceLoader.GetOrCreateResource(ref _InvalidPublicKey, "SymbolsTests.Metadata.InvalidPublicKey.dll");

        private static byte[] _MDTestAttributeApplicationLib;
        public static byte[] MDTestAttributeApplicationLib => ResourceLoader.GetOrCreateResource(ref _MDTestAttributeApplicationLib, "SymbolsTests.Metadata.MDTestAttributeApplicationLib.dll");

        private static byte[] _MDTestAttributeDefLib;
        public static byte[] MDTestAttributeDefLib => ResourceLoader.GetOrCreateResource(ref _MDTestAttributeDefLib, "SymbolsTests.Metadata.MDTestAttributeDefLib.dll");

        private static byte[] _MscorlibNamespacesAndTypes;
        public static byte[] MscorlibNamespacesAndTypes => ResourceLoader.GetOrCreateResource(ref _MscorlibNamespacesAndTypes, "SymbolsTests.Metadata.MscorlibNamespacesAndTypes.bsl");
    }

    public static class Methods
    {
        private static byte[] _ByRefReturn;
        public static byte[] ByRefReturn => ResourceLoader.GetOrCreateResource(ref _ByRefReturn, "SymbolsTests.Methods.ByRefReturn.dll");

        private static byte[] _CSMethods;
        public static byte[] CSMethods => ResourceLoader.GetOrCreateResource(ref _CSMethods, "SymbolsTests.Methods.CSMethods.dll");

        private static byte[] _ILMethods;
        public static byte[] ILMethods => ResourceLoader.GetOrCreateResource(ref _ILMethods, "SymbolsTests.Methods.ILMethods.dll");

        private static byte[] _VBMethods;
        public static byte[] VBMethods => ResourceLoader.GetOrCreateResource(ref _VBMethods, "SymbolsTests.Methods.VBMethods.dll");
    }

    public static class MissingTypes
    {
        private static byte[] _CL2;
        public static byte[] CL2 => ResourceLoader.GetOrCreateResource(ref _CL2, "SymbolsTests.MissingTypes.CL2.dll");

        private static byte[] _CL3;
        public static byte[] CL3 => ResourceLoader.GetOrCreateResource(ref _CL3, "SymbolsTests.MissingTypes.CL3.dll");

        private static string _CL3_VB;
        public static string CL3_VB => ResourceLoader.GetOrCreateResource(ref _CL3_VB, "SymbolsTests.MissingTypes.CL3.vb");

        private static byte[] _MDMissingType;
        public static byte[] MDMissingType => ResourceLoader.GetOrCreateResource(ref _MDMissingType, "SymbolsTests.MissingTypes.MDMissingType.dll");

        private static byte[] _MDMissingTypeLib;
        public static byte[] MDMissingTypeLib => ResourceLoader.GetOrCreateResource(ref _MDMissingTypeLib, "SymbolsTests.MissingTypes.MDMissingTypeLib.dll");

        private static byte[] _MDMissingTypeLib_New;
        public static byte[] MDMissingTypeLib_New => ResourceLoader.GetOrCreateResource(ref _MDMissingTypeLib_New, "SymbolsTests.MissingTypes.MDMissingTypeLib_New.dll");

        private static byte[] _MissingTypesEquality1;
        public static byte[] MissingTypesEquality1 => ResourceLoader.GetOrCreateResource(ref _MissingTypesEquality1, "SymbolsTests.MissingTypes.MissingTypesEquality1.dll");

        private static byte[] _MissingTypesEquality2;
        public static byte[] MissingTypesEquality2 => ResourceLoader.GetOrCreateResource(ref _MissingTypesEquality2, "SymbolsTests.MissingTypes.MissingTypesEquality2.dll");
    }

    public static class MultiModule
    {
        private static byte[] _Consumer;
        public static byte[] Consumer => ResourceLoader.GetOrCreateResource(ref _Consumer, "SymbolsTests.MultiModule.Consumer.dll");

        private static byte[] _mod2;
        public static byte[] mod2 => ResourceLoader.GetOrCreateResource(ref _mod2, "SymbolsTests.MultiModule.mod2.netmodule");

        private static byte[] _mod3;
        public static byte[] mod3 => ResourceLoader.GetOrCreateResource(ref _mod3, "SymbolsTests.MultiModule.mod3.netmodule");

        private static byte[] _MultiModuleDll;
        public static byte[] MultiModuleDll => ResourceLoader.GetOrCreateResource(ref _MultiModuleDll, "SymbolsTests.MultiModule.MultiModule.dll");
    }

    public static class MultiTargeting
    {
        private static byte[] _Source1Module;
        public static byte[] Source1Module => ResourceLoader.GetOrCreateResource(ref _Source1Module, "SymbolsTests.MultiTargeting.Source1Module.netmodule");

        private static byte[] _Source3Module;
        public static byte[] Source3Module => ResourceLoader.GetOrCreateResource(ref _Source3Module, "SymbolsTests.MultiTargeting.Source3Module.netmodule");

        private static byte[] _Source4Module;
        public static byte[] Source4Module => ResourceLoader.GetOrCreateResource(ref _Source4Module, "SymbolsTests.MultiTargeting.Source4Module.netmodule");

        private static byte[] _Source5Module;
        public static byte[] Source5Module => ResourceLoader.GetOrCreateResource(ref _Source5Module, "SymbolsTests.MultiTargeting.Source5Module.netmodule");

        private static byte[] _Source7Module;
        public static byte[] Source7Module => ResourceLoader.GetOrCreateResource(ref _Source7Module, "SymbolsTests.MultiTargeting.Source7Module.netmodule");
    }

    public static class netModule
    {
        private static byte[] _CrossRefLib;
        public static byte[] CrossRefLib => ResourceLoader.GetOrCreateResource(ref _CrossRefLib, "SymbolsTests.netModule.CrossRefLib.dll");

        private static byte[] _CrossRefModule1;
        public static byte[] CrossRefModule1 => ResourceLoader.GetOrCreateResource(ref _CrossRefModule1, "SymbolsTests.netModule.CrossRefModule1.netmodule");

        private static byte[] _CrossRefModule2;
        public static byte[] CrossRefModule2 => ResourceLoader.GetOrCreateResource(ref _CrossRefModule2, "SymbolsTests.netModule.CrossRefModule2.netmodule");

        private static byte[] _hash_module;
        public static byte[] hash_module => ResourceLoader.GetOrCreateResource(ref _hash_module, "SymbolsTests.netModule.hash_module.netmodule");

        private static byte[] _netModule1;
        public static byte[] netModule1 => ResourceLoader.GetOrCreateResource(ref _netModule1, "SymbolsTests.netModule.netModule1.netmodule");

        private static byte[] _netModule2;
        public static byte[] netModule2 => ResourceLoader.GetOrCreateResource(ref _netModule2, "SymbolsTests.netModule.netModule2.netmodule");

        private static byte[] _x64COFF;
        public static byte[] x64COFF => ResourceLoader.GetOrCreateResource(ref _x64COFF, "SymbolsTests.netModule.x64COFF.obj");
    }

    public static class NoPia
    {
        private static byte[] _A;
        public static byte[] A => ResourceLoader.GetOrCreateResource(ref _A, "SymbolsTests.NoPia.A.dll");

        private static byte[] _B;
        public static byte[] B => ResourceLoader.GetOrCreateResource(ref _B, "SymbolsTests.NoPia.B.dll");

        private static byte[] _C;
        public static byte[] C => ResourceLoader.GetOrCreateResource(ref _C, "SymbolsTests.NoPia.C.dll");

        private static byte[] _D;
        public static byte[] D => ResourceLoader.GetOrCreateResource(ref _D, "SymbolsTests.NoPia.D.dll");

        private static byte[] _ExternalAsm1;
        public static byte[] ExternalAsm1 => ResourceLoader.GetOrCreateResource(ref _ExternalAsm1, "SymbolsTests.NoPia.ExternalAsm1.dll");

        private static byte[] _GeneralPia;
        public static byte[] GeneralPia => ResourceLoader.GetOrCreateResource(ref _GeneralPia, "SymbolsTests.NoPia.GeneralPia.dll");

        private static byte[] _GeneralPiaCopy;
        public static byte[] GeneralPiaCopy => ResourceLoader.GetOrCreateResource(ref _GeneralPiaCopy, "SymbolsTests.NoPia.GeneralPiaCopy.dll");

        private static byte[] _Library1;
        public static byte[] Library1 => ResourceLoader.GetOrCreateResource(ref _Library1, "SymbolsTests.NoPia.Library1.dll");

        private static byte[] _Library2;
        public static byte[] Library2 => ResourceLoader.GetOrCreateResource(ref _Library2, "SymbolsTests.NoPia.Library2.dll");

        private static byte[] _LocalTypes1;
        public static byte[] LocalTypes1 => ResourceLoader.GetOrCreateResource(ref _LocalTypes1, "SymbolsTests.NoPia.LocalTypes1.dll");

        private static byte[] _LocalTypes2;
        public static byte[] LocalTypes2 => ResourceLoader.GetOrCreateResource(ref _LocalTypes2, "SymbolsTests.NoPia.LocalTypes2.dll");

        private static byte[] _LocalTypes3;
        public static byte[] LocalTypes3 => ResourceLoader.GetOrCreateResource(ref _LocalTypes3, "SymbolsTests.NoPia.LocalTypes3.dll");

        private static byte[] _MissingPIAAttributes;
        public static byte[] MissingPIAAttributes => ResourceLoader.GetOrCreateResource(ref _MissingPIAAttributes, "SymbolsTests.NoPia.MissingPIAAttributes.dll");

        private static byte[] _NoPIAGenerics1_Asm1;
        public static byte[] NoPIAGenerics1_Asm1 => ResourceLoader.GetOrCreateResource(ref _NoPIAGenerics1_Asm1, "SymbolsTests.NoPia.NoPIAGenerics1-Asm1.dll");

        private static byte[] _Pia1;
        public static byte[] Pia1 => ResourceLoader.GetOrCreateResource(ref _Pia1, "SymbolsTests.NoPia.Pia1.dll");

        private static byte[] _Pia1Copy;
        public static byte[] Pia1Copy => ResourceLoader.GetOrCreateResource(ref _Pia1Copy, "SymbolsTests.NoPia.Pia1Copy.dll");

        private static byte[] _Pia2;
        public static byte[] Pia2 => ResourceLoader.GetOrCreateResource(ref _Pia2, "SymbolsTests.NoPia.Pia2.dll");

        private static byte[] _Pia3;
        public static byte[] Pia3 => ResourceLoader.GetOrCreateResource(ref _Pia3, "SymbolsTests.NoPia.Pia3.dll");

        private static byte[] _Pia4;
        public static byte[] Pia4 => ResourceLoader.GetOrCreateResource(ref _Pia4, "SymbolsTests.NoPia.Pia4.dll");

        private static byte[] _Pia5;
        public static byte[] Pia5 => ResourceLoader.GetOrCreateResource(ref _Pia5, "SymbolsTests.NoPia.Pia5.dll");
    }
}

namespace TestResources.SymbolsTests.RetargetingCycle
{
    public static class RetV1
    {
        private static byte[] _ClassA;
        public static byte[] ClassA => ResourceLoader.GetOrCreateResource(ref _ClassA, "SymbolsTests.RetargetingCycle.V1.ClassA.dll");

        private static byte[] _ClassB;
        public static byte[] ClassB => ResourceLoader.GetOrCreateResource(ref _ClassB, "SymbolsTests.RetargetingCycle.V1.ClassB.netmodule");
    }

    public static class RetV2
    {
        private static byte[] _ClassA;
        public static byte[] ClassA => ResourceLoader.GetOrCreateResource(ref _ClassA, "SymbolsTests.RetargetingCycle.V2.ClassA.dll");

        private static byte[] _ClassB;
        public static byte[] ClassB => ResourceLoader.GetOrCreateResource(ref _ClassB, "SymbolsTests.RetargetingCycle.V2.ClassB.dll");
    }
}

namespace TestResources.SymbolsTests
{
    public static class TypeForwarders
    {
        private static byte[] _Forwarded;
        public static byte[] Forwarded => ResourceLoader.GetOrCreateResource(ref _Forwarded, "SymbolsTests.TypeForwarders.Forwarded.netmodule");

        private static byte[] _TypeForwarder;
        public static byte[] TypeForwarder => ResourceLoader.GetOrCreateResource(ref _TypeForwarder, "SymbolsTests.TypeForwarders.TypeForwarder.dll");

        private static byte[] _TypeForwarderBase;
        public static byte[] TypeForwarderBase => ResourceLoader.GetOrCreateResource(ref _TypeForwarderBase, "SymbolsTests.TypeForwarders.TypeForwarderBase.dll");

        private static byte[] _TypeForwarderLib;
        public static byte[] TypeForwarderLib => ResourceLoader.GetOrCreateResource(ref _TypeForwarderLib, "SymbolsTests.TypeForwarders.TypeForwarderLib.dll");
    }

    public static class V1
    {
        private static byte[] _MTTestLib1;
        public static byte[] MTTestLib1 => ResourceLoader.GetOrCreateResource(ref _MTTestLib1, "SymbolsTests.V1.MTTestLib1.Dll");

        private static string _MTTestLib1_V1;
        public static string MTTestLib1_V1 => ResourceLoader.GetOrCreateResource(ref _MTTestLib1_V1, "SymbolsTests.V1.MTTestLib1_V1.vb");

        private static byte[] _MTTestLib2;
        public static byte[] MTTestLib2 => ResourceLoader.GetOrCreateResource(ref _MTTestLib2, "SymbolsTests.V1.MTTestLib2.Dll");

        private static string _MTTestLib2_V1;
        public static string MTTestLib2_V1 => ResourceLoader.GetOrCreateResource(ref _MTTestLib2_V1, "SymbolsTests.V1.MTTestLib2_V1.vb");

        private static byte[] _MTTestModule1;
        public static byte[] MTTestModule1 => ResourceLoader.GetOrCreateResource(ref _MTTestModule1, "SymbolsTests.V1.MTTestModule1.netmodule");

        private static byte[] _MTTestModule2;
        public static byte[] MTTestModule2 => ResourceLoader.GetOrCreateResource(ref _MTTestModule2, "SymbolsTests.V1.MTTestModule2.netmodule");
    }

    public static class V2
    {
        private static byte[] _MTTestLib1;
        public static byte[] MTTestLib1 => ResourceLoader.GetOrCreateResource(ref _MTTestLib1, "SymbolsTests.V2.MTTestLib1.Dll");

        private static string _MTTestLib1_V2;
        public static string MTTestLib1_V2 => ResourceLoader.GetOrCreateResource(ref _MTTestLib1_V2, "SymbolsTests.V2.MTTestLib1_V2.vb");

        private static byte[] _MTTestLib3;
        public static byte[] MTTestLib3 => ResourceLoader.GetOrCreateResource(ref _MTTestLib3, "SymbolsTests.V2.MTTestLib3.Dll");

        private static string _MTTestLib3_V2;
        public static string MTTestLib3_V2 => ResourceLoader.GetOrCreateResource(ref _MTTestLib3_V2, "SymbolsTests.V2.MTTestLib3_V2.vb");

        private static byte[] _MTTestModule1;
        public static byte[] MTTestModule1 => ResourceLoader.GetOrCreateResource(ref _MTTestModule1, "SymbolsTests.V2.MTTestModule1.netmodule");

        private static byte[] _MTTestModule3;
        public static byte[] MTTestModule3 => ResourceLoader.GetOrCreateResource(ref _MTTestModule3, "SymbolsTests.V2.MTTestModule3.netmodule");
    }

    public static class V3
    {
        private static byte[] _MTTestLib1;
        public static byte[] MTTestLib1 => ResourceLoader.GetOrCreateResource(ref _MTTestLib1, "SymbolsTests.V3.MTTestLib1.Dll");

        private static string _MTTestLib1_V3;
        public static string MTTestLib1_V3 => ResourceLoader.GetOrCreateResource(ref _MTTestLib1_V3, "SymbolsTests.V3.MTTestLib1_V3.vb");

        private static byte[] _MTTestLib4;
        public static byte[] MTTestLib4 => ResourceLoader.GetOrCreateResource(ref _MTTestLib4, "SymbolsTests.V3.MTTestLib4.Dll");

        private static string _MTTestLib4_V3;
        public static string MTTestLib4_V3 => ResourceLoader.GetOrCreateResource(ref _MTTestLib4_V3, "SymbolsTests.V3.MTTestLib4_V3.vb");

        private static byte[] _MTTestModule1;
        public static byte[] MTTestModule1 => ResourceLoader.GetOrCreateResource(ref _MTTestModule1, "SymbolsTests.V3.MTTestModule1.netmodule");

        private static byte[] _MTTestModule4;
        public static byte[] MTTestModule4 => ResourceLoader.GetOrCreateResource(ref _MTTestModule4, "SymbolsTests.V3.MTTestModule4.netmodule");
    }

    public static class WithEvents
    {
        private static byte[] _SimpleWithEvents;
        public static byte[] SimpleWithEvents => ResourceLoader.GetOrCreateResource(ref _SimpleWithEvents, "SymbolsTests.WithEvents.SimpleWithEvents.dll");
    }
}

namespace TestResources
{
    public static class WinRt
    {
        private static byte[] _W1;
        public static byte[] W1 => ResourceLoader.GetOrCreateResource(ref _W1, "WinRt.W1.winmd");

        private static byte[] _W2;
        public static byte[] W2 => ResourceLoader.GetOrCreateResource(ref _W2, "WinRt.W2.winmd");

        private static byte[] _WB;
        public static byte[] WB => ResourceLoader.GetOrCreateResource(ref _WB, "WinRt.WB.winmd");

        private static byte[] _WB_Version1;
        public static byte[] WB_Version1 => ResourceLoader.GetOrCreateResource(ref _WB_Version1, "WinRt.WB_Version1.winmd");

        private static byte[] _WImpl;
        public static byte[] WImpl => ResourceLoader.GetOrCreateResource(ref _WImpl, "WinRt.WImpl.winmd");

        private static byte[] _Windows_dump;
        public static byte[] Windows_dump => ResourceLoader.GetOrCreateResource(ref _Windows_dump, "WinRt.Windows.ildump");

        private static byte[] _Windows_Languages_WinRTTest;
        public static byte[] Windows_Languages_WinRTTest => ResourceLoader.GetOrCreateResource(ref _Windows_Languages_WinRTTest, "WinRt.Windows.Languages.WinRTTest.winmd");

        private static byte[] _Windows;
        public static byte[] Windows => ResourceLoader.GetOrCreateResource(ref _Windows, "WinRt.Windows.winmd");

        private static byte[] _WinMDPrefixing_dump;
        public static byte[] WinMDPrefixing_dump => ResourceLoader.GetOrCreateResource(ref _WinMDPrefixing_dump, "WinRt.WinMDPrefixing.ildump");

        private static byte[] _WinMDPrefixing;
        public static byte[] WinMDPrefixing => ResourceLoader.GetOrCreateResource(ref _WinMDPrefixing, "WinRt.WinMDPrefixing.winmd");
    }
}
