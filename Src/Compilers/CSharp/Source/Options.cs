// ==++==
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--==

using System;
using System.Collections.Generic;

namespace Roslyn.Compilers.CSharp
{
    // this is a stand-in for compiler options

    public enum CompatibilityMode
    {
        None = 0,
        ECMA1 = 1,  /* "ISO-1" */
        ECMA2 = 2,  /* "ISO-2" */
    }

    public enum LanguageVersion
    {
        CSharp1 = 1,
        CSharp2 = 2,
        CSharp3 = 3,
        CSharp4 = 4,

        /// <summary>
        /// Features: async.
        /// </summary>
        CSharp5 = 5,

        /// <summary>
        /// Features: using of a static class.
        /// </summary>
        CSharp6 = 6,
    }

    internal static partial class EnumBounds
    {
        internal static bool IsValid(this CompatibilityMode value)
        {
            return value >= CompatibilityMode.None && value <= CompatibilityMode.ECMA2;
        }

        internal static bool IsValid(this LanguageVersion value)
        {
            return value >= LanguageVersion.CSharp1 && value <= LanguageVersion.CSharp6;
        }
    }

    internal static class OptionsValidator
    {
        internal static bool IsValidFullName(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            char lastChar = '.';
            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '.')
                {
                    if (lastChar == '.')
                    {
                        return false;
                    }
                }
                else if (!(lastChar == '.' ? CharacterInfo.IsIdentifierStartChar(name[i]) : CharacterInfo.IsIdentifierPartChar(name[i])))
                {
                    return false;
                }

                lastChar = name[i];
            }

            return lastChar != '.';
        }
    }

    public sealed class ParseOptions : IParseOptions
    {
        public static readonly ParseOptions Default = new ParseOptions();

        public CompatibilityMode Compatibility { get; private set; }
        public LanguageVersion LanguageVersion { get; private set; }
        public ReadOnlyArray<string> PreprocessorSymbols { get; private set; }
        public bool SuppressDocumentationCommentParse { get; private set; }
        public SourceCodeKind Kind { get; private set; }

        //warnaserror[+|-]
        //warnaserror[+|-]:<warn list>
        //unsafe[+|-]
        //warn:<n>
        //nowarn:<warn list>

        public ParseOptions(
            CompatibilityMode compatibility = CompatibilityMode.None,
            LanguageVersion languageVersion = CSharp.LanguageVersion.CSharp4,
            IEnumerable<string> preprocessorSymbols = null,
            bool suppressDocumentationCommentParse = false,
            SourceCodeKind kind = SourceCodeKind.Regular)
        {
            if (!compatibility.IsValid())
            {
                throw new ArgumentOutOfRangeException("compatibility");
            }
            if (!languageVersion.IsValid())
            {
                throw new ArgumentOutOfRangeException("languageVersion");
            }
            if (!kind.IsValid())
            {
                throw new ArgumentOutOfRangeException("kind");
            }

            this.Compatibility = compatibility;
            this.LanguageVersion = languageVersion;
            this.PreprocessorSymbols = preprocessorSymbols.AsReadOnlyOrEmpty();
            this.SuppressDocumentationCommentParse = suppressDocumentationCommentParse;
            this.Kind = kind;
        }

        public ParseOptions Copy(
            CompatibilityMode? compatibility = null,
            LanguageVersion? languageVersion = null,
            IEnumerable<string> preprocessorSymbols = null,
            bool? suppressDocumentationCommentParse = null,
            SourceCodeKind? kind = SourceCodeKind.Regular)
        {
            return new ParseOptions(
                compatibility ?? this.Compatibility,
                languageVersion ?? this.LanguageVersion,
                preprocessorSymbols ?? this.PreprocessorSymbols.AsEnumerable(),
                suppressDocumentationCommentParse ?? this.SuppressDocumentationCommentParse,
                kind ?? this.Kind
            );
        }

        #region IParseOptions Members

        SourceCodeKind IParseOptions.Kind
        {
            get { return this.Kind; }
        }

        #endregion
    }

    public sealed class CompilationOptions : ICompilationOptions
    {
        public static readonly CompilationOptions Default = new CompilationOptions();
        public const string AnyMain = "*";

        /// <summary>
        /// The full name of a global implicit class (script class). This class implicitly encapsulates top-level statements, 
        /// type declarations, and member declarations. Could be a namespace qualified name.
        /// </summary>
        public string ScriptClassName { get; private set; }

        /// <summary>
        /// The full name of a type that declares static Main method. Must be a valid non-generic namespace qualified name.
        /// </summary>
        public string MainTypeName { get; private set; }

        public AssemblyKind AssemblyKind { get; private set; }
        public bool Optimize { get; private set; }
        public bool CheckOverflow { get; private set; }

        public bool IsNetModule { get; private set; }

        // TODO:
        // platform:<string>
        // baseaddress:<address>
        // filealign:<n>

        public CompilationOptions(
            string mainTypeName = AnyMain,
            string scriptClassName = null,
            AssemblyKind assemblyKind = AssemblyKind.ConsoleApplication,
            bool optimize = false,
            bool checkOverflow = false,
            bool isNetModule = false)
        {
            if (mainTypeName != null && mainTypeName != AnyMain && !OptionsValidator.IsValidFullName(mainTypeName))
            {
                throw new ArgumentException("Invalid type name", "mainTypeName");
            }
            if (scriptClassName != null && !OptionsValidator.IsValidFullName(scriptClassName))
            {
                throw new ArgumentException("Invalid type name", "scriptClassName");
            }
            if (!assemblyKind.IsValid())
            {
                throw new ArgumentOutOfRangeException("assemblyKind");
            }

            ScriptClassName = scriptClassName ?? "Script";
            MainTypeName = mainTypeName ?? AnyMain;
            AssemblyKind = assemblyKind;
            Optimize = optimize;
            CheckOverflow = checkOverflow;
            IsNetModule = isNetModule;
        }

        public CompilationOptions Copy(
            string mainTypeName = null,
            string globalTypeName = null,
            AssemblyKind? assemblyKind = null,
            bool? optimize = null,
            bool? checkOverflow = null,
            bool? isNetModule = null)
        {
            return new CompilationOptions(
                mainTypeName ?? this.MainTypeName,
                globalTypeName ?? this.ScriptClassName,
                assemblyKind ?? this.AssemblyKind,
                optimize ?? this.Optimize,
                checkOverflow ?? this.CheckOverflow,
                isNetModule ?? this.IsNetModule
            );
        }

        #region ICompilationOptions Members

        bool ICompilationOptions.IsNetModule
        {
            get { return this.IsNetModule; }
        }

        AssemblyKind ICompilationOptions.AssemblyKind
        {
            get { return this.AssemblyKind; }
        }

        #endregion
    }
}