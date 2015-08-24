// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

//////////////////////////////////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Diagnostics.CodeAnalysis;
using System.Collections.ObjectModel;

namespace Microsoft.VisualStudio.LanguageServices.Implementation.Watson
{
    /// <summary>
    /// The kind of consent already obtained from the user.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "We don't want consumers passing None around because it's not a valid value")]
    internal enum ErrorReportConsent
    {
        /// <summary>
        /// Allows the error reporting infrastructure to decide whether to ask the user based on their previously established consent level.
        /// </summary>
        NotAsked = 1,
        /// <summary>
        /// The user has already approved the submission of this error report through another means.
        /// </summary>
        /// <remarks>
        /// This value should not be used without first obtaining approval from mailto:ddwattac.
        /// </remarks>
        Approved = 2,
        /// <summary>
        /// Indicates the user has denied permission to submit the report.
        /// </summary>
        Denied = 3,
        /// <summary>
        /// Causes UI to appear to ask the user before submitting the report.
        /// </summary>
        AlwaysPrompt = 4,
        /// <summary>
        /// Undocumented.
        /// </summary>
        Max = 5,
    }
    /// <summary>
    /// The level of detail and size of the dump to submit.
    /// </summary>
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Justification = "We don't want consumers passing None around because it's not a valid value")]
    internal enum ErrorDumpType
    {
        /// <summary>
        /// Similar to MiniDump but only capture the stack trace of the thread passed into WerReportAddDump
        /// which is the most reliable dump type
        /// If http://watson has been configured to ask for more information, this can be
        /// automatically upgraded to a heap dump.
        /// </summary>
        MicroDump = 1,
        /// <summary>
        /// By default, a dump that includes callstacks for all threads is submitted.
        /// If http://watson has been configured to ask for more information, this can be
        /// automatically upgraded to a heap dump.
        /// </summary>
        MiniDump = 2,
        /// <summary>
        /// Produces a much larger CAB that includes the heap.
        /// </summary>
        HeapDump = 3,
        /// <summary>
        /// Undocumented.
        /// </summary>
        Max = 4,
    }
    /// <summary>
    /// The severity of the error being reported.
    /// </summary>
    internal enum ErrorReportType
    {
        /// <summary>
        /// Undocumented.
        /// </summary>
        Noncritical,
        /// <summary>
        /// Undocumented.
        /// </summary>
        Critical,
        /// <summary>
        /// Undocumented.
        /// </summary>
        ApplicationCrash,
        /// <summary>
        /// Undocumented.
        /// </summary>
        ApplicationHang,
        /// <summary>
        /// Undocumented.
        /// </summary>
        Kernel,
        /// <summary>
        /// Undocumented.
        /// </summary>
        Invalid,
    }
    /// <summary>
    /// The type of files that can be added to the report.
    /// </summary>
    internal enum ErrorFileType
    {
        /// <summary>
        /// A limited minidump that contains only a stack trace.
        /// </summary>
        Microdump = 1,
        /// <summary>
        /// A minidump file.
        /// </summary>
        Minidump = 2,
        /// <summary>
        /// An extended minidump that contains additional data such as the process memory.
        /// </summary>
        Heapdump = 3,
        /// <summary>
        /// The document in use by the application at the time of the event. The document is added only if the server asks for this type of document.
        /// </summary>
        UserDocument = 4,
        /// <summary>
        /// Any other type of file. This file will always get added to the cab (but only if the server asks for a cab).
        /// </summary>
        Other = 5,
    }
    /// <summary>
    /// Flags that can be specified when adding a file to the report. 
    /// </summary>
    [Flags]
    internal enum ErrorFileFlags
    {
        /// <summary>
        /// Delete the file once WER is done
        /// </summary>
        DeleteWhenDone = 0x1,
        /// <summary>
        /// This file does not contain any PII
        /// </summary>
        AnonymousData = 0x2,
    }
    /// <summary>
    /// An immutable description of the type of error report to submit.
    /// </summary>
    internal class ErrorReportSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorReportSettings"/> class.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed", Justification = "Ignored")]
        public ErrorReportSettings(
        ErrorDumpType dumpType = ErrorDumpType.MiniDump,
        ErrorReportType reportType = ErrorReportType.Noncritical,
        string component = null,
        string eventName = null,
        ReadOnlyCollection<ErrorFile> files = null)
        {
            this.DumpType = dumpType;
            this.ReportType = reportType;
            this.Component = component;
            this.EventName = eventName;
            this.Files = files;
        }
        /// <summary>
        /// Gets the type of information to include in the error report.
        /// </summary>
        /// <value>The default value is <see cref="ErrorDumpType.MiniDump"/>.</value>
        /// <remarks>
        /// This value should typically be left at its default unless you first check with
        /// mailto:ddwattac
        /// </remarks>
        public readonly ErrorDumpType DumpType;

        /// <summary>
        /// Gets the type of report being 
        /// </summary>
        /// <value>The default value is <see cref="ErrorReportType.Noncritical"/>.</value>
        /// <remarks>
        /// This value should typically be either <see cref="ErrorReportType.Noncritical"/> or <see cref="ErrorReportType.Critical"/>
        /// unless you first check with mailto:ddwattac
        /// </remarks>
        public readonly ErrorReportType ReportType;
        /// <summary>
        /// Gets the logical component where the failure occurred.
        /// </summary>
        /// <value>
        /// A non-localized constant value.
        /// If <c>null</c> the default component name is used in the report.
        /// </value>
        /// <remarks>
        /// This value should not contain any parameterized values so that a single Watson bucket collects all instances of this failure.
        /// Its value will be used to assist in matching a failure to the team that owns the feature.
        /// </remarks>
        public readonly string Component;

        /// <summary>
        /// Gets the value that will appear as "Event Name" in the Windows Application Log and in the Watson error report.
        /// </summary>
        /// <value>
        /// A non-localized constant value.
        /// If <c>null</c> the default component name is used in the report.
        /// </value>
        /// <remarks>
        /// This value should not contain any parameterized values so that a single Watson bucket collects all instances of this failure.
        /// Generally it should be left at <c>null</c> so that the product's reserved event name can be used.
        /// </remarks>
        public readonly string EventName;

        /// <summary>
        /// Gets the files being added to report.
        /// </summary>
        public readonly ReadOnlyCollection<ErrorFile> Files;
    }

    /// <summary>
    /// Encapsulate the info required to add a file to report.
    /// </summary>
    internal class ErrorFile
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorFile"/> class.
        /// </summary>
        public ErrorFile(string path, ErrorFileType type, ErrorFileFlags flags)
        {
            this.Path = path;
            this.Type = type;
            this.Flags = flags;
        }
        /// <summary>
        /// Gets the file path being added to report
        /// </summary>
        public string Path { get; }
        /// <summary>
        /// Gets the type of the file being added to report.
        /// </summary>
        public ErrorFileType Type { get; }
        /// <summary>
        /// Gets the flags of the file being added to report.
        /// </summary>
        public ErrorFileFlags Flags { get; }
    }
}
