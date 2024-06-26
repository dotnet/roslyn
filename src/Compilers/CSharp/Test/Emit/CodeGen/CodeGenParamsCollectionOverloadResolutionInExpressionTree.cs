// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Microsoft.CodeAnalysis.CSharp.Symbols.Metadata.PE;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Microsoft.CodeAnalysis.Test.Utilities;
using Roslyn.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.CodeGen
{
    public class CodeGenParamsCollectionOverloadResolutionInExpressionTree : CSharpTestBase
    {
        [Fact]
        public void TestParamsCollectionOverloadInExpressionTree()
        {
            var source = """
                using System.Linq;
                using System;

                class Program
                {
                    static IQueryable<string> GetStrings() => default;

                    static string Do(string s, params ReadOnlySpan<Object> span){
                        return s;
                    }

                    static string Do(string s, params object[] arr){
                        return s;
                    }

                    public static void Main(){
                        try{
                            GetStrings().Select(
                                o => Do("{0} {1} {2} {3}", o, o, o, o)); 
                        }
                        catch(ArgumentNullException)
                        {
                            Console.WriteLine("ArgumentNullException");
                        }
                    }
                }
                """;
            var comp = CompileAndVerify(
                source,
                targetFramework: TargetFramework.Net80, //we need ReadOnlySpan
                expectedOutput: "ArgumentNullException",
                parseOptions: TestOptions.RegularPreview //csharp 12+
            );
            comp.VerifyDiagnostics();
            comp.VerifyIL("Program.Main",
                """
                {
                  // Code size      153 (0x99)
                  .maxstack  11
                  .locals init (System.Linq.Expressions.ParameterExpression V_0)
                  .try
                  {
                    IL_0000:  call       "System.Linq.IQueryable<string> Program.GetStrings()"
                    IL_0005:  ldtoken    "string"
                    IL_000a:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                    IL_000f:  ldstr      "o"
                    IL_0014:  call       "System.Linq.Expressions.ParameterExpression System.Linq.Expressions.Expression.Parameter(System.Type, string)"
                    IL_0019:  stloc.0
                    IL_001a:  ldnull
                    IL_001b:  ldtoken    "string Program.Do(string, params object[])"
                    IL_0020:  call       "System.Reflection.MethodBase System.Reflection.MethodBase.GetMethodFromHandle(System.RuntimeMethodHandle)"
                    IL_0025:  castclass  "System.Reflection.MethodInfo"
                    IL_002a:  ldc.i4.2
                    IL_002b:  newarr     "System.Linq.Expressions.Expression"
                    IL_0030:  dup
                    IL_0031:  ldc.i4.0
                    IL_0032:  ldstr      "{0} {1} {2} {3}"
                    IL_0037:  ldtoken    "string"
                    IL_003c:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                    IL_0041:  call       "System.Linq.Expressions.ConstantExpression System.Linq.Expressions.Expression.Constant(object, System.Type)"
                    IL_0046:  stelem.ref
                    IL_0047:  dup
                    IL_0048:  ldc.i4.1
                    IL_0049:  ldtoken    "object"
                    IL_004e:  call       "System.Type System.Type.GetTypeFromHandle(System.RuntimeTypeHandle)"
                    IL_0053:  ldc.i4.4
                    IL_0054:  newarr     "System.Linq.Expressions.Expression"
                    IL_0059:  dup
                    IL_005a:  ldc.i4.0
                    IL_005b:  ldloc.0
                    IL_005c:  stelem.ref
                    IL_005d:  dup
                    IL_005e:  ldc.i4.1
                    IL_005f:  ldloc.0
                    IL_0060:  stelem.ref
                    IL_0061:  dup
                    IL_0062:  ldc.i4.2
                    IL_0063:  ldloc.0
                    IL_0064:  stelem.ref
                    IL_0065:  dup
                    IL_0066:  ldc.i4.3
                    IL_0067:  ldloc.0
                    IL_0068:  stelem.ref
                    IL_0069:  call       "System.Linq.Expressions.NewArrayExpression System.Linq.Expressions.Expression.NewArrayInit(System.Type, params System.Linq.Expressions.Expression[])"
                    IL_006e:  stelem.ref
                    IL_006f:  call       "System.Linq.Expressions.MethodCallExpression System.Linq.Expressions.Expression.Call(System.Linq.Expressions.Expression, System.Reflection.MethodInfo, params System.Linq.Expressions.Expression[])"
                    IL_0074:  ldc.i4.1
                    IL_0075:  newarr     "System.Linq.Expressions.ParameterExpression"
                    IL_007a:  dup
                    IL_007b:  ldc.i4.0
                    IL_007c:  ldloc.0
                    IL_007d:  stelem.ref
                    IL_007e:  call       "System.Linq.Expressions.Expression<System.Func<string, string>> System.Linq.Expressions.Expression.Lambda<System.Func<string, string>>(System.Linq.Expressions.Expression, params System.Linq.Expressions.ParameterExpression[])"
                    IL_0083:  call       "System.Linq.IQueryable<string> System.Linq.Queryable.Select<string, string>(System.Linq.IQueryable<string>, System.Linq.Expressions.Expression<System.Func<string, string>>)"
                    IL_0088:  pop
                    IL_0089:  leave.s    IL_0098
                  }
                  catch System.ArgumentNullException
                  {
                    IL_008b:  pop
                    IL_008c:  ldstr      "ArgumentNullException"
                    IL_0091:  call       "void System.Console.WriteLine(string)"
                    IL_0096:  leave.s    IL_0098
                  }
                  IL_0098:  ret
                }
                """);

        }
    }
}
