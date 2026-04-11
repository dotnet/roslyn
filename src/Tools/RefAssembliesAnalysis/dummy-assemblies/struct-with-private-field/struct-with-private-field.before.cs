#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net8.0
#:property LangVersion=preview
#:property PublishAot=false
#:property ProduceReferenceAssembly=true
#:property AssemblyName=struct-with-private-field

System.Console.Write("");

public struct S
{
    public int Value;
}
