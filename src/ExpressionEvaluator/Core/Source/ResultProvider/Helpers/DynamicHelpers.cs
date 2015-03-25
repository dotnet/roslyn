// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal static class DynamicHelpers
    {
        private static readonly BitArray TrueArray = new BitArray(new[] { true });

        public static DynamicFlagsCustomTypeInfo GetDynamicFlags(this IList<CustomAttributeData> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Constructor.DeclaringType.IsType("System.Runtime.CompilerServices", "DynamicAttribute"))
                {
                    var arguments = attribute.ConstructorArguments;
                    if (arguments.Count == 0)
                    {
                        return new DynamicFlagsCustomTypeInfo(TrueArray);
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
                            var array = new BitArray(numFlags);
                            for (int i = 0; i < numFlags; i++)
                            {
                                array[i] = (bool)collection[i].Value;
                            }
                            return new DynamicFlagsCustomTypeInfo(array);
                        }
                    }
                }
            }

            return default(DynamicFlagsCustomTypeInfo);
        }
    }
}
