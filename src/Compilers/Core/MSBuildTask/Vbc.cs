// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Build.Framework;
using Microsoft.Build.Tasks.Hosting;
using Microsoft.Build.Utilities;
using Microsoft.CodeAnalysis.CommandLine;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.BuildTasks
{
    /// <summary>
    /// This class defines the "Vbc" XMake task, which enables building assemblies from VB
    /// source files by invoking the VB compiler. This is the new Roslyn XMake task,
    /// meaning that the code is compiled by using the Roslyn compiler server, rather
    /// than vbc.exe. The two should be functionally identical, but the compiler server
    /// should be significantly faster with larger projects and have a smaller memory
    /// footprint.
    /// </summary>
    public class Vbc : ManagedCompiler
    {
        private bool _useHostCompilerIfAvailable;

        // The following 1 fields are used, set and re-set in LogEventsFromTextOutput()
        /// <summary>
        /// This stores the original lines and error priority together in the order in which they were received.
        /// </summary>
        private readonly Queue<VBError> _vbErrorLines = new Queue<VBError>();

        // Used when parsing vbc output to determine the column number of an error
        private bool _isDoneOutputtingErrorMessage;
        private int _numberOfLinesInErrorMessage;

        internal override RequestLanguage Language => RequestLanguage.VisualBasicCompile;

        #region Properties

        // Please keep these alphabetized.  These are the parameters specific to Vbc.  The
        // ones shared between Vbc and Csc are defined in ManagedCompiler.cs, which is
        // the base class.

        public string? BaseAddress
        {
            set { _store[nameof(BaseAddress)] = value; }
            get { return (string?)_store[nameof(BaseAddress)]; }
        }

        public string? DisabledWarnings
        {
            set { _store[nameof(DisabledWarnings)] = value; }
            get { return (string?)_store[nameof(DisabledWarnings)]; }
        }

        public bool DisableSdkPath
        {
            set { _store[nameof(DisableSdkPath)] = value; }
            get { return _store.GetOrDefault(nameof(DisableSdkPath), false); }
        }

        public string? DocumentationFile
        {
            set { _store[nameof(DocumentationFile)] = value; }
            get { return (string?)_store[nameof(DocumentationFile)]; }
        }

        public string? ErrorReport
        {
            set { _store[nameof(ErrorReport)] = value; }
            get { return (string?)_store[nameof(ErrorReport)]; }
        }

        public bool GenerateDocumentation
        {
            set { _store[nameof(GenerateDocumentation)] = value; }
            get { return _store.GetOrDefault(nameof(GenerateDocumentation), false); }
        }

        public ITaskItem[]? Imports
        {
            set { _store[nameof(Imports)] = value; }
            get { return (ITaskItem[]?)_store[nameof(Imports)]; }
        }

        public string? ModuleAssemblyName
        {
            set { _store[nameof(ModuleAssemblyName)] = value; }
            get { return (string?)_store[nameof(ModuleAssemblyName)]; }
        }

        public bool NoStandardLib
        {
            set { _store[nameof(NoStandardLib)] = value; }
            get { return _store.GetOrDefault(nameof(NoStandardLib), false); }
        }

        // This is not a documented switch. It prevents the automatic reference to Microsoft.VisualBasic.dll.
        // The VB team believes the only scenario for this is when you are building that assembly itself.
        // We have to support the switch here so that we can build the SDE and VB trees, which need to build this assembly.
        // Although undocumented, it cannot be wrapped with #if BUILDING_DF_LKG because this would prevent dogfood builds
        // within VS, which must use non-LKG msbuild bits.
        public bool NoVBRuntimeReference
        {
            set { _store[nameof(NoVBRuntimeReference)] = value; }
            get { return _store.GetOrDefault(nameof(NoVBRuntimeReference), false); }
        }

        public bool NoWarnings
        {
            set { _store[nameof(NoWarnings)] = value; }
            get { return _store.GetOrDefault(nameof(NoWarnings), false); }
        }

        public string? OptionCompare
        {
            set { _store[nameof(OptionCompare)] = value; }
            get { return (string?)_store[nameof(OptionCompare)]; }
        }

        public bool OptionExplicit
        {
            set { _store[nameof(OptionExplicit)] = value; }
            get { return _store.GetOrDefault(nameof(OptionExplicit), true); }
        }

        public bool OptionStrict
        {
            set { _store[nameof(OptionStrict)] = value; }
            get { return _store.GetOrDefault(nameof(OptionStrict), false); }
        }

        public bool OptionInfer
        {
            set { _store[nameof(OptionInfer)] = value; }
            get { return _store.GetOrDefault(nameof(OptionInfer), false); }
        }

        // Currently only /optionstrict:custom
        public string? OptionStrictType
        {
            set { _store[nameof(OptionStrictType)] = value; }
            get { return (string?)_store[nameof(OptionStrictType)]; }
        }

        public bool RemoveIntegerChecks
        {
            set { _store[nameof(RemoveIntegerChecks)] = value; }
            get { return _store.GetOrDefault(nameof(RemoveIntegerChecks), false); }
        }

        public string? RootNamespace
        {
            set { _store[nameof(RootNamespace)] = value; }
            get { return (string?)_store[nameof(RootNamespace)]; }
        }

        public string? SdkPath
        {
            set { _store[nameof(SdkPath)] = value; }
            get { return (string?)_store[nameof(SdkPath)]; }
        }

        /// <summary>
        /// Name of the language passed to "/preferreduilang" compiler option.
        /// </summary>
        /// <remarks>
        /// If set to null, "/preferreduilang" option is omitted, and vbc.exe uses its default setting.
        /// Otherwise, the value is passed to "/preferreduilang" as is.
        /// </remarks>
        public string? PreferredUILang
        {
            set { _store[nameof(PreferredUILang)] = value; }
            get { return (string?)_store[nameof(PreferredUILang)]; }
        }

        public string? VsSessionGuid
        {
            set { _store[nameof(VsSessionGuid)] = value; }
            get { return (string?)_store[nameof(VsSessionGuid)]; }
        }

        public bool TargetCompactFramework
        {
            set { _store[nameof(TargetCompactFramework)] = value; }
            get { return _store.GetOrDefault(nameof(TargetCompactFramework), false); }
        }

        public bool UseHostCompilerIfAvailable
        {
            set { _useHostCompilerIfAvailable = value; }
            get { return _useHostCompilerIfAvailable; }
        }

        public string? VBRuntimePath
        {
            set { _store[nameof(VBRuntimePath)] = value; }
            get { return (string?)_store[nameof(VBRuntimePath)]; }
        }

        public string? Verbosity
        {
            set { _store[nameof(Verbosity)] = value; }
            get { return (string?)_store[nameof(Verbosity)]; }
        }

        public string? WarningsAsErrors
        {
            set { _store[nameof(WarningsAsErrors)] = value; }
            get { return (string?)_store[nameof(WarningsAsErrors)]; }
        }

        public string? WarningsNotAsErrors
        {
            set { _store[nameof(WarningsNotAsErrors)] = value; }
            get { return (string?)_store[nameof(WarningsNotAsErrors)]; }
        }

        public string? VBRuntime
        {
            set { _store[nameof(VBRuntime)] = value; }
            get { return (string?)_store[nameof(VBRuntime)]; }
        }

        public string? PdbFile
        {
            set { _store[nameof(PdbFile)] = value; }
            get { return (string?)_store[nameof(PdbFile)]; }
        }
        #endregion

        #region Tool Members

        private static readonly string[] s_separator = { Environment.NewLine };

        internal override void LogCompilerOutput(string output, MessageImportance messageImportance)
        {
            var lines = output.Split(s_separator, StringSplitOptions.None);
            foreach (string line in lines)
            {
                //Code below will parse the set of four lines that comprise a VB
                //error message into a single object. The four-line format contains
                //a second line that is blank. This must be passed to the code below
                //to satisfy the parser. The parser needs to work with output from
                //old compilers as well. 
                LogEventsFromTextOutput(line, messageImportance);
            }
        }

        /// <summary>
        ///  Return the name of the tool to execute.
        /// </summary>
        protected override string ToolNameWithoutExtension
        {
            get
            {
                return "vbc";
            }
        }

        /// <summary>
        /// Override Execute so that we can moved the PDB file if we need to,
        /// after the compiler is done.
        /// </summary>
        public override bool Execute()
        {
            if (!base.Execute())
            {
                return false;
            }

            if (!SkipCompilerExecution)
            {
                MovePdbFileIfNecessary(OutputAssembly?.ItemSpec);
            }

            return !Log.HasLoggedErrors;
        }

        /// <summary>
        /// Move the PDB file if the PDB file that was generated by the compiler
        /// is not at the specified path, or if it is newer than the one there.
        /// VBC does not have a switch to specify the PDB path, so we are essentially implementing that for it here.
        /// We need make this possible to avoid colliding with the PDB generated by WinMDExp.
        /// 
        /// If at some future point VBC.exe offers a /pdbfile switch, this function can be removed.
        /// </summary>
        internal void MovePdbFileIfNecessary(string? outputAssembly)
        {
            // Get the name of the output assembly because the pdb will be written beside it and will have the same name
            if (RoslynString.IsNullOrEmpty(PdbFile) || String.IsNullOrEmpty(outputAssembly))
            {
                return;
            }

            try
            {
                string actualPdb = Path.ChangeExtension(outputAssembly, ".pdb"); // This is the pdb that the compiler generated

                FileInfo actualPdbInfo = new FileInfo(actualPdb);

                string desiredLocation = PdbFile;
                if (!desiredLocation.EndsWith(".pdb", StringComparison.OrdinalIgnoreCase))
                {
                    desiredLocation += ".pdb";
                }

                FileInfo desiredPdbInfo = new FileInfo(desiredLocation);

                // If the compiler generated a pdb..
                if (actualPdbInfo.Exists)
                {
                    // .. and the desired one does not exist or it's older...
                    if (!desiredPdbInfo.Exists || (desiredPdbInfo.Exists && actualPdbInfo.LastWriteTime > desiredPdbInfo.LastWriteTime))
                    {
                        // Delete the existing one if it's already there, as Move would otherwise fail
                        if (desiredPdbInfo.Exists)
                        {
                            Utilities.DeleteNoThrow(desiredPdbInfo.FullName);
                        }

                        // Move the file to where we actually wanted VBC to put it
                        File.Move(actualPdbInfo.FullName, desiredLocation);
                    }
                }
            }
            catch (Exception e) when (Utilities.IsIoRelatedException(e))
            {
                Log.LogErrorWithCodeFromResources("VBC_RenamePDB", PdbFile, e.Message);
            }
        }

        /// <summary>
        /// vbc.exe only takes the BaseAddress in hexadecimal format.  But we allow the caller
        /// of the task to pass in the BaseAddress in either decimal or hexadecimal format.
        /// Examples of supported hex formats include "0x10000000" or "&amp;H10000000".
        /// </summary>
        internal string? GetBaseAddressInHex()
        {
            string? originalBaseAddress = this.BaseAddress;

            if (originalBaseAddress != null)
            {
                if (originalBaseAddress.Length > 2)
                {
                    string twoLetterPrefix = originalBaseAddress.Substring(0, 2);

                    if (
                         (0 == String.Compare(twoLetterPrefix, "0x", StringComparison.OrdinalIgnoreCase)) ||
                         (0 == String.Compare(twoLetterPrefix, "&h", StringComparison.OrdinalIgnoreCase))
                       )
                    {
                        // The incoming string is already in hex format ... we just need to
                        // remove the 0x or &H from the beginning.
                        return originalBaseAddress.Substring(2);
                    }
                }

                // The incoming BaseAddress is not in hexadecimal format, so we need to
                // convert it to hex.
                try
                {
                    uint baseAddressDecimal = UInt32.Parse(originalBaseAddress, CultureInfo.InvariantCulture);
                    return baseAddressDecimal.ToString("X", CultureInfo.InvariantCulture);
                }
                catch (FormatException e)
                {
                    throw Utilities.GetLocalizedArgumentException(e,
                        ErrorString.Vbc_ParameterHasInvalidValue, "BaseAddress", originalBaseAddress);
                }
            }

            return null;
        }

        /// <summary>
        /// Looks at all the parameters that have been set, and builds up the string
        /// containing all the command-line switches.
        /// </summary>
        protected override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
            // Pass sdkpath if we are invoking core compiler from framework to preserve the behavior that framework compiler would have.
            if (SdkPath is null && IsSdkFrameworkToCoreBridgeTask)
            {
                commandLine.AppendSwitchIfNotNull("/sdkpath:", RuntimeEnvironment.GetRuntimeDirectory());
            }

            commandLine.AppendSwitchIfNotNull("/baseaddress:", this.GetBaseAddressInHex());
            commandLine.AppendSwitchIfNotNull("/libpath:", this.AdditionalLibPaths, ",");
            commandLine.AppendSwitchIfNotNull("/imports:", this.Imports, ",");
            // Make sure this /doc+ switch comes *before* the /doc:<file> switch (which is handled in the
            // ManagedCompiler.cs base class).  /doc+ is really just an alias for /doc:<assemblyname>.xml,
            // and the last /doc switch on the command-line wins.  If the user provided a specific doc filename,
            // we want that one to win.
            commandLine.AppendPlusOrMinusSwitch("/doc", this._store, "GenerateDocumentation");
            commandLine.AppendSwitchIfNotNull("/optioncompare:", this.OptionCompare);
            commandLine.AppendPlusOrMinusSwitch("/optionexplicit", this._store, "OptionExplicit");
            // Make sure this /optionstrict+ switch appears *before* the /optionstrict:xxxx switch below

            /* twhitney: In Orcas a change was made for devdiv bug 16889 that set Option Strict-, whenever this.DisabledWarnings was
             * empty.  That was clearly the wrong thing to do and we found it when we had a project with all the warning configuration 
             * entries set to WARNING.  Because this.DisabledWarnings was empty in that case we would end up sending /OptionStrict- 
             * effectively silencing all the warnings that had been selected.
             * 
             * Now what we do is:
             *  If option strict+ is specified, that trumps everything and we just set option strict+ 
             *  Otherwise, just set option strict:custom.
             *  You may wonder why we don't try to set Option Strict-  The reason is that Option Strict- just implies a certain
             *  set of warnings that should be disabled (there's ten of them today)  You get the same effect by sending 
             *  option strict:custom on along with the correct list of disabled warnings.
             *  Rather than make this code know the current set of disabled warnings that comprise Option strict-, we just send
             *  option strict:custom on with the understanding that we'll get the same behavior as option strict- since we are passing
             *  the /nowarn line on that contains all the warnings OptionStrict- would disable anyway. The IDE knows what they are
             *  and puts them in the project file so we are good.  And by not making this code aware of which warnings comprise
             *  Option Strict-, we have one less place we have to keep up to date in terms of what comprises option strict-
             */

            // Decide whether we are Option Strict+ or Option Strict:custom
            object? optionStrictSetting = this._store["OptionStrict"];
            bool optionStrict = optionStrictSetting != null ? (bool)optionStrictSetting : false;
            if (optionStrict)
            {
                commandLine.AppendSwitch("/optionstrict+");
            }
            else // OptionStrict+ wasn't specified so use :custom.
            {
                commandLine.AppendSwitch("/optionstrict:custom");
            }

            commandLine.AppendSwitchIfNotNull("/optionstrict:", this.OptionStrictType);
            commandLine.AppendWhenTrue("/nowarn", this._store, "NoWarnings");
            commandLine.AppendSwitchWithSplitting("/nowarn:", this.DisabledWarnings, ",", ';', ',');
            commandLine.AppendWhenTrue("/nosdkpath", _store, nameof(DisableSdkPath));
            commandLine.AppendPlusOrMinusSwitch("/optioninfer", this._store, "OptionInfer");
            commandLine.AppendWhenTrue("/nostdlib", this._store, "NoStandardLib");
            commandLine.AppendWhenTrue("/novbruntimeref", this._store, "NoVBRuntimeReference");
            commandLine.AppendSwitchIfNotNull("/errorreport:", this.ErrorReport);
            commandLine.AppendSwitchIfNotNull("/platform:", this.PlatformWith32BitPreference);
            commandLine.AppendPlusOrMinusSwitch("/removeintchecks", this._store, "RemoveIntegerChecks");
            commandLine.AppendSwitchIfNotNull("/rootnamespace:", this.RootNamespace);
            commandLine.AppendSwitchIfNotNull("/sdkpath:", this.SdkPath);
            commandLine.AppendSwitchIfNotNull("/moduleassemblyname:", this.ModuleAssemblyName);
            commandLine.AppendWhenTrue("/netcf", this._store, "TargetCompactFramework");
            commandLine.AppendSwitchIfNotNull("/preferreduilang:", this.PreferredUILang);
            commandLine.AppendPlusOrMinusSwitch("/highentropyva", this._store, "HighEntropyVA");

            if (0 == String.Compare(this.VBRuntimePath, this.VBRuntime, StringComparison.OrdinalIgnoreCase))
            {
                commandLine.AppendSwitchIfNotNull("/vbruntime:", this.VBRuntimePath);
            }
            else if (this.VBRuntime != null)
            {
                string vbRuntimeSwitch = this.VBRuntime;
                if (0 == String.Compare(vbRuntimeSwitch, "EMBED", StringComparison.OrdinalIgnoreCase))
                {
                    commandLine.AppendSwitch("/vbruntime*");
                }
                else if (0 == String.Compare(vbRuntimeSwitch, "NONE", StringComparison.OrdinalIgnoreCase))
                {
                    commandLine.AppendSwitch("/vbruntime-");
                }
                else if (0 == String.Compare(vbRuntimeSwitch, "DEFAULT", StringComparison.OrdinalIgnoreCase))
                {
                    commandLine.AppendSwitch("/vbruntime+");
                }
                else
                {
                    commandLine.AppendSwitchIfNotNull("/vbruntime:", vbRuntimeSwitch);
                }
            }

            // Verbosity
            if (
                   (this.Verbosity != null) &&

                   (
                      (0 == String.Compare(this.Verbosity, "quiet", StringComparison.OrdinalIgnoreCase)) ||
                      (0 == String.Compare(this.Verbosity, "verbose", StringComparison.OrdinalIgnoreCase))
                   )
                )
            {
                commandLine.AppendSwitchIfNotNull("/", this.Verbosity);
            }

            if ((bool?)this._store[nameof(GenerateDocumentation)] != false)
            {
                // Only provide the filename when GenerateDocumentation is not
                // explicitly disabled.  Otherwise, the /doc switch (which comes
                // later in the command) overrides and re-enabled generating
                // documentation.
                commandLine.AppendSwitchIfNotNull("/doc:", this.DocumentationFile);
            }

            commandLine.AppendSwitchUnquotedIfNotNull("/define:", Vbc.GetDefineConstantsSwitch(this.DefineConstants));
            AddReferencesToCommandLine(commandLine);
            commandLine.AppendSwitchIfNotNull("/win32resource:", this.Win32Resource);

            // Special case for "Sub Main" (See VSWhidbey 381254)
            if (0 != String.Compare("Sub Main", this.MainEntryPoint, StringComparison.OrdinalIgnoreCase))
            {
                commandLine.AppendSwitchIfNotNull("/main:", this.MainEntryPoint);
            }

            base.AddResponseFileCommands(commandLine);

            // This should come after the "TreatWarningsAsErrors" flag is processed (in managedcompiler.cs).
            // Because if TreatWarningsAsErrors=false, then we'll have a /warnaserror- on the command-line,
            // and then any specific warnings that should be treated as errors should be specified with
            // /warnaserror+:<list> after the /warnaserror- switch.  The order of the switches on the command-line
            // does matter.
            //
            // Note that
            //      /warnaserror+
            // is just shorthand for:
            //      /warnaserror+:<all possible warnings>
            //
            // Similarly,
            //      /warnaserror-
            // is just shorthand for:
            //      /warnaserror-:<all possible warnings>
            commandLine.AppendSwitchWithSplitting("/warnaserror+:", this.WarningsAsErrors, ",", ';', ',');
            commandLine.AppendSwitchWithSplitting("/warnaserror-:", this.WarningsNotAsErrors, ",", ';', ',');

            // If not design time build and the globalSessionGuid property was set then add a -globalsessionguid:<guid>
            bool designTime = false;
            if (this.HostObject is IVbcHostObject vbHost)
            {
                designTime = vbHost.IsDesignTime();
            }
            else if (this.HostObject != null)
            {
                throw new InvalidOperationException(string.Format(ErrorString.General_IncorrectHostObject, "Vbc", "IVbcHostObject"));
            }
            if (!designTime)
            {
                if (!string.IsNullOrWhiteSpace(this.VsSessionGuid))
                {
                    commandLine.AppendSwitchIfNotNull("/sqmsessionguid:", this.VsSessionGuid);
                }
            }

            // It's a good idea for the response file to be the very last switch passed, just 
            // from a predictability perspective.  It also solves the problem that a dogfooder
            // ran into, which is described in an email thread attached to bug VSWhidbey 146883.
            // See also bugs 177762 and 118307 for additional bugs related to response file position.
            if (this.ResponseFiles != null)
            {
                foreach (ITaskItem response in this.ResponseFiles)
                {
                    commandLine.AppendSwitchIfNotNull("@", response.ItemSpec);
                }
            }
        }

        private void AddReferencesToCommandLine(CommandLineBuilderExtension commandLine)
        {
            if ((this.References == null) || (this.References.Length == 0))
            {
                return;
            }

            var references = new List<ITaskItem>(this.References.Length);
            var links = new List<ITaskItem>(this.References.Length);

            foreach (ITaskItem reference in this.References)
            {
                bool embed = Utilities.TryConvertItemMetadataToBool(reference, "EmbedInteropTypes");

                if (embed)
                {
                    links.Add(reference);
                }
                else
                {
                    references.Add(reference);
                }
            }

            if (links.Count > 0)
            {
                commandLine.AppendSwitchIfNotNull("/link:", links.ToArray(), ",");
            }

            if (references.Count > 0)
            {
                commandLine.AppendSwitchIfNotNull("/reference:", references.ToArray(), ",");
            }
        }

        /// <summary>
        /// Validate parameters, log errors and warnings and return true if
        /// Execute should proceed.
        /// </summary>
        protected override bool ValidateParameters()
        {
            if (!base.ValidateParameters())
            {
                return false;
            }

            // Validate that the "Verbosity" parameter is one of "quiet", "normal", or "verbose".
            if (this.Verbosity != null)
            {
                if ((0 != String.Compare(Verbosity, "normal", StringComparison.OrdinalIgnoreCase)) &&
                    (0 != String.Compare(Verbosity, "quiet", StringComparison.OrdinalIgnoreCase)) &&
                    (0 != String.Compare(Verbosity, "verbose", StringComparison.OrdinalIgnoreCase)))
                {
                    Log.LogErrorWithCodeFromResources("Vbc_EnumParameterHasInvalidValue", "Verbosity", this.Verbosity, "Quiet, Normal, Verbose");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This method is called by MSBuild when running vbc as a separate process, it does not get called
        /// for normal VBCSCompiler compilations. 
        /// 
        /// The vbc process emits multi-line error messages and this method is called for every line of 
        /// output one at a time. This method must queue up the messages and re-hydrate them back into the 
        /// original vbc structure such that we can call <see cref="TaskLoggingHelper.LogMessageFromText(string, MessageImportance)" />
        /// with the complete error message.
        /// </summary>
        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            // We can return immediately if this was not called by the out of proc compiler
            if (!this.UsedCommandLineTool)
            {
                base.LogEventsFromTextOutput(singleLine, messageImportance);
                return;
            }

            // We can also return immediately if the current string is not a warning or error
            // and we have not seen a warning or error yet. 'Error' and 'Warning' are not localized.
            if (_vbErrorLines.Count == 0 &&
                singleLine.IndexOf("warning", StringComparison.OrdinalIgnoreCase) == -1 &&
                singleLine.IndexOf("error", StringComparison.OrdinalIgnoreCase) == -1)
            {
                base.LogEventsFromTextOutput(singleLine, messageImportance);
                return;
            }

            ParseVBErrorOrWarning(singleLine, messageImportance);
        }

        /// <summary>
        /// Given a string, parses it to find out whether it's an error or warning and, if so,
        /// make sure it's validated properly.  
        /// </summary>
        /// <param name="singleLine">The line to parse</param>
        /// <param name="messageImportance">The MessageImportance to use when reporting the error.</param>
        private void ParseVBErrorOrWarning(string singleLine, MessageImportance messageImportance)
        {
            // if this string is empty then we haven't seen the first line of an error yet
            if (_vbErrorLines.Count > 0)
            {
                // vbc separates the error message from the source text with an empty line, so
                // we can check for an empty line to see if vbc finished outputting the error message
                if (!_isDoneOutputtingErrorMessage && singleLine.Length == 0)
                {
                    _isDoneOutputtingErrorMessage = true;
                    _numberOfLinesInErrorMessage = _vbErrorLines.Count;
                }

                _vbErrorLines.Enqueue(new VBError(singleLine, messageImportance));

                // We are looking for the line that indicates the column (contains the '~'),
                // which vbc outputs 3 lines below the error message:
                //
                // <error message>
                // <blank line>
                // <line with the source text>
                // <line with the '~'>
                if (_isDoneOutputtingErrorMessage &&
                    _vbErrorLines.Count == _numberOfLinesInErrorMessage + 3)
                {
                    // Once we have the 4th line (error line + 3), then parse it for the first ~
                    // which will correspond to the column of the token with the error because
                    // VBC respects the users's indentation settings in the file it is compiling
                    // and only outputs SPACE chars to STDOUT.

                    // The +1 is to translate the index into user columns which are 1 based.

                    VBError originalVBError = _vbErrorLines.Dequeue();
                    string originalVBErrorString = originalVBError.Message;

                    int column = singleLine.IndexOf('~') + 1;
                    int endParenthesisLocation = originalVBErrorString.IndexOf(") :", StringComparison.Ordinal);

                    // If for some reason the line does not contain any ~ then something went wrong
                    // so abort and return the original string.
                    if (column < 0 || endParenthesisLocation < 0)
                    {
                        // we need to output all of the original lines we ate.
                        Log.LogMessageFromText(originalVBErrorString, originalVBError.MessageImportance);
                        foreach (VBError vberror in _vbErrorLines)
                        {
                            base.LogEventsFromTextOutput(vberror.Message, vberror.MessageImportance);
                        }

                        _vbErrorLines.Clear();
                        return;
                    }

                    string? newLine = originalVBErrorString.Substring(0, endParenthesisLocation) + "," + column + originalVBErrorString.Substring(endParenthesisLocation);

                    // Output all of the lines of the error, but with the modified first line as well.
                    Log.LogMessageFromText(newLine, originalVBError.MessageImportance);
                    foreach (VBError vberror in _vbErrorLines)
                    {
                        base.LogEventsFromTextOutput(vberror.Message, vberror.MessageImportance);
                    }

                    _vbErrorLines.Clear();
                }
            }
            else
            {
                CanonicalError.Parts? parts = CanonicalError.Parse(singleLine);
                if (parts == null)
                {
                    base.LogEventsFromTextOutput(singleLine, messageImportance);
                }
                else if ((parts.category == CanonicalError.Parts.Category.Error ||
                     parts.category == CanonicalError.Parts.Category.Warning) &&
                     parts.column == CanonicalError.Parts.numberNotSpecified)
                {
                    if (parts.line != CanonicalError.Parts.numberNotSpecified)
                    {
                        // If we got here, then this is a standard VBC error or warning.
                        _vbErrorLines.Enqueue(new VBError(singleLine, messageImportance));
                        _isDoneOutputtingErrorMessage = false;
                        _numberOfLinesInErrorMessage = 0;
                    }
                    else
                    {
                        // Project-level errors don't have line numbers -- just output now. 
                        base.LogEventsFromTextOutput(singleLine, messageImportance);
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Many VisualStudio VB projects have values for the DefineConstants property that
        /// contain quotes and spaces.  Normally we don't allow parameters passed into the
        /// task to contain quotes, because if we weren't careful, we might accidentally
        /// allow a parameter injection attach.  But for "DefineConstants", we have to allow
        /// it.
        /// So this method prepares the string to be passed in on the /define: command-line
        /// switch.  It does that by quoting the entire string, and escaping the embedded
        /// quotes.
        /// </summary>
        internal static string? GetDefineConstantsSwitch
            (
            string? originalDefineConstants
            )
        {
            if ((originalDefineConstants == null) || (originalDefineConstants.Length == 0))
            {
                return null;
            }

            StringBuilder finalDefineConstants = new StringBuilder(originalDefineConstants);

            // Replace slash-quote with slash-slash-quote.
            finalDefineConstants.Replace("\\\"", "\\\\\"");

            // Replace quote with slash-quote.
            finalDefineConstants.Replace("\"", "\\\"");

            // Surround the whole thing with a pair of double-quotes.
            finalDefineConstants.Insert(0, '"');
            finalDefineConstants.Append('"');

            // Now it's ready to be passed in to the /define: switch.
            return finalDefineConstants.ToString();
        }

        /// <summary>
        /// This method will initialize the host compiler object with all the switches,
        /// parameters, resources, references, sources, etc.
        ///
        /// It returns true if everything went according to plan.  It returns false if the
        /// host compiler had a problem with one of the parameters that was passed in.
        /// 
        /// This method also sets the "this.HostCompilerSupportsAllParameters" property
        /// accordingly.
        ///
        /// Example:
        ///     If we attempted to pass in Platform="goobar", then this method would
        ///     set HostCompilerSupportsAllParameters=true, but it would throw an 
        ///     exception because the host compiler fully supports
        ///     the Platform parameter, but "goobar" is an illegal value.
        ///
        /// Example:
        ///     If we attempted to pass in NoConfig=false, then this method would set
        ///     HostCompilerSupportsAllParameters=false, because while this is a legal
        ///     thing for csc.exe, the IDE compiler cannot support it.  In this situation
        ///     the return value will also be false.
        /// </summary>
        private bool InitializeHostCompiler(IVbcHostObject vbcHostObject)
        {
            this.HostCompilerSupportsAllParameters = this.UseHostCompilerIfAvailable;
            string param = "Unknown";

            try
            {
                param = nameof(vbcHostObject.BeginInitialization);
                vbcHostObject.BeginInitialization();

                CheckHostObjectSupport(param = nameof(AdditionalLibPaths), vbcHostObject.SetAdditionalLibPaths(AdditionalLibPaths));
                CheckHostObjectSupport(param = nameof(AddModules), vbcHostObject.SetAddModules(AddModules));

                // For host objects which support them, set the analyzers, ruleset and additional files.
                IAnalyzerHostObject? analyzerHostObject = vbcHostObject as IAnalyzerHostObject;
                if (analyzerHostObject != null)
                {
                    CheckHostObjectSupport(param = nameof(Analyzers), analyzerHostObject.SetAnalyzers(Analyzers));
                    CheckHostObjectSupport(param = nameof(CodeAnalysisRuleSet), analyzerHostObject.SetRuleSet(CodeAnalysisRuleSet));
                    CheckHostObjectSupport(param = nameof(AdditionalFiles), analyzerHostObject.SetAdditionalFiles(AdditionalFiles));
                }

                // For host objects which support them, set analyzer config files and potential analyzer config files
                if (vbcHostObject is IAnalyzerConfigFilesHostObject analyzerConfigFilesHostObject)
                {
                    CheckHostObjectSupport(param = nameof(AnalyzerConfigFiles), analyzerConfigFilesHostObject.SetAnalyzerConfigFiles(AnalyzerConfigFiles));
                    CheckHostObjectSupport(param = nameof(PotentialAnalyzerConfigFiles), analyzerConfigFilesHostObject.SetPotentialAnalyzerConfigFiles(PotentialAnalyzerConfigFiles));
                }

                CheckHostObjectSupport(param = nameof(BaseAddress), vbcHostObject.SetBaseAddress(TargetType, GetBaseAddressInHex()));
                CheckHostObjectSupport(param = nameof(CodePage), vbcHostObject.SetCodePage(CodePage));
                CheckHostObjectSupport(param = nameof(DebugType), vbcHostObject.SetDebugType(EmitDebugInformation, DebugType));
                CheckHostObjectSupport(param = nameof(DefineConstants), vbcHostObject.SetDefineConstants(DefineConstants));
                CheckHostObjectSupport(param = nameof(DelaySign), vbcHostObject.SetDelaySign(DelaySign));
                CheckHostObjectSupport(param = nameof(DocumentationFile), vbcHostObject.SetDocumentationFile(DocumentationFile));
                CheckHostObjectSupport(param = nameof(FileAlignment), vbcHostObject.SetFileAlignment(FileAlignment));
                CheckHostObjectSupport(param = nameof(GenerateDocumentation), vbcHostObject.SetGenerateDocumentation(GenerateDocumentation));
                CheckHostObjectSupport(param = nameof(Imports), vbcHostObject.SetImports(Imports));
                CheckHostObjectSupport(param = nameof(KeyContainer), vbcHostObject.SetKeyContainer(KeyContainer));
                CheckHostObjectSupport(param = nameof(KeyFile), vbcHostObject.SetKeyFile(KeyFile));
                CheckHostObjectSupport(param = nameof(LinkResources), vbcHostObject.SetLinkResources(LinkResources));
                CheckHostObjectSupport(param = nameof(MainEntryPoint), vbcHostObject.SetMainEntryPoint(MainEntryPoint));
                CheckHostObjectSupport(param = nameof(NoConfig), vbcHostObject.SetNoConfig(NoConfig));
                CheckHostObjectSupport(param = nameof(NoStandardLib), vbcHostObject.SetNoStandardLib(NoStandardLib));
                CheckHostObjectSupport(param = nameof(NoWarnings), vbcHostObject.SetNoWarnings(NoWarnings));
                CheckHostObjectSupport(param = nameof(Optimize), vbcHostObject.SetOptimize(Optimize));
                CheckHostObjectSupport(param = nameof(OptionCompare), vbcHostObject.SetOptionCompare(OptionCompare));
                CheckHostObjectSupport(param = nameof(OptionExplicit), vbcHostObject.SetOptionExplicit(OptionExplicit));
                CheckHostObjectSupport(param = nameof(OptionStrict), vbcHostObject.SetOptionStrict(OptionStrict));
                CheckHostObjectSupport(param = nameof(OptionStrictType), vbcHostObject.SetOptionStrictType(OptionStrictType));
                CheckHostObjectSupport(param = nameof(OutputAssembly), vbcHostObject.SetOutputAssembly(OutputAssembly?.ItemSpec));

                // For host objects which support them, set platform with 32BitPreference, HighEntropyVA, and SubsystemVersion
                IVbcHostObject5? vbcHostObject5 = vbcHostObject as IVbcHostObject5;
                if (vbcHostObject5 != null)
                {
                    CheckHostObjectSupport(param = nameof(PlatformWith32BitPreference), vbcHostObject5.SetPlatformWith32BitPreference(PlatformWith32BitPreference));
                    CheckHostObjectSupport(param = nameof(HighEntropyVA), vbcHostObject5.SetHighEntropyVA(HighEntropyVA));
                    CheckHostObjectSupport(param = nameof(SubsystemVersion), vbcHostObject5.SetSubsystemVersion(SubsystemVersion));
                }
                else
                {
                    CheckHostObjectSupport(param = nameof(Platform), vbcHostObject.SetPlatform(Platform));
                }

                IVbcHostObject6? vbcHostObject6 = vbcHostObject as IVbcHostObject6;
                if (vbcHostObject6 != null)
                {
                    CheckHostObjectSupport(param = nameof(ErrorLog), vbcHostObject6.SetErrorLog(ErrorLog));
                    CheckHostObjectSupport(param = nameof(ReportAnalyzer), vbcHostObject6.SetReportAnalyzer(ReportAnalyzer));
                }

                CheckHostObjectSupport(param = nameof(References), vbcHostObject.SetReferences(References));
                CheckHostObjectSupport(param = nameof(RemoveIntegerChecks), vbcHostObject.SetRemoveIntegerChecks(RemoveIntegerChecks));
                CheckHostObjectSupport(param = nameof(Resources), vbcHostObject.SetResources(Resources));
                CheckHostObjectSupport(param = nameof(ResponseFiles), vbcHostObject.SetResponseFiles(ResponseFiles));
                CheckHostObjectSupport(param = nameof(RootNamespace), vbcHostObject.SetRootNamespace(RootNamespace));
                CheckHostObjectSupport(param = nameof(SdkPath), vbcHostObject.SetSdkPath(SdkPath));
                CheckHostObjectSupport(param = nameof(Sources), vbcHostObject.SetSources(Sources));
                CheckHostObjectSupport(param = nameof(TargetCompactFramework), vbcHostObject.SetTargetCompactFramework(TargetCompactFramework));
                CheckHostObjectSupport(param = nameof(TargetType), vbcHostObject.SetTargetType(TargetType));
                CheckHostObjectSupport(param = nameof(TreatWarningsAsErrors), vbcHostObject.SetTreatWarningsAsErrors(TreatWarningsAsErrors));
                CheckHostObjectSupport(param = nameof(WarningsAsErrors), vbcHostObject.SetWarningsAsErrors(WarningsAsErrors));
                CheckHostObjectSupport(param = nameof(WarningsNotAsErrors), vbcHostObject.SetWarningsNotAsErrors(WarningsNotAsErrors));
                // DisabledWarnings needs to come after WarningsAsErrors and WarningsNotAsErrors, because
                // of the way the host object works, and the fact that DisabledWarnings trump Warnings[Not]AsErrors.
                CheckHostObjectSupport(param = nameof(DisabledWarnings), vbcHostObject.SetDisabledWarnings(DisabledWarnings));
                CheckHostObjectSupport(param = nameof(Win32Icon), vbcHostObject.SetWin32Icon(Win32Icon));
                CheckHostObjectSupport(param = nameof(Win32Resource), vbcHostObject.SetWin32Resource(Win32Resource));

                // In order to maintain compatibility with previous host compilers, we must
                // light-up for IVbcHostObject2
                if (vbcHostObject is IVbcHostObject2)
                {
                    IVbcHostObject2 vbcHostObject2 = (IVbcHostObject2)vbcHostObject;
                    CheckHostObjectSupport(param = nameof(ModuleAssemblyName), vbcHostObject2.SetModuleAssemblyName(ModuleAssemblyName));
                    CheckHostObjectSupport(param = nameof(OptionInfer), vbcHostObject2.SetOptionInfer(OptionInfer));
                    CheckHostObjectSupport(param = nameof(Win32Manifest), vbcHostObject2.SetWin32Manifest(GetWin32ManifestSwitch(NoWin32Manifest, Win32Manifest)));
                    // initialize option Infer
                    CheckHostObjectSupport(param = nameof(OptionInfer), vbcHostObject2.SetOptionInfer(OptionInfer));
                }
                else
                {
                    // If we have been given a property that the host compiler doesn't support
                    // then we need to state that we are falling back to the command line compiler
                    if (!String.IsNullOrEmpty(ModuleAssemblyName))
                    {
                        CheckHostObjectSupport(param = nameof(ModuleAssemblyName), resultFromHostObjectSetOperation: false);
                    }

                    if (_store.ContainsKey(nameof(OptionInfer)))
                    {
                        CheckHostObjectSupport(param = nameof(OptionInfer), resultFromHostObjectSetOperation: false);
                    }

                    if (!String.IsNullOrEmpty(Win32Manifest))
                    {
                        CheckHostObjectSupport(param = nameof(Win32Manifest), resultFromHostObjectSetOperation: false);
                    }
                }

                // Check for support of the LangVersion property
                if (vbcHostObject is IVbcHostObject3 && !DeferToICompilerOptionsHostObject(LangVersion, vbcHostObject))
                {
                    IVbcHostObject3 vbcHostObject3 = (IVbcHostObject3)vbcHostObject;
                    CheckHostObjectSupport(param = nameof(LangVersion), vbcHostObject3.SetLanguageVersion(LangVersion));
                }
                else if (!String.IsNullOrEmpty(LangVersion) && !UsedCommandLineTool)
                {
                    CheckHostObjectSupport(param = nameof(LangVersion), resultFromHostObjectSetOperation: false);
                }

                if (vbcHostObject is IVbcHostObject4)
                {
                    IVbcHostObject4 vbcHostObject4 = (IVbcHostObject4)vbcHostObject;
                    CheckHostObjectSupport(param = nameof(VBRuntime), vbcHostObject4.SetVBRuntime(VBRuntime));
                }
                // Support for NoVBRuntimeReference was added to this task after IVbcHostObject was frozen. That doesn't matter much because the host
                // compiler doesn't support it, and almost nobody uses it anyway. But if someone has set it, we need to hard code falling back to
                // the command line compiler here.
                if (NoVBRuntimeReference)
                {
                    CheckHostObjectSupport(param = nameof(NoVBRuntimeReference), resultFromHostObjectSetOperation: false);
                }

                InitializeHostObjectSupportForNewSwitches(vbcHostObject, ref param);

                // In general, we don't support preferreduilang with the in-proc compiler.  It will always use the same locale as the
                // host process, so in general, we have to fall back to the command line compiler if this option is specified.
                // However, we explicitly allow two values (mostly for parity with C#):
                // Null is supported because it means that option should be omitted, and compiler default used - obviously always valid.
                // Explicitly specified name of current locale is also supported, since it is effectively a no-op.
                if (!String.IsNullOrEmpty(PreferredUILang) && !String.Equals(PreferredUILang, System.Globalization.CultureInfo.CurrentUICulture.Name, StringComparison.OrdinalIgnoreCase))
                {
                    CheckHostObjectSupport(param = nameof(PreferredUILang), resultFromHostObjectSetOperation: false);
                }
            }
            catch (Exception e)
            {
                Log.LogErrorWithCodeFromResources("General_CouldNotSetHostObjectParameter", param, e.Message);
                return false;
            }
            finally
            {
                // In the case of the VB host compiler, the EndInitialization method will
                // throw (due to FAILED HRESULT) if there was a bad value for one of the
                // parameters.
                vbcHostObject.EndInitialization();
            }

            return true;
        }

        // VbcHostObject doesn't support VB versions beyond 15,
        // so the LangVersion will be passed through ICompilerOptionsHostObject.SetCompilerOptions instead
        private static bool DeferToICompilerOptionsHostObject(string? langVersion, IVbcHostObject vbcHostObject)
        {
            if (!(vbcHostObject is ICompilerOptionsHostObject))
            {
                return false;
            }

            if (langVersion == null)
            {
                // CVbcMSBuildHostObject::SetLanguageVersion can handle null
                return false;
            }

            // CVbcMSBuildHostObject::SetLanguageVersion can handle versions up to 15
            var supportedList = new[]
            {
                "9", "9.0",
                "10", "10.0",
                "11", "11.0",
                "12", "12.0",
                "14", "14.0",
                "15", "15.0"
            };

            return Array.IndexOf(supportedList, langVersion) < 0;
        }

        /// <summary>
        /// This method will get called during Execute() if a host object has been passed into the Vbc
        /// task.  Returns one of the following values to indicate what the next action should be:
        ///     UseHostObjectToExecute          Host compiler exists and was initialized.
        ///     UseAlternateToolToExecute       Host compiler doesn't exist or was not appropriate.
        ///     NoActionReturnSuccess           Host compiler was already up-to-date, and we're done.
        ///     NoActionReturnFailure           Bad parameters were passed into the task.
        /// </summary>
        protected override HostObjectInitializationStatus InitializeHostObject()
        {
            if (this.HostObject != null)
            {
                // When the host object was passed into the task, it was passed in as a generic
                // "Object" (because ITask interface obviously can't have any Vbc-specific stuff
                // in it, and each task is going to want to communicate with its host in a unique
                // way).  Now we cast it to the specific type that the Vbc task expects.  If the
                // host object does not match this type, the host passed in an invalid host object
                // to Vbc, and we error out.

                // NOTE: For compat reasons this must remain IVbcHostObject
                // we can dynamically test for smarter interfaces later..
                if (HostObject is IVbcHostObject hostObjectCOM)
                {
                    using (RCWForCurrentContext<IVbcHostObject> hostObject = new RCWForCurrentContext<IVbcHostObject>(hostObjectCOM))
                    {
                        IVbcHostObject vbcHostObject = hostObject.RCW;
                        bool hostObjectSuccessfullyInitialized = InitializeHostCompiler(vbcHostObject);

                        // If we're currently only in design-time (as opposed to build-time),
                        // then we're done.  We've initialized the host compiler as best we
                        // can, and we certainly don't want to actually do the final compile.
                        // So return true, saying we're done and successful.
                        if (vbcHostObject.IsDesignTime())
                        {
                            // If we are design-time then we do not want to continue the build at 
                            // this time.
                            return hostObjectSuccessfullyInitialized ?
                                HostObjectInitializationStatus.NoActionReturnSuccess :
                                HostObjectInitializationStatus.NoActionReturnFailure;
                        }

                        if (!this.HostCompilerSupportsAllParameters)
                        {
                            // Since the host compiler has refused to take on the responsibility for this compilation,
                            // we're about to shell out to the command-line compiler to handle it.  If some of the
                            // references don't exist on disk, we know the command-line compiler will fail, so save
                            // the trouble, and just throw a consistent error ourselves.  This allows us to give
                            // more information than the compiler would, and also make things consistent across
                            // Vbc / Csc / etc.  Actually, the real reason is bug 275726 (ddsuites\src\vs\env\vsproject\refs\ptp3).
                            // This suite behaves differently in localized builds than on English builds because 
                            // VBC.EXE doesn't localize the word "error" when they emit errors and so we can't scan for it.
                            if (!CheckAllReferencesExistOnDisk())
                            {
                                return HostObjectInitializationStatus.NoActionReturnFailure;
                            }

                            // The host compiler doesn't support some of the switches/parameters
                            // being passed to it.  Therefore, we resort to using the command-line compiler
                            // in this case.
                            UsedCommandLineTool = true;
                            return HostObjectInitializationStatus.UseAlternateToolToExecute;
                        }

                        // Ok, by now we validated that the host object supports the necessary switches
                        // and parameters.  Last thing to check is whether the host object is up to date,
                        // and in that case, we will inform the caller that no further action is necessary.
                        if (hostObjectSuccessfullyInitialized)
                        {
                            return vbcHostObject.IsUpToDate() ?
                                HostObjectInitializationStatus.NoActionReturnSuccess :
                                HostObjectInitializationStatus.UseHostObjectToExecute;
                        }
                        else
                        {
                            return HostObjectInitializationStatus.NoActionReturnFailure;
                        }
                    }
                }
                else
                {
                    Log.LogErrorWithCodeFromResources("General_IncorrectHostObject", "Vbc", "IVbcHostObject");
                }
            }

            // No appropriate host object was found.
            UsedCommandLineTool = true;
            return HostObjectInitializationStatus.UseAlternateToolToExecute;
        }

        /// <summary>
        /// This method will get called during Execute() if a host object has been passed into the Vbc
        /// task.  Returns true if an appropriate host object was found, it was called to do the compile,
        /// and the compile succeeded.  Otherwise, we return false.
        /// </summary>
        protected override bool CallHostObjectToExecute()
        {
            Debug.Assert(this.HostObject != null, "We should not be here if the host object has not been set.");

            IVbcHostObject? vbcHostObject = this.HostObject as IVbcHostObject;
            RoslynDebug.Assert(vbcHostObject != null, "Wrong kind of host object passed in!");

            IVbcHostObject5? vbcHostObject5 = vbcHostObject as IVbcHostObject5;
            Debug.Assert(vbcHostObject5 != null, "Wrong kind of host object passed in!");

            // IVbcHostObjectFreeThreaded::Compile is the preferred way to compile the host object
            // because while it is still synchronous it does its waiting on our BG thread 
            // (as opposed to the UI thread for IVbcHostObject::Compile)
            if (vbcHostObject5 != null)
            {
                IVbcHostObjectFreeThreaded freeThreadedHostObject = vbcHostObject5.GetFreeThreadedHostObject();
                return freeThreadedHostObject.Compile();
            }
            else
            {
                // If for some reason we can't get to IVbcHostObject5 we just fall back to the old
                // Compile method. This method unfortunately allows for reentrancy on the UI thread.
                return vbcHostObject.Compile();
            }
        }

        /// <summary>
        /// private class that just holds together name, value pair for the vbErrorLines Queue
        /// </summary>
        private class VBError
        {
            public string Message { get; }
            public MessageImportance MessageImportance { get; }

            public VBError(string message, MessageImportance importance)
            {
                this.Message = message;
                this.MessageImportance = importance;
            }
        }
    }
}
