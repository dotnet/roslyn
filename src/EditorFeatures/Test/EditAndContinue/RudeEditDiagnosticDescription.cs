// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal struct RudeEditDiagnosticDescription : IEquatable<RudeEditDiagnosticDescription>
    {
        private readonly RudeEditKind _rudeEditKind;
        private readonly string _squiggle;
        private readonly string[] _arguments;

        internal RudeEditDiagnosticDescription(RudeEditKind rudeEditKind, string squiggle, params string[] arguments)
        {
            _rudeEditKind = rudeEditKind;
            _squiggle = squiggle;
            _arguments = arguments ?? SpecializedCollections.EmptyArray<string>();
        }

        public bool Equals(RudeEditDiagnosticDescription other)
        {
            return _rudeEditKind == other._rudeEditKind
                && _squiggle == other._squiggle
                && _arguments.SequenceEqual(other._arguments, object.Equals);
        }

        public override bool Equals(object obj)
        {
            return obj is RudeEditDiagnosticDescription && Equals((RudeEditDiagnosticDescription)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(_squiggle, Hash.CombineValues(_arguments, (int)_rudeEditKind));
        }

        public override string ToString()
        {
            return string.Format("Diagnostic(RudeEditKind.{0}, {1})",
                _rudeEditKind,
                string.Join(", ", new[] { (_squiggle != null) ? "\"" + _squiggle.Replace("\r\n", "\\r\\n") + "\"" : "null" }.Concat(_arguments.Select(a => "\"" + a + "\""))));
        }
    }
}
