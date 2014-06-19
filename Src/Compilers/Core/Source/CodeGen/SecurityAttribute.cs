using Microsoft.Cci;
using SecurityAction = System.Security.Permissions.SecurityAction;

namespace Roslyn.Compilers.CodeGen
{
    /// <summary>
    /// Represents a CodeAccessSecurityAttribute wrapped with the specified SecurityAction.
    /// Each security attribute represents a serialized permission or permission set for a specified security action.
    /// </summary>
    internal struct SecurityAttribute : ISecurityAttribute
    {
        private readonly SecurityAction action;
        private readonly ICustomAttribute attribute;

        public SecurityAttribute(SecurityAction action, ICustomAttribute attribute)
        {
            this.attribute = attribute;
            this.action = action;
        }

        public SecurityAction Action
        {
            get { return action; }
        }

        public ICustomAttribute Attribute
        {
            get { return attribute; }
        }
    }
}