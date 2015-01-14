// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Test.Utilities;
using Xunit;

namespace Microsoft.CodeAnalysis.CSharp.UnitTests.Symbols
{
    public class PEParameterSymbolTests : CSharpTestBase
    {
        [Fact]
        public void NoParameterNames()
        {
            // Create simple interface where method parameters have no names.
            // interface I
            // {
            //   void M(object, object);
            // }
            var reference = CreateReflectionEmitAssembly(moduleBuilder =>
                {
                    var typeBuilder = moduleBuilder.DefineType(
                        "I",
                        TypeAttributes.Interface | TypeAttributes.Public | TypeAttributes.Abstract);
                    var methodBuilder = typeBuilder.DefineMethod(
                        "M",
                        MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual,
                        typeof(void),
                        new Type[] { typeof(object), typeof(object) });
                    methodBuilder.DefineParameter(1, ParameterAttributes.None, null);
                    methodBuilder.DefineParameter(2, ParameterAttributes.None, null);
                    typeBuilder.CreateType();
                });
            var source =
@"class C
{
    static void M(I o)
    {
        o.M(0, value: 2);
    }
}";
            var compilation = CreateCompilationWithMscorlib(source, new[] { reference });
            compilation.VerifyDiagnostics(
                // (5,16): error CS1744: Named argument 'value' specifies a parameter for which a positional argument has already been given
                Diagnostic(ErrorCode.ERR_NamedArgumentUsedInPositional, "value").WithArguments("value").WithLocation(5, 16));
        }
    }
}
