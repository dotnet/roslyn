// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal delegate void OnFunctionResolvedDelegate<TModule, TRequest>(TModule module, TRequest request, int token, int version, int ilOffset);

    internal sealed class MetadataResolver<TProcess, TModule, TRequest>
        where TProcess : class
        where TModule : class
        where TRequest : class
    {
        private readonly TModule _module;
        private readonly MetadataReader _reader;
        private readonly StringComparer _stringComparer; // for comparing strings
        private readonly bool _ignoreCase; // for comparing strings to strings represented with StringHandles
        private readonly OnFunctionResolvedDelegate<TModule, TRequest> _onFunctionResolved;

        internal MetadataResolver(
            TModule module,
            MetadataReader reader,
            bool ignoreCase,
            OnFunctionResolvedDelegate<TModule, TRequest> onFunctionResolved)
        {
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
                    if (MatchesMethod(typeDef, methodDef, memberName, allTypeParameters, containingArity, memberTypeParameters, memberParameters))
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
                        var accessors = propertyDef.GetAccessors();
                        if (MatchesPropertyOrEvent(propertyDef.Name, accessors.Getter, memberName, allTypeParameters, containingArity, memberParameters))
                        {
                            OnAccessorResolved(request, accessors.Getter);
                            OnAccessorResolved(request, accessors.Setter);
                        }
                    }

                    // Visit events.
                    foreach (var eventHandle in typeDef.GetEvents())
                    {
                        var eventDef = _reader.GetEventDefinition(eventHandle);
                        var accessors = eventDef.GetAccessors();

                        if (MatchesPropertyOrEvent(eventDef.Name, accessors.Adder, memberName, allTypeParameters, containingArity, memberParameters))
                        {
                            OnAccessorResolved(request, accessors.Adder);
                            OnAccessorResolved(request, accessors.Remover);
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
            var declaringType = declaringTypeHandle.IsNil ? default : _reader.GetTypeDefinition(declaringTypeHandle);
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
            string methodName,
            ImmutableArray<string> allTypeParameters,
            int containingArity,
            ImmutableArray<string> methodTypeParameters,
            ImmutableArray<ParameterSignature> methodParameters)
        {
            if (!MatchesMethodName(methodDef, typeDef, methodName))
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

        private bool MatchesPropertyOrEvent(
            StringHandle memberName,
            MethodDefinitionHandle primaryAccessorHandle,
            string name,
            ImmutableArray<string> allTypeParameters,
            int containingArity,
            ImmutableArray<ParameterSignature> propertyParameters)
        {
            if (!MatchesMemberName(memberName, name))
            {
                return false;
            }

            if (propertyParameters.IsDefault)
            {
                return true;
            }

            if (propertyParameters.Length == 0)
            {
                // Parameter-less properties/events should be specified
                // with no parameter list.
                return false;
            }

            // Match parameters against getter/adder. Not supporting
            // matching against setter for write-only properties.
            if (primaryAccessorHandle.IsNil)
            {
                return false;
            }

            var methodDef = _reader.GetMethodDefinition(primaryAccessorHandle);
            return MatchesParameters(methodDef, allTypeParameters, containingArity, ImmutableArray<string>.Empty, propertyParameters);
        }

        private bool MatchesMethodName(in MethodDefinition methodDef, in TypeDefinition declaringTypeDef, string name)
        {
            // special names:
            if ((methodDef.Attributes & MethodAttributes.RTSpecialName) != 0)
            {
                // constructor:
                var ctorName = (methodDef.Attributes & MethodAttributes.Static) == 0 ? WellKnownMemberNames.InstanceConstructorName : WellKnownMemberNames.StaticConstructorName;
                if (_reader.StringComparer.Equals(methodDef.Name, ctorName, ignoreCase: false) && MatchesTypeName(declaringTypeDef, name))
                {
                    return true;
                }
            }

            return MatchesMemberName(methodDef.Name, name);
        }

        private bool MatchesMemberName(in StringHandle memberName, string name)
        {
            if (_reader.StringComparer.Equals(memberName, name, _ignoreCase))
            {
                return true;
            }

            var comparer = _ignoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            var metadataName = _reader.GetString(memberName);

            // C# local function
            if (GeneratedNameParser.TryParseLocalFunctionName(metadataName, out var localFunctionName))
            {
                return comparer.Equals(name, localFunctionName);
            }

            // implicitly implemented interface member:
            var lastDot = metadataName.LastIndexOf('.');
            if (lastDot >= 0 && comparer.Equals(metadataName.Substring(lastDot + 1), name))
            {
                return true;
            }

            return false;
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
