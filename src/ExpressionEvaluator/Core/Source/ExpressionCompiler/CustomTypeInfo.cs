// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <remarks>
    /// We can't instantiate <see cref="DkmClrCustomTypeInfo"/> in the unit tests
    /// (since we run against a reference assembly), so we use this type as an
    /// intermediary.
    /// </remarks>
    internal struct CustomTypeInfo
    {
        public Guid PayloadTypeId;
        public ReadOnlyCollection<byte> Payload;

        public CustomTypeInfo(Guid payloadTypeId, ReadOnlyCollection<byte> payload)
        {
            this.PayloadTypeId = payloadTypeId;
            this.Payload = payload;
        }

        public DynamicFlagsCustomTypeInfo ToDynamicFlagsCustomTypeInfo()
        {
            return DynamicFlagsCustomTypeInfo.Create(this);
        }

        public DkmClrCustomTypeInfo ToDkmClrCustomTypeInfo()
        {
            return Payload == null
                ? null
                : DkmClrCustomTypeInfo.Create(PayloadTypeId, Payload);
        }
    }
}
