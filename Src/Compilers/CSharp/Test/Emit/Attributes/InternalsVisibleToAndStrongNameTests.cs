// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

public class InternalsVisibleToAndStrongNameTests : CSharpTestBase
{
    #region Helpers

    public InternalsVisibleToAndStrongNameTests()
    {
        SigningTestHelpers.InstallKey();
    }

    private static readonly string KeyPairFile = SigningTestHelpers.KeyPairFile;
    private static readonly string PublicKeyFile = SigningTestHelpers.PublicKeyFile;
    private static readonly ImmutableArray<byte> PublicKey = SigningTestHelpers.PublicKey;
    private static readonly DesktopStrongNameProvider DefaultProvider = new SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create<string>());

    private static DesktopStrongNameProvider GetProviderWithPath(string keyFilePath)
    {
        return new SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create(keyFilePath));
    }

    #endregion

    #region Naming Tests

    [Fact, WorkItem(529419, "DevDiv")]
    public void AssemblyKeyFileAttributeNotExistFile()
    {
        string source = @"
using System;
using System.Reflection;

[assembly: AssemblyKeyFile(""MyKey.snk"")]
[assembly: AssemblyKeyName(""Key Name"")]

public class Test
{
    public static void Main()
    {
        Console.Write(""Hello World!"");
    }
}
";
        // Dev11 RC gives error now (CS1548) + two warnings
        // Diagnostic(ErrorCode.WRN_UseSwitchInsteadOfAttribute).WithArguments(@"/keyfile", "AssemblyKeyFile"),
        // Diagnostic(ErrorCode.WRN_UseSwitchInsteadOfAttribute).WithArguments(@"/keycontainer", "AssemblyKeyName")
        var c = CreateCompilationWithMscorlib(source, 
            references: new[] { SystemRef },
            compOptions: TestOptions.Dll.WithStrongNameProvider(new DesktopStrongNameProvider()));

        c.VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments("MyKey.snk", "File not found."));
    }

    [Fact]
    public void PubKeyFromKeyFileAttribute()
    {
        var x = KeyPairFile;
        string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", x, @""")] public class C {}");

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        other.VerifyDiagnostics();
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey));

        CompileAndVerify(other, symbolValidator: (ModuleSymbol m) =>
            {
                bool haveAttribute = false;

                foreach (var attrData in m.ContainingAssembly.GetAttributes())
                {
                    if (attrData.IsTargetAttribute(m.ContainingAssembly, AttributeDescription.AssemblyKeyFileAttribute))
                    {
                        haveAttribute = true;
                        break;
                    }
                }

                Assert.True(haveAttribute);
            }, emitOptions: EmitOptions.CCI); 
    }

    [Fact]
    public void PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver()
    {
        string keyFileDir = Path.GetDirectoryName(KeyPairFile);
        string keyFileName = Path.GetFileName(KeyPairFile);

        string s = string.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", keyFileName, @""")] public class C {}");
        var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");
        
        // verify failure with default assembly key file resolver
        var comp = CreateCompilationWithMscorlib(syntaxTree, compOptions: TestOptions.Dll);
        comp.VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(keyFileName, "Assembly signing not supported."));

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

        // verify success with custom assembly key file resolver with keyFileDir added to search paths
        comp = CSharpCompilation.Create(
            GetUniqueName(),
            new[] { syntaxTree },
            new[] { MscorlibRef },
            TestOptions.Dll.WithStrongNameProvider(GetProviderWithPath(keyFileDir)));

        comp.VerifyDiagnostics();

        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey));
    }

    [Fact]
    public void PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver_RelativeToCurrentParent()
    {
        string keyFileDir = Path.GetDirectoryName(KeyPairFile);
        string keyFileName = Path.GetFileName(KeyPairFile);

        string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""..\", keyFileName, @""")] public class C {}");
        var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

        // verify failure with default assembly key file resolver
        var comp = CreateCompilationWithMscorlib(syntaxTree, compOptions: TestOptions.Dll);
        comp.VerifyDiagnostics(
            // error CS7027: Error extracting public key from file '..\KeyPairFile.snk' -- File not found.
            Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(@"..\" + keyFileName, "Assembly signing not supported."));

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

        // verify success with custom assembly key file resolver with keyFileDir\TempSubDir added to search paths
        comp = CSharpCompilation.Create(
            GetUniqueName(),
            new[] { syntaxTree },
            new[] { MscorlibRef },
            TestOptions.Dll.WithStrongNameProvider(GetProviderWithPath(PathUtilities.CombineAbsoluteAndRelativePaths(keyFileDir, @"TempSubDir\"))));

        Assert.Empty(comp.GetDiagnostics());
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey));
    }
    
    [Fact]
    public void PubKeyFromKeyContainerAttribute()
    {
        var x = KeyPairFile;
        string s = @"[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        other.VerifyDiagnostics();
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey));

        CompileAndVerify(other, symbolValidator: (ModuleSymbol m) =>
        {
            bool haveAttribute = false;

            foreach (var attrData in m.ContainingAssembly.GetAttributes())
            {
                if (attrData.IsTargetAttribute(m.ContainingAssembly, AttributeDescription.AssemblyKeyNameAttribute))
                {
                    haveAttribute = true;
                    break;
                }
            }

            Assert.True(haveAttribute);
        }, emitOptions: EmitOptions.CCI);
    }

    [Fact]
    public void PubKeyFromKeyFileOptions()
    {
        string s = "public class C {}";
        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics();
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey));
    }

    [Fact]
    public void PubKeyFromKeyFileOptions_ReferenceResolver()
    {
        string keyFileDir = Path.GetDirectoryName(KeyPairFile);
        string keyFileName = Path.GetFileName(KeyPairFile);

        string s = "public class C {}";
        var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

        // verify failure with default resolver
        var comp = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithCryptoKeyFile(keyFileName).WithStrongNameProvider(DefaultProvider));

        comp.VerifyDiagnostics(
            // error CS7027: Error extracting public key from file 'KeyPairFile.snk' -- File not found.
            Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(keyFileName, "File not found."));
        
        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

        // verify success with custom assembly key file resolver with keyFileDir added to search paths
        comp = CSharpCompilation.Create(
            GetUniqueName(),
            new[] { syntaxTree },
            new[] { MscorlibRef },
            TestOptions.Dll.WithCryptoKeyFile(keyFileName).WithStrongNameProvider(GetProviderWithPath(keyFileDir)));

        Assert.Empty(comp.GetDiagnostics());
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey));
    }

    [Fact]
    public void PubKeyFromKeyFileOptionsJustPublicKey()
    {
        string s = "public class C {}";
        var other = CreateCompilationWithMscorlib(s, 
            compOptions: TestOptions.Dll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(true).WithStrongNameProvider(DefaultProvider));
        other.VerifyDiagnostics();
        Assert.True(ByteSequenceComparer.Instance.Equals(TestResources.SymbolsTests.General.snPublicKey.AsImmutableOrNull(), other.Assembly.Identity.PublicKey));
    }

    [Fact]
    public void PubKeyFromKeyFileOptionsJustPublicKey_ReferenceResolver()
    {
        string publicKeyFileDir = Path.GetDirectoryName(PublicKeyFile);
        string publicKeyFileName = Path.GetFileName(PublicKeyFile);

        string s = "public class C {}";
        var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

        // verify failure with default resolver
        var comp = CreateCompilationWithMscorlib(s, 
            compOptions: TestOptions.Dll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(true).WithStrongNameProvider(DefaultProvider));

        comp.VerifyDiagnostics(
            // error CS7027: Error extracting public key from file 'PublicKeyFile.snk' -- File not found.
            Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(publicKeyFileName, "File not found."),
            // warning CS7033: Delay signing was specified and requires a public key, but no public key was specified
            Diagnostic(ErrorCode.WRN_DelaySignButNoKey)
        );

        Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

        // verify success with custom assembly key file resolver with publicKeyFileDir added to search paths
        comp = CSharpCompilation.Create(
            GetUniqueName(),
            new[] { syntaxTree },
            new[] { MscorlibRef },
            TestOptions.Dll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(true).WithStrongNameProvider(GetProviderWithPath(publicKeyFileDir)));
        Assert.Empty(comp.GetDiagnostics());
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, comp.Assembly.Identity.PublicKey));
    }

    [Fact]
    public void PubKeyFileNotFoundOptions()
    {
        string s = "public class C {}";
        var other = CreateCompilationWithMscorlib(s, 
            compOptions: TestOptions.Dll.WithCryptoKeyFile("foo").WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics(
            Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments("foo", "File not found."));

        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
    }

    [Fact]
    public void PubKeyFileBogusOptions()
    {
        var tempFile = Temp.CreateFile().WriteAllBytes(new byte[] { 1, 2, 3, 4 });
        string s = "public class C {}";

        CSharpCompilation other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithCryptoKeyFile(tempFile.Path));

        //TODO check for specific error
        Assert.NotEmpty(other.GetDiagnostics());
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
    }

    [Fact]
    public void PubKeyContainerBogusOptions()
    {
        string s = "public class C {}";
        var other = CreateCompilationWithMscorlib(s, 
            compOptions: TestOptions.Dll.WithCryptoKeyContainer("foo").WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_PublicKeyContainerFailure, arguments: new object[] { "foo", "Keyset does not exist (Exception from HRESULT: 0x80090016)" }));
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
    }

    [Fact]
    public void KeyFileAttributeOptionConflict()
    {
        string s = @"[assembly: System.Reflection.AssemblyKeyFile(""bogus"")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, 
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_CmdOptionConflictsSource).WithArguments("CryptoKeyFile", "System.Reflection.AssemblyKeyFileAttribute"));
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey));
    }

    [Fact]
    public void KeyContainerAttributeOptionConflict()
    {
        string s = @"[assembly: System.Reflection.AssemblyKeyName(""bogus"")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, 
            compOptions: TestOptions.Dll.WithCryptoKeyContainer("RoslynTestContainer").WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_CmdOptionConflictsSource).WithArguments("CryptoKeyContainer", "System.Reflection.AssemblyKeyNameAttribute"));
        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, other.Assembly.Identity.PublicKey));
    }

    [Fact]
    public void KeyFileAttributeEmpty()
    {
        string s = @"[assembly: System.Reflection.AssemblyKeyFile("""")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
        other.VerifyDiagnostics();
    }

    [Fact]
    public void KeyContainerEmpty()
    {
        string s = @"[assembly: System.Reflection.AssemblyKeyName("""")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
        other.VerifyDiagnostics();
    }

    #endregion

    #region IVT Access Checking

    [Fact]
    public void IVTBasicCompilation()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""WantsIVTAccess"")]
            public class C { internal void Foo() {} }";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        var c = CreateCompilationWithMscorlib(
@"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Foo();
        }
    }
}",
            new[] { new CSharpCompilationReference(other) },
            assemblyName: "WantsIVTAccessButCantHave",
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        //compilation should not succeed, and internals should not be imported.
        c.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_BadAccess, "Foo").WithArguments("C.Foo()"));

        var c2 = CreateCompilationWithMscorlib(
@"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Foo();
        }
    }
}",
            new[] { new CSharpCompilationReference(other) }, 
            assemblyName: "WantsIVTAccess",
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        Assert.Empty(c2.GetDiagnostics());
    }

    [Fact]
    public void IVTBasicMetadata()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""WantsIVTAccess"")]
            public class C { internal void Foo() {} }";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider)).EmitToStream();

        var c = CreateCompilationWithMscorlib(
@"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Foo();
        }
    }
}",
        references: new[] { new MetadataImageReference(other) }, 
        assemblyName: "WantsIVTAccessButCantHave",
        compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        //compilation should not succeed, and internals should not be imported.
        c.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Foo").WithArguments("C", "Foo"));

        other.Position = 0;

        var c2 = CreateCompilationWithMscorlib(
@"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Foo();
        }
    }
}",
            new[] { new MetadataImageReference(other) }, 
            assemblyName: "WantsIVTAccess",
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        Assert.Empty(c2.GetDiagnostics());
    }

    [Fact]
    public void IVTSigned()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            public class C { internal void Foo() {} }";

        var other = CreateCompilationWithMscorlib(s,
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider),
            assemblyName: "Paul");

        other.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Foo();
        }
    }
}",
            new MetadataReference[] { new CSharpCompilationReference(other) },
            TestOptions.Dll.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider),
            assemblyName: "John");

        Assert.Empty(requestor.GetDiagnostics());
    }

    [Fact]
    public void IVTErrorNotBothSigned()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            public class C { internal void Foo() {} }";

        var other = CreateCompilationWithMscorlib(s, assemblyName: "Paul", compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        other.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"public class A
{
    internal class B
    {
        protected B(C o)
        {
            o.Foo();
        }
    }
}",
            references: new[] { new CSharpCompilationReference(other) },
            assemblyName: "John",
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        // We allow John to access Paul's internal Foo even though strong-named John should not be referencing weak-named Paul.
        // Paul has, after all, specifically granted access to John.

        // TODO: During emit time we should produce an error that says that a strong-named assembly cannot reference
        // TODO: a weak-named assembly.
        requestor.VerifyDiagnostics();
    }

    [Fact]
    public void IVTDeferredSuccess()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

        var other = CreateCompilationWithMscorlib(s,
            assemblyName: "Paul",
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"
[assembly: C()]  //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
            new[] { new CSharpCompilationReference(other) },
            assemblyName: "John",
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, requestor.Assembly.Identity.PublicKey));
        Assert.Empty(requestor.GetDiagnostics());
    }

    [Fact]
    public void IVTDeferredFailSignMismatch()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

        var other = CreateCompilationWithMscorlib(s,
            assemblyName: "Paul",
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider)); //not signed. cryptoKeyFile: KeyPairFile,

        other.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"
[assembly: C()] //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
            new[] { new CSharpCompilationReference(other) },
            assemblyName: "John",
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, requestor.Assembly.Identity.PublicKey));
        requestor.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FriendRefSigningMismatch, null, new object[] { "Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" }));
    }

    [Fact]
    public void IVTDeferredFailKeyMismatch()
    {
        //key is wrong in the first digit. correct key starts with 0
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=10240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

        var other = CreateCompilationWithMscorlib(s,
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider),
            assemblyName: "Paul");

        other.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"
[assembly: C()]  //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
          new MetadataReference[] { new CSharpCompilationReference(other) },
          assemblyName: "John",
          compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        Assert.True(ByteSequenceComparer.Instance.Equals(PublicKey, requestor.Assembly.Identity.PublicKey));
        requestor.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FriendRefNotEqualToThis, null, new object[] { "Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2" }));
    }

    [Fact]
    public void IVTSuccessThroughIAssembly()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

        var other = CreateCompilationWithMscorlib(s,
            assemblyName: "Paul",
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));
 
        other.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"
[assembly: C()]  //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
            new MetadataReference[] { new CSharpCompilationReference(other) },
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider),
            assemblyName: "John");

        Assert.True(((IAssemblySymbol)other.Assembly).GivesAccessTo(requestor.Assembly));
        Assert.Empty(requestor.GetDiagnostics());
    }

    [Fact]
    public void IVTDeferredFailKeyMismatchIAssembly()
    {
        //key is wrong in the first digit. correct key starts with 0
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=10240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

        var other = CreateCompilationWithMscorlib(s,
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider),
            assemblyName: "Paul");

        other.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"

[assembly: C()]  //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
            new MetadataReference[] { new CSharpCompilationReference(other) },
            TestOptions.Dll.WithStrongNameProvider(DefaultProvider),
            assemblyName: "John");

        Assert.False(((IAssemblySymbol)other.Assembly).GivesAccessTo(requestor.Assembly));
        requestor.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FriendRefNotEqualToThis, null, new object[] { "Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2" }));
    }

    [WorkItem(820450, "DevDiv")]
    [Fact]
    public void IVTGivesAccessToUsingDifferentKeys()
    {
        string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            namespace ClassLibrary1 { internal class Class1 { } } ";

        var giver = CreateCompilationWithMscorlib(s,
            assemblyName: "Paul",
            compOptions: TestOptions.Dll.WithCryptoKeyFile(SigningTestHelpers.KeyPairFile2).WithStrongNameProvider(DefaultProvider));

        giver.VerifyDiagnostics();

        var requestor = CreateCompilationWithMscorlib(
@"
namespace ClassLibrary2
{
    internal class A
    {
        public void Foo(ClassLibrary1.Class1 a)
        {   
        }
    }
}",
            new MetadataReference[] { new CSharpCompilationReference(giver) },
            compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider),
            assemblyName: "John");

        Assert.True(((IAssemblySymbol)giver.Assembly).GivesAccessTo(requestor.Assembly));
        Assert.Empty(requestor.GetDiagnostics());
    }
    #endregion

    #region IVT instantiations

    [Fact]
    public void IVTHasCulture()
    {
        var other = CreateCompilationWithMscorlib(
        @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""WantsIVTAccess, Culture=neutral"")]
public class C
{
  static void Foo() {}
}
", compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FriendAssemblyBadArgs, @"InternalsVisibleTo(""WantsIVTAccess, Culture=neutral"")").WithArguments("WantsIVTAccess, Culture=neutral"));
    }

    [Fact]
    public void IVTNoKey()
    {
        var other = CreateCompilationWithMscorlib(
        @"
using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""WantsIVTAccess"")]
public class C
{
  static void Main() {}
}
", compOptions: TestOptions.Exe.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FriendAssemblySNReq, @"InternalsVisibleTo(""WantsIVTAccess"")").WithArguments("WantsIVTAccess"));
    }

    #endregion

    #region Signing

    [Fact]
    public void SignIt()
    {
        var other = CreateCompilationWithMscorlib(
        @"
public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var success = other.Emit(outStrm);
            Assert.True(success.Success);
        }

        AssertFileIsSigned(tempFile);
    }

    private static void AssertFileIsSigned(TempFile file)
    {
        //TODO should check to see that the output was actually signed
        using (var metadata = new FileStream(file.Path, FileMode.Open))
        {
            var flags = new PEHeaders(metadata).CorHeader.Flags;
            Assert.Equal(CorFlags.StrongNameSigned, flags & CorFlags.StrongNameSigned);
        }
    }

    void ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(MemoryStream moduleContents, AttributeDescription expectedModuleAttr)
    {
        //a module doesn't get signed for real. It should have either a keyfile or keycontainer attribute
        //parked on a typeRef named 'AssemblyAttributesGoHere.' When the module is added to an assembly, the
        //resulting assembly is signed with the key referred to by the aforementioned attribute.

        EmitResult success;
        var tempFile = Temp.CreateFile();
        moduleContents.Position = 0;

        using (var metadata = ModuleMetadata.CreateFromImageStream(moduleContents))
        {
            var flags = metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags;
            //confirm file does not claim to be signed
            Assert.Equal(0, (int)(flags & CorFlags.StrongNameSigned));
            Handle token = metadata.Module.GetTypeRef(metadata.Module.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHere");
            Assert.False(token.IsNil);   //could the type ref be located? If not then the attribute's not there.
            var attrInfos = metadata.Module.FindTargetAttributes(token, expectedModuleAttr);
            Assert.Equal(1, attrInfos.Count());

            var source = @"
public class Z
{
}";

            //now that the module checks out, ensure that adding it to a compilation outputing a dll
            //results in a signed assembly.
            var assemblyComp = CreateCompilationWithMscorlib(source, 
                new[] { new MetadataImageReference(metadata) },
                TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

            using (var finalStrm = tempFile.Open())
            {
                success = assemblyComp.Emit(finalStrm);
            }
        }

        success.Diagnostics.Verify();

        Assert.True(success.Success);
        AssertFileIsSigned(tempFile);
    }

    [Fact]
    public void SignModuleKeyFileAttr()
    {
        var x = KeyPairFile;
        string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", x, @""")] public class C {}");

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.True(success.Success);

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute);
    }

    [Fact]
    public void SignModuleKeyContainerAttr()
    {
        string s = @"[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.True(success.Success);

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute);
    }

    [Fact]
    public void SignModuleKeyContainerBogus()
    {
        string s = @"[assembly: System.Reflection.AssemblyKeyName(""bogus"")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithStrongNameProvider(DefaultProvider));
        //shouldn't have an error. The attribute's contents are checked when the module is added.
        var reference = other.EmitToImageReference();

        s = @"class D {}";

        other = CreateCompilationWithMscorlib(s, new[] { reference }, TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_PublicKeyContainerFailure).WithArguments("bogus", "Keyset does not exist (Exception from HRESULT: 0x80090016)"));
    }

    [Fact]
    public void SignModuleKeyFileBogus()
    {
        string s = @"[assembly: System.Reflection.AssemblyKeyFile(""bogus"")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithStrongNameProvider(DefaultProvider));

        //shouldn't have an error. The attribute's contents are checked when the module is added.
        var reference = other.EmitToImageReference();

        s = @"class D {}";

        other = CreateCompilationWithMscorlib(s, new[] { reference }, TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments("bogus", "File not found."));
    }

    [WorkItem(531195, "DevDiv")]
    [Fact()]
    public void SignModuleKeyContainerCmdLine()
    {
        string s = "public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.True(success.Success);

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute);
    }

    [WorkItem(531195, "DevDiv")]
    [Fact()]
    public void SignModuleKeyContainerCmdLine_1()
    {
        string s = @"
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class C {}";

        var other = CreateCompilationWithMscorlib(s, 
            compOptions: TestOptions.NetModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.True(success.Success);

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute);
    }

    [WorkItem(531195, "DevDiv")]
    [Fact()]
    public void SignModuleKeyContainerCmdLine_2()
    {
        string s = @"
[assembly: System.Reflection.AssemblyKeyName(""bogus"")]
public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.False(success.Success);
        success.Diagnostics.Verify(
            // error CS7091: Attribute 'System.Reflection.AssemblyKeyNameAttribute' given in a source file conflicts with option 'CryptoKeyContainer'.
    Diagnostic(ErrorCode.ERR_CmdOptionConflictsSource).WithArguments("System.Reflection.AssemblyKeyNameAttribute", "CryptoKeyContainer")
            );
    }

    [WorkItem(531195, "DevDiv")]
    [Fact()]
    public void SignModuleKeyFileCmdLine()
    {
        string s = "public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.True(success.Success);

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute);
    }

    [WorkItem(531195, "DevDiv")]
    [Fact()]
    public void SignModuleKeyFileCmdLine_1()
    {
        var x = KeyPairFile;
        string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", x, @""")] public class C {}");

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.True(success.Success);

        ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute);
    }

    [WorkItem(531195, "DevDiv")]
    [Fact()]
    public void SignModuleKeyFileCmdLine_2()
    {
        var x = KeyPairFile;
        string s = @"[assembly: System.Reflection.AssemblyKeyFile(""bogus"")] public class C {}";

        var other = CreateCompilationWithMscorlib(s, compOptions: TestOptions.NetModule.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var success = other.Emit(outStrm);
        Assert.False(success.Success);
        success.Diagnostics.Verify(
            // error CS7091: Attribute 'System.Reflection.AssemblyKeyFileAttribute' given in a source file conflicts with option 'CryptoKeyFile'.
            Diagnostic(ErrorCode.ERR_CmdOptionConflictsSource).WithArguments("System.Reflection.AssemblyKeyFileAttribute", "CryptoKeyFile"));
    }

    [Fact]
    public void SignItWithOnlyPublicKey()
    {
        var other = CreateCompilationWithMscorlib(
        @"
public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(PublicKeyFile).WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var emitResult = other.Emit(outStrm);
        other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_SignButNoPrivateKey).WithArguments(PublicKeyFile));

        other = other.WithOptions(TestOptions.NetModule.WithCryptoKeyFile(PublicKeyFile));

        var assembly = CreateCompilationWithMscorlib("", 
            references: new[] { other.EmitToImageReference() }, 
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        assembly.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_SignButNoPrivateKey).WithArguments(PublicKeyFile));
    }

    [Fact]
    public void DelaySignItWithOnlyPublicKey()
    {
        var other = CreateCompilationWithMscorlib(
            @"
[assembly: System.Reflection.AssemblyDelaySign(true)]
public class C
{
  static void Foo() {}
}", compOptions: TestOptions.Dll.WithCryptoKeyFile(PublicKeyFile).WithStrongNameProvider(DefaultProvider));

        using (var outStrm = new MemoryStream())
        {
            var emitResult = other.Emit(outStrm);
            Assert.True(emitResult.Success);
        }
    }

    [Fact]
    public void DelaySignButNoKey()
    {
        var other = CreateCompilationWithMscorlib(
            @"
[assembly: System.Reflection.AssemblyDelaySign(true)]
public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        var emitResult = other.Emit(outStrm);
        // Dev11: warning CS1699: Use command line option '/delaysign' or appropriate project settings instead of 'AssemblyDelaySignAttribute'
        //        warning CS1607: Assembly generation -- Delay signing was requested, but no key was given
        // Roslyn: warning CS7033: Delay signing was specified and requires a public key, but no public key was specified
        other.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_DelaySignButNoKey));
        Assert.True(emitResult.Success);
    }

    [Fact]
    public void SignInMemory()
    {
        var other = CreateCompilationWithMscorlib(
            @"
public class C
{
  static void Foo() {}
}",
compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));
        var outStrm = new MemoryStream();
        var emitResult = other.Emit(outStrm);
        Assert.True(emitResult.Success);
    }

    [Fact]
    public void DelaySignConflict()
    {
        var other = CreateCompilationWithMscorlib(
            @"
[assembly: System.Reflection.AssemblyDelaySign(true)]
public class C
{
  static void Foo() {}
}", compOptions: TestOptions.Dll.WithDelaySign(false).WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        //shouldn't get any key warning.
        other.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_CmdOptionConflictsSource).WithArguments("DelaySign", "System.Reflection.AssemblyDelaySignAttribute"));
        var emitResult = other.Emit(outStrm);
        Assert.True(emitResult.Success);
    }

    [Fact]
    public void DelaySignNoConflict()
    {
        var other = CreateCompilationWithMscorlib(
            @"
[assembly: System.Reflection.AssemblyDelaySign(true)]
public class C
{
  static void Foo() {}
}", compOptions: TestOptions.Dll.WithDelaySign(true).WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        var outStrm = new MemoryStream();
        //shouldn't get any key warning.
        other.VerifyDiagnostics();
        var emitResult = other.Emit(outStrm);
        Assert.True(emitResult.Success);
    }

    [Fact]
    public void DelaySignWithAssemblySignatureKey()
    {
        //Note that this SignatureKey is some random one that I found in the devdiv build.
        //It is not related to the other keys we use in these tests.

        //In the native compiler, when the AssemblySignatureKey attribute is present, and
        //the binary is configured for delay signing, the contents of the assemblySignatureKey attribute
        //(rather than the contents of the keyfile or container) are used to compute the size needed to 
        //reserve in the binary for its signature. Signing using this key is only supported via sn.exe

        var other = CreateCompilation(
            @"
[assembly: System.Reflection.AssemblyDelaySign(true)]
[assembly: System.Reflection.AssemblySignatureKey(""002400000c800000140100000602000000240000525341310008000001000100613399aff18ef1a2c2514a273a42d9042b72321f1757102df9ebada69923e2738406c21e5b801552ab8d200a65a235e001ac9adc25f2d811eb09496a4c6a59d4619589c69f5baf0c4179a47311d92555cd006acc8b5959f2bd6e10e360c34537a1d266da8085856583c85d81da7f3ec01ed9564c58d93d713cd0172c8e23a10f0239b80c96b07736f5d8b022542a4e74251a5f432824318b3539a5a087f8e53d2f135f9ca47f3bb2e10aff0af0849504fb7cea3ff192dc8de0edad64c68efde34c56d302ad55fd6e80f302d5efcdeae953658d3452561b5f36c542efdbdd9f888538d374cef106acf7d93a4445c3c73cd911f0571aaf3d54da12b11ddec375b3"", ""a5a866e1ee186f807668209f3b11236ace5e21f117803a3143abb126dd035d7d2f876b6938aaf2ee3414d5420d753621400db44a49c486ce134300a2106adb6bdb433590fef8ad5c43cba82290dc49530effd86523d9483c00f458af46890036b0e2c61d077d7fbac467a506eba29e467a87198b053c749aa2a4d2840c784e6d"")]
public class C
{
  static void Foo() {}
}", 
            new MetadataReference[] { MscorlibRef_v4_0_30316_17626 }, 
            compOptions: TestOptions.Dll.WithDelaySign(true).WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        using (var metadata = ModuleMetadata.CreateFromImage(other.EmitToArray()))
        {
            var header = metadata.Module.PEReaderOpt.PEHeaders.CorHeader;
            //confirm header has expected SN signature size
            Assert.Equal(256, header.StrongNameSignatureDirectory.Size);
        }
    }

    [WorkItem(545720, "DevDiv")]
    [WorkItem(530050, "DevDiv")]
    [Fact]
    public void InvalidAssemblyName()
    {
        var il = @"
.assembly extern mscorlib { }
.assembly asm1
{
    .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 09 2F 5C 3A 2A 3F 27 3C 3E 7C 00 00 ) // .../\:*?'<>|..
}

.class private auto ansi beforefieldinit Base
       extends [mscorlib]System.Object
{
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [mscorlib]System.Object::.ctor()
    ret
  }
}
";

        var csharp = @"
class Derived : Base 
{
}
";

        var ilRef = CompileIL(il, appendDefaultHeader: false);

        var comp = CreateCompilationWithMscorlib(csharp, new[] { ilRef }, assemblyName: "asm2", compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider));
        comp.VerifyDiagnostics(
            // NOTE: dev10 reports WRN_InvalidAssemblyName, but Roslyn won't (DevDiv #15099).

            // (2,17): error CS0122: 'Base' is inaccessible due to its protection level
            // class Derived : Base 
            Diagnostic(ErrorCode.ERR_BadAccess, "Base").WithArguments("Base"));
    }

    [WorkItem(546331, "DevDiv")]
    [Fact]
    public void IvtVirtualCall1()
    {
        var source1 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm2"")]

public class A
{
    internal virtual void M() { }
    internal virtual int P { get { return 0; } }
    internal virtual event System.Action E { add { } remove { } }
}
";
        var source2 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm3"")]

public class B : A
{
    internal override void M() { }
    internal override int P { get { return 0; } }
    internal override event System.Action E { add { } remove { } }
}
";
        var source3 = @"
using System;
using System.Linq.Expressions;

public class C : B
{
    internal override void M() { }

    void Test()
    {
        C c = new C();
        c.M();
        int x = c.P;
        c.E += null;
    }

    void TestET() 
    {
        C c = new C();
        Expression<Action> expr = () => c.M();
    }
}
";

        var comp1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "asm1");
        comp1.VerifyDiagnostics();
        var ref1 = new CSharpCompilationReference(comp1);

        var comp2 = CreateCompilationWithMscorlib(source2, new[] { ref1 }, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "asm2");
        comp2.VerifyDiagnostics();
        var ref2 = new CSharpCompilationReference(comp2);

        var comp3 = CreateCompilationWithMscorlib(source3, new[] { SystemCoreRef, ref1, ref2 }, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "asm3");
        comp3.VerifyDiagnostics();

        // Note: calls B.M, not A.M, since asm1 is not accessible.
        var verifier = CompileAndVerify(comp3, emitOptions: EmitOptions.CCI);
            
        verifier.VerifyIL("C.Test", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (C V_0, //c
  int V_1) //x
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   ""void B.M()""
  IL_000c:  ldloc.0
  IL_000d:  callvirt   ""int B.P.get""
  IL_0012:  stloc.1
  IL_0013:  ldloc.0
  IL_0014:  ldnull
  IL_0015:  callvirt   ""void B.E.add""
  IL_001a:  ret
}");

        verifier.VerifyIL("C.TestET", @"
{
  // Code size       87 (0x57)
  .maxstack  3
  .locals init (C.<>c__DisplayClass0 V_0, //CS$<>8__locals0
  System.Linq.Expressions.Expression<System.Action> V_1) //expr
  IL_0000:  newobj     ""C.<>c__DisplayClass0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""C..ctor()""
  IL_000c:  stfld      ""C C.<>c__DisplayClass0.c""
  IL_0011:  ldloc.0
  IL_0012:  ldtoken    ""C.<>c__DisplayClass0""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0021:  ldtoken    ""C C.<>c__DisplayClass0.c""
  IL_0026:  call       ""System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)""
  IL_002b:  call       ""System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)""
  IL_0030:  ldtoken    ""void B.M()""
  IL_0035:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_003a:  castclass  ""System.Reflection.MethodInfo""
  IL_003f:  ldc.i4.0
  IL_0040:  newarr     ""System.Linq.Expressions.Expression""
  IL_0045:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_004a:  ldc.i4.0
  IL_004b:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0050:  call       ""System.Linq.Expressions.Expression<System.Action> System.Linq.Expressions.Expression.Lambda<System.Action>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0055:  stloc.1
  IL_0056:  ret
}
");
    }

    [WorkItem(546331, "DevDiv")]
    [Fact]
    public void IvtVirtualCall2()
    {
        var source1 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm2"")]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm4"")]

public class A
{
    internal virtual void M() { }
    internal virtual int P { get { return 0; } }
    internal virtual event System.Action E { add { } remove { } }
}
";
        var source2 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm3"")]

public class B : A
{
    internal override void M() { }
    internal override int P { get { return 0; } }
    internal override event System.Action E { add { } remove { } }
}
";
        var source3 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm4"")]

public class C : B
{
    internal override void M() { }
    internal override int P { get { return 0; } }
    internal override event System.Action E { add { } remove { } }
}
";
        var source4 = @"
using System;
using System.Linq.Expressions;

public class D : C
{
    internal override void M() { }

    void Test()
    {
        D d = new D();
        d.M();
        int x = d.P;
        d.E += null;
    }

    void TestET() 
    {
        D d = new D();
        Expression<Action> expr = () => d.M();
    }
}
";

        var comp1 = CreateCompilationWithMscorlib(source1, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "asm1");
        comp1.VerifyDiagnostics();
        var ref1 = new CSharpCompilationReference(comp1);

        var comp2 = CreateCompilationWithMscorlib(source2, new[] { ref1 }, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "asm2");
        comp2.VerifyDiagnostics();
        var ref2 = new CSharpCompilationReference(comp2);

        var comp3 = CreateCompilationWithMscorlib(source3, new[] { ref1, ref2 }, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "asm3");
        comp3.VerifyDiagnostics();
        var ref3 = new CSharpCompilationReference(comp3);

        var comp4 = CreateCompilationWithMscorlib(source4, new[] { SystemCoreRef, ref1, ref2, ref3 }, compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "asm4");
        comp4.VerifyDiagnostics();

        // Note: calls C.M, not A.M, since asm2 is not accessible (stops search).
        // Confirmed in Dev11.
        var verifier = CompileAndVerify(comp4, emitOptions: EmitOptions.CCI);
        
        verifier.VerifyIL("D.Test", @"
{
  // Code size       27 (0x1b)
  .maxstack  2
  .locals init (D V_0, //d
  int V_1) //x
  IL_0000:  newobj     ""D..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  callvirt   ""void C.M()""
  IL_000c:  ldloc.0
  IL_000d:  callvirt   ""int C.P.get""
  IL_0012:  stloc.1
  IL_0013:  ldloc.0
  IL_0014:  ldnull
  IL_0015:  callvirt   ""void C.E.add""
  IL_001a:  ret
}");

        verifier.VerifyIL("D.TestET", @"
{
  // Code size       87 (0x57)
  .maxstack  3
  .locals init (D.<>c__DisplayClass0 V_0, //CS$<>8__locals0
  System.Linq.Expressions.Expression<System.Action> V_1) //expr
  IL_0000:  newobj     ""D.<>c__DisplayClass0..ctor()""
  IL_0005:  stloc.0
  IL_0006:  ldloc.0
  IL_0007:  newobj     ""D..ctor()""
  IL_000c:  stfld      ""D D.<>c__DisplayClass0.d""
  IL_0011:  ldloc.0
  IL_0012:  ldtoken    ""D.<>c__DisplayClass0""
  IL_0017:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001c:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_0021:  ldtoken    ""D D.<>c__DisplayClass0.d""
  IL_0026:  call       ""System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)""
  IL_002b:  call       ""System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)""
  IL_0030:  ldtoken    ""void C.M()""
  IL_0035:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_003a:  castclass  ""System.Reflection.MethodInfo""
  IL_003f:  ldc.i4.0
  IL_0040:  newarr     ""System.Linq.Expressions.Expression""
  IL_0045:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_004a:  ldc.i4.0
  IL_004b:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_0050:  call       ""System.Linq.Expressions.Expression<System.Action> System.Linq.Expressions.Expression.Lambda<System.Action>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0055:  stloc.1
  IL_0056:  ret
}");
    }

    [Fact]
    public void IvtVirtual_ParamsAndDynamic()
    {
        var source1 = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm2"")]

public class A
{
    internal virtual void F(params int[] a) { }
    internal virtual void G(System.Action<dynamic> a) { }

    [System.Obsolete(""obsolete"", true)]
    internal virtual void H() { }

    internal virtual int this[int x, params int[] a] { get { return 0; } }
}
";
        // use IL to generate code that doesn't have synthesized ParamArrayAttribute on int[] parameters:

        // [assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""asm3"")]
        // public class B : A
        // {
        //     internal override void F(int[] a) { }                            
        //     internal override void G(System.Action<object> a) { }
        //     internal override void H() { }
        //     internal override int this[int x, int[] a] { get { return 0; } }
        // }

        var source2 = @"
.assembly extern asm1
{
  .ver 0:0:0:0
}
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly asm2
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 04 61 73 6D 33 00 00 )                      // ...asm3..
}

.class public auto ansi beforefieldinit B extends [asm1]A
{
  .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = ( 01 00 04 49 74 65 6D 00 00 )                      // ...Item..
  
  .method assembly hidebysig strict virtual instance void  F(int32[] a) cil managed 
  {
    nop
    ret
  }

  .method assembly hidebysig strict virtual instance void  G(class [mscorlib]System.Action`1<object> a) cil managed
  {
    nop
    ret
  }

  .method assembly hidebysig strict virtual instance void  H() cil managed
  {
    nop
    ret
  }

  .method assembly hidebysig specialname strict virtual instance int32  get_Item(int32 x, int32[] a) cil managed
  {
    ldloc.0
    ret
  }

  .method public hidebysig specialname rtspecialname instance void  .ctor() cil managed
  {
    ldarg.0
    call       instance void [asm1]A::.ctor()
    ret
  }

  .property instance int32 Item(int32, int32[])
  {
    .get instance int32 B::get_Item(int32,
                                    int32[])
  }
}";

        var source3 = @"
public class C : B
{
    void Test()
    {
        C c = new C();
        c.F();
        c.G(x => x.Bar());
        c.H();
        var z = c[1];
    }
}
";

        var comp1 = CreateCompilationWithMscorlib(source1, 
            new[] { SystemCoreRef }, 
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider),
            assemblyName: "asm1");

        comp1.VerifyDiagnostics();
        var ref1 = new CSharpCompilationReference(comp1);

        var ref2 = CompileIL(source2, appendDefaultHeader: false);

        var comp3 = CreateCompilationWithMscorlib(source3, 
            new[] { SystemCoreRef, ref1, ref2 }, 
            compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), 
            assemblyName: "asm3");

        comp3.VerifyDiagnostics(
            // (7,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'a' of 'B.F(int[])'
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "F").WithArguments("a", "B.F(int[])").WithLocation(7, 11),
            // (8,20): error CS1061: 'object' does not contain a definition for 'Bar' and no extension method 'Bar' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
            Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Bar").WithArguments("object", "Bar").WithLocation(8, 20),
            // (10,17): error CS7036: There is no argument given that corresponds to the required formal parameter 'a' of 'B.this[int, int[]]'
            Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c[1]").WithArguments("a", "B.this[int, int[]]").WithLocation(10, 17));
    }

    [Fact] [WorkItem(529779, "DevDiv")]
    public void Bug529779_1()
    {
        CSharpCompilation unsigned = CreateCompilationWithMscorlib(
@"
public class C1
{}
", compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "Unsigned");

        CSharpCompilation other = CreateCompilationWithMscorlib(
@"
public class C
{
    internal void Foo()
    {
        var x = new System.Guid();
        System.Console.WriteLine(x);
    }
}
", compOptions:TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider));

        CompileAndVerify(other.WithReferences(new []{other.References.ElementAt(0), new CSharpCompilationReference(unsigned)}),
                         emitOptions: EmitOptions.CCI).VerifyDiagnostics();

        CompileAndVerify(other.WithReferences(new[] { other.References.ElementAt(0), new MetadataImageReference(unsigned.EmitToStream()) }), 
                         emitOptions: EmitOptions.CCI).VerifyDiagnostics();
    }

    [Fact] [WorkItem(529779, "DevDiv")]
    public void Bug529779_2()
    {
        CSharpCompilation unsigned = CreateCompilationWithMscorlib(
@"
public class C1
{}
",        compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "Unsigned");

        CSharpCompilation other = CreateCompilationWithMscorlib(
@"
public class C
{
    internal void Foo()
    {
        var x = new C1();
        System.Console.WriteLine(x);
    }
}
", compOptions:TestOptions.Dll.WithStrongNameProvider(DefaultProvider).WithCryptoKeyFile(KeyPairFile));

        var comps = new [] {other.WithReferences(new []{other.References.ElementAt(0), new CSharpCompilationReference(unsigned)}),
                            other.WithReferences(new []{other.References.ElementAt(0), new MetadataImageReference(unsigned.EmitToStream())})};

        foreach (var comp in comps)
        {
            var outStrm = new MemoryStream();
            var emitResult = comp.Emit(outStrm);

            // Dev12 reports an error
            Assert.True(emitResult.Success);

            emitResult.Diagnostics.Verify(
                // warning CS8002: Referenced assembly 'Unsigned, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null' does not have a strong name.
                Diagnostic(ErrorCode.WRN_ReferencedAssemblyDoesNotHaveStrongName).WithArguments("Unsigned, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null"));
        }
    }

    [Fact]
    public void AssemblySignatureKeyAttribute_1()
    {
        var other = CreateCompilation(
        @"
[assembly: System.Reflection.AssemblySignatureKeyAttribute(
""00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"",
""bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819"")]

public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider), references: new [] {MscorlibRef_v4_0_30316_17626});

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var success = other.Emit(outStrm);
            Assert.True(success.Success);
        }

        AssertFileIsSigned(tempFile);
    }

    [Fact]
    public void AssemblySignatureKeyAttribute_2()
    {
        var other = CreateCompilation(
        @"
[assembly: System.Reflection.AssemblySignatureKeyAttribute(
""xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"",
""bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819"")]

public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var success = other.Emit(outStrm);
            Assert.False(success.Success);
            success.Diagnostics.Verify(
                // (3,1): error CS8003: Invalid signature public key specified in AssemblySignatureKeyAttribute.
                // "xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
                Diagnostic(ErrorCode.ERR_InvalidSignaturePublicKey, @"""xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"""));
        }
    }

    [Fact]
    public void AssemblySignatureKeyAttribute_3()
    {
        var other = CreateCompilation(
        @"
[assembly: System.Reflection.AssemblySignatureKeyAttribute(
""00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"",
""FFFFbc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819"")]

public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(KeyPairFile).WithStrongNameProvider(DefaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var result = other.Emit(outStrm);
            Assert.False(result.Success);
            result.Diagnostics.VerifyErrorCodes(
                // error CS7027: Error signing output with public key from file 'KeyPairFile.snk' -- Invalid countersignature specified in AssemblySignatureKeyAttribute. (Exception from HRESULT: 0x80131423)
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure));
        }
    }

    [Fact]
    public void AssemblySignatureKeyAttribute_4()
    {
        var other = CreateCompilation(
        @"
[assembly: System.Reflection.AssemblySignatureKeyAttribute(
""xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"",
""bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819"")]

public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(true).WithStrongNameProvider(DefaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var success = other.Emit(outStrm);
            Assert.False(success.Success);
            success.Diagnostics.Verify(
                // (3,1): error CS8003: Invalid signature public key specified in AssemblySignatureKeyAttribute.
                // "xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb",
    Diagnostic(ErrorCode.ERR_InvalidSignaturePublicKey, @"""xxx 00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb""")
                );
        }
    }

    [Fact]
    public void AssemblySignatureKeyAttribute_5()
    {
        var other = CreateCompilation(
        @"
[assembly: System.Reflection.AssemblySignatureKeyAttribute(
""00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"",
""FFFFbc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819"")]

public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(true).WithStrongNameProvider(DefaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var success = other.Emit(outStrm);
            Assert.True(success.Success);
        }
    }

    [Fact]
    public void AssemblySignatureKeyAttribute_6()
    {
        var other = CreateCompilation(
        @"
[assembly: System.Reflection.AssemblySignatureKeyAttribute(
null,
""bc6402e37ad723580b576953f40475ceae4b784d3661b90c3c6f5a1f7283388a7880683e0821610bee977f70506bb75584080e01b2ec97483c4d601ce1c981752a07276b420d78594d0ef28f8ec016d0a5b6d56cfc22e9f25a2ed9545942ccbf2d6295b9528641d98776e06a3273ab233271a3c9f53099b4d4e029582a6d5819"")]

public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(true).WithStrongNameProvider(DefaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var success = other.Emit(outStrm);
            Assert.False(success.Success);
            success.Diagnostics.Verify(
                // (3,1): error CS8003: Invalid signature public key specified in AssemblySignatureKeyAttribute.
                // null,
    Diagnostic(ErrorCode.ERR_InvalidSignaturePublicKey, "null")
                );
        }
    }

    [Fact]
    public void AssemblySignatureKeyAttribute_7()
    {
        var other = CreateCompilation(
        @"
[assembly: System.Reflection.AssemblySignatureKeyAttribute(
""00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"",
null)]

public class C
{
  static void Foo() {}
}",
  compOptions: TestOptions.Dll.WithCryptoKeyFile(PublicKeyFile).WithDelaySign(true).WithStrongNameProvider(DefaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

        var tempFile = Temp.CreateFile();

        using (var outStrm = tempFile.Open())
        {
            var success = other.Emit(outStrm);
            Assert.True(success.Success);
        }
    }

    [Fact, WorkItem(769840, "DevDiv")]
    public void Bug769840()
    {
        var ca = CreateCompilationWithMscorlib(
@"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Bug769840_B, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100458a131798af87d9e33088a3ab1c6101cbd462760f023d4f41d97f691033649e60b42001e94f4d79386b5e087b0a044c54b7afce151b3ad19b33b332b83087e3b8b022f45b5e4ff9b9a1077b0572ff0679ce38f884c7bd3d9b4090e4a7ee086b7dd292dc20f81a3b1b8a0b67ee77023131e59831c709c81d11c6856669974cc4"")]

internal class A
{
    public int Value = 3;
}
", compOptions: TestOptions.Dll.WithStrongNameProvider(DefaultProvider), assemblyName: "Bug769840_A");

        CompileAndVerify(ca);

        var cb = CreateCompilationWithMscorlib(
@"
internal class B
{
    public A GetA()
    {
        return new A();
    }
}", 
            compOptions: TestOptions.NetModule.WithStrongNameProvider(DefaultProvider),
            assemblyName: "Bug769840_B", 
            references: new[] { new CSharpCompilationReference(ca)});

        CompileAndVerify(cb, verify:false).Diagnostics.Verify(); 
    }


    #endregion
}
