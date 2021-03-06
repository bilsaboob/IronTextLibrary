﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using IronText.Extensibility;
using IronText.Reflection.Reporting;

namespace IronText.Framework
{
    public class ScannerDocumentAttribute : LanguageMetadataAttribute, IReport
    {
        private readonly string fileName;

        public ScannerDocumentAttribute(string fileName)
        {
            this.fileName = fileName;
        }

        public override IEnumerable<IReport> GetReports()
        {
            yield return this;
        }

        public void Build(IReportData data)
        {
            string path = Path.Combine(data.DestinationDirectory, fileName);

            using (var file = new StreamWriter(path, false, Encoding.UTF8))
            {
                foreach (var scanCondition in data.Grammar.Conditions)
                {
                    file.WriteLine("-------------------------------------");
                    file.WriteLine("ScanMode {0}:", scanCondition.Name);
                    foreach (var scanProduciton in scanCondition.Matchers)
                    {
                        file.WriteLine(" " + scanProduciton.ToString());
                    }
                }
            }
        }
    }
}
