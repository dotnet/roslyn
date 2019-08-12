// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Host
{
    /// <summary>
    /// list of well known <see cref="IEventListener"/> types
    /// </summary>
    internal static class WellKnownEventListeners
    {
        public const string Workspace = nameof(Workspace);
        public const string DiagnosticService = nameof(DiagnosticService);
        public const string TodoListProvider = nameof(TodoListProvider);
    }
}
