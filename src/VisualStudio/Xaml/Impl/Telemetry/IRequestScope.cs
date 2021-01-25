// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.VisualStudio.LanguageServices.Xaml.Telemetry
{
    internal interface IRequestScope : IDisposable
    {
        /// <summary>
        /// Record if any failure happens inside a RequestScope
        /// </summary>
        void RecordFailure(Exception? exception = null);
    }
}
