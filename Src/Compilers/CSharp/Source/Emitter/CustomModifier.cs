using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Roslyn.Compilers.Internal;

namespace Roslyn.Compilers.CSharp.Emit
{
    internal sealed class CustomModifier : Microsoft.Cci.ICustomModifier
    {
        private readonly bool isOptional;
        private readonly Microsoft.Cci.ITypeReference modifier;

        public CustomModifier(Microsoft.Cci.ITypeReference modifier, bool isOptional)
        {
            Contract.ThrowIfNull(modifier);

            this.modifier = modifier;
            this.isOptional = isOptional;
        }

        bool Microsoft.Cci.ICustomModifier.IsOptional
        {
            get 
            {
                return isOptional;
            }
        }

        Microsoft.Cci.ITypeReference Microsoft.Cci.ICustomModifier.Modifier
        {
            get 
            {
                return modifier; 
            }
        }
    }
}
