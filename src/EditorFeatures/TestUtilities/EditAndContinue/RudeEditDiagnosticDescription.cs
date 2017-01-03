// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.EditAndContinue.UnitTests
{
    internal struct RudeEditDiagnosticDescription : IEquatable<RudeEditDiagnosticDescription>
    {
        private readonly RudeEditKind _rudeEditKind;
        private readonly string _firstLine;
        private readonly string _squiggle;
        private readonly string[] _arguments;

        internal RudeEditDiagnosticDescription(RudeEditKind rudeEditKind, string squiggle, string[] arguments, string firstLine)
        {
            _rudeEditKind = rudeEditKind;
            _squiggle = squiggle;
            _firstLine = firstLine;
            _arguments = arguments ?? Array.Empty<string>();
        }

        public string FirstLine => _firstLine;

        public RudeEditDiagnosticDescription WithFirstLine(string value)
        {
            return new RudeEditDiagnosticDescription(_rudeEditKind, _squiggle, _arguments, value.Trim());
        }

        public bool Equals(RudeEditDiagnosticDescription other)
        {
            return _rudeEditKind == other._rudeEditKind
                && _squiggle == other._squiggle
                && (_firstLine == other._firstLine || _firstLine == null || other._firstLine == null)
                && _arguments.SequenceEqual(other._arguments, object.Equals);
        }

        public override bool Equals(object obj)
        {
            return obj is RudeEditDiagnosticDescription && Equals((RudeEditDiagnosticDescription)obj);
        }

        public override int GetHashCode()
        {
            return
                Hash.Combine(_squiggle,
                Hash.CombineValues(_arguments, (int)_rudeEditKind));
        }

        public override string ToString()
        {
            var arguments = string.Join(", ", new[] { (_squiggle != null) ? "\"" + _squiggle.Replace("\r\n", "\\r\\n") + "\"" : "null" }.Concat(_arguments.Select(a => "\"" + a + "\"")));
            var withLine = (_firstLine != null) ? $".WithFirstLine(\"{_firstLine}\")" : null;

            return $"Diagnostic(RudeEditKind.{_rudeEditKind}, {arguments}){withLine}";
        }
    }
}
