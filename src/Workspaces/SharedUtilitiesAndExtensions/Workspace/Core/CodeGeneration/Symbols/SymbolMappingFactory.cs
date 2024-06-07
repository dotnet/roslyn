// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CodeGeneration;

using TypeInfo = System.Reflection.TypeInfo;

/// <summary>
/// Generates implementations of an interface at runtime based on a mirroring interface implementation provided by the
/// type. The simplest concrete example is generating a type that implements <see cref="ISymbol"/> from a type that
/// implements <see cref="ICodeGenerationSymbol"/>.
/// </summary>
/// <remarks>
/// <para>The mapping process for a type <c>T</c> produces a type <c>U</c> which is derived from <c>T</c>. Each
/// interface implemented by <c>T</c> is looked up in <paramref name="interfaceMapping"/>, and if a match is found, the
/// type <c>U</c> will implement the mapped interface found in the collection. Every member of the mapped run-time
/// interface will be examined against the compile-time interface for a matching definition, and if found, that
/// implementation will be used. Otherwise, a default implementation will be generated based on the parameter and return
/// types.</para>
///
/// <para>This factory allows for future versions of an interface (e.g. <see cref="ISymbol"/>) to contain members which
/// were not present at the time a library was compiled, and still be able to provide implementations of the complete
/// interface.</para>
/// </remarks>
/// <seealso href="https://github.com/dotnet/roslyn/issues/72811"/>
/// <param name="interfaceMapping">The mapping of compile-time interfaces to run-time interfaces.</param>
internal abstract class SymbolMappingFactory(FrozenDictionary<Type, Type> interfaceMapping)
{
    /// <summary>
    /// The <see cref="Attribute.Attribute()"/> constructor.
    /// </summary>
    private static readonly ConstructorInfo s_attributeBaseClassCtor = typeof(Attribute).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(ctor => ctor.GetParameters().Length == 0);

    /// <summary>
    /// The <see cref="AttributeUsageAttribute(AttributeTargets)"/> constructor.
    /// </summary>
    private static readonly ConstructorInfo s_attributeUsageCtor = typeof(AttributeUsageAttribute).GetConstructor([typeof(AttributeTargets)])!;

    /// <summary>
    /// The <see cref="AttributeUsageAttribute.AllowMultiple"/> property.
    /// </summary>
    private static readonly PropertyInfo s_attributeUsageAllowMultipleProperty = typeof(AttributeUsageAttribute).GetProperty(nameof(AttributeUsageAttribute.AllowMultiple))!;

    private readonly object _codeGenerationLock = new();
    private readonly FrozenDictionary<Type, Type> _interfaceMapping = interfaceMapping;

    private ImmutableDictionary<Type, Type> _implementationTypes = ImmutableDictionary<Type, Type>.Empty;
    private ImmutableDictionary<Type, ImmutableArray<(ImmutableArray<Type> parameterTypes, Delegate constructor)>> _constructors = ImmutableDictionary<Type, ImmutableArray<(ImmutableArray<Type> parameterTypes, Delegate constructor)>>.Empty;

    protected Delegate GetOrCreateConstructor(Type type, ImmutableArray<Type> parameterTypes)
    {
        if (_constructors.TryGetValue(type, out var constructors))
        {
            foreach (var (types, constructor) in constructors)
            {
                if (types.SequenceEqual(parameterTypes))
                    return constructor;
            }
        }

        foreach (var constructor in type.GetConstructors(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
        {
            var parameters = constructor.GetParameters();
            if (parameters.Length != parameterTypes.Length)
                continue;

            for (var i = 0; i < parameters.Length; i++)
            {
                if (parameters[i].ParameterType != parameterTypes[i])
                    continue;
            }

            var parameterExpressions = new ParameterExpression[parameterTypes.Length];
            for (var i = 0; i < parameterTypes.Length; i++)
            {
                parameterExpressions[i] = Expression.Parameter(parameterTypes[i], parameters[i].Name);
            }

            var body = Expression.New(constructor, parameterExpressions);
            var lambda = Expression.Lambda(body, parameterExpressions);
            var compiled = lambda.Compile();
            ImmutableInterlocked.AddOrUpdate(
                ref _constructors,
                type,
                _ => ImmutableArray.Create((parameterTypes, compiled)),
                (_, existing) =>
                {
                    foreach (var (types, constructor) in existing)
                    {
                        if (types.SequenceEqual(parameterTypes))
                            return existing;
                    }

                    return existing.Add((parameterTypes, compiled));
                });

            // On second call, the constructor is guaranteed to be located in s_constructors
            return GetOrCreateConstructor(type, parameterTypes);
        }

        throw ExceptionUtilities.Unreachable();
    }

    protected Type GetOrCreateImplementationType(Type baseType)
    {
        if (_implementationTypes.TryGetValue(baseType, out var implementationType))
        {
            return implementationType;
        }

        lock (_codeGenerationLock)
        {
            if (_implementationTypes.TryGetValue(baseType, out implementationType))
            {
                return implementationType;
            }

            var baseTypeInterfaces = baseType.GetInterfaces();
            var interfaceMappings = baseTypeInterfaces.Select(type => (type, IReadOnlyDictionaryExtensions.GetValueOrDefault(_interfaceMapping, type))).Where(pair => pair.Item2 is not null)!.ToImmutableArray<(Type? codeGenerationInterface, Type compilerInterface)>();
            var allRemainingInterfacesToImplement = interfaceMappings
                .SelectMany(x => x.compilerInterface.GetInterfaces())
                .Distinct()
                .Where(x => !baseTypeInterfaces.Contains(x) && !interfaceMappings.Any(mapping => x == mapping.compilerInterface))
                .Select(x => (codeGenerationInterface: (Type?)null, compilerInterface: x));
            interfaceMappings = interfaceMappings.AddRange(allRemainingInterfacesToImplement);

            var moduleBuilder = CreateModuleBuilder(baseType.Assembly);
            var typeBuilder = moduleBuilder.DefineType(
                $"{baseType.Namespace}.{baseType.Name}Impl",
                TypeAttributes.Public,
                baseType,
                interfaceMappings.Select(pair => pair.compilerInterface).ToArray());
            // Define constructors matching each constructor from the base type
            foreach (var constructor in baseType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (constructor.IsPrivate)
                    continue;

                var parameterTypes = Array.ConvertAll(constructor.GetParameters(), static p => p.ParameterType);
                var ctor = typeBuilder.DefineConstructor(
                    MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    CallingConventions.Standard,
                    parameterTypes);

                var ilGenerator = ctor.GetILGenerator();
                GenerateReturnFromInvokingBaseMember(ilGenerator, constructor, parameterTypes);
            }

            // Implement each new interface
            foreach (var (codeGenerationInterface, compilerInterface) in interfaceMappings)
            {
                var mappedCompilerMethods = new HashSet<MethodInfo>();
                var mappedCodeGenerationMethods = new HashSet<MethodInfo>();

                // Properties
                foreach (var declaredProperty in compilerInterface.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
                {
                    var indexParameterTypes = Array.ConvertAll(declaredProperty.GetIndexParameters(), static p => p.ParameterType);
                    var propertyBuilder = typeBuilder.DefineProperty(compilerInterface.FullName + '.' + declaredProperty.Name, PropertyAttributes.None, declaredProperty.PropertyType, indexParameterTypes);

                    // If the same property already exists on codeGenerationInterface, just mark the implementation of
                    // that property as the implementation of the compilerInterface property.
                    if (declaredProperty.CanRead && FindImplementation(baseType, declaredProperty.GetMethod!, out var codeGenerationMethod, out var baseMethod))
                    {
                        if (baseMethod.IsPublic && baseMethod.Name == declaredProperty.GetMethod!.Name)
                        {
                            // The runtime will automatically locate this method as the interface implementation
                        }
                        else
                        {
                            // Need to create an implementation of the interface member
                            var parameterTypes = Array.ConvertAll(declaredProperty.GetMethod!.GetParameters(), static p => p.ParameterType);
                            var method = typeBuilder.DefineMethod(
                                compilerInterface.FullName + '.' + declaredProperty.GetMethod.Name,
                                MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                                CallingConventions.Standard,
                                declaredProperty.PropertyType,
                                parameterTypes);

                            var ilGenerator = method.GetILGenerator();
                            GenerateReturnFromInvokingBaseMember(ilGenerator, baseMethod, parameterTypes);

                            propertyBuilder.SetGetMethod(method);
                            typeBuilder.DefineMethodOverride(method, declaredProperty.GetMethod!);
                        }

                        mappedCompilerMethods.Add(declaredProperty.GetMethod!);
                        mappedCodeGenerationMethods.Add(codeGenerationMethod);
                    }
                    else if (declaredProperty.CanRead)
                    {
                        // Otherwise define a default implementation.
                        var parameterTypes = Array.ConvertAll(declaredProperty.GetMethod!.GetParameters(), static p => p.ParameterType);
                        var method = typeBuilder.DefineMethod(
                            compilerInterface.FullName + '.' + declaredProperty.GetMethod.Name,
                            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                            CallingConventions.Standard,
                            declaredProperty.PropertyType,
                            parameterTypes);

                        var ilGenerator = method.GetILGenerator();
                        GenerateDefaultReturnForType(ilGenerator, declaredProperty.PropertyType);

                        propertyBuilder.SetGetMethod(method);
                        typeBuilder.DefineMethodOverride(method, declaredProperty.GetMethod!);

                        mappedCompilerMethods.Add(declaredProperty.GetMethod!);
                    }

                    if (declaredProperty.CanWrite && FindImplementation(baseType, declaredProperty.SetMethod!, out codeGenerationMethod, out baseMethod))
                    {
                        if (baseMethod.IsPublic && baseMethod.Name == declaredProperty.SetMethod!.Name)
                        {
                            // The runtime will automatically locate this method as the interface implementation
                        }
                        else
                        {
                            // Need to create an implementation of the interface member
                            var parameterTypes = Array.ConvertAll(declaredProperty.SetMethod!.GetParameters(), static p => p.ParameterType);
                            var method = typeBuilder.DefineMethod(
                                compilerInterface.FullName + '.' + declaredProperty.SetMethod.Name,
                                MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                                CallingConventions.Standard,
                                typeof(void),
                                parameterTypes);

                            var ilGenerator = method.GetILGenerator();
                            GenerateReturnFromInvokingBaseMember(ilGenerator, baseMethod, parameterTypes);

                            propertyBuilder.SetSetMethod(method);
                            typeBuilder.DefineMethodOverride(method, declaredProperty.SetMethod!);
                        }

                        mappedCompilerMethods.Add(declaredProperty.SetMethod!);
                        mappedCodeGenerationMethods.Add(codeGenerationMethod);
                    }
                    else if (declaredProperty.CanWrite)
                    {
                        // Otherwise define a default implementation, which for setters just throws.
                        var parameterTypes = Array.ConvertAll(declaredProperty.SetMethod!.GetParameters(), static p => p.ParameterType);
                        var method = typeBuilder.DefineMethod(
                            compilerInterface.FullName + '.' + declaredProperty.SetMethod.Name,
                            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                            CallingConventions.Standard,
                            typeof(void),
                            parameterTypes);

                        var ilGenerator = method.GetILGenerator();
                        var ctor = typeof(NotSupportedException).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, [], null)!;
                        ilGenerator.Emit(OpCodes.Newobj, ctor);
                        ilGenerator.Emit(OpCodes.Throw);

                        propertyBuilder.SetSetMethod(method);
                        typeBuilder.DefineMethodOverride(method, declaredProperty.SetMethod!);

                        mappedCompilerMethods.Add(declaredProperty.SetMethod!);
                    }
                }

                // Methods
                foreach (var declaredMethod in compilerInterface.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
                {
                    if (!mappedCompilerMethods.Add(declaredMethod))
                    {
                        // This method was already mapped as part of a property.
                        continue;
                    }

                    // If the same method already exists on codeGenerationInterface, just mark the implementation of
                    // that method as the implementation of the compilerInterface method.
                    if (FindImplementation(baseType, declaredMethod, out var codeGenerationMethod, out var baseMethod))
                    {
                        if (baseMethod.IsPublic && baseMethod.Name == declaredMethod.Name)
                        {
                            // The runtime will automatically locate this method as the interface implementation
                        }
                        else
                        {
                            // Need to create an implementation of the interface member
                            var parameterTypes = Array.ConvertAll(declaredMethod.GetParameters(), static p => p.ParameterType);
                            var method = typeBuilder.DefineMethod(
                                compilerInterface.FullName + '.' + declaredMethod.Name,
                                MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                                CallingConventions.Standard,
                                declaredMethod.ReturnType,
                                parameterTypes);

                            GenericTypeParameterBuilder[] genericTypeParameters;
                            if (declaredMethod.IsGenericMethod)
                            {
                                method.DefineGenericParameters(Array.ConvertAll(declaredMethod.GetGenericMethodDefinition().GetGenericArguments(), static argument => argument.Name));
                            }
                            else
                            {
                                genericTypeParameters = [];
                            }

                            var ilGenerator = method.GetILGenerator();
                            GenerateReturnFromInvokingBaseMember(ilGenerator, baseMethod, parameterTypes);

                            typeBuilder.DefineMethodOverride(method, declaredMethod);
                        }

                        mappedCompilerMethods.Add(declaredMethod);
                        mappedCodeGenerationMethods.Add(codeGenerationMethod);
                    }
                    else
                    {
                        // Otherwise define a default implementation.
                        var parameterTypes = Array.ConvertAll(declaredMethod.GetParameters(), static p => p.ParameterType);
                        var method = typeBuilder.DefineMethod(
                            compilerInterface.FullName + '.' + declaredMethod.Name,
                            MethodAttributes.Private | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
                            CallingConventions.Standard,
                            declaredMethod.ReturnType,
                            parameterTypes);

                        GenericTypeParameterBuilder[] genericTypeParameters;
                        if (declaredMethod.IsGenericMethod)
                        {
                            method.DefineGenericParameters(Array.ConvertAll(declaredMethod.GetGenericMethodDefinition().GetGenericArguments(), static argument => argument.Name));
                        }
                        else
                        {
                            genericTypeParameters = [];
                        }

                        var ilGenerator = method.GetILGenerator();
                        if (declaredMethod.ReturnType != typeof(void))
                        {
                            GenerateDefaultReturnForType(ilGenerator, declaredMethod.ReturnType);
                        }
                        else
                        {
                            var ctor = typeof(NotSupportedException).GetConstructor(BindingFlags.Instance | BindingFlags.Public, null, [], null)!;
                            ilGenerator.Emit(OpCodes.Newobj, ctor);
                            ilGenerator.Emit(OpCodes.Throw);
                        }

                        typeBuilder.DefineMethodOverride(method, declaredMethod);

                        mappedCompilerMethods.Add(declaredMethod);
                    }
                }

                // The code generation interface should not declare any members which have no mapping to the code style
                // interface. Note that this check may be altered in the future to support light-up scenarios.
                if (codeGenerationInterface is null)
                {
                    Contract.ThrowIfFalse(mappedCodeGenerationMethods.Count == 0);
                }
                else
                {
                    foreach (var declaredCodeGenerationMethod in codeGenerationInterface.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public))
                    {
                        Contract.ThrowIfFalse(mappedCodeGenerationMethods.Contains(declaredCodeGenerationMethod), $"Expected to map code generation method '{declaredCodeGenerationMethod}'");
                    }
                }
            }

            var constructedType = typeBuilder.CreateTypeInfo()?.AsType();
            Contract.ThrowIfNull(constructedType);
            Contract.ThrowIfFalse(ImmutableInterlocked.TryAdd(ref _implementationTypes, baseType, constructedType));

            return constructedType;
        }
    }

    private static void LoadLocal(ILGenerator ilGenerator, LocalBuilder local)
    {
        switch (local.LocalIndex)
        {
            case 0:
                ilGenerator.Emit(OpCodes.Ldloc_0);
                break;
            case 1:
                ilGenerator.Emit(OpCodes.Ldloc_1);
                break;
            case 2:
                ilGenerator.Emit(OpCodes.Ldloc_2);
                break;
            case 3:
                ilGenerator.Emit(OpCodes.Ldloc_3);
                break;
            case <= byte.MaxValue:
                ilGenerator.Emit(OpCodes.Ldloc_S, local);
                break;
            default:
                ilGenerator.Emit(OpCodes.Ldloc, local);
                break;
        }
    }

    private static void LoadLocalAddress(ILGenerator ilGenerator, LocalBuilder local)
    {
        switch (local.LocalIndex)
        {
            case <= byte.MaxValue:
                ilGenerator.Emit(OpCodes.Ldloca_S, local);
                break;
            default:
                ilGenerator.Emit(OpCodes.Ldloca, local);
                break;
        }
    }

    private static void GenerateDefaultReturnForType(ILGenerator ilGenerator, Type type)
    {
        if (type != typeof(void))
        {
            // Types that do not currently have special cases, but may need them:
            //
            //  Task
            //  Task<T>
            //  ValueTask<T> where we want to return something other than default(T)
            //  IEnumerable<T> and derived interfaces
            //  Immutable collections, except for ImmutableArray<T> which returns empty
            var genericTypeDefinition = type.IsGenericType ? type.GetGenericTypeDefinition() : type;
            if (genericTypeDefinition == typeof(ImmutableArray<>))
            {
                // Return ImmutableArray<T>.Empty instead of default(ImmutableArray<T>)
                var fieldInfo = type.GetField(nameof(ImmutableArray<int>.Empty), BindingFlags.Public | BindingFlags.Static);
                Contract.ThrowIfNull(fieldInfo);
                ilGenerator.Emit(OpCodes.Ldsfld, fieldInfo);
            }
            else if (genericTypeDefinition.IsValueType)
            {
                var local = ilGenerator.DeclareLocal(type);
                LoadLocalAddress(ilGenerator, local);
                ilGenerator.Emit(OpCodes.Initobj, type);
                LoadLocal(ilGenerator, local);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Ldnull);
            }
        }

        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void GenerateReturnFromInvokingBaseMember(ILGenerator ilGenerator, MethodBase method, Type[] parameterTypes)
    {
        LoadParameters(ilGenerator, parameterTypes);

        switch (method)
        {
            case ConstructorInfo con:
                ilGenerator.Emit(OpCodes.Call, con);
                break;

            case MethodInfo meth:
                ilGenerator.Emit(OpCodes.Call, meth);
                break;

            default:
                throw ExceptionUtilities.UnexpectedValue(method);
        }

        ilGenerator.Emit(OpCodes.Ret);
    }

    private static void LoadParameters(ILGenerator ilGenerator, Type[] parameterTypes)
    {
        ilGenerator.Emit(OpCodes.Ldarg_0);
        for (var i = 0; i < parameterTypes.Length; i++)
        {
            // Note: all values are offset by 1 because the first argument to the base constructor is 'this',
            // but that parameter is not part of GetParameters().
            switch (i)
            {
                case 0:
                    ilGenerator.Emit(OpCodes.Ldarg_1);
                    break;
                case 1:
                    ilGenerator.Emit(OpCodes.Ldarg_2);
                    break;
                case 2:
                    ilGenerator.Emit(OpCodes.Ldarg_3);
                    break;
                case >= 3 and < 255:
                    ilGenerator.Emit(OpCodes.Ldarg_S, (byte)(i + 1));
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(i);
            }
        }
    }

    private bool FindImplementation(Type type, MethodInfo method, [NotNullWhen(true)] out MethodInfo? codeGenerationMethod, [NotNullWhen(true)] out MethodInfo? baseMethod)
    {
        var originalInterface = method.DeclaringType;
        var codeStyleInterface = _interfaceMapping.FirstOrDefault(pair => pair.Value == originalInterface).Key;
        if (codeStyleInterface is null)
        {
            codeGenerationMethod = null;
            baseMethod = null;
            return false;
        }

        var methodParameters = method.GetParameters();
        var genericArguments = method.IsGenericMethod ? method.GetGenericArguments() : [];
        foreach (var codeStyleMethod in codeStyleInterface.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (codeStyleMethod.Name != method.Name)
                continue;

            if (!IsEquivalentTypeForMapping(method.ReturnType, codeStyleMethod.ReturnType))
                continue;

            if (method.IsGenericMethod)
            {
                if (!codeStyleMethod.IsGenericMethod)
                    continue;

                var codeStyleGenericArguments = codeStyleMethod.GetGenericArguments();
                if (codeStyleGenericArguments.Length != genericArguments.Length)
                    continue;

                for (var i = 0; i < codeStyleGenericArguments.Length; i++)
                {
                    if (!IsEquivalentTypeForMapping(genericArguments[i], codeStyleGenericArguments[i]))
                        continue;
                }
            }

            var codeStyleParameters = codeStyleMethod.GetParameters();
            if (codeStyleParameters.Length != methodParameters.Length)
                continue;

            for (var i = 0; i < codeStyleParameters.Length; i++)
            {
                if (!IsEquivalentTypeForMapping(methodParameters[i].ParameterType, codeStyleParameters[i].ParameterType))
                    continue;
            }

            // We have a matching method. Now just need to find the implementation of this method in the class.
            var mapping = type.GetInterfaceMap(codeStyleInterface);
            for (var i = 0; i < mapping.InterfaceMethods.Length; i++)
            {
                if (mapping.InterfaceMethods[i] == codeStyleMethod)
                {
                    codeGenerationMethod = codeStyleMethod;
                    baseMethod = mapping.TargetMethods[i];
                    return true;
                }
            }
        }

        codeGenerationMethod = null;
        baseMethod = null;
        return false;
    }

    private static bool IsEquivalentTypeForMapping(Type compilerType, Type codeStyleType)
    {
        if (compilerType.IsGenericParameter)
        {
            if (!codeStyleType.IsGenericParameter)
                return false;

            return compilerType.GenericParameterPosition == codeStyleType.GenericParameterPosition;
        }

        return compilerType == codeStyleType;
    }

    private static ModuleBuilder CreateModuleBuilder(Assembly baseTypeAssembly)
    {
        var assemblyBuilder = CreateAssemblyBuilder();
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("codeAnalysisProxies");

        SkipVisibilityChecksFor(assemblyBuilder, moduleBuilder, baseTypeAssembly);

        return moduleBuilder;
    }

    private static AssemblyBuilder CreateAssemblyBuilder()
    {
        var assemblyName = new AssemblyName($"codeAnalysisProxies_{Guid.NewGuid()}");
        return AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
    }

    private static void SkipVisibilityChecksFor(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder, Assembly baseTypeAssembly)
    {
        var attributeBuilder = new CustomAttributeBuilder(GetMagicAttributeCtor(moduleBuilder), [baseTypeAssembly.GetName().Name!]);
        assemblyBuilder.SetCustomAttribute(attributeBuilder);
    }

    private static ConstructorInfo GetMagicAttributeCtor(ModuleBuilder moduleBuilder)
    {
        var magicAttribute = EmitMagicAttribute(moduleBuilder);
        return magicAttribute.GetConstructor([typeof(string)])!;
    }

    private static TypeInfo EmitMagicAttribute(ModuleBuilder moduleBuilder)
    {
        var tb = moduleBuilder.DefineType(
            "System.Runtime.CompilerServices.IgnoresAccessChecksToAttribute",
            TypeAttributes.NotPublic,
            typeof(Attribute));

        var attributeUsage = new CustomAttributeBuilder(
            s_attributeUsageCtor,
            [AttributeTargets.Assembly],
            [s_attributeUsageAllowMultipleProperty],
            [false]);
        tb.SetCustomAttribute(attributeUsage);

        var cb = tb.DefineConstructor(
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName,
            CallingConventions.Standard,
            [typeof(string)]);
        cb.DefineParameter(1, ParameterAttributes.None, "assemblyName");

        var il = cb.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, s_attributeBaseClassCtor);
        il.Emit(OpCodes.Ret);

        return tb.CreateTypeInfo()!;
    }
}
