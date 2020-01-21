// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Utilities
{
    internal static class DateTimeUtilities
    {
        // From DateTime.cs.
        private const long TicksMask = 0x3FFFFFFFFFFFFFFF;

        internal static DateTime ToDateTime(double raw)
        {
            // This mechanism for getting the tick count from the underlying ulong field is copied
            // from System.DateTime.InternalTicks (ndp\clr\src\BCL\System\DateTime.cs).
            var tickCount = BitConverter.DoubleToInt64Bits(raw) & TicksMask;
            return new DateTime(tickCount);
        }
    }
}
