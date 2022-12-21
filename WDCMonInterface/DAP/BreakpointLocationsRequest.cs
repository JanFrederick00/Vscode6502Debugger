#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface
{
    class BreakpointLocationsRequest
    {
        public DapSource? source { get; set; }
        public int? line { get; set; }
        public int? endLine { get; set; }
    }
}