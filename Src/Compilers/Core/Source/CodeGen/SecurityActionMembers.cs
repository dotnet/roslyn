using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using Microsoft.CodeAnalysis.Text;

namespace Microsoft.CodeAnalysis.CodeGen
{
    /// <summary>
    /// This is a duplication of the obsolete members of the enum System.Security.Permissions.SecurityAction.SecurityAction because the original
    /// enum has obsolete members and it's not possible in VB to suppress warnings for only parts of the source code.
    /// </summary>
    internal static class SecurityActionMembers
    {
        // Disable Obsolete warnings for certain security actions which are obsolete.
#pragma warning disable 618
        public const SecurityAction Deny = SecurityAction.Deny;
        public const SecurityAction RequestMinimum = SecurityAction.RequestMinimum;
        public const SecurityAction RequestOptional = SecurityAction.RequestOptional;
        public const SecurityAction RequestRefuse = SecurityAction.RequestRefuse;
#pragma warning restore 618

        // this value is accepted by the compiler, but not part of the documented SecurityAction enum.
        public const SecurityAction AcceptedButUndocumented = (SecurityAction)1;
    }
}