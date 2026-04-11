#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net8.0
#:property LangVersion=preview
#:property PublishAot=false
#:property ProduceReferenceAssembly=true
#:property AssemblyName=private-type-public-property

System.Console.Write("");

public class Outer
{
    private class Hidden
    {
        public int Count { get; } = 1;
    }
}
