// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Diagnostics;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class DynamicFlagsMap
    {
        private static readonly DynamicFlagsMap s_empty = new DynamicFlagsMap();

        private readonly Type _typeDefinition;
        private readonly DynamicFlagsCustomTypeInfo _dynamicFlags;
        private readonly int[] _startIndices;

        private DynamicFlagsMap()
        {
        }

        private DynamicFlagsMap(
            Type typeDefinition,
            DynamicFlagsCustomTypeInfo dynamicFlagsArray,
            int[] startIndices)
        {
            Debug.Assert(typeDefinition != null);
            Debug.Assert(startIndices != null);

            Debug.Assert(typeDefinition.IsGenericTypeDefinition);
            Debug.Assert(startIndices.Length == typeDefinition.GetGenericArguments().Length + 1);

            _typeDefinition = typeDefinition;
            _dynamicFlags = dynamicFlagsArray;
            _startIndices = startIndices;
        }

        internal static DynamicFlagsMap Create(TypeAndCustomInfo typeAndInfo)
        {
            var type = typeAndInfo.Type;
            Debug.Assert(type != null);
            if (!type.IsGenericType)
            {
                return s_empty;
            }

            var dynamicFlags = DynamicFlagsCustomTypeInfo.Create(typeAndInfo.Info);
            if (!dynamicFlags.Any())
            {
                return s_empty;
            }

            var typeDefinition = type.GetGenericTypeDefinition();
            Debug.Assert(typeDefinition != null);

            var typeArgs = type.GetGenericArguments();
            Debug.Assert(typeArgs.Length > 0);

            int pos = 1; // Consider "type" to have already been consumed.
            var startsBuilder = ArrayBuilder<int>.GetInstance();
            foreach (var typeArg in typeArgs)
            {
                startsBuilder.Add(pos);

                foreach (Type curr in new TypeWalker(typeArg))
                {
                    pos++;
                }
            }

            Debug.Assert(pos > 1);
            startsBuilder.Add(pos);

            return new DynamicFlagsMap(typeDefinition, dynamicFlags, startsBuilder.ToArrayAndFree());
        }

        internal DynamicFlagsCustomTypeInfo SubstituteDynamicFlags(Type type, DynamicFlagsCustomTypeInfo originalDynamicFlags)
        {
            if (_typeDefinition == null)
            {
                return originalDynamicFlags;
            }

            var substitutedFlags = ArrayBuilder<bool>.GetInstance();
            int f = 0;

            foreach (Type curr in new TypeWalker(type))
            {
                if (curr.IsGenericParameter && curr.DeclaringType.Equals(_typeDefinition))
                {
                    AppendFlagsFor(curr, substitutedFlags);
                }
                else
                {
                    substitutedFlags.Add(originalDynamicFlags[f]);
                }

                f++;
            }

            var result = DynamicFlagsCustomTypeInfo.Create(substitutedFlags);
            substitutedFlags.Free();
            return result;
        }

        private void AppendFlagsFor(Type type, ArrayBuilder<bool> builder)
        {
            Debug.Assert(type.IsGenericParameter);

            var genericParameterPosition = type.GenericParameterPosition;
            var start = _startIndices[genericParameterPosition];
            var nextStart = _startIndices[genericParameterPosition + 1];
            for (int i = start; i < nextStart; i++)
            {
                builder.Add(_dynamicFlags[i]);
            }
        }
    }
}