#pragma warning disable IDE1006 // Naming Styles

namespace WDCMonInterface
{
    static class Logger
    {
        public static string FileName { get; set; } = "log.txt";
        public static void Log(string message)
        {
            try
            {
                File.AppendAllLines(FileName, new string[] { message });
            }
            catch (Exception)
            {

            }
        }

        public static void Log(Exception ex)
        {
            try
            {
                File.AppendAllLines(FileName, new string[] { "Exception:", ex.Message, ex.StackTrace ?? "" });
                if (ex.InnerException != null)
                {
                    Log(ex.InnerException);
                }
            }
            catch (Exception)
            {

            }
        }
    }

}