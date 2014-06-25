// This is the source for WinMDPrefixing.winmd. To generate a copy of 
// WinMDPrefixing.winmd executive the following commands:
//    csc.exe /t:winmdobj WinMDPrefixing.cs
//    winmdexp [/r: references to mscorlib, System.Runtime.dll, and windows.winmd] WinMDPrefixing.winmdobj
namespace WinMDPrefixing
{
  public sealed class TestClass
    : TestInterface
  {
  }

  public interface TestInterface
  {
  }

  public delegate void TestDelegate();

  public struct TestStruct
  {
    public int TestField;
  }
}
