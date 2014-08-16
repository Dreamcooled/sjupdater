using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Windows.Forms;

namespace updaterhasher
{
    internal class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Warning: Filenames cannot have spaces yet");
            Console.WriteLine("Warning: Sub Directories are not support yet");

            Console.ResetColor();
            Console.WriteLine("\nDrag'n Drop a Folder");
            string path = Console.ReadLine().Replace("\"", "");

            string[] files = Directory.GetFiles(path);

            string text = "";

            Console.WriteLine();

            foreach (var file in files)
            {
                MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();

                byte[] hashBytes;
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    hashBytes = md5.ComputeHash(fs);
                }

                string filetext = Path.GetFileName(file) + ":" + BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

                Console.WriteLine(filetext);

                text += filetext + "\n";
            }

            Clipboard.SetText(text);
            Console.WriteLine("\nText copied to Clipboard");
            Thread.Sleep(3500);
        }
    }
}