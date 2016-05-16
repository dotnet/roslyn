// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// Represents an identity of an assembly as defined by CLI metadata specification.
    /// </summary>
    /// <remarks>
    /// May represent assembly definition or assembly reference identity.
    /// </remarks>
    [DebuggerDisplay("{GetDebuggerDisplay(), nq}")]
    public sealed partial class AssemblyIdentity : IEquatable<AssemblyIdentity>
    {
        // determines the binding model (how assembly references are matched to assembly definitions)
        private readonly AssemblyContentType _contentType;

        // not null, not empty:
        private readonly string _name;

        // no version: 0.0.0.0
        private readonly Version _version;

        // Invariant culture is represented as empty string.
        // The culture might not exist on the system.
        private readonly string _cultureName;

        // weak name:   empty
        // strong name: full public key or empty (only token is available)
        private readonly ImmutableArray<byte> _publicKey;

        // weak name: empty
        // strong name: null (to be initialized), or 8 bytes (initialized)
        private ImmutableArray<byte> _lazyPublicKeyToken;

        private readonly bool _isRetargetable;

        // cached display name
        private string _lazyDisplayName;

        // cached hash code
        private int _lazyHashCode;

        internal const int PublicKeyTokenSize = 8;

        private AssemblyIdentity(AssemblyIdentity other, Version version)
        {
            Debug.Assert((object)other != null);
            Debug.Assert((object)version != null);

            _contentType = other.ContentType;
            _name = other._name;
            _cultureName = other._cultureName;
            _publicKey = other._publicKey;
            _lazyPublicKeyToken = other._lazyPublicKeyToken;
            _isRetargetable = other._isRetargetable;

            _version = version;
            _lazyDisplayName = null;
            _lazyHashCode = 0;
        }

        internal AssemblyIdentity WithVersion(Version version) => (version == _version) ? this : new AssemblyIdentity(this, version);

        /// <summary>
        /// Constructs an <see cref="AssemblyIdentity"/> from its constituent parts.
        /// </summary>
        /// <param name="name">The simple name of the assembly.</param>
        /// <param name="version">The version of the assembly.</param>
        /// <param name="cultureName">
        /// The name of the culture to associate with the assembly. 
        /// Specify null, <see cref="string.Empty"/>, or "neutral" (any casing) to represent <see cref="System.Globalization.CultureInfo.InvariantCulture"/>.
        /// The name can be an arbitrary string that doesn't contain NUL character, the legality of the culture name is not validated.
        /// </param>
        /// <param name="publicKeyOrToken">The public key or public key token of the assembly.</param>
        /// <param name="hasPublicKey">Indicates whether <paramref name="publicKeyOrToken"/> represents a public key.</param>
        /// <param name="isRetargetable">Indicates whether the assembly is retargetable.</param>
        /// <param name="contentType">Specifies the binding model for how this object will be treated in comparisons.</param>
        /// <exception cref="ArgumentException">If <paramref name="name"/> is null, empty or contains a NUL character.</exception>
        /// <exception cref="ArgumentException">If <paramref name="cultureName"/> contains a NUL character.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="contentType"/> is not a value of the <see cref="AssemblyContentType"/> enumeration.</exception>
        /// <exception cref="ArgumentException"><paramref name="version"/> contains values that are not greater than or equal to zero and less than or equal to ushort.MaxValue.</exception>
        /// <exception cref="ArgumentException"><paramref name="hasPublicKey"/> is true and <paramref name="publicKeyOrToken"/> is not set.</exception>
        /// <exception cref="ArgumentException"><paramref name="hasPublicKey"/> is false and <paramref name="publicKeyOrToken"/> 
        /// contains a value that is not the size of a public key token, 8 bytes.</exception>
        public AssemblyIdentity(
            string name,
            Version version = null,
            string cultureName = null,
            ImmutableArray<byte> publicKeyOrToken = default(ImmutableArray<byte>),
            bool hasPublicKey = false,
            bool isRetargetable = false,
            AssemblyContentType contentType = AssemblyContentType.Default)
        {
            if (!IsValid(contentType))
            {
                throw new ArgumentOutOfRangeException(nameof(contentType), CodeAnalysisResources.InvalidContentType);
            }

            if (!IsValidName(name))
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.InvalidAssemblyName, name), nameof(name));
            }

            if (!IsValidCultureName(cultureName))
            {
                throw new ArgumentException(string.Format(CodeAnalysisResources.InvalidCultureName, cultureName), nameof(cultureName));
            }

            // Version allows more values then can be encoded in metadata:
            if (!IsValid(version))
            {
                throw new ArgumentOutOfRangeException(nameof(version));
            }

            if (hasPublicKey)
            {
                if (!MetadataHelpers.IsValidPublicKey(publicKeyOrToken))
                {
                    throw new ArgumentException(CodeAnalysisResources.InvalidPublicKey, nameof(publicKeyOrToken));
                }
            }
            else
            {
                if (!publicKeyOrToken.IsDefaultOrEmpty && publicKeyOrToken.Length != PublicKeyTokenSize)
                {
                    throw new ArgumentException(CodeAnalysisResources.InvalidSizeOfPublicKeyToken, nameof(publicKeyOrToken));
                }
            }

            if (isRetargetable && contentType == AssemblyContentType.WindowsRuntime)
            {
                throw new ArgumentException(CodeAnalysisResources.WinRTIdentityCantBeRetargetable, nameof(isRetargetable));
            }

            _name = name;
            _version = version ?? NullVersion;
            _cultureName = NormalizeCultureName(cultureName);
            _isRetargetable = isRetargetable;
            _contentType = contentType;
            InitializeKey(publicKeyOrToken, hasPublicKey, out _publicKey, out _lazyPublicKeyToken);
        }

        // asserting constructor used by SourceAssemblySymbol:
        internal AssemblyIdentity(
            string name,
            Version version,
            string cultureName,
            ImmutableArray<byte> publicKeyOrToken,
            bool hasPublicKey)
        {
            Debug.Assert(IsValidName(name));
            Debug.Assert(IsValid(version));
            Debug.Assert(IsValidCultureName(cultureName));
            Debug.Assert((hasPublicKey && MetadataHelpers.IsValidPublicKey(publicKeyOrToken)) || (!hasPublicKey && (publicKeyOrToken.IsDefaultOrEmpty || publicKeyOrToken.Length == PublicKeyTokenSize)));

            _name = name;
            _version = version ?? NullVersion;
            _cultureName = NormalizeCultureName(cultureName);
            _isRetargetable = false;
            _contentType = AssemblyContentType.Default;
            InitializeKey(publicKeyOrToken, hasPublicKey, out _publicKey, out _lazyPublicKeyToken);
        }

        // constructor used by metadata reader:
        internal AssemblyIdentity(
            string name,
            Version version,
            string cultureName,
            ImmutableArray<byte> publicKeyOrToken,
            bool hasPublicKey,
            bool isRetargetable,
            AssemblyContentType contentType,
            bool noThrow)
        {
            Debug.Assert(!string.IsNullOrEmpty(name));
            Debug.Assert((hasPublicKey && MetadataHelpers.IsValidPublicKey(publicKeyOrToken)) || (!hasPublicKey && (publicKeyOrToken.IsDefaultOrEmpty || publicKeyOrToken.Length == PublicKeyTokenSize)));
            Debug.Assert(noThrow);

            _name = name;
            _version = version ?? NullVersion;
            _cultureName = NormalizeCultureName(cultureName);
            _contentType = IsValid(contentType) ? contentType : AssemblyContentType.Default;
            _isRetargetable = isRetargetable && _contentType != AssemblyContentType.WindowsRuntime;
            InitializeKey(publicKeyOrToken, hasPublicKey, out _publicKey, out _lazyPublicKeyToken);
        }

        private static string NormalizeCultureName(string cultureName)
        {
            // Treat "neutral" culture as invariant culture name, although it is technically not a legal culture name.
            //
            // A few reasons:
            // 1) Invariant culture is displayed as "neutral" in the identity display name.
            //    Thus a) an identity with culture "neutral" wouldn't roundrip serialization to display name.
            //         b) an identity with culture "neutral" wouldn't compare equal to invariant culture identity, 
            //            yet their display names are the same which is confusing.
            //
            // 2) The implementation of AssemblyName.CultureName on Mono incorrectly returns "neutral" for invariant culture identities.

            return cultureName == null || AssemblyIdentityComparer.CultureComparer.Equals(cultureName, InvariantCultureDisplay) ?
                string.Empty : cultureName;
        }

        private static void InitializeKey(ImmutableArray<byte> publicKeyOrToken, bool hasPublicKey,
            out ImmutableArray<byte> publicKey, out ImmutableArray<byte> publicKeyToken)
        {
            if (hasPublicKey)
            {
                publicKey = publicKeyOrToken;
                publicKeyToken = default(ImmutableArray<byte>);
            }
            else
            {
                publicKey = ImmutableArray<byte>.Empty;
                publicKeyToken = publicKeyOrToken.IsDefault ? ImmutableArray<byte>.Empty : publicKeyOrToken;
            }
        }

        internal static bool IsValidCultureName(string name)
        {
            // The native compiler doesn't enforce that the culture be anything in particular. 
            // AssemblyIdentity should preserve user input even if it is of dubious utility.

            // Note: If these checks change, the error messages emitted by the compilers when
            // this case is detected will also need to change. They currently directly
            // name the presence of the NUL character as the reason that the culture name is invalid.
            return name == null || name.IndexOf('\0') < 0;
        }

        private static bool IsValidName(string name)
        {
            return !string.IsNullOrEmpty(name) && name.IndexOf('\0') < 0;
        }

        internal readonly static Version NullVersion = new Version(0, 0, 0, 0);

        private static bool IsValid(Version value)
        {
            return value == null
                || value.Major >= 0
                && value.Minor >= 0
                && value.Build >= 0
                && value.Revision >= 0
                && value.Major <= ushort.MaxValue
                && value.Minor <= ushort.MaxValue
                && value.Build <= ushort.MaxValue
                && value.Revision <= ushort.MaxValue;
        }

        private static bool IsValid(AssemblyContentType value)
        {
            return value >= AssemblyContentType.Default && value <= AssemblyContentType.WindowsRuntime;
        }

        /// <summary>
        /// The simple name of the assembly.
        /// </summary>
        public string Name { get { return _name; } }

        /// <summary>
        /// The version of the assembly.
        /// </summary>
        public Version Version { get { return _version; } }

        /// <summary>
        /// The culture name of the assembly, or empty if the culture is neutral.
        /// </summary>
        public string CultureName { get { return _cultureName; } }

        /// <summary>
        /// The AssemblyNameFlags.
        /// </summary>
        public AssemblyNameFlags Flags
        {
            get
            {
                return (_isRetargetable ? AssemblyNameFlags.Retargetable : AssemblyNameFlags.None) |
                       (HasPublicKey ? AssemblyNameFlags.PublicKey : AssemblyNameFlags.None);
            }
        }

        /// <summary>
        /// Specifies assembly binding model for the assembly definition or reference;
        /// that is how assembly references are matched to assembly definitions.
        /// </summary>
        public AssemblyContentType ContentType
        {
            get { return _contentType; }
        }

        /// <summary>
        /// True if the assembly identity includes full public key.
        /// </summary>
        public bool HasPublicKey
        {
            get { return _publicKey.Length > 0; }
        }

        /// <summary>
        /// Full public key or empty.
        /// </summary>
        public ImmutableArray<byte> PublicKey
        {
            get { return _publicKey; }
        }

        /// <summary>
        /// Low 8 bytes of SHA1 hash of the public key, or empty.
        /// </summary>
        public ImmutableArray<byte> PublicKeyToken
        {
            get
            {
                if (_lazyPublicKeyToken.IsDefault)
                {
                    ImmutableInterlocked.InterlockedCompareExchange(ref _lazyPublicKeyToken, CalculatePublicKeyToken(_publicKey), default(ImmutableArray<byte>));
                }

                return _lazyPublicKeyToken;
            }
        }

        /// <summary>
        /// True if the assembly identity has a strong name, ie. either a full public key or a token.
        /// </summary>
        public bool IsStrongName
        {
            get
            {
                // if we don't have public key, the token is either empty or 8-byte value
                Debug.Assert(HasPublicKey || !_lazyPublicKeyToken.IsDefault);

                return HasPublicKey || _lazyPublicKeyToken.Length > 0;
            }
        }

        /// <summary>
        /// Gets the value which specifies if the assembly is retargetable. 
        /// </summary>
        public bool IsRetargetable
        {
            get { return _isRetargetable; }
        }

        internal static bool IsFullName(AssemblyIdentityParts parts)
        {
            const AssemblyIdentityParts nvc = AssemblyIdentityParts.Name | AssemblyIdentityParts.Version | AssemblyIdentityParts.Culture;
            return (parts & nvc) == nvc && (parts & AssemblyIdentityParts.PublicKeyOrToken) != 0;
        }

        #region Equals, GetHashCode 

        /// <summary>
        /// Determines whether two <see cref="AssemblyIdentity"/> instances are equal.
        /// </summary>
        /// <param name="left">The operand appearing on the left side of the operator.</param>
        /// <param name="right">The operand appearing on the right side of the operator.</param>
        public static bool operator ==(AssemblyIdentity left, AssemblyIdentity right)
        {
            return EqualityComparer<AssemblyIdentity>.Default.Equals(left, right);
        }

        /// <summary>
        /// Determines whether two <see cref="AssemblyIdentity"/> instances are not equal.
        /// </summary>
        /// <param name="left">The operand appearing on the left side of the operator.</param>
        /// <param name="right">The operand appearing on the right side of the operator.</param>
        public static bool operator !=(AssemblyIdentity left, AssemblyIdentity right)
        {
            return !(left == right);
        }

        /// <summary>
        /// Determines whether the specified instance is equal to the current instance.
        /// </summary>
        /// <param name="obj">The object to be compared with the current instance.</param>
        public bool Equals(AssemblyIdentity obj)
        {
            return !ReferenceEquals(obj, null)
                && (_lazyHashCode == 0 || obj._lazyHashCode == 0 || _lazyHashCode == obj._lazyHashCode)
                && MemberwiseEqual(this, obj) == true;
        }

        /// <summary>
        /// Determines whether the specified instance is equal to the current instance.
        /// </summary>
        /// <param name="obj">The object to be compared with the current instance.</param>
        public override bool Equals(object obj)
        {
            return Equals(obj as AssemblyIdentity);
        }

        /// <summary>
        /// Returns the hash code for the current instance.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            if (_lazyHashCode == 0)
            {
                // Do not include PK/PKT in the hash - collisions on PK/PKT are rare (assembly identities differ only in PKT/PK)
                // and we can't calculate hash of PKT if only PK is available
                _lazyHashCode =
                    Hash.Combine(AssemblyIdentityComparer.SimpleNameComparer.GetHashCode(_name),
                    Hash.Combine(_version.GetHashCode(), GetHashCodeIgnoringNameAndVersion()));
            }

            return _lazyHashCode;
        }

        internal int GetHashCodeIgnoringNameAndVersion()
        {
            return 
                Hash.Combine((int)_contentType, 
                Hash.Combine(_isRetargetable, 
                AssemblyIdentityComparer.CultureComparer.GetHashCode(_cultureName)));
        }

        // internal for testing
        internal static ImmutableArray<byte> CalculatePublicKeyToken(ImmutableArray<byte> publicKey)
        {
            var hash = CryptographicHashProvider.ComputeSha1(publicKey);

            // SHA1 hash is always 160 bits:
            Debug.Assert(hash.Length == CryptographicHashProvider.Sha1HashSize);

            // PublicKeyToken is the low 64 bits of the SHA-1 hash of the public key.
            int l = hash.Length - 1;
            var result = ArrayBuilder<byte>.GetInstance(PublicKeyTokenSize);
            for (int i = 0; i < PublicKeyTokenSize; i++)
            {
                result.Add(hash[l - i]);
            }

            return result.ToImmutableAndFree();
        }

        /// <summary>
        /// Returns true (false) if specified assembly identities are (not) equal 
        /// regardless of unification, retargeting or other assembly binding policies. 
        /// Returns null if these policies must be consulted to determine name equivalence.
        /// </summary>
        internal static bool? MemberwiseEqual(AssemblyIdentity x, AssemblyIdentity y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (!AssemblyIdentityComparer.SimpleNameComparer.Equals(x._name, y._name))
            {
                return false;
            }

            if (x._version.Equals(y._version) && EqualIgnoringNameAndVersion(x, y))
            {
                return true;
            }

            return null;
        }

        internal static bool EqualIgnoringNameAndVersion(AssemblyIdentity x, AssemblyIdentity y)
        {
            return
                x.IsRetargetable == y.IsRetargetable &&
                x.ContentType == y.ContentType &&
                AssemblyIdentityComparer.CultureComparer.Equals(x.CultureName, y.CultureName) &&
                KeysEqual(x, y);
        }

        internal static bool KeysEqual(AssemblyIdentity x, AssemblyIdentity y)
        {
            var xToken = x._lazyPublicKeyToken;
            var yToken = y._lazyPublicKeyToken;

            // weak names or both strong names with initialized PKT - compare tokens:
            if (!xToken.IsDefault && !yToken.IsDefault)
            {
                return xToken.SequenceEqual(yToken);
            }

            // both are strong names with uninitialized PKT - compare full keys:
            if (xToken.IsDefault && yToken.IsDefault)
            {
                return x._publicKey.SequenceEqual(y._publicKey);
            }

            // one of the strong names doesn't have PK, other doesn't have PTK initialized.
            if (xToken.IsDefault)
            {
                return x.PublicKeyToken.SequenceEqual(yToken);
            }
            else
            {
                return xToken.SequenceEqual(y.PublicKeyToken);
            }
        }

        #endregion

        #region AssemblyName conversions 

        /// <summary>
        /// Retrieves assembly definition identity from given runtime assembly.
        /// </summary>
        /// <param name="assembly">The runtime assembly.</param>
        /// <returns>Assembly definition identity.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="assembly"/> is null.</exception>
        public static AssemblyIdentity FromAssemblyDefinition(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            return FromAssemblyDefinition(assembly.GetName());
        }

        // internal for testing
        internal static AssemblyIdentity FromAssemblyDefinition(AssemblyName name)
        {
            // AssemblyDef always has full key or no key:
            var publicKeyBytes = name.GetPublicKey();
            ImmutableArray<byte> publicKey = (publicKeyBytes != null) ? ImmutableArray.Create(publicKeyBytes) : ImmutableArray<byte>.Empty;

            return new AssemblyIdentity(
                name.Name,
                name.Version,
                name.CultureName,
                publicKey,
                hasPublicKey: publicKey.Length > 0,
                isRetargetable: (name.Flags & AssemblyNameFlags.Retargetable) != 0,
                contentType: name.ContentType);
        }

        internal static AssemblyIdentity FromAssemblyReference(AssemblyName name)
        {
            // AssemblyRef either has PKT or no key:
            return new AssemblyIdentity(
                name.Name,
                name.Version,
                name.CultureName,
                ImmutableArray.Create(name.GetPublicKeyToken()),
                hasPublicKey: false,
                isRetargetable: (name.Flags & AssemblyNameFlags.Retargetable) != 0,
                contentType: name.ContentType);
        }

        #endregion
    }
}
