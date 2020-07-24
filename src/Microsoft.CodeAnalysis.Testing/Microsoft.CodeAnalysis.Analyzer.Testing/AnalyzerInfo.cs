// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.CodeAnalysis.Testing
{
    internal static class AnalyzerInfo
    {
        /// <summary>
        /// The <see cref="Attribute.Attribute()"/> constructor.
        /// </summary>
        private static readonly ConstructorInfo AttributeBaseClassCtor = typeof(Attribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(ctor => ctor.GetParameters().Length == 0);

        /// <summary>
        /// The <see cref="AttributeUsageAttribute(AttributeTargets)"/> constructor.
        /// </summary>
        private static readonly ConstructorInfo AttributeUsageCtor = typeof(AttributeUsageAttribute).GetConstructor(new Type[] { typeof(AttributeTargets) })!;

        /// <summary>
        /// The <see cref="AttributeUsageAttribute.AllowMultiple"/> property.
        /// </summary>
        private static readonly PropertyInfo AttributeUsageAllowMultipleProperty = typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))!;

        private static readonly object s_codeGenerationLock = new object();
        private static Type? s_generatedAnalysisContextType;

        public static bool HasConfiguredGeneratedCodeAnalysis(DiagnosticAnalyzer analyzer)
        {
            var context = CreateAnalysisContext();
            analyzer.Initialize(context);
            return context.ConfiguresGeneratedCode;
        }

        private static CustomAnalysisContext CreateAnalysisContext()
        {
            if (s_generatedAnalysisContextType is null)
            {
                lock (s_codeGenerationLock)
                {
                    if (s_generatedAnalysisContextType is null)
                    {
                        s_generatedAnalysisContextType = GenerateAnalysisContextType();
                    }
                }
            }

            return (CustomAnalysisContext)Activator.CreateInstance(s_generatedAnalysisContextType)!;
        }

        private static Type GenerateAnalysisContextType()
        {
            Debug.Assert(Monitor.IsEntered(s_codeGenerationLock), "Assertion failed: Monitor.IsEntered(s_codeGenerationLock)");

            var moduleBuilder = CreateModuleBuilder();
            var typeBuilder = moduleBuilder.DefineType("CustomAnalysisContextImpl", TypeAttributes.Public, typeof(CustomAnalysisContext));
            foreach (var method in typeof(AnalysisContext).GetTypeInfo().DeclaredMethods)
            {
                if (!method.IsVirtual && !method.IsAbstract)
                {
                    continue;
                }

                if (method.ReturnType != typeof(void))
                {
                    continue;
                }

                var accessAttributes = method.Attributes & MethodAttributes.MemberAccessMask;
                var methodBuilder = typeBuilder.DefineMethod(method.Name, accessAttributes | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.Virtual, method.ReturnType, method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
                if (method.IsGenericMethod)
                {
                    var genericParameterBuilders = methodBuilder.DefineGenericParameters(method.GetGenericArguments().Select(type => type.Name).ToArray());
                    for (var i = 0; i < genericParameterBuilders.Length; i++)
                    {
                        var parameterBuilder = genericParameterBuilders[i];
                        parameterBuilder.SetBaseTypeConstraint(method.GetGenericArguments()[i].GetTypeInfo().BaseType);
                        parameterBuilder.SetInterfaceConstraints(method.GetGenericArguments()[i].GetTypeInfo().ImplementedInterfaces.ToArray());
                    }
                }

                var generator = methodBuilder.GetILGenerator();

                if (method.Name == "ConfigureGeneratedCodeAnalysis")
                {
                    var generatedCodeField = typeof(CustomAnalysisContext).GetTypeInfo().DeclaredFields.Single(field => field.Name == nameof(CustomAnalysisContext.ConfiguresGeneratedCode));
                    generator.Emit(OpCodes.Ldarg_0);
                    generator.Emit(OpCodes.Ldc_I4_1);
                    generator.Emit(OpCodes.Stfld, generatedCodeField);
                }

                generator.Emit(OpCodes.Ret);
            }

            return typeBuilder.CreateTypeInfo()!.AsType();
        }

        private static ModuleBuilder CreateModuleBuilder()
        {
            var assemblyBuilder = CreateAssemblyBuilder();
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("codeAnalysisProxies");

            SkipVisibilityChecksFor(assemblyBuilder, moduleBuilder, typeof(CustomAnalysisContext));

            return moduleBuilder;
        }

        private static AssemblyBuilder CreateAssemblyBuilder()
        {
            var assemblyName = new AssemblyName($"codeAnalysisProxies_{Guid.NewGuid()}");
            return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
        }

        private static void SkipVisibilityChecksFor(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder, Type type)
        {
            var attributeBuilder = new CustomAttributeBuilder(GetMagicAttributeCtor(moduleBuilder), new object[] { type.GetTypeInfo().Assembly.GetName().Name! });
            assemblyBuilder.SetCustomAttribute(attributeBuilder);
        }

        private static ConstructorInfo GetMagicAttributeCtor(ModuleBuilder moduleBuilder)
        {
            var magicAttribute = EmitMagicAttribute(moduleBuilder);
            return magicAttribute.GetConstructor(new Type[] { typeof(string) })!;
        }

        private static System.Reflection.TypeInfo EmitMagicAttribute(ModuleBuilder moduleBuilder)
        {
            var tb = moduleBuilder.DefineType(
                "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
                TypeAttributes.NotPublic,
                typeof(Attribute));

            var attributeUsage = new CustomAttributeBuilder(
                AttributeUsageCtor,
                new object[] { AttributeTargets.Assembly },
                new PropertyInfo[] { AttributeUsageAllowMultipleProperty },
                new object[] { false });
            tb.SetCustomAttribute(attributeUsage);

            var cb = tb.DefineConstructor(
                MethodAttributes.Public |
                MethodAttributes.HideBySig |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName,
                CallingConventions.Standard,
                new Type[] { typeof(string) });
            cb.DefineParameter(1, ParameterAttributes.None, "assemblyName");

            var il = cb.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, AttributeBaseClassCtor);
            il.Emit(OpCodes.Ret);

            return tb.CreateTypeInfo()!;
        }

        internal abstract class CustomAnalysisContext : AnalysisContext
        {
            [SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1401:Fields should be private", Justification = "Set via reflection.")]
            public bool ConfiguresGeneratedCode;
        }
    }
}
