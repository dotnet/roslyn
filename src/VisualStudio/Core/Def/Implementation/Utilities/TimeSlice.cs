// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Utilities
{
    internal class TimeSlice
    {
        private readonly DateTime _end;

        public TimeSlice(TimeSpan duration)
        {
            _end = DateTime.UtcNow + duration;
        }

        public bool IsOver
        {
            get
            {
                return DateTime.UtcNow > _end;
            }
        }
    }
}
