// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.PDB
{
    public class PDBWinMdExpTests : CSharpTestBase
    {
        [Fact]
        public void TestWinMdExpData_Empty()
        {
            #region "Source"
            var text = @"";
            #endregion

            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<token-map>
</token-map>";

            var compilation = CreateCompilationWithMscorlib45(
                text,
                options: TestOptions.ReleaseWinMD,
                sourceFileName: "source.cs").VerifyDiagnostics();

            string actual = PdbTestUtilities.GetTokenToLocationMap(compilation, true);
            AssertXml.Equal(expected, actual);
        }

        [Fact]
        public void TestWinMdExpData_Basic()
        {
            var text = @"using System;
using System.Threading;
using System.Threading.Tasks;

namespace X
{ 
	class DynamicMembers
	{
		public Func<Task<int>> Prop { get; set; }
	}
	public sealed partial class TestCase
	{
		private static int Count = 0;
		public async void Run()
		{
			DynamicMembers dc2 = new DynamicMembers();
			dc2.Prop = async () => { await Task.Delay(10000); return 3; };
			var rez2 = await dc2.Prop();
			if (rez2 == 3) Count++;
	 
			Driver.Result = TestCase.Count - 1;
			//When test complete, set the flag.
			Driver.CompletedSignal.Set();
		}

        static  partial void Foo();
        static  partial void Bar();
	}

	public sealed partial class TestCase
    {
        static partial void Bar(){}
    }

	class Driver
	{
		public static int Result = -1;
		public static AutoResetEvent CompletedSignal = new AutoResetEvent(false);
		static int Main()
		{
			var t = new TestCase();
			t.Run();
	 
			CompletedSignal.WaitOne();
			return Driver.Result;
		}
	}
}";
            string expected = @"
<token-map>
    <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""7"" start-column=""8"" end-line=""7"" end-column=""22""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""7"" start-column=""8"" end-line=""7"" end-column=""22""/>
    <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""9"" start-column=""26"" end-line=""9"" end-column=""30""/>
    <token-location token=""0x17xxxxxx"" file=""source.cs"" start-line=""9"" start-column=""26"" end-line=""9"" end-column=""30""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""9"" start-column=""33"" end-line=""9"" end-column=""36""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""9"" start-column=""38"" end-line=""9"" end-column=""41""/>
    <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""11"" start-column=""30"" end-line=""11"" end-column=""38""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""11"" start-column=""30"" end-line=""11"" end-column=""38""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""11"" start-column=""30"" end-line=""11"" end-column=""38""/>
    <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""13"" start-column=""22"" end-line=""13"" end-column=""27""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""14"" start-column=""21"" end-line=""14"" end-column=""24""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""27"" start-column=""30"" end-line=""27"" end-column=""33""/>
    <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""35"" start-column=""8"" end-line=""35"" end-column=""14""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""35"" start-column=""8"" end-line=""35"" end-column=""14""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""35"" start-column=""8"" end-line=""35"" end-column=""14""/>
    <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""37"" start-column=""21"" end-line=""37"" end-column=""27""/>
    <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""38"" start-column=""32"" end-line=""38"" end-column=""47""/>
    <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""39"" start-column=""14"" end-line=""39"" end-column=""18""/>
</token-map>";

            var compilation = CreateCompilationWithMscorlib45(
                text,
                options: TestOptions.ReleaseWinMD,
                sourceFileName: "source.cs").VerifyDiagnostics();

            string actual = PdbTestUtilities.GetTokenToLocationMap(compilation, true);
            AssertXml.Equal(expected, actual);
        }

        [WorkItem(693206, "DevDiv")]
        [Fact]
        public void Bug693206()
        {
            #region "Source"
            var text = @"
namespace X
{ 
	class DynamicMembers
	{
        enum HRESULT : int
        {
            S_OK = 0x0000,
            S_FALSE = 0x0001,
            S_PT_NO_CONFLICT = 0x40001,
            E_INVALID_DATA = unchecked((int)0x8007000D),
            E_INVALIDARG = unchecked((int)0x80070057),
            E_OUTOFMEMORY = unchecked((int)0x8007000E),
            ERROR_NOT_FOUND = unchecked((int)0x80070490)
        }
    }
}";
            #endregion

            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<token-map>
  <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""4"" start-column=""8"" end-line=""4"" end-column=""22"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""4"" start-column=""8"" end-line=""4"" end-column=""22"" />
  <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""6"" start-column=""14"" end-line=""6"" end-column=""21"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""8"" start-column=""13"" end-line=""8"" end-column=""17"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""9"" start-column=""13"" end-line=""9"" end-column=""20"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""10"" start-column=""13"" end-line=""10"" end-column=""29"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""11"" start-column=""13"" end-line=""11"" end-column=""27"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""12"" start-column=""13"" end-line=""12"" end-column=""25"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""13"" start-column=""13"" end-line=""13"" end-column=""26"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""14"" start-column=""13"" end-line=""14"" end-column=""28"" />
</token-map>";

            var compilation = CreateCompilationWithMscorlib45(
                text,
                options: TestOptions.ReleaseWinMD,
                sourceFileName: "source.cs").VerifyDiagnostics();

            string actual = PdbTestUtilities.GetTokenToLocationMap(compilation, true);
            AssertXml.Equal(expected, actual);
        }

        [Fact]
        public void TestWinMdExpData_Property_Event()
        {
            #region "Source"
            var text = @"
using System;

namespace X
{ 
	public delegate void D(int k);

	public sealed class TestCase
	{
		static TestCase()
		{
		}

		public TestCase(int rr)
		{
		}
		
		public event D E;
		
		public event Action E2;
		
		public int P { get; set; }
		
		public int P2 
		{ 
			get{ return 1; }
			set{}
		}
		
		public int this[int a]
		{
			get{ return 1; }
			set{}
		}
	}
}";
            #endregion

            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<token-map>
  <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""6"" start-column=""23"" end-line=""6"" end-column=""24"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""6"" start-column=""23"" end-line=""6"" end-column=""24"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""6"" start-column=""23"" end-line=""6"" end-column=""24"" />
  <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""8"" start-column=""22"" end-line=""8"" end-column=""30"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""10"" start-column=""10"" end-line=""10"" end-column=""18"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""14"" start-column=""10"" end-line=""14"" end-column=""18"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""18"" start-column=""18"" end-line=""18"" end-column=""19"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""18"" start-column=""18"" end-line=""18"" end-column=""19"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""18"" start-column=""18"" end-line=""18"" end-column=""19"" />
  <token-location token=""0x14xxxxxx"" file=""source.cs"" start-line=""18"" start-column=""18"" end-line=""18"" end-column=""19"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""20"" start-column=""23"" end-line=""20"" end-column=""25"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""20"" start-column=""23"" end-line=""20"" end-column=""25"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""20"" start-column=""23"" end-line=""20"" end-column=""25"" />
  <token-location token=""0x14xxxxxx"" file=""source.cs"" start-line=""20"" start-column=""23"" end-line=""20"" end-column=""25"" />
  <token-location token=""0x04xxxxxx"" file=""source.cs"" start-line=""22"" start-column=""14"" end-line=""22"" end-column=""15"" />
  <token-location token=""0x17xxxxxx"" file=""source.cs"" start-line=""22"" start-column=""14"" end-line=""22"" end-column=""15"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""22"" start-column=""18"" end-line=""22"" end-column=""21"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""22"" start-column=""23"" end-line=""22"" end-column=""26"" />
  <token-location token=""0x17xxxxxx"" file=""source.cs"" start-line=""24"" start-column=""14"" end-line=""24"" end-column=""16"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""26"" start-column=""4"" end-line=""26"" end-column=""7"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""27"" start-column=""4"" end-line=""27"" end-column=""7"" />
  <token-location token=""0x17xxxxxx"" file=""source.cs"" start-line=""30"" start-column=""14"" end-line=""30"" end-column=""18"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""32"" start-column=""4"" end-line=""32"" end-column=""7"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""33"" start-column=""4"" end-line=""33"" end-column=""7"" />
</token-map>";

            var compilation = CreateCompilationWithMscorlib45(
                text,
                options: TestOptions.ReleaseWinMD,
                sourceFileName: "source.cs").VerifyDiagnostics(
                    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E").WithArguments("X.TestCase.E"),
                    Diagnostic(ErrorCode.WRN_UnreferencedEvent, "E2").WithArguments("X.TestCase.E2"));

            string actual = PdbTestUtilities.GetTokenToLocationMap(compilation, true);
            AssertXml.Equal(expected, actual);
        }

        [Fact]
        public void TestWinMdExpData_AnonymousTypes()
        {
            #region "Source"
            var text = @"
namespace X
{ 
	public sealed class TestCase
	{
		public void M() 
		{ 
			var a = new { x = 1, y = new { a = 1 } };
			var b = new { t = new { t = new { t = new { t = new { a = 1 } } } } };
		}
	}
}";
            #endregion

            string expected = @"<?xml version=""1.0"" encoding=""utf-16""?>
<token-map>
  <token-location token=""0x02xxxxxx"" file=""source.cs"" start-line=""4"" start-column=""22"" end-line=""4"" end-column=""30"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""4"" start-column=""22"" end-line=""4"" end-column=""30"" />
  <token-location token=""0x06xxxxxx"" file=""source.cs"" start-line=""6"" start-column=""15"" end-line=""6"" end-column=""16"" />
</token-map>";

            var compilation = CreateCompilationWithMscorlib45(
                text,
                options: TestOptions.ReleaseWinMD,
                sourceFileName: "source.cs").VerifyDiagnostics();

            string actual = PdbTestUtilities.GetTokenToLocationMap(compilation, true);
            AssertXml.Equal(expected, actual);
        }
    }
}
