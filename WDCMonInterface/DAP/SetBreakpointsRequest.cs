#pragma warning disable IDE1006 // Naming Styles


namespace WDCMonInterface.DAP
{
    class SetBreakpointsRequest
    {

        public DapSource? source { get; set; }
        public class SetBreakpointRequestLocation
        {
            public int line { get; set; }
        }
        public List<SetBreakpointRequestLocation>? breakpoints { get; set; }
        public bool? sourceModified { get; set; }
    }
}