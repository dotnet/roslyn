// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Roslyn.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public partial class InternalsVisibleToAndStrongNameTests : CSharpTestBase
    {
        #region Helpers

        public InternalsVisibleToAndStrongNameTests()
        {
            SigningTestHelpers.InstallKey();
        }

        private static readonly string s_keyPairFile = SigningTestHelpers.KeyPairFile;
        private static readonly string s_publicKeyFile = SigningTestHelpers.PublicKeyFile;
        private static readonly ImmutableArray<byte> s_publicKey = SigningTestHelpers.PublicKey;
        private static readonly DesktopStrongNameProvider s_defaultProvider = new SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create<string>());

        private static DesktopStrongNameProvider GetProviderWithPath(string keyFilePath)
        {
            return new SigningTestHelpers.VirtualizedStrongNameProvider(ImmutableArray.Create(keyFilePath));
        }

        #endregion

        #region Naming Tests

        [Fact, WorkItem(529419, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529419")]
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
                options: TestOptions.ReleaseDll.WithStrongNameProvider(new DesktopStrongNameProvider()));

            c.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments("MyKey.snk", CodeAnalysisResources.FileNotFound));
        }

        [Fact]
        public void PubKeyFromKeyFileAttribute()
        {
            var x = s_keyPairFile;
            string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", x, @""")] public class C {}");

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));
            other.VerifyDiagnostics();
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));

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
            });
        }

        [Fact]
        public void PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver()
        {
            string keyFileDir = Path.GetDirectoryName(s_keyPairFile);
            string keyFileName = Path.GetFileName(s_keyPairFile);

            string s = string.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", keyFileName, @""")] public class C {}");
            var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

            // verify failure with default assembly key file resolver
            var comp = CreateCompilationWithMscorlib(syntaxTree, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(keyFileName, "Assembly signing not supported."));

            Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

            // verify success with custom assembly key file resolver with keyFileDir added to search paths
            comp = CSharpCompilation.Create(
                GetUniqueName(),
                new[] { syntaxTree },
                new[] { MscorlibRef },
                TestOptions.ReleaseDll.WithStrongNameProvider(GetProviderWithPath(keyFileDir)));

            comp.VerifyDiagnostics();

            Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void PubKeyFromKeyFileAttribute_AssemblyKeyFileResolver_RelativeToCurrentParent()
        {
            string keyFileDir = Path.GetDirectoryName(s_keyPairFile);
            string keyFileName = Path.GetFileName(s_keyPairFile);

            string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""..\", keyFileName, @""")] public class C {}");
            var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

            // verify failure with default assembly key file resolver
            var comp = CreateCompilationWithMscorlib(syntaxTree, options: TestOptions.ReleaseDll);
            comp.VerifyDiagnostics(
                // error CS7027: Error extracting public key from file '..\KeyPairFile.snk' -- File not found.
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(@"..\" + keyFileName, "Assembly signing not supported."));

            Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

            // verify success with custom assembly key file resolver with keyFileDir\TempSubDir added to search paths
            comp = CSharpCompilation.Create(
                GetUniqueName(),
                new[] { syntaxTree },
                new[] { MscorlibRef },
                TestOptions.ReleaseDll.WithStrongNameProvider(GetProviderWithPath(PathUtilities.CombineAbsoluteAndRelativePaths(keyFileDir, @"TempSubDir\"))));

            Assert.Empty(comp.GetDiagnostics());
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void SigningNotAvailable001()
        {
            string keyFileDir = Path.GetDirectoryName(s_keyPairFile);
            string keyFileName = Path.GetFileName(s_keyPairFile);

            string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""..\", keyFileName, @""")] public class C {}");
            var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

            // verify failure 
            var comp = CSharpCompilation.Create(
                GetUniqueName(),
                new[] { syntaxTree },
                new[] { MscorlibRef },
                TestOptions.ReleaseDll.WithStrongNameProvider(GetProviderWithPath(PathUtilities.CombineAbsoluteAndRelativePaths(keyFileDir, @"TempSubDir\"))));

            var provider = (DesktopStrongNameProvider)comp.Options.StrongNameProvider;

            provider.TestStrongNameInterfaceFactory = () =>
            {
                throw new DllNotFoundException("aaa.dll not found.");
            };

            comp.VerifyEmitDiagnostics(
                // error CS7027: Error signing output with public key from file '..\KeyPair_6187d0d6-f691-47fd-985b-03570bc0668d.snk' -- aaa.dll not found.
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments("..\\" + keyFileName, "aaa.dll not found.").WithLocation(1, 1)
            );
        }

        [Fact]
        public void PubKeyFromKeyContainerAttribute()
        {
            var x = s_keyPairFile;
            string s = @"[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));
            other.VerifyDiagnostics();
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));

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
            });
        }

        [Fact]
        public void PubKeyFromKeyFileOptions()
        {
            string s = "public class C {}";
            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

            other.VerifyDiagnostics();
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void PubKeyFromKeyFileOptions_ReferenceResolver()
        {
            string keyFileDir = Path.GetDirectoryName(s_keyPairFile);
            string keyFileName = Path.GetFileName(s_keyPairFile);

            string s = "public class C {}";
            var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

            // verify failure with default resolver
            var comp = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithCryptoKeyFile(keyFileName).WithStrongNameProvider(s_defaultProvider));

            comp.VerifyDiagnostics(
                // error CS7027: Error extracting public key from file 'KeyPairFile.snk' -- File not found.
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(keyFileName, CodeAnalysisResources.FileNotFound));

            Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

            // verify success with custom assembly key file resolver with keyFileDir added to search paths
            comp = CSharpCompilation.Create(
                GetUniqueName(),
                new[] { syntaxTree },
                new[] { MscorlibRef },
                TestOptions.ReleaseDll.WithCryptoKeyFile(keyFileName).WithStrongNameProvider(GetProviderWithPath(keyFileDir)));

            Assert.Empty(comp.GetDiagnostics());
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void PubKeyFromKeyFileOptionsJustPublicKey()
        {
            string s = "public class C {}";
            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(true).WithStrongNameProvider(s_defaultProvider));
            other.VerifyDiagnostics();
            Assert.True(ByteSequenceComparer.Equals(TestResources.General.snPublicKey.AsImmutableOrNull(), other.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void PubKeyFromKeyFileOptionsJustPublicKey_ReferenceResolver()
        {
            string publicKeyFileDir = Path.GetDirectoryName(s_publicKeyFile);
            string publicKeyFileName = Path.GetFileName(s_publicKeyFile);

            string s = "public class C {}";
            var syntaxTree = Parse(s, @"IVTAndStrongNameTests\AnotherTempDir\temp.cs");

            // verify failure with default resolver
            var comp = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(true).WithStrongNameProvider(s_defaultProvider));

            comp.VerifyDiagnostics(
                // error CS7027: Error extracting public key from file 'PublicKeyFile.snk' -- File not found.
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments(publicKeyFileName, CodeAnalysisResources.FileNotFound),
                // warning CS7033: Delay signing was specified and requires a public key, but no public key was specified
                Diagnostic(ErrorCode.WRN_DelaySignButNoKey)
            );

            Assert.True(comp.Assembly.Identity.PublicKey.IsEmpty);

            // verify success with custom assembly key file resolver with publicKeyFileDir added to search paths
            comp = CSharpCompilation.Create(
                GetUniqueName(),
                new[] { syntaxTree },
                new[] { MscorlibRef },
                TestOptions.ReleaseDll.WithCryptoKeyFile(publicKeyFileName).WithDelaySign(true).WithStrongNameProvider(GetProviderWithPath(publicKeyFileDir)));
            Assert.Empty(comp.GetDiagnostics());
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, comp.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void PubKeyFileNotFoundOptions()
        {
            string s = "public class C {}";
            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyFile("foo").WithStrongNameProvider(s_defaultProvider));

            other.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments("foo", CodeAnalysisResources.FileNotFound));

            Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
        }

        [Fact]
        public void PubKeyFileBogusOptions()
        {
            var tempFile = Temp.CreateFile().WriteAllBytes(new byte[] { 1, 2, 3, 4 });
            string s = "public class C {}";

            CSharpCompilation other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithCryptoKeyFile(tempFile.Path));

            //TODO check for specific error
            Assert.NotEmpty(other.GetDiagnostics());
            Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
        }

        [WorkItem(5662, "https://github.com/dotnet/roslyn/issues/5662")]
        [ConditionalFact(typeof(IsEnglishLocal))]
        public void PubKeyContainerBogusOptions()
        {
            string s = "public class C {}";
            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyContainer("foo").WithStrongNameProvider(s_defaultProvider));

            // error CS7028: Error signing output with public key from container 'foo' -- Keyset does not exist (Exception from HRESULT: 0x80090016)
            var err = other.GetDiagnostics().Single();

            Assert.Equal((int)ErrorCode.ERR_PublicKeyContainerFailure, err.Code);
            Assert.Equal(2, err.Arguments.Count);
            Assert.Equal("foo", err.Arguments[0]);
            Assert.True(((string)err.Arguments[1]).EndsWith(" HRESULT: 0x80090016)", StringComparison.Ordinal));

            Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
        }

        [Fact]
        public void KeyFileAttributeOptionConflict()
        {
            string s = @"[assembly: System.Reflection.AssemblyKeyFile(""bogus"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

            other.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_CmdOptionConflictsSource).WithArguments("CryptoKeyFile", "System.Reflection.AssemblyKeyFileAttribute"));
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void KeyContainerAttributeOptionConflict()
        {
            string s = @"[assembly: System.Reflection.AssemblyKeyName(""bogus"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyContainer("RoslynTestContainer").WithStrongNameProvider(s_defaultProvider));

            other.VerifyDiagnostics(Diagnostic(ErrorCode.WRN_CmdOptionConflictsSource).WithArguments("CryptoKeyContainer", "System.Reflection.AssemblyKeyNameAttribute"));
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, other.Assembly.Identity.PublicKey));
        }

        [Fact]
        public void KeyFileAttributeEmpty()
        {
            string s = @"[assembly: System.Reflection.AssemblyKeyFile("""")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));
            Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
            other.VerifyDiagnostics();
        }

        [Fact]
        public void KeyContainerEmpty()
        {
            string s = @"[assembly: System.Reflection.AssemblyKeyName("""")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));
            Assert.True(other.Assembly.Identity.PublicKey.IsEmpty);
            other.VerifyDiagnostics();
        }

        [Fact]
        public void PublicKeyFromOptions_DelaySigned()
        {
            string source = @"
[assembly: System.Reflection.AssemblyDelaySign(true)]
public class C {}";

            var c = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll.WithCryptoPublicKey(s_publicKey));
            c.VerifyDiagnostics();
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, c.Assembly.Identity.PublicKey));

            var metadata = ModuleMetadata.CreateFromImage(c.EmitToArray());
            var identity = metadata.Module.ReadAssemblyIdentityOrThrow();

            Assert.True(identity.HasPublicKey);
            AssertEx.Equal(identity.PublicKey, s_publicKey);
            Assert.Equal(CorFlags.ILOnly, metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags);
        }

        [Fact, WorkItem(9150, "https://github.com/dotnet/roslyn/issues/9150")]
        public void PublicKeyFromOptions_PublicSign()
        {
            // attributes are ignored
            string source = @"
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")] 
[assembly: System.Reflection.AssemblyKeyFile(""some file"")] 
public class C {}
";

            var c = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll.WithCryptoPublicKey(s_publicKey).WithPublicSign(true));
            c.VerifyDiagnostics(
                // warning CS7103: Attribute 'System.Reflection.AssemblyKeyNameAttribute' is ignored when public signing is specified.
                Diagnostic(ErrorCode.WRN_AttributeIgnoredWhenPublicSigning).WithArguments("System.Reflection.AssemblyKeyNameAttribute").WithLocation(1, 1),
                // warning CS7103: Attribute 'System.Reflection.AssemblyKeyFileAttribute' is ignored when public signing is specified.
                Diagnostic(ErrorCode.WRN_AttributeIgnoredWhenPublicSigning).WithArguments("System.Reflection.AssemblyKeyFileAttribute").WithLocation(1, 1)
            );
            Assert.True(ByteSequenceComparer.Equals(s_publicKey, c.Assembly.Identity.PublicKey));

            var metadata = ModuleMetadata.CreateFromImage(c.EmitToArray());
            var identity = metadata.Module.ReadAssemblyIdentityOrThrow();

            Assert.True(identity.HasPublicKey);
            AssertEx.Equal(identity.PublicKey, s_publicKey);
            Assert.Equal(CorFlags.ILOnly | CorFlags.StrongNameSigned, metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags);
        }

        [Fact, WorkItem(9150, "https://github.com/dotnet/roslyn/issues/9150")]
        public void KeyFileFromAttributes_PublicSign()
        {
            string source = @"
[assembly: System.Reflection.AssemblyKeyFile(""test.snk"")]
public class C {}
";
            var c = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll.WithPublicSign(true));
            c.VerifyDiagnostics(
                // warning CS7103: Attribute 'System.Reflection.AssemblyKeyFileAttribute' is ignored when public signing is specified.
                Diagnostic(ErrorCode.WRN_AttributeIgnoredWhenPublicSigning).WithArguments("System.Reflection.AssemblyKeyFileAttribute").WithLocation(1, 1),
                // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
                Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1)
            );

            Assert.True(c.Options.PublicSign);
        }

        [Fact, WorkItem(9150, "https://github.com/dotnet/roslyn/issues/9150")]
        public void KeyContainerFromAttributes_PublicSign()
        {
            string source = @"
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class C {}
";
            var c = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll.WithPublicSign(true));
            c.VerifyDiagnostics(
                // warning CS7103: Attribute 'System.Reflection.AssemblyKeyNameAttribute' is ignored when public signing is specified.
                Diagnostic(ErrorCode.WRN_AttributeIgnoredWhenPublicSigning).WithArguments("System.Reflection.AssemblyKeyNameAttribute").WithLocation(1, 1),
                // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
                Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1)
            );

            Assert.True(c.Options.PublicSign);
        }

        private void VerifySignedBitSetAfterEmit(Compilation comp)
        {
            var outStrm = new MemoryStream();
            var emitResult = comp.Emit(outStrm);
            Assert.True(emitResult.Success);

            outStrm.Position = 0;

            // Verify that the sign bit is set
            using (var reader = new PEReader(outStrm))
            {
                Assert.True(reader.HasMetadata);

                var flags = reader.PEHeaders.CorHeader.Flags;
                Assert.True(flags.HasFlag(CorFlags.StrongNameSigned));
            }
        }

        [Fact]
        public void MaxSizeKey()
        {
            var pubKey = "002400000480000014080000060200000024000052534131004000000100010079bb5332224912" +
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

            var comp = CreateCompilationWithMscorlib($@"
using System;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo(""MaxSizeComp2, PublicKey={pubKey}, PublicKeyToken=1540923db30520b2"")]

internal class C
{{
    public static void M()
    {{
        Console.WriteLine(""Called M"");
    }}
}}",
                options: TestOptions.ReleaseDll
                         .WithCryptoKeyFile(SigningTestHelpers.MaxSizeKeyFile)
                         .WithStrongNameProvider(s_defaultProvider));

            Assert.True(comp.IsRealSigned);
            VerifySignedBitSetAfterEmit(comp);

            var comp2 = CreateCompilationWithMscorlib(@"
class D
{
    public static void Main()
    {
        C.M();
    }
}", 
references: new[] { comp.ToMetadataReference() },
assemblyName: "MaxSizeComp2",
options: TestOptions.ReleaseExe
        .WithCryptoKeyFile(SigningTestHelpers.MaxSizeKeyFile)
        .WithStrongNameProvider(s_defaultProvider));

            CompileAndVerify(comp2, expectedOutput: "Called M");
        }

        [Fact]
        public void SnkFile_PublicSign()
        {
            var snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey);

            var comp = CreateCompilationWithMscorlib("public class C{}",
                options: TestOptions.ReleaseDll
                    .WithCryptoKeyFile(snk.Path)
                    .WithPublicSign(true));

            comp.VerifyDiagnostics();

            Assert.True(comp.Options.PublicSign);
            Assert.Null(comp.Options.DelaySign);
            Assert.False(comp.IsRealSigned);
            Assert.NotNull(comp.Options.CryptoKeyFile);

            VerifySignedBitSetAfterEmit(comp);
        }

        [Fact]
        public void PublicKeyFile_PublicSign()
        {
            var pubKeyFile = Temp.CreateFile().WriteAllBytes(TestResources.General.snPublicKey);

            var comp = CreateCompilationWithMscorlib("public class C {}",
                options: TestOptions.ReleaseDll
                    .WithCryptoKeyFile(pubKeyFile.Path)
                    .WithPublicSign(true));

            comp.VerifyDiagnostics();

            Assert.True(comp.Options.PublicSign);
            Assert.Null(comp.Options.DelaySign);
            Assert.False(comp.IsRealSigned);
            Assert.NotNull(comp.Options.CryptoKeyFile);

            VerifySignedBitSetAfterEmit(comp);
        }

        [Fact]
        public void PublicSign_DelaySignAttribute()
        {
            var pubKeyFile = Temp.CreateFile().WriteAllBytes(TestResources.General.snPublicKey);

            var comp = CreateCompilationWithMscorlib(@"
[assembly: System.Reflection.AssemblyDelaySign(true)]
public class C {}",
                options: TestOptions.ReleaseDll
                    .WithCryptoKeyFile(pubKeyFile.Path)
                    .WithPublicSign(true));

            comp.VerifyDiagnostics(
    // warning CS1616: Option 'PublicSign' overrides attribute 'System.Reflection.AssemblyDelaySignAttribute' given in a source file or added module
    Diagnostic(ErrorCode.WRN_CmdOptionConflictsSource).WithArguments("PublicSign", "System.Reflection.AssemblyDelaySignAttribute").WithLocation(1, 1));

            Assert.True(comp.Options.PublicSign);
            Assert.Null(comp.Options.DelaySign);
            Assert.False(comp.IsRealSigned);
            Assert.NotNull(comp.Options.CryptoKeyFile);

            VerifySignedBitSetAfterEmit(comp);
        }

        [Fact]
        public void KeyContainerNoSNProvider_PublicSign()
        {
            var comp = CreateCompilationWithMscorlib("public class C {}",
                options: TestOptions.ReleaseDll
                    .WithCryptoKeyContainer("roslynTestContainer")
                    .WithPublicSign(true));

            comp.VerifyDiagnostics(
    // error CS7102: Compilation options 'PublicSign' and 'CryptoKeyContainer' can't both be specified at the same time.
    Diagnostic(ErrorCode.ERR_MutuallyExclusiveOptions).WithArguments("PublicSign", "CryptoKeyContainer").WithLocation(1, 1),
    // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
    Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1));
        }

        [Fact]
        public void KeyContainerDesktopProvider_PublicSign()
        {
            var comp = CreateCompilationWithMscorlib("public class C {}",
                options: TestOptions.ReleaseDll
                    .WithCryptoKeyContainer("roslynTestContainer")
                    .WithStrongNameProvider(s_defaultProvider)
                    .WithPublicSign(true));

            comp.VerifyDiagnostics(
    // error CS7102: Compilation options 'PublicSign' and 'CryptoKeyContainer' can't both be specified at the same time.
    Diagnostic(ErrorCode.ERR_MutuallyExclusiveOptions).WithArguments("PublicSign", "CryptoKeyContainer").WithLocation(1, 1),
    // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
    Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1));

            Assert.True(comp.Options.PublicSign);
            Assert.Null(comp.Options.DelaySign);
            Assert.False(comp.IsRealSigned);
            Assert.NotNull(comp.Options.CryptoKeyContainer);
        }

        [Fact]
        public void PublicSignAndDelaySign()
        {
            var snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey);

            var comp = CreateCompilationWithMscorlib("public class C {}",
                options: TestOptions.ReleaseDll
                    .WithPublicSign(true)
                    .WithDelaySign(true)
                    .WithCryptoKeyFile(snk.Path));

            comp.VerifyDiagnostics(
    // error CS7102: Compilation options 'PublicSign' and 'DelaySign' can't both be specified at the same time.
    Diagnostic(ErrorCode.ERR_MutuallyExclusiveOptions).WithArguments("PublicSign", "DelaySign").WithLocation(1, 1));

            Assert.True(comp.Options.PublicSign);
            Assert.True(comp.Options.DelaySign);
        }

        [Fact]
        public void PublicSignAndDelaySignFalse()
        {
            var snk = Temp.CreateFile().WriteAllBytes(TestResources.General.snKey);

            var comp = CreateCompilationWithMscorlib("public class C {}",
                options: TestOptions.ReleaseDll
                    .WithPublicSign(true)
                    .WithDelaySign(false)
                    .WithCryptoKeyFile(snk.Path));

            comp.VerifyDiagnostics();

            Assert.True(comp.Options.PublicSign);
            Assert.False(comp.Options.DelaySign);
        }

        [Fact]
        public void PublicSignNoKey()
        {
            var comp = CreateCompilationWithMscorlib("public class C {}",
                options: TestOptions.ReleaseDll
                    .WithPublicSign(true));

            comp.VerifyDiagnostics(
    // error CS8102: Public signing was specified and requires a public key, but no public key was specified.
    Diagnostic(ErrorCode.ERR_PublicSignButNoKey).WithLocation(1, 1));
            Assert.True(comp.Options.PublicSign);
            Assert.True(comp.Assembly.PublicKey.IsDefaultOrEmpty);
        }

        [Fact]
        public void PublicKeyFromOptions_InvalidCompilationOptions()
        {
            string source = @"public class C {}";

            var c = CreateCompilationWithMscorlib(source, options: TestOptions.ReleaseDll.
                WithCryptoPublicKey(ImmutableArray.Create<byte>(1, 2, 3)).
                WithCryptoKeyContainer("roslynTestContainer").
                WithCryptoKeyFile("file.snk").
                WithStrongNameProvider(s_defaultProvider));

            c.VerifyDiagnostics(
                // error CS7102: Compilation options 'CryptoPublicKey' and 'CryptoKeyFile' can't both be specified at the same time.
                Diagnostic(ErrorCode.ERR_MutuallyExclusiveOptions).WithArguments("CryptoPublicKey", "CryptoKeyFile").WithLocation(1, 1),
                // error CS7102: Compilation options 'CryptoPublicKey' and 'CryptoKeyContainer' can't both be specified at the same time.
                Diagnostic(ErrorCode.ERR_MutuallyExclusiveOptions).WithArguments("CryptoPublicKey", "CryptoKeyContainer").WithLocation(1, 1),
                // error CS7088: Invalid 'CryptoPublicKey' value: '01-02-03'.
                Diagnostic(ErrorCode.ERR_BadCompilationOptionValue).WithArguments("CryptoPublicKey", "01-02-03").WithLocation(1, 1));
        }

        #endregion

        #region IVT Access Checking

        [Fact]
        public void IVTBasicCompilation()
        {
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""WantsIVTAccess"")]
            public class C { internal void Foo() {} }";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

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
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

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
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            Assert.Empty(c2.GetDiagnostics());
        }

        [Fact]
        public void IVTBasicMetadata()
        {
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""WantsIVTAccess"")]
            public class C { internal void Foo() {} }";

            var otherStream = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider)).EmitToStream();

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
            references: new[] { AssemblyMetadata.CreateFromStream(otherStream, leaveOpen: true).GetReference() },
            assemblyName: "WantsIVTAccessButCantHave",
            options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            //compilation should not succeed, and internals should not be imported.
            c.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Foo").WithArguments("C", "Foo"));

            otherStream.Position = 0;

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
                new[] { MetadataReference.CreateFromStream(otherStream) },
                assemblyName: "WantsIVTAccess",
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            Assert.Empty(c2.GetDiagnostics());
        }

        [Fact]
        public void IVTSigned()
        {
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            public class C { internal void Foo() {} }";

            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider),
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
                TestOptions.ReleaseDll.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(s_defaultProvider),
                assemblyName: "John");

            Assert.Empty(requestor.GetDiagnostics());
        }

        [Fact]
        public void IVTErrorNotBothSigned()
        {
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            public class C { internal void Foo() {} }";

            var other = CreateCompilationWithMscorlib(s, assemblyName: "Paul", options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));
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
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

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
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

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
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            Assert.True(ByteSequenceComparer.Equals(s_publicKey, requestor.Assembly.Identity.PublicKey));
            Assert.Empty(requestor.GetDiagnostics());
        }

        [Fact]
        public void IVTDeferredFailSignMismatch()
        {
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilationWithMscorlib(s,
                assemblyName: "Paul",
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider)); //not signed. cryptoKeyFile: KeyPairFile,

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
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            Assert.True(ByteSequenceComparer.Equals(s_publicKey, requestor.Assembly.Identity.PublicKey));
            requestor.VerifyDiagnostics(
                Diagnostic(ErrorCode.ERR_FriendRefSigningMismatch, arguments: new object[] { "Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null" }));
        }

        [Fact]
        public void IVTDeferredFailKeyMismatch()
        {
            //key is wrong in the first digit. correct key starts with 0
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=10240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider),
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
              options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            Assert.True(ByteSequenceComparer.Equals(s_publicKey, requestor.Assembly.Identity.PublicKey));
            requestor.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FriendRefNotEqualToThis, arguments: new object[] { "Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2" }));
        }

        [Fact]
        public void IVTSuccessThroughIAssembly()
        {
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            internal class CAttribute : System.Attribute { public CAttribute() {} }";

            var other = CreateCompilationWithMscorlib(s,
                assemblyName: "Paul",
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

            other.VerifyDiagnostics();

            var requestor = CreateCompilationWithMscorlib(
    @"
[assembly: C()]  //causes optimistic granting
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class A
{
}",
                new MetadataReference[] { new CSharpCompilationReference(other) },
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider),
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
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider),
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
                TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider),
                assemblyName: "John");

            Assert.False(((IAssemblySymbol)other.Assembly).GivesAccessTo(requestor.Assembly));
            requestor.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_FriendRefNotEqualToThis, arguments: new object[] { "Paul, Version=0.0.0.0, Culture=neutral, PublicKeyToken=ce65828c82a341f2" }));
        }

        [WorkItem(820450, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/820450")]
        [Fact]
        public void IVTGivesAccessToUsingDifferentKeys()
        {
            string s = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""John, PublicKey=00240000048000009400000006020000002400005253413100040000010001002b986f6b5ea5717d35c72d38561f413e267029efa9b5f107b9331d83df657381325b3a67b75812f63a9436ceccb49494de8f574f8e639d4d26c0fcf8b0e9a1a196b80b6f6ed053628d10d027e032df2ed1d60835e5f47d32c9ef6da10d0366a319573362c821b5f8fa5abc5bb22241de6f666a85d82d6ba8c3090d01636bd2bb"")]
            namespace ClassLibrary1 { internal class Class1 { } } ";

            var giver = CreateCompilationWithMscorlib(s,
                assemblyName: "Paul",
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(SigningTestHelpers.KeyPairFile2).WithStrongNameProvider(s_defaultProvider));

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
                options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider),
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
", options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

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
", options: TestOptions.ReleaseExe.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

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

        private void ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(MemoryStream moduleContents, AttributeDescription expectedModuleAttr)
        {
            //a module doesn't get signed for real. It should have either a keyfile or keycontainer attribute
            //parked on a typeRef named 'AssemblyAttributesGoHere.' When the module is added to an assembly, the
            //resulting assembly is signed with the key referred to by the aforementioned attribute.

            EmitResult success;
            var tempFile = Temp.CreateFile();
            moduleContents.Position = 0;

            using (var metadata = ModuleMetadata.CreateFromStream(moduleContents))
            {
                var flags = metadata.Module.PEReaderOpt.PEHeaders.CorHeader.Flags;
                //confirm file does not claim to be signed
                Assert.Equal(0, (int)(flags & CorFlags.StrongNameSigned));
                EntityHandle token = metadata.Module.GetTypeRef(metadata.Module.GetAssemblyRef("mscorlib"), "System.Runtime.CompilerServices", "AssemblyAttributesGoHere");
                Assert.False(token.IsNil);   //could the type ref be located? If not then the attribute's not there.
                var attrInfos = metadata.Module.FindTargetAttributes(token, expectedModuleAttr);
                Assert.Equal(1, attrInfos.Count());

                var source = @"
public class Z
{
}";

                //now that the module checks out, ensure that adding it to a compilation outputting a dll
                //results in a signed assembly.
                var assemblyComp = CreateCompilationWithMscorlib(source,
                    new[] { metadata.GetReference() },
                    TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

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
            var x = s_keyPairFile;
            string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", x, @""")] public class C {}");

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var success = other.Emit(outStrm);
            Assert.True(success.Success);

            ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute);
        }

        [Fact]
        public void SignModuleKeyContainerAttr()
        {
            string s = @"[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var success = other.Emit(outStrm);
            Assert.True(success.Success);

            ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute);
        }

        [WorkItem(5665, "https://github.com/dotnet/roslyn/issues/5665")]
        [ConditionalFact(typeof(IsEnglishLocal))]
        public void SignModuleKeyContainerBogus()
        {
            string s = @"[assembly: System.Reflection.AssemblyKeyName(""bogus"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithStrongNameProvider(s_defaultProvider));
            //shouldn't have an error. The attribute's contents are checked when the module is added.
            var reference = other.EmitToImageReference();

            s = @"class D {}";

            other = CreateCompilationWithMscorlib(s, new[] { reference }, TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            // error CS7028: Error signing output with public key from container 'bogus' -- Keyset does not exist (Exception from HRESULT: 0x80090016)
            var err = other.GetDiagnostics().Single();

            Assert.Equal((int)ErrorCode.ERR_PublicKeyContainerFailure, err.Code);
            Assert.Equal(2, err.Arguments.Count);
            Assert.Equal("bogus", err.Arguments[0]);
            Assert.True(((string)err.Arguments[1]).EndsWith(" HRESULT: 0x80090016)", StringComparison.Ordinal));
        }

        [Fact]
        public void SignModuleKeyFileBogus()
        {
            string s = @"[assembly: System.Reflection.AssemblyKeyFile(""bogus"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithStrongNameProvider(s_defaultProvider));

            //shouldn't have an error. The attribute's contents are checked when the module is added.
            var reference = other.EmitToImageReference();

            s = @"class D {}";

            other = CreateCompilationWithMscorlib(s, new[] { reference }, TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));
            other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_PublicKeyFileFailure).WithArguments("bogus", CodeAnalysisResources.FileNotFound));
        }

        [WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")]
        [Fact()]
        public void SignModuleKeyContainerCmdLine()
        {
            string s = "public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var success = other.Emit(outStrm);
            Assert.True(success.Success);

            ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute);
        }

        [WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")]
        [Fact()]
        public void SignModuleKeyContainerCmdLine_1()
        {
            string s = @"
[assembly: System.Reflection.AssemblyKeyName(""roslynTestContainer"")]
public class C {}";

            var other = CreateCompilationWithMscorlib(s,
                options: TestOptions.ReleaseModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var success = other.Emit(outStrm);
            Assert.True(success.Success);

            ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyNameAttribute);
        }

        [WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")]
        [Fact()]
        public void SignModuleKeyContainerCmdLine_2()
        {
            string s = @"
[assembly: System.Reflection.AssemblyKeyName(""bogus"")]
public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithCryptoKeyContainer("roslynTestContainer").WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var success = other.Emit(outStrm);
            Assert.False(success.Success);
            success.Diagnostics.Verify(
        // error CS7091: Attribute 'System.Reflection.AssemblyKeyNameAttribute' given in a source file conflicts with option 'CryptoKeyContainer'.
        Diagnostic(ErrorCode.ERR_CmdOptionConflictsSource).WithArguments("System.Reflection.AssemblyKeyNameAttribute", "CryptoKeyContainer")
                );
        }

        [WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")]
        [Fact()]
        public void SignModuleKeyFileCmdLine()
        {
            string s = "public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var success = other.Emit(outStrm);
            Assert.True(success.Success);

            ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute);
        }

        [WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")]
        [Fact()]
        public void SignModuleKeyFileCmdLine_1()
        {
            var x = s_keyPairFile;
            string s = String.Format("{0}{1}{2}", @"[assembly: System.Reflection.AssemblyKeyFile(@""", x, @""")] public class C {}");

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var success = other.Emit(outStrm);
            Assert.True(success.Success);

            ConfirmModuleAttributePresentAndAddingToAssemblyResultsInSignedOutput(outStrm, AttributeDescription.AssemblyKeyFileAttribute);
        }

        [WorkItem(531195, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/531195")]
        [Fact()]
        public void SignModuleKeyFileCmdLine_2()
        {
            var x = s_keyPairFile;
            string s = @"[assembly: System.Reflection.AssemblyKeyFile(""bogus"")] public class C {}";

            var other = CreateCompilationWithMscorlib(s, options: TestOptions.ReleaseModule.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithStrongNameProvider(s_defaultProvider));

            var outStrm = new MemoryStream();
            var emitResult = other.Emit(outStrm);
            other.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_SignButNoPrivateKey).WithArguments(s_publicKeyFile));

            other = other.WithOptions(TestOptions.ReleaseModule.WithCryptoKeyFile(s_publicKeyFile));

            var assembly = CreateCompilationWithMscorlib("",
                references: new[] { other.EmitToImageReference() },
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

            assembly.VerifyDiagnostics(Diagnostic(ErrorCode.ERR_SignButNoPrivateKey).WithArguments(s_publicKeyFile));
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
}", options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithStrongNameProvider(s_defaultProvider));

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
      options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));

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
    options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));
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
}", options: TestOptions.ReleaseDll.WithDelaySign(false).WithStrongNameProvider(s_defaultProvider));

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
}", options: TestOptions.ReleaseDll.WithDelaySign(true).WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

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
                options: TestOptions.ReleaseDll.WithDelaySign(true).WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

            using (var metadata = ModuleMetadata.CreateFromImage(other.EmitToArray()))
            {
                var header = metadata.Module.PEReaderOpt.PEHeaders.CorHeader;
                //confirm header has expected SN signature size
                Assert.Equal(256, header.StrongNameSignatureDirectory.Size);
            }
        }

        [WorkItem(545720, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/545720")]
        [WorkItem(530050, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/530050")]
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

            var comp = CreateCompilationWithMscorlib(csharp, new[] { ilRef }, assemblyName: "asm2", options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider));
            comp.VerifyDiagnostics(
                // NOTE: dev10 reports WRN_InvalidAssemblyName, but Roslyn won't (DevDiv #15099).

                // (2,17): error CS0122: 'Base' is inaccessible due to its protection level
                // class Derived : Base 
                Diagnostic(ErrorCode.ERR_BadAccess, "Base").WithArguments("Base"));
        }

        [WorkItem(546331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546331")]
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

            var comp1 = CreateCompilationWithMscorlib(source1, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "asm1");
            comp1.VerifyDiagnostics();
            var ref1 = new CSharpCompilationReference(comp1);

            var comp2 = CreateCompilationWithMscorlib(source2, new[] { ref1 }, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "asm2");
            comp2.VerifyDiagnostics();
            var ref2 = new CSharpCompilationReference(comp2);

            var comp3 = CreateCompilationWithMscorlib(source3, new[] { SystemCoreRef, ref1, ref2 }, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "asm3");
            comp3.VerifyDiagnostics();

            // Note: calls B.M, not A.M, since asm1 is not accessible.
            var verifier = CompileAndVerify(comp3);

            verifier.VerifyIL("C.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  newobj     ""C..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""void B.M()""
  IL_000b:  dup
  IL_000c:  callvirt   ""int B.P.get""
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  callvirt   ""void B.E.add""
  IL_0018:  ret
}");

            verifier.VerifyIL("C.TestET", @"
{
  // Code size       85 (0x55)
  .maxstack  3
  IL_0000:  newobj     ""C.<>c__DisplayClass2_0..ctor()""
  IL_0005:  dup
  IL_0006:  newobj     ""C..ctor()""
  IL_000b:  stfld      ""C C.<>c__DisplayClass2_0.c""
  IL_0010:  ldtoken    ""C.<>c__DisplayClass2_0""
  IL_0015:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001a:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_001f:  ldtoken    ""C C.<>c__DisplayClass2_0.c""
  IL_0024:  call       ""System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)""
  IL_0029:  call       ""System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)""
  IL_002e:  ldtoken    ""void B.M()""
  IL_0033:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0038:  castclass  ""System.Reflection.MethodInfo""
  IL_003d:  ldc.i4.0
  IL_003e:  newarr     ""System.Linq.Expressions.Expression""
  IL_0043:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_0048:  ldc.i4.0
  IL_0049:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_004e:  call       ""System.Linq.Expressions.Expression<System.Action> System.Linq.Expressions.Expression.Lambda<System.Action>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0053:  pop
  IL_0054:  ret
}
");
        }

        [WorkItem(546331, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/546331")]
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

            var comp1 = CreateCompilationWithMscorlib(source1, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "asm1");
            comp1.VerifyDiagnostics();
            var ref1 = new CSharpCompilationReference(comp1);

            var comp2 = CreateCompilationWithMscorlib(source2, new[] { ref1 }, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "asm2");
            comp2.VerifyDiagnostics();
            var ref2 = new CSharpCompilationReference(comp2);

            var comp3 = CreateCompilationWithMscorlib(source3, new[] { ref1, ref2 }, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "asm3");
            comp3.VerifyDiagnostics();
            var ref3 = new CSharpCompilationReference(comp3);

            var comp4 = CreateCompilationWithMscorlib(source4, new[] { SystemCoreRef, ref1, ref2, ref3 }, options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "asm4");
            comp4.VerifyDiagnostics();

            // Note: calls C.M, not A.M, since asm2 is not accessible (stops search).
            // Confirmed in Dev11.
            var verifier = CompileAndVerify(comp4);

            verifier.VerifyIL("D.Test", @"
{
  // Code size       25 (0x19)
  .maxstack  2
  IL_0000:  newobj     ""D..ctor()""
  IL_0005:  dup
  IL_0006:  callvirt   ""void C.M()""
  IL_000b:  dup
  IL_000c:  callvirt   ""int C.P.get""
  IL_0011:  pop
  IL_0012:  ldnull
  IL_0013:  callvirt   ""void C.E.add""
  IL_0018:  ret
}");

            verifier.VerifyIL("D.TestET", @"
{
  // Code size       85 (0x55)
  .maxstack  3
  IL_0000:  newobj     ""D.<>c__DisplayClass2_0..ctor()""
  IL_0005:  dup
  IL_0006:  newobj     ""D..ctor()""
  IL_000b:  stfld      ""D D.<>c__DisplayClass2_0.d""
  IL_0010:  ldtoken    ""D.<>c__DisplayClass2_0""
  IL_0015:  call       ""System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)""
  IL_001a:  call       ""System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)""
  IL_001f:  ldtoken    ""D D.<>c__DisplayClass2_0.d""
  IL_0024:  call       ""System.Reflection.FieldInfo System.Reflection.FieldInfo.GetFieldFromHandle(System.RuntimeFieldHandle)""
  IL_0029:  call       ""System.Linq.Expressions.MemberExpression System.Linq.Expressions.Expression.Field(System.Linq.Expressions.Expression, System.Reflection.FieldInfo)""
  IL_002e:  ldtoken    ""void C.M()""
  IL_0033:  call       ""System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)""
  IL_0038:  castclass  ""System.Reflection.MethodInfo""
  IL_003d:  ldc.i4.0
  IL_003e:  newarr     ""System.Linq.Expressions.Expression""
  IL_0043:  call       ""System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])""
  IL_0048:  ldc.i4.0
  IL_0049:  newarr     ""System.Linq.Expressions.ParameterExpression""
  IL_004e:  call       ""System.Linq.Expressions.Expression<System.Action> System.Linq.Expressions.Expression.Lambda<System.Action>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])""
  IL_0053:  pop
  IL_0054:  ret
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
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider),
                assemblyName: "asm1");

            comp1.VerifyDiagnostics();
            var ref1 = new CSharpCompilationReference(comp1);

            var ref2 = CompileIL(source2, appendDefaultHeader: false);

            var comp3 = CreateCompilationWithMscorlib(source3,
                new[] { SystemCoreRef, ref1, ref2 },
                options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider),
                assemblyName: "asm3");

            comp3.VerifyDiagnostics(
                // (7,9): error CS7036: There is no argument given that corresponds to the required formal parameter 'a' of 'B.F(int[])'
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "F").WithArguments("a", "B.F(int[])").WithLocation(7, 11),
                // (8,20): error CS1061: 'object' does not contain a definition for 'Bar' and no extension method 'Bar' accepting a first argument of type 'object' could be found (are you missing a using directive or an assembly reference?)
                Diagnostic(ErrorCode.ERR_NoSuchMemberOrExtension, "Bar").WithArguments("object", "Bar").WithLocation(8, 20),
                // (10,17): error CS7036: There is no argument given that corresponds to the required formal parameter 'a' of 'B.this[int, int[]]'
                Diagnostic(ErrorCode.ERR_NoCorrespondingArgument, "c[1]").WithArguments("a", "B.this[int, int[]]").WithLocation(10, 17));
        }

        [Fact]
        [WorkItem(529779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529779")]
        public void Bug529779_1()
        {
            CSharpCompilation unsigned = CreateCompilationWithMscorlib(
    @"
public class C1
{}
", options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "Unsigned");

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
", options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider));

            CompileAndVerify(other.WithReferences(new[] { other.References.ElementAt(0), new CSharpCompilationReference(unsigned) })).VerifyDiagnostics();

            CompileAndVerify(other.WithReferences(new[] { other.References.ElementAt(0), MetadataReference.CreateFromStream(unsigned.EmitToStream()) })).VerifyDiagnostics();
        }

        [Fact]
        [WorkItem(529779, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529779")]
        public void Bug529779_2()
        {
            CSharpCompilation unsigned = CreateCompilationWithMscorlib(
    @"
public class C1
{}
", options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "Unsigned");

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
", options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider).WithCryptoKeyFile(s_keyPairFile));

            var comps = new[] {other.WithReferences(new []{other.References.ElementAt(0), new CSharpCompilationReference(unsigned)}),
                            other.WithReferences(new []{other.References.ElementAt(0), MetadataReference.CreateFromStream(unsigned.EmitToStream()) })};

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
        [WorkItem(399, "https://github.com/dotnet/roslyn/issues/399")]
        public void Bug399()
        {
            // The referenced assembly Signed.dll from the repro steps
            var signed = CreateMetadataReferenceFromHexGZipImage(@"
1f8b0800000000000400f38d9ac0c0ccc0c0c002c4ffff3330ec6080000706c2a00188f9e477f1316ce13cabb883d1e7ac62
484666b14241517e7a5162ae4272625e5e7e894252aa4251699e42669e828b7fb0426e7e4aaa1e2f2f970ad48c005706061f
4626869e0db74260e63e606052e466e486388a0922f64f094828c01d26006633419430302068860488f8790f06a0bf1c5a41
4a410841c32930580334d71f9f2781f6f11011161800e83e0e242e0790ef81c4d72b49ad2801b99b19a216d9af484624e815
a5e6e42743dde00055c386e14427729c08020f9420b407d86856860b404bef30323070a2a90b5080c4372120f781b1f3ada8
5ec1078b0a8f606f87dacdfeae3b162edb7de055d1af12c942bde5a267ef37e6c6b787945936b0ece367e8f6f87566c6f7bd
46a67f5da4f50d2f8a7e95e159552d1bf747b3ccdae1679c666dd10626bb1bf9815ad1c1876d04a76d78163e4b32a8a77fb3
a77adbec4d15c75de79cd9a3a4a5155dd1fc50b86ce5bd7797cce73b057b3931323082dd020ab332133d033d630363434b90
082b430e90ac0162e53a06861740da00c40e2e29cacc4b2f06a9906084c49b72683083022324ad28bb877aba80d402f96b40
7ca79cfc24a87f81d1c1c8ca000daf5faac60c620c60db41d1c408c50c0c5c509a012e0272e3425078c1792c0d0c48aa407a
d41890d2355895288263e39b9f529a936ac7109c999e979aa2979293c3905b9c9c5f949399c4e0091184ca81d5332b80a9a9
8764e24b67aff2dff0feb1f6c7b7e6d50c1cdbab62c2244d1e74362c6000664a902ba600d5b1813c00e407053b1a821c0172
e1dddd9665aa576abb26acf9f6e2eaeaab7527ed1f49174726fc8f395ad7c676f650da9c159bbcd6a73cd031d8a9762d8d6b
47f9eac4955b0566f61fbc9010e4bbf0c405d6e6cc8392f63e6f4bc5339f2d9bb9725d79c0d5cecbacacc9af4522debeb30a
bebd207fe9963cbbe995f66bb227ac4c0cfd91c3dce095617a66ce0e9d0b9e8eae9b25965c514278ff1dac3cc0021e2821f3
e29df38b5c72727c1333f32001949a0a0e2c10f8af0a344300ab2123052840cb16e30176c72818100000c85fc49900080000", filePath: "Signed.dll");

            var compilation = CreateCompilationWithMscorlib(
                "interface IDerived : ISigned { }",
                references: new[] { signed },
                options: TestOptions.ReleaseDll
                    .WithGeneralDiagnosticOption(ReportDiagnostic.Error)
                    .WithStrongNameProvider(s_defaultProvider)
                    .WithCryptoKeyFile(s_keyPairFile));

            // ACTUAL: error CS8002: Referenced assembly 'Signed, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null' does not have a strong name.
            // EXPECTED: no errors
            compilation.VerifyEmitDiagnostics();
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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

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

        [ConditionalFact(typeof(IsEnglishLocal))]
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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_keyPairFile).WithStrongNameProvider(s_defaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(true).WithStrongNameProvider(s_defaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(true).WithStrongNameProvider(s_defaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(true).WithStrongNameProvider(s_defaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

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
      options: TestOptions.ReleaseDll.WithCryptoKeyFile(s_publicKeyFile).WithDelaySign(true).WithStrongNameProvider(s_defaultProvider), references: new[] { MscorlibRef_v4_0_30316_17626 });

            var tempFile = Temp.CreateFile();

            using (var outStrm = tempFile.Open())
            {
                var success = other.Emit(outStrm);
                Assert.True(success.Success);
            }
        }

        [Fact, WorkItem(769840, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/769840")]
        public void Bug769840()
        {
            var ca = CreateCompilationWithMscorlib(
    @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""Bug769840_B, PublicKey = 0024000004800000940000000602000000240000525341310004000001000100458a131798af87d9e33088a3ab1c6101cbd462760f023d4f41d97f691033649e60b42001e94f4d79386b5e087b0a044c54b7afce151b3ad19b33b332b83087e3b8b022f45b5e4ff9b9a1077b0572ff0679ce38f884c7bd3d9b4090e4a7ee086b7dd292dc20f81a3b1b8a0b67ee77023131e59831c709c81d11c6856669974cc4"")]

internal class A
{
    public int Value = 3;
}
", options: TestOptions.ReleaseDll.WithStrongNameProvider(s_defaultProvider), assemblyName: "Bug769840_A");

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
                options: TestOptions.ReleaseModule.WithStrongNameProvider(s_defaultProvider),
                assemblyName: "Bug769840_B",
                references: new[] { new CSharpCompilationReference(ca) });

            CompileAndVerify(cb, verify: false).Diagnostics.Verify();
        }

        [Fact, WorkItem(1072350, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072350")]
        public void Bug1072350()
        {
            const string sourceA = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""X "")]
internal class A
{
    internal static int I = 42;
}";

            const string sourceB = @"
class B
{
    static void Main()
    {
        System.Console.Write(A.I);
    }
}";

            var ca = CreateCompilationWithMscorlib(sourceA, options: TestOptions.ReleaseDll, assemblyName: "ClassLibrary2");
            CompileAndVerify(ca);

            var cb = CreateCompilationWithMscorlib(sourceB, options: TestOptions.ReleaseExe, assemblyName: "X", references: new[] { new CSharpCompilationReference(ca) });
            CompileAndVerify(cb, expectedOutput: "42").Diagnostics.Verify();
        }

        [Fact, WorkItem(1072339, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1072339")]
        public void Bug1072339()
        {
            const string sourceA = @"
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""x"")]
internal class A
{
    internal static int I = 42;
}";

            const string sourceB = @"
class B
{
    static void Main()
    {
        System.Console.Write(A.I);
    }
}";

            var ca = CreateCompilationWithMscorlib(sourceA, options: TestOptions.ReleaseDll, assemblyName: "ClassLibrary2");
            CompileAndVerify(ca);

            var cb = CreateCompilationWithMscorlib(sourceB, options: TestOptions.ReleaseExe, assemblyName: "X", references: new[] { new CSharpCompilationReference(ca) });
            CompileAndVerify(cb, expectedOutput: "42").Diagnostics.Verify();
        }

        [Fact, WorkItem(1095618, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/1095618")]
        public void Bug1095618()
        {
            const string source = @"[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(""System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000"")]";

            var ca = CreateCompilationWithMscorlib(source);
            ca.VerifyDiagnostics(
                // (1,12): warning CS1700: Assembly reference 'System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000' is invalid and cannot be resolved
                // [assembly: System.Runtime.CompilerServices.InternalsVisibleTo("System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000")]
                Diagnostic(ErrorCode.WRN_InvalidAssemblyName, @"System.Runtime.CompilerServices.InternalsVisibleTo(""System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000"")").WithArguments("System.Runtime.Serialization, PublicKey = 10000000000000000400000000000000").WithLocation(1, 12));

            var verifier = CompileAndVerify(ca, symbolValidator: module =>
            {
                var assembly = module.ContainingAssembly;
                Assert.NotNull(assembly);
                Assert.False(assembly.GetAttributes().Any(attr => attr.IsTargetAttribute(assembly, AttributeDescription.InternalsVisibleToAttribute)));
            });
        }

        #endregion
    }
}