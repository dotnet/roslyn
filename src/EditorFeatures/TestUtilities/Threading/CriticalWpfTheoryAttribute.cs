// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;

namespace Roslyn.Test.Utilities
{
    /// <summary>
    /// Indicates a <see cref="WpfTheoryAttribute"/> test which is essential to product quality and cannot be skipped.
    /// </summary>
    public class CriticalWpfTheoryAttribute : WpfTheoryAttribute
    {
        [Obsolete("Critical tests cannot be skipped.", error: true)]
        public new string Skip
        {
            get { return base.Skip; }
            set { base.Skip = value; }
        }
    }
}
