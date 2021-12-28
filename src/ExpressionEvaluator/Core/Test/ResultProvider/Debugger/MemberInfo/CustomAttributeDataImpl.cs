// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Debugger.Metadata;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class CustomAttributeDataImpl : CustomAttributeData
    {
        internal readonly System.Reflection.CustomAttributeData CustomAttributeData;

        internal CustomAttributeDataImpl(System.Reflection.CustomAttributeData customAttributeData)
        {
            Debug.Assert(customAttributeData != null);
            this.CustomAttributeData = customAttributeData;
        }

        public override ConstructorInfo Constructor
        {
            get
            {
                return new ConstructorInfoImpl(CustomAttributeData.Constructor);
            }
        }

        public override IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get
            {
                return CustomAttributeData.ConstructorArguments.Select(MakeTypedArgument).ToList();
            }
        }

        private static CustomAttributeTypedArgument MakeTypedArgument(System.Reflection.CustomAttributeTypedArgument a)
        {
            var argumentType = (TypeImpl)a.ArgumentType;
            if (!argumentType.IsArray)
            {
                return new CustomAttributeTypedArgument(argumentType, a.Value);
            }

            var reflectionValue = (ReadOnlyCollection<System.Reflection.CustomAttributeTypedArgument>)a.Value;
            var lmrValue = new ReadOnlyCollection<CustomAttributeTypedArgument>(reflectionValue.Select(MakeTypedArgument).ToList());
            return new CustomAttributeTypedArgument(argumentType, lmrValue);
        }
    }
}
