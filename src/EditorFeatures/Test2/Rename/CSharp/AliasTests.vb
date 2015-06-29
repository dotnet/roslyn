' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    Public Class AliasTests
        <WorkItem(543759)>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespaceAlias()
            Using result = RenameEngineResult.Create(
                    <Workspace>
                        <Project Language="C#" CommonReferences="true">
                            <Document>
                            using [|$$Alias|] = System;

                            class C
                            {
                                private [|Alias|].String s;
                            }
                        </Document>
                        </Project>
                    </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespaceAndAlias()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                               using [|N2|] = N.[|N2|];
                               
                               namespace N { namespace [|$$N2|] { class D { } } }
                               
                               class C : [|N2|].D
                               {
                               }
                           </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespaceButNotDifferentlyNamedAlias()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using M2 = N.[|N2|];
                            
                            namespace N { namespace [|$$N2|] { class D { } } }
                            
                            class C : M2.D
                            {
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConstructedTypeAliasFromUse()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
                            using [|D|] = C<int>;

                            class C<T>
                            {
                                void Foo()
                                {
                                    var x = new [|$$D|]();
                                }
                            }
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConstructedTypeAliasFromDeclaration()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
                            using [|$$D|] = C<int>;

                            class C<T>
                            {
                                void Foo()
                                {
                                    var x = new [|D|]();
                                }
                            }
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConstructedTypeAliasFromDeclaration2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
                            using [|$$D|] = System.Func<int>;

                            class C
                            {
                                void Foo()
                                {
                                    [|D|] d;
                                }
                            }
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleTypeAliasFromUse()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using [|D|] = C;
                            class C
                            {
                                void Foo()
                                {
                                    var x = new [|$$D|]();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleTypeAliasFromDeclaration()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using [|$$D|] = C;
                            class C
                            {
                                void Foo()
                                {
                                    var x = new [|D|]();
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleSpecialTypeAliasVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using Foo = System.Int32;
                            class C
                            {
                                void Foo()
                                {
                                    Foo [|$$x|] = 23;
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleSpecialTypeDoubleAliasVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using Foo = System.Int32;
                            using Bar = System.Int32;
                            class C
                            {
                                void Bar()
                                {
                                    Bar [|$$x|] = 23;
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")

            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameSimpleTypeAliasVariable()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using Foo = Program;

                            class Program
                            {
                                public void Foo()
                                {
                                    Foo [|$$x|] = null;
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasNoConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|Foo|] = C3;

namespace N1
{
    class C1
    {
        public void Foo()
        {
            {|stmt1:$$Foo|} f = null;
            C1 c = null;
        }
    }
}

public class C3
{

}
                        </Document>
                    </Project>
                </Workspace>, renameTo:="C1")

                result.AssertLabeledSpansAre("stmt1", "C3 f = null;", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToSameNameNoConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using [|Foo|] = N1.C1;

                            namespace N1
                            {
                                class C1
                                {
                                    public void Foo()
                                    {
                                        [|$$Foo|] f = null;
                                        C1 c = null;
                                    }
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="C1")

            End Using
        End Sub

        <Fact>
        <WorkItem(586743)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOneDuplicateAliasToNoConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using foo = System.Int32;
                            using [|bar|] = System.Int32;

                            class Program
                            {
                                static void Main(string[] args)
                                {
                                    foo f = 1;
                                    {|stmt1:$$bar|} b = 2;
                                }
                            }
                        </Document>
                    </Project>
                </Workspace>, renameTo:="BarBaz")


                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub


        <Fact>
        <WorkItem(542693)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOuterAliasWithNestedAlias()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|$$C|] = System.Action;

namespace N
{
    using C = A<[|C|]>;

    class A<T> { }

    class B : C { }
}

                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="D")

            End Using
        End Sub

        <Fact>
        <WorkItem(10028, "DevDiv_Projects/Roslyn")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameConflictWithAlias()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
using [|$$Foo|] = System.Console;

class Bar : {|qualify:Attribute|}
{ }

class C1
{
    static void Main()
    {
        {|stmt1:Foo|}.WriteLine("Baz");
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Attribute")

                result.AssertLabeledSpansAre("stmt1", "Attribute", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("qualify", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(579200)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug579200_RenameNestedAliasTarget()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
using System.Collections.Generic;
 
namespace N
{
    using X = [|A|];
    using Y = List<[|A|]>;
 
    class [|$$A|] { }
 
    class B : X { }
    class C : Y { }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="A2")

            End Using
        End Sub

        <Fact>
        <WorkItem(579214)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug579214_RenameAttributeNamedDynamic()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|dynamic|] = System;
class C : [|$$dynamic|]::Object { }
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="foo")

            End Using
        End Sub

        <Fact>
        <WorkItem(629695)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockCompUnit()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using {|declconflict:Bar|} = A.B;
using [|$$Foo|] = A.C; // Rename Foo to Bar

namespace A{    
    class C
    {        
        public class B        
        {            
            public class Foo            
            { 
            }         
        }    
    }
} 

namespace A.B.B
{    
    class Foo { }
}

class Program
{
    static void Main(string[] args)    
    {        
        Bar.B.Foo b;        
        {|stmt1:Foo|}.B.Foo c;
     }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Foo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(629695)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockNSDecl()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace A{    
    class C
    {        
        public class B        
        {            
            public class Foo            
            { 
            }         
        }    
    }
} 

namespace A.B.B
{    
    class Foo { }
}

namespace X
{
    using {|declconflict:Bar|} = A.B;
    using [|$$Foo|] = A.C; // Rename Foo to Bar

    class Program
    {
        static void Main(string[] args)    
        {        
            Bar.B.Foo b;        
            {|stmt1:Foo|}.B.Foo c;
         }
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Foo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(629695)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockWithEscaping()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace A{    
    class C
    {        
        public class B        
        {            
            public class Foo            
            { 
            }         
        }    
    }
} 

namespace A.B.B
{    
    class Foo { }
}

namespace X
{
    using {|declconflict:@Bar|} = A.B;
    using [|$$Foo|] = A.C; // Rename Foo to Bar

    class Program
    {
        static void Main(string[] args)    
        {        
            Bar.B.Foo b;        
            {|stmt1:Foo|}.B.Foo c;
         }
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="B\u0061r")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Foo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(603365), WorkItem(745833)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug603365_RenameAliasToClassNameOnlyFixesAliasUsages_1()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System.Collections.Generic;
namespace N
{
    using X = M.A;
    namespace M
    {
        using [|$$Y|] = List<X>;
        class A { }
        class B : X { }
        class C : {|resolved:Y|} { }
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="A")

                result.AssertLabeledSpansAre("resolved", "List<X>", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(603365)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug603365_RenameAliasToClassNameOnlyFixesAliasUsages_2()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System.Collections.Generic;
namespace N
{
    using X = M.A;
    namespace M
    {
        using [|$$Y|] = List<X>;
        class A { }
        class B : {|resolved_nonref:X|} { }
        class C : {|resolved:Y|} { }
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="X")

                result.AssertLabeledSpansAre("resolved", "X", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("resolved_nonref", "A", type:=RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(633860)>
        <WorkItem(632303)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttribute()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|$$FooAttribute|] = System.ObsoleteAttribute;

[{|long:FooAttribute|}]
class C{ }

[{|short:Foo|}]
class D{ }

[{|long:FooAttribute|}()]
class B{ }

[{|short:Foo|}()] 
class Program
{    
    static void Main(string[] args)    
    {}
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short", "Bar", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(633860)>
        <WorkItem(632303)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttributeWithResolvedConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|$$FooAttribute|] = System.ObsoleteAttribute;
using Bar = System.ContextStaticAttribute;

[{|long1:FooAttribute|}]
class C{ }

[{|short1:Foo|}]
class D{ }

[{|long2:FooAttribute|}()]
class B{ }

[{|short2:Foo|}()] 
class Program
{    
    static void Main(string[] args)    
    {}
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short1", "BarAttribute", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("short2", "BarAttribute()", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("long1", "BarAttribute", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long2", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(529531)>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToNullableWithResolvedConflict()
            Using result = RenameEngineResult.Create(
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using N = System.Nullable<int>;
 
class Program
{
    static void Main()
    {
        object x = 1;
        var y = x as {|resolved:N|} + 1;
    }
 
    class [|$$C|] { } // Rename C to N
}
 

                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="N")

                result.AssertLabeledSpansAre("resolved", "var y = (x as int?) + 1;", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub
    End Class
End Namespace
