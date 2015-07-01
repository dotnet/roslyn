// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Runtime.InteropServices;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Information that describes how a method from the underlying Platform is to be invoked.
    /// </summary>
    public sealed class DllImportData : Cci.IPlatformInvokeInformation
    {
        private readonly string _moduleName;
        private readonly string _entryPointName;            // null if unspecified, the name of the target method should be used
        private readonly Cci.PInvokeAttributes _flags;

        internal DllImportData(string moduleName, string entryPointName, Cci.PInvokeAttributes flags)
        {
            _moduleName = moduleName;
            _entryPointName = entryPointName;
            _flags = flags;
        }

        /// <summary>
        /// Module name. Null if value specified in the attribute is not valid.
        /// </summary>
        public string ModuleName
        {
            get { return _moduleName; }
        }

        /// <summary>
        /// Name of the native entry point or null if not specified (the effective name is the same as the name of the target method).
        /// </summary>
        public string EntryPointName
        {
            get { return _entryPointName; }
        }

        Cci.PInvokeAttributes Cci.IPlatformInvokeInformation.Flags
        {
            get { return _flags; }
        }

        /// <summary>
        /// Controls whether the <see cref="CharacterSet"/> field causes the common language runtime 
        /// to search an unmanaged DLL for entry-point names other than the one specified.
        /// </summary>
        public bool ExactSpelling
        {
            get
            {
                return (_flags & Cci.PInvokeAttributes.NoMangle) != 0;
            }
        }

        /// <summary>
        /// Indicates how to marshal string parameters and controls name mangling.
        /// </summary>
        public CharSet CharacterSet
        {
            get
            {
                switch (_flags & Cci.PInvokeAttributes.CharSetMask)
                {
                    case Cci.PInvokeAttributes.CharSetAnsi:
                        return CharSet.Ansi;

                    case Cci.PInvokeAttributes.CharSetUnicode:
                        return CharSet.Unicode;

                    case Cci.PInvokeAttributes.CharSetAuto:
                        return Cci.Constants.CharSet_Auto;

                    case 0:
                        return Cci.Constants.CharSet_None;
                }

                throw ExceptionUtilities.UnexpectedValue(_flags);
            }
        }

        /// <summary>
        /// Indicates whether the callee calls the SetLastError Win32 API function before returning from the attributed method.
        /// </summary>
        public bool SetLastError
        {
            get
            {
                return (_flags & Cci.PInvokeAttributes.SupportsLastError) != 0;
            }
        }

        /// <summary>
        /// Indicates the calling convention of an entry point.
        /// </summary>
        public CallingConvention CallingConvention
        {
            get
            {
                switch (_flags & Cci.PInvokeAttributes.CallConvMask)
                {
                    default:
                        return CallingConvention.Winapi;

                    case Cci.PInvokeAttributes.CallConvCdecl:
                        return CallingConvention.Cdecl;

                    case Cci.PInvokeAttributes.CallConvStdcall:
                        return CallingConvention.StdCall;

                    case Cci.PInvokeAttributes.CallConvThiscall:
                        return CallingConvention.ThisCall;

                    case Cci.PInvokeAttributes.CallConvFastcall:
                        return Cci.Constants.CallingConvention_FastCall;
                }
            }
        }

        /// <summary>
        /// Enables or disables best-fit mapping behavior when converting Unicode characters to ANSI characters.
        /// Null if not specified (the setting for the containing type or assembly should be used, <see cref="BestFitMappingAttribute"/>).
        /// </summary>
        public bool? BestFitMapping
        {
            get
            {
                switch (_flags & Cci.PInvokeAttributes.BestFitMask)
                {
                    case Cci.PInvokeAttributes.BestFitEnabled:
                        return true;

                    case Cci.PInvokeAttributes.BestFitDisabled:
                        return false;

                    default:
                        return null;
                }
            }
        }

        /// <summary>
        /// Enables or disables the throwing of an exception on an unmappable Unicode character that is converted to an ANSI "?" character.
        /// Null if not specified.
        /// </summary>
        public bool? ThrowOnUnmappableCharacter
        {
            get
            {
                switch (_flags & Cci.PInvokeAttributes.ThrowOnUnmappableCharMask)
                {
                    case Cci.PInvokeAttributes.ThrowOnUnmappableCharEnabled:
                        return true;

                    case Cci.PInvokeAttributes.ThrowOnUnmappableCharDisabled:
                        return false;

                    default:
                        return null;
                }
            }
        }

        internal static Cci.PInvokeAttributes MakeFlags(bool noMangle, CharSet charSet, bool setLastError, CallingConvention callingConvention, bool? useBestFit, bool? throwOnUnmappable)
        {
            Cci.PInvokeAttributes result = 0;
            if (noMangle)
            {
                result |= Cci.PInvokeAttributes.NoMangle;
            }

            switch (charSet)
            {
                case CharSet.Ansi:
                    result |= Cci.PInvokeAttributes.CharSetAnsi;
                    break;

                case CharSet.Unicode:
                    result |= Cci.PInvokeAttributes.CharSetUnicode;
                    break;

                case Cci.Constants.CharSet_Auto:
                    result |= Cci.PInvokeAttributes.CharSetAuto;
                    break;

                    // Dev10: use default without reporting an error
            }

            if (setLastError)
            {
                result |= Cci.PInvokeAttributes.SupportsLastError;
            }

            switch (callingConvention)
            {
                default: // Dev10: uses default without reporting an error
                    result |= Cci.PInvokeAttributes.CallConvWinapi;
                    break;

                case CallingConvention.Cdecl:
                    result |= Cci.PInvokeAttributes.CallConvCdecl;
                    break;

                case CallingConvention.StdCall:
                    result |= Cci.PInvokeAttributes.CallConvStdcall;
                    break;

                case CallingConvention.ThisCall:
                    result |= Cci.PInvokeAttributes.CallConvThiscall;
                    break;

                case Cci.Constants.CallingConvention_FastCall:
                    result |= Cci.PInvokeAttributes.CallConvFastcall;
                    break;
            }

            if (throwOnUnmappable.HasValue)
            {
                if (throwOnUnmappable.Value)
                {
                    result |= Cci.PInvokeAttributes.ThrowOnUnmappableCharEnabled;
                }
                else
                {
                    result |= Cci.PInvokeAttributes.ThrowOnUnmappableCharDisabled;
                }
            }

            if (useBestFit.HasValue)
            {
                if (useBestFit.Value)
                {
                    result |= Cci.PInvokeAttributes.BestFitEnabled;
                }
                else
                {
                    result |= Cci.PInvokeAttributes.BestFitDisabled;
                }
            }

            return result;
        }
    }
}
