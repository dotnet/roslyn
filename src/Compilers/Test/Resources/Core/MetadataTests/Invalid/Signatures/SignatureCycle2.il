//
// Example #2 -- call within same assembly to cyclical signature
//
// NOTE: build.bat creates the cycle by modifying the ilasm output
//
// From:
//   List`1<int32 modopt([TypeSpec #1])>
//                             |
//                             v
//                           int32
//
// To:
//   List`1<int32 modopt([TypeSpec #2])
//       ^---------------------|
//
// Symptoms: 
//   * peverify.exe succeeds
//   * program crashes (stack overflow)
//   * ildasm crashes (likely stack overflow)
//

.assembly extern mscorlib { .ver 4:0:0:0 .publickeytoken = (B7 7A 5C 56 19 34 E0 89 ) }
.assembly SignatureCycle2 {}
.module SignatureCycle2.exe

.class public Extender extends class [mscorlib]System.Collections.Generic.List`1<int32 modopt(int32)>
{
    .method public specialname rtspecialname instance void .ctor()
    {
        ldarg.0
        call instance void class [mscorlib]System.Collections.Generic.List`1<int32 modopt(int32)>::.ctor()
        ldstr "hello world"
        call void [mscorlib]System.Console::WriteLine(object)
        ret
    }

    .method public static void Main()
    {
        .entrypoint
        newobj instance void Extender::.ctor()
        call void User::X(class [mscorlib]System.Collections.Generic.List`1<int32 modopt(int32)>)
        ret
    }
}

.class public User extends [mscorlib]System.Object
{
    .method public static void X(class [mscorlib]System.Collections.Generic.List`1<int32 modopt(int32)>)
    {
        ldstr "goodbye world"
        call void [mscorlib]System.Console::WriteLine(object)
        ret
    }
}
 