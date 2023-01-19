// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.UnitTests;

public abstract class BaseCompilerFeatureRequiredTests<TCompilation, TSource> : CommonTestBase where TCompilation : Compilation
{
    private static string CompilerFeatureRequiredApplication(
        bool? isOptional)
    {
        var builder = new BlobBuilder();
        builder.WriteSerializedString("test");
        var featureLengthAndName = string.Join(" ", builder.ToImmutableArray().Select(b => $"{b:x2}"));

        var isOptionalText = isOptional switch
        {
            true => "01 00 54 02 0a 49 73 4f 70 74 69 6f 6e 61 6c 01", // One optional parameter, "IsOptional", true
            false => "01 00 54 02 0a 49 73 4f 70 74 69 6f 6e 61 6c 00", // One optional parameter, "IsOptional", false
            null => "00 00" // No optional parameters
        };

        return $"""
                 .custom instance void System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::.ctor(string) = (
                     01 00 {featureLengthAndName} {isOptionalText}
                 )
                 """;
    }

    private const string CompilerFeatureRequiredIl =
        """
        .class public auto ansi sealed beforefieldinit System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
             extends [mscorlib]System.Attribute
         {
             .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = (
                 01 00 ff 7f 00 00 02 00 54 02 0d 41 6c 6c 6f 77
                 4d 75 6c 74 69 70 6c 65 01 54 02 09 49 6e 68 65
                 72 69 74 65 64 00
             )
             // Fields
             .field private initonly string '<FeatureName>k__BackingField'
             .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                 01 00 00 00
             )
             .field private initonly bool '<IsOptional>k__BackingField'
             .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = (
                 01 00 00 00
             )

             .field public static literal string RefStructs = "RefStructs"
             .field public static literal string RequiredMembers = "RequiredMembers"
         
             // Methods
             .method public hidebysig specialname rtspecialname 
                 instance void .ctor (
                     string featureName
                 ) cil managed 
             {
                 ldarg.0
                 call instance void [mscorlib]System.Attribute::.ctor()
                 ldarg.0
                 ldarg.1
                 stfld string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<FeatureName>k__BackingField'
                 ret
             } // end of method CompilerFeatureRequiredAttribute::.ctor
         
             .method public hidebysig specialname 
                 instance string get_FeatureName () cil managed 
             {
                 ldarg.0
                 ldfld string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<FeatureName>k__BackingField'
                 ret
             } // end of method CompilerFeatureRequiredAttribute::get_FeatureName
         
             .method public hidebysig specialname 
                 instance bool get_IsOptional () cil managed 
             {
                 ldarg.0
                 ldfld bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<IsOptional>k__BackingField'
                 ret
             } // end of method CompilerFeatureRequiredAttribute::get_IsOptional
         
             .method public hidebysig specialname 
                 instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) set_IsOptional (
                     bool 'value'
                 ) cil managed 
             {
                 ldarg.0
                 ldarg.1
                 stfld bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::'<IsOptional>k__BackingField'
                 ret
             } // end of method CompilerFeatureRequiredAttribute::set_IsOptional
         
             // Properties
             .property instance string FeatureName()
             {
                 .get instance string System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::get_FeatureName()
             }
             .property instance bool IsOptional()
             {
                 .get instance bool System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::get_IsOptional()
                 .set instance void modreq([mscorlib]System.Runtime.CompilerServices.IsExternalInit) System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute::set_IsOptional(bool)
             }
         
         } // end of class System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute
        """;

    // This IL is equivalent to the following definition, which format holes for the required attribute under test:
    // [CompilerFeatureRequired("test")]
    // public class OnType
    // {
    // }
    // 
    // public class OnMethod
    // {
    //     [CompilerFeatureRequired("test")]
    //     public static void M() {}
    // }
    // 
    // public class OnMethodReturn
    // {
    //     [return: CompilerFeatureRequired("test")]
    //     public static void M() {}
    // }
    // 
    // public class OnParameter
    // {
    //     public static void M([CompilerFeatureRequired("test")] int param) {}
    // }
    // 
    // public class OnField
    // {
    //     [CompilerFeatureRequired("test")]
    //     public static int Field;
    // }
    // 
    // public class OnProperty
    // {
    //     [CompilerFeatureRequired("test")]
    //     public static int Property { get => 0; set {} }
    // }
    // 
    // public class OnPropertySetter
    // {
    //     public static int Property { get => 0; [CompilerFeatureRequired("test")] set {} }
    // }
    // 
    // public class OnPropertyGetter
    // {
    //     public static int Property { [CompilerFeatureRequired("test")] get => 0; set {} }
    // }
    // 
    // public class OnEvent
    // {
    //     [CompilerFeatureRequired("test")]
    //     public static event Action Event { add {} remove {} }
    // }
    // 
    // public class OnEventAdder
    // {
    //     public static event Action Event { [CompilerFeatureRequired("test")] add {} remove {} }
    // }
    // 
    // public class OnEventRemover
    // {
    //     public static event Action Event { [CompilerFeatureRequired("test")] add {} remove {} }
    // }
    // 
    // [CompilerFeatureRequired("test")]
    // public enum OnEnum
    // {
    //     A
    // }
    // 
    // public enum OnEnumMember
    // {
    //     [CompilerFeatureRequired("test")] A
    // }
    // 
    // public class OnClassTypeParameter<[CompilerFeatureRequired("test")] T>
    // {
    // }
    // 
    // public class OnMethodTypeParameter
    // {
    //     public static void M<[CompilerFeatureRequired("test")] T>() {}
    // }
    // 
    // [CompilerFeatureRequired("test")]
    // public delegate void OnDelegateType();
    //
    // VB:
    // Public Class OnIndexedPropertyParameter
    //    Public Property [Property](<CompilerFeatureRequired("test")> param As Integer) As Integer
    //        Get
    //            Return 1
    //        End Get
    //        Set
    //        End Set
    //    End Property
    // End Class
    private string GetTestIl(string attributeApplication)
    {
        return $$"""
            .class public auto ansi beforefieldinit OnType
                extends [mscorlib]System.Object
            {
                {{attributeApplication}}
                // Methods
                .method public hidebysig static
                    void M () cil managed 
                {
                    ret
                } // end of method OnMethod::M
            } // end of class OnType
            
            .class public auto ansi beforefieldinit OnMethod
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig static
                    void M () cil managed 
                {
                    {{attributeApplication}}
                    ret
                } // end of method OnMethod::M
            } // end of class OnMethod
            
            .class public auto ansi beforefieldinit OnMethodReturn
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig static
                    void M () cil managed 
                {
                    .param [0]
                        {{attributeApplication}}
                    ret
                } // end of method OnMethodReturn::M
            } // end of class OnMethodReturn
            
            .class public auto ansi beforefieldinit OnParameter
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig static
                    void M (
                        int32 param
                    ) cil managed 
                {
                    .param [1]
                        {{attributeApplication}}
                    ret
                } // end of method OnParameter::M
            } // end of class OnParameter
            
            .class public auto ansi beforefieldinit OnField
                extends [mscorlib]System.Object
            {
                // Fields
                .field public static int32 Field
                {{attributeApplication}}
            } // end of class OnField
            
            .class public auto ansi beforefieldinit OnProperty
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig specialname static
                    int32 get_Property () cil managed 
                {
                    ldc.i4.0
                    ret
                } // end of method OnProperty::get_Property
            
                .method public hidebysig specialname static
                    void set_Property (
                        int32 'value'
                    ) cil managed 
                {
                    ret
                } // end of method OnProperty::set_Property
            
                // Properties
                .property int32 Property()
                {
                    {{attributeApplication}}
                    .get int32 OnProperty::get_Property()
                    .set void OnProperty::set_Property(int32)
                }
            
            } // end of class OnProperty
            
            .class public auto ansi beforefieldinit OnPropertySetter
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig specialname static
                    int32 get_Property () cil managed 
                {
                    ldc.i4.0
                    ret
                } // end of method OnPropertySetter::get_Property

                .method public hidebysig specialname static
                    void set_Property (
                        int32 'value'
                    ) cil managed 
                {
                    {{attributeApplication}}
                    ret
                } // end of method OnPropertySetter::set_Property

                // Properties
                .property int32 Property()
                {
                    .get int32 OnPropertySetter::get_Property()
                    .set void OnPropertySetter::set_Property(int32)
                }
            } // end of class OnPropertySetter

            .class public auto ansi beforefieldinit OnPropertyGetter
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig specialname static
                    int32 get_Property () cil managed 
                {
                    {{attributeApplication}}
                    ldc.i4.0
                    ret
                } // end of method OnPropertyGetter::get_Property
            
                .method public hidebysig specialname static
                    void set_Property (
                        int32 'value'
                    ) cil managed 
                {
                    ret
                } // end of method OnPropertyGetter::set_Property

                // Properties
                .property int32 Property()
                {
                    .get int32 OnPropertyGetter::get_Property()
                    .set void OnPropertyGetter::set_Property(int32)
                }
            
            } // end of class OnPropertyGetter
            
            .class public auto ansi beforefieldinit OnEvent
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig specialname static
                    void add_Event (
                        class [mscorlib]System.Action 'value'
                    ) cil managed 
                {
                    ret
                } // end of method OnEvent::add_Event
            
                .method public hidebysig specialname static
                    void remove_Event (
                        class [mscorlib]System.Action 'value'
                    ) cil managed 
                {
                    ret
                } // end of method OnEvent::remove_Event

                // Events
                .event [mscorlib]System.Action Event
                {
                    {{attributeApplication}}
                    .addon void OnEvent::add_Event(class [mscorlib]System.Action)
                    .removeon void OnEvent::remove_Event(class [mscorlib]System.Action)
                }
            
            
            } // end of class OnEvent
            
            .class public auto ansi beforefieldinit OnEventAdder
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig specialname static
                    void add_Event (
                        class [mscorlib]System.Action 'value'
                    ) cil managed 
                {
                    {{attributeApplication}}
                    ret
                } // end of method OnEventAdder::add_Event

                .method public hidebysig specialname static
                    void remove_Event (
                        class [mscorlib]System.Action 'value'
                    ) cil managed 
                {
                    ret
                } // end of method OnEventAdder::remove_Event

                // Events
                .event [mscorlib]System.Action Event
                {
                    .addon void OnEventAdder::add_Event(class [mscorlib]System.Action)
                    .removeon void OnEventAdder::remove_Event(class [mscorlib]System.Action)
                }
            
            
            } // end of class OnEventAdder
            
            .class public auto ansi beforefieldinit OnEventRemover
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig specialname static
                    void add_Event (
                        class [mscorlib]System.Action 'value'
                    ) cil managed 
                {
                    ret
                } // end of method OnEventRemover::add_Event
            
                .method public hidebysig specialname static
                    void remove_Event (
                        class [mscorlib]System.Action 'value'
                    ) cil managed 
                {
                    {{attributeApplication}}
                    ret
                } // end of method OnEventRemover::remove_Event

                // Events
                .event [mscorlib]System.Action Event
                {
                    .addon void OnEventRemover::add_Event(class [mscorlib]System.Action)
                    .removeon void OnEventRemover::remove_Event(class [mscorlib]System.Action)
                }
            
            
            } // end of class OnEventRemover
            
            .class public auto ansi sealed OnEnum
                extends [mscorlib]System.Enum
            {
                {{attributeApplication}}
                // Fields
                .field public specialname rtspecialname int32 value__
                .field public static literal valuetype OnEnum A = int32(0)
            
            } // end of class OnEnum
            
            .class public auto ansi sealed OnEnumMember
                extends [mscorlib]System.Enum
            {
                // Fields
                .field public specialname rtspecialname int32 value__
                .field public static literal valuetype OnEnumMember A = int32(0)
                {{attributeApplication}}
            
            } // end of class OnEnumMember
            
            .class public auto ansi beforefieldinit OnClassTypeParameter`1<T>
                extends [mscorlib]System.Object
            {
                .param type T
                    {{attributeApplication}}
            } // end of class OnClassTypeParameter`1
            
            .class public auto ansi beforefieldinit OnMethodTypeParameter
                extends [mscorlib]System.Object
            {
                // Methods
                .method public hidebysig static
                    void M<T> () cil managed 
                {
                    .param type T
                        {{attributeApplication}}
                    ret
                } // end of method OnMethodTypeParameter::M
            } // end of class OnMethodTypeParameter
            
            .class public auto ansi sealed OnDelegateType
                extends [mscorlib]System.MulticastDelegate
            {
                {{attributeApplication}}
                // Methods
                .method public hidebysig specialname rtspecialname 
                    instance void .ctor (
                        object 'object',
                        native int 'method'
                    ) runtime managed 
                {
                } // end of method OnDelegateType::.ctor
            
                .method public hidebysig newslot virtual 
                    instance void Invoke () runtime managed 
                {
                } // end of method OnDelegateType::Invoke
            
                .method public hidebysig newslot virtual 
                    instance class [mscorlib]System.IAsyncResult BeginInvoke (
                        class [mscorlib]System.AsyncCallback callback,
                        object 'object'
                    ) runtime managed 
                {
                } // end of method OnDelegateType::BeginInvoke
            
                .method public hidebysig newslot virtual 
                    instance void EndInvoke (
                        class [mscorlib]System.IAsyncResult result
                    ) runtime managed 
                {
                } // end of method OnDelegateType::EndInvoke
            
            } // end of class OnDelegateType

            .class public auto ansi OnIndexedPropertyParameter
                extends [mscorlib]System.Object
            {
                // Methods
                .method public specialname static
                    int32 get_Property (
                        int32 param
                    ) cil managed 
                {
                    .param [1]
                        {{attributeApplication}}

                    ldc.i4.1
                    stloc.0
                    ldloc.0
                    ret
                } // end of method OnIndexedPropertyParameter::get_Property
            
                .method public specialname static
                    void set_Property (
                        int32 param,
                        int32 Value
                    ) cil managed 
                {
                    .param [1]
                        {{attributeApplication}}
            
                    ret
                } // end of method OnIndexedPropertyParameter::set_Property
            
                // Properties
                .property int32 Property(
                    int32 param
                )
                {
                    .get int32 OnIndexedPropertyParameter::get_Property(int32)
                    .set void OnIndexedPropertyParameter::set_Property(int32, int32)
                }
            
            } // end of class OnIndexedPropertyParameter

            .class public auto ansi beforefieldinit OnThisIndexerParameter
                extends [mscorlib]System.Object
            {
                .custom instance void [mscorlib]System.Reflection.DefaultMemberAttribute::.ctor(string) = (
                    01 00 04 49 74 65 6d 00 00
                )
                // Methods
                .method public hidebysig specialname 
                    instance int32 get_Item (
                        int32 i
                    ) cil managed 
                {
                    .param [1]
                        {{attributeApplication}}
                    ldc.i4.0
                    ret
                } // end of method OnThisIndexerParameter::get_Item

                .method public hidebysig specialname 
                    instance void set_Item (
                        int32 i,
                        int32 'value'
                    ) cil managed 
                {
                    .param [1]
                        {{attributeApplication}}
                    ret
                } // end of method OnThisIndexerParameter::set_Item

                .method public hidebysig specialname rtspecialname 
                    instance void .ctor () cil managed 
                {
                    ldarg.0
                    call instance void [mscorlib]System.Object::.ctor()
                    ret
                } // end of method OnThisIndexerParameter::.ctor

                // Properties
                .property instance int32 Item(
                    int32 i
                )
                {
                    .get instance int32 OnThisIndexerParameter::get_Item(int32)
                    .set instance void OnThisIndexerParameter::set_Item(int32, int32)
                }
            } // end of class OnThisIndexerParameter
            """;
    }

    protected abstract TSource GetUsage();
    protected abstract TCompilation CreateCompilationWithIL(TSource source, string ilSource);
    protected abstract TCompilation CreateCompilation(TSource source, MetadataReference[] references);
    protected abstract CompilationVerifier CompileAndVerify(TCompilation compilation);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void OnNormalSymbols(bool? isOptional)
    {
        var testIl = $"""
        {CompilerFeatureRequiredIl}
        {GetTestIl(CompilerFeatureRequiredApplication(isOptional: isOptional))}
        """;
        var comp = CreateCompilationWithIL(source: GetUsage(), ilSource: testIl);

        if (isOptional == true)
        {
            CompileAndVerify(comp).VerifyDiagnostics();
        }
        else
        {
            AssertNormalErrors(comp);
        }
    }

    protected abstract void AssertNormalErrors(TCompilation compilation);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void OnAssembly(bool? isOptional)
    {
        var il = $$"""
        .assembly 'AssemblyTest'
        {
            {{CompilerFeatureRequiredApplication(isOptional: isOptional)}}
        }

        .assembly extern mscorlib 
        {
            .publickeytoken = (B7 7A 5C 56 19 34 E0 89)
            .ver 4:0:0:0
        } 

        {{CompilerFeatureRequiredIl}}
        {{GetTestIl(attributeApplication: "")}}
        """;

        var compiledIl = CompileIL(il, prependDefaultHeader: false);

        var comp = CreateCompilation(source: GetUsage(), references: new[] { compiledIl });

        if (isOptional == true)
        {
            CompileAndVerify(comp).VerifyDiagnostics();
        }
        else
        {
            AssertAssemblyErrors(comp, compiledIl);
        }
    }

    protected abstract void AssertAssemblyErrors(TCompilation compilation, MetadataReference ilRef);

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    public void OnModule(bool? isOptional)
    {
        var il = $$"""
        .module OnModule
        {{CompilerFeatureRequiredApplication(isOptional: isOptional)}}

        {{CompilerFeatureRequiredIl}}
        {{GetTestIl(attributeApplication: "")}}
        """;

        var compiledIl = CompileIL(il);
        var comp = CreateCompilation(source: GetUsage(), references: new[] { compiledIl });

        if (isOptional == true)
        {
            CompileAndVerify(comp).VerifyDiagnostics();
        }
        else
        {
            AssertModuleErrors(comp, compiledIl);
        }
    }

    protected abstract void AssertModuleErrors(TCompilation compilation, MetadataReference ilRef);
}
