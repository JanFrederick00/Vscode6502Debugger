#pragma warning disable IDE1006 // Naming Styles


namespace WDCMonInterface.DAP
{
    class AdapterInformation
    {
        /// <summary>
        /// The ID of the client using this adapter.
        /// </summary>
        public string? clientID { get; set; }
        /// <summary>
        /// The human-readable name of the client using this adapter.
        /// </summary>
        public string? clientName { get; set; }
        /// <summary>
        /// The ID of the debug adapter.
        /// </summary>
        public string? adapterID { get; set; }
        /// <summary>
        /// The ISO-639 locale of the client using this adapter, e.g. en-US or de-CH.
        /// </summary>
        public string? locale { get; set; }
        /// <summary>
        /// If true all line numbers are 1-based (default).
        /// </summary>
        public bool? linesStartAt1 { get; set; }
        /// <summary>
        /// If true all column numbers are 1-based (default).
        /// </summary>
        public bool? columnsStartAt1 { get; set; }
        /// <summary>
        /// Determines in what format paths are specified. The default is `path`, which
        /// is the native format.
        /// Values: 'path', 'uri', etc.
        /// </summary>
        public string? pathFormat { get; set; }
    }
}