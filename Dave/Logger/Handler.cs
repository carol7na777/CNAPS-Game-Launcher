using System;
using System.IO;
using System.Text;

namespace Dave.Logger
{
    class ConsoleHandler
    {
        public void WriteImpl(string message) { Console.Write(message); }
    }

    class FileHandler
    {
        private string m_File { get; set; }

        public FileHandler(string file) { m_File = file; }

        public void WriteImpl(string message)
        {
            if (File.Exists(m_File))
            {
                File.Delete(m_File);
            }

            using FileStream fs = File.Create(m_File);
            byte[] info = new UTF8Encoding(true).GetBytes(message);
            fs.Write(info, 0, info.Length);
        }
    }
}
