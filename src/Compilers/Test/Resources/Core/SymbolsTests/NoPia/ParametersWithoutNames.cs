// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;


class Program
{
    /// <summary>
    /// Compile and run this program to generate ParametersWithoutNames.dll
    /// </summary>
    static void Main()
    {

        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ParametersWithoutNames"), AssemblyBuilderAccess.Save,
                                    new[] { new CustomAttributeBuilder(typeof(ImportedFromTypeLibAttribute).GetConstructor(new[] { typeof(string) }), new[] { "GeneralPIA.dll" }),
                                            new CustomAttributeBuilder(typeof(GuidAttribute).GetConstructor(new[] { typeof(string) }), new[] { "f9c2d51d-4f44-45f0-9eda-c9d599b58257" })});
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("ParametersWithoutNames", "ParametersWithoutNames.dll");
        var typeBuilder = moduleBuilder.DefineType("I1", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Import | TypeAttributes.Interface);
        typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(GuidAttribute).GetConstructor(new[] { typeof(string) }), new[] { "f9c2d51d-4f44-45f0-9eda-c9d599b58277" }));

        var methodBuilder = typeBuilder.DefineMethod("M1", MethodAttributes.Public | MethodAttributes.Abstract | MethodAttributes.Virtual | MethodAttributes.NewSlot | MethodAttributes.HideBySig,
                                typeof(void), new[] { typeof(int), typeof(int), typeof(int) });

        methodBuilder.DefineParameter(2, ParameterAttributes.Optional, null);
        methodBuilder.DefineParameter(3, ParameterAttributes.None, "");

        typeBuilder.CreateType();

        assemblyBuilder.Save(@"ParametersWithoutNames.dll");
    }
}
