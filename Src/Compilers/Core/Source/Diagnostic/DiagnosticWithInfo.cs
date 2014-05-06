// Copyright (c) Microsoft Open Technologies, Inc.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;
using System.Collections.Immutable;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A diagnostic (such as a compiler error or a warning), along with the location where it occurred.
    /// </summary>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    [Serializable]
    internal class DiagnosticWithInfo : Diagnostic, ISerializable
    {
        private readonly DiagnosticInfo info;
        private readonly Location location;
        
        internal DiagnosticWithInfo(DiagnosticInfo info, Location location)
        {
            Debug.Assert(info != null);
            Debug.Assert(location != null);
            this.info = info;
            this.location = location;
        }

        protected DiagnosticWithInfo(SerializationInfo info, StreamingContext context)
        {
            this.info = (DiagnosticInfo)info.GetValue("info", typeof(DiagnosticInfo));
            this.location = (Location)info.GetValue("location", typeof(Location));
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            GetObjectData(info, context);
        }

        protected virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("info", Info, typeof(DiagnosticInfo));
            info.AddValue("location", this.location, typeof(Location));
        }

        public override Location Location
        {
            get { return this.location; }
        }

        public override IReadOnlyList<Location> AdditionalLocations
        {
            get { return this.Info.AdditionalLocations; }
        }

        public override IReadOnlyList<string> CustomTags
        {
            get
            {
                // Compiler diagnostics don't have any custom tags.
                return SpecializedCollections.EmptyReadOnlyList<string>();
            }
        }

        public override string Id
        {
            get { return this.Info.MessageIdentifier; }
        }

        public override string Category
        {
            get { return CompilerDiagnosticCategory; }
        }

        internal sealed override int Code
        {
            get { return this.Info.Code; }
        }

        public sealed override DiagnosticSeverity Severity
        {
            get { return this.Info.Severity; }
        }

        public sealed override bool IsEnabledByDefault
        {
            // All compiler errors and warnings are enabled by default.
            get { return true; }
        }

        public sealed override int WarningLevel
        {
            get { return this.Info.WarningLevel; }
        }

        public sealed override bool IsWarningAsError
        {
            get { return this.Info.IsWarningAsError; }
        }

        public override string GetMessage(System.Globalization.CultureInfo culture = null)
        {
            return this.Info.GetMessage(culture);
        }

        internal override IReadOnlyList<object> Arguments
        {
            get { return this.Info.Arguments; }
        }

        /// <summary>
        /// Get the information about the diagnostic: the code, severity, message, etc.
        /// </summary>
        public DiagnosticInfo Info
        {
            get
            {
                if (this.info.Severity == InternalDiagnosticSeverity.Unknown)
                {
                    return this.info.GetResolvedInfo();
                }

                return this.info;
            }
        }

        /// <summary>
        /// True if the DiagnosticInfo for this diagnostic requires (or required - this property
        /// is immutable) resolution.
        /// </summary>
        internal bool HasLazyInfo
        {
            get
            {
                return this.info.Severity == InternalDiagnosticSeverity.Unknown ||
                    this.info.Severity == InternalDiagnosticSeverity.Void;
            }
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Location.GetHashCode(), this.Info.GetHashCode());
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as Diagnostic);
        }

        public override bool Equals(Diagnostic obj)
        {
            if (this == obj)
            {
                return true;
            }

            var other = obj as DiagnosticWithInfo;

            if (other == null || this.GetType() != other.GetType())
            {
                return false;
            }

            return
                this.Location.Equals(other.location) &&
                this.Info.Equals(other.Info) &&
                this.AdditionalLocations.SequenceEqual(other.AdditionalLocations);
        }

        private string GetDebuggerDisplay()
        {
            switch (info.Severity)
            {
                case InternalDiagnosticSeverity.Unknown:
                    // If we called ToString before the diagnostic was resolved,
                    // we would risk infinite recursion (e.g. if we were still computing
                    // member lists).
                    return "Unresolved diagnostic at " + this.Location;

                case InternalDiagnosticSeverity.Void:
                    // If we called ToString on a void diagnostic, the MessageProvider
                    // would complain about the code.
                    return "Void diagnostic at " + this.Location;

                default:
                    return ToString();
            }
        }

        internal override Diagnostic WithLocation(Location location)
        {
            if (location == null)
            {
                throw new ArgumentNullException("location");
            }

            if (location != this.location)
            {
                return new DiagnosticWithInfo(this.info, location);
            }

            return this;
        }

        internal override Diagnostic WithWarningAsError(bool isWarningAsError)
        {
            if (this.IsWarningAsError != isWarningAsError)
            {
                return new DiagnosticWithInfo(this.Info.GetInstanceWithReportWarning(isWarningAsError), this.location);
            }

            return this;
        }

        internal override Diagnostic WithSeverity(DiagnosticSeverity severity)
        {
            if (this.Severity != severity)
            {
                return new DiagnosticWithInfo(this.Info.GetInstanceWithSeverity(severity), this.location);
            }

            return this;
        }
    }
}
