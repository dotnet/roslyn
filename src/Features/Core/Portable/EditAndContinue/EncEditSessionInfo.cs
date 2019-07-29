// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Microsoft.CodeAnalysis.EditAndContinue
{
    internal class EncEditSessionInfo
    {
        public readonly HashSet<ValueTuple<ushort, ushort>> RudeEdits = new HashSet<ValueTuple<ushort, ushort>>();

        public IEnumerable<string> EmitDeltaErrorIds;
        public bool HadCompilationErrors;
        public bool HadRudeEdits;
        public bool HadValidChanges;
        public bool HadValidInsignificantChanges;

        public void LogRudeEdit(ushort kind, ushort syntaxKind)
        {
            RudeEdits.Add(ValueTuple.Create(kind, syntaxKind));
        }

        public bool IsEmpty()
        {
            return !(HadCompilationErrors || HadRudeEdits || HadValidChanges || HadValidInsignificantChanges);
        }
    }
}
