namespace Microsoft.CodeAnalysis.Scripting.Hosting
{
    /// <summary>
    /// Provides access to the scripting environment.
    /// </summary>
    public class ScriptEnv
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptEnv"/> class.
        /// </summary>
        ///<param name="filePath">The path to the script being executed.</param>
        public ScriptEnv(string filePath)
        {
            FilePath = filePath;
        }

        /// <summary>
        /// The path to the script source if it originated from a file, empty otherwise.
        /// </summary>
        public string FilePath { get; }
    }
}
