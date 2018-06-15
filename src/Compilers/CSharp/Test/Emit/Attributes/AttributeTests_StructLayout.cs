﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class AttributeTests_StructLayout : CSharpTestBase
    {
        [Fact]
        public void Pack()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 0  )] class Pack0   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 1  )] class Pack1   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 2  )] class Pack2   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 4  )] class Pack4   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 8  )] class Pack8   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 16 )] class Pack16  {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 32 )] class Pack32  {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 64 )] class Pack64  {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 128)] class Pack128 {}
";
            const TypeAttributes typeDefMask = TypeAttributes.StringFormatMask | TypeAttributes.LayoutMask;

            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                Assert.Equal(9, metadataReader.GetTableRowCount(TableIndex.ClassLayout));

                foreach (var typeHandle in metadataReader.TypeDefinitions)
                {
                    var type = metadataReader.GetTypeDefinition(typeHandle);

                    var layout = type.GetLayout();
                    if (!layout.IsDefault)
                    {
                        Assert.Equal(TypeAttributes.SequentialLayout, type.Attributes & typeDefMask);
                        string typeName = metadataReader.GetString(type.Name);

                        int expectedAlignment = int.Parse(typeName.Substring("Pack".Length));
                        Assert.Equal(expectedAlignment, layout.PackingSize);
                        Assert.Equal(1, layout.Size);
                    }
                }
            });
        }

        [Fact]
        public void SizeAndPack()
        {
            var verifiable = @" 
using System;
using System.Runtime.InteropServices;

class Classes
{
	[StructLayout(LayoutKind.Explicit)] class E {}
	[StructLayout(LayoutKind.Explicit, Size = 0)] class E_S0 {}
	[StructLayout(LayoutKind.Explicit, Size = 1)] class E_S1 {}
	[StructLayout(LayoutKind.Explicit, Pack = 0)] class E_P0 {}
	[StructLayout(LayoutKind.Explicit, Pack = 1)] class E_P1 {}
	[StructLayout(LayoutKind.Explicit, Pack = 0, Size = 0)] class E_P0_S0 {}
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 10)] class E_P1_S10 {}
	
	[StructLayout(LayoutKind.Sequential)] class Q {}
	[StructLayout(LayoutKind.Sequential, Size = 0)] class Q_S0 {}
	[StructLayout(LayoutKind.Sequential, Size = 1)] class Q_S1 {}
	[StructLayout(LayoutKind.Sequential, Pack = 0)] class Q_P0 {}
	[StructLayout(LayoutKind.Sequential, Pack = 1)] class Q_P1 {}
	[StructLayout(LayoutKind.Sequential, Pack = 0, Size = 0)] class Q_P0_S0 {}
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)] class Q_P1_S10 {}
	
	[StructLayout(LayoutKind.Auto)] class A {}
}

class Structs
{
	[StructLayout(LayoutKind.Explicit)] struct E { [FieldOffset(0)]int a; }
	[StructLayout(LayoutKind.Explicit, Size = 0)] struct E_S0 { [FieldOffset(0)]int a; }
	[StructLayout(LayoutKind.Explicit, Size = 1)] struct E_S1 { [FieldOffset(0)]int a; }
	[StructLayout(LayoutKind.Explicit, Pack = 0)] struct E_P0 { [FieldOffset(0)]int a; }
	[StructLayout(LayoutKind.Explicit, Pack = 1)] struct E_P1 { [FieldOffset(0)]int a; }
	[StructLayout(LayoutKind.Explicit, Pack = 0, Size = 0)] struct E_P0_S0 { [FieldOffset(0)]int a; }
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 10)] struct E_P1_S10 { [FieldOffset(0)]int a; }
	
	[StructLayout(LayoutKind.Sequential)] struct Q { int a; }
	[StructLayout(LayoutKind.Sequential, Size = 0)] struct Q_S0 { int a; }
	[StructLayout(LayoutKind.Sequential, Size = 1)] struct Q_S1 { int a; }
	[StructLayout(LayoutKind.Sequential, Pack = 0)] struct Q_P0 { int a; }
	[StructLayout(LayoutKind.Sequential, Pack = 1)] struct Q_P1 { int a; }
	[StructLayout(LayoutKind.Sequential, Pack = 0, Size = 0)] struct Q_P0_S0 { int a; }
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = 10)] struct Q_P1_S10 { int a; }
	
	[StructLayout(LayoutKind.Auto)] struct A { int a; }
}";

            // peverify reports errors, but the types can be loaded and used:
            var unverifiable = @" 
using System;
using System.Runtime.InteropServices;

class Classes
{
    [StructLayout(LayoutKind.Auto, Size = 0)] class A_S0 {}
    [StructLayout(LayoutKind.Auto, Size = 1)] class A_S1 {}
    [StructLayout(LayoutKind.Auto, Pack = 0)] class A_P0 {}
    [StructLayout(LayoutKind.Auto, Pack = 1)] class A_P1 {}
    [StructLayout(LayoutKind.Auto, Pack = 0, Size = 0)] class A_P0_S0 {}
    [StructLayout(LayoutKind.Auto, Pack = 1, Size = 10)] class A_P1_S10 {}
}

class Structs
{
    [StructLayout(LayoutKind.Auto, Size = 0)] struct A_S0 {}
    [StructLayout(LayoutKind.Auto, Size = 1)] struct A_S1 {}
    [StructLayout(LayoutKind.Auto, Pack = 0)] struct A_P0 {}
    [StructLayout(LayoutKind.Auto, Pack = 1)] struct A_P1 {}
    [StructLayout(LayoutKind.Auto, Pack = 0, Size = 0)] struct A_P0_S0 {}
    [StructLayout(LayoutKind.Auto, Pack = 1, Size = 10)] struct A_P1_S10 {}
}
";
            // types can't be loaded as they are too big:
            var unloadable = @"
using System;
using System.Runtime.InteropServices;

class Classes
{
    [StructLayout(LayoutKind.Auto, Pack = 1, Size = Int32.MaxValue)] class A_P1_S2147483647 {}
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = Int32.MaxValue)] class Q_P1_S2147483647 {}
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = Int32.MaxValue)] class E_P1_S2147483647 {}
}

class Structs
{
    [StructLayout(LayoutKind.Auto, Pack = 1, Size = Int32.MaxValue)] struct A_P1_S2147483647 {}
	[StructLayout(LayoutKind.Sequential, Pack = 1, Size = Int32.MaxValue)] struct Q_P1_S2147483647 {}
	[StructLayout(LayoutKind.Explicit, Pack = 1, Size = Int32.MaxValue)] struct E_P1_S2147483647 {}
}
";
            Action<PEAssembly> validator = (assembly) =>
            {
                var metadataReader = assembly.GetMetadataReader();

                foreach (var typeHandle in metadataReader.TypeDefinitions)
                {
                    var type = metadataReader.GetTypeDefinition(typeHandle);

                    var layout = type.GetLayout();
                    if (layout.IsDefault)
                    {
                        continue;
                    }

                    string typeName = metadataReader.GetString(type.Name);
                    bool isValueType = (type.Attributes & TypeAttributes.Sealed) != 0;

                    uint expectedSize = 0;
                    ushort expectedPack = 0;
                    TypeAttributes expectedKind = TypeAttributes.AutoLayout;

                    if (typeName != "Structs" && typeName != "Classes")
                    {
                        foreach (var part in typeName.Split('_'))
                        {
                            switch (part[0])
                            {
                                case 'A':
                                    expectedKind = TypeAttributes.AutoLayout;
                                    break;

                                case 'E':
                                    expectedKind = TypeAttributes.ExplicitLayout;
                                    break;

                                case 'Q':
                                    expectedKind = TypeAttributes.SequentialLayout;
                                    break;

                                case 'P':
                                    expectedPack = ushort.Parse(part.Substring(1));
                                    break;

                                case 'S':
                                    expectedSize = uint.Parse(part.Substring(1));
                                    break;
                            }
                        }
                    }

                    // unlike Dev10, we don't add ClassLayout if .pack == 0 & .size == 0
                    Assert.False(expectedPack == 0 && expectedSize == 0, "Either expectedPack or expectedSize should be non-zero");

                    Assert.Equal(expectedPack, layout.PackingSize);
                    Assert.Equal(expectedSize, (uint)layout.Size);
                    Assert.Equal(expectedKind, type.Attributes & TypeAttributes.LayoutMask);
                }
            };

            CompileAndVerify(verifiable, assemblyValidator: validator);
            CompileAndVerify(unverifiable, assemblyValidator: validator, verify: Verification.Fails);

            // CLR limitation on type size, not a RefEmit bug:
            CompileAndVerify(unloadable, assemblyValidator: validator, verify: Verification.Fails);
        }

        [Fact]
        public void Pack_Errors()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = 1, Pack = -1  )] class PM1   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 3  )] class P3   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 5  )] class P5   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 6  )] class P6   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 256  )] class P256   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = 512  )] class P512   {}
[StructLayout(LayoutKind.Sequential, Size = 1, Pack = Int32.MaxValue  )] class PMax   {}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,48): error CS0599: Invalid value for named attribute argument 'Pack'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Pack = -1").WithArguments("Pack"),
                // (6,48): error CS0599: Invalid value for named attribute argument 'Pack'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Pack = 3").WithArguments("Pack"),
                // (7,48): error CS0599: Invalid value for named attribute argument 'Pack'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Pack = 5").WithArguments("Pack"),
                // (8,48): error CS0599: Invalid value for named attribute argument 'Pack'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Pack = 6").WithArguments("Pack"),
                // (9,48): error CS0599: Invalid value for named attribute argument 'Pack'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Pack = 256").WithArguments("Pack"),
                // (10,48): error CS0599: Invalid value for named attribute argument 'Pack'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Pack = 512").WithArguments("Pack"),
                // (11,48): error CS0599: Invalid value for named attribute argument 'Pack'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Pack = Int32.MaxValue").WithArguments("Pack"));
        }

        [Fact]
        public void Size_Errors()
        {
            var source = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Size = -1)] class S {}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (4,38): error CS0599: Invalid value for named attribute argument 'Size'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "Size = -1").WithArguments("Size"));
        }

        [Fact]
        public void LayoutAndCharSet_Errors()
        {
            var source = @"
using System;
using System.Runtime.InteropServices;

[StructLayout((LayoutKind)(-1), CharSet = CharSet.Ansi)]
public class C1 { }

[StructLayout((LayoutKind)4, CharSet = CharSet.Ansi)]
public class C2 { }

[StructLayout(LayoutKind.Sequential, CharSet = (CharSet)(-1))]
public class C3 { }

[StructLayout(LayoutKind.Sequential, CharSet = (CharSet)5)]
public class C4 { }

[StructLayout(LayoutKind.Sequential, CharSet = (CharSet)Int32.MaxValue)]
public class C5 { }
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,15): error CS0591: Invalid value for argument to 'StructLayout' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(LayoutKind)(-1)").WithArguments("StructLayout"),
                // (8,15): error CS0591: Invalid value for argument to 'StructLayout' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "(LayoutKind)4").WithArguments("StructLayout"),
                // (11,38): error CS0599: Invalid value for named attribute argument 'CharSet'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "CharSet = (CharSet)(-1)").WithArguments("CharSet"),
                // (14,38): error CS0599: Invalid value for named attribute argument 'CharSet'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "CharSet = (CharSet)5").WithArguments("CharSet"),
                // (17,38): error CS0599: Invalid value for named attribute argument 'CharSet'
                Diagnostic(ErrorCode.ERR_InvalidNamedArgument, "CharSet = (CharSet)Int32.MaxValue").WithArguments("CharSet"));
        }

        /// <summary>
        /// CLI spec (22.8 ClassLayout):
        ///  "A type has layout if it is marked SequentialLayout or ExplicitLayout. 
        ///   If any type within an inheritance chain has layout, then so shall all its base classes, 
        ///   up to the one that descends immediately from System.ValueType (if it exists in the type's hierarchy); 
        ///   otherwise, from System.Object."
        ///   
        /// But this rule is only enforced by the loader, not by the compiler.
        /// TODO: should we report an error?
        /// </summary>
        [Fact]
        public void Inheritance()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public class A
{
    public int a, b;
}

public class B : A
{
    public int c, d;
}

[StructLayout(LayoutKind.Sequential)]
public class C : B
{
    public int e, f;
}
";
            // type C can't be loaded
            CompileAndVerify(source, verify: Verification.Fails);
        }

        [Fact]
        [WorkItem(22512, "https://github.com/dotnet/roslyn/issues/22512")]
        public void ExplicitFieldLayout()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public class A
{
    [FieldOffset(4)]
    int a;

    [field: FieldOffset(8)]
    event Action b;
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var reader = assembly.GetMetadataReader();
                Assert.Equal(2, reader.GetTableRowCount(TableIndex.FieldLayout));

                foreach (var fieldHandle in reader.FieldDefinitions)
                {
                    var field = reader.GetFieldDefinition(fieldHandle);
                    string name = reader.GetString(field.Name);

                    int expectedOffset;
                    switch (name)
                    {
                        case "a":
                            expectedOffset = 4;
                            break;

                        case "b":
                            expectedOffset = 8;
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    Assert.Equal(expectedOffset, field.GetOffset());
                }
            });
        }

        [Fact]
        [WorkItem(22512, "https://github.com/dotnet/roslyn/issues/22512")]
        public void ExplicitFieldLayout_OnBackingField()
        {
            string source = @"
using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Explicit)]
public struct A
{
    [field: FieldOffset(4)]
    int a { get; set; }

    [field: FieldOffset(8)]
    event Action b;
}
";
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var reader = assembly.GetMetadataReader();
                Assert.Equal(2, reader.GetTableRowCount(TableIndex.FieldLayout));

                foreach (var fieldHandle in reader.FieldDefinitions)
                {
                    var field = reader.GetFieldDefinition(fieldHandle);
                    string name = reader.GetString(field.Name);

                    int expectedOffset;
                    switch (name)
                    {
                        case "<a>k__BackingField":
                            expectedOffset = 4;
                            break;

                        case "b":
                            expectedOffset = 8;
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }

                    Assert.Equal(expectedOffset, field.GetOffset());
                }
            });
        }

        /// <summary>
        /// CLI spec (22.16 FieldLayout):
        ///  - Offset shall be zero or more.
        ///  - The Type whose Fields are described by each row of the FieldLayout table shall have Flags.ExplicitLayout.
        ///  - Flags.Static for the row in the Field table indexed by Field shall be non-static
        ///  - Every Field of an ExplicitLayout Type shall be given an offset; that is, it shall have a row in the FieldLayout table
        /// </summary>
        [Fact]
        public void ExplicitFieldLayout_Errors()
        {
            string source = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Auto)]
public class A
{
    [FieldOffset(4)]
    int a;
}

[StructLayout(LayoutKind.Sequential)]
public class S
{
    [FieldOffset(4)]
    int a;

    [FieldOffset(-1)]
    int b;
}

[StructLayout(LayoutKind.Explicit)]
public class E
{
    [FieldOffset(-1)]
    int a;

    [FieldOffset(5)]
    static int b;

    int c1, c2;

    static int d;

    const int e = 3;

    [FieldOffset(10)]
    object f;

    [FieldOffset(-1)]
    static int g;

    [FieldOffset(5)]
    const int h = 1;
}

enum En
{
    [FieldOffset(5)]
    A = 1
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (7,6): error CS0636: The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadStruct, "FieldOffset"),
                // (14,6): error CS0636: The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadStruct, "FieldOffset"),
                // (17,18): error CS0591: Invalid value for argument to 'FieldOffset' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "-1").WithArguments("FieldOffset"),
                // (17,6): error CS0636: The FieldOffset attribute can only be placed on members of types marked with the StructLayout(LayoutKind.Explicit)
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadStruct, "FieldOffset"),
                // (24,18): error CS0591: Invalid value for argument to 'FieldOffset' attribute
                Diagnostic(ErrorCode.ERR_InvalidAttributeArgument, "-1").WithArguments("FieldOffset"),
                // (27,6): error CS0637: The FieldOffset attribute is not allowed on static or const fields
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadField, "FieldOffset").WithArguments("FieldOffset"),
                // (30,9): error CS0625: 'E.c1': instance field types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
                Diagnostic(ErrorCode.ERR_MissingStructOffset, "c1").WithArguments("E.c1"),
                // (30,13): error CS0625: 'E.c2': instance field types marked with StructLayout(LayoutKind.Explicit) must have a FieldOffset attribute
                Diagnostic(ErrorCode.ERR_MissingStructOffset, "c2").WithArguments("E.c2"),
                // (39,6): error CS0637: The FieldOffset attribute is not allowed on static or const fields
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadField, "FieldOffset").WithArguments("FieldOffset"),
                // (42,6): error CS0637: The FieldOffset attribute is not allowed on static or const fields
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadField, "FieldOffset").WithArguments("FieldOffset"),
                // (48,6): error CS0637: The FieldOffset attribute is not allowed on static or const fields
                Diagnostic(ErrorCode.ERR_StructOffsetOnBadField, "FieldOffset").WithArguments("FieldOffset"));
        }

        [Fact, WorkItem(546660, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546660"), WorkItem(546662, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546662")]
        public void SequentialLayout_Partials()
        {
            string source = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]  // error
partial class C
{
    public int x;
}

partial class C
{
    public int y;
}

[StructLayout(LayoutKind.Sequential)]  // ok
partial class D
{
    public int x;
}

partial class D
{
    public static int y;
}

[StructLayout(LayoutKind.Sequential)]  // ok
partial class E
{
}

partial class E
{
    public int y;
}

[StructLayout(LayoutKind.Auto)]        // ok
partial struct S
{
    public int y;
}

partial struct S
{
    public int x;
}
";
            CreateCompilation(source).VerifyDiagnostics(
                // (5,15): warning CS0282: There is no defined ordering between fields in multiple declarations of partial struct 'C'. To specify an ordering, all instance fields must be in the same declaration.
                Diagnostic(ErrorCode.WRN_SequentialOnPartialClass, "C").WithArguments("C"));
        }

        [Fact, WorkItem(631467, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/631467")]
        public void SequentialLayout_Partials_02()
        {
            string source = @"
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
partial struct C
{
    public const int x = 2;
}

partial struct C
{
    public int y;
}";
            CreateCompilation(source).VerifyDiagnostics();
        }

        [Fact]
        public void ReadingFromMetadata()
        {
            // the image is missing a record in ClassLayout table
            using (var module = ModuleMetadata.CreateFromImage(TestResources.MetadataTests.Invalid.ClassLayout))
            {
                var reader = module.Module.GetMetadataReader();

                foreach (var typeHandle in reader.TypeDefinitions)
                {
                    var type = reader.GetTypeDefinition(typeHandle);
                    var name = reader.GetString(type.Name);

                    bool badLayout = false;
                    System.Reflection.Metadata.TypeLayout mdLayout;
                    try
                    {
                        mdLayout = type.GetLayout();
                    }
                    catch (BadImageFormatException)
                    {
                        badLayout = true;
                        mdLayout = default(System.Reflection.Metadata.TypeLayout);
                    }

                    bool hasClassLayout = !mdLayout.IsDefault;
                    TypeLayout layout = module.Module.GetTypeLayout(typeHandle);
                    switch (name)
                    {
                        case "<Module>":
                            Assert.False(hasClassLayout);
                            Assert.Equal(default(TypeLayout), layout);
                            Assert.False(badLayout);
                            break;

                        case "S1":
                        case "S2":
                            // invalid size/pack value
                            Assert.False(hasClassLayout);
                            Assert.True(badLayout);
                            break;

                        case "S3":
                            Assert.True(hasClassLayout);
                            Assert.Equal(1, mdLayout.Size);
                            Assert.Equal(2, mdLayout.PackingSize);
                            Assert.Equal(new TypeLayout(LayoutKind.Sequential, size: 1, alignment: 2), layout);
                            Assert.False(badLayout);
                            break;

                        case "S4":
                            Assert.True(hasClassLayout);
                            Assert.Equal(unchecked((int)0x12345678), mdLayout.Size);
                            Assert.Equal(0, mdLayout.PackingSize);
                            Assert.Equal(new TypeLayout(LayoutKind.Sequential, size: 0x12345678, alignment: 0), layout);
                            Assert.False(badLayout);
                            break;

                        case "S5":
                            // doesn't have layout
                            Assert.False(hasClassLayout);
                            Assert.Equal(new TypeLayout(LayoutKind.Sequential, size: 0, alignment: 0), layout);
                            Assert.False(badLayout);
                            break;

                        default:
                            throw TestExceptionUtilities.UnexpectedValue(name);
                    }
                }
            }
        }

        private void VerifyStructLayout(string source, bool hasInstanceFields)
        {
            CompileAndVerify(source, assemblyValidator: (assembly) =>
            {
                var reader = assembly.GetMetadataReader();
                var type = reader.TypeDefinitions
                    .Select(handle => reader.GetTypeDefinition(handle))
                    .Where(typeDef => reader.GetString(typeDef.Name) == "S")
                    .Single();

                var layout = type.GetLayout();
                if (!hasInstanceFields)
                {
                    const TypeAttributes typeDefMask = TypeAttributes.StringFormatMask | TypeAttributes.LayoutMask;

                    Assert.False(layout.IsDefault);
                    Assert.Equal(TypeAttributes.SequentialLayout, type.Attributes & typeDefMask);
                    Assert.Equal(0, layout.PackingSize);
                    Assert.Equal(1, layout.Size);
                }
                else
                {
                    Assert.True(layout.IsDefault);
                }
            });
        }

        [Fact]
        public void Bug1075326()
        {
            // no instance fields
            VerifyStructLayout(@"struct S {}", hasInstanceFields: false);
            VerifyStructLayout(@"struct S { static int f; }", hasInstanceFields: false);
            VerifyStructLayout(@"struct S { static int P { get; set; } }", hasInstanceFields: false);
            VerifyStructLayout(@"struct S { int P { set { } } }", hasInstanceFields: false);
            VerifyStructLayout(@"struct S { static int P { set { } } }", hasInstanceFields: false);
            VerifyStructLayout(@"delegate void D(); struct S { static event D D; }", hasInstanceFields: false);
            VerifyStructLayout(@"delegate void D(); struct S { event D D { add { } remove { } } }", hasInstanceFields: false);
            VerifyStructLayout(@"delegate void D(); struct S { static event D D { add { } remove { } } }", hasInstanceFields: false);

            // instance fields
            VerifyStructLayout(@"struct S { int f; }", hasInstanceFields: true);
            VerifyStructLayout(@"struct S { int P { get; set; } }", hasInstanceFields: true);
            VerifyStructLayout(@"delegate void D(); struct S { event D D; }", hasInstanceFields: true);
        }
    }
}
