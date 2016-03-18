using System;
using System.IO;

namespace ReportTranslator
{
    static class Program
    {
        /// <summary>
        ///     Enables localization of report files through xml translation tables
        /// </summary>
        static int Main(params string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: <prepare|translate> <path-to-reports> <translation-xlsx-filename>");
                return -1;
            }

            try
            {
                string command = args[0];
                string pathTorReports = args[1];
                string translationXml = args[2];

                if (!Directory.Exists(pathTorReports))
                {
                    Console.WriteLine(@"Directory {0} does not exist", pathTorReports);
                    return -2;
                }

                string absDirectory = Path.GetFullPath(pathTorReports);

                switch (command)
                {
                    case "prepare":
                        new RdlcTranslator(absDirectory, translationXml).Prepare();
                        break;

                    case "translate":
                        new RdlcTranslator(absDirectory, translationXml).Translate();
                        break;

                    default:
                        Console.WriteLine(@"Command must be either 'prepare' or 'translate'");
                        return -2;
                }

                return 0;
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine(ex.Message);
                return 2; // cancelled in the progress, some files may have been swapped
            }
            catch (Exception ex)
            {
                Console.Write(@"Unexpected error: ");
                Console.WriteLine(ex.Message);
                return -4; // unexpected exception
            }
            finally
            {
#if DEBUG
                Console.ReadLine();
#endif
            }

            return 0;
        }


        /// <summary>
        ///     Display a message and ask for confirmation
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static bool Confirm(string message)
        {
            Console.WriteLine(message);

            for (; ; )
            {
                Console.Write(@"Proceed? Yes / No / Cancel (y/n/c) ");
                ConsoleKeyInfo result = Console.ReadKey();
                Console.WriteLine();
                if (result.Key == ConsoleKey.Y) return true;
                if (result.Key == ConsoleKey.N) return false;
                if (result.Key == ConsoleKey.C)
                    throw new OperationCanceledException("User cancelled process. Some files may already have been swapped.");
            }
        }
    }
}
