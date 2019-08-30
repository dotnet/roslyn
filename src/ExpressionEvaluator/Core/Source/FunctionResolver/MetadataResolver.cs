// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal delegate void OnFunctionResolvedDelegate<TModule, TRequest>(TModule module, TRequest request, int token, int version, int ilOffset);

    internal sealed class MetadataResolver<TProcess, TModule, TRequest>
        where TProcess : class
        where TModule : class
        where TRequest : class
    {
        private readonly TProcess _process;
        private readonly TModule _module;
        private readonly MetadataReader _reader;
        private readonly StringComparer _stringComparer; // for comparing strings
        private readonly bool _ignoreCase; // for comparing strings to strings represented with StringHandles
        private readonly OnFunctionResolvedDelegate<TModule, TRequest> _onFunctionResolved;

        internal MetadataResolver(
            TProcess process,
            TModule module,
            MetadataReader reader,
            bool ignoreCase,
            OnFunctionResolvedDelegate<TModule, TRequest> onFunctionResolved)
        {
            _process = process;
            _module = module;
            _reader = reader;
            _stringComparer = ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            _ignoreCase = ignoreCase;
            _onFunctionResolved = onFunctionResolved;
        }

        internal void Resolve(TRequest request, RequestSignature signature)
        {
            QualifiedName qualifiedTypeName;
            ImmutableArray<string> memberTypeParameters;
            GetNameAndTypeParameters(signature.MemberName, out qualifiedTypeName, out memberTypeParameters);

            var typeName = qualifiedTypeName.Qualifier;
            var memberName = qualifiedTypeName.Name;
            var memberParameters = signature.Parameters;

            var allTypeParameters = GetAllGenericTypeParameters(typeName);

            foreach (var typeHandle in _reader.TypeDefinitions)
            {
                var typeDef = _reader.GetTypeDefinition(typeHandle);
                int containingArity = CompareToTypeDefinition(typeDef, typeName);
                if (containingArity < 0)
                {
                    continue;
                }
                // Visit methods.
                foreach (var methodHandle in typeDef.GetMethods())
                {
                    var methodDef = _reader.GetMethodDefinition(methodHandle);
                    if (!IsResolvableMethod(methodDef))
                    {
                        continue;
                    }
                    if (MatchesMethod(typeDef, methodDef, typeName, memberName, allTypeParameters, containingArity, memberTypeParameters, memberParameters))
                    {
                        OnFunctionResolved(request, methodHandle);
                    }
                }
                if (memberTypeParameters.IsEmpty)
                {
                    // Visit properties.
                    foreach (var propertyHandle in typeDef.GetProperties())
                    {
                        var propertyDef = _reader.GetPropertyDefinition(propertyHandle);
                        if (MatchesProperty(typeDef, propertyDef, typeName, memberName, allTypeParameters, containingArity, memberParameters))
                        {
                            var accessors = propertyDef.GetAccessors();
                            OnAccessorResolved(request, accessors.Getter);
                            OnAccessorResolved(request, accessors.Setter);
                        }
                    }
                }
            }
        }

        // If the signature matches the TypeDefinition, including some or all containing
        // types and namespaces, returns a non-negative value indicating the arity of the
        // containing types that were not specified in the signature; otherwise returns -1.
        // For instance, "C<T>.D" will return 1 matching against metadata type N.M.A<T>.B.C<U>.D,
        // where N.M is a namespace and A<T>, B, C<U>, D are nested types. The value 1
        // is the arity of the containing types A<T>.B that were missing from the signature.
        // "C<T>.D" will not match C<T> or C<T>.D.E since the match must include the entire
        // signature, from nested TypeDefinition, out.
        private int CompareToTypeDefinition(TypeDefinition typeDef, Name signature)
        {
            if (signature == null)
            {
                return typeDef.GetGenericParameters().Count;
            }

            QualifiedName qualifiedName;
            ImmutableArray<string> typeParameters;
            GetNameAndTypeParameters(signature, out qualifiedName, out typeParameters);

            if (!MatchesTypeName(typeDef, qualifiedName.Name))
            {
                return -1;
            }

            var declaringTypeHandle = typeDef.GetDeclaringType();
            var declaringType = declaringTypeHandle.IsNil ? default(TypeDefinition) : _reader.GetTypeDefinition(declaringTypeHandle);
            int declaringTypeParameterCount = declaringTypeHandle.IsNil ? 0 : declaringType.GetGenericParameters().Count;
            if (!MatchesTypeParameterCount(typeParameters, typeDef.GetGenericParameters(), declaringTypeParameterCount))
            {
                return -1;
            }

            var qualifier = qualifiedName.Qualifier;
            if (declaringTypeHandle.IsNil)
            {
                // Compare namespace.
                return MatchesNamespace(typeDef, qualifier) ? 0 : -1;
            }
            else
            {
                // Compare declaring type.
                return CompareToTypeDefinition(declaringType, qualifier);
            }
        }

        private bool MatchesNamespace(TypeDefinition typeDef, Name signature)
        {
            if (signature == null)
            {
                return true;
            }

            var namespaceName = _reader.GetString(typeDef.Namespace);
            if (string.IsNullOrEmpty(namespaceName))
            {
                return false;
            }

            var parts = namespaceName.Split('.');
            for (int index = parts.Length - 1; index >= 0; index--)
            {
                if (signature == null)
                {
                    return true;
                }
                var qualifiedName = signature as QualifiedName;
                if (qualifiedName == null)
                {
                    return false;
                }
                var part = parts[index];
                if (!_stringComparer.Equals(qualifiedName.Name, part))
                {
                    return false;
                }
                signature = qualifiedName.Qualifier;
            }

            return signature == null;
        }

        private bool MatchesTypeName(TypeDefinition typeDef, string name)
        {
            var typeName = RemoveAritySeparatorIfAny(_reader.GetString(typeDef.Name));
            return _stringComparer.Equals(typeName, name);
        }

        private bool MatchesMethod(
            TypeDefinition typeDef,
            MethodDefinition methodDef,
            Name typeName,
            string methodName,
            ImmutableArray<string> allTypeParameters,
            int containingArity,
            ImmutableArray<string> methodTypeParameters,
            ImmutableArray<ParameterSignature> methodParameters)
        {
            if ((methodDef.Attributes & MethodAttributes.RTSpecialName) != 0)
            {
                if (!_reader.StringComparer.Equals(methodDef.Name, ".ctor", ignoreCase: false))
                {
                    // Unhandled special name.
                    return false;
                }
                if (!MatchesTypeName(typeDef, methodName))
                {
                    return false;
                }
            }
            else if (!_reader.StringComparer.Equals(methodDef.Name, methodName, _ignoreCase))
            {
                return false;
            }
            if (!MatchesTypeParameterCount(methodTypeParameters, methodDef.GetGenericParameters(), offset: 0))
            {
                return false;
            }
            if (methodParameters.IsDefault)
            {
                return true;
            }
            return MatchesParameters(methodDef, allTypeParameters, containingArity, methodTypeParameters, methodParameters);
        }

        private bool MatchesProperty(
            TypeDefinition typeDef,
            PropertyDefinition propertyDef,
            Name typeName,
            string propertyName,
            ImmutableArray<string> allTypeParameters,
            int containingArity,
            ImmutableArray<ParameterSignature> propertyParameters)
        {
            if (!_reader.StringComparer.Equals(propertyDef.Name, propertyName, _ignoreCase))
            {
                return false;
            }
            if (propertyParameters.IsDefault)
            {
                return true;
            }
            if (propertyParameters.Length == 0)
            {
                // Parameter-less properties should be specified
                // with no parameter list.
                return false;
            }
            // Match parameters against getter. Not supporting
            // matching against setter for write-only properties.
            var methodHandle = propertyDef.GetAccessors().Getter;
            if (methodHandle.IsNil)
            {
                return false;
            }
            var methodDef = _reader.GetMethodDefinition(methodHandle);
            return MatchesParameters(methodDef, allTypeParameters, containingArity, ImmutableArray<string>.Empty, propertyParameters);
        }

        private ImmutableArray<string> GetAllGenericTypeParameters(Name typeName)
        {
            var builder = ImmutableArray.CreateBuilder<string>();
            GetAllGenericTypeParameters(typeName, builder);
            return builder.ToImmutable();
        }

        private void GetAllGenericTypeParameters(Name typeName, ImmutableArray<string>.Builder builder)
        {
            if (typeName == null)
            {
                return;
            }

            QualifiedName qualifiedName;
            ImmutableArray<string> typeParameters;
            GetNameAndTypeParameters(typeName, out qualifiedName, out typeParameters);
            GetAllGenericTypeParameters(qualifiedName.Qualifier, builder);
            builder.AddRange(typeParameters);
        }

        private bool MatchesParameters(
            MethodDefinition methodDef,
            ImmutableArray<string> allTypeParameters,
            int containingArity,
            ImmutableArray<string> methodTypeParameters,
            ImmutableArray<ParameterSignature> methodParameters)
        {
            ImmutableArray<ParameterSignature> parameters;
            try
            {
                var decoder = new MetadataDecoder(_reader, allTypeParameters, containingArity, methodTypeParameters);
                parameters = decoder.DecodeParameters(methodDef);
            }
            catch (NotSupportedException)
            {
                return false;
            }
            catch (BadImageFormatException)
            {
                return false;
            }
            return methodParameters.SequenceEqual(parameters, MatchesParameter);
        }

        private void OnFunctionResolved(
            TRequest request,
            MethodDefinitionHandle handle)
        {
            Debug.Assert(!handle.IsNil);
            _onFunctionResolved(_module, request, token: MetadataTokens.GetToken(handle), version: 1, ilOffset: 0);
        }

        private void OnAccessorResolved(
            TRequest request,
            MethodDefinitionHandle handle)
        {
            if (handle.IsNil)
            {
                return;
            }
            var methodDef = _reader.GetMethodDefinition(handle);
            if (IsResolvableMethod(methodDef))
            {
                OnFunctionResolved(request, handle);
            }
        }

        private static bool MatchesTypeParameterCount(ImmutableArray<string> typeArguments, GenericParameterHandleCollection typeParameters, int offset)
        {
            return typeArguments.Length == typeParameters.Count - offset;
        }

        // parameterA from string signature, parameterB from metadata.
        private bool MatchesParameter(ParameterSignature parameterA, ParameterSignature parameterB)
        {
            return MatchesType(parameterA.Type, parameterB.Type) &&
                parameterA.IsByRef == parameterB.IsByRef;
        }

        // typeA from string signature, typeB from metadata.
        private bool MatchesType(TypeSignature typeA, TypeSignature typeB)
        {
            if (typeA.Kind != typeB.Kind)
            {
                return false;
            }

            switch (typeA.Kind)
            {
                case TypeSignatureKind.GenericType:
                    {
                        var genericA = (GenericTypeSignature)typeA;
                        var genericB = (GenericTypeSignature)typeB;
                        return MatchesType(genericA.QualifiedName, genericB.QualifiedName) &&
                            genericA.TypeArguments.SequenceEqual(genericB.TypeArguments, MatchesType);
                    }
                case TypeSignatureKind.QualifiedType:
                    {
                        var qualifiedA = (QualifiedTypeSignature)typeA;
                        var qualifiedB = (QualifiedTypeSignature)typeB;
                        // Metadata signature may be more qualified than the
                        // string signature but still considered a match
                        // (e.g.: "B<U>.C" should match N.A<T>.B<U>.C).
                        return (qualifiedA.Qualifier == null || (qualifiedB.Qualifier != null && MatchesType(qualifiedA.Qualifier, qualifiedB.Qualifier))) &&
                            _stringComparer.Equals(qualifiedA.Name, qualifiedB.Name);
                    }
                case TypeSignatureKind.ArrayType:
                    {
                        var arrayA = (ArrayTypeSignature)typeA;
                        var arrayB = (ArrayTypeSignature)typeB;
                        return MatchesType(arrayA.ElementType, arrayB.ElementType) &&
                            arrayA.Rank == arrayB.Rank;
                    }
                case TypeSignatureKind.PointerType:
                    {
                        var pointerA = (PointerTypeSignature)typeA;
                        var pointerB = (PointerTypeSignature)typeB;
                        return MatchesType(pointerA.PointedAtType, pointerB.PointedAtType);
                    }
                default:
                    throw ExceptionUtilities.UnexpectedValue(typeA.Kind);
            }
        }

        private static void GetNameAndTypeParameters(
            Name name,
            out QualifiedName qualifiedName,
            out ImmutableArray<string> typeParameters)
        {
            switch (name.Kind)
            {
                case NameKind.GenericName:
                    {
                        var genericName = (GenericName)name;
                        qualifiedName = genericName.QualifiedName;
                        typeParameters = genericName.TypeParameters;
                    }
                    break;
                case NameKind.QualifiedName:
                    {
                        qualifiedName = (QualifiedName)name;
                        typeParameters = ImmutableArray<string>.Empty;
                    }
                    break;
                default:
                    throw ExceptionUtilities.UnexpectedValue(name.Kind);
            }
        }

        private static bool IsResolvableMethod(MethodDefinition methodDef)
        {
            return (methodDef.Attributes & (MethodAttributes.Abstract | MethodAttributes.PinvokeImpl)) == 0;
        }

        private static string RemoveAritySeparatorIfAny(string typeName)
        {
            int index = typeName.LastIndexOf('`');
            return (index < 0) ? typeName : typeName.Substring(0, index);
        }
    }
}
