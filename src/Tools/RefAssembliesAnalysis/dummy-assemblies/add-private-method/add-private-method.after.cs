#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net8.0
#:property LangVersion=preview
#:property PublishAot=false
#:property ProduceReferenceAssembly=true
#:property AssemblyName=add-private-method

System.Console.Write("");

public class C
{
    public int M() { PrivateMethod(); return 1; }

    private void PrivateMethod()
    {
    }
}
