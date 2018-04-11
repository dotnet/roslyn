// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Roslyn.Utilities;
using Xunit;

namespace Test.Utilities
{
    public struct DiagnosticResultLocation : IEquatable<DiagnosticResultLocation>
    {
        public DiagnosticResultLocation(string path, int line, int column)
        {
            Assert.True(line >= 0 || column >= 0, "At least one of line and column must be > 0");
            Assert.True(line >= -1 && column >= -1, "Both line and column must be >= -1");

            this.Path = path;
            this.Line = line;
            this.Column = column;
        }

        public string Path { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

        public override bool Equals(object obj)
        {
            return this.Equals((DiagnosticResultLocation)obj);
        }

        public override int GetHashCode()
        {
            return Hash.CombineValues(new[] { this.Path.GetHashCode(), this.Line, this.Column });
        }

        public bool Equals(DiagnosticResultLocation other)
        {
            return this.Path.Equals(other.Path, System.StringComparison.OrdinalIgnoreCase) &&
                this.Line == other.Line &&
                this.Column == other.Column;
        }

        public static bool operator ==(DiagnosticResultLocation left, DiagnosticResultLocation right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DiagnosticResultLocation left, DiagnosticResultLocation right)
        {
            return !left.Equals(right);
        }
    }

    public struct DiagnosticResult : IEquatable<DiagnosticResult>
    {
        private DiagnosticResultLocation[] _locations;

#pragma warning disable CA1819 // Properties should not return arrays
        public DiagnosticResultLocation[] Locations
#pragma warning restore CA1819 // Properties should not return arrays
        {
            get
            {
                if (_locations == null)
                {
                    _locations = Array.Empty<DiagnosticResultLocation>();
                }

                return _locations;
            }

            set
            {
                _locations = value;
            }
        }

        public DiagnosticSeverity Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

        public string Path => this.Locations.Length > 0 ? this.Locations[0].Path : "";

        public int Line => this.Locations.Length > 0 ? this.Locations[0].Line : -1;

        public int Column => this.Locations.Length > 0 ? this.Locations[0].Column : -1;

        private string GetSeverityString(DiagnosticSeverity severity)
        {
            switch (severity)
            {
                case DiagnosticSeverity.Hidden:
                    return "Hidden";
                case DiagnosticSeverity.Info:
                    return "Info";
                case DiagnosticSeverity.Warning:
                    return "Warning";
                case DiagnosticSeverity.Error:
                    return "Error";
                default:
                    return "";
            }
        }

        public override string ToString()
        {
            return $"{System.IO.Path.GetFileName(Path)}({Line},{Column}): {GetSeverityString(Severity)} {Id}: {Message}";
        }

        public override bool Equals(object obj)
        {
            return this.Equals((DiagnosticResult)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Locations.Length,
                Hash.CombineValues(this.Locations.Select(l => l.GetHashCode())));
        }

        public bool Equals(DiagnosticResult other)
        {
            return this.Locations.Length == other.Locations.Length &&
                this.Locations.SetEquals(other.Locations);
        }

        public static bool operator ==(DiagnosticResult left, DiagnosticResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(DiagnosticResult left, DiagnosticResult right)
        {
            return right.Equals(left);
        }
    }
}
