﻿using System;
using System.IO;
using System.Reflection;
using DsmSuite.Analyzer.Cpp.Settings;
using DsmSuite.Analyzer.Model.Core;
using DsmSuite.Analyzer.Util;
using DsmSuite.Common.Util;

namespace DsmSuite.Analyzer.Cpp
{
    public static class Program
    {
        private static AnalyzerSettings _analyzerSettings;

        static void Main(string[] args)
        {
            if (args.Length < 1)
            {
                Logger.LogUserMessage("Usage: DsmSuite.Analyzer.Cpp <settingsfile>");
            }
            else
            {
                FileInfo settingsFileInfo = new FileInfo(args[0]);
                if (!settingsFileInfo.Exists)
                {
                    AnalyzerSettings.WriteToFile(settingsFileInfo.FullName, AnalyzerSettings.CreateDefault());
                    Logger.LogUserMessage("Settings file does not exist. Default one created");
                }
                else
                {
                    _analyzerSettings = AnalyzerSettings.ReadFromFile(settingsFileInfo.FullName);
                    Logger.EnableLogging(Assembly.GetExecutingAssembly(), _analyzerSettings.LoggingEnabled);

                    ConsoleActionExecutor executor = new ConsoleActionExecutor("Analyzing C++ code");
                    executor.Execute(Analyze);
                }
            }
        }

        static void Analyze(IProgress<ProgressInfo> progress)
        {
            DsiModel model = new DsiModel("Analyzer", Assembly.GetExecutingAssembly());
            Analysis.Analyzer analyzer = new Analysis.Analyzer(model, _analyzerSettings, progress);
            analyzer.Analyze();
            model.Save(_analyzerSettings.OutputFilename, _analyzerSettings.CompressOutputFile, null);

        }
    }
}
