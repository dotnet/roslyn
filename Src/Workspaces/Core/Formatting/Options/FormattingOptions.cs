using System;

namespace Roslyn.Services.Formatting.Options
{
    public sealed class FormattingOptions
    {
        public bool UseTab { get; private set; }
        public int TabSize { get; private set; }
        public int IndentationSize { get; private set; }

        public FormattingOptions(bool useTab, int tabSize, int indentationSize) :
            this(useTab, tabSize, indentationSize, debugMode: false)
        {
        }

        // internal usage only
        internal bool DebugMode { get; private set; }

        internal FormattingOptions(bool useTab, int tabSize, int indentationSize, bool debugMode)
        {
            this.UseTab = useTab;
            this.TabSize = tabSize;
            this.IndentationSize = indentationSize;
            this.DebugMode = debugMode;
        }

        /// <summary>
        /// return default formatting options
        /// 
        /// this can be modified and given to Format method to have different formatting behavior
        /// </summary>
        public static FormattingOptions GetDefaultOptions()
        {
            return new FormattingOptions(useTab: false, tabSize: 4, indentationSize: 4, debugMode: false);
        }
    }
}
