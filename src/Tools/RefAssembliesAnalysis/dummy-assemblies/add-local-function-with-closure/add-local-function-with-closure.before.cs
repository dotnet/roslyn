#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net8.0
#:property LangVersion=preview
#:property PublishAot=false
#:property ProduceReferenceAssembly=true
#:property AssemblyName=add-local-function-with-closure

System.Console.Write("");

public class C
{
    public int M(int value) => value + 1;
}
