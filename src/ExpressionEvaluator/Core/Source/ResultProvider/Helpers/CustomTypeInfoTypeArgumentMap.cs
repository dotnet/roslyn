// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class CustomTypeInfoTypeArgumentMap
    {
        private static readonly CustomTypeInfoTypeArgumentMap s_empty = new CustomTypeInfoTypeArgumentMap();

        private readonly Type _typeDefinition;
        private readonly ReadOnlyCollection<byte> _dynamicFlags;
        private readonly int[] _dynamicFlagStartIndices;
        private readonly int[] _tupleElementNameStartIndices;

        private CustomTypeInfoTypeArgumentMap()
        {
        }

        private CustomTypeInfoTypeArgumentMap(
            Type typeDefinition,
            ReadOnlyCollection<byte> dynamicFlags,
            int[] dynamicFlagStartIndices,
            ReadOnlyCollection<string> tupleElementNames,
            int[] tupleElementNameStartIndices)
        {
            Debug.Assert(typeDefinition != null);
            Debug.Assert((dynamicFlags != null) == (dynamicFlagStartIndices != null));
            Debug.Assert((tupleElementNames != null) == (tupleElementNameStartIndices != null));

#if DEBUG
            Debug.Assert(typeDefinition.IsGenericTypeDefinition);
            int n = typeDefinition.GetGenericArguments().Length;
            Debug.Assert(dynamicFlagStartIndices == null || dynamicFlagStartIndices.Length == n + 1);
            Debug.Assert(tupleElementNameStartIndices == null || tupleElementNameStartIndices.Length == n + 1);
#endif

            _typeDefinition = typeDefinition;
            _dynamicFlags = dynamicFlags;
            _dynamicFlagStartIndices = dynamicFlagStartIndices;
            TupleElementNames = tupleElementNames;
            _tupleElementNameStartIndices = tupleElementNameStartIndices;
        }

        internal static CustomTypeInfoTypeArgumentMap Create(TypeAndCustomInfo typeAndInfo)
        {
            var typeInfo = typeAndInfo.Info;
            if (typeInfo == null)
            {
                return s_empty;
            }

            var type = typeAndInfo.Type;
            Debug.Assert(type != null);
            if (!type.IsGenericType)
            {
                return s_empty;
            }

            ReadOnlyCollection<byte> dynamicFlags;
            ReadOnlyCollection<string> tupleElementNames;
            CustomTypeInfo.Decode(typeInfo.PayloadTypeId, typeInfo.Payload, out dynamicFlags, out tupleElementNames);
            if (dynamicFlags == null && tupleElementNames == null)
            {
                return s_empty;
            }

            var typeDefinition = type.GetGenericTypeDefinition();
            Debug.Assert(typeDefinition != null);

            var dynamicFlagStartIndices = (dynamicFlags == null) ? null : GetStartIndices(type, t => 1);
            var tupleElementNameStartIndices = (tupleElementNames == null) ? null : GetStartIndices(type, TypeHelpers.GetTupleCardinalityIfAny);

            return new CustomTypeInfoTypeArgumentMap(
                typeDefinition,
                dynamicFlags,
                dynamicFlagStartIndices,
                tupleElementNames,
                tupleElementNameStartIndices);
        }

        internal ReadOnlyCollection<string> TupleElementNames { get; }

        internal DkmClrCustomTypeInfo SubstituteCustomTypeInfo(Type type, DkmClrCustomTypeInfo customInfo)
        {
            if (_typeDefinition == null)
            {
                return customInfo;
            }

            ReadOnlyCollection<byte> dynamicFlags = null;
            ReadOnlyCollection<string> tupleElementNames = null;
            if (customInfo != null)
            {
                CustomTypeInfo.Decode(
                    customInfo.PayloadTypeId,
                    customInfo.Payload,
                    out dynamicFlags,
                    out tupleElementNames);
            }

            var substitutedFlags = SubstituteDynamicFlags(type, dynamicFlags);
            var substitutedNames = SubstituteTupleElementNames(type, tupleElementNames);
            return CustomTypeInfo.Create(substitutedFlags, substitutedNames);
        }

        private ReadOnlyCollection<byte> SubstituteDynamicFlags(Type type, ReadOnlyCollection<byte> dynamicFlagsOpt)
        {
            var builder = ArrayBuilder<bool>.GetInstance();
            int f = 0;

            foreach (Type curr in new TypeWalker(type))
            {
                if (curr.IsGenericParameter && curr.DeclaringType.Equals(_typeDefinition))
                {
                    AppendRangeFor(
                        curr,
                        _dynamicFlags,
                        _dynamicFlagStartIndices,
                        DynamicFlagsCustomTypeInfo.GetFlag,
                        builder);
                }
                else
                {
                    builder.Add(DynamicFlagsCustomTypeInfo.GetFlag(dynamicFlagsOpt, f));
                }

                f++;
            }

            var result = DynamicFlagsCustomTypeInfo.ToBytes(builder);
            builder.Free();
            return result;
        }

        private ReadOnlyCollection<string> SubstituteTupleElementNames(Type type, ReadOnlyCollection<string> tupleElementNamesOpt)
        {
            var builder = ArrayBuilder<string>.GetInstance();
            int i = 0;

            foreach (Type curr in new TypeWalker(type))
            {
                if (curr.IsGenericParameter && curr.DeclaringType.Equals(_typeDefinition))
                {
                    AppendRangeFor(
                        curr,
                        TupleElementNames,
                        _tupleElementNameStartIndices,
                        CustomTypeInfo.GetTupleElementNameIfAny,
                        builder);
                }
                else
                {
                    int n = curr.GetTupleCardinalityIfAny();
                    AppendRange(tupleElementNamesOpt, i, i + n, CustomTypeInfo.GetTupleElementNameIfAny, builder);
                    i += n;
                }
            }

            var result = (builder.Count == 0) ? null : builder.ToImmutable();
            builder.Free();
            return result;
        }

        private delegate int GetIndexCount(Type type);

        private static int[] GetStartIndices(Type type, GetIndexCount getIndexCount)
        {
            var typeArgs = type.GetGenericArguments();
            Debug.Assert(typeArgs.Length > 0);

            int pos = getIndexCount(type); // Consider "type" to have already been consumed.
            var startsBuilder = ArrayBuilder<int>.GetInstance();
            foreach (var typeArg in typeArgs)
            {
                startsBuilder.Add(pos);

                foreach (Type curr in new TypeWalker(typeArg))
                {
                    pos += getIndexCount(curr);
                }
            }

            startsBuilder.Add(pos);
            return startsBuilder.ToArrayAndFree();
        }

        private delegate U Map<T, U>(ReadOnlyCollection<T> collection, int index);

        private static void AppendRangeFor<T, U>(
            Type type,
            ReadOnlyCollection<T> collection,
            int[] startIndices,
            Map<T, U> map,
            ArrayBuilder<U> builder)
        {
            Debug.Assert(type.IsGenericParameter);
            if (startIndices == null)
            {
                return;
            }
            var genericParameterPosition = type.GenericParameterPosition;
            AppendRange(
                collection,
                startIndices[genericParameterPosition],
                startIndices[genericParameterPosition + 1],
                map,
                builder);
        }

        private static void AppendRange<T, U>(
            ReadOnlyCollection<T> collection,
            int start,
            int end,
            Map<T, U> map,
            ArrayBuilder<U> builder)
        {
            for (int i = start; i < end; i++)
            {
                builder.Add(map(collection, i));
            }
        }
    }
}
