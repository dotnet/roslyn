// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeGen;
using Microsoft.CodeAnalysis.Emit;
using Roslyn.Utilities;
using Cci = Microsoft.Cci;

namespace Microsoft.CodeAnalysis.Scripting.Emit
{
    /// <summary>
    /// Emits types in a given <see cref="Cci.IModule"/> to a <see cref="ModuleBuilder"/>.
    /// </summary>
    /// <remarks>
    /// Some types can't be emitted either due to the current Reflection.Emit implementation bugs/architecture or 
    /// due to limitations of collectible assemblies (http://msdn.microsoft.com/en-us/library/dd554932.aspx).
    /// 
    /// The main problem with Reflection.Emit is that it uses two-phase type creation process. <see cref="TypeBuilder"/>s are defined in the first phase
    /// and created (baked) one by one in the second. This isn't compatible with the way how CLR Type Loader loads cyclic type references. 
    /// 
    /// The following rules apply and imply a partial order on the type baking (based upon observation and tests; they are not clearly documented anywhere) :
    /// 1) Before a type can be baked its derived type and all interfaces it implements needs to be baked.
    /// 2) Before a generic type can be baked the constraints of its generic type parameters need to be baked.
    /// 3) Before a type can be baked all types of its fields that are value types need to be baked (reference typed fields don't).
    /// 4) If a type is dependent on a generic type instantiation it is also dependent on its generic arguments.
    /// 5) If a type is dependent on a type nested in another type the outer type need to be baked first.
    ///
    /// If these rule imply an order that is not satisfiable (there is a cycle) the emitter throws <see cref="NotSupportedException"/>.
    /// 
    /// TODO (tomat):
    /// Some of these rules can be circumvented by using AppDomain.TypeLoad event, but it's unclear which exactly (I suspect #4).
    /// 
    /// Examples of type topologies that can't be emitted today are:
    /// 
    /// <code>
    /// class B{T} where T : A              // B depends on A by rule #2
    /// class A : B{A}                      // A depends on B by rule #1
    /// </code>
    /// 
    /// <code>
    /// public class E                      
    /// {
    ///     public struct N2
    ///     {
    ///         public N3 n1;               // E.N2 depends on E.N3 by rule #3 and thus on E by rule #5
    ///     }
    ///     public struct N3    
    ///     {
    ///     }
    ///     N2 n2;                          // E depends on E.N2 by rule #3
    /// }
    /// </code>
    /// </remarks>
    internal sealed partial class ReflectionEmitter
    {
        private readonly Cci.IModule _module;
        private readonly EmitContext _context;
        private readonly ModuleBuilder _builder;
        private readonly ITokenDeferral _tokenResolver;
        private readonly AssemblyLoader _assemblyLoader;

        // builders created for various members:
        private readonly Dictionary<Cci.IAssemblyReference, Assembly> _referencedAssemblies = new Dictionary<Cci.IAssemblyReference, Assembly>();
        private readonly Dictionary<Cci.ITypeDefinition, TypeBuilder> _typeBuilders = new Dictionary<Cci.ITypeDefinition, TypeBuilder>();
        private readonly Dictionary<Cci.IGenericParameterReference, GenericTypeParameterBuilder> _genericParameterBuilders = new Dictionary<Cci.IGenericParameterReference, GenericTypeParameterBuilder>();
        private readonly Dictionary<Cci.IMethodDefinition, MethodBuilder> _methodBuilders = new Dictionary<Cci.IMethodDefinition, MethodBuilder>();
        private readonly Dictionary<Cci.IMethodDefinition, ConstructorBuilder> _constructorBuilders = new Dictionary<Cci.IMethodDefinition, ConstructorBuilder>();
        private readonly Dictionary<Cci.IFieldDefinition, FieldBuilder> _fieldBuilders = new Dictionary<Cci.IFieldDefinition, FieldBuilder>();

        // cached references to external members:
        private readonly Dictionary<Cci.ITypeReference, Type> _typeRefs = new Dictionary<Cci.ITypeReference, Type>();
        private readonly Dictionary<Cci.IMethodReference, MethodBase> _methodRefs = new Dictionary<Cci.IMethodReference, MethodBase>();
        private readonly Dictionary<Cci.IFieldReference, FieldInfo> _fieldRefs = new Dictionary<Cci.IFieldReference, FieldInfo>();

        // captures type creation dependencies { type -> dependent types }
        private readonly Dictionary<TypeBuilder, List<TypeBuilder>> _dependencyGraph = new Dictionary<TypeBuilder, List<TypeBuilder>>();

        private ReflectionEmitter(
            EmitContext context,
            IEnumerable<KeyValuePair<Cci.IAssemblyReference, string>> referencedAssemblies,
            ModuleBuilder builder,
            AssemblyLoader assemblyLoader)
        {
            Debug.Assert(context.Module != null);
            Debug.Assert(referencedAssemblies != null);
            Debug.Assert(builder != null);
            Debug.Assert(assemblyLoader != null);

            _module = context.Module;
            _context = context;
            _builder = builder;
            _tokenResolver = (ITokenDeferral)context.Module;
            _assemblyLoader = assemblyLoader;
            _referencedAssemblies = LoadReferencedAssemblies(referencedAssemblies);
        }

        #region Resolution

        // Resolve* methods resolve CCI references to concrete Reflection objects (assemblies, types, methods, fields) and load them if necessary.

        private Dictionary<Cci.IAssemblyReference, Assembly> LoadReferencedAssemblies(IEnumerable<KeyValuePair<Cci.IAssemblyReference, string>> assemblyRefs)
        {
            // Loads all referenced assemblies. We do it upfront so that
            // 1) the assembly loader is notified about all possible dependencies and can handle/avoid AssemblyResolve events.
            // 2) Fail fast before we start emitting any code.
            // The referenced assemblies are going to be loaded during emit at some point so there is no point in deferring the loads.
            // TODO (tomat): only load references that are actually used in the metadata

            var result = new Dictionary<Cci.IAssemblyReference, Assembly>();

            foreach (var assemblyLocationAndReference in assemblyRefs)
            {
                var assemblyRef = assemblyLocationAndReference.Key;
                var location = assemblyLocationAndReference.Value;

                result.Add(assemblyRef, LoadAssembly(assemblyRef, location));
            }

            return result;
        }

        private Assembly LoadAssembly(Cci.IAssemblyReference assemblyRef, string locationOpt)
        {
            var identity = new AssemblyIdentity(
                name: assemblyRef.Name,
                version: assemblyRef.Version,
                cultureName: assemblyRef.Culture,
                publicKeyOrToken: assemblyRef.PublicKeyToken,
                hasPublicKey: false,
                isRetargetable: assemblyRef.IsRetargetable,
                contentType: assemblyRef.ContentType
            );

            Assembly assembly;
            try
            {
                assembly = _assemblyLoader.Load(identity, locationOpt);
            }
            catch (NotSupportedException)
            {
                throw;
            }
            catch (Exception e)
            {
                throw new InvalidOperationException("AssemblyLoader failed to load assembly " + identity, e);
            }

            if (assembly == null)
            {
                throw new InvalidOperationException("AssemblyLoader failed to load assembly " + identity);
            }

            return assembly;
        }

        private Assembly ResolveAssembly(Cci.IAssemblyReference assemblyRef)
        {
            Assembly assembly;
            if (_referencedAssemblies.TryGetValue(assemblyRef, out assembly))
            {
                return assembly;
            }

            // We pre-loaded all assemblies that were passed as explicit references to the compilation.
            // Now we need to handle those that weren't -- these should only be the previous submissions of interactive session.
            assembly = LoadAssembly(assemblyRef, locationOpt: null);
            _referencedAssemblies.Add(assemblyRef, assembly);
            return assembly;
        }

        /// <summary>
        /// Establishes a creation dependency of <paramref name="builder"/> on <paramref name="dependentType"/>. 
        /// The <paramref name="builder"/> can't be baked before <paramref name="dependentType"/> is.
        /// </summary>
        private void AddDependency(TypeBuilder builder, TypeBuilder dependentType)
        {
            // TODO (tomat): builder == dependentType fails for enums (see InteractiveSessionTests.Enums). Is this a Reflection bug?
            if (ReferenceEquals(builder, dependentType))
            {
                return;
            }

            List<TypeBuilder> existing;
            if (!_dependencyGraph.TryGetValue(builder, out existing))
            {
                _dependencyGraph.Add(builder, existing = new List<TypeBuilder>());
            }

            do
            {
                existing.Add(dependentType);
                dependentType = (TypeBuilder)dependentType.DeclaringType;
            }
            while (dependentType != null && builder != dependentType);
        }

        private struct GenericContext
        {
            public readonly Type[] TypeParameters;
            public readonly Type[] MethodParameters;

            public GenericContext(Type[] typeParameters, Type[] methodParameters)
            {
                TypeParameters = typeParameters;
                MethodParameters = methodParameters;
            }

            public bool IsNull
            {
                get
                {
                    Debug.Assert((TypeParameters == null) == (MethodParameters == null));
                    return TypeParameters == null;
                }
            }
        }

        private Type ResolveType(
            Cci.ITypeReference typeRef,
            GenericContext genericContext = default(GenericContext),
            TypeBuilder dependentType = null,
            bool valueTypeDependency = false)
        {
            var typeDef = typeRef.AsTypeDefinition(_context);
            if (typeDef != null && IsLocal(typeRef))
            {
                var builder = _typeBuilders[typeDef];
                if (dependentType != null && (!valueTypeDependency || builder.IsValueType))
                {
                    AddDependency(dependentType, builder);
                }

                return builder;
            }

            Type result;
            Cci.IGenericParameterReference genericParamRef;
            Cci.IGenericTypeInstanceReference genericRef;
            Cci.ISpecializedNestedTypeReference specializedNestedRef;
            Cci.INestedTypeReference nestedRef;
            Cci.IArrayTypeReference arrayType;
            Cci.IManagedPointerTypeReference refType;
            Cci.IPointerTypeReference ptrType;
            Cci.INamespaceTypeReference nsType;
            Cci.IModifiedTypeReference modType;

            if ((nsType = typeRef.AsNamespaceTypeReference) != null)
            {
                // cache lookup (no type dependencies to track):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                // a namespace type builder would already be found in type builders, so we don't get here:
                Debug.Assert(!IsLocal(typeRef));

                Cci.IUnitReference unitRef = nsType.GetUnit(_context);

                Cci.IAssemblyReference assemblyRef;
                var moduleRef = unitRef as Cci.IModuleReference;
                if (moduleRef != null)
                {
                    if (ReferenceEquals(moduleRef.GetContainingAssembly(_context), _module.GetContainingAssembly(_context)))
                    {
                        throw new NotSupportedException("Ref.Emit limitation: modules not supported");
                    }
                    else
                    {
                        assemblyRef = moduleRef.GetContainingAssembly(_context);
                    }
                }
                else
                {
                    assemblyRef = unitRef as Cci.IAssemblyReference;
                }

                // We only track dependency among type builders so we don't need to track it here.
                result = ResolveType(ResolveAssembly(assemblyRef), nsType);
            }
            else if ((specializedNestedRef = typeRef.AsSpecializedNestedTypeReference) != null)
            {
                Type unspecialized = ResolveType(specializedNestedRef.UnspecializedVersion, genericContext, dependentType, valueTypeDependency);

                // the resulting type doesn't depend on generic arguments if it is not a value type:
                if (valueTypeDependency && !unspecialized.IsValueType)
                {
                    dependentType = null;
                }

                Type[] typeArgs = ResolveGenericArguments(specializedNestedRef, genericContext, dependentType);

                // cache lookup (all type dependencies already established above):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                result = unspecialized.MakeGenericType(typeArgs);
            }
            else if ((genericRef = typeRef.AsGenericTypeInstanceReference) != null)
            {
                Type genericType = ResolveType(genericRef.GenericType, genericContext, dependentType, valueTypeDependency);

                // the resulting type doesn't depend on generic arguments if it is not a value type:
                if (valueTypeDependency && !genericType.IsValueType)
                {
                    dependentType = null;
                }

                Type[] typeArgs = ResolveGenericArguments(genericRef, genericContext, dependentType);

                // cache lookup (all type dependencies already established above):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                result = genericType.MakeGenericType(typeArgs);
            }
            else if ((nestedRef = typeRef.AsNestedTypeReference) != null)
            {
                // cache lookup (no type dependencies to track):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                // a nested type builder would already be found in type builders, so we don't get here:
                Debug.Assert(!IsLocal(typeRef));

                // we only track dependency among type builders so we don't need to track it here:
                Type containingType = ResolveType(nestedRef.GetContainingType(_context), genericContext);

                result = containingType.GetNestedType(Cci.MetadataWriter.GetMangledName(nestedRef), BindingFlags.Public | BindingFlags.NonPublic);
            }
            else if ((arrayType = typeRef as Cci.IArrayTypeReference) != null)
            {
                // an array isn't a value type -> don't propagate dependency:
                Type elementType = ResolveType(arrayType.GetElementType(_context), genericContext, valueTypeDependency ? null : dependentType);

                // cache lookup (all type dependencies already established above):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                result = (arrayType.Rank > 1) ? elementType.MakeArrayType((int)arrayType.Rank) : elementType.MakeArrayType();
            }
            else if ((refType = typeRef as Cci.IManagedPointerTypeReference) != null)
            {
                // a managed pointer isn't a value type -> don't propagate dependency:
                Type elementType = ResolveType(refType.GetTargetType(_context), genericContext, valueTypeDependency ? null : dependentType);

                // cache lookup (all type dependencies already established above):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                result = elementType.MakeByRefType();
            }
            else if ((ptrType = typeRef as Cci.IPointerTypeReference) != null)
            {
                // a pointer isn't a value type -> don't propagate dependency:
                Type elementType = ResolveType(ptrType.GetTargetType(_context), genericContext, valueTypeDependency ? null : dependentType);

                // cache lookup (all type dependencies already established above):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                result = elementType.MakePointerType();
            }
            else if ((modType = typeRef as Cci.IModifiedTypeReference) != null)
            {
                Type[] reqMods, optMods;
                ResolveCustomModifiers(modType, dependentType, out reqMods, out optMods);
                Type unmodified = ResolveType(modType.UnmodifiedType, genericContext, dependentType, valueTypeDependency);

                // cache lookup (all type dependencies already established above):
                if (_typeRefs.TryGetValue(typeRef, out result))
                {
                    return result;
                }

                result = new ModifiedType(unmodified, reqMods, optMods);
            }
            else if ((genericParamRef = typeRef as Cci.IGenericParameterReference) != null)
            {
                GenericTypeParameterBuilder builder;
                if (_genericParameterBuilders.TryGetValue(genericParamRef, out builder))
                {
                    return builder;
                }

                Debug.Assert(!genericContext.IsNull);

                if (genericParamRef.AsGenericMethodParameterReference != null)
                {
                    // Note that all occurrences of M in the following snippet refer to the same
                    // IGenericMethodParameterReference object
                    // 
                    // void foo<M>() 
                    // {
                    //     C<M>.bar<T>(T, M);
                    // }
                    // 
                    // Though in the context of C<M>.bar<T>(T, M) method reference only T is bound to the generic parameter of method bar.
                    // We never get to resolve M here, because it can only be a GenericTypeParameterBuilder resolved earlier.
                    return genericContext.MethodParameters[genericParamRef.AsGenericMethodParameterReference.Index];
                }
                else
                {
                    return genericContext.TypeParameters[GetConsolidatedGenericTypeParameterIndex(genericParamRef.AsGenericTypeParameterReference)];
                }
            }
            else
            {
                throw ExceptionUtilities.Unreachable;
            }

            // do not cache if the lookup is relative to a generic context:
            if (genericContext.IsNull)
            {
                _typeRefs.Add(typeRef, result);
            }

            return result;
        }

        private static Type ResolveType(Assembly assembly, Cci.INamespaceTypeReference typeRef)
        {
            string mangledName = Cci.MetadataWriter.GetMangledName(typeRef);
            return assembly.GetType(MetadataHelpers.BuildQualifiedName(typeRef.NamespaceName, mangledName), true, false);
        }

        private static Type ResolveType(Module module, Cci.INamespaceTypeReference typeRef)
        {
            string mangledName = Cci.MetadataWriter.GetMangledName(typeRef);
            return module.GetType(MetadataHelpers.BuildQualifiedName(typeRef.NamespaceName, mangledName), true, false);
        }

        private Type[] ResolveGenericArguments(Cci.ITypeReference typeRef, GenericContext genericContext, TypeBuilder dependentType)
        {
            List<Cci.ITypeReference> argRefs = new List<Cci.ITypeReference>();
            GetConsolidatedGenericArguments(argRefs, typeRef);
            return argRefs.Select(arg => ResolveType(arg, genericContext, dependentType)).ToArray();
        }

        private void GetConsolidatedGenericArguments(List<Cci.ITypeReference> args, Cci.ITypeReference typeRef)
        {
            var nestedRef = typeRef.AsNestedTypeReference;
            if (nestedRef != null)
            {
                GetConsolidatedGenericArguments(args, nestedRef.GetContainingType(_context));
            }

            var genericRef = typeRef.AsGenericTypeInstanceReference;
            if (genericRef != null)
            {
                args.AddRange(genericRef.GetGenericArguments(_context));
            }
        }

        private void ResolveCustomModifiers(Cci.IModifiedTypeReference modType, TypeBuilder dependentType, out Type[] required, out Type[] optional)
        {
            List<Type> reqMods = null, optMods = null;
            foreach (var modifier in modType.CustomModifiers)
            {
                // TODO (tomat): can custom modifiers contain generic arguments, if so pass generic context thru?
                var type = ResolveType(modifier.GetModifier(_context), dependentType: dependentType);
                if (modifier.IsOptional)
                {
                    if (optMods == null)
                    {
                        optMods = new List<Type>();
                    }
                    optMods.Add(type);
                }
                else
                {
                    if (reqMods == null)
                    {
                        reqMods = new List<Type>();
                    }
                    reqMods.Add(type);
                }
            }
            required = (reqMods != null) ? reqMods.ToArray() : null;
            optional = (optMods != null) ? optMods.ToArray() : null;
        }

        // TODO: unify with CCI?
        private IEnumerable<Cci.IGenericTypeParameter> GetConsolidatedTypeParameters(Cci.INamedTypeDefinition typeDef)
        {
            Cci.INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(_context);
            if (nestedTypeDef == null)
            {
                if (typeDef.IsGeneric)
                {
                    return typeDef.GenericParameters;
                }

                return null;
            }

            return GetConsolidatedTypeParameters(typeDef, typeDef);
        }

        private List<Cci.IGenericTypeParameter> GetConsolidatedTypeParameters(Cci.ITypeDefinition typeDef, Cci.INamedTypeDefinition owner)
        {
            List<Cci.IGenericTypeParameter> result = null;
            Cci.INestedTypeDefinition nestedTypeDef = typeDef.AsNestedTypeDefinition(_context);
            if (nestedTypeDef != null)
            {
                result = GetConsolidatedTypeParameters(nestedTypeDef.ContainingTypeDefinition, owner);
            }

            if (typeDef.GenericParameterCount > 0)
            {
                ushort index = 0;
                if (result == null)
                {
                    result = new List<Cci.IGenericTypeParameter>();
                }
                else
                {
                    index = (ushort)result.Count;
                }

                if (typeDef == owner) // TODO: CCI had if (typeDef == owner && index == 0)
                {
                    result.AddRange(typeDef.GenericParameters);
                }
                else
                {
                    foreach (Cci.IGenericTypeParameter genericParameter in typeDef.GenericParameters)
                    {
                        result.Add(new Cci.InheritedTypeParameter(index++, owner, genericParameter));
                    }
                }
            }

            return result;
        }

        private int GetConsolidatedGenericTypeParameterIndex(Cci.IGenericTypeParameterReference genericParamRef)
        {
            int index = genericParamRef.Index;
            Cci.INamedTypeReference definingType = (Cci.INamedTypeReference)genericParamRef.DefiningType;

            while (true)
            {
                if (definingType != genericParamRef.DefiningType)
                {
                    index += definingType.GenericParameterCount;
                }

                var nestedTypeRef = definingType.AsNestedTypeReference;
                if (nestedTypeRef != null)
                {
                    definingType = (Cci.INamedTypeReference)nestedTypeRef.GetContainingType(_context);
                }
                else
                {
                    break;
                }
            }

            return index;
        }

        private bool IsLocal(Cci.ITypeReference typeRef)
        {
            var unit = Cci.MetadataWriter.GetDefiningUnitReference(typeRef, _context);
            Debug.Assert(unit != null);
            return ReferenceEquals(unit, _module);
        }

        private FieldInfo ResolveField(Cci.IFieldReference fieldRef)
        {
            var fieldDef = fieldRef.GetResolvedField(_context);
            if (fieldDef != null && IsLocal(fieldRef.GetContainingType(_context)))
            {
                return _fieldBuilders[fieldDef];
            }

            FieldInfo result;
            if (_fieldRefs.TryGetValue(fieldRef, out result))
            {
                return result;
            }

            Type declaringType = ResolveType(fieldRef.GetContainingType(_context));
            Cci.ISpecializedFieldReference specializedRef = fieldRef.AsSpecializedFieldReference;

            if (specializedRef != null)
            {
                if (IsLocal(specializedRef.UnspecializedVersion.GetContainingType(_context)))
                {
                    // declaring type is TypeBuilder or TypeBuilderInstantiation since it's defined in the module being built:
                    FieldBuilder fieldBuilder = _fieldBuilders[(Cci.IFieldDefinition)specializedRef.UnspecializedVersion.AsDefinition(_context)];
                    result = TypeBuilder.GetField(declaringType, fieldBuilder);
                }
                else
                {
                    FieldInfo unspecializedDefinition = ResolveField(specializedRef.UnspecializedVersion);
                    result = new FieldRef(declaringType, fieldRef.Name, unspecializedDefinition.FieldType);
                }
            }
            else
            {
                GenericContext genericContext;
                if (declaringType.IsGenericTypeDefinition)
                {
                    genericContext = new GenericContext(declaringType.GetGenericArguments(), Type.EmptyTypes);
                }
                else
                {
                    genericContext = default(GenericContext);
                }

                // TODO: modifiers?
                Type fieldType = ResolveType(fieldRef.GetType(_context), genericContext);
                result = new FieldRef(declaringType, fieldRef.Name, fieldType);
            }

            _fieldRefs.Add(fieldRef, result);
            return result;
        }

        private MethodBase ResolveMethodOrConstructor(Cci.IMethodReference methodRef)
        {
            if (methodRef.Name == ".ctor" || methodRef.Name == ".cctor")
            {
                return ResolveConstructor(methodRef);
            }
            else
            {
                return ResolveMethod(methodRef);
            }
        }

        private MethodInfo ResolveMethod(Cci.IMethodReference methodRef)
        {
            var methodDef = (Cci.IMethodDefinition)methodRef.AsDefinition(_context);

            // A method ref to a varargs method is always resolved as an entry in the method
            // ref table, never in the method def table, *even if the method is locally declared.*
            // (We could in theory resolve it as a method def if there were no extra arguments,
            // but in practice we do not.)

            if (methodDef != null && IsLocal(methodRef.GetContainingType(_context)) && !methodRef.AcceptsExtraArguments)
            {
                return _methodBuilders[methodDef];
            }

            MethodBase methodBase;
            if (_methodRefs.TryGetValue(methodRef, out methodBase))
            {
                return (MethodInfo)methodBase;
            }

            MethodInfo result;

            Cci.ISpecializedMethodReference specializedRef = methodRef.AsSpecializedMethodReference;
            Cci.IGenericMethodInstanceReference genericRef = methodRef.AsGenericMethodInstanceReference;

            if (specializedRef != null &&
                IsLocal(specializedRef.UnspecializedVersion.GetContainingType(_context)))
            {
                // get declaring type (TypeBuilder or TypeBuilderInstantiation since it's defined in the module being built):
                Type type = ResolveType(specializedRef.GetContainingType(_context));
                MethodBuilder methodBuilder = _methodBuilders[(Cci.IMethodDefinition)specializedRef.UnspecializedVersion.AsDefinition(_context)];
                MethodInfo methodOnTypeBuilder = TypeBuilder.GetMethod(type, methodBuilder);

                if (genericRef != null)
                {
                    Type[] typeArgs = genericRef.GetGenericArguments(_context).Select(arg => ResolveType(arg)).ToArray();
                    result = methodOnTypeBuilder.MakeGenericMethod(typeArgs);
                }
                else
                {
                    result = methodOnTypeBuilder;
                }
            }
            else if (genericRef != null)
            {
                MethodInfo genericMethod = ResolveMethod(genericRef.GetGenericMethod(_context));
                Type[] typeArgs = genericRef.GetGenericArguments(_context).Select((arg) => ResolveType(arg)).ToArray();

                if (genericMethod is MethodRef)
                {
                    result = new MethodSpec(genericMethod, typeArgs);
                }
                else
                {
                    result = genericMethod.MakeGenericMethod(typeArgs);
                }
            }
            else
            {
                result = MakeMethodRef(methodRef, specializedRef, isConstructor: false);
            }

            _methodRefs.Add(methodRef, result);
            return result;
        }

        private MethodBase ResolveConstructor(Cci.IMethodReference methodRef)
        {
            Debug.Assert(!methodRef.IsGeneric);

            // A method ref to a varargs method is always resolved as an entry in the method
            // ref table, never in the method def table, *even if the method is locally declared.*
            // (We could in theory resolve it as a method def if there were no extra arguments,
            // but in practice we do not.)

            var methodDef = (Cci.IMethodDefinition)methodRef.AsDefinition(_context);
            if (methodDef != null && IsLocal(methodRef.GetContainingType(_context)) && !methodRef.AcceptsExtraArguments)
            {
                return _constructorBuilders[methodDef];
            }

            MethodBase result;
            if (_methodRefs.TryGetValue(methodRef, out result))
            {
                return result;
            }

            Debug.Assert(methodRef.AsGenericMethodInstanceReference == null);

            Cci.ISpecializedMethodReference specializedRef = methodRef.AsSpecializedMethodReference;

            if (specializedRef != null &&
                IsLocal(specializedRef.UnspecializedVersion.GetContainingType(_context)))
            {
                // get declaring type (TypeBuilder or TypeBuilderInstantiation since it's defined in the module being built):
                Type declaringType = ResolveType(methodRef.GetContainingType(_context));
                ConstructorBuilder ctorBuilder = _constructorBuilders[(Cci.IMethodDefinition)specializedRef.UnspecializedVersion.AsDefinition(_context)];
                result = TypeBuilder.GetConstructor(declaringType, ctorBuilder);
            }
            else
            {
                result = MakeMethodRef(methodRef, specializedRef, isConstructor: true);
            }

            _methodRefs.Add(methodRef, result);
            return result;
        }

        private MethodRef MakeMethodRef(Cci.IMethodReference methodRef, Cci.ISpecializedMethodReference specializedRef, bool isConstructor)
        {
            Type declaringType = ResolveType(methodRef.GetContainingType(_context));

            MethodBase unspecializedDefinition = null;
            if (specializedRef != null)
            {
                // resolvemethod
                unspecializedDefinition = isConstructor ? ResolveConstructor(specializedRef.UnspecializedVersion) : ResolveMethod(specializedRef.UnspecializedVersion);
            }

            var m = new MethodRef(declaringType, methodRef.Name, GetManagedCallingConvention(methodRef.CallingConvention), unspecializedDefinition, GetExtraParameterTypes(methodRef));

            Type[] genericParameters;
            ParameterInfo[] parameters;
            ParameterInfo returnParameter;

            if (unspecializedDefinition != null)
            {
                if (isConstructor)
                {
                    returnParameter = new SimpleParameterInfo(unspecializedDefinition, 0, typeof(void));
                }
                else
                {
                    returnParameter = ((MethodInfo)unspecializedDefinition).ReturnParameter;
                }

                // TODO(tomat): change containing method?
                genericParameters = unspecializedDefinition.GetGenericArguments();
                parameters = unspecializedDefinition.GetParameters();
            }
            else
            {
                MakeMethodParameters(m, methodRef, out genericParameters, out parameters, out returnParameter);
            }

            m.InitializeParameters(genericParameters, parameters, returnParameter);

            return m;
        }

        private Type[] GetExtraParameterTypes(Cci.IMethodReference methodRef)
        {
            var extraParameters = methodRef.ExtraParameters;
            if (extraParameters.IsDefaultOrEmpty)
            {
                return null;
            }

            return extraParameters.Select(p =>
            {
                // modified types not supported by RefEmit, C#/VB don't support them either so it's ok:
                if (p.CustomModifiers.Any())
                {
                    throw new NotSupportedException();
                }

                var t = ResolveType(p.GetType(_context));
                return p.IsByReference ? t.MakeByRefType() : t;
            }).ToArray();
        }

        private void MakeMethodParameters(MethodBase method, Cci.IMethodReference methodRef, out Type[] methodGenericParameters, out ParameterInfo[] parameters, out ParameterInfo returnParameter)
        {
            Type declaringType = method.DeclaringType;

            if (methodRef.IsGeneric && methodRef.AsGenericMethodInstanceReference == null)
            {
                // generic definition
                methodGenericParameters = MakeGenericParameters((MethodInfo)method, methodRef);
            }
            else
            {
                methodGenericParameters = Type.EmptyTypes;
            }

            Type[] typeGenericParameters;
            if (declaringType.IsGenericTypeDefinition)
            {
                typeGenericParameters = declaringType.GetGenericArguments();
            }
            else
            {
                typeGenericParameters = Type.EmptyTypes;
            }

            GenericContext genericContext = methodGenericParameters.Length > 0 || typeGenericParameters.Length > 0 ?
                new GenericContext(typeGenericParameters, methodGenericParameters) : default(GenericContext);

            parameters = new ParameterInfo[methodRef.ParameterCount];
            int i = 0;
            foreach (var parameter in methodRef.GetParameters(_context))
            {
                parameters[i] = MakeParameterInfo(method, i + 1, parameter.GetType(_context), genericContext, parameter.CustomModifiers, parameter.IsByReference);
                i++;
            }

            returnParameter = MakeParameterInfo(method, 0, methodRef.GetType(_context), genericContext, methodRef.ReturnValueCustomModifiers, methodRef.ReturnValueIsByRef);
        }

        private ParameterInfo MakeParameterInfo(MethodBase containingMethod, int position, Cci.ITypeReference typeRef, GenericContext genericContext, ImmutableArray<Cci.ICustomModifier> customModifiers, bool isByReference)
        {
            var type = ResolveType(typeRef, genericContext);

            if (isByReference)
            {
                type = type.MakeByRefType();
            }

            if (customModifiers != null)
            {
                Type[] reqMods, optMods;
                ResolveCustomModifiers(customModifiers, out reqMods, out optMods);
                return new ModifiedParameterInfo(containingMethod, position, type, reqMods, optMods);
            }
            else
            {
                return new SimpleParameterInfo(containingMethod, position, type);
            }
        }

        private static Type[] MakeGenericParameters(MethodInfo containingMethod, Cci.IMethodReference methodRef)
        {
            Type[] result = new Type[methodRef.GenericParameterCount];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = new MethodGenericParameter(containingMethod, i);
            }

            return result;
        }

        public static MethodInfo ResolveEntryPoint(Assembly assembly, Cci.IMethodReference method, EmitContext context)
        {
            var containingType = method.GetContainingType(context);
            Debug.Assert(containingType is Cci.INamespaceTypeReference);
            var type = ResolveType(assembly, (Cci.INamespaceTypeReference)containingType);
            return type.GetMethod(method.Name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        }

        private MethodBase ResolveRuntimeMethodOrConstructor(Type declaringType, Cci.IMethodReference methodRef, bool isConstructor)
        {
            // GetMember does a pattern match if the last character is *
            string name = methodRef.Name;
            Debug.Assert(name[name.Length - 1] != '*');

            MemberTypes memberTypes = isConstructor ? MemberTypes.Constructor : MemberTypes.Method;
            BindingFlags bindingFlags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            Cci.ISpecializedMethodReference specializedRef = methodRef.AsSpecializedMethodReference;

            if (declaringType.IsGenericType && !declaringType.IsGenericTypeDefinition && declaringType.ContainsGenericParameters)
            {
                // TODO (tomat, Dev12):
                // Generic types instantiated with TypeBuilder(s) or GenericParameterBuilder(s) are represented by TypeBuilderInstantiation in Ref.Emit.
                // Unfortunately TypeBuilderInstantiation.GetMember throws NotSupportedException so we need to resolve the overload on generic definition
                // and then map the result back using TypeBuilder.GetMethod/GetConstructor APIs.
                //
                // Note that this API only works if the declaring type instantiation contains TypeBuilders as generic parameters.
                // So we can't handle all specializedRef != null cases here, only those such that declaringType.ContainsGenericParameters.
                Debug.Assert(specializedRef != null);

                Type genericDefinition = declaringType.GetGenericTypeDefinition();
                MemberInfo[] genericMembers = genericDefinition.GetMember(name, memberTypes, bindingFlags);

                if (genericMembers.Length == 1)
                {
                    // There is only one member of the methodRef name so it must be the one we are looking for.
                    return GetMethodOrConstructorOnTypeBuilderInstantiation(declaringType, genericMembers[0]);
                }

                // resolve on generic definitions and instantiate the result:
                MethodBase overload = ResolveOverload(genericMembers, specializedRef.UnspecializedVersion);
                return GetMethodOrConstructorOnTypeBuilderInstantiation(declaringType, overload);
            }
            else
            {
                MemberInfo[] members = declaringType.GetMember(name, memberTypes, bindingFlags);

                if (members.Length == 1)
                {
                    // There is only one member of the methodRef name so it must be the one we are looking for.
                    return (MethodBase)members[0];
                }

                if (specializedRef != null)
                {
                    // We need to distinguish between methods of a that have the same signature when their declaring type is instantiated.
                    // class C<T> 
                    // { 
                    //     void foo(T); 
                    //     void foo(int);
                    // } 
                    // 
                    // MethodInfos C<int>.foo(T == int) and C<int>.foo(int) are indistinguishable except for their token.
                    // Their tokens are the same as tokens of their respectie generic definitions. We can use this to figure out which is which.
                    // Resolve overload on generic definition and then match the specialized one by MethodDef token.
                    Type genericDefinition = declaringType.GetGenericTypeDefinition();
                    MethodBase unspecializedOverload = ResolveRuntimeMethodOrConstructor(genericDefinition, specializedRef.UnspecializedVersion, isConstructor);
                    return ResolveOverload(members, unspecializedOverload.MetadataToken);
                }
                else
                {
                    return ResolveOverload(members, methodRef);
                }
            }
        }

        private static MethodBase GetMethodOrConstructorOnTypeBuilderInstantiation(Type declaringType, MemberInfo member)
        {
            MethodInfo method = member as MethodInfo;
            if (method != null)
            {
                return TypeBuilder.GetMethod(declaringType, method);
            }
            else
            {
                return TypeBuilder.GetConstructor(declaringType, (ConstructorInfo)member);
            }
        }

        private static MethodBase ResolveOverload(MemberInfo[] members, int methodDef)
        {
            foreach (MethodBase method in members)
            {
                if (method.MetadataToken == methodDef)
                {
                    return method;
                }
            }

            throw ExceptionUtilities.Unreachable;
        }

        private MethodBase ResolveOverload(MemberInfo[] members, Cci.IMethodReference methodRef)
        {
            IEnumerable<Cci.IParameterTypeInformation> paramRefs = methodRef.GetParameters(_context);

            Type[] reusableResolvedParameters = null;
            Type[] typeGenericParameters = members[0].DeclaringType.GetGenericArguments();

            MethodBase candidate = null;
            foreach (MethodBase method in members)
            {
                Debug.Assert(!method.IsGenericMethod || method.IsGenericMethodDefinition);

                if (methodRef.AcceptsExtraArguments && method.CallingConvention != CallingConventions.VarArgs)
                {
                    continue;
                }

                if (methodRef.IsGeneric != method.IsGenericMethodDefinition)
                {
                    continue;
                }

                Type[] methodGenericParameters = (methodRef.IsGeneric) ? method.GetGenericArguments() : Type.EmptyTypes;
                if (methodGenericParameters.Length != methodRef.GenericParameterCount)
                {
                    continue;
                }
                GenericContext genericContext = new GenericContext(typeGenericParameters, methodGenericParameters);
                Type[] resolvedParameters = reusableResolvedParameters ??
                    ResolveMethodParameters(methodRef, paramRefs, genericContext);

                if (ParametersMatch(resolvedParameters, method.GetParameters()) && ReturnTypeMatches(methodRef, method, genericContext))
                {
                    Debug.Assert(candidate == null);
                    candidate = method;
#if !DEBUG
                    break;
#endif
                }

                // We can reuse resolved parameters if the method isn't generic. 
                // If it is its signature might contain references to its generic parameters which are different for each overload.
                if (reusableResolvedParameters == null && !methodRef.IsGeneric)
                {
                    reusableResolvedParameters = resolvedParameters;
                }
            }

            Debug.Assert(candidate != null);
            return candidate;
        }

        private bool ReturnTypeMatches(Cci.IMethodReference methodRef, MethodBase method, GenericContext genericContext)
        {
            MethodInfo methodInfo = method as MethodInfo;
            if (methodInfo == null)
            {
                return true;
            }

            return methodInfo.ReturnType.IsEquivalentTo(ResolveType(methodRef.GetType(_context), genericContext));
        }

        private static bool ParametersMatch(Type[] types, ParameterInfo[] paramInfos)
        {
            if (paramInfos.Length != types.Length)
            {
                return false;
            }

            for (int i = 0; i < paramInfos.Length; i++)
            {
                if (!ParameterTypeMatches(types[i], paramInfos[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool ParameterTypeMatches(Type type, ParameterInfo param)
        {
            if (!type.IsEquivalentTo(param.ParameterType))
            {
                return false;
            }

            // TODO (tomat): how expensive is GetOptional/GetRequiredCustomModifiers?
            ModifiedType modified = type as ModifiedType;
            return IsEquivalentTo(modified != null ? modified.OptionalModifiers : null, param.GetOptionalCustomModifiers())
                && IsEquivalentTo(modified != null ? modified.RequiredModifiers : null, param.GetRequiredCustomModifiers());
        }

        private static bool IsEquivalentTo(Type[] actual, Type[] expected)
        {
            if (actual == null)
            {
                actual = Type.EmptyTypes;
            }

            if (expected == null)
            {
                expected = Type.EmptyTypes;
            }

            if (actual.Length != expected.Length)
            {
                return false;
            }

            for (int i = 0; i < actual.Length; i++)
            {
                if (!actual[i].IsEquivalentTo(expected[i]))
                {
                    return false;
                }
            }

            return true;
        }

        private Type[] ResolveMethodParameters(Cci.IMethodReference methodRef, IEnumerable<Cci.IParameterTypeInformation> paramRefs,
            GenericContext genericContext)
        {
            var paramTypes = new Type[methodRef.ParameterCount];
            int i = 0;
            foreach (var paramRef in paramRefs)
            {
                paramTypes[i++] = ResolveParameterType(paramRef, genericContext);
            }

            return paramTypes;
        }

        #endregion

        #region Emit

        public static MethodInfo Emit(
            EmitContext context,
            IEnumerable<KeyValuePair<Cci.IAssemblyReference, string>> referencedAssemblies,
            ModuleBuilder builder,
            AssemblyLoader assemblyLoader,
            Cci.IMethodReference entryPoint,
            CancellationToken cancellationToken)
        {
            var emitter = new ReflectionEmitter(context, referencedAssemblies, builder, assemblyLoader);
            return emitter.EmitWorker(entryPoint, cancellationToken);
        }

        /// <summary>
        /// The main worker. Emits all types.
        /// </summary>
        /// <param name="entryPoint">An entry point to resolve and return. This could be an arbitrary method, not just PE entry point.</param>
        /// <param name="cancellationToken">Token used to cancel the operation.</param>
        /// <returns>The entry point or null if there is none.</returns>
        /// <exception cref="NotSupportedException">Reflection.Emit doesn't support the feature being emitted.</exception>
        private MethodInfo EmitWorker(Cci.IMethodReference entryPoint, CancellationToken cancellationToken)
        {
            // types and nesting:
            foreach (var typeDef in _module.GetTopLevelTypes(_context))
            {
                // global type (TODO: detect properly)
                if (typeDef.Name == "<Module>")
                {
                    continue;
                }

                DefineTypeRecursive(typeDef, containingTypeBuilder: null);
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Base types: we need to define them before defining fields.
            // Field definition creates a signature, which might ask whether a type builder is a value type.
            // The implementation of Type.IsValueType calls IsSubclassOf(System.ValueType).
            foreach (var definedType in _typeBuilders)
            {
                var typeDef = definedType.Key;
                var typeBuilder = definedType.Value;

                var baseClass = typeDef.GetBaseClass(_context);
                if (baseClass != null)
                {
                    // base type must be loaded before the type:
                    typeBuilder.SetParent(ResolveType(baseClass, dependentType: typeBuilder, valueTypeDependency: false));
                }
            }

            foreach (var genericParameter in _genericParameterBuilders)
            {
                var gpBuilder = genericParameter.Value;
                var typeParameter = (Cci.IGenericTypeParameter)genericParameter.Key;

                if (typeParameter.AsGenericTypeParameter != null)
                {
                    DefineGenericParameterConstraints(gpBuilder, typeParameter);
                }
            }

            // inheritance, generic type parameters, fields, methods, constructors (need types, base types):
            foreach (var definedType in _typeBuilders)
            {
                DefineFieldsAndMethods(definedType.Value, definedType.Key);
            }

            // TODO: define methods and fields of global type

            // parameter names and custom attributes (needs all members and types):
            foreach (var method in _methodBuilders)
            {
                var methodDef = method.Key;
                var methodBuilder = method.Value;

                DefineParameters(methodBuilder, null, methodDef);
                EmitCustomAttributes(methodBuilder, methodDef.GetAttributes(_context));
            }

            foreach (var ctor in _constructorBuilders)
            {
                var ctorDef = ctor.Key;
                var ctorBuilder = ctor.Value;

                DefineParameters(null, ctorBuilder, ctorDef);
                EmitCustomAttributes(ctorBuilder, ctorDef.GetAttributes(_context));
            }

            foreach (var field in _fieldBuilders)
            {
                var fieldDef = field.Key;
                var fieldBuilder = field.Value;

                // Constant: Assign field literals after all fields have been defined. 
                // TypeBuilder that represents an enum determines its underlying type based upon the first (and only) instance field type (__value).
                // So we need to define all the fields first and then assign their literal values.
                var constant = fieldDef.GetCompileTimeValue(_context);
                if (constant != null)
                {
                    // TODO(tomat): This fails for enum fields if the enum is a nested type in a generic type (TypeBuilderInstantiation throws in IsSubclassOf)
                    fieldBuilder.SetConstant(constant.Value);
                }

                EmitCustomAttributes(fieldBuilder, fieldDef.GetAttributes(_context));
            }

            // define generic parameter custom attributes (needs all types, methods and constructors defined):
            foreach (var genericParameter in _genericParameterBuilders)
            {
                EmitCustomAttributes(genericParameter.Value, ((Cci.IGenericParameter)genericParameter.Key).GetAttributes(_context));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // bodies (IL may need a method token of a generic method, which bakes its generic parameters -> we need to emit generic parameters first)
            foreach (var method in _methodBuilders)
            {
                if (Cci.Extensions.HasBody(method.Key))
                {
                    EmitMethodBody(method.Value, null, method.Key.GetBody(_context));
                }
            }

            foreach (var ctor in _constructorBuilders)
            {
                if (Cci.Extensions.HasBody(ctor.Key))
                {
                    EmitMethodBody(null, ctor.Value, ctor.Key.GetBody(_context));
                }
            }

            // base type, implemented interfaces, generic parameters
            // member relationships, properties, events, custom attributes (needs attribute constructors, all members):
            foreach (var definedType in _typeBuilders)
            {
                FinishType(definedType.Value, definedType.Key);
            }

            // module and assembly attributes:
            EmitCustomAttributes(_builder, _module.ModuleAttributes);
            EmitCustomAttributes((AssemblyBuilder)_builder.Assembly, _module.AssemblyAttributes);

            cancellationToken.ThrowIfCancellationRequested();

            // order types to satisfy dependencies:
            IEnumerable<TypeBuilder> orderedTypeBuilders = OrderTypeBuilders();
            if (orderedTypeBuilders == null)
            {
                throw new NotSupportedException("Ref.Emit limitation: cycle in type dependencies");
            }

            // create types and entry point:
            MethodInfo resolvedEntryPoint = null;
            Type entryPointType = null;
            if (entryPoint != null)
            {
                entryPointType = ResolveType(entryPoint.GetContainingType(_context));
            }

            foreach (var typeBuilder in orderedTypeBuilders)
            {
                Type type = null;

                // TODO (tomat): we should test for features not allowed in a collectible assembly upfront
                // rather than waiting for an exception to happen.
                try
                {
                    type = typeBuilder.CreateType();

                    // TODO (tomat): this is very strange, the TypeLoadException is swallowed when VS debugger is attached
                    // and the execution continues with type == null. See bug DevDiv2\DevDiv bug 391550.
                    if (type == null)
                    {
                        throw new NotSupportedException("Ref.Emit limitation");
                    }
                }
                catch (TypeLoadException e)
                {
                    throw new NotSupportedException("Ref.Emit limitation: " + e.Message);
                }

                if (typeBuilder == entryPointType)
                {
                    resolvedEntryPoint = (MethodInfo)ResolveRuntimeMethodOrConstructor(type, entryPoint, isConstructor: false);
                }

                cancellationToken.ThrowIfCancellationRequested();
            }

            // PE entry point:
            if (_module.EntryPoint != null)
            {
                Debug.Assert(_module.EntryPoint == entryPoint);
                ((AssemblyBuilder)_builder.Assembly).SetEntryPoint(resolvedEntryPoint);
            }

            Debug.Assert(entryPoint == null || resolvedEntryPoint != null);
            return resolvedEntryPoint;
        }

        private void FinishType(TypeBuilder typeBuilder, Cci.ITypeDefinition typeDef)
        {
            // implemented interfaces
            foreach (var iface in typeDef.Interfaces(_context))
            {
                // an implemented interface must be loaded before the type that implements it:
                typeBuilder.AddInterfaceImplementation(ResolveType(iface, dependentType: typeBuilder, valueTypeDependency: false));
            }

            // method implementations
            foreach (Cci.MethodImplementation impl in typeDef.GetExplicitImplementationOverrides(_context))
            {
                typeBuilder.DefineMethodOverride(ResolveMethod(impl.ImplementingMethod), ResolveMethod(impl.ImplementedMethod));
            }

            // properties (don't need to be defined prior custom attributes - we don't use CustomAttributeBuilders):
            foreach (Cci.IPropertyDefinition propertyDef in typeDef.GetProperties(_context))
            {
                EmitCustomAttributes(DefineProperty(typeBuilder, propertyDef), propertyDef.GetAttributes(_context));
            }

            // events
            foreach (Cci.IEventDefinition eventDef in typeDef.Events)
            {
                EmitCustomAttributes(DefineEvent(typeBuilder, eventDef), eventDef.GetAttributes(_context));
            }

            // custom attributes
            EmitCustomAttributes(typeBuilder, typeDef.GetAttributes(_context));

            // TODO:
            // decl security
        }

        /// <summary>
        /// Bakes types in the order implied by <see cref="_dependencyGraph"/>. A type can't be baked until all of its dependencies are.
        /// </summary>
        private IEnumerable<TypeBuilder> OrderTypeBuilders()
        {
            if (_dependencyGraph.Count == 0)
            {
                // there are no dependencies, so any order works fine:
                return _typeBuilders.Values;
            }

            // Implements DFS based topological sort.

            // Set of nodes of dependencyGraph that have at least one incoming edge. We build this to find the set of DFS roots.
            var withIncomingEdge = new HashSet<TypeBuilder>();
            TypeBuilder[] ordered = new TypeBuilder[_typeBuilders.Count];
            var state = new Dictionary<TypeBuilder, DfsState>();
            int ordinal = 0;

            foreach (var type in _typeBuilders.Values)
            {
                List<TypeBuilder> typeDependencies;
                if (!_dependencyGraph.TryGetValue(type, out typeDependencies))
                {
                    // type has no dependencies, so we can emit it right away:
                    ordered[ordinal++] = type;
                    state[type] = DfsState.Finished;
                }
                else
                {
                    foreach (var typeDependency in typeDependencies)
                    {
                        withIncomingEdge.Add(typeDependency);
                    }
                }
            }

            var workitems = new List<TypeBuilder>();

            // add roots:
            foreach (var edge in _dependencyGraph)
            {
                if (!withIncomingEdge.Contains(edge.Key))
                {
                    workitems.Add(edge.Key);
                }
            }

            if (workitems.Count == 0)
            {
                // a cycle found:
                return null;
            }

            const TypeBuilder NodeFinishedMarker = null;

            while (workitems.Count > 0)
            {
                TypeBuilder workitem = Pop(workitems);

                // marker -> this means we should pop the next type and bake it:
                if (workitem == NodeFinishedMarker)
                {
                    workitem = Pop(workitems);
                    Debug.Assert(state[workitem] == DfsState.Entered);
                    state[workitem] = DfsState.Finished;

                    ordered[ordinal++] = workitem;
                    continue;
                }

                // already visited?
                DfsState s;
                if (state.TryGetValue(workitem, out s))
                {
                    // cycle:
                    if (s == DfsState.Entered)
                    {
                        return null;
                    }

                    // side-edge:
                    Debug.Assert(s == DfsState.Finished);
                    continue;
                }

                // visit children:
                state[workitem] = DfsState.Entered;
                workitems.Add(workitem);
                workitems.Add(NodeFinishedMarker);
                workitems.AddRange(_dependencyGraph[workitem]);
            }

            Debug.Assert(ordinal <= ordered.Length);

            // If all type builders visited we have an ordering, otherise there were some cycles which 
            // prevented us to add roots in the workitems thus not visiting all types:
            return (ordinal == ordered.Length) ? ordered : null;
        }

        private enum DfsState
        {
            Entered = 1,
            Finished = 2
        }

        private static TypeBuilder Pop(List<TypeBuilder> list)
        {
            var result = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return result;
        }

        #endregion

        #region Definitions

        private void DefineTypeRecursive(Cci.INamedTypeDefinition typeDef, TypeBuilder containingTypeBuilder)
        {
            var builder = DefineType(typeDef, containingTypeBuilder);
            foreach (var nestedType in typeDef.GetNestedTypes(_context))
            {
                // TODO (tomat, Dev12): Do not emit types of mapped fields as Ref.Emit defines its own.
                // We should remove this when Ref.Emit is fixed.
                if (nestedType.Name.StartsWith("__StaticArrayInitTypeSize=", StringComparison.Ordinal))
                {
                    continue;
                }

                DefineTypeRecursive(nestedType, containingTypeBuilder: builder);
            }
        }

        private TypeBuilder DefineType(Cci.INamedTypeDefinition typeDef, TypeBuilder containingTypeBuilder)
        {
            Debug.Assert(!_typeBuilders.ContainsKey(typeDef));
            TypeBuilder typeBuilder;

            string mangledName = Cci.MetadataWriter.GetMangledName(typeDef);
            TypeAttributes attrs = (TypeAttributes)Cci.MetadataWriter.GetTypeDefFlags(typeDef, _context);

            if (containingTypeBuilder != null)
            {
                typeBuilder = containingTypeBuilder.DefineNestedType(mangledName, attrs, null, (PackingSize)typeDef.Alignment, (int)typeDef.SizeOf);
            }
            else
            {
                var namespaceType = (Cci.INamespaceTypeDefinition)typeDef;

                typeBuilder = _builder.DefineType(
                    MetadataHelpers.BuildQualifiedName(namespaceType.NamespaceName, mangledName),
                    attrs,
                    null, // parent set later
                    (PackingSize)typeDef.Alignment,
                    (int)typeDef.SizeOf
                );
            }

            // generic parameters:
            // We need to define generic parameters so that type references that target them can be resolved later on:
            var typeParameters = GetConsolidatedTypeParameters(typeDef);
            if (typeParameters != null)
            {
                DefineGenericParameters(typeBuilder, null, typeParameters);
            }

            _typeBuilders.Add(typeDef, typeBuilder);
            return typeBuilder;
        }

        private void DefineFieldsAndMethods(TypeBuilder typeBuilder, Cci.ITypeDefinition typeDef)
        {
            // Ref.Emit quirk: 
            // We need to define methods (at least generic methods with type parameter constrains) before constructors.
            // Unfortunately ConstructorBuilder gets a token for the underlying method builder,
            // which might trigger baking of generic parameters of other method builders! 

            // methods
            foreach (Cci.IMethodDefinition methodDef in typeDef.GetMethods(_context))
            {
                if (!methodDef.IsConstructor)
                {
                    DefineMethod(typeBuilder, methodDef);
                }
            }

            // constructors
            foreach (Cci.IMethodDefinition methodDef in typeDef.GetMethods(_context))
            {
                if (methodDef.IsConstructor)
                {
                    DefineConstructor(typeBuilder, methodDef);
                }
            }

            // fields
            foreach (Cci.IFieldDefinition fieldDef in typeDef.GetFields(_context))
            {
                Debug.Assert(fieldDef.IsStatic || !typeDef.IsEnum || fieldDef.Name == WellKnownMemberNames.EnumBackingFieldName);
                DefineField(typeBuilder, fieldDef);
            }
        }

        private void DefineField(TypeBuilder typeBuilder, Cci.IFieldDefinition fieldDef)
        {
            FieldAttributes attrs = (FieldAttributes)Cci.MetadataWriter.GetFieldFlags(fieldDef);
            FieldBuilder fieldBuilder;

            if (!fieldDef.MappedData.IsDefault)
            {
                // TODO (tomat, Dev12): Unfortunately, Reflection.Emit doesn't allow us to directly set FieldRVA on arbitrary FieldBuilder.
                // Instead it creates the mapping types itself and provides APIs to define mapped fields.
                // So we use that API and ignore the mapping types provided by the compiler.

                // FieldRVA
                fieldBuilder = typeBuilder.DefineInitializedData(fieldDef.Name, fieldDef.MappedData.ToArray(), attrs);
            }
            else
            {
                // Field types that are value types need to be loaded before the declaring type.
                // If the field type is a nested type, e.g. A.B.C only the inner-most type needs to be loaded before the declaring type.
                Type type = ResolveType(
                    fieldDef.GetType(_context),
                    genericContext: default(GenericContext),
                    dependentType: typeBuilder,
                    valueTypeDependency: true);

                // TODO (tomat, Dev12): this doesn't handle types constructed from modified types, we need Ref.Emit support for that:
                Type[] reqMods = null, optMods = null;
                ModifiedType modified = type as ModifiedType;
                if (modified != null)
                {
                    reqMods = modified.RequiredModifiers;
                    optMods = modified.OptionalModifiers;
                    type = modified.UnmodifiedType;
                }

                fieldBuilder = typeBuilder.DefineField(fieldDef.Name, type, reqMods, optMods, attrs);
            }

            // FieldLayout
            if (fieldDef.ContainingTypeDefinition.Layout == LayoutKind.Explicit && !fieldDef.IsStatic)
            {
                fieldBuilder.SetOffset((int)fieldDef.Offset);
            }

            // FieldMarshal
            if (fieldDef.IsMarshalledExplicitly)
            {
                var marshallingInformation = fieldDef.MarshallingInformation;

                if (marshallingInformation != null)
                {
                    fieldBuilder.SetCustomAttribute(GetMarshalAsAttribute(marshallingInformation));
                }
                else
                {
                    Debug.Assert(!fieldDef.MarshallingDescriptor.IsDefaultOrEmpty);
                    // TODO:
                    throw new NotImplementedException();
                }
            }

            _fieldBuilders.Add(fieldDef, fieldBuilder);
        }

        private MethodImplAttributes GetMethodImplAttributes(Cci.IMethodDefinition methodDef)
        {
            // TODO (tomat): bug in Ref.Emit - ForwardRef can't be emitted
            MethodImplAttributes implAttrs = methodDef.GetImplementationAttributes(_context) & ~MethodImplAttributes.ForwardRef;

            // TODO (tomat): bug in Ref.Emit - it seems that a body RVA of the last method
            // is reused if the method is InternalCall but not Runtime (see TestMethodImplAttribute_VerifiableMD),
            if ((implAttrs & MethodImplAttributes.InternalCall) != 0)
            {
                implAttrs |= MethodImplAttributes.Runtime;
            }

            return implAttrs;
        }

        private void DefineConstructor(TypeBuilder typeBuilder, Cci.IMethodDefinition methodDef)
        {
            Debug.Assert(methodDef.IsConstructor);
            Debug.Assert(!methodDef.IsGeneric);

            Type[] paramTypes;
            Type[][] paramReqMods, paramOptMods;
            GetParameterTypes(methodDef, out paramTypes, out paramReqMods, out paramOptMods);

            var ctorBuilder = typeBuilder.DefineConstructor(
            (MethodAttributes)Cci.MetadataWriter.GetMethodFlags(methodDef),
                GetManagedCallingConvention(methodDef.CallingConvention),
                paramTypes,
                paramReqMods,
                paramOptMods
            );

            ctorBuilder.SetImplementationFlags(GetMethodImplAttributes(methodDef));
            _constructorBuilders.Add(methodDef, ctorBuilder);
        }

        private void DefineMethod(TypeBuilder typeBuilder, Cci.IMethodDefinition methodDef)
        {
            Debug.Assert(!methodDef.IsConstructor);

            // First, define a builder without signature:
            MethodBuilder methodBuilder = typeBuilder.DefineMethod(
                methodDef.Name,
            (MethodAttributes)Cci.MetadataWriter.GetMethodFlags(methodDef),
            GetManagedCallingConvention(methodDef.CallingConvention)
            );

            // define generic parameters:
            if (methodDef.GenericParameterCount > 0)
            {
                var gpBuilders = DefineGenericParameters(null, methodBuilder, methodDef.GenericParameters);

                // We need to define constraints before any constructor is defined.
                // Unfortunately ConstructorBuilder gets a token for the underlying method builder,
                // which might trigger baking of generic parameters of other method builders! 
                int i = 0;
                foreach (var typeParameter in methodDef.GenericParameters)
                {
                    DefineGenericParameterConstraints(gpBuilders[i++], typeParameter);
                }
            }

            Type[] paramTypes;
            Type[][] paramReqMods, paramOptMods;
            Type returnType;
            Type[] returnTypeReqMods, returnTypeOptMods;

            // resolve signature types (they can resolve to generic parameters):
            GetParameterTypes(methodDef, out paramTypes, out paramReqMods, out paramOptMods);
            GetReturnType(methodDef, out returnType, out returnTypeReqMods, out returnTypeOptMods);

            // set the signature:
            methodBuilder.SetSignature(
                returnType,
                returnTypeReqMods,
                returnTypeOptMods,
                paramTypes,
                paramReqMods,
                paramOptMods
            );

            if (methodDef.IsPlatformInvoke)
            {
                var data = methodDef.PlatformInvokeData;
                SetPInvokeAttributes(methodBuilder, data.ModuleName, data.EntryPointName ?? methodDef.Name, data.Flags);
            }

            // SetPInvokeAttributes seems to clear impl flags, so they have to be set after P/Invoke attributes are set:
            methodBuilder.SetImplementationFlags(GetMethodImplAttributes(methodDef));

            _methodBuilders.Add(methodDef, methodBuilder);
        }

        private PropertyBuilder DefineProperty(TypeBuilder typeBuilder, Cci.IPropertyDefinition propertyDef)
        {
            Type[] paramTypes;
            Type[][] paramReqMods, paramOptMods;
            GetParameterTypes(propertyDef, out paramTypes, out paramReqMods, out paramOptMods);

            Type returnType;
            Type[] returnTypeReqMods, returnTypeOptMods;
            GetReturnType(propertyDef, out returnType, out returnTypeReqMods, out returnTypeOptMods);

            CallingConventions callingConvention = GetManagedCallingConvention(propertyDef.CallingConvention);
            PropertyAttributes attrs = (PropertyAttributes)Cci.MetadataWriter.GetPropertyFlags(propertyDef);

            // Property, PropertyMap
            var propertyBuilder = typeBuilder.DefineProperty(
                propertyDef.Name,
                attrs,
                callingConvention,
                returnType,
                returnTypeReqMods,
                returnTypeOptMods,
                paramTypes,
                paramReqMods,
                paramOptMods
            );

            // MethodSemantics
            foreach (var accessor in propertyDef.Accessors)
            {
                var accessorDef = (Cci.IMethodDefinition)accessor.AsDefinition(_context);
                var accessorBuilder = _methodBuilders[accessorDef];

                if (accessor == propertyDef.Getter)
                {
                    propertyBuilder.SetGetMethod(accessorBuilder);
                }
                else if (accessor == propertyDef.Setter)
                {
                    propertyBuilder.SetSetMethod(accessorBuilder);
                }
                else
                {
                    propertyBuilder.AddOtherMethod(accessorBuilder);
                }
            }

            // Constant
            if (propertyDef.HasDefaultValue)
            {
                propertyBuilder.SetConstant(propertyDef.DefaultValue.Value);
            }

            return propertyBuilder;
        }

        private EventBuilder DefineEvent(TypeBuilder typeBuilder, Cci.IEventDefinition eventDef)
        {
            EventAttributes attrs = (EventAttributes)Cci.MetadataWriter.GetEventFlags(eventDef);
            Type type = ResolveType(eventDef.GetType(_context));

            // Event, EventMap
            var eventBuilder = typeBuilder.DefineEvent(eventDef.Name, attrs, type);

            // MethodSemantics
            foreach (var accessor in eventDef.Accessors)
            {
                var accessorDef = (Cci.IMethodDefinition)accessor.AsDefinition(_context);
                var accessorBuilder = _methodBuilders[accessorDef];

                if (accessor == eventDef.Adder)
                {
                    eventBuilder.SetAddOnMethod(accessorBuilder);
                }
                else if (accessor == eventDef.Remover)
                {
                    eventBuilder.SetRemoveOnMethod(accessorBuilder);
                }
                else if (accessor == eventDef.Caller)
                {
                    eventBuilder.SetRaiseMethod(accessorBuilder);
                }
                else
                {
                    eventBuilder.AddOtherMethod(accessorBuilder);
                }
            }

            return eventBuilder;
        }

        private void GetReturnType(Cci.ISignature signature, out Type returnType, out Type[] reqMods, out Type[] optMods)
        {
            returnType = ResolveType(signature.GetType(_context));
            if (signature.ReturnValueIsByRef)
            {
                returnType = returnType.MakeByRefType();
            }

            // TODO (tomat, Dev12): this doesn't handle types constructed from modified types, we need Ref.Emit supporte for that:
            if (signature.ReturnValueCustomModifiers.Any())
            {
                ResolveCustomModifiers(signature.ReturnValueCustomModifiers, out reqMods, out optMods);
            }
            else
            {
                reqMods = optMods = null;
            }
        }

        private Type ResolveParameterType(Cci.IParameterTypeInformation parameter, GenericContext genericContext = default(GenericContext))
        {
            var parameterType = ResolveType(parameter.GetType(_context), genericContext);

            if (parameter.IsByReference)
            {
                parameterType = parameterType.MakeByRefType();
            }

            if (parameter.CustomModifiers.Any())
            {
                Type[] reqMods, optMods;
                ResolveCustomModifiers(parameter.CustomModifiers, out reqMods, out optMods);
                return new ModifiedType(parameterType, reqMods, optMods);
            }
            else
            {
                return parameterType;
            }
        }

        private void ResolveCustomModifiers(ImmutableArray<Cci.ICustomModifier> modifers, out Type[] reqMods, out Type[] optMods)
        {
            List<Type> reqModList = null, optModList = null;
            foreach (Cci.ICustomModifier customModifier in modifers)
            {
                Type modifier = ResolveType(customModifier.GetModifier(_context));

                if (customModifier.IsOptional)
                {
                    if (optModList == null)
                    {
                        optModList = new List<Type>();
                    }

                    optModList.Add(modifier);
                }
                else
                {
                    if (reqModList == null)
                    {
                        reqModList = new List<Type>();
                    }

                    reqModList.Add(modifier);
                }
            }

            reqMods = (reqModList != null) ? reqModList.ToArray() : null;
            optMods = (optModList != null) ? optModList.ToArray() : null;
        }

        private void GetParameterTypes(Cci.ISignature signature, out Type[] types, out Type[][] reqMods, out Type[][] optMods)
        {
            // name, default value, marshalling and custom attributes are handled later in DefineParameter
            types = new Type[signature.ParameterCount];
            reqMods = optMods = null;

            int i = 0;
            foreach (var parameter in signature.GetParameters(_context))
            {
                Type type = ResolveParameterType(parameter);

                // TODO (tomat, Dev12): this doesn't handle types constructed from modified types, we need Ref.Emit support for that:
                ModifiedType modifiedType = type as ModifiedType;
                if (modifiedType != null)
                {
                    type = modifiedType.UnmodifiedType;

                    if (modifiedType.RequiredModifiers != null)
                    {
                        if (reqMods == null)
                        {
                            reqMods = new Type[types.Length][];
                        }

                        reqMods[i] = modifiedType.RequiredModifiers;
                    }

                    if (modifiedType.OptionalModifiers != null)
                    {
                        if (optMods == null)
                        {
                            optMods = new Type[types.Length][];
                        }

                        optMods[i] = modifiedType.OptionalModifiers;
                    }
                }

                types[i++] = type;
            }
        }

        private GenericTypeParameterBuilder[] DefineGenericParameters(TypeBuilder typeBuilder, MethodBuilder methodBuilder, IEnumerable<Cci.IGenericParameter> typeParameters)
        {
            Debug.Assert(typeBuilder != null ^ methodBuilder != null);

            var names = new List<string>(typeParameters.Select(typeParameter => typeParameter.Name)).ToArray();
            var gpBuilders = (typeBuilder != null) ? typeBuilder.DefineGenericParameters(names) : methodBuilder.DefineGenericParameters(names);

            int i = 0;
            foreach (var typeParameter in typeParameters)
            {
                _genericParameterBuilders.Add(typeParameter, gpBuilders[i++]);
            }

            return gpBuilders;
        }

        private void DefineGenericParameterConstraints(GenericTypeParameterBuilder gpBuilder, Cci.IGenericParameter typeParameter)
        {
            List<Type> typeConstraints = new List<Type>();
            foreach (var constraint in typeParameter.GetConstraints(_context))
            {
                // generic constraints must be loaded before the declaring type:
                var typeConstraint = ResolveType(constraint, dependentType: (TypeBuilder)gpBuilder.DeclaringType, valueTypeDependency: false);
                typeConstraints.Add(typeConstraint);
            }

            // The types actually don't need to be interfaces. Ref.Emit merges them eventually with base type constraint into a single list.
            // Besides there might be multiple non-interface constraints applied on the parameter if they are another type parameters.
            gpBuilder.SetInterfaceConstraints(typeConstraints.ToArray());

            gpBuilder.SetGenericParameterAttributes(GetGenericParameterAttributes(typeParameter));
        }

        private void DefineParameters(MethodBuilder methodBuilder, ConstructorBuilder constructorBuilder, Cci.IMethodDefinition methodDef)
        {
            Debug.Assert(methodBuilder != null ^ constructorBuilder != null);

            // return value
            if (methodDef.ReturnValueIsMarshalledExplicitly || methodDef.ReturnValueAttributes.Any())
            {
                DefineParameter(methodBuilder, constructorBuilder, new Cci.ReturnValueParameter(methodDef));
            }

            // parameters
            foreach (Cci.IParameterDefinition paramDef in methodDef.Parameters)
            {
                DefineParameter(methodBuilder, constructorBuilder, paramDef);
            }
        }

        private void DefineParameter(MethodBuilder methodBuilder, ConstructorBuilder constructorBuilder, Cci.IParameterDefinition paramDef)
        {
            // No explicit param row is needed if param has no flags (other than optionally IN),
            // no name and no references to the param row, such as CustomAttribute, Constant, or FieldMarshall
            var attributes = paramDef.GetAttributes(_context);
            var defaultValue = paramDef.GetDefaultValue(_context);

            if (defaultValue != null ||
                paramDef.IsOptional ||
                paramDef.IsOut ||
                paramDef.IsMarshalledExplicitly ||
                attributes.Any() ||
                paramDef.Name.Length > 0)
            {
                int index = paramDef is Cci.ReturnValueParameter ? 0 : paramDef.Index + 1;
                ParameterAttributes attrs = (ParameterAttributes)Cci.MetadataWriter.GetParameterFlags(paramDef);

                ParameterBuilder paramBuilder = (methodBuilder != null) ?
                    methodBuilder.DefineParameter(index, attrs, paramDef.Name) :
                    constructorBuilder.DefineParameter(index, attrs, paramDef.Name);

                if (defaultValue != null)
                {
                    object rawValue = defaultValue.Value;
                    if (rawValue == null)
                    {
                        var paramTypeRef = paramDef.GetType(_context);
                        if (paramTypeRef.IsValueType)
                        {
                            SetParameterDefaultStructValue(paramBuilder);
                        }
                        else
                        {
                            paramBuilder.SetConstant(null);
                        }
                    }
                    else
                    {
                        // TODO (tomat): Ref.Emit has too strict checks on the constant type. While it is ok to emit value,
                        // e.g. of type Int16 for parameter of type Int32 to metadata, Ref.Emit checks if these types are Type.IsAssignableFrom.
                        // To make this work we need to convert.
                        //
                        // We also need to support Nullable<T>.
                        if (rawValue.GetType().IsPrimitive)
                        {
                            // parameter type has already been resolved once when defining the method, so just retrive it:
                            Type paramType = ResolveType(paramDef.GetType(_context));

                            if (paramType.IsGenericType && paramType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            {
                                paramType = paramType.GetGenericArguments()[0];
                            }

                            if (paramType.IsEnum)
                            {
                                paramType = paramType.UnderlyingSystemType;
                            }

                            rawValue = Convert.ChangeType(rawValue, paramType, System.Globalization.CultureInfo.InvariantCulture);
                        }

                        paramBuilder.SetConstant(rawValue);
                    }
                }

                if (paramDef.IsMarshalledExplicitly)
                {
                    // FieldMarshal
                    var marshallingInformation = paramDef.MarshallingInformation;

                    if (marshallingInformation != null)
                    {
                        paramBuilder.SetCustomAttribute(GetMarshalAsAttribute(marshallingInformation));
                    }
                    else
                    {
                        Debug.Assert(!paramDef.MarshallingDescriptor.IsDefaultOrEmpty);
                        // TODO:
                        throw new NotImplementedException();
                    }
                }

                EmitCustomAttributes(paramBuilder, attributes);
            }
        }

        #endregion

        #region Method Bodies

        private Cci.IReference ReadSymbolRef(byte[] buffer, int pos)
        {
            return _tokenResolver.GetReferenceFromToken(BitConverter.ToUInt32(buffer, pos));
        }

        private string ReadString(byte[] buffer, int pos)
        {
            return _tokenResolver.GetStringFromToken(BitConverter.ToUInt32(buffer, pos)); ;
        }

        private static void WriteInt(byte[] buffer, int value, int pos)
        {
            unchecked
            {
                buffer[pos++] = (byte)value;
                buffer[pos++] = (byte)(value >> 8);
                buffer[pos++] = (byte)(value >> 16);
                buffer[pos] = (byte)(value >> 24);
            }
        }

        private void EmitMethodBody(MethodBuilder methodBuilder, ConstructorBuilder constructorBuilder, Cci.IMethodBody body)
        {
            byte[] byteCode = body.IL.Copy(0, body.IL.Length);
            int[] tokenFixupOffsets = SubstituteTokens(byteCode);

            // prepare signature:
            var sigHelper = SignatureHelper.GetLocalVarSigHelper(_builder);
            int localCount = 0;
            foreach (var local in body.LocalVariables)
            {
                var type = ResolveType(local.Type);
                if (local.IsReference)
                {
                    type = type.MakeByRefType();
                }

                if (local.CustomModifiers.Any())
                {
                    if (local.IsPinned)
                    {
                        throw new NotSupportedException("Ref.Emit limitation");
                    }

                    Type[] reqMods, optMods;
                    ResolveCustomModifiers(local.CustomModifiers, out reqMods, out optMods);
                    sigHelper.AddArgument(type, reqMods, optMods);
                }
                else
                {
                    sigHelper.AddArgument(type, local.IsPinned);
                }

                localCount++;
            }

            byte[] signature = sigHelper.GetSignature();

            // prepare exception handlers:
            var regions = body.ExceptionRegions;
            var handlers = new ExceptionHandler[regions.Length];
            int i = 0;
            foreach (var info in regions)
            {
                handlers[i++] = new ExceptionHandler(
                    tryOffset: (int)info.TryStartOffset,
                    tryLength: (int)info.TryEndOffset - (int)info.TryStartOffset,
                    filterOffset: (int)info.FilterDecisionStartOffset,
                    handlerOffset: (int)info.HandlerStartOffset,
                    handlerLength: (int)info.HandlerEndOffset - (int)info.HandlerStartOffset,
                    kind: (ExceptionHandlingClauseOptions)info.HandlerKind,
                    exceptionTypeToken: (info.HandlerKind == ExceptionRegionKind.Catch) ? GetToken(ResolveType(info.ExceptionType)) : 0);
            }

            if (methodBuilder != null)
            {
                methodBuilder.InitLocals = body.LocalsAreZeroed;
                methodBuilder.SetMethodBody(byteCode, body.MaxStack, signature, handlers, tokenFixupOffsets);
            }
            else
            {
                constructorBuilder.InitLocals = body.LocalsAreZeroed;
                constructorBuilder.SetMethodBody(byteCode, body.MaxStack, signature, handlers, tokenFixupOffsets);
            }
        }

        private int GetToken(MemberInfo member)
        {
            // TODO: cache in builders dictionary?
            switch (member.MemberType)
            {
                case MemberTypes.Constructor:
                    return _builder.GetConstructorToken((ConstructorInfo)member, optionalParameterTypes: null).Token;

                case MemberTypes.Method:
                    var methodRef = member as MethodRef;
                    if (methodRef != null)
                    {
                        return _builder.GetMethodToken(methodRef, methodRef.ExtraParameterTypes).Token;
                    }
                    else
                    {
                        return _builder.GetMethodToken((MethodInfo)member, optionalParameterTypes: null).Token;
                    }

                case MemberTypes.TypeInfo:
                case MemberTypes.NestedType:
                    return _builder.GetTypeToken((Type)member).Token;

                case MemberTypes.Field:
                    return _builder.GetFieldToken((FieldInfo)member).Token;

                default:
                    throw ExceptionUtilities.UnexpectedValue(member.MemberType);
            }
        }

        private int[] SubstituteTokens(byte[] buffer)
        {
            // TODO: SequencePoints
            // var sequencePoints = body.SequencePoints;
            // int curSequencePoint = 0;

            int curIndex = 0;
            MemberInfo member;

            List<int> tokenFixupOffsets = new List<int>();

            while (curIndex < buffer.Length)
            {
                OperandType operandType = Cci.InstructionOperandTypes.ReadOperandType(buffer, ref curIndex);

                // TODO: 
                // SequencePoint? location = null;
                // if ((sequencePoints != null) && 
                //     curSequencePoint < sequencePoints.Length && 
                //     sequencePoints[curSequencePoint].Offset == instructionStartIndex)
                // {
                //     location = sequencePoints[curSequencePoint++];
                // }

                // this.EmitPdbInformationFor(instructionStartIndex, location);

                switch (operandType)
                {
                    case OperandType.InlineField:
                        tokenFixupOffsets.Add(curIndex);
                        member = ResolveField((Cci.IFieldReference)ReadSymbolRef(buffer, curIndex));
                        WriteInt(buffer, GetToken(member), curIndex);
                        curIndex += 4;
                        break;

                    case OperandType.InlineMethod:
                        tokenFixupOffsets.Add(curIndex);
                        member = ResolveMethodOrConstructor((Cci.IMethodReference)ReadSymbolRef(buffer, curIndex));
                        WriteInt(buffer, GetToken(member), curIndex);
                        curIndex += 4;
                        break;

                    case OperandType.InlineType:
                        tokenFixupOffsets.Add(curIndex);
                        member = ResolveType((Cci.ITypeReference)ReadSymbolRef(buffer, curIndex));
                        WriteInt(buffer, GetToken(member), curIndex);
                        curIndex += 4;
                        break;

                    case OperandType.InlineTok: // ldtoken
                        tokenFixupOffsets.Add(curIndex);

                        // FieldRef, MethodRef, TypeRef
                        var symbolRef = ReadSymbolRef(buffer, curIndex);

                        Cci.ITypeReference typeRef;
                        Cci.IMethodReference methodRef;
                        Cci.IFieldReference fieldRef;
                        if ((typeRef = symbolRef as Cci.ITypeReference) != null)
                        {
                            member = ResolveType((Cci.ITypeReference)ReadSymbolRef(buffer, curIndex));
                        }
                        else if ((methodRef = symbolRef as Cci.IMethodReference) != null)
                        {
                            member = ResolveMethodOrConstructor((Cci.IMethodReference)ReadSymbolRef(buffer, curIndex));
                        }
                        else if ((fieldRef = symbolRef as Cci.IFieldReference) != null)
                        {
                            member = ResolveField((Cci.IFieldReference)ReadSymbolRef(buffer, curIndex));
                        }
                        else
                        {
                            throw ExceptionUtilities.Unreachable;
                        }
                        WriteInt(buffer, GetToken(member), curIndex);
                        curIndex += 4;
                        break;

                    case OperandType.InlineString: // ldstr
                        tokenFixupOffsets.Add(curIndex);
                        string str = ReadString(buffer, curIndex);
                        WriteInt(buffer, _builder.GetStringConstant(str).Token, curIndex);
                        curIndex += 4;
                        break;

                    case OperandType.InlineSig: // calli (not emitted by C#/VB)
                        throw new NotSupportedException();

                    case OperandType.InlineBrTarget:
                    case OperandType.InlineI:
                    case OperandType.ShortInlineR:
                        curIndex += 4;
                        break;

                    case OperandType.InlineI8:
                    case OperandType.InlineR:
                        curIndex += 8;
                        break;

                    case OperandType.InlineNone:
                        break;

                    case OperandType.InlineVar:
                        curIndex += 2;
                        break;

                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar:
                        curIndex += 1;
                        break;

                    case OperandType.InlineSwitch:
                        // skip switch arguments
                        curIndex += 4 + BitConverter.ToInt32(buffer, curIndex) * 4;
                        break;

                    default:
                        throw ExceptionUtilities.UnexpectedValue(operandType);
                }
            }

            return tokenFixupOffsets.ToArray();
        }

        #endregion

        #region Custom Attributes

        // TODO: Use CCI to builder CA blobs instead of CustomAttributeBuilder - we avoid a bunch of reflection checks.

        private static ConstructorInfo s_marshalAsCtor;
        private static FieldInfo s_marshalArraySubType;
        private static FieldInfo s_marshalIidParameterIndex;
        private static FieldInfo s_marshalMarshalCookie;
        private static FieldInfo s_marshalMarshalType;
        private static FieldInfo s_marshalMarshalTypeRef;
        private static FieldInfo s_marshalSafeArraySubType;
        private static FieldInfo s_marshalSafeArrayUserDefinedSubType;
        private static FieldInfo s_marshalSizeConst;
        private static FieldInfo s_marshalSizeParamIndex;

        private CustomAttributeBuilder GetMarshalAsAttribute(Cci.IMarshallingInformation marshallingInformation)
        {
            if (s_marshalAsCtor == null)
            {
                Type m = typeof(MarshalAsAttribute);
                s_marshalAsCtor = m.GetConstructor(new[] { typeof(UnmanagedType) });
                s_marshalArraySubType = m.GetField("ArraySubType");
                s_marshalIidParameterIndex = m.GetField("IidParameterIndex");
                s_marshalMarshalCookie = m.GetField("MarshalCookie");
                s_marshalMarshalType = m.GetField("MarshalType");
                s_marshalMarshalTypeRef = m.GetField("MarshalTypeRef");
                s_marshalSafeArraySubType = m.GetField("SafeArraySubType");
                s_marshalSafeArrayUserDefinedSubType = m.GetField("SafeArrayUserDefinedSubType");
                s_marshalSizeConst = m.GetField("SizeConst");
                s_marshalSizeParamIndex = m.GetField("SizeParamIndex");
            }

            FieldInfo[] fields = SpecializedCollections.EmptyArray<FieldInfo>();
            object[] values = SpecializedCollections.EmptyArray<object>();

            switch (marshallingInformation.UnmanagedType)
            {
                case UnmanagedType.ByValArray:
                    Debug.Assert(marshallingInformation.NumberOfElements >= 0);
                    if (marshallingInformation.ElementType >= 0)
                    {
                        fields = new[] { s_marshalSizeConst, s_marshalArraySubType };
                        values = new object[] { marshallingInformation.NumberOfElements, marshallingInformation.ElementType };
                    }
                    else
                    {
                        fields = new[] { s_marshalSizeConst };
                        values = new object[] { marshallingInformation.NumberOfElements };
                    }

                    break;

                case UnmanagedType.CustomMarshaler:
                    var marshaller = marshallingInformation.GetCustomMarshaller(_context);
                    var marshallerTypeRef = marshaller as Cci.ITypeReference;
                    if (marshallerTypeRef != null)
                    {
                        Type resolvedMarshaller = ResolveType(marshallerTypeRef);

                        fields = new[] { s_marshalMarshalTypeRef, s_marshalMarshalCookie };
                        values = new object[] { resolvedMarshaller, marshallingInformation.CustomMarshallerRuntimeArgument };
                    }
                    else
                    {
                        fields = new[] { s_marshalMarshalType, s_marshalMarshalCookie };
                        values = new object[] { marshaller, marshallingInformation.CustomMarshallerRuntimeArgument };
                    }

                    break;

                case UnmanagedType.LPArray:
                    var valueBuilder = ArrayBuilder<object>.GetInstance();
                    var fieldBuilder = ArrayBuilder<FieldInfo>.GetInstance();

                    fieldBuilder.Add(s_marshalArraySubType);
                    valueBuilder.Add(marshallingInformation.ElementType);

                    if (marshallingInformation.ParamIndex >= 0)
                    {
                        fieldBuilder.Add(s_marshalSizeParamIndex);
                        valueBuilder.Add(marshallingInformation.ParamIndex);
                    }

                    if (marshallingInformation.NumberOfElements >= 0)
                    {
                        fieldBuilder.Add(s_marshalSizeConst);
                        valueBuilder.Add(marshallingInformation.NumberOfElements);
                    }

                    fields = fieldBuilder.ToArrayAndFree();
                    values = valueBuilder.ToArrayAndFree();
                    break;

                case UnmanagedType.SafeArray:
                    if (marshallingInformation.SafeArrayElementSubtype >= 0)
                    {
                        var elementType = marshallingInformation.GetSafeArrayElementUserDefinedSubtype(_context);
                        if (elementType != null)
                        {
                            var resolvedType = ResolveType(elementType);

                            fields = new[] { s_marshalSafeArraySubType, s_marshalSafeArrayUserDefinedSubType };
                            values = new object[] { marshallingInformation.SafeArrayElementSubtype, resolvedType };
                        }
                        else
                        {
                            fields = new[] { s_marshalSafeArraySubType };
                            values = new object[] { marshallingInformation.SafeArrayElementSubtype };
                        }
                    }

                    break;

                case UnmanagedType.ByValTStr:
                    Debug.Assert(marshallingInformation.NumberOfElements >= 0);
                    fields = new[] { s_marshalSizeConst };
                    values = new object[] { marshallingInformation.NumberOfElements };
                    break;

                case UnmanagedType.Interface:
                case UnmanagedType.IDispatch:
                case UnmanagedType.IUnknown:
                    if (marshallingInformation.IidParameterIndex >= 0)
                    {
                        fields = new[] { s_marshalIidParameterIndex };
                        values = new object[] { marshallingInformation.IidParameterIndex };
                    }

                    break;

                default:
                    break;
            }

            return new CustomAttributeBuilder(s_marshalAsCtor, new object[] { marshallingInformation.UnmanagedType }, fields, values);
        }

        private void EmitCustomAttributes(TypeBuilder typeBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                typeBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(GenericTypeParameterBuilder genericParamBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                genericParamBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(MethodBuilder methodBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                methodBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(ConstructorBuilder ctorBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                ctorBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(FieldBuilder fieldBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                fieldBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(EventBuilder eventBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                eventBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(PropertyBuilder propertyBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                propertyBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(ParameterBuilder paramBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                paramBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(ModuleBuilder moduleBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                moduleBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private void EmitCustomAttributes(AssemblyBuilder assemblyBuilder, IEnumerable<Cci.ICustomAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                assemblyBuilder.SetCustomAttribute(CreateCustomAttributeBuilder(attribute));
            }
        }

        private CustomAttributeBuilder CreateCustomAttributeBuilder(Cci.ICustomAttribute attribute)
        {
            // TODO (tomat): Use CCI to build the blob?
            // TODO (tomat): The CustomAttributeBuilder has problems with application of an attribute on itself 
            // and other dependencies on unbaked types.
            // e.g. [My]class MyAttribute : Attribute { }

            var ctor = ResolveConstructor(attribute.Constructor(_context));
            var attributeType = ctor.DeclaringType;

            object[] argValues = new object[attribute.ArgumentCount];
            ArrayBuilder<object> propertyValues = null;
            ArrayBuilder<object> fieldValues = null;
            ArrayBuilder<FieldInfo> fields = null;
            ArrayBuilder<PropertyInfo> properties = null;
            try
            {
                int i = 0;
                foreach (Cci.IMetadataExpression arg in attribute.GetArguments(_context))
                {
                    argValues[i++] = GetMetadataExpressionValue(arg);
                }

                foreach (Cci.IMetadataNamedArgument namedArg in attribute.GetNamedArguments(_context))
                {
                    object value = GetMetadataExpressionValue(namedArg.ArgumentValue);
                    string name = namedArg.ArgumentName;

                    Type type = ResolveType(namedArg.Type);

                    if (namedArg.IsField)
                    {
                        if (fields == null)
                        {
                            fields = ArrayBuilder<FieldInfo>.GetInstance();
                            fieldValues = ArrayBuilder<object>.GetInstance();
                        }

                        var field = new FieldRef(attributeType, name, type);
                        fields.Add(field);
                        fieldValues.Add(value);
                    }
                    else
                    {
                        if (properties == null)
                        {
                            properties = ArrayBuilder<PropertyInfo>.GetInstance();
                            propertyValues = ArrayBuilder<object>.GetInstance();
                        }

                        var property = new CustomAttributeProperty(attributeType, name, type);
                        properties.Add(property);
                        propertyValues.Add(value);
                    }
                }

                try
                {
                    return new CustomAttributeBuilder(
                        ctor as ConstructorInfo ?? new ConstructorRef(ctor),
                        argValues,
                        (properties != null) ? properties.ToArray() : SpecializedCollections.EmptyArray<PropertyInfo>(),
                        (properties != null) ? propertyValues.ToArray() : SpecializedCollections.EmptyArray<object>(),
                        (fields != null) ? fields.ToArray() : SpecializedCollections.EmptyArray<FieldInfo>(),
                        (fields != null) ? fieldValues.ToArray() : SpecializedCollections.EmptyArray<object>());
                }
                catch (ArgumentException)
                {
                    throw new NotSupportedException("Ref.Emit limitation");
                }
            }
            finally
            {
                if (properties != null)
                {
                    properties.Free();
                    propertyValues.Free();
                }

                if (fields != null)
                {
                    fields.Free();
                    fieldValues.Free();
                }
            }
        }

        private object GetMetadataExpressionValue(Cci.IMetadataExpression expression)
        {
            var constant = expression as Cci.IMetadataConstant;
            if (constant != null)
            {
                return constant.Value;
            }

            var typeOf = expression as Cci.IMetadataTypeOf;
            if (typeOf != null)
            {
                return ResolveType(typeOf.TypeToGet);
            }

            var array = (Cci.IMetadataCreateArray)expression;
            Type elementType = ResolveType(array.ElementType);
            Array arrayValue = Array.CreateInstance(elementType, array.ElementCount);

            int i = 0;
            foreach (var element in array.Elements)
            {
                arrayValue.SetValue(GetMetadataExpressionValue(element), i++);
            }

            return arrayValue;
        }

        #endregion

        #region Flag mapping

        private static CallingConventions GetManagedCallingConvention(Cci.CallingConvention convention)
        {
            CallingConventions result;
            if ((convention & Cci.CallingConvention.ExtraArguments) != 0)
            {
                result = CallingConventions.VarArgs;
            }
            else
            {
                result = CallingConventions.Standard;
            }

            if ((convention & Cci.CallingConvention.HasThis) != 0)
            {
                result |= CallingConventions.HasThis;

                if ((convention & Cci.CallingConvention.ExplicitThis) != 0)
                {
                    result |= CallingConventions.ExplicitThis;
                }
            }

            return result;
        }

        private static GenericParameterAttributes GetGenericParameterAttributes(Cci.IGenericParameter typeParameter)
        {
            var result = GenericParameterAttributes.None;
            if (typeParameter.MustBeReferenceType)
            {
                result |= GenericParameterAttributes.ReferenceTypeConstraint;
            }

            if (typeParameter.MustBeValueType)
            {
                result |= GenericParameterAttributes.NotNullableValueTypeConstraint;
            }

            if (typeParameter.MustHaveDefaultConstructor)
            {
                result |= GenericParameterAttributes.DefaultConstructorConstraint;
            }

            if (typeParameter.Variance == Cci.TypeParameterVariance.Covariant)
            {
                result |= GenericParameterAttributes.Covariant;
            }

            if (typeParameter.Variance == Cci.TypeParameterVariance.Contravariant)
            {
                result |= GenericParameterAttributes.Contravariant;
            }

            return result;
        }

        #endregion

        #region TODO: HACK! HACK! HACK!

        private static MethodInfo s_lazyTypeBuilder_SetPInvokeData;
        private static MethodInfo s_lazyTypeBuilder_SetConstantValue;
        private object _lazyRuntimeModule;

        private object RuntimeModule
        {
            get
            {
                if (_lazyRuntimeModule == null)
                {
                    var ModuleBuilder_GetNativeHandle = typeof(ModuleBuilder).GetMethod("GetNativeHandle", BindingFlags.NonPublic | BindingFlags.Instance);
                    if (ModuleBuilder_GetNativeHandle == null)
                    {
                        throw new NotSupportedException("Ref.Emit limitation");
                    }

                    _lazyRuntimeModule = ModuleBuilder_GetNativeHandle.Invoke(_builder, SpecializedCollections.EmptyArray<object>());
                }

                return _lazyRuntimeModule;
            }
        }

        // Reflection.Emit is missing API to set all PInvoke flags (only some can be set via available APIs).
        private void SetPInvokeAttributes(MethodBuilder method, string dllName, string importName, Cci.PInvokeAttributes attributes)
        {
            if (s_lazyTypeBuilder_SetPInvokeData == null)
            {
                s_lazyTypeBuilder_SetPInvokeData = typeof(TypeBuilder).GetMethod("SetPInvokeData", BindingFlags.NonPublic | BindingFlags.Static);

                if (s_lazyTypeBuilder_SetPInvokeData == null)
                {
                    throw new NotSupportedException("Ref.Emit limitation");
                }
            }

            s_lazyTypeBuilder_SetPInvokeData.Invoke(null, new[] { RuntimeModule, dllName, importName, method.GetToken().Token, attributes });
        }

        // ParameterBuilder.SetConstant(object) doesn't allow us to set null as a default value for a value typed parameter.
        private void SetParameterDefaultStructValue(ParameterBuilder builder)
        {
            if (s_lazyTypeBuilder_SetConstantValue == null)
            {
                s_lazyTypeBuilder_SetConstantValue = (MethodInfo)typeof(TypeBuilder).GetMember("SetConstantValue", BindingFlags.NonPublic | BindingFlags.Static).
                    SingleOrDefault(overload => (((MethodInfo)overload).Attributes & MethodAttributes.PinvokeImpl) != 0);

                if (s_lazyTypeBuilder_SetConstantValue == null)
                {
                    throw new NotSupportedException("Ref.Emit limitation");
                }
            }

            const int ELEMENT_TYPE_VALUETYPE = 0x11;

            s_lazyTypeBuilder_SetConstantValue.Invoke(null, new[] { RuntimeModule, builder.GetToken().Token, ELEMENT_TYPE_VALUETYPE, null });
        }

        private static Func<char[], Type, int, Type> ArrayTypeFactory
        {
            get
            {
                if (s_lazyArrayTypeFactory == null)
                {
                    var st = typeof(System.Reflection.Emit.TypeBuilder).Assembly.GetType("System.Reflection.Emit.SymbolType");
                    var factoryMethod = st.GetMethod("FormCompoundType", BindingFlags.NonPublic | BindingFlags.Static);
                    s_lazyArrayTypeFactory = (Func<char[], Type, int, Type>)factoryMethod.CreateDelegate(typeof(Func<char[], Type, int, Type>));
                }

                return s_lazyArrayTypeFactory;
            }
        }

        private static Func<char[], Type, int, Type> s_lazyArrayTypeFactory;
        private static char[] s_arrayFormat = new[] { '[', ']' };

        // Creates and instance of a BCL internal SymbolType that represents a SzArray of given element type.
        // We can't implement this ourselves since Type.IsSzArray called by signature builder is internal.
        private static Type MakeSzArrayType(Type elementType)
        {
            return ArrayTypeFactory(s_arrayFormat, elementType, 0);
        }

        #endregion
    }
}
