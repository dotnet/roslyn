// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal readonly struct TimeSlice
    {
        private readonly DateTime _end;

        public TimeSlice(TimeSpan duration)
            => _end = DateTime.UtcNow + duration;

        public bool IsOver
        {
            get
            {
                return DateTime.UtcNow > _end;
            }
        }
    }
}
