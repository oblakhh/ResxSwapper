using System;
using System.IO;
using System.Xml;
using Microsoft.VisualBasic.FileIO;

namespace ResxSwapper
{
    public class ResxSwapper
    {
        public string NeutralFilename { get; private set; }
        
        public string ImportFilename { get; private set; }

        public string ExportFilename { get; private set; }

        public ResxSwapper(string neutralFilename, string importFilename, string exportFilename)
        {
            NeutralFilename = neutralFilename;
            ImportFilename = importFilename;
            ExportFilename = exportFilename;
        }

        public void Run()
        {
            // Basic validation
            if(!File.Exists(NeutralFilename)) throw new Exception(@"Neutral file does not exist");
            if(!File.Exists(ImportFilename)) throw new Exception(@"Import file does not exist");
            if(File.Exists(ExportFilename)) throw new Exception(@"Export file does already exist");

            var neutralDoc = new XmlDocument();
            neutralDoc.LoadXml(File.ReadAllText(NeutralFilename));

            var importDoc = new XmlDocument();
            importDoc.LoadXml(File.ReadAllText(ImportFilename));

            var exportDoc = new XmlDocument();
            exportDoc.LoadXml(File.ReadAllText(ImportFilename));

            var importNodes = importDoc.SelectNodes("/root/data");

            if (importNodes != null)
            {
                foreach (XmlElement importNode in importNodes)
                {
                    var nameAttribute = importNode.Attributes["name"];
                    if (nameAttribute == null)
                    {
                        // Should not happen
                        continue;   
                    }

                    var name = nameAttribute.Value;

                    var importValue = importNode.SelectSingleNode("value");
                    if (importValue == null)
                    {
                        // Should not happen
                        continue;
                    }

                    var neutralValue = neutralDoc.SelectSingleNode("/root/data[@name=\"" + name + "\"]/value");
                    if (neutralValue == null)
                    {
                        // Should not happen
                        continue;
                    }

                    var exportValue = exportDoc.SelectSingleNode("/root/data[@name=\"" + name + "\"]/value");

// ReSharper disable once PossibleNullReferenceException
                    exportValue.InnerText = neutralValue.InnerText;
                    neutralValue.InnerText = importValue.InnerText;

                    Console.WriteLine(@"  Swapped [{0}]", name);

                }

                neutralDoc.Save(NeutralFilename);
                exportDoc.Save(ExportFilename);

                Console.WriteLine(@" Finished! Moving {0} to recycle bin", Path.GetFileName(ImportFilename));
                FileSystem.DeleteFile(ImportFilename, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }

        }
    }
}
