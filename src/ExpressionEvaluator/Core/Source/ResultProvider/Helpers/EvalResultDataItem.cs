// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Debugger;
using Microsoft.VisualStudio.Debugger.Evaluation;
using Microsoft.VisualStudio.Debugger.Evaluation.ClrCompilation;
using Type = Microsoft.VisualStudio.Debugger.Metadata.Type;

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    /// <summary>
    /// A pair of DkmClrValue and Expansion, used to store
    /// state on a DkmEvaluationResult.  Also computes the
    /// full name of the DkmClrValue.
    /// </summary>
    /// <remarks>
    /// The DkmClrValue is included here rather than directly
    /// on the Expansion so that the DkmClrValue is not kept
    /// alive by the Expansion.
    /// </remarks>
    internal sealed class EvalResultDataItem : DkmDataItem
    {
        /// <summary>
        /// May be null for synthesized nodes like "Results View", etc.
        /// </summary>
        public readonly string NameOpt;
        public readonly Type TypeDeclaringMember;
        public readonly Type DeclaredType;
        public readonly DkmClrValue Value;
        public readonly Expansion Expansion;
        public readonly bool ChildShouldParenthesize;
        public readonly string FullNameWithoutFormatSpecifiers;
        public readonly ReadOnlyCollection<string> FormatSpecifiers;
        public readonly string ChildFullNamePrefix;
        public readonly DkmEvaluationResultCategory Category;
        public readonly DkmEvaluationResultFlags Flags;
        public readonly string EditableValue;

        public string FullName
        {
            get
            {
                var name = this.FullNameWithoutFormatSpecifiers;
                if (name != null)
                {
                    foreach (var formatSpecifier in this.FormatSpecifiers)
                    {
                        name += ", " + formatSpecifier;
                    }
                }
                return name;
            }
        }

        public EvalResultDataItem(
            string name,
            Type typeDeclaringMember,
            Type declaredType,
            DkmClrValue value,
            Expansion expansion,
            bool childShouldParenthesize,
            string fullName,
            string childFullNamePrefixOpt,
            ReadOnlyCollection<string> formatSpecifiers,
            DkmEvaluationResultCategory category,
            DkmEvaluationResultFlags flags,
            string editableValue)
        {
            Debug.Assert(formatSpecifiers != null);
            Debug.Assert((flags & DkmEvaluationResultFlags.Expandable) == 0);

            this.NameOpt = name;
            this.TypeDeclaringMember = typeDeclaringMember;
            this.DeclaredType = declaredType;
            this.Value = value;
            this.ChildShouldParenthesize = childShouldParenthesize;
            this.FullNameWithoutFormatSpecifiers = fullName;
            this.ChildFullNamePrefix = childFullNamePrefixOpt;
            this.FormatSpecifiers = formatSpecifiers;
            this.Category = category;
            this.EditableValue = editableValue;
            this.Flags = flags | GetFlags(value) | ((expansion == null) ? DkmEvaluationResultFlags.None : DkmEvaluationResultFlags.Expandable);
            this.Expansion = expansion;
        }

        private static DkmEvaluationResultFlags GetFlags(DkmClrValue value)
        {
            if (value == null)
            {
                return DkmEvaluationResultFlags.None;
            }

            var resultFlags = value.EvalFlags;
            var type = value.Type.GetLmrType();

            if (type.IsBoolean())
            {
                resultFlags |= DkmEvaluationResultFlags.Boolean;
                if (true.Equals(value.HostObjectValue))
                {
                    resultFlags |= DkmEvaluationResultFlags.BooleanTrue;
                }
            }

            if (!value.IsError() && value.HasUnderlyingString())
            {
                resultFlags |= DkmEvaluationResultFlags.RawString;
            }

            return resultFlags;
        }

        protected override void OnClose()
        {
            Debug.WriteLine("Closing " + FullName);
            Value.Close();
        }
    }
}
