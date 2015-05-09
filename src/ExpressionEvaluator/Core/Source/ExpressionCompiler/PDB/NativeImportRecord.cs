// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.CodeAnalysis.ExpressionEvaluator
{
    internal sealed class NativeImportRecord : ImportRecord
    {
        private readonly ImportTargetKind _targetKind;
        private readonly string _externAlias;
        private readonly string _alias;
        private readonly string _targetString;

        public override ImportTargetKind TargetKind => _targetKind;
        public override string Alias => _alias;
        public override string TargetString => _targetString;

        public string ExternAlias => _externAlias;

        private NativeImportRecord(
            ImportTargetKind targetKind,
            string externAlias,
            string alias,
            string targetString)
        {
            _targetKind = targetKind;
            _externAlias = externAlias;
            _alias = alias;
            _targetString = targetString;
        }

        public static bool TryCreateFromCSharpImportString(string importString, out ImportRecord record)
        {
            ImportTargetKind targetKind;
            string externAlias;
            string alias;
            string targetString;
            if (CustomDebugInfoReader.TryParseCSharpImportString(importString, out alias, out externAlias, out targetString, out targetKind))
            {
                record = new NativeImportRecord(
                    targetKind,
                    externAlias,
                    alias,
                    targetString);
                return true;
            }

            record = default(ImportRecord);
            return false;
        }

        public static bool TryCreateFromVisualBasicImportString(string importString, out ImportRecord record, out ImportScope scope)
        {
            ImportTargetKind targetKind;
            string alias;
            string targetString;
            if (CustomDebugInfoReader.TryParseVisualBasicImportString(importString, out alias, out targetString, out targetKind, out scope))
            {
                record = new NativeImportRecord(
                    targetKind,
                    externAlias: null,
                    alias: alias,
                    targetString: targetString);
                return true;
            }

            record = default(ImportRecord);
            return false;
        }

        public static ImportRecord CreateFromVisualBasicDteeNamespace(string namespaceName)
        {
            return new NativeImportRecord(
                ImportTargetKind.Namespace,
                externAlias: null,
                alias: null,
                targetString: namespaceName);
        }
    }
}
