// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

namespace TestResources
{
    public static class DiagnosticTests
    {
        private static byte[] s_badresfile;
        public static byte[] badresfile => ResourceLoader.GetOrCreateResource(ref s_badresfile, "DiagnosticTests.badresfile.res");

        private static byte[] s_errTestLib01;
        public static byte[] ErrTestLib01 => ResourceLoader.GetOrCreateResource(ref s_errTestLib01, "DiagnosticTests.ErrTestLib01.dll");

        private static byte[] s_errTestLib02;
        public static byte[] ErrTestLib02 => ResourceLoader.GetOrCreateResource(ref s_errTestLib02, "DiagnosticTests.ErrTestLib02.dll");

        private static byte[] s_errTestLib11;
        public static byte[] ErrTestLib11 => ResourceLoader.GetOrCreateResource(ref s_errTestLib11, "DiagnosticTests.ErrTestLib11.dll");

        private static byte[] s_errTestMod01;
        public static byte[] ErrTestMod01 => ResourceLoader.GetOrCreateResource(ref s_errTestMod01, "DiagnosticTests.ErrTestMod01.netmodule");

        private static byte[] s_errTestMod02;
        public static byte[] ErrTestMod02 => ResourceLoader.GetOrCreateResource(ref s_errTestMod02, "DiagnosticTests.ErrTestMod02.netmodule");
    }

    public static class Basic
    {
        private static byte[] s_members;
        public static byte[] Members => ResourceLoader.GetOrCreateResource(ref s_members, "MetadataTests.Members.dll");

        private static byte[] s_nativeApp;
        public static byte[] NativeApp => ResourceLoader.GetOrCreateResource(ref s_nativeApp, "MetadataTests.NativeApp.exe");
    }

    public static class ExpressionCompiler
    {
        private static byte[] s_empty;
        public static byte[] Empty => ResourceLoader.GetOrCreateResource(ref s_empty, "ExpressionCompiler.Empty.dll");

        private static byte[] s_libraryA;
        public static byte[] LibraryA => ResourceLoader.GetOrCreateResource(ref s_libraryA, "ExpressionCompiler.LibraryA.winmd");

        private static byte[] s_libraryB;
        public static byte[] LibraryB => ResourceLoader.GetOrCreateResource(ref s_libraryB, "ExpressionCompiler.LibraryB.winmd");

        private static byte[] s_noValidTables;
        public static byte[] NoValidTables => ResourceLoader.GetOrCreateResource(ref s_noValidTables, "ExpressionCompiler.NoValidTables.metadata");

        private static byte[] s_windows;
        public static byte[] Windows => ResourceLoader.GetOrCreateResource(ref s_windows, "ExpressionCompiler.Windows.winmd");

        private static byte[] s_windowsData;
        public static byte[] WindowsData => ResourceLoader.GetOrCreateResource(ref s_windowsData, "ExpressionCompiler.Windows.Data.winmd");

        private static byte[] s_windowsStorage;
        public static byte[] WindowsStorage => ResourceLoader.GetOrCreateResource(ref s_windowsStorage, "ExpressionCompiler.Windows.Storage.winmd");
    }
}

namespace TestResources.MetadataTests
{
    public static class InterfaceAndClass
    {
        private static byte[] s_CSClasses01;
        public static byte[] CSClasses01 => ResourceLoader.GetOrCreateResource(ref s_CSClasses01, "MetadataTests.InterfaceAndClass.CSClasses01.dll");

        private static byte[] s_CSInterfaces01;
        public static byte[] CSInterfaces01 => ResourceLoader.GetOrCreateResource(ref s_CSInterfaces01, "MetadataTests.InterfaceAndClass.CSInterfaces01.dll");

        private static byte[] s_VBClasses01;
        public static byte[] VBClasses01 => ResourceLoader.GetOrCreateResource(ref s_VBClasses01, "MetadataTests.InterfaceAndClass.VBClasses01.dll");

        private static byte[] s_VBClasses02;
        public static byte[] VBClasses02 => ResourceLoader.GetOrCreateResource(ref s_VBClasses02, "MetadataTests.InterfaceAndClass.VBClasses02.dll");

        private static byte[] s_VBInterfaces01;
        public static byte[] VBInterfaces01 => ResourceLoader.GetOrCreateResource(ref s_VBInterfaces01, "MetadataTests.InterfaceAndClass.VBInterfaces01.dll");
    }

    public static class Interop
    {
        private static byte[] s_indexerWithByRefParam;
        public static byte[] IndexerWithByRefParam => ResourceLoader.GetOrCreateResource(ref s_indexerWithByRefParam, "MetadataTests.Interop.IndexerWithByRefParam.dll");

        private static byte[] s_interop_Mock01;
        public static byte[] Interop_Mock01 => ResourceLoader.GetOrCreateResource(ref s_interop_Mock01, "MetadataTests.Interop.Interop.Mock01.dll");

        private static byte[] s_interop_Mock01_Impl;
        public static byte[] Interop_Mock01_Impl => ResourceLoader.GetOrCreateResource(ref s_interop_Mock01_Impl, "MetadataTests.Interop.Interop.Mock01.Impl.dll");
    }

    public static class Invalid
    {
        private static byte[] s_classLayout;
        public static byte[] ClassLayout => ResourceLoader.GetOrCreateResource(ref s_classLayout, "MetadataTests.Invalid.ClassLayout.dll");

        private static byte[] s_customAttributeTableUnsorted;
        public static byte[] CustomAttributeTableUnsorted => ResourceLoader.GetOrCreateResource(ref s_customAttributeTableUnsorted, "MetadataTests.Invalid.CustomAttributeTableUnsorted.dll");

        private static byte[] s_emptyModuleTable;
        public static byte[] EmptyModuleTable => ResourceLoader.GetOrCreateResource(ref s_emptyModuleTable, "MetadataTests.Invalid.EmptyModuleTable.netmodule");

        private static byte[] s_incorrectCustomAssemblyTableSize_TooManyMethodSpecs;
        public static byte[] IncorrectCustomAssemblyTableSize_TooManyMethodSpecs => ResourceLoader.GetOrCreateResource(ref s_incorrectCustomAssemblyTableSize_TooManyMethodSpecs, "MetadataTests.Invalid.IncorrectCustomAssemblyTableSize_TooManyMethodSpecs.dll");

        private static byte[] s_invalidDynamicAttributeArgs;
        public static byte[] InvalidDynamicAttributeArgs => ResourceLoader.GetOrCreateResource(ref s_invalidDynamicAttributeArgs, "MetadataTests.Invalid.InvalidDynamicAttributeArgs.dll");

        private static byte[] s_invalidFuncDelegateName;
        public static byte[] InvalidFuncDelegateName => ResourceLoader.GetOrCreateResource(ref s_invalidFuncDelegateName, "MetadataTests.Invalid.InvalidFuncDelegateName.dll");

        private static byte[] s_invalidGenericType;
        public static byte[] InvalidGenericType => ResourceLoader.GetOrCreateResource(ref s_invalidGenericType, "MetadataTests.Invalid.InvalidGenericType.dll");

        private static byte[] s_invalidModuleName;
        public static byte[] InvalidModuleName => ResourceLoader.GetOrCreateResource(ref s_invalidModuleName, "MetadataTests.Invalid.InvalidModuleName.dll");

        private static byte[] s_longTypeFormInSignature;
        public static byte[] LongTypeFormInSignature => ResourceLoader.GetOrCreateResource(ref s_longTypeFormInSignature, "MetadataTests.Invalid.LongTypeFormInSignature.dll");

        private static string s_manyMethodSpecs;
        public static string ManyMethodSpecs => ResourceLoader.GetOrCreateResource(ref s_manyMethodSpecs, "MetadataTests.Invalid.ManyMethodSpecs.vb");

        private static byte[] s_obfuscated;
        public static byte[] Obfuscated => ResourceLoader.GetOrCreateResource(ref s_obfuscated, "MetadataTests.Invalid.Obfuscated.dll");

        private static byte[] s_obfuscated2;
        public static byte[] Obfuscated2 => ResourceLoader.GetOrCreateResource(ref s_obfuscated2, "MetadataTests.Invalid.Obfuscated2.dll");

        public static class Signatures
        {
            private static byte[] s_signatureCycle2;
            public static byte[] SignatureCycle2 => ResourceLoader.GetOrCreateResource(ref s_signatureCycle2, "MetadataTests.Invalid.Signatures.SignatureCycle2.exe");

            private static byte[] s_typeSpecInWrongPlace;
            public static byte[] TypeSpecInWrongPlace => ResourceLoader.GetOrCreateResource(ref s_typeSpecInWrongPlace, "MetadataTests.Invalid.Signatures.TypeSpecInWrongPlace.exe");
        }
    }

    public static class NetModule01
    {
        private static byte[] s_appCS;
        public static byte[] AppCS => ResourceLoader.GetOrCreateResource(ref s_appCS, "MetadataTests.NetModule01.AppCS.exe");

        private static byte[] s_moduleCS00;
        public static byte[] ModuleCS00 => ResourceLoader.GetOrCreateResource(ref s_moduleCS00, "MetadataTests.NetModule01.ModuleCS00.mod");

        private static byte[] s_moduleCS01;
        public static byte[] ModuleCS01 => ResourceLoader.GetOrCreateResource(ref s_moduleCS01, "MetadataTests.NetModule01.ModuleCS01.mod");

        private static byte[] s_moduleVB01;
        public static byte[] ModuleVB01 => ResourceLoader.GetOrCreateResource(ref s_moduleVB01, "MetadataTests.NetModule01.ModuleVB01.mod");
    }
}

namespace TestResources.NetFX
{
    public static class aacorlib_v15_0_3928
    {
        private static string s_aacorlib_v15_0_3928_cs;
        public static string aacorlib_v15_0_3928_cs => ResourceLoader.GetOrCreateResource(ref s_aacorlib_v15_0_3928_cs, "NetFX.aacorlib.aacorlib.v15.0.3928.cs");
    }

    public static class Minimal
    {
        private static byte[] s_mincorlib;
        public static byte[] mincorlib => ResourceLoader.GetOrCreateResource(ref s_mincorlib, "NetFX.Minimal.mincorlib.dll");

        private static byte[] s_minasync;
        public static byte[] minasync => ResourceLoader.GetOrCreateResource(ref s_minasync, "NetFX.Minimal.minasync.dll");

        private static byte[] s_minasynccorlib;
        public static byte[] minasynccorlib => ResourceLoader.GetOrCreateResource(ref s_minasynccorlib, "NetFX.Minimal.minasynccorlib.dll");
    }

    public static class ValueTuple
    {
        private static byte[] s_tuplelib;
        public static byte[] tuplelib => ResourceLoader.GetOrCreateResource(ref s_tuplelib, "NetFX.ValueTuple.System.ValueTuple.dll");

        private static string s_tuplelib_cs;
        public static string tuplelib_cs => ResourceLoader.GetOrCreateResource(ref s_tuplelib_cs, "NetFX.ValueTuple.ValueTuple.cs");

        private static string s_tupleAttributes_cs;
        public static string tupleattributes_cs => ResourceLoader.GetOrCreateResource(ref s_tupleAttributes_cs, "NetFX.ValueTuple.TupleElementNamesAttribute.cs");
    }
}

namespace TestResources
{
    public static class PerfTests
    {
        private static string s_CSPerfTest;
        public static string CSPerfTest => ResourceLoader.GetOrCreateResource(ref s_CSPerfTest, "PerfTests.CSPerfTest.cs");

        private static string s_VBPerfTest;
        public static string VBPerfTest => ResourceLoader.GetOrCreateResource(ref s_VBPerfTest, "PerfTests.VBPerfTest.vb");
    }

    public static class General
    {
        private static byte[] s_bigVisitor;
        public static byte[] BigVisitor => ResourceLoader.GetOrCreateResource(ref s_bigVisitor, "SymbolsTests.BigVisitor.dll");

        private static byte[] s_delegateByRefParamArray;
        public static byte[] DelegateByRefParamArray => ResourceLoader.GetOrCreateResource(ref s_delegateByRefParamArray, "SymbolsTests.Delegates.DelegateByRefParamArray.dll");

        private static byte[] s_delegatesWithoutInvoke;
        public static byte[] DelegatesWithoutInvoke => ResourceLoader.GetOrCreateResource(ref s_delegatesWithoutInvoke, "SymbolsTests.Delegates.DelegatesWithoutInvoke.dll");

        private static byte[] s_shiftJisSource;
        public static byte[] ShiftJisSource => ResourceLoader.GetOrCreateResource(ref s_shiftJisSource, "Encoding.sjis.cs");

        private static byte[] s_events;
        public static byte[] Events => ResourceLoader.GetOrCreateResource(ref s_events, "SymbolsTests.Events.dll");

        private static byte[] s_CSharpExplicitInterfaceImplementation;
        public static byte[] CSharpExplicitInterfaceImplementation => ResourceLoader.GetOrCreateResource(ref s_CSharpExplicitInterfaceImplementation, "SymbolsTests.ExplicitInterfaceImplementation.CSharpExplicitInterfaceImplementation.dll");

        private static byte[] s_CSharpExplicitInterfaceImplementationEvents;
        public static byte[] CSharpExplicitInterfaceImplementationEvents => ResourceLoader.GetOrCreateResource(ref s_CSharpExplicitInterfaceImplementationEvents, "SymbolsTests.ExplicitInterfaceImplementation.CSharpExplicitInterfaceImplementationEvents.dll");

        private static byte[] s_CSharpExplicitInterfaceImplementationProperties;
        public static byte[] CSharpExplicitInterfaceImplementationProperties => ResourceLoader.GetOrCreateResource(ref s_CSharpExplicitInterfaceImplementationProperties, "SymbolsTests.ExplicitInterfaceImplementation.CSharpExplicitInterfaceImplementationProperties.dll");

        private static byte[] s_ILExplicitInterfaceImplementation;
        public static byte[] ILExplicitInterfaceImplementation => ResourceLoader.GetOrCreateResource(ref s_ILExplicitInterfaceImplementation, "SymbolsTests.ExplicitInterfaceImplementation.ILExplicitInterfaceImplementation.dll");

        private static byte[] s_ILExplicitInterfaceImplementationProperties;
        public static byte[] ILExplicitInterfaceImplementationProperties => ResourceLoader.GetOrCreateResource(ref s_ILExplicitInterfaceImplementationProperties, "SymbolsTests.ExplicitInterfaceImplementation.ILExplicitInterfaceImplementationProperties.dll");

        private static byte[] s_FSharpTestLibrary;
        public static byte[] FSharpTestLibrary => ResourceLoader.GetOrCreateResource(ref s_FSharpTestLibrary, "SymbolsTests.FSharpTestLibrary.dll");

        private static byte[] s_indexers;
        public static byte[] Indexers => ResourceLoader.GetOrCreateResource(ref s_indexers, "SymbolsTests.Indexers.dll");

        private static byte[] s_inheritIComparable;
        public static byte[] InheritIComparable => ResourceLoader.GetOrCreateResource(ref s_inheritIComparable, "SymbolsTests.InheritIComparable.dll");

        private static byte[] s_MVID1;
        public static byte[] MVID1 => ResourceLoader.GetOrCreateResource(ref s_MVID1, "SymbolsTests.RefAssembly.MVID1.dll");

        private static byte[] s_MVID2;
        public static byte[] MVID2 => ResourceLoader.GetOrCreateResource(ref s_MVID2, "SymbolsTests.RefAssembly.MVID2.dll");

        private static byte[] s_MDTestLib1;
        public static byte[] MDTestLib1 => ResourceLoader.GetOrCreateResource(ref s_MDTestLib1, "SymbolsTests.MDTestLib1.dll");

        private static byte[] s_MDTestLib2;
        public static byte[] MDTestLib2 => ResourceLoader.GetOrCreateResource(ref s_MDTestLib2, "SymbolsTests.MDTestLib2.dll");

        private static byte[] s_nativeCOFFResources;
        public static byte[] nativeCOFFResources => ResourceLoader.GetOrCreateResource(ref s_nativeCOFFResources, "SymbolsTests.nativeCOFFResources.obj");

        private static byte[] s_properties;
        public static byte[] Properties => ResourceLoader.GetOrCreateResource(ref s_properties, "SymbolsTests.Properties.dll");

        private static byte[] s_propertiesWithByRef;
        public static byte[] PropertiesWithByRef => ResourceLoader.GetOrCreateResource(ref s_propertiesWithByRef, "SymbolsTests.PropertiesWithByRef.dll");

        private static byte[] s_regress40025DLL;
        public static byte[] Regress40025DLL => ResourceLoader.GetOrCreateResource(ref s_regress40025DLL, "SymbolsTests.Regress40025DLL.dll");

        private static byte[] s_snKey;
        public static byte[] snKey => ResourceLoader.GetOrCreateResource(ref s_snKey, "SymbolsTests.snKey.snk");

        private static byte[] s_snKey2;
        public static byte[] snKey2 => ResourceLoader.GetOrCreateResource(ref s_snKey2, "SymbolsTests.snKey2.snk");

        private static byte[] s_snPublicKey;
        public static byte[] snPublicKey => ResourceLoader.GetOrCreateResource(ref s_snPublicKey, "SymbolsTests.snPublicKey.snk");

        private static byte[] s_snPublicKey2;
        public static byte[] snPublicKey2 => ResourceLoader.GetOrCreateResource(ref s_snPublicKey2, "SymbolsTests.snPublicKey2.snk");

        private static byte[] s_snMaxSizeKey;
        public static byte[] snMaxSizeKey => ResourceLoader.GetOrCreateResource(ref s_snMaxSizeKey, "SymbolsTests.snMaxSizeKey.snk");

        private static byte[] s_snMaxSizePublicKey;
        public static byte[] snMaxSizePublicKey => ResourceLoader.GetOrCreateResource(ref s_snMaxSizePublicKey, "SymbolsTests.snMaxSizePublicKey.snk");

        public static string snMaxSizePublicKeyString => "002400000480000014080000060200000024000052534131004000000100010079bb5332224912" +
"5411d2b44dd63b137e1b452899a7e7f626917328ff9e25c728e3e3b503ba34deab31d1f1ae1558" +
"8c4bda69eccea5b13e4a4e10b39fc2fd9f05d1ba728beb8365bad6b6da9adc653836d3ff12b9a6" +
"98900c3f593cf088b2504ec949489b6f837e76fe84ddd30ccedce1d836e5b8fb149b8e9e0b8b8f" +
"bc2cdaee0e76eb549270c4df104accb72530113f431d88982ae69ed75e09530d6951722b60342e" +
"b1f5dd5babacdb365dd71597680c50fe85bce823ee902ab3377e7eef8f96168f8c8a1e8264ba94" +
"481f5208e4c21208ea312bc1a34bd0e615b39ce8948c4a4d2c0a48b0bc901dfc0519afc378f859" +
"5a77375e6c265e1c38bdc7dbf7c4d07d36b67ac94464fe5c26aed915f1c035756d0f3363fce751" +
"0f12459060f417ab5df610ffca60e6dd739dc750189f23a47716c75a7a8e3363b198f05093d2a0" +
"c9debafbfca3d682c5ea3ed578118d9dc7d0f8828cad1c03ede009d774585b9665e0c8d7325805" +
"faba80796f668f79c92b9a195bc7530bb8ecaaba07a7cfdb70c46b96ca613102b1a674bfc742fa" +
"9562704edb78063db818c0675c9bd8c18d203fc4d5bc2685003bc6c136caf07a202578cb85480d" +
"50f6187b88fb733a2f4ce200bbda68c4ef47483a3530ae8403cb38253a06e2e9385b6d3ae9a718" +
"2ba7a23f03499cec1c92ae06dde6b304c025d23466ebbbac9e06b5d7eb932fc009bc1803d03571" +
"0ec7bce4a6176b407ffdc9a5b55a3ff444609172a146bf76ae40759634e8224ba2882371808f44" +
"59a37f8e69115424947818f19ff6609a715f550e33de0307195fe1e526c57efc7212d6cb561dd8" +
"33cb8c28ae9dc32a4bc0f775887001a5ec36cf63e5b2aa9989d3fa29ebf57e4fa89a206a32e75b" +
"cac3c2f26c3267ec1b7433d4a3b90bc01563ddbffffe586ccfb8ee59af34e3127ebf99036427e0" +
"9c107d47c1e885a032065dce6dd646305bf84fb9123392c89794318e2fdffd5eaa62d1e52d29b9" +
"4e484f2fb73fea0487bbdaa1790e79fc0e09372c6187c742c8a3f160d09818f51dc58f71ff1a1e" +
"d955d9b373bfe92e09eac22241c2b96ce0213aa266f21aae95489921269bffdf5c0a0794716daf" +
"8b5daa3a496004297b3a25c6472027f4b6f9fd82d4e297546faa6ac31579a30b3da1d6c6f04125" +
"667868b536b9d9ebd767e4d1cabbaeb977ec0738dab3b599fe63ea0ec622261d90c3c0c1ebc1ab" +
"631b2162284a9659e961c541aab1658853a9a6501e73f11c9c27b8e9bf41f03187dab5909d8433" +
"499f9dfaec2a2c907e39bf1683c75c882e469e79aba05d597a24db33479fac758f5bdc4cbd79df" +
"03ec1e403f231bfb81ff9db7ee4cfe084f5c187729cc9f072d7a710651ea15f0e43f330e321723" +
"21554d7bf9fd784d18a80a13509818286616d4a7251e2c57f1c257aa0c57bd75da0b0e01532ad5" +
"17de450733f8379a9db8c9f12ac77b65215d44b40eebb513ec9ddc9537f7811eb5283386422d90" +
"6d26077608a1f506e966426d40cc5e61e2d7e888586c85050ec29eff79116c42c9714ad6672441" +
"03a9e79af9b330825ff186b19a791b60eca8776539ca2759f9dbbd87d07f3dac38b814ae9707e4" +
"73ee52e10b3e8d8344bc06287a9c6c58ab36658b4a6ab48ae2e6d08d748b35868c5207aab08311" +
"91f451595d0104968050ae1c13e0d619fa766cd90821732b9fbcc429815606704633515cbe5ad5" +
"e33a28690534748e15413c65d9a370b12946a36796aa4d8e5b471675a3471439e133476981e21e" +
"9a4dfeae52f657a5fe3ab6cd6ad8aabc09bd5d9af77226c6cfbe01fb38546b5c0b8b825e03bda1" +
"3d85403765bd5a6cbed19fd09674fd691d732328948f5ab07e03d7f919eee0ac23f6de7d49ae44" +
"f15f8459683ab792270945ee2807158a5e6898cb912cbe3b0b6820565045a41699d0a5e3b89319" +
"fb921008e18bb1c28557600c33cf2c299a79213834cb9ec72ba6402699c381060cfebaa3faf52d" +
"9b2f1b68c3cc0db79ff47b293853b80ec4198c7fe099077f876f2d6c26305cab1c9de8bb8daae4" +
"22e1ef7c5c76949c8d27fde90281781eef364cc001d0916108d6c0ace740521ec549d912fbaf71" +
"68bd37f790b46282684030dcdc2d52cb41d4b763adfc701a1d392166d4b3269ab30fb83a4fd183" +
"4771e0ea24680c09f55413750b082787e4bb301e107c34cfab1cc88b7d68489602cb8e46bd73c9" +
"6c8de8af5285f919e93cc6251df057443460a15d432e130510f8adbaa8d28c574db7d9ef6fb947" +
"b70e274d93cfaa47d00f3318643a08815c10975722324037504d7f0e3902393d5327bc0467ea5b" +
"d555ba0671ca3873486038abeccc6d48a11c6e3ffb2acca285a53641a02233bb7e7c76ab38acf6" +
"759b985e22b18da77932c0c04217798d1473ebf41061d8c006c9479b34745fbea8a1761000d16f" +
"414a544a7dc4a5a346871981d1ed3fe4dfcb8494e95643b8bae2e13bbfcb5a432c2dfd481e1d61" +
"bab2bcc0d7140fe9b472d25112b2e241c3026a7468560ce3ed582d6872b041680bff3998d51afc" +
"a45094e3e1982510fe8573ac2d3ab596d9d0c6b43a5f72c6046f24c2ac457fd440d6f8d4dd0b71" +
"399d0c1aa366e7a86c57ba5235d327da1245b5ecdf0b3e0e81a0418a5743f3fe98ef6c9236dce0" +
"2463c798af2b239f6ddf2e5a5ffa198151c2ffbf932b7357e80e858c9ddb81fe8223897af61cae" +
"c44ae4f07e686b1d721fa78b39c7934179786592472f8739fb90fd5ae41e118fafbb30bd7b02c3" +
"cf3def669d830f4dcdf863919c1ee6c3b68a4d66a74af3088592a4055b54738804034d134c5a92" +
"e47395955d222b04472da50de86f931084653e4b0f91ffccef2c777c80d92683f8f87b6b60733d" +
"73b0035501dd2adba2bbdf6697";

        private static byte[] s_CSharpErrors;
        public static byte[] CSharpErrors => ResourceLoader.GetOrCreateResource(ref s_CSharpErrors, "SymbolsTests.UseSiteErrors.CSharpErrors.dll");

        private static byte[] s_ILErrors;
        public static byte[] ILErrors => ResourceLoader.GetOrCreateResource(ref s_ILErrors, "SymbolsTests.UseSiteErrors.ILErrors.dll");

        private static byte[] s_unavailable;
        public static byte[] Unavailable => ResourceLoader.GetOrCreateResource(ref s_unavailable, "SymbolsTests.UseSiteErrors.Unavailable.dll");

        private static byte[] s_VBConversions;
        public static byte[] VBConversions => ResourceLoader.GetOrCreateResource(ref s_VBConversions, "SymbolsTests.VBConversions.dll");

        private static byte[] s_culture_AR_SA;
        public static byte[] Culture_AR_SA => ResourceLoader.GetOrCreateResource(ref s_culture_AR_SA, "SymbolsTests.Versioning.AR_SA.Culture.dll");

        private static byte[] s_culture_EN_US;
        public static byte[] Culture_EN_US => ResourceLoader.GetOrCreateResource(ref s_culture_EN_US, "SymbolsTests.Versioning.EN_US.Culture.dll");

        private static byte[] s_C1;
        public static byte[] C1 => ResourceLoader.GetOrCreateResource(ref s_C1, "SymbolsTests.Versioning.V1.C.dll");

        private static byte[] s_C2;
        public static byte[] C2 => ResourceLoader.GetOrCreateResource(ref s_C2, "SymbolsTests.Versioning.V2.C.dll");

        private static byte[] s_with_Spaces;
        public static byte[] With_Spaces => ResourceLoader.GetOrCreateResource(ref s_with_Spaces, "SymbolsTests.With Spaces.dll");

        private static byte[] s_with_SpacesModule;
        public static byte[] With_SpacesModule => ResourceLoader.GetOrCreateResource(ref s_with_SpacesModule, "SymbolsTests.With Spaces.netmodule");
    }
}

namespace TestResources.SymbolsTests
{
    public static class CorLibrary
    {
        private static byte[] s_fakeMsCorLib;
        public static byte[] FakeMsCorLib => ResourceLoader.GetOrCreateResource(ref s_fakeMsCorLib, "SymbolsTests.CorLibrary.FakeMsCorLib.dll");

        private static byte[] s_guidTest2;
        public static byte[] GuidTest2 => ResourceLoader.GetOrCreateResource(ref s_guidTest2, "SymbolsTests.CorLibrary.GuidTest2.exe");

        private static byte[] s_noMsCorLibRef;
        public static byte[] NoMsCorLibRef => ResourceLoader.GetOrCreateResource(ref s_noMsCorLibRef, "SymbolsTests.CorLibrary.NoMsCorLibRef.dll");
    }

    public static class CustomModifiers
    {
        private static byte[] s_cppCli;
        public static byte[] CppCli => ResourceLoader.GetOrCreateResource(ref s_cppCli, "SymbolsTests.CustomModifiers.CppCli.dll");

        private static byte[] s_modifiers;
        public static byte[] Modifiers => ResourceLoader.GetOrCreateResource(ref s_modifiers, "SymbolsTests.CustomModifiers.Modifiers.dll");

        private static byte[] s_modifiersModule;
        public static byte[] ModifiersModule => ResourceLoader.GetOrCreateResource(ref s_modifiersModule, "SymbolsTests.CustomModifiers.Modifiers.netmodule");

        private static byte[] s_modoptTests;
        public static byte[] ModoptTests => ResourceLoader.GetOrCreateResource(ref s_modoptTests, "SymbolsTests.CustomModifiers.ModoptTests.dll");

        private static byte[] s_genericMethodWithModifiers;
        public static byte[] GenericMethodWithModifiers => ResourceLoader.GetOrCreateResource(ref s_genericMethodWithModifiers, "SymbolsTests.CustomModifiers.GenericMethodWithModifiers.dll");
    }

    public static class Cyclic
    {
        private static byte[] s_cyclic1;
        public static byte[] Cyclic1 => ResourceLoader.GetOrCreateResource(ref s_cyclic1, "SymbolsTests.Cyclic.Cyclic1.dll");

        private static byte[] s_cyclic2;
        public static byte[] Cyclic2 => ResourceLoader.GetOrCreateResource(ref s_cyclic2, "SymbolsTests.Cyclic.Cyclic2.dll");
    }

    public static class CyclicInheritance
    {
        private static byte[] s_class1;
        public static byte[] Class1 => ResourceLoader.GetOrCreateResource(ref s_class1, "SymbolsTests.CyclicInheritance.Class1.dll");

        private static byte[] s_class2;
        public static byte[] Class2 => ResourceLoader.GetOrCreateResource(ref s_class2, "SymbolsTests.CyclicInheritance.Class2.dll");

        private static byte[] s_class3;
        public static byte[] Class3 => ResourceLoader.GetOrCreateResource(ref s_class3, "SymbolsTests.CyclicInheritance.Class3.dll");
    }

    public static class CyclicStructure
    {
        private static byte[] s_cycledstructs;
        public static byte[] cycledstructs => ResourceLoader.GetOrCreateResource(ref s_cycledstructs, "SymbolsTests.CyclicStructure.cycledstructs.dll");
    }

    public static class DifferByCase
    {
        private static byte[] s_consumer;
        public static byte[] Consumer => ResourceLoader.GetOrCreateResource(ref s_consumer, "SymbolsTests.DifferByCase.Consumer.dll");

        private static byte[] s_csharpCaseSen;
        public static byte[] CsharpCaseSen => ResourceLoader.GetOrCreateResource(ref s_csharpCaseSen, "SymbolsTests.DifferByCase.CsharpCaseSen.dll");

        private static byte[] s_CSharpDifferCaseOverloads;
        public static byte[] CSharpDifferCaseOverloads => ResourceLoader.GetOrCreateResource(ref s_CSharpDifferCaseOverloads, "SymbolsTests.DifferByCase.CSharpDifferCaseOverloads.dll");

        private static byte[] s_typeAndNamespaceDifferByCase;
        public static byte[] TypeAndNamespaceDifferByCase => ResourceLoader.GetOrCreateResource(ref s_typeAndNamespaceDifferByCase, "SymbolsTests.DifferByCase.TypeAndNamespaceDifferByCase.dll");
    }

    public static class Fields
    {
        private static byte[] s_constantFields;
        public static byte[] ConstantFields => ResourceLoader.GetOrCreateResource(ref s_constantFields, "SymbolsTests.Fields.ConstantFields.dll");

        private static byte[] s_CSFields;
        public static byte[] CSFields => ResourceLoader.GetOrCreateResource(ref s_CSFields, "SymbolsTests.Fields.CSFields.dll");

        private static byte[] s_VBFields;
        public static byte[] VBFields => ResourceLoader.GetOrCreateResource(ref s_VBFields, "SymbolsTests.Fields.VBFields.dll");
    }

    public static class Interface
    {
        private static byte[] s_MDInterfaceMapping;
        public static byte[] MDInterfaceMapping => ResourceLoader.GetOrCreateResource(ref s_MDInterfaceMapping, "SymbolsTests.Interface.MDInterfaceMapping.dll");

        private static byte[] s_staticMethodInInterface;
        public static byte[] StaticMethodInInterface => ResourceLoader.GetOrCreateResource(ref s_staticMethodInInterface, "SymbolsTests.Interface.StaticMethodInInterface.dll");
    }

    public static class Metadata
    {
        private static byte[] s_attributeInterop01;
        public static byte[] AttributeInterop01 => ResourceLoader.GetOrCreateResource(ref s_attributeInterop01, "SymbolsTests.Metadata.AttributeInterop01.dll");

        private static byte[] s_attributeInterop02;
        public static byte[] AttributeInterop02 => ResourceLoader.GetOrCreateResource(ref s_attributeInterop02, "SymbolsTests.Metadata.AttributeInterop02.dll");

        private static byte[] s_attributeTestDef01;
        public static byte[] AttributeTestDef01 => ResourceLoader.GetOrCreateResource(ref s_attributeTestDef01, "SymbolsTests.Metadata.AttributeTestDef01.dll");

        private static byte[] s_attributeTestLib01;
        public static byte[] AttributeTestLib01 => ResourceLoader.GetOrCreateResource(ref s_attributeTestLib01, "SymbolsTests.Metadata.AttributeTestLib01.dll");

        private static byte[] s_dynamicAttribute;
        public static byte[] DynamicAttribute => ResourceLoader.GetOrCreateResource(ref s_dynamicAttribute, "SymbolsTests.Metadata.DynamicAttribute.dll");

        private static byte[] s_invalidCharactersInAssemblyName;
        public static byte[] InvalidCharactersInAssemblyName => ResourceLoader.GetOrCreateResource(ref s_invalidCharactersInAssemblyName, "SymbolsTests.Metadata.InvalidCharactersInAssemblyName.dll");

        private static byte[] s_invalidPublicKey;
        public static byte[] InvalidPublicKey => ResourceLoader.GetOrCreateResource(ref s_invalidPublicKey, "SymbolsTests.Metadata.InvalidPublicKey.dll");

        private static byte[] s_MDTestAttributeApplicationLib;
        public static byte[] MDTestAttributeApplicationLib => ResourceLoader.GetOrCreateResource(ref s_MDTestAttributeApplicationLib, "SymbolsTests.Metadata.MDTestAttributeApplicationLib.dll");

        private static byte[] s_MDTestAttributeDefLib;
        public static byte[] MDTestAttributeDefLib => ResourceLoader.GetOrCreateResource(ref s_MDTestAttributeDefLib, "SymbolsTests.Metadata.MDTestAttributeDefLib.dll");

        private static byte[] s_mscorlibNamespacesAndTypes;
        public static byte[] MscorlibNamespacesAndTypes => ResourceLoader.GetOrCreateResource(ref s_mscorlibNamespacesAndTypes, "SymbolsTests.Metadata.MscorlibNamespacesAndTypes.bsl");

        private static byte[] s_publicAndPrivateFlags;
        public static byte[] PublicAndPrivateFlags => ResourceLoader.GetOrCreateResource(ref s_publicAndPrivateFlags, "SymbolsTests.Metadata.public-and-private.dll");
    }

    public static class Methods
    {
        private static byte[] s_byRefReturn;
        public static byte[] ByRefReturn => ResourceLoader.GetOrCreateResource(ref s_byRefReturn, "SymbolsTests.Methods.ByRefReturn.dll");

        private static byte[] s_CSMethods;
        public static byte[] CSMethods => ResourceLoader.GetOrCreateResource(ref s_CSMethods, "SymbolsTests.Methods.CSMethods.dll");

        private static byte[] s_ILMethods;
        public static byte[] ILMethods => ResourceLoader.GetOrCreateResource(ref s_ILMethods, "SymbolsTests.Methods.ILMethods.dll");

        private static byte[] s_VBMethods;
        public static byte[] VBMethods => ResourceLoader.GetOrCreateResource(ref s_VBMethods, "SymbolsTests.Methods.VBMethods.dll");
    }

    public static class MissingTypes
    {
        private static byte[] s_CL2;
        public static byte[] CL2 => ResourceLoader.GetOrCreateResource(ref s_CL2, "SymbolsTests.MissingTypes.CL2.dll");

        private static byte[] s_CL3;
        public static byte[] CL3 => ResourceLoader.GetOrCreateResource(ref s_CL3, "SymbolsTests.MissingTypes.CL3.dll");

        private static string s_CL3_VB;
        public static string CL3_VB => ResourceLoader.GetOrCreateResource(ref s_CL3_VB, "SymbolsTests.MissingTypes.CL3.vb");

        private static byte[] s_MDMissingType;
        public static byte[] MDMissingType => ResourceLoader.GetOrCreateResource(ref s_MDMissingType, "SymbolsTests.MissingTypes.MDMissingType.dll");

        private static byte[] s_MDMissingTypeLib;
        public static byte[] MDMissingTypeLib => ResourceLoader.GetOrCreateResource(ref s_MDMissingTypeLib, "SymbolsTests.MissingTypes.MDMissingTypeLib.dll");

        private static byte[] s_MDMissingTypeLib_New;
        public static byte[] MDMissingTypeLib_New => ResourceLoader.GetOrCreateResource(ref s_MDMissingTypeLib_New, "SymbolsTests.MissingTypes.MDMissingTypeLib_New.dll");

        private static byte[] s_missingTypesEquality1;
        public static byte[] MissingTypesEquality1 => ResourceLoader.GetOrCreateResource(ref s_missingTypesEquality1, "SymbolsTests.MissingTypes.MissingTypesEquality1.dll");

        private static byte[] s_missingTypesEquality2;
        public static byte[] MissingTypesEquality2 => ResourceLoader.GetOrCreateResource(ref s_missingTypesEquality2, "SymbolsTests.MissingTypes.MissingTypesEquality2.dll");
    }

    public static class MultiModule
    {
        private static byte[] s_consumer;
        public static byte[] Consumer => ResourceLoader.GetOrCreateResource(ref s_consumer, "SymbolsTests.MultiModule.Consumer.dll");

        private static byte[] s_mod2;
        public static byte[] mod2 => ResourceLoader.GetOrCreateResource(ref s_mod2, "SymbolsTests.MultiModule.mod2.netmodule");

        private static byte[] s_mod3;
        public static byte[] mod3 => ResourceLoader.GetOrCreateResource(ref s_mod3, "SymbolsTests.MultiModule.mod3.netmodule");

        private static byte[] s_multiModuleDll;
        public static byte[] MultiModuleDll => ResourceLoader.GetOrCreateResource(ref s_multiModuleDll, "SymbolsTests.MultiModule.MultiModule.dll");
    }

    public static class MultiTargeting
    {
        private static byte[] s_source1Module;
        public static byte[] Source1Module => ResourceLoader.GetOrCreateResource(ref s_source1Module, "SymbolsTests.MultiTargeting.Source1Module.netmodule");

        private static byte[] s_source3Module;
        public static byte[] Source3Module => ResourceLoader.GetOrCreateResource(ref s_source3Module, "SymbolsTests.MultiTargeting.Source3Module.netmodule");

        private static byte[] s_source4Module;
        public static byte[] Source4Module => ResourceLoader.GetOrCreateResource(ref s_source4Module, "SymbolsTests.MultiTargeting.Source4Module.netmodule");

        private static byte[] s_source5Module;
        public static byte[] Source5Module => ResourceLoader.GetOrCreateResource(ref s_source5Module, "SymbolsTests.MultiTargeting.Source5Module.netmodule");

        private static byte[] s_source7Module;
        public static byte[] Source7Module => ResourceLoader.GetOrCreateResource(ref s_source7Module, "SymbolsTests.MultiTargeting.Source7Module.netmodule");
    }

    public static class netModule
    {
        private static byte[] s_crossRefLib;
        public static byte[] CrossRefLib => ResourceLoader.GetOrCreateResource(ref s_crossRefLib, "SymbolsTests.netModule.CrossRefLib.dll");

        private static byte[] s_crossRefModule1;
        public static byte[] CrossRefModule1 => ResourceLoader.GetOrCreateResource(ref s_crossRefModule1, "SymbolsTests.netModule.CrossRefModule1.netmodule");

        private static byte[] s_crossRefModule2;
        public static byte[] CrossRefModule2 => ResourceLoader.GetOrCreateResource(ref s_crossRefModule2, "SymbolsTests.netModule.CrossRefModule2.netmodule");

        private static byte[] s_hash_module;
        public static byte[] hash_module => ResourceLoader.GetOrCreateResource(ref s_hash_module, "SymbolsTests.netModule.hash_module.netmodule");

        private static byte[] s_netModule1;
        public static byte[] netModule1 => ResourceLoader.GetOrCreateResource(ref s_netModule1, "SymbolsTests.netModule.netModule1.netmodule");

        private static byte[] s_netModule2;
        public static byte[] netModule2 => ResourceLoader.GetOrCreateResource(ref s_netModule2, "SymbolsTests.netModule.netModule2.netmodule");

        private static byte[] s_x64COFF;
        public static byte[] x64COFF => ResourceLoader.GetOrCreateResource(ref s_x64COFF, "SymbolsTests.netModule.x64COFF.obj");
    }

    public static class NoPia
    {
        private static byte[] s_A;
        public static byte[] A => ResourceLoader.GetOrCreateResource(ref s_A, "SymbolsTests.NoPia.A.dll");

        private static byte[] s_B;
        public static byte[] B => ResourceLoader.GetOrCreateResource(ref s_B, "SymbolsTests.NoPia.B.dll");

        private static byte[] s_C;
        public static byte[] C => ResourceLoader.GetOrCreateResource(ref s_C, "SymbolsTests.NoPia.C.dll");

        private static byte[] s_D;
        public static byte[] D => ResourceLoader.GetOrCreateResource(ref s_D, "SymbolsTests.NoPia.D.dll");

        private static byte[] s_externalAsm1;
        public static byte[] ExternalAsm1 => ResourceLoader.GetOrCreateResource(ref s_externalAsm1, "SymbolsTests.NoPia.ExternalAsm1.dll");

        private static byte[] s_generalPia;
        public static byte[] GeneralPia => ResourceLoader.GetOrCreateResource(ref s_generalPia, "SymbolsTests.NoPia.GeneralPia.dll");

        private static byte[] s_generalPiaCopy;
        public static byte[] GeneralPiaCopy => ResourceLoader.GetOrCreateResource(ref s_generalPiaCopy, "SymbolsTests.NoPia.GeneralPiaCopy.dll");

        private static byte[] s_library1;
        public static byte[] Library1 => ResourceLoader.GetOrCreateResource(ref s_library1, "SymbolsTests.NoPia.Library1.dll");

        private static byte[] s_library2;
        public static byte[] Library2 => ResourceLoader.GetOrCreateResource(ref s_library2, "SymbolsTests.NoPia.Library2.dll");

        private static byte[] s_localTypes1;
        public static byte[] LocalTypes1 => ResourceLoader.GetOrCreateResource(ref s_localTypes1, "SymbolsTests.NoPia.LocalTypes1.dll");

        private static byte[] s_localTypes2;
        public static byte[] LocalTypes2 => ResourceLoader.GetOrCreateResource(ref s_localTypes2, "SymbolsTests.NoPia.LocalTypes2.dll");

        private static byte[] s_localTypes3;
        public static byte[] LocalTypes3 => ResourceLoader.GetOrCreateResource(ref s_localTypes3, "SymbolsTests.NoPia.LocalTypes3.dll");

        private static byte[] s_missingPIAAttributes;
        public static byte[] MissingPIAAttributes => ResourceLoader.GetOrCreateResource(ref s_missingPIAAttributes, "SymbolsTests.NoPia.MissingPIAAttributes.dll");

        private static byte[] s_noPIAGenerics1_Asm1;
        public static byte[] NoPIAGenerics1_Asm1 => ResourceLoader.GetOrCreateResource(ref s_noPIAGenerics1_Asm1, "SymbolsTests.NoPia.NoPIAGenerics1-Asm1.dll");

        private static byte[] s_pia1;
        public static byte[] Pia1 => ResourceLoader.GetOrCreateResource(ref s_pia1, "SymbolsTests.NoPia.Pia1.dll");

        private static byte[] s_pia1Copy;
        public static byte[] Pia1Copy => ResourceLoader.GetOrCreateResource(ref s_pia1Copy, "SymbolsTests.NoPia.Pia1Copy.dll");

        private static byte[] s_pia2;
        public static byte[] Pia2 => ResourceLoader.GetOrCreateResource(ref s_pia2, "SymbolsTests.NoPia.Pia2.dll");

        private static byte[] s_pia3;
        public static byte[] Pia3 => ResourceLoader.GetOrCreateResource(ref s_pia3, "SymbolsTests.NoPia.Pia3.dll");

        private static byte[] s_pia4;
        public static byte[] Pia4 => ResourceLoader.GetOrCreateResource(ref s_pia4, "SymbolsTests.NoPia.Pia4.dll");

        private static byte[] s_pia5;
        public static byte[] Pia5 => ResourceLoader.GetOrCreateResource(ref s_pia5, "SymbolsTests.NoPia.Pia5.dll");

        private static byte[] s_parametersWithoutNames;
        public static byte[] ParametersWithoutNames => ResourceLoader.GetOrCreateResource(ref s_parametersWithoutNames, "SymbolsTests.NoPia.ParametersWithoutNames.dll");
    }
}

namespace TestResources.SymbolsTests.RetargetingCycle
{
    public static class RetV1
    {
        private static byte[] s_classA;
        public static byte[] ClassA => ResourceLoader.GetOrCreateResource(ref s_classA, "SymbolsTests.RetargetingCycle.V1.ClassA.dll");

        private static byte[] s_classB;
        public static byte[] ClassB => ResourceLoader.GetOrCreateResource(ref s_classB, "SymbolsTests.RetargetingCycle.V1.ClassB.netmodule");
    }

    public static class RetV2
    {
        private static byte[] s_classA;
        public static byte[] ClassA => ResourceLoader.GetOrCreateResource(ref s_classA, "SymbolsTests.RetargetingCycle.V2.ClassA.dll");

        private static byte[] s_classB;
        public static byte[] ClassB => ResourceLoader.GetOrCreateResource(ref s_classB, "SymbolsTests.RetargetingCycle.V2.ClassB.dll");
    }
}

namespace TestResources.SymbolsTests
{
    public static class TypeForwarders
    {
        private static byte[] s_forwarded;
        public static byte[] Forwarded => ResourceLoader.GetOrCreateResource(ref s_forwarded, "SymbolsTests.TypeForwarders.Forwarded.netmodule");

        private static byte[] s_typeForwarder;
        public static byte[] TypeForwarder => ResourceLoader.GetOrCreateResource(ref s_typeForwarder, "SymbolsTests.TypeForwarders.TypeForwarder.dll");

        private static byte[] s_typeForwarderBase;
        public static byte[] TypeForwarderBase => ResourceLoader.GetOrCreateResource(ref s_typeForwarderBase, "SymbolsTests.TypeForwarders.TypeForwarderBase.dll");

        private static byte[] s_typeForwarderLib;
        public static byte[] TypeForwarderLib => ResourceLoader.GetOrCreateResource(ref s_typeForwarderLib, "SymbolsTests.TypeForwarders.TypeForwarderLib.dll");
    }

    public static class V1
    {
        private static byte[] s_MTTestLib1;
        public static byte[] MTTestLib1 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib1, "SymbolsTests.V1.MTTestLib1.Dll");

        private static string s_MTTestLib1_V1;
        public static string MTTestLib1_V1 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib1_V1, "SymbolsTests.V1.MTTestLib1_V1.vb");

        private static byte[] s_MTTestLib2;
        public static byte[] MTTestLib2 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib2, "SymbolsTests.V1.MTTestLib2.Dll");

        private static string s_MTTestLib2_V1;
        public static string MTTestLib2_V1 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib2_V1, "SymbolsTests.V1.MTTestLib2_V1.vb");

        private static byte[] s_MTTestModule1;
        public static byte[] MTTestModule1 => ResourceLoader.GetOrCreateResource(ref s_MTTestModule1, "SymbolsTests.V1.MTTestModule1.netmodule");

        private static byte[] s_MTTestModule2;
        public static byte[] MTTestModule2 => ResourceLoader.GetOrCreateResource(ref s_MTTestModule2, "SymbolsTests.V1.MTTestModule2.netmodule");
    }

    public static class V2
    {
        private static byte[] s_MTTestLib1;
        public static byte[] MTTestLib1 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib1, "SymbolsTests.V2.MTTestLib1.Dll");

        private static string s_MTTestLib1_V2;
        public static string MTTestLib1_V2 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib1_V2, "SymbolsTests.V2.MTTestLib1_V2.vb");

        private static byte[] s_MTTestLib3;
        public static byte[] MTTestLib3 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib3, "SymbolsTests.V2.MTTestLib3.Dll");

        private static string s_MTTestLib3_V2;
        public static string MTTestLib3_V2 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib3_V2, "SymbolsTests.V2.MTTestLib3_V2.vb");

        private static byte[] s_MTTestModule1;
        public static byte[] MTTestModule1 => ResourceLoader.GetOrCreateResource(ref s_MTTestModule1, "SymbolsTests.V2.MTTestModule1.netmodule");

        private static byte[] s_MTTestModule3;
        public static byte[] MTTestModule3 => ResourceLoader.GetOrCreateResource(ref s_MTTestModule3, "SymbolsTests.V2.MTTestModule3.netmodule");
    }

    public static class V3
    {
        private static byte[] s_MTTestLib1;
        public static byte[] MTTestLib1 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib1, "SymbolsTests.V3.MTTestLib1.Dll");

        private static string s_MTTestLib1_V3;
        public static string MTTestLib1_V3 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib1_V3, "SymbolsTests.V3.MTTestLib1_V3.vb");

        private static byte[] s_MTTestLib4;
        public static byte[] MTTestLib4 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib4, "SymbolsTests.V3.MTTestLib4.Dll");

        private static string s_MTTestLib4_V3;
        public static string MTTestLib4_V3 => ResourceLoader.GetOrCreateResource(ref s_MTTestLib4_V3, "SymbolsTests.V3.MTTestLib4_V3.vb");

        private static byte[] s_MTTestModule1;
        public static byte[] MTTestModule1 => ResourceLoader.GetOrCreateResource(ref s_MTTestModule1, "SymbolsTests.V3.MTTestModule1.netmodule");

        private static byte[] s_MTTestModule4;
        public static byte[] MTTestModule4 => ResourceLoader.GetOrCreateResource(ref s_MTTestModule4, "SymbolsTests.V3.MTTestModule4.netmodule");
    }

    public static class WithEvents
    {
        private static byte[] s_simpleWithEvents;
        public static byte[] SimpleWithEvents => ResourceLoader.GetOrCreateResource(ref s_simpleWithEvents, "SymbolsTests.WithEvents.SimpleWithEvents.dll");
    }
}

namespace TestResources
{
    public static class WinRt
    {
        private static byte[] s_W1;
        public static byte[] W1 => ResourceLoader.GetOrCreateResource(ref s_W1, "WinRt.W1.winmd");

        private static byte[] s_W2;
        public static byte[] W2 => ResourceLoader.GetOrCreateResource(ref s_W2, "WinRt.W2.winmd");

        private static byte[] s_WB;
        public static byte[] WB => ResourceLoader.GetOrCreateResource(ref s_WB, "WinRt.WB.winmd");

        private static byte[] s_WB_Version1;
        public static byte[] WB_Version1 => ResourceLoader.GetOrCreateResource(ref s_WB_Version1, "WinRt.WB_Version1.winmd");

        private static byte[] s_WImpl;
        public static byte[] WImpl => ResourceLoader.GetOrCreateResource(ref s_WImpl, "WinRt.WImpl.winmd");

        private static byte[] s_windows_dump;
        public static byte[] Windows_dump => ResourceLoader.GetOrCreateResource(ref s_windows_dump, "WinRt.Windows.ildump");

        private static byte[] s_windows_Languages_WinRTTest;
        public static byte[] Windows_Languages_WinRTTest => ResourceLoader.GetOrCreateResource(ref s_windows_Languages_WinRTTest, "WinRt.Windows.Languages.WinRTTest.winmd");

        private static byte[] s_windows;
        public static byte[] Windows => ResourceLoader.GetOrCreateResource(ref s_windows, "WinRt.Windows.winmd");

        private static byte[] s_winMDPrefixing_dump;
        public static byte[] WinMDPrefixing_dump => ResourceLoader.GetOrCreateResource(ref s_winMDPrefixing_dump, "WinRt.WinMDPrefixing.ildump");

        private static byte[] s_winMDPrefixing;
        public static byte[] WinMDPrefixing => ResourceLoader.GetOrCreateResource(ref s_winMDPrefixing, "WinRt.WinMDPrefixing.winmd");
    }
}
