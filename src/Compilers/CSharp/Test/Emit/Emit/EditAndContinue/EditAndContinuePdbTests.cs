﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.UnitTests;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.EditAndContinue.UnitTests
{
    public class EditAndContinuePdbTests : EditAndContinueTestBase
    {
        [Theory]
        [InlineData(DebugInformationFormat.Pdb)]
        [InlineData(DebugInformationFormat.PortablePdb)]
        public void MethodExtents(DebugInformationFormat format)
        {
            var source0 = MarkedSource(@"#pragma checksum ""C:\Enc1.cs"" ""{ff1816ec-aa5e-4d10-87f7-6f4963833460}"" ""1111111111111111111111111111111111111111""
using System;

public class C
{
    int A() => 1;                  
                                   
    void F()                       
    {                              
#line 10 ""C:\F\A.cs""               
        Console.WriteLine();
#line 20 ""C:\F\B.cs""
        Console.WriteLine();
#line default
    }

    int E() => 1;

    void G()
    {
        Func<int> H1 = <N:0>() => 1</N:0>;

        Action H2 = <N:1>() =>
        {
            Func<int> H3 = <N:2>() => 3</N:2>;

        }</N:1>;
    }
}                              
", fileName: @"C:\Enc1.cs");

            var source1 = MarkedSource(@"#pragma checksum ""C:\Enc1.cs"" ""{ff1816ec-aa5e-4d10-87f7-6f4963833460}"" ""2222222222222222222222222222222222222222""
using System;

public class C
{
    int A() => 1;                 
                              
    void F()                      
    {                             
#line 10 ""C:\F\A.cs""         
        Console.WriteLine();
#line 10 ""C:\F\C.cs""
        Console.WriteLine();
#line default
    }

    void G()
    {
        Func<int> H1 = <N:0>() => 1</N:0>;

        Action H2 = <N:1>() =>
        {
            Func<int> H3 = <N:2>() => 3</N:2>;
            Func<int> H4 = <N:3>() => 4</N:3>;
        }</N:1>;
    }

    int E() => 1;
}
", fileName: @"C:\Enc1.cs");

            var source2 = MarkedSource(@"#pragma checksum ""C:\Enc1.cs"" ""{ff1816ec-aa5e-4d10-87f7-6f4963833460}"" ""3333333333333333333333333333333333333333""
using System;

public class C
{
    int A() => 3;                 
                              
    void F()       
    {    
#line 10 ""C:\F\B.cs""
        Console.WriteLine();
#line 10 ""C:\F\E.cs""
        Console.WriteLine();
#line default
    }

    void G()
    {
        

        Action H2 = <N:1>() =>
        {
            
            Func<int> H4 = <N:3>() => 4</N:3>;
        }</N:1>;
    }

    int E() => 1;  

    int B() => 4;
}
", fileName: @"C:\Enc1.cs");

            var compilation0 = CreateStandardCompilation(source0.Tree, options: ComSafeDebugDll.WithMetadataImportOptions(MetadataImportOptions.All), assemblyName: "EncMethodExtents");
            var compilation1 = compilation0.WithSource(source1.Tree);
            var compilation2 = compilation1.WithSource(source2.Tree);

            compilation0.VerifyDiagnostics();
            compilation1.VerifyDiagnostics();
            compilation2.VerifyDiagnostics();

            var v0 = CompileAndVerify(compilation0, emitOptions: EmitOptions.Default.WithDebugInformationFormat(format));
            var md0 = ModuleMetadata.CreateFromImage(v0.EmittedAssemblyData);

            var f0 = compilation0.GetMember<MethodSymbol>("C.F");
            var f1 = compilation1.GetMember<MethodSymbol>("C.F");
            var f2 = compilation2.GetMember<MethodSymbol>("C.F");

            var g0 = compilation0.GetMember<MethodSymbol>("C.G");
            var g1 = compilation1.GetMember<MethodSymbol>("C.G");
            var g2 = compilation2.GetMember<MethodSymbol>("C.G");

            var a1 = compilation1.GetMember<MethodSymbol>("C.A");
            var a2 = compilation2.GetMember<MethodSymbol>("C.A");

            var b2 = compilation2.GetMember<MethodSymbol>("C.B");

            var generation0 = EmitBaseline.CreateInitialBaseline(md0, v0.CreateSymReader().GetEncMethodDebugInfo);

            var syntaxMap1 = GetSyntaxMapFromMarkers(source0, source1);
            var diff1 = compilation1.EmitDifference(
                generation0,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f0, f1, syntaxMap1, preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Update, g0, g1, syntaxMap1, preserveLocalVariables: true)));

            diff1.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__3_0, <>9__3_2, <>9__3_3#1, <>9__3_1, <G>b__3_0, <G>b__3_1, <G>b__3_2, <G>b__3_3#1}");

            var reader1 = diff1.GetMetadata().Reader;

            CheckEncLogDefinitions(reader1,
                Row(3, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(4, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddField),
                Row(5, TableIndex.Field, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(8, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(10, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(3, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default));

            if (format == DebugInformationFormat.PortablePdb)
            {
                using (var pdbProvider = MetadataReaderProvider.FromPortablePdbImage(diff1.PdbDelta))
                {
                    CheckEncMap(pdbProvider.GetMetadataReader(),
                        Handle(2, TableIndex.MethodDebugInformation),
                        Handle(4, TableIndex.MethodDebugInformation),
                        Handle(8, TableIndex.MethodDebugInformation),
                        Handle(9, TableIndex.MethodDebugInformation),
                        Handle(10, TableIndex.MethodDebugInformation),
                        Handle(11, TableIndex.MethodDebugInformation));
                }
            }

            diff1.VerifyPdb(Enumerable.Range(0x06000001, 20), @"
<symbols>
  <files>
    <file id=""1"" name=""C:\Enc1.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""D1, 84, C6,  3, AA, 83, C0, CF, 49, E2, 52, 48, BB, 35, 99, 51, 88, 2B, 6C, D6, "" />
    <file id=""2"" name=""C:\F\A.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""3"" name=""C:\F\C.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  </files>
  <methods>
    <method token=""0x6000002"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""29"" document=""2"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""29"" document=""3"" />
        <entry offset=""0xd"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0xe"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method token=""0x6000004"">
      <customDebugInfo>
        <forward token=""0x6000002"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""19"" startColumn=""9"" endLine=""19"" endColumn=""43"" document=""1"" />
        <entry offset=""0x21"" startLine=""21"" startColumn=""9"" endLine=""25"" endColumn=""17"" document=""1"" />
        <entry offset=""0x41"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x42"">
        <local name=""H1"" il_index=""2"" il_start=""0x0"" il_end=""0x42"" attributes=""0"" />
        <local name=""H2"" il_index=""3"" il_start=""0x0"" il_end=""0x42"" attributes=""0"" />
      </scope>
    </method>
    <method token=""0x6000008"">
      <customDebugInfo>
        <forward token=""0x6000002"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""19"" startColumn=""35"" endLine=""19"" endColumn=""36"" document=""1"" />
      </sequencePoints>
    </method>
    <method token=""0x6000009"">
      <customDebugInfo>
        <forward token=""0x6000002"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""23"" startColumn=""13"" endLine=""23"" endColumn=""47"" document=""1"" />
        <entry offset=""0x21"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""47"" document=""1"" />
        <entry offset=""0x41"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x42"">
        <local name=""H3"" il_index=""1"" il_start=""0x0"" il_end=""0x42"" attributes=""0"" />
        <local name=""H4"" il_index=""2"" il_start=""0x0"" il_end=""0x42"" attributes=""0"" />
      </scope>
    </method>
    <method token=""0x600000a"">
      <customDebugInfo>
        <forward token=""0x6000002"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""23"" startColumn=""39"" endLine=""23"" endColumn=""40"" document=""1"" />
      </sequencePoints>
    </method>
    <method token=""0x600000b"">
      <customDebugInfo>
        <forward token=""0x6000002"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""24"" startColumn=""39"" endLine=""24"" endColumn=""40"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
            var syntaxMap2 = GetSyntaxMapFromMarkers(source1, source2);
            var diff2 = compilation2.EmitDifference(
                diff1.NextGeneration,
                ImmutableArray.Create(
                    new SemanticEdit(SemanticEditKind.Update, f1, f2, syntaxMap2, preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Update, g1, g2, syntaxMap2, preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Update, a1, a2, syntaxMap2, preserveLocalVariables: true),
                    new SemanticEdit(SemanticEditKind.Insert, null, b2)));

            diff2.VerifySynthesizedMembers(
                "C: {<>c}",
                "C.<>c: {<>9__3_3#1, <>9__3_1, <G>b__3_1, <G>b__3_3#1, <>9__3_0, <>9__3_2, <G>b__3_0, <G>b__3_2}");

            var reader2 = diff2.GetMetadata().Reader;

            CheckEncLogDefinitions(reader2,
                Row(5, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(6, TableIndex.StandAloneSig, EditAndContinueOperation.Default),
                Row(1, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(4, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(9, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(11, TableIndex.MethodDef, EditAndContinueOperation.Default),
                Row(2, TableIndex.TypeDef, EditAndContinueOperation.AddMethod),
                Row(12, TableIndex.MethodDef, EditAndContinueOperation.Default));

            if (format == DebugInformationFormat.PortablePdb)
            {
                using (var pdbProvider = MetadataReaderProvider.FromPortablePdbImage(diff2.PdbDelta))
                {
                    CheckEncMap(pdbProvider.GetMetadataReader(),
                        Handle(1, TableIndex.MethodDebugInformation),
                        Handle(2, TableIndex.MethodDebugInformation),
                        Handle(4, TableIndex.MethodDebugInformation),
                        Handle(9, TableIndex.MethodDebugInformation),
                        Handle(11, TableIndex.MethodDebugInformation),
                        Handle(12, TableIndex.MethodDebugInformation));
                }
            }

            diff2.VerifyPdb(Enumerable.Range(0x06000001, 20), @"
<symbols>
  <files>
    <file id=""1"" name=""C:\Enc1.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" checkSumAlgorithmId=""ff1816ec-aa5e-4d10-87f7-6f4963833460"" checkSum=""4D, 14, 76, DE, 68, ED, ED, C1, 88, DF,  C, 85, 1C, 6B, 15, F7, BE, C0, 34, 34, "" />
    <file id=""2"" name=""C:\F\B.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
    <file id=""3"" name=""C:\F\E.cs"" language=""3f5162f8-07c6-11d3-9053-00c04fa302a1"" languageVendor=""994b45c4-e6e9-11d2-903f-00c04fa302a1"" documentType=""5a869d0b-6611-11d3-bd2a-0000f80849bd"" />
  </files>
  <methods>
    <method token=""0x6000001"">
      <customDebugInfo>
        <using>
          <namespace usingCount=""1"" />
        </using>
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""6"" startColumn=""16"" endLine=""6"" endColumn=""17"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x2"">
        <namespace name=""System"" />
      </scope>
    </method>
    <method token=""0x6000002"">
      <customDebugInfo>
        <forward token=""0x6000001"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""9"" startColumn=""5"" endLine=""9"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""29"" document=""2"" />
        <entry offset=""0x7"" startLine=""10"" startColumn=""9"" endLine=""10"" endColumn=""29"" document=""3"" />
        <entry offset=""0xd"" startLine=""15"" startColumn=""5"" endLine=""15"" endColumn=""6"" document=""1"" />
      </sequencePoints>
    </method>
    <method token=""0x6000004"">
      <customDebugInfo>
        <forward token=""0x6000001"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""18"" startColumn=""5"" endLine=""18"" endColumn=""6"" document=""1"" />
        <entry offset=""0x1"" startLine=""21"" startColumn=""9"" endLine=""25"" endColumn=""17"" document=""1"" />
        <entry offset=""0x22"" startLine=""26"" startColumn=""5"" endLine=""26"" endColumn=""6"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x23"">
        <local name=""H2"" il_index=""4"" il_start=""0x0"" il_end=""0x23"" attributes=""0"" />
      </scope>
    </method>
    <method token=""0x6000009"">
      <customDebugInfo>
        <forward token=""0x6000001"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""22"" startColumn=""9"" endLine=""22"" endColumn=""10"" document=""1"" />
        <entry offset=""0x1"" startLine=""24"" startColumn=""13"" endLine=""24"" endColumn=""47"" document=""1"" />
        <entry offset=""0x21"" startLine=""25"" startColumn=""9"" endLine=""25"" endColumn=""10"" document=""1"" />
      </sequencePoints>
      <scope startOffset=""0x0"" endOffset=""0x22"">
        <local name=""H4"" il_index=""1"" il_start=""0x0"" il_end=""0x22"" attributes=""0"" />
      </scope>
    </method>
    <method token=""0x600000b"">
      <customDebugInfo>
        <forward token=""0x6000001"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""24"" startColumn=""39"" endLine=""24"" endColumn=""40"" document=""1"" />
      </sequencePoints>
    </method>
    <method token=""0x600000c"">
      <customDebugInfo>
        <forward token=""0x6000001"" />
      </customDebugInfo>
      <sequencePoints>
        <entry offset=""0x0"" startLine=""30"" startColumn=""16"" endLine=""30"" endColumn=""17"" document=""1"" />
      </sequencePoints>
    </method>
  </methods>
</symbols>
");
        }
    }
}
