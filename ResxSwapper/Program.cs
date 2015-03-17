using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResxSwapper
{
    internal static class Program
    {
        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        private static int Main(params string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine(@"Usage: <neutral-culture> <import-culture> <directory>");
                return -1;
            }

            try
            {
                // Two letter culture code of the current culture, that will be added as a 
                // translated culture for the future
                string neutralLang = args[0];

                // Two letter culture of the translated culture that shall become the new
                // neutral culture and which translation shall be deleted afterwards
                string importLang = args[1];

                // Directory in which to search for occurences
                string directory = args[2];

                if (!Directory.Exists(directory))
                {
                    Console.WriteLine(@"Directory {0} does not exist", directory);
                    return -2;
                }

                string absDirectory = Path.GetFullPath(directory);

                // Ask user to proceed
                if (
                    !Confirm(
                        string.Format(@"Will recursively switch neutral language from [{0}] to [{1}] in directory {2}",
                            neutralLang, importLang, absDirectory)))
                {
                    return 1; // cancelled
                }


                Scan(neutralLang, importLang, absDirectory);
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
        }

        private static void Scan(string neutralLang, string importLang, string directory)
        {
            Console.WriteLine(@"Scanning " + directory);

            // Scan resx files in current dir
            IEnumerable<string> files = Directory.GetFiles(directory, "*.resx").Select(Path.GetFileName);
            foreach (string file in files)
            {
                if (!Regex.IsMatch(file, @"^[^\.]+\.resx$"))
                {
                    // Skip located resource
                    continue;
                }

                string importFile = file.Replace(".resx", string.Format(".{0}.resx", importLang));

                if (!File.Exists(Path.Combine(directory, importFile)))
                {
                    Console.WriteLine(@"{0} seems not yet to be localized to [{1}] or has already been swapped.", file,
                        importLang);
                    continue;
                }

                string exportFile = file.Replace(".resx", string.Format(".{0}.resx", neutralLang));

                if (File.Exists(Path.Combine(directory, exportFile)))
                {
                    Console.WriteLine(@"{0} seems already to be localized to [{1}] or has already been swapped.", file,
                        neutralLang);
                    continue;
                }

                Swap(
                    Path.Combine(directory, file),
                    Path.Combine(directory, importFile),
                    Path.Combine(directory, exportFile)
                    );
            }

            // Scan subdirectories
            foreach (string subDir in Directory.GetDirectories(directory))
            {
                Scan(neutralLang, importLang, subDir);
            }
        }

        /// <summary>
        ///     Carries out the actual swap
        /// </summary>
        /// <param name="neutralFileName"></param>
        /// <param name="importFileName"></param>
        /// <param name="exportFileName"></param>
        private static void Swap(string neutralFileName, string importFileName, string exportFileName)
        {
            if (
                !Confirm(string.Format(@"Importing strings from {0} to {1} and will create {2}", importFileName,
                    neutralFileName, exportFileName))) return;

            new ResxSwapper(neutralFileName, importFileName, exportFileName).Run();
        }

        /// <summary>
        ///     Display a message and ask for confirmation
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private static bool Confirm(string message)
        {
            Console.WriteLine(message);

            for (;;)
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