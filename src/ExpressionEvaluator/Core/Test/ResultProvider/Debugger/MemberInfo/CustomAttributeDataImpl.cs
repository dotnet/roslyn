// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Diagnostics;
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
    }
}
