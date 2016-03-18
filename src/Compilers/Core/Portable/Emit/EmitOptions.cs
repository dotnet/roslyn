// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.Emit
{
    /// <summary>
    /// Represents compilation emit options.
    /// </summary>
    public sealed class EmitOptions : IEquatable<EmitOptions>
    {
        internal static readonly EmitOptions Default = new EmitOptions();

        /// <summary>
        /// True to emit an assembly excluding executable code such as method bodies.
        /// </summary>
        public bool EmitMetadataOnly { get; private set; }

        /// <summary>
        /// Tolerate errors, producing a PE stream and a success result even in the presence of (some) errors. 
        /// </summary>
        public bool TolerateErrors { get; private set; }

        /// <summary>
        /// Unless set (private) members that don't affect the language semantics of the resulting assembly will be excluded
        /// when emitting with <see cref="EmitMetadataOnly"/> on. 
        /// </summary>
        /// <remarks>
        /// Has no effect when <see cref="EmitMetadataOnly"/> is false.
        /// </remarks>
        public bool IncludePrivateMembers { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public string Instrument { get; private set; }

        /// <summary>
        /// Subsystem version
        /// </summary>
        public SubsystemVersion SubsystemVersion { get; private set; }

        /// <summary>
        /// Specifies the size of sections in the output file. 
        /// </summary>
        /// <remarks>
        /// Valid values are 0, 512, 1024, 2048, 4096 and 8192.
        /// If the value is 0 the file alignment is determined based upon the value of <see cref="Platform"/>.
        /// </remarks>
        public int FileAlignment { get; private set; }

        /// <summary>
        /// True to enable high entropy virtual address space for the output binary.
        /// </summary>
        public bool HighEntropyVirtualAddressSpace { get; private set; }

        /// <summary>
        /// Specifies the preferred base address at which to load the output DLL.
        /// </summary>
        public ulong BaseAddress { get; private set; }

        /// <summary>
        /// Debug information format.
        /// </summary>
        public DebugInformationFormat DebugInformationFormat { get; private set; }

        /// <summary>
        /// Assembly name override - file name and extension. If not specified the compilation name is used.
        /// </summary>
        /// <remarks>
        /// By default the name of the output assembly is <see cref="Compilation.AssemblyName"/>. Only in rare cases it is necessary
        /// to override the name.
        /// 
        /// CAUTION: If this is set to a (non-null) value other than the existing compilation output name, then internals-visible-to
        /// and assembly references may not work as expected.  In particular, things that were visible at bind time, based on the 
        /// name of the compilation, may not be visible at runtime and vice-versa.
        /// </remarks>
        public string OutputNameOverride { get; private set; }

        /// <summary>
        /// The name of the PDB file to be embedded in the PE image, or null to use the default.
        /// </summary>
        /// <remarks>
        /// If not specified the file name of the source module with an extension changed to "pdb" is used.
        /// </remarks>
        public string PdbFilePath { get; private set; }

        /// <summary>
        /// Runtime metadata version. 
        /// </summary>
        public string RuntimeMetadataVersion { get; private set; }

        // 1.2 BACKCOMPAT OVERLOAD -- DO NOT TOUCH
        public EmitOptions(
            bool metadataOnly,
            DebugInformationFormat debugInformationFormat,
            string pdbFilePath,
            string outputNameOverride,
            int fileAlignment,
            ulong baseAddress,
            bool highEntropyVirtualAddressSpace,
            SubsystemVersion subsystemVersion,
            string runtimeMetadataVersion,
            bool tolerateErrors,
            bool includePrivateMembers)
            : this(
                  metadataOnly,
                  debugInformationFormat,
                  pdbFilePath,
                  outputNameOverride,
                  fileAlignment,
                  baseAddress,
                  highEntropyVirtualAddressSpace,
                  subsystemVersion,
                  runtimeMetadataVersion,
                  tolerateErrors,
                  includePrivateMembers,
                  instrument: "")
        {
        }

        public EmitOptions(
            bool metadataOnly = false,
            DebugInformationFormat debugInformationFormat = 0,
            string pdbFilePath = null,
            string outputNameOverride = null,
            int fileAlignment = 0,
            ulong baseAddress = 0,
            bool highEntropyVirtualAddressSpace = false,
            SubsystemVersion subsystemVersion = default(SubsystemVersion),
            string runtimeMetadataVersion = null,
            bool tolerateErrors = false,
            bool includePrivateMembers = false,
            string instrument = "")
        {
            this.EmitMetadataOnly = metadataOnly;
            this.DebugInformationFormat = (debugInformationFormat == 0) ? DebugInformationFormat.Pdb : debugInformationFormat;
            this.PdbFilePath = pdbFilePath;
            this.OutputNameOverride = outputNameOverride;
            this.FileAlignment = fileAlignment;
            this.BaseAddress = baseAddress;
            this.HighEntropyVirtualAddressSpace = highEntropyVirtualAddressSpace;
            this.SubsystemVersion = subsystemVersion;
            this.RuntimeMetadataVersion = runtimeMetadataVersion;
            this.TolerateErrors = tolerateErrors;
            this.IncludePrivateMembers = includePrivateMembers;
            this.Instrument = instrument;
        }

        private EmitOptions(EmitOptions other) : this(
            other.EmitMetadataOnly,
            other.DebugInformationFormat,
            other.PdbFilePath,
            other.OutputNameOverride,
            other.FileAlignment,
            other.BaseAddress,
            other.HighEntropyVirtualAddressSpace,
            other.SubsystemVersion,
            other.RuntimeMetadataVersion,
            other.TolerateErrors,
            other.IncludePrivateMembers,
            other.Instrument)
        {
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as EmitOptions);
        }

        public bool Equals(EmitOptions other)
        {
            if (object.ReferenceEquals(other, null))
            {
                return false;
            }

            return
                this.EmitMetadataOnly == other.EmitMetadataOnly &&
                this.BaseAddress == other.BaseAddress &&
                this.FileAlignment == other.FileAlignment &&
                this.HighEntropyVirtualAddressSpace == other.HighEntropyVirtualAddressSpace &&
                this.SubsystemVersion.Equals(other.SubsystemVersion) &&
                this.DebugInformationFormat == other.DebugInformationFormat &&
                this.PdbFilePath == other.PdbFilePath &&
                this.OutputNameOverride == other.OutputNameOverride &&
                this.RuntimeMetadataVersion == other.RuntimeMetadataVersion &&
                this.TolerateErrors == other.TolerateErrors &&
                this.IncludePrivateMembers == other.IncludePrivateMembers &&
                this.Instrument == other.Instrument;
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.EmitMetadataOnly,
                   Hash.Combine(this.BaseAddress.GetHashCode(),
                   Hash.Combine(this.FileAlignment,
                   Hash.Combine(this.HighEntropyVirtualAddressSpace,
                   Hash.Combine(this.SubsystemVersion.GetHashCode(),
                   Hash.Combine((int)this.DebugInformationFormat,
                   Hash.Combine(this.PdbFilePath,
                   Hash.Combine(this.OutputNameOverride,
                   Hash.Combine(this.RuntimeMetadataVersion,
                   Hash.Combine(this.TolerateErrors,
                   Hash.Combine(this.IncludePrivateMembers,
                   Hash.Combine(this.Instrument, 0))))))))))));
        }

        public static bool operator ==(EmitOptions left, EmitOptions right)
        {
            return object.Equals(left, right);
        }

        public static bool operator !=(EmitOptions left, EmitOptions right)
        {
            return !object.Equals(left, right);
        }

        internal void ValidateOptions(DiagnosticBag diagnostics, CommonMessageProvider messageProvider)
        {
            if (!DebugInformationFormat.IsValid())
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_InvalidDebugInformationFormat, Location.None, (int)DebugInformationFormat));
            }

            if (OutputNameOverride != null)
            {
                Exception error = MetadataHelpers.CheckAssemblyOrModuleName(OutputNameOverride, argumentName: null);
                if (error != null)
                {
                    diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_InvalidOutputName, Location.None, error.Message));
                }
            }

            if (FileAlignment != 0 && !IsValidFileAlignment(FileAlignment))
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_InvalidFileAlignment, Location.None, FileAlignment));
            }

            if (!SubsystemVersion.Equals(SubsystemVersion.None) && !SubsystemVersion.IsValid)
            {
                diagnostics.Add(messageProvider.CreateDiagnostic(messageProvider.ERR_InvalidSubsystemVersion, Location.None, SubsystemVersion.ToString()));
            }
        }

        internal bool EmitDynamicAnalysisData => !string.IsNullOrEmpty(Instrument);

        internal static bool IsValidFileAlignment(int value)
        {
            switch (value)
            {
                case 512:
                case 1024:
                case 2048:
                case 4096:
                case 8192:
                    return true;

                default:
                    return false;
            }
        }

        public EmitOptions WithEmitMetadataOnly(bool value)
        {
            if (this.EmitMetadataOnly == value)
            {
                return this;
            }

            return new EmitOptions(this) { EmitMetadataOnly = value };
        }

        public EmitOptions WithPdbFilePath(string path)
        {
            if (this.PdbFilePath == path)
            {
                return this;
            }

            return new EmitOptions(this) { PdbFilePath = path };
        }

        public EmitOptions WithOutputNameOverride(string outputName)
        {
            if (this.OutputNameOverride == outputName)
            {
                return this;
            }

            return new EmitOptions(this) { OutputNameOverride = outputName };
        }

        public EmitOptions WithDebugInformationFormat(DebugInformationFormat format)
        {
            if (this.DebugInformationFormat == format)
            {
                return this;
            }

            return new EmitOptions(this) { DebugInformationFormat = format };
        }

        /// <summary>
        /// Sets the byte alignment for portable executable file sections.
        /// </summary>
        /// <param name="value">Can be one of the following values: 0, 512, 1024, 2048, 4096, 8192</param>
        public EmitOptions WithFileAlignment(int value)
        {
            if (this.FileAlignment == value)
            {
                return this;
            }

            return new EmitOptions(this) { FileAlignment = value };
        }

        public EmitOptions WithBaseAddress(ulong value)
        {
            if (this.BaseAddress == value)
            {
                return this;
            }

            return new EmitOptions(this) { BaseAddress = value };
        }

        public EmitOptions WithHighEntropyVirtualAddressSpace(bool value)
        {
            if (this.HighEntropyVirtualAddressSpace == value)
            {
                return this;
            }

            return new EmitOptions(this) { HighEntropyVirtualAddressSpace = value };
        }

        public EmitOptions WithSubsystemVersion(SubsystemVersion subsystemVersion)
        {
            if (subsystemVersion.Equals(this.SubsystemVersion))
            {
                return this;
            }

            return new EmitOptions(this) { SubsystemVersion = subsystemVersion };
        }

        public EmitOptions WithRuntimeMetadataVersion(string version)
        {
            if (RuntimeMetadataVersion == version)
            {
                return this;
            }

            return new EmitOptions(this) { RuntimeMetadataVersion = version };
        }

        public EmitOptions WithTolerateErrors(bool value)
        {
            if (TolerateErrors == value)
            {
                return this;
            }

            return new EmitOptions(this) { TolerateErrors = value };
        }

        public EmitOptions WithIncludePrivateMembers(bool value)
        {
            if (IncludePrivateMembers == value)
            {
                return this;
            }

            return new EmitOptions(this) { IncludePrivateMembers = value };
        }

        public EmitOptions WithInstrument(string instrument)
        {
            return new EmitOptions(this) { Instrument = instrument };
        }
    }
}
