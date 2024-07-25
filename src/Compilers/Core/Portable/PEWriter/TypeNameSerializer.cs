// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Collections;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.PooledObjects;
using System.Text;
using System.Diagnostics;
using System.Linq;
using System.Globalization;

namespace Microsoft.Cci
{
    internal static class TypeNameSerializer
    {
        internal static string GetSerializedTypeName(this ITypeReference typeReference, EmitContext context, bool allowTypeParameters = false)
        {
            bool isAssemblyQualified = true;
            return GetSerializedTypeName(typeReference, context, ref isAssemblyQualified, allowTypeParameters);
        }

        internal static string GetSerializedTypeName(this ITypeReference typeReference, EmitContext context, ref bool isAssemblyQualified, bool allowTypeParameters = false)
        {
            var pooled = PooledStringBuilder.GetInstance();
            StringBuilder sb = pooled.Builder;
            IArrayTypeReference arrType = typeReference as IArrayTypeReference;
            if (arrType != null)
            {
                typeReference = arrType.GetElementType(context);
                bool isAssemQual = false;
                AppendSerializedTypeName(sb, typeReference, ref isAssemQual, context, allowTypeParameters);
                if (arrType.IsSZArray)
                {
                    sb.Append("[]");
                }
                else
                {
                    sb.Append('[');
                    if (arrType.Rank == 1)
                    {
                        sb.Append('*');
                    }

                    sb.Append(',', (int)arrType.Rank - 1);

                    sb.Append(']');
                }

                goto done;
            }

            IPointerTypeReference pointer = typeReference as IPointerTypeReference;
            if (pointer != null)
            {
                typeReference = pointer.GetTargetType(context);
                bool isAssemQual = false;
                AppendSerializedTypeName(sb, typeReference, ref isAssemQual, context, allowTypeParameters);
                sb.Append('*');
                goto done;
            }

            INamespaceTypeReference namespaceType = typeReference.AsNamespaceTypeReference;
            if (namespaceType != null)
            {
                var name = namespaceType.NamespaceName;
                if (name.Length != 0)
                {
                    sb.Append(name);
                    sb.Append('.');
                }

                sb.Append(GetEscapedMetadataName(namespaceType));
                goto done;
            }

            if (typeReference.IsTypeSpecification())
            {
                if (typeReference is IFunctionPointerTypeReference)
                {
                    var messageProvider = context.Module.CommonCompilation.MessageProvider;
                    context.Diagnostics.Add(messageProvider.CreateDiagnostic(
                        messageProvider.ERR_FunctionPointerTypesInAttributeNotSupported,
                        context.Location ?? Location.None));
                    sb.Append("(fnptr)");
                    goto done;
                }

                if (allowTypeParameters)
                {
                    // When encoding types into extension erasure attributes we allow type parameters (encoded as `!N` and `!!N`).
                    if (typeReference.AsGenericTypeParameterReference is { } genericTypeParameter)
                    {
                        sb.Append("!");
                        var index = MetadataWriter.GetNumberOfInheritedTypeParameters(genericTypeParameter.DefiningType, context) + genericTypeParameter.Index;
                        sb.Append(index.ToString(CultureInfo.InvariantCulture));
                        goto done;
                    }
                    else if (typeReference.AsGenericMethodParameterReference is { } genericMethodParameter)
                    {
                        sb.Append("!!");
                        var index = genericMethodParameter.Index;
                        sb.Append(index.ToString(CultureInfo.InvariantCulture));
                        goto done;
                    }
                }

                ITypeReference uninstantiatedTypeReference = typeReference.GetUninstantiatedGenericType(context);
                Debug.Assert(uninstantiatedTypeReference != typeReference);

                ArrayBuilder<ITypeReference> consolidatedTypeArguments = ArrayBuilder<ITypeReference>.GetInstance();
                typeReference.GetConsolidatedTypeArguments(consolidatedTypeArguments, context);

                bool uninstantiatedTypeIsAssemblyQualified = false;
                sb.Append(GetSerializedTypeName(uninstantiatedTypeReference, context, ref uninstantiatedTypeIsAssemblyQualified));
                sb.Append('[');
                bool first = true;
                foreach (ITypeReference argument in consolidatedTypeArguments)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        sb.Append(',');
                    }

                    bool isAssemQual = true;
                    AppendSerializedTypeName(sb, argument, ref isAssemQual, context, allowTypeParameters);
                }
                consolidatedTypeArguments.Free();

                sb.Append(']');
                goto done;
            }

            INestedTypeReference nestedType = typeReference.AsNestedTypeReference;
            if (nestedType != null)
            {
                bool nestedTypeIsAssemblyQualified = false;
                sb.Append(GetSerializedTypeName(nestedType.GetContainingType(context), context, ref nestedTypeIsAssemblyQualified));
                sb.Append('+');
                sb.Append(GetEscapedMetadataName(nestedType));
                goto done;
            }

// TODO: error
done:
            if (isAssemblyQualified)
            {
                AppendAssemblyQualifierIfNecessary(sb, UnwrapTypeReference(typeReference, context), out isAssemblyQualified, context);
            }

            return pooled.ToStringAndFree();
        }

        private static void AppendSerializedTypeName(StringBuilder sb, ITypeReference type, ref bool isAssemQualified, EmitContext context, bool allowTypeParameters)
        {
            string argTypeName = GetSerializedTypeName(type, context, ref isAssemQualified, allowTypeParameters);
            if (isAssemQualified)
            {
                sb.Append('[');
            }

            sb.Append(argTypeName);
            if (isAssemQualified)
            {
                sb.Append(']');
            }
        }

        private static void AppendAssemblyQualifierIfNecessary(StringBuilder sb, ITypeReference typeReference, out bool isAssemQualified, EmitContext context)
        {
            INestedTypeReference nestedType = typeReference.AsNestedTypeReference;
            if (nestedType != null)
            {
                AppendAssemblyQualifierIfNecessary(sb, nestedType.GetContainingType(context), out isAssemQualified, context);
                return;
            }

            IGenericTypeInstanceReference genInst = typeReference.AsGenericTypeInstanceReference;
            if (genInst != null)
            {
                AppendAssemblyQualifierIfNecessary(sb, genInst.GetGenericType(context), out isAssemQualified, context);
                return;
            }

            IArrayTypeReference arrType = typeReference as IArrayTypeReference;
            if (arrType != null)
            {
                AppendAssemblyQualifierIfNecessary(sb, arrType.GetElementType(context), out isAssemQualified, context);
                return;
            }

            IPointerTypeReference pointer = typeReference as IPointerTypeReference;
            if (pointer != null)
            {
                AppendAssemblyQualifierIfNecessary(sb, pointer.GetTargetType(context), out isAssemQualified, context);
                return;
            }

            isAssemQualified = false;
            IAssemblyReference referencedAssembly = null;
            INamespaceTypeReference namespaceType = typeReference.AsNamespaceTypeReference;
            if (namespaceType != null)
            {
                referencedAssembly = namespaceType.GetUnit(context) as IAssemblyReference;
            }

            if (referencedAssembly != null)
            {
                var containingAssembly = context.Module.GetContainingAssembly(context);

                if (containingAssembly == null || !ReferenceEquals(referencedAssembly, containingAssembly))
                {
                    sb.Append(", ");
                    sb.Append(MetadataWriter.StrongName(referencedAssembly));
                    isAssemQualified = true;
                }
            }
        }

        private static string GetEscapedMetadataName(INamedTypeReference namedType)
        {
            var pooled = PooledStringBuilder.GetInstance();
            StringBuilder mangledName = pooled.Builder;

            const string needsEscaping = "\\[]*.+,& ";
            if (namedType.AssociatedFileIdentifier is string fileIdentifier)
            {
                Debug.Assert(needsEscaping.All(c => !fileIdentifier.Contains(c)));
                mangledName.Append(fileIdentifier);
            }

            foreach (var ch in namedType.Name)
            {
                if (needsEscaping.IndexOf(ch) >= 0)
                {
                    mangledName.Append('\\');
                }

                mangledName.Append(ch);
            }

            if (namedType.MangleName && namedType.GenericParameterCount > 0)
            {
                mangledName.Append(MetadataHelpers.GetAritySuffix(namedType.GenericParameterCount));
            }

            return pooled.ToStringAndFree();
        }

        /// <summary>
        /// Strip off *, &amp;, and [].
        /// </summary>
        private static ITypeReference UnwrapTypeReference(ITypeReference typeReference, EmitContext context)
        {
            while (true)
            {
                IArrayTypeReference arrType = typeReference as IArrayTypeReference;
                if (arrType != null)
                {
                    typeReference = arrType.GetElementType(context);
                    continue;
                }

                IPointerTypeReference pointer = typeReference as IPointerTypeReference;
                if (pointer != null)
                {
                    typeReference = pointer.GetTargetType(context);
                    continue;
                }

                return typeReference;
            }
        }

        /// <summary>
        /// Qualified name of namespace.
        /// e.g. "A.B.C"
        /// </summary>
        internal static string BuildQualifiedNamespaceName(INamespace @namespace)
        {
            Debug.Assert(@namespace != null);

            if (@namespace.ContainingNamespace == null)
            {
                return @namespace.Name;
            }

            var namesReversed = ArrayBuilder<string>.GetInstance();
            do
            {
                string name = @namespace.Name;
                if (name.Length != 0)
                {
                    namesReversed.Add(name);
                }

                @namespace = @namespace.ContainingNamespace;
            }
            while (@namespace != null);

            var result = PooledStringBuilder.GetInstance();

            for (int i = namesReversed.Count - 1; i >= 0; i--)
            {
                result.Builder.Append(namesReversed[i]);

                if (i > 0)
                {
                    result.Builder.Append('.');
                }
            }

            namesReversed.Free();
            return result.ToStringAndFree();
        }
    }
}
