// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis.Text;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents subsystem version, see /subsystemversion command line 
    /// option for details and valid values.
    /// 
    /// The following table lists common subsystem versions of Windows.
    /// 
    /// Windows version             Subsystem version
    ///   - Windows 2000                5.00
    ///   - Windows XP                  5.01
    ///   - Windows Vista               6.00
    ///   - Windows 7                   6.01
    ///   - Windows 8 Release Preview   6.02
    /// </summary>
    public struct SubsystemVersion : IEquatable<SubsystemVersion>
    {
        /// <summary>
        /// Major subsystem version
        /// </summary>
        public int Major { get; }

        /// <summary>
        /// Minor subsystem version
        /// </summary>
        public int Minor { get; }

        /// <summary>
        /// Subsystem version not specified
        /// </summary>
        public static SubsystemVersion None => new SubsystemVersion();

        /// <summary>
        /// Subsystem version: Windows 2000
        /// </summary>
        public static SubsystemVersion Windows2000 => new SubsystemVersion(5, 0);

        /// <summary>
        /// Subsystem version: Windows XP 
        /// </summary>
        public static SubsystemVersion WindowsXP => new SubsystemVersion(5, 1);

        /// <summary>
        /// Subsystem version: Windows Vista
        /// </summary>
        public static SubsystemVersion WindowsVista => new SubsystemVersion(6, 0);

        /// <summary>
        /// Subsystem version: Windows 7
        /// </summary>
        public static SubsystemVersion Windows7 => new SubsystemVersion(6, 1);

        /// <summary>
        /// Subsystem version: Windows 8
        /// </summary>
        public static SubsystemVersion Windows8 => new SubsystemVersion(6, 2);

        private SubsystemVersion(int major, int minor)
        {
            this.Major = major;
            this.Minor = minor;
        }

        /// <summary>
        /// Try parse subsystem version in "x.y" format. Note, no spaces are allowed in string representation.
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="version">the value if successfully parsed or None otherwise</param>
        /// <returns>true if parsed successfully, false otherwise</returns>
        public static bool TryParse(string str, out SubsystemVersion version)
        {
            version = SubsystemVersion.None;
            if (!string.IsNullOrWhiteSpace(str))
            {
                string major;
                string minor;

                int index = str.IndexOf('.');

                //found a dot
                if (index >= 0)
                {
                    //if there's a dot and no following digits, it's an error in the native compiler.
                    if (str.Length == index + 1)
                        return false;

                    major = str.Substring(0, index);
                    minor = str.Substring(index + 1);
                }
                else
                {
                    major = str;
                    minor = null;
                }

                int majorValue;

                if (major != major.Trim() ||
                    !int.TryParse(major, NumberStyles.None, CultureInfo.InvariantCulture, out majorValue) ||
                    majorValue >= 65356 || majorValue < 0)
                {
                    return false;
                }

                int minorValue = 0;

                //it's fine to have just a single number specified for the subsystem.

                if (minor != null)
                {
                    if (minor != minor.Trim() ||
                        !int.TryParse(minor, NumberStyles.None, CultureInfo.InvariantCulture, out minorValue) ||
                        minorValue >= 65356 || minorValue < 0)
                    {
                        return false;
                    }
                }

                version = new SubsystemVersion(majorValue, minorValue);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Create a new instance of subsystem version with specified major and minor values.
        /// </summary>
        /// <param name="major">major subsystem version</param>
        /// <param name="minor">minor subsystem version</param>
        /// <returns>subsystem version with provided major and minor</returns>
        public static SubsystemVersion Create(int major, int minor)
        {
            return new SubsystemVersion(major, minor);
        }

        /// <summary>
        /// Subsystem version default for the specified output kind and platform combination
        /// </summary>
        /// <param name="outputKind">Output kind</param>
        /// <param name="platform">Platform</param>
        /// <returns>Subsystem version</returns>
        internal static SubsystemVersion Default(OutputKind outputKind, Platform platform)
        {
            if (platform == Platform.Arm)
                return Windows8;

            switch (outputKind)
            {
                case OutputKind.ConsoleApplication:
                case OutputKind.DynamicallyLinkedLibrary:
                case OutputKind.NetModule:
                case OutputKind.WindowsApplication:
                    return new SubsystemVersion(4, 0);
                case OutputKind.WindowsRuntimeApplication:
                case OutputKind.WindowsRuntimeMetadata:
                    return Windows8;

                default:
                    throw new ArgumentOutOfRangeException(CodeAnalysisResources.OutputKindNotSupported, "outputKind");
            }
        }

        /// <summary>
        /// True if the subsystem version has a valid value
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this.Major >= 0 &&
                       this.Minor >= 0 &&
                       this.Major < 65536 && this.Minor < 65536;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is SubsystemVersion && Equals((SubsystemVersion)obj);
        }

        public override int GetHashCode()
        {
            return Hash.Combine(this.Minor.GetHashCode(), this.Major.GetHashCode());
        }

        public bool Equals(SubsystemVersion other)
        {
            return this.Major == other.Major && this.Minor == other.Minor;
        }

        public override string ToString()
        {
            return string.Format("{0}.{1:00}", this.Major, this.Minor);
        }
    }
}
