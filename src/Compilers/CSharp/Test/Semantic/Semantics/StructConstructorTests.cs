// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Semantics
{
    public class StructConstructorTests : CSharpTestBase
    {
        [CombinatorialData]
        [Theory]
        public void PublicParameterlessConstructor(bool useCompilationReference)
        {
            var sourceA =
@"public struct S
{
    public readonly bool Initialized;
    public S() { Initialized = true; }
}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (4,12): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     public S() { Initialized = true; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "S").WithArguments("parameterless struct constructors").WithLocation(4, 12));

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"using System;
class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static T CreateStruct<T>() where T : struct => new T();
    static void Main()
    {
        Console.WriteLine(new S().Initialized);
        Console.WriteLine(CreateNew<S>().Initialized);
        Console.WriteLine(CreateStruct<S>().Initialized);
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, expectedOutput:
$@"True
True
{ExecutionConditionUtil.IsCoreClr}"); // Activator.CreateInstance<T>() ignores constructor on desktop framework.
        }

        [InlineData("public")]
        [InlineData("internal")]
        [InlineData("private")]
        [Theory]
        public void PrivateConstructor_UseFromStruct(string accessibility)
        {
            var sourceA =
$@"public struct S
{{
    public readonly bool Initialized;
    {accessibility}
    S() {{ Initialized = true; }}
    public static S Create() => new S();
}}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.Regular9);
            comp.VerifyDiagnostics(
                // (5,5): error CS8652: The feature 'parameterless struct constructors' is currently in Preview and *unsupported*. To use Preview features, use the 'preview' language version.
                //     S() { Initialized = true; }
                Diagnostic(ErrorCode.ERR_FeatureInPreview, "S").WithArguments("parameterless struct constructors").WithLocation(5, 5));

            comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var refA = comp.EmitToImageReference();

            var sourceB =
@"using System;
class Program
{
    static void Main()
    {
        Console.WriteLine(S.Create().Initialized);
    }
}";
            CompileAndVerify(sourceB, references: new[] { refA }, expectedOutput: "True");
        }

        [InlineData("internal", false)]
        [InlineData("internal", true)]
        [InlineData("private", false)]
        [InlineData("private", true)]
        [Theory]
        public void PrivateConstructor_NewConstraint(string accessibility, bool useCompilationReference)
        {
            var sourceA =
$@"public struct S
{{
    {accessibility} S() {{ }}
}}";
            var comp = CreateCompilation(sourceA, parseOptions: TestOptions.RegularPreview);
            comp.VerifyDiagnostics();
            var refA = AsReference(comp, useCompilationReference);

            var sourceB =
@"using System;
class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static void Main()
    {
        Console.WriteLine(new S());
        Console.WriteLine(CreateNew<S>());
    }
}";
            comp = CreateCompilation(sourceB, references: new[] { refA });
            comp.VerifyDiagnostics(
                // (7,31): error CS0122: 'S.S()' is inaccessible due to its protection level
                //         Console.WriteLine(new S());
                Diagnostic(ErrorCode.ERR_BadAccess, "S").WithArguments("S.S()").WithLocation(7, 31),
                // (8,27): error CS0310: 'S' must be a non-abstract type with a public parameterless constructor in order to use it as parameter 'T' in the generic type or method 'Program.CreateNew<T>()'
                //         Console.WriteLine(CreateNew<S>());
                Diagnostic(ErrorCode.ERR_NewConstraintNotSatisfied, "CreateNew<S>").WithArguments("Program.CreateNew<T>()", "T", "S").WithLocation(8, 27));

            var sourceC =
@"using System;
class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static T CreateStruct1<T>() where T : struct => new T();
    static T CreateStruct2<T>() where T : struct => CreateNew<T>();
    static string Invoke(Func<object> f)
    {
        object obj;
        try
        {
            obj = f();
        }
        catch (Exception e)
        {
            obj = e;
        }
        return obj.GetType().FullName;
    }
    static void Main()
    {
        Console.WriteLine(""{0}, {1}"",
            Invoke(() => CreateStruct1<S>()),
            Invoke(() => CreateStruct2<S>()));
    }
}";
            CompileAndVerify(sourceC, references: new[] { refA }, expectedOutput: "System.MissingMethodException, System.MissingMethodException");
        }

        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("protected internal")]
        [InlineData("private")]
        [InlineData("private protected")]
        [Theory]
        public void PublicConstructorPrivateStruct_NewConstraint(string accessibility)
        {
            var sourceA =
$@"partial class Program
{{
    {accessibility} struct S
    {{
        public readonly bool Initialized;
        public S()
        {{
            Initialized = true;
        }}
    }}    
}}";
            var sourceB =
@"using System;
partial class Program
{
    static T CreateNew<T>() where T : new() => new T();
    static void Main()
    {
        Console.WriteLine(CreateNew<S>().Initialized);
    }
}";
            CompileAndVerify(new[] { sourceA, sourceB }, parseOptions: TestOptions.RegularPreview, expectedOutput: "True");
        }
    }
}
