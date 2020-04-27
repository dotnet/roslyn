// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal class EncapsulateFieldCodeAction : CodeAction
    {
        private readonly EncapsulateFieldResult _result;
        private readonly string _title;

        public EncapsulateFieldCodeAction(EncapsulateFieldResult result, string title)
        {
            _result = result;
            _title = title;
        }

        public override string Title => _title;

        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
            => _result.GetSolutionAsync(cancellationToken);
    }
}
