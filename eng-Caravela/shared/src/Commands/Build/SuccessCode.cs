namespace PostSharp.Engineering.BuildTools.Commands.Build
{
    public enum SuccessCode
    {
        /// <summary>
        /// Success.
        /// </summary>
        Success,
        
        /// <summary>
        /// Error, but we can try to continue to the next item.
        /// </summary>
        Error,
        
        /// <summary>
        /// Error, and we have to stop immediately.
        /// </summary>
        Fatal
    }
}