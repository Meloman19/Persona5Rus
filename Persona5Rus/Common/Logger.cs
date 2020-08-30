using System.IO;

namespace Persona5Rus.Common
{
    internal static class Logger
    {
        private static FileStream outputStream;
        private static TextWriter writer;

        private static object _lock = new object();

        public static void Init(string output)
        {
            lock (_lock)
            {
                if (outputStream != null)
                {
                    outputStream.Dispose();
                    outputStream = null;
                    writer = null;
                }

                try
                {
                    outputStream = new FileStream(output, FileMode.Append, FileAccess.Write);
                    writer = new StreamWriter(outputStream);
                }
                catch { }
            }
        }

        public static void Write(string message)
        {
            lock (_lock)
            {
                writer?.WriteLine(message);
            }
        }
    }
}