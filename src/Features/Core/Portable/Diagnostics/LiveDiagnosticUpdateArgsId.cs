// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Diagnostics
{
    internal class LiveDiagnosticUpdateArgsId : AnalyzerUpdateArgsId
    {
        public readonly object Key;
        public readonly int Kind;

        public LiveDiagnosticUpdateArgsId(DiagnosticAnalyzer analyzer, object key, int kind) : base(analyzer)
        {
            Key = key;
            Kind = kind;
        }

        public override bool Equals(object obj)
        {
            var other = obj as LiveDiagnosticUpdateArgsId;
            if (other == null)
            {
                return false;
            }

            return Kind == other.Kind && Equals(Key, other.Key) && base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(Key, Hash.Combine(Kind, base.GetHashCode()));
        }
    }
}
