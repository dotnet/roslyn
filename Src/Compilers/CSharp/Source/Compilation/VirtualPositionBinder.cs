namespace Roslyn.Compilers.CSharp
{
    // See the comments on Binder.VirtualPosition for an explanation
    // of why we have this class.
    internal sealed class VirtualPositionBinder : Binder
    {
        private readonly int position;

        public VirtualPositionBinder(int position, Binder next)
            : base(next)
        {
            this.position = position;
        }

        internal override int? VirtualPosition
        {
            get
            {
                return position;
            }
        }
    }
}