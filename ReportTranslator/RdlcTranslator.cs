using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.SqlServer.Server;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace ReportTranslator
{
    public class RdlcTranslator
    {
        private class Expression
        {
            public string Value { get; set; }
            public List<string> Occurences { get; private set; }

            public Expression()
            {
                Occurences = new List<string>();
            }
        }

        public string PathToReports { get; private set; }
        public string TranslationFileName { get; private set; }

        public RdlcTranslator(string pathToReports, string translationFileName)
        {
            PathToReports = pathToReports;
            TranslationFileName = translationFileName;
        }

        /// <summary>
        /// Extracts all translatable values from the reports inside the given directory and
        /// puts them in a consolidated translation file
        /// </summary>
        public void Prepare()
        {
            var allValues = new List<Expression>();

            var absFileName = Path.Combine(PathToReports, TranslationFileName);
            if (File.Exists(absFileName))
            {
                throw new Exception(string.Format(@"Translation file already exists, will not overwrite: {0}",
                    absFileName));
            }

            var xMngr = new XmlNamespaceManager(new NameTable());
            xMngr.AddNamespace("def", "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition");
            xMngr.AddNamespace("rd", "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner");

            // Scan rdlc files in current dir
            foreach (string file in Directory.GetFiles(PathToReports, "*.rdlc"))
            {
                string fileName = Path.GetFileName(file);
                Console.Write(@"Scanning {0} ... ", fileName);

                var reportDoc = new XmlDocument();
                reportDoc.LoadXml(File.ReadAllText(file));

                int foundCounter = 0;
                var values = reportDoc.SelectNodes("//def:Textbox/def:Value", xMngr);
                if (values != null)
                {
                    foreach (XmlElement value in values)
                    {
                        if (value != null && !string.IsNullOrEmpty(value.InnerText))
                        {
                            var innerText = value.InnerText;

                            var expr = allValues.FirstOrDefault(expression => expression.Value == innerText);
                            if (expr == null)
                            {
                                expr = new Expression {Value = innerText};
                                allValues.Add(expr);
                                foundCounter++;
                            }
                            expr.Occurences.Add(fileName);
                        }
                    }
                }

                Console.WriteLine(@"Found {0} new values.", foundCounter);
            }

            allValues = allValues.OrderBy(it => it.Value).ToList();

            using (var pck = new ExcelPackage())
            {
                //Create the worksheet
                ExcelWorksheet ws = pck.Workbook.Worksheets.Add("Report Translation");

                ws.InsertRow(1, allValues.Count + 1);

                var originalHeader = ws.Cells[1, 1];
                originalHeader.Value = "Original";
                originalHeader.Style.Font.Bold = true;

                var translatedHeader = ws.Cells[1, 2];
                translatedHeader.Value = "Translated";
                translatedHeader.Style.Font.Bold = true;

                var occurrencesHeader = ws.Cells[1, 3];
                occurrencesHeader.Value = "Occurences";
                occurrencesHeader.Style.Font.Bold = true;


                for (int rowi = 0; rowi < allValues.Count; rowi++)
                {
                    var originalValue = ws.Cells[rowi + 2, 1];
                    originalValue.Value = allValues[rowi].Value;

                    var translatedValue = ws.Cells[rowi + 2, 2];
                    translatedValue.Value = allValues[rowi].Value;

                    var occurrencesValue = ws.Cells[rowi + 2, 3];
                    occurrencesValue.Value = string.Join(", ", allValues[rowi].Occurences);
                }

                ws.Column(1).AutoFit(15);
                ws.Column(3).AutoFit(25);

                ws.Column(1).Style.Numberformat.Format = "@";
                ws.Column(1).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                ws.Column(1).Style.WrapText = true;
                ws.Column(1).Style.Locked = true;
                ws.Column(1).Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Column(1).Style.Fill.BackgroundColor.SetColor(Color.Gainsboro);

                ws.Column(2).Width = ws.Column(1).Width;
                ws.Column(2).Style.Numberformat.Format = "@";
                ws.Column(2).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                ws.Column(2).Style.WrapText = true;

                ws.Column(3).Style.Numberformat.Format = "@";
                ws.Column(3).Style.VerticalAlignment = ExcelVerticalAlignment.Top;
                ws.Column(3).Style.WrapText = true;

                using (Stream stream = File.OpenWrite(absFileName))
                {
                    pck.SaveAs(stream);
                }
            }
        }

        /// <summary>.
        /// Applies translations from a consolidated translation file to all reports in the given directory
        /// </summary>
        public void Translate()
        {
            var absFileName = Path.Combine(PathToReports, TranslationFileName);
            if (!File.Exists(absFileName))
            {
                throw new Exception(string.Format(@"Translation file does not exist: {0}", absFileName));
            }

            var xMngr = new XmlNamespaceManager(new NameTable());
            xMngr.AddNamespace("def", "http://schemas.microsoft.com/sqlserver/reporting/2005/01/reportdefinition");
            xMngr.AddNamespace("rd", "http://schemas.microsoft.com/SQLServer/reporting/reportdesigner");

            var translations = new Dictionary<string, string>();

            using (var pck = new ExcelPackage(new FileInfo(absFileName)))
            {
                //Create the worksheet
                ExcelWorksheet ws = pck.Workbook.Worksheets["Report Translation"];

                for (int i = 2; ; i++)
                {
                    var original = ws.Cells[i, 1].Text;
                    var translated = ws.Cells[i, 2].Text;

                    if (string.IsNullOrEmpty(original))
                        break;

                    translations[original] = translated;
                }
            }

            // Scan rdlc files in current dir
            foreach (string file in Directory.GetFiles(PathToReports, "*.rdlc"))
            {
                string fileName = Path.GetFileName(file);
                Console.Write(@"Scanning {0} ... ", fileName);

                var reportDoc = new XmlDocument();
                reportDoc.LoadXml(File.ReadAllText(file));

                int foundCounter = 0;
                var values = reportDoc.SelectNodes("//def:Textbox/def:Value", xMngr);
                if (values != null)
                {
                    foreach (XmlElement value in values)
                    {
                        if (value != null && !string.IsNullOrEmpty(value.InnerText) && translations.ContainsKey(value.InnerText))
                        {
                            try
                            {
                                value.InnerText = translations[value.InnerText];
                                foundCounter++;
                            }
                            catch (Exception ex)
                            {
                                Console.Error.WriteLine(ex);
                            }

                        }
                    }
                }

                using (var stream = File.OpenWrite(Path.Combine(PathToReports, "T_" + fileName)))
                    reportDoc.Save(stream);

                Console.WriteLine(@"Replaced {0} values.", foundCounter);
            }


  
        }
    }
}
