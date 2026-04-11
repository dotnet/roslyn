#:sdk Microsoft.NET.Sdk
#:property TargetFramework=net8.0
#:property LangVersion=preview
#:property PublishAot=false
#:property ProduceReferenceAssembly=true
#:property AssemblyName=add-local-function

System.Console.Write("");

public class C
{
    public int M()
    {
        return Local();

        static int Local() => 1;
    }
}
