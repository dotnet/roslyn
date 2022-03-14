// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable disable

using System;
using System.Linq;
using Roslyn.Utilities;
using Xunit;

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
            => new RudeEditDiagnosticDescription(_rudeEditKind, _squiggle, _arguments, value.Trim());

        public bool Equals(RudeEditDiagnosticDescription other)
        {
            return _rudeEditKind == other._rudeEditKind
                && _squiggle == other._squiggle
                && (_firstLine == other._firstLine || _firstLine == null || other._firstLine == null)
                && _arguments.SequenceEqual(other._arguments, object.Equals);
        }

        public override bool Equals(object obj)
            => obj is RudeEditDiagnosticDescription && Equals((RudeEditDiagnosticDescription)obj);

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

        internal void VerifyMessageFormat()
        {
            var descriptior = EditAndContinueDiagnosticDescriptors.GetDescriptor(_rudeEditKind);
            var format = descriptior.MessageFormat.ToString();
            try
            {
                string.Format(format, _arguments);
            }
            catch (FormatException)
            {
                Assert.True(false, $"Message format string was not supplied enough arguments.\nRudeEditKind: {_rudeEditKind}\nArguments supplied: {_arguments.Length}\nFormat string: {format}");
            }
        }
    }
}
