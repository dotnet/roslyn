﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;

namespace Microsoft.CodeAnalysis.EncapsulateField
{
    internal class EncapsulateFieldCodeAction : CodeAction
    {
        private EncapsulateFieldResult _result;
        private LocalizableString _title;

        public EncapsulateFieldCodeAction(EncapsulateFieldResult result, LocalizableString title)
        {
            _result = result;
            _title = title;
        }

        public override LocalizableString Title
        {
            get { return _title; }
        }

        protected override Task<Solution> GetChangedSolutionAsync(CancellationToken cancellationToken)
        {
            return _result.GetSolutionAsync(cancellationToken);
        }
    }
}
