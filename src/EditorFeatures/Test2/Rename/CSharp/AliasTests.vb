' Licensed to the .NET Foundation under one or more agreements.
' The .NET Foundation licenses this file to you under the MIT license.
' See the LICENSE file in the project root for more information.

Imports Microsoft.CodeAnalysis.Rename.ConflictEngine
Imports Xunit.Abstractions

Namespace Microsoft.CodeAnalysis.Editor.UnitTests.Rename.CSharp
    <UseExportProvider>
    <Trait(Traits.Feature, Traits.Features.Rename)>
    Public Class AliasTests
        Private ReadOnly _outputHelper As ITestOutputHelper

        Public Sub New(outputHelper As ITestOutputHelper)
            _outputHelper = outputHelper
        End Sub

        <Theory, CombinatorialData>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/543759")>
        Public Sub RenameNamespaceAlias(host As RenameTestHost)
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
                    </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameNamespaceAndAlias(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameNamespaceButNotDifferentlyNamedAlias(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameConstructedTypeAliasFromUse(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameConstructedTypeAliasFromDeclaration(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameConstructedTypeAliasFromDeclaration2(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleTypeAliasFromUse(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleTypeAliasFromDeclaration(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleSpecialTypeAliasVariable(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleSpecialTypeDoubleAliasVariable(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameSimpleTypeAliasVariable(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameAliasNoConflict(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="C1")

                result.AssertLabeledSpansAre("stmt1", "C3 f = null;", RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory>
        <CombinatorialData>
        Public Sub RenameAliasToSameNameNoConflict(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="C1")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/586743")>
        <CombinatorialData>
        Public Sub RenameOneDuplicateAliasToNoConflict(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarBaz")

                result.AssertLabeledSpansAre("stmt1", "BarBaz", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/542693")>
        <CombinatorialData>
        Public Sub RenameOuterAliasWithNestedAlias(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="D")

            End Using
        End Sub

        <Theory, WorkItem(10028, "DevDiv_Projects/Roslyn")>
        <CombinatorialData>
        Public Sub RenameConflictWithAlias(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Attribute")

                result.AssertLabeledSpansAre("stmt1", "Attribute", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("qualify", "System.Attribute", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579200")>
        <CombinatorialData>
        Public Sub Bug579200_RenameNestedAliasTarget(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="A2")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/579214")>
        <CombinatorialData>
        Public Sub Bug579214_RenameAttributeNamedDynamic(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|dynamic|] = System;
class C : [|$$dynamic|]::Object { }
                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="goo")

            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629695")>
        <CombinatorialData>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockCompUnit(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Goo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629695")>
        <CombinatorialData>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockNSDecl(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="Bar")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Goo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/629695")>
        <CombinatorialData>
        Public Sub Bug629695_DetectConflictWithAliasInSameBlockWithEscaping(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="B\u0061r")

                result.AssertLabeledSpansAre("declconflict", type:=RelatedLocationType.UnresolvedConflict)
                result.AssertLabeledSpansAre("stmt1", "A.C.B.Goo c;", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603365"), WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/745833")>
        <CombinatorialData>
        Public Sub Bug603365_RenameAliasToClassNameOnlyFixesAliasUsages_1(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="A")

                result.AssertLabeledSpansAre("resolved", "List<X>", type:=RelatedLocationType.ResolvedReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/603365")>
        <CombinatorialData>
        Public Sub Bug603365_RenameAliasToClassNameOnlyFixesAliasUsages_2(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="X")

                result.AssertLabeledSpansAre("resolved", "X", type:=RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("resolved_nonref", "A", type:=RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633860")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632303")>
        <CombinatorialData>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttribute(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short", "Bar", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/633860")>
        <WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/632303")>
        <CombinatorialData>
        Public Sub RenameAliasToAttributeAndEndingWithAttributeAttributeWithResolvedConflict(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="BarAttribute")

                result.AssertLabeledSpansAre("short1", "BarAttribute", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("short2", "BarAttribute()", RelatedLocationType.ResolvedReferenceConflict)
                result.AssertLabeledSpansAre("long1", "BarAttribute", RelatedLocationType.NoConflict)
                result.AssertLabeledSpansAre("long2", "BarAttribute", RelatedLocationType.NoConflict)
            End Using
        End Sub

        <Theory, WorkItem("http://vstfdevdiv:8080/DevDiv2/DevDiv/_workitems/edit/529531")>
        <CombinatorialData>
        Public Sub RenameAliasToNullableWithResolvedConflict(host As RenameTestHost)
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
                </Workspace>, host:=host, renameTo:="N")

                result.AssertLabeledSpansAre("resolved", "var y = (x as int?) + 1;", RelatedLocationType.ResolvedNonReferenceConflict)
            End Using
        End Sub

        <Theory, WorkItem("https://github.com/dotnet/roslyn/issues/58463")>
        <CombinatorialData>
        Public Sub RenameReferencingConstreuctorViaAlias(host As RenameTestHost)
            Using result = RenameEngineResult.Create(_outputHelper,
                <Workspace>
                    <Project Language="C#" CommonReferences="true">
                        <Document><![CDATA[
using [|ToBeRenamed|] = N.M.[|ToBeRenamed|];

namespace N
{
    class Program
    {
        static void Main(string[] args)
        {
            [|ToBeRenamed|] myClass = new [|ToBeRenamed|]("hello");     // references to the constructor via an identically named alias should also be renamed
        }
    }
}

                        ]]></Document>
                        <Document><![CDATA[
namespace N.M
{
    internal class [|$$ToBeRenamed|] 
    {
        private string myVar2;
        internal [|ToBeRenamed|](string var1)
        {
            myVar2 = var1;
        }
    }
}

                        ]]></Document>
                    </Project>
                </Workspace>, host:=host, renameTo:="ThisIsTheNewName")

            End Using
        End Sub
    End Class
End Namespace
