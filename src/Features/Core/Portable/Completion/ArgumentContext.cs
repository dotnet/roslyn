// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Microsoft.CodeAnalysis.Completion
{
    internal sealed class ArgumentContext
    {
        public ArgumentContext(
            ArgumentProvider provider,
            Document document,
            int position,
            IParameterSymbol parameter,
            string? previousValue,
            CancellationToken cancellationToken)
        {
            Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Document = document ?? throw new ArgumentNullException(nameof(document));
            Position = position;
            Parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            PreviousValue = previousValue;
            CancellationToken = cancellationToken;
        }

        internal ArgumentProvider Provider { get; }

        public Document Document { get; }

        public int Position { get; }

        public IParameterSymbol Parameter { get; }

        public string? PreviousValue { get; }

        public CancellationToken CancellationToken { get; }

        public string? DefaultValue { get; set; }
    }
}
