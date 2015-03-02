﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Build.Tasks.Hosting;

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
        private bool _useHostCompilerIfAvailable = false;

        // The following 1 fields are used, set and re-set in LogEventsFromTextOutput()
        /// <summary>
        /// This stores the origional lines and error priority together in the order in which they were recieved.
        /// </summary>
        private Queue<VBError> _vbErrorLines = new Queue<VBError>();

        // Used when parsing vbc output to determine the column number of an error
        private bool _isDoneOutputtingErrorMessage = false;
        private int _numberOfLinesInErrorMessage = 0;

        #region Properties

        // Please keep these alphabetized.  These are the parameters specific to Vbc.  The
        // ones shared between Vbc and Csc are defined in ManagedCompiler.cs, which is
        // the base class.

        public string BaseAddress
        {
            set { _store["BaseAddress"] = value; }
            get { return (string)_store["BaseAddress"]; }
        }

        public string DisabledWarnings
        {
            set { _store["DisabledWarnings"] = value; }
            get { return (string)_store["DisabledWarnings"]; }
        }

        public string DocumentationFile
        {
            set { _store["DocumentationFile"] = value; }
            get { return (string)_store["DocumentationFile"]; }
        }

        public string ErrorReport
        {
            set { _store["ErrorReport"] = value; }
            get { return (string)_store["ErrorReport"]; }
        }

        public bool GenerateDocumentation
        {
            set { _store["GenerateDocumentation"] = value; }
            get { return _store.GetOrDefault("GenerateDocumentation", false); }
        }

        public ITaskItem[] Imports
        {
            set { _store["Imports"] = value; }
            get { return (ITaskItem[])_store["Imports"]; }
        }

        public string LangVersion
        {
            set { _store["LangVersion"] = value; }
            get { return (string)_store["LangVersion"]; }
        }

        public string ModuleAssemblyName
        {
            set { _store["ModuleAssemblyName"] = value; }
            get { return (string)_store["ModuleAssemblyName"]; }
        }

        public bool NoStandardLib
        {
            set { _store["NoStandardLib"] = value; }
            get { return _store.GetOrDefault("NoStandardLib", false); }
        }

        // This is not a documented switch. It prevents the automatic reference to Microsoft.VisualBasic.dll.
        // The VB team believes the only scenario for this is when you are building that assembly itself.
        // We have to support the switch here so that we can build the SDE and VB trees, which need to build this assembly.
        // Although undocumented, it cannot be wrapped with #if BUILDING_DF_LKG because this would prevent dogfood builds
        // within VS, which must use non-LKG msbuild bits.
        public bool NoVBRuntimeReference
        {
            set { _store["NoVBRuntimeReference"] = value; }
            get { return _store.GetOrDefault("NoVBRuntimeReference", false); }
        }

        public bool NoWarnings
        {
            set { _store["NoWarnings"] = value; }
            get { return _store.GetOrDefault("NoWarnings", false); }
        }

        public string OptionCompare
        {
            set { _store["OptionCompare"] = value; }
            get { return (string)_store["OptionCompare"]; }
        }

        public bool OptionExplicit
        {
            set { _store["OptionExplicit"] = value; }
            get { return _store.GetOrDefault("OptionExplicit", true); }
        }

        public bool OptionStrict
        {
            set { _store["OptionStrict"] = value; }
            get { return _store.GetOrDefault("OptionStrict", false); }
        }

        public bool OptionInfer
        {
            set { _store["OptionInfer"] = value; }
            get { return _store.GetOrDefault("OptionInfer", false); }
        }

        // Currently only /optionstrict:custom
        public string OptionStrictType
        {
            set { _store["OptionStrictType"] = value; }
            get { return (string)_store["OptionStrictType"]; }
        }

        public bool RemoveIntegerChecks
        {
            set { _store["RemoveIntegerChecks"] = value; }
            get { return _store.GetOrDefault("RemoveIntegerChecks", false); }
        }

        public string RootNamespace
        {
            set { _store["RootNamespace"] = value; }
            get { return (string)_store["RootNamespace"]; }
        }

        public string SdkPath
        {
            set { _store["SdkPath"] = value; }
            get { return (string)_store["SdkPath"]; }
        }

        /// <summary>
        /// Name of the language passed to "/preferreduilang" compiler option.
        /// </summary>
        /// <remarks>
        /// If set to null, "/preferreduilang" option is omitted, and vbc.exe uses its default setting.
        /// Otherwise, the value is passed to "/preferreduilang" as is.
        /// </remarks>
        public string PreferredUILang
        {
            set { _store["PreferredUILang"] = value; }
            get { return (string)_store["PreferredUILang"]; }
        }

        public string VsSessionGuid
        {
            set { _store["VsSessionGuid"] = value; }
            get { return (string)_store["VsSessionGuid"]; }
        }

        public bool TargetCompactFramework
        {
            set { _store["TargetCompactFramework"] = value; }
            get { return _store.GetOrDefault("TargetCompactFramework", false); }
        }

        public bool UseHostCompilerIfAvailable
        {
            set { _useHostCompilerIfAvailable = value; }
            get { return _useHostCompilerIfAvailable; }
        }

        public string VBRuntimePath
        {
            set { _store["VBRuntimePath"] = value; }
            get { return (string)_store["VBRuntimePath"]; }
        }

        public string Verbosity
        {
            set { _store["Verbosity"] = value; }
            get { return (string)_store["Verbosity"]; }
        }

        public string WarningsAsErrors
        {
            set { _store["WarningsAsErrors"] = value; }
            get { return (string)_store["WarningsAsErrors"]; }
        }

        public string WarningsNotAsErrors
        {
            set { _store["WarningsNotAsErrors"] = value; }
            get { return (string)_store["WarningsNotAsErrors"]; }
        }

        public string VBRuntime
        {
            set { _store["VBRuntime"] = value; }
            get { return (string)_store["VBRuntime"]; }
        }

        public string PdbFile
        {
            set { _store["PdbFile"] = value; }
            get { return (string)_store["PdbFile"]; }
        }
        #endregion

        #region Tool Members

        /// <summary>
        ///  Return the name of the tool to execute.
        /// </summary>
        override protected string ToolName
        {
            get
            {
                return "vbc2.exe";
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

            MovePdbFileIfNecessary(OutputAssembly.ItemSpec);

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
        internal void MovePdbFileIfNecessary(string outputAssembly)
        {
            // Get the name of the output assembly because the pdb will be written beside it and will have the same name
            if (String.IsNullOrEmpty(PdbFile) || String.IsNullOrEmpty(outputAssembly))
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
                Log.LogErrorWithCodeFromResources("VBC.RenamePDB", PdbFile, e.Message);
            }
        }

        /// <summary>
        /// Generate the path to the tool
        /// </summary>
        override protected string GenerateFullPathToTool()
        {
            string pathToTool = ToolLocationHelper.GetPathToBuildToolsFile(ToolName, ToolLocationHelper.CurrentToolsVersion);

            if (null == pathToTool)
            {
                pathToTool = ToolLocationHelper.GetPathToDotNetFrameworkFile(ToolName, TargetDotNetFrameworkVersion.VersionLatest);

                if (null == pathToTool)
                {
                    Log.LogErrorWithCodeFromResources("General.FrameworksFileNotFound", ToolName, ToolLocationHelper.GetDotNetFrameworkVersionFolderPrefix(TargetDotNetFrameworkVersion.VersionLatest));
                }
            }

            return pathToTool;
        }

        /// <summary>
        /// vbc.exe only takes the BaseAddress in hexadecimal format.  But we allow the caller
        /// of the task to pass in the BaseAddress in either decimal or hexadecimal format.
        /// Examples of supported hex formats include "0x10000000" or "&amp;H10000000".
        /// </summary>
        internal string GetBaseAddressInHex()
        {
            string originalBaseAddress = this.BaseAddress;

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
        /// <param name="commandLine"></param>
        /// <owner>RGoel, JomoF</owner>
        protected internal override void AddResponseFileCommands(CommandLineBuilderExtension commandLine)
        {
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
            object optionStrictSetting = this._store["OptionStrict"];
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
            commandLine.AppendPlusOrMinusSwitch("/optioninfer", this._store, "OptionInfer");
            commandLine.AppendWhenTrue("/nostdlib", this._store, "NoStandardLib");
            commandLine.AppendWhenTrue("/novbruntimeref", this._store, "NoVBRuntimeReference");
            commandLine.AppendSwitchIfNotNull("/errorreport:", this.ErrorReport);
            commandLine.AppendSwitchIfNotNull("/platform:", this.PlatformWith32BitPreference);
            commandLine.AppendPlusOrMinusSwitch("/removeintchecks", this._store, "RemoveIntegerChecks");
            commandLine.AppendSwitchIfNotNull("/rootnamespace:", this.RootNamespace);
            commandLine.AppendSwitchIfNotNull("/sdkpath:", this.SdkPath);
            commandLine.AppendSwitchIfNotNull("/langversion:", this.LangVersion);
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

            commandLine.AppendSwitchIfNotNull("/doc:", this.DocumentationFile);
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
            if (this.HostObject != null)
            {
                var vbHost = this.HostObject as IVbcHostObject;
                designTime = vbHost.IsDesignTime();
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
        override protected bool ValidateParameters()
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
                    Log.LogErrorWithCodeFromResources("Vbc.EnumParameterHasInvalidValue", "Verbosity", this.Verbosity, "Quiet, Normal, Verbose");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// This method intercepts the lines to be logged coming from STDOUT from VBC.
        /// Once we see a standard vb warning or error, then we capture it and grab the next 3
        /// lines so we can transform the string form the form of FileName.vb(line) to FileName.vb(line,column)
        /// which will allow us to report the line and column to the IDE, and thus filter the error
        /// in the duplicate case for multi-targeting, or just squiggle the appropriate token 
        /// instead of the entire line.
        /// </summary>
        /// <param name="singleLine">A single line from the STDOUT of the vbc compiler</param>
        /// <param name="messageImportance">High,Low,Normal</param>
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
        /// <comments>
        /// INTERNAL FOR UNITTESTING ONLY
        /// </comments>
        /// <param name="singleLine">The line to parse</param>
        /// <param name="messageImportance">The MessageImportance to use when reporting the error.</param>
        internal void ParseVBErrorOrWarning(string singleLine, MessageImportance messageImportance)
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
                    int endParenthesisLocation = originalVBErrorString.IndexOf(')');

                    // If for some reason the line does not contain any ~ then something went wrong
                    // so abort and return the origional string.
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

                    string newLine = null;
                    newLine = originalVBErrorString.Substring(0, endParenthesisLocation) + "," + column + originalVBErrorString.Substring(endParenthesisLocation);

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
                CanonicalError.Parts parts = CanonicalError.Parse(singleLine);
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
        /// task to contain quotes, because if we weren't careful, we might accidently
        /// allow a parameter injection attach.  But for "DefineConstants", we have to allow
        /// it.
        /// So this method prepares the string to be passed in on the /define: command-line
        /// switch.  It does that by quoting the entire string, and escaping the embedded
        /// quotes.
        /// </summary>
        internal static string GetDefineConstantsSwitch
            (
            string originalDefineConstants
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
        /// private class that just holds together name, value pair for the vbErrorLines Queue
        /// </summary>
        private class VBError
        {
            public string Message { get; set; }
            public MessageImportance MessageImportance { get; set; }

            public VBError(string message, MessageImportance importance)
            {
                this.Message = message;
                this.MessageImportance = importance;
            }
        }
    }
}