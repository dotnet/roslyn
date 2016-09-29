// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Microsoft.CodeAnalysis.Options;

namespace Microsoft.CodeAnalysis.Shared.Options
{
    /// <summary>
    /// options to indicate whether a certain component in Roslyn is enabled or not
    /// </summary>
    internal static class ServiceComponentOnOffOptions
    {
        public static readonly Option<bool> DiagnosticProvider = new Option<bool>(nameof(ServiceComponentOnOffOptions), nameof(DiagnosticProvider), defaultValue: true);
    }
}