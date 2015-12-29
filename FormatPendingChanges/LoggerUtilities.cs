using System;
using System.Globalization;
using System.IO;

namespace Microsoft.FormatPendingChanges
{
    internal static class LoggerUtilities
    {
        private static readonly string LogFileName = Path.Combine(Path.GetTempPath(), "FormatPendingChanges_Errors.txt");
        private static readonly string MessageFormat =
@"Time {0}
Begin Message
{1}
End Message

";

        public static void LogError(string error)
        {
            try
            {
                File.AppendAllText(LogFileName, string.Format(CultureInfo.CurrentCulture, MessageFormat, DateTime.Now.ToString("s"), error));
            }
            catch
            {
                // Silently fails if the error can not be logged.
            }
        }
    }
}
