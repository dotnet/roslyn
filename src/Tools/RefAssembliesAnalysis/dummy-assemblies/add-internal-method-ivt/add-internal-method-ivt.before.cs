#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net8.0
#:property LangVersion=preview
#:property PublishAot=false
#:property ProduceReferenceAssembly=true
#:property AssemblyName=add-internal-method-ivt

using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("FriendAssembly")]

System.Console.Write("");

public class C
{
    public int M() => 1;
}
