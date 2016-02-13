//
// Example #5 -- using (CLASS | VALUETYPE) TypeSpec instead of embedding the type in to the signature.
//
// build.bat will change the siganture of User::X to use a TypeSpec for List<int> instead of embedding GENERICINST in the signature.
//
// * peverify correctly identifies error:
//    [MD]: Error: Signature has token following ELEMENT_TYPE_CLASS (_VALUETYPE) that is not a TypeDef or TypeRef (token: 0x1b000001; offset: 0x00000005). [token:0x06000002]
//
// This just demonstrates that the rule is not called out in the CLI spec

.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly TypeSpecInWrongPlace {}
.module TypeSpecInWrongPlace.exe

.class public Extender extends class [mscorlib]System.Collections.Generic.List`1<int32>
{
    .method public static void Main()
    {
        .entrypoint
        newobj instance void class [mscorlib]System.Collections.Generic.List`1<int32>::.ctor()
        call void User::X(class [mscorlib]System.Collections.Generic.List`1<int32>)
        ret
    }
}

.class public User extends [mscorlib]System.Object
{
    .method public static void X(class [mscorlib]System.Collections.Generic.List`1<int32>)
    {
        ldstr "goodbye world"
        call void [mscorlib]System.Console::WriteLine(object)
        ret
    }
}