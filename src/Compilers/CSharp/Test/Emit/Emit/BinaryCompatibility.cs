// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests
{
    public class BinaryCompatibility : EmitMetadataTestBase
    {
        [Fact]
        public void InvokeVirtualBoundToOriginal()
        {
            // A method invocation of a virtual method is statically bound by the C# language to
            // the original declaration of the virtual method, not the most derived override of
            // that method. The difference between these two choices is visible in the binary
            // compatibility behavior of the program at runtime (e.g. when executing the program
            // against a modified library). This test checks that we bind the invocation to the
            // virtual method, not the override.
            var lib0 = @"
public class Base
{
    public virtual void M() { System.Console.WriteLine(""Base0""); }
}
public class Derived : Base
{
    public override void M() { System.Console.WriteLine(""Derived0""); }
}
";
            var lib0Image = CreateCompilationWithMscorlib46(lib0, options: TestOptions.ReleaseDll, assemblyName: "lib").EmitToImageReference();

            var lib1 = @"
public class Base
{
    public virtual void M() { System.Console.WriteLine(""Base1""); }
}
public class Derived : Base
{
    public new virtual void M() { System.Console.WriteLine(""Derived1""); }
}
";
            var lib1Image = CreateCompilationWithMscorlib46(lib1, options: TestOptions.ReleaseDll, assemblyName: "lib").EmitToImageReference();

            var client = @"
public class Client
{
    public static void M()
    {
        Derived d = new Derived();
        d.M();
    }
}
";
            var clientImage = CreateCompilationWithMscorlib46(client, references: new[] { lib0Image }, options: TestOptions.ReleaseDll).EmitToImageReference();

            var program = @"
public class Program
{
    public static void Main()
    {
        Client.M();
    }
}
";
            var compilation = CreateCompilationWithMscorlib46(program, references: new[] { lib1Image, clientImage }, options: TestOptions.ReleaseExe);
            CompileAndVerify(compilation, expectedOutput: @"Base1");
        }
    }
}
