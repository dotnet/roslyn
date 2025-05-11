// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Debugging;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class TypeDefinitionDocumentTests : CSharpTestBase
    {
        [Fact]
        public void ClassWithMethod()
        {
            string source = @"
class M
{
    public static void A()
    {
        System.Console.WriteLine();
    }
}
";
            TestTypeDefinitionDocuments(new[] { source });
        }

        [Fact]
        public void NestedClassWithMethod()
        {
            string source = @"
class C
{
    class N
    {
        public static void A()
        {
            System.Console.WriteLine();
        }
    }
}
";
            TestTypeDefinitionDocuments(new[] { source });
        }

        [Fact]
        public void MultiNestedClassWithMethod()
        {
            string source = @"
class C
{
    class N
    {
        class N2
        {
            public static void A()
            {
                System.Console.WriteLine();
            }
        }
    }
}
";
            TestTypeDefinitionDocuments(new[] { source });
        }

        [Fact]
        public void PartialNestedClassWithMethod()
        {
            string source1 = @"
partial class C
{
    partial class N
    {
        public static void A()
        {
            System.Console.WriteLine();
        }
    }
}
";
            string source2 = @"
partial class C
{
    partial class N
    {
    }
}
";
            TestTypeDefinitionDocuments(new[] { source1, source2 },
                ("C", "2.cs"));
        }

        [Fact]
        public void EmptyClass()
        {
            string source = @"
class O
{
}
";
            TestTypeDefinitionDocuments(new[] { source },
                ("O", "1.cs"));
        }

        [Fact]
        public void EmptyNestedClass()
        {
            string source = @"
class O
{
    class N
    {
    }
}
";
            TestTypeDefinitionDocuments(new[] { source },
                ("O", "1.cs"));
        }

        [Fact]
        public void EmptyMultiNestedClass()
        {
            string source = @"
class O
{
    class N
    {
        class N2
        {
        }
    }
}
";
            TestTypeDefinitionDocuments(new[] { source },
                ("O", "1.cs"));
        }

        [Fact]
        public void MultipleClassesAndFiles()
        {
            string source1 = @"
class M
{
    public static void A()
    {
        System.Console.WriteLine();
    }
}

class N
{
}

class O
{
}
";

            string source2 = @"
class C
{
}

class D
{
}
";

            TestTypeDefinitionDocuments(new[] { source1, source2 },
                ("N", "1.cs"),
                ("O", "1.cs"),
                ("C", "2.cs"),
                ("D", "2.cs"));
        }

        [Fact]
        public void PartialClasses()
        {
            string source1 = @"
partial class C
{
}
";
            string source2 = @"
partial class C
{
}
";
            TestTypeDefinitionDocuments(new[] { source1, source2 },
                ("C", "1.cs, 2.cs"));
        }

        [Fact]
        public void PartialClasses2()
        {
            string source1 = @"
partial class C
{
}
";
            string source2 = @"
partial class C
{
    int x = 1;
    void M() { }
}
";
            TestTypeDefinitionDocuments(new[] { source1, source2 },
                ("C", "1.cs"));
        }

        [Fact]
        public void PartialClasses3()
        {
            string source1 = @"
partial class C
{
}
";
            string source2 = @"
partial class C
{
    int x;
    void M() { }
}
";
            TestTypeDefinitionDocuments(new[] { source1, source2 },
                ("C", "1.cs"));
        }

        [Fact]
        public void Property()
        {
            string source = @"
class C
{
    public int X { get; set; }
}
";

            TestTypeDefinitionDocuments(new[] { source });
        }

        [Fact]
        public void Fields()
        {
            string source = @"
class C
{
    int x;
    const int z = 2;
    int y;
}
";

            TestTypeDefinitionDocuments(new[] { source },
                ("C", "1.cs"));
        }

        [Fact]
        public void Fields_WithInitializer()
        {
            string source = @"
class C
{
    int x;
    const int z = 2;
    int y = 1;
}
";

            TestTypeDefinitionDocuments(new[] { source });
        }

        [Fact]
        public void AbstractMethod()
        {
            string source = @"
abstract class C
{
    public abstract void M();
}
";

            TestTypeDefinitionDocuments(new[] { source },
                ("C", "1.cs"));
        }

        [Fact]
        public void ExternMethod()
        {
            string source = @"
class C
{
    public extern void M();
}
";

            TestTypeDefinitionDocuments(new[] { source },
                ("C", "1.cs"));
        }

        [Fact]
        public void Interfaces()
        {
            string source1 = @"
interface I1
{
}

partial interface I2
{
    public void F();
}
";
            string source2 = @"
partial interface I2
{
}
";
            TestTypeDefinitionDocuments(new[] { source1, source2 },
                ("I1", "1.cs"),
                ("I2", "1.cs, 2.cs"));
        }

        [Fact]
        public void Record()
        {
            string source = @"
record R(int X);
";

            // The compiler synthesized methods have document info so we don't expect a type document
            TestTypeDefinitionDocuments(new[] { source, IsExternalInitTypeDefinition },
                ("IsExternalInit", "2.cs"));
        }

        [Fact]
        public void Record_SynthesizedMember()
        {
            string source = @"
record R(int X)
{
    protected virtual bool PrintMembers(System.Text.StringBuilder builder)
    {
        return true;
    }
}
";

            TestTypeDefinitionDocuments(new[] { source, IsExternalInitTypeDefinition },
                ("IsExternalInit", "2.cs"));
        }

        [Fact]
        public void Enum()
        {
            string source = @"
enum E
{
}

enum E2
{
    A,
    B
}
";

            TestTypeDefinitionDocuments(new[] { source },
                ("E", "1.cs"),
                ("E2", "1.cs"));
        }

        [Fact]
        public void Delegate()
        {
            string source = @"
delegate void D(int a);

class C
{
    void M()
    {
        var x = (int a, ref int b) => a;
    }
}
";

            TestTypeDefinitionDocuments(new[] { source },
                ("D", "1.cs"));
        }

        [Fact]
        public void AnonymousTypes()
        {
            string source = @"
class C
{
    void M()
    {
        var x = new { Goo = 1, Bar = ""Hi"" };
    }
}
";

            TestTypeDefinitionDocuments(new[] { source });
        }

        [Fact]
        public void LineDirectives()
        {
            string source = @"
class C
{
#line 1 ""C.cs""
    void M()
    {
    }
#line default
}

class D
{
#line 1 ""D.cs""
    private int _x = 1;
    private int X { get; set; } = 1;
}

#line 1 ""E.cs""
class E
{
    void M()
    {
    }
}

#line 1 ""F.cs""
class F
{
}
";

            TestTypeDefinitionDocuments(new[] { source },
                ("C", "1.cs"),
                ("D", "1.cs"),
                ("E", "1.cs"),
                ("F", "1.cs"));
        }

        private static void TestTypeDefinitionDocuments(string[] sources, params (string typeName, string documentName)[] expected)
        {
            var trees = sources.Select((s, i) => SyntaxFactory.ParseSyntaxTree(s, path: $"{i + 1}.cs", encoding: Encoding.UTF8)).ToArray();
            var compilation = CreateCompilation(trees, options: TestOptions.DebugDll);

            var pdbStream = new MemoryStream();
            var pe = compilation.EmitToArray(EmitOptions.Default.WithDebugInformationFormat(DebugInformationFormat.PortablePdb), pdbStream: pdbStream);
            pdbStream.Position = 0;

            var metadata = ModuleMetadata.CreateFromImage(pe);
            var metadataReader = metadata.GetMetadataReader();

            using var provider = MetadataReaderProvider.FromPortablePdbStream(pdbStream);
            var pdbReader = provider.GetMetadataReader();

            var actual = from handle in pdbReader.CustomDebugInformation
                         let entry = pdbReader.GetCustomDebugInformation(handle)
                         where pdbReader.GetGuid(entry.Kind).Equals(PortableCustomDebugInfoKinds.TypeDefinitionDocuments)
                         select (typeName: GetTypeName(entry.Parent), documentName: GetDocumentNames(entry.Value));

            AssertEx.Equal(expected, actual, itemSeparator: ",\n", itemInspector: i => $"(\"{i.typeName}\", \"{i.documentName}\")");

            string GetTypeName(EntityHandle handle)
            {
                var typeHandle = (TypeDefinitionHandle)handle;
                var type = metadataReader.GetTypeDefinition(typeHandle);
                return metadataReader.GetString(type.Name);
            }

            string GetDocumentNames(BlobHandle value)
            {
                var result = new List<string>();

                var reader = pdbReader.GetBlobReader(value);
                while (reader.RemainingBytes > 0)
                {
                    var documentRow = reader.ReadCompressedInteger();
                    if (documentRow > 0)
                    {
                        var doc = pdbReader.GetDocument(MetadataTokens.DocumentHandle(documentRow));
                        result.Add(pdbReader.GetString(doc.Name));
                    }
                }

                return string.Join(", ", result);
            }
        }
    }
}
