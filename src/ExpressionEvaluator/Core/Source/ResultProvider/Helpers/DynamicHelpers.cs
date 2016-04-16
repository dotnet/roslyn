// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class DynamicHelpers
    {
        public static DynamicFlagsCustomTypeInfo GetDynamicFlags(this IList<CustomAttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Constructor.DeclaringType.IsType("System.Runtime.CompilerServices", "DynamicAttribute"))
                {
                    var arguments = attribute.ConstructorArguments;
                    if (arguments.Count == 0)
                    {
                        var builder = ArrayBuilder<bool>.GetInstance(1);
                        builder.Add(true);
                        var result = DynamicFlagsCustomTypeInfo.Create(builder);
                        builder.Free();
                        return result;
                    }
                    else if (arguments.Count == 1)
                    {
                        var argumentType = arguments[0].ArgumentType;
                        if (argumentType.IsArray && argumentType.GetElementType().IsBoolean())
                        {
                            // Per https://msdn.microsoft.com/en-us/library/system.reflection.customattributetypedargument.argumenttype(v=vs.110).aspx,
                            // if ArgumentType indicates an array, then Value will actually be a ReadOnlyCollection.
                            var collection = (ReadOnlyCollection<CustomAttributeTypedArgument>)arguments[0].Value;
                            var numFlags = collection.Count;
                            var builder = ArrayBuilder<bool>.GetInstance(numFlags);
                            foreach (var typedArg in collection)
                            {
                                builder.Add((bool)typedArg.Value);
                            }
                            var result = DynamicFlagsCustomTypeInfo.Create(builder);
                            builder.Free();
                            return result;
                        }
                    }
                }
            }

            return default(DynamicFlagsCustomTypeInfo);
        }
    }
}
