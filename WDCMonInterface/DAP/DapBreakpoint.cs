#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface
{
    class DapBreakpoint
    {
        private static int runningNumber = 1;
        /// <summary>
        /// The identifier for the breakpoint. It is needed if breakpoint events are
        /// used to update or remove breakpoints.
        /// </summary>
        public int number { get; set; } = runningNumber++;
        /// <summary>
        /// If true, the breakpoint could be set (but not necessarily at the desired
        /// location
        /// </summary>
        public bool? verified { get; set; }
        /// <summary>
        /// A message about the state of the breakpoint.
        /// This is shown to the user and can be used to explain why a breakpoint could
        /// not be verified.
        /// </summary>
        public string? message { get; set; } = null;
        /// <summary>
        /// The source where the breakpoint is located.
        /// </summary>
        public DapSource? source { get; set; } = null;
        /// <summary>
        /// The start line of the actual range covered by the breakpoint.
        /// </summary>
        public int line { get; set; }
        /// <summary>
        /// A memory reference to where the breakpoint is set.
        /// </summary>
        public string? instructionReference { get; set; } = null;
    }
}