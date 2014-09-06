﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using IronText.Extensibility;
using IronText.Reflection.Reporting;

namespace IronText.Reports
{
    public class ScannerReport : IReport
    {
        private readonly string fileName;

        public ScannerReport(string fileName)
        {
            this.fileName = fileName;
        }

        public void Build(IReportData data)
        {
            string path = Path.Combine(data.DestinationDirectory, fileName);

            using (var file = new StreamWriter(path, false, Encoding.UTF8))
            {
                foreach (var scanProduciton in data.Grammar.Matchers)
                {
                    file.WriteLine(" " + scanProduciton.ToString());
                }
            }
        }
    }
}