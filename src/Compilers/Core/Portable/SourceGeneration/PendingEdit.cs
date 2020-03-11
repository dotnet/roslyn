// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;

#nullable enable
namespace Microsoft.CodeAnalysis
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In Progress")]
    public delegate bool EditCallback<T>(EditContext context, T edit) where T : PendingEdit;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In progress")]
    public abstract class PendingEdit
    {
        internal abstract GeneratorDriverState Commit(GeneratorDriverState state);

        internal abstract bool AcceptedBy(GeneratorInfo info);

        internal abstract bool TryApply(GeneratorInfo info, EditContext context);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In progress")]
    public abstract class AdditionalFileEdit : PendingEdit
    {
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("ApiDesign", "RS0016:Add public types and members to the declared API", Justification = "In progress")]
    public sealed class AdditionalFileAddedEdit : AdditionalFileEdit
    {
        public AdditionalFileAddedEdit(AdditionalText addedText)
        {
            AddedText = addedText;
        }

        public AdditionalText AddedText { get; }

        internal override GeneratorDriverState Commit(GeneratorDriverState state) => state.With(additionalTexts: state.AdditionalTexts.Add(this.AddedText));

        internal override bool AcceptedBy(GeneratorInfo info) => info.EditCallback is object;

        internal override bool TryApply(GeneratorInfo info, EditContext context) => info.EditCallback!.Invoke(context, this);
    }
}
