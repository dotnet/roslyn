// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class AttributeHelpers
    {
        internal static DkmClrCustomTypeInfo GetCustomTypeInfo(this IList<CustomAttributeData> attributes)
        {
            ReadOnlyCollection<byte> dynamicFlags = null;
            ReadOnlyCollection<string> tupleElementNames = null;
            foreach (var attribute in attributes)
            {
                var attributeType = attribute.Constructor.DeclaringType;
                if (attributeType.IsType("System.Runtime.CompilerServices", "DynamicAttribute"))
                {
                    dynamicFlags = GetDynamicFlags(attribute);
                }
                else if (attributeType.IsType("System.Runtime.CompilerServices", "TupleElementNamesAttribute"))
                {
                    tupleElementNames = GetTupleElementNames(attribute);
                }
            }
            return CustomTypeInfo.Create(dynamicFlags, tupleElementNames);
        }

        private static ReadOnlyCollection<CustomAttributeTypedArgument> GetAttributeArrayArgumentValue(CustomAttributeTypedArgument argument)
        {
            // Per https://msdn.microsoft.com/en-us/library/system.reflection.customattributetypedargument.argumenttype(v=vs.110).aspx,
            // if ArgumentType indicates an array, then Value will actually be a ReadOnlyCollection.
            return (ReadOnlyCollection<CustomAttributeTypedArgument>)argument.Value;
        }

        private static readonly ReadOnlyCollection<byte> DynamicFlagsTrue = new ReadOnlyCollection<byte>(new byte[] { 1 });

        private static ReadOnlyCollection<byte> GetDynamicFlags(CustomAttributeData attribute)
        {
            var arguments = attribute.ConstructorArguments;
            if (arguments.Count == 0)
            {
                return DynamicFlagsTrue;
            }
            else if (arguments.Count == 1)
            {
                var argument = arguments[0];
                var argumentType = argument.ArgumentType;
                if (argumentType.IsArray && argumentType.GetElementType().IsBoolean())
                {
                    var collection = GetAttributeArrayArgumentValue(argument);
                    var numFlags = collection.Count;
                    var builder = ArrayBuilder<bool>.GetInstance(numFlags);
                    foreach (var typedArg in collection)
                    {
                        builder.Add((bool)typedArg.Value);
                    }
                    var result = DynamicFlagsCustomTypeInfo.ToBytes(builder);
                    builder.Free();
                    return result;
                }
            }
            return null;
        }

        private static ReadOnlyCollection<string> GetTupleElementNames(CustomAttributeData attribute)
        {
            var arguments = attribute.ConstructorArguments;
            if (arguments.Count == 1)
            {
                var argument = arguments[0];
                var argumentType = argument.ArgumentType;
                if (argumentType.IsArray && argumentType.GetElementType().IsString())
                {
                    var collection = GetAttributeArrayArgumentValue(argument);
                    var numFlags = collection.Count;
                    var builder = ArrayBuilder<string>.GetInstance(numFlags);
                    foreach (var typedArg in collection)
                    {
                        builder.Add((string)typedArg.Value);
                    }
                    return builder.ToImmutableAndFree();
                }
            }
            return null;
        }
    }
}
