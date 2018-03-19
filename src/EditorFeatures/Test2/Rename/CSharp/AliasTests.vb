' Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <UseExportProvider>
    Public Class AliasTests
        Private ReadOnly _outputHelper As ITestOutputHelper

        Public Sub New(outputHelper As ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <WorkItem(543759, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543759")>
        <Fact, Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameNamespaceAlias()
            Using result = RenameEngineResult.Create(_outputHelper,
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
            Using result = RenameEngineResult.Create(_outputHelper,
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
            Using result = RenameEngineResult.Create(_outputHelper,
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
                            using [|D|] = C<int>;

                            class C<T>
                            {
                                void Goo()
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
                            using [|$$D|] = C<int>;

                            class C<T>
                            {
                                void Goo()
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
                            using [|$$D|] = System.Func<int>;

                            class C
                            {
                                void Goo()
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using [|D|] = C;
                            class C
                            {
                                void Goo()
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using [|$$D|] = C;
                            class C
                            {
                                void Goo()
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using Goo = System.Int32;
                            class C
                            {
                                void Goo()
                                {
                                    Goo [|$$x|] = 23;
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using Goo = System.Int32;
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using Goo = Program;

                            class Program
                            {
                                public void Goo()
                                {
                                    Goo [|$$x|] = null;
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
using [|Goo|] = C3;

namespace N1
{
    class C1
    {
        public void Goo()
        {
            {|stmt1:$$Goo|} f = null;
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using [|Goo|] = N1.C1;

                            namespace N1
                            {
                                class C1
                                {
                                    public void Goo()
                                    {
                                        [|$$Goo|] f = null;
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
        <WorkItem(586743, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586743")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOneDuplicateAliasToNoConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document>
                            using goo = System.Int32;
                            using [|bar|] = System.Int32;

                            class Program
                            {
                                static void Main(string[] args)
                                {
                                    goo f = 1;
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
        <WorkItem(542693, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542693")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameOuterAliasWithNestedAlias()
            Using result = RenameEngineResult.Create(_outputHelper,
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
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using System;
using [|$$Goo|] = System.Console;

class Bar : {|qualify:Attribute|}
{ }

class C1
{
    static void Main()
    {
        {|stmt1:Goo|}.WriteLine("Baz");
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
        <WorkItem(579200, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579200")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug579200_RenameNestedAliasTarget()
            Using result = RenameEngineResult.Create(_outputHelper,
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
        <WorkItem(579214, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579214")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug579214_RenameAttributeNamedDynamic()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|dynamic|] = System;
class C : [|$$dynamic|]::Object { }
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="goo")

            End Using
        End Sub

        <Fact>
        <WorkItem(629695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629695")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockCompUnit()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using {|declconflict:Bar|} = A.B;
using [|$$Goo|] = A.C; // Rename Goo to Bar

namespace A{    
    class C
    {        
        public class B        
        {            
            public class Goo            
            { 
            }         
        }    
    }
} 

namespace A.B.B
{    
    class Goo { }
}

class Program
{
    static void Main(string[] args)    
    {        
        Bar.B.Goo b;        
        {|stmt1:Goo|}.B.Goo c;
     }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Goo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(629695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629695")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockNSDecl()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace A{    
    class C
    {        
        public class B        
        {            
            public class Goo            
            { 
            }         
        }    
    }
} 

namespace A.B.B
{    
    class Goo { }
}

namespace X
{
    using {|declconflict:Bar|} = A.B;
    using [|$$Goo|] = A.C; // Rename Goo to Bar

    class Program
    {
        static void Main(string[] args)    
        {        
            Bar.B.Goo b;        
            {|stmt1:Goo|}.B.Goo c;
         }
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="Bar")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Goo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(629695, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629695")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockWithEscaping()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
namespace A{    
    class C
    {        
        public class B        
        {            
            public class Goo            
            { 
            }         
        }    
    }
} 

namespace A.B.B
{    
    class Goo { }
}

namespace X
{
    using {|declconflict:@Bar|} = A.B;
    using [|$$Goo|] = A.C; // Rename Goo to Bar

    class Program
    {
        static void Main(string[] args)    
        {        
            Bar.B.Goo b;        
            {|stmt1:Goo|}.B.Goo c;
         }
    }
}
                        ]]></Document>
                    </Project>
                </Workspace>, renameTo:="B\u0061r")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Goo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Fact>
        <WorkItem(603365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603365"), WorkItem(745833, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/745833")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug603365_RenameAliasToClassNameOnlyFixesAliasUsages_1()
            Using result = RenameEngineResult.Create(_outputHelper,
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
        <WorkItem(603365, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603365")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub Bug603365_RenameAliasToClassNameOnlyFixesAliasUsages_2()
            Using result = RenameEngineResult.Create(_outputHelper,
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
        <WorkItem(633860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633860")>
        <WorkItem(632303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632303")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttribute()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|$$GooAttribute|] = System.ObsoleteAttribute;

[{|long:GooAttribute|}]
class C{ }

[{|short:Goo|}]
class D{ }

[{|long:GooAttribute|}()]
class B{ }

[{|short:Goo|}()] 
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
        <WorkItem(633860, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633860")>
        <WorkItem(632303, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632303")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttributeWithResolvedConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|$$GooAttribute|] = System.ObsoleteAttribute;
using Bar = System.ContextStaticAttribute;

[{|long1:GooAttribute|}]
class C{ }

[{|short1:Goo|}]
class D{ }

[{|long2:GooAttribute|}()]
class B{ }

[{|short2:Goo|}()] 
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
        <WorkItem(529531, "http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529531")>
        <Trait(Traits.Feature, Traits.Features.Rename)>
        Public Sub RenameAliasToNullableWithResolvedConflict()
            Using result = RenameEngineResult.Create(_outputHelper,
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
