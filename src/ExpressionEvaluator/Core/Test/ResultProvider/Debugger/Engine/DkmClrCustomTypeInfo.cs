// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation
{
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    public class DkmClrCustomTypeInfo
    {
        public readonly Guid PayloadTypeId;
        public readonly ReadOnlyCollection<byte> Payload;

        private DkmClrCustomTypeInfo(Guid payloadTypeId, ReadOnlyCollection<byte> payload)
        {
            PayloadTypeId = payloadTypeId;
            Payload = payload;
        }

        public static DkmClrCustomTypeInfo Create(Guid payloadTypeId, ReadOnlyCollection<byte> payload)
        {
            return new DkmClrCustomTypeInfo(payloadTypeId, payload);
        }

        private string DebuggerDisplay => $"[{string.Join(", ", Payload.Select(b => $"0x{b:x2}"))}] from {PayloadTypeId}";
    }
}
