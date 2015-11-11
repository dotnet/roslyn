// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
#region Assembly Microsoft.VisualStudio.Debugger.Engine, Version=1.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
// References\Debugger\v2.0\Microsoft.VisualStudio.Debugger.Engine.dll

#endregion
using Microsoft.VisualStudio.Debugger.Clr;
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
        public readonly ReadOnlyCollection<DkmClrType> Modopts;
        public readonly ReadOnlyCollection<DkmClrType> Modreqs;

        private DkmClrCustomTypeInfo(Guid payloadTypeId, ReadOnlyCollection<byte> payload, ReadOnlyCollection<DkmClrType> modopts, ReadOnlyCollection<DkmClrType> modreqs)
        {
            PayloadTypeId = payloadTypeId;
            Payload = payload;
            Modopts = modreqs;
            Modreqs = modreqs;

        }

        public static DkmClrCustomTypeInfo Create(Guid payloadTypeId, ReadOnlyCollection<byte> payload)
        {
            return new DkmClrCustomTypeInfo(payloadTypeId, payload, null, null);
        }

        public static DkmClrCustomTypeInfo Create(Guid payloadTypeId, ReadOnlyCollection<byte> payload, ReadOnlyCollection<DkmClrType> modopts, ReadOnlyCollection<DkmClrType> modreqs)
        {
            return new DkmClrCustomTypeInfo(payloadTypeId, payload, modopts, modreqs);
        }

        private string DebuggerDisplay => $"[{string.Join(", ", Payload.Select(b => $"0x{b:x2}"))}] from {PayloadTypeId}";
    }
}
