// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Xunit;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit.Abstractions;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Parsing
{
    [CompilerTrait(CompilerFeature.ReadOnlyReferences)]
    public class ReadOnlyStructs : ParsingTests
    {
        public ReadOnlyStructs(ITestOutputHelper output) : base(output) { }

        protected override SyntaxTree ParseTree(string text, CSharpParseOptions options)
        {
            return SyntaxFactory.ParseSyntaxTree(text, options: options);
        }

        [Fact]
        public void ReadOnlyStructSimple()
        {
            var text = @"
class Program
{
    readonly struct S1{}

    public readonly struct S2{}

    readonly public struct S3{}
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
            );


            var s1 = comp.GetTypeByMetadataName("Program+S1");
            Assert.False(s1.IsRefLikeType);
            Assert.True(s1.IsReadOnly);
            Assert.Equal(Accessibility.Private, s1.DeclaredAccessibility);
            Assert.Equal(TypeKind.Struct, s1.TypeKind);

            var s2 = comp.GetTypeByMetadataName("Program+S2");
            Assert.False(s2.IsRefLikeType);
            Assert.True(s2.IsReadOnly);
            Assert.Equal(Accessibility.Public, s2.DeclaredAccessibility);
            Assert.Equal(TypeKind.Struct, s2.TypeKind);

            var s3 = comp.GetTypeByMetadataName("Program+S3");
            Assert.False(s3.IsRefLikeType);
            Assert.True(s3.IsReadOnly);
            Assert.Equal(Accessibility.Public, s3.DeclaredAccessibility);
            Assert.Equal(TypeKind.Struct, s3.TypeKind);
        }

        [Fact]
        public void ReadOnlyStructSimpleLangVer()
        {
            var text = @"
class Program
{
    readonly struct S1{}

    public readonly struct S2{}

    readonly public struct S3{}
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.CSharp7), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (4,5): error CS8107: Feature 'readonly structs' is not available in C# 7. Please use language version 7.2 or greater.
                //     readonly struct S1{}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "readonly").WithArguments("readonly structs", "7.2").WithLocation(4, 5),
                // (6,12): error CS8107: Feature 'readonly structs' is not available in C# 7. Please use language version 7.2 or greater.
                //     public readonly struct S2{}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "readonly").WithArguments("readonly structs", "7.2").WithLocation(6, 12),
                // (8,5): error CS8107: Feature 'readonly structs' is not available in C# 7. Please use language version 7.2 or greater.
                //     readonly public struct S3{}
                Diagnostic(ErrorCode.ERR_FeatureNotAvailableInVersion7, "readonly").WithArguments("readonly structs", "7.2").WithLocation(8, 5)
            );
        }

        [Fact]
        public void ReadOnlyClassErr()
        {
            var text = @"
class Program
{
    readonly class S1{}

    public readonly delegate ref readonly int S2();

    readonly public interface S3{}
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
                // (4,20): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly class S1{}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S1").WithArguments("readonly").WithLocation(4, 20),
                // (6,47): error CS0106: The modifier 'readonly' is not valid for this item
                //     public readonly delegate ref readonly int S2();
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S2").WithArguments("readonly").WithLocation(6, 47),
                // (8,31): error CS0106: The modifier 'readonly' is not valid for this item
                //     readonly public interface S3{}
                Diagnostic(ErrorCode.ERR_BadMemberFlag, "S3").WithArguments("readonly").WithLocation(8, 31)
            );

            var s1 = comp.GetTypeByMetadataName("Program+S1");
            Assert.False(s1.IsRefLikeType);
            Assert.False(s1.IsReadOnly);
            Assert.Equal(Accessibility.Private, s1.DeclaredAccessibility);
            Assert.Equal(TypeKind.Class, s1.TypeKind);

            var s2 = comp.GetTypeByMetadataName("Program+S2");
            Assert.False(s2.IsRefLikeType);
            Assert.False(s2.IsReadOnly);
            Assert.Equal(Accessibility.Public, s2.DeclaredAccessibility);
            Assert.Equal(TypeKind.Delegate, s2.TypeKind);

            var s3 = comp.GetTypeByMetadataName("Program+S3");
            Assert.False(s3.IsRefLikeType);
            Assert.False(s3.IsReadOnly);
            Assert.Equal(Accessibility.Public, s3.DeclaredAccessibility);
            Assert.Equal(TypeKind.Interface, s3.TypeKind);
        }

        [Fact]
        public void ReadOnlyRefStruct()
        {
            var text = @"
class Program
{
    readonly ref struct S1{}

    unsafe readonly public ref struct S2{}
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll.WithAllowUnsafe(true));
            comp.VerifyDiagnostics(
            );

            var s1 = comp.GetTypeByMetadataName("Program+S1");
            Assert.True(s1.IsRefLikeType);
            Assert.True(s1.IsReadOnly);
            Assert.Equal(Accessibility.Private, s1.DeclaredAccessibility);
            Assert.Equal(TypeKind.Struct, s1.TypeKind);

            var s2 = comp.GetTypeByMetadataName("Program+S2");
            Assert.True(s2.IsRefLikeType);
            Assert.True(s2.IsReadOnly);
            Assert.Equal(Accessibility.Public, s2.DeclaredAccessibility);
            Assert.Equal(TypeKind.Struct, s2.TypeKind);
        }

        [Fact]
        public void ReadOnlyStructPartialMatchingModifiers()
        {
            var text = @"
class Program
{
    readonly partial struct S1{}

    readonly partial struct S1{}

    readonly ref partial struct S2{}

    readonly ref partial struct S2{}
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
            );

            var s1 = comp.GetTypeByMetadataName("Program+S1");
            Assert.False(s1.IsRefLikeType);
            Assert.True(s1.IsReadOnly);

            var s2 = comp.GetTypeByMetadataName("Program+S2");
            Assert.True(s2.IsRefLikeType);
            Assert.True(s2.IsReadOnly);
        }

        [WorkItem(19808, "https://github.com/dotnet/roslyn/issues/19808")]
        [Fact]
        public void ReadOnlyStructPartialNotMatchingModifiers()
        {
            var text = @"
class Program
{
    readonly partial struct S1{}

    readonly ref partial struct S1{}

    readonly partial struct S2{}

    partial struct S2{}

    readonly ref partial struct S3{}

    partial struct S3{}
}
";

            var comp = CreateCompilationWithMscorlib45(text, parseOptions: TestOptions.Regular.WithLanguageVersion(LanguageVersion.Latest), options: TestOptions.DebugDll);
            comp.VerifyDiagnostics(
            );

            var s1 = comp.GetTypeByMetadataName("Program+S1");
            Assert.True(s1.IsRefLikeType);
            Assert.True(s1.IsReadOnly);

            var s2 = comp.GetTypeByMetadataName("Program+S2");
            Assert.False(s2.IsRefLikeType);
            Assert.True(s2.IsReadOnly);

            var s3 = comp.GetTypeByMetadataName("Program+S3");
            Assert.True(s3.IsRefLikeType);
            Assert.True(s3.IsReadOnly);
        }
    }
}
