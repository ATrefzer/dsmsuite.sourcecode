﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DsmSuite.Analyzer.VisualStudio.Settings;

namespace DsmSuite.Analyzer.VisualStudio.VisualStudio
{
    public class CsProjectFile : ProjectFileBase
    {
        public CsProjectFile(string solutionFolder, string solutionDir, string projectPath, AnalyzerSettings analyzerSettings) :
            base(solutionFolder, solutionDir, projectPath, analyzerSettings)
        {
        }

        public override void Analyze()
        {
        }
    }
}
