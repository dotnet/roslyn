// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.Editor.UnitTests.SignatureHelp
{
    public struct SignatureHelpTestItem
    {
        /// <summary>
        /// Includes prefix, signature, suffix.
        /// </summary>
        public readonly string Signature;

        /// <summary>
        /// The method xml documentation.
        /// </summary>
        public readonly string MethodDocumentation;

        /// <summary>
        /// The (currently selected/expected) parameter documentation. This can be null.
        /// </summary>
        public readonly string ParameterDocumentation;

        /// <summary>
        /// The currently selected parameter index. For some reason it can be null.
        /// For methods without any parameters, it's still 0 if cursor is between the parentheses, "goo($$)" for example.
        /// </summary>
        public readonly int? CurrentParameterIndex;

        /// <summary>
        /// Description of the method, such as the list of anonymous types: 
        /// Anonymous Types:
        ///     'a is new { string Name, int Age }
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Includes prefix, signature, suffix in pretty-printed form (i.e. when the signature wraps).
        /// </summary>
        public readonly string PrettyPrintedSignature;

        /// <summary>
        /// Whether this item is expected to be selected.
        /// Note: If no item is expected to be selected, the verification of the actual selected item is skipped.
        /// </summary>
        public readonly bool IsSelected;

        public SignatureHelpTestItem(
            string signature,
            string methodDocumentation = null,
            string parameterDocumentation = null,
            int? currentParameterIndex = null,
            string description = null,
            string prettyPrintedSignature = null,
            bool isSelected = false)
        {
            this.Signature = signature;
            this.MethodDocumentation = methodDocumentation;
            this.ParameterDocumentation = parameterDocumentation;
            this.CurrentParameterIndex = currentParameterIndex;
            this.Description = description;
            this.PrettyPrintedSignature = prettyPrintedSignature;
            this.IsSelected = isSelected;
        }
    }
}
