﻿using System.IO;
using System.Reflection;
using DsmSuite.Analyzer.DotNet.Settings;
using DsmSuite.Analyzer.DotNet.Transfromation;
using DsmSuite.Analyzer.Model.Core;
using DsmSuite.Common.Util;

namespace DsmSuite.Analyzer.DotNet
{
    public class ConsoleAction : ConsoleActionBase
    {
        private readonly AnalyzerSettings _analyzerSettings;

        public ConsoleAction(AnalyzerSettings analyzerSettings) : base("Analyzing .Net code")
        {
            _analyzerSettings = analyzerSettings;
        }

        protected override bool CheckPrecondition()
        {
            bool result = true;
            if (!Directory.Exists(_analyzerSettings.Input.AssemblyDirectory))
            {
                result = false;
                Logger.LogUserMessage($"Input directory '{_analyzerSettings.Input.AssemblyDirectory}' does not exist.");
            }
            return result;
        }

        protected override void LogInputParameters()
        {
            Logger.LogUserMessage($"Assembly directory:{_analyzerSettings.Input.AssemblyDirectory}");
        }

        protected override void Action()
        {
            DsiModel model = new DsiModel("Analyzer", Assembly.GetExecutingAssembly());

            Analysis.Analyzer analyzer = new Analysis.Analyzer(model, _analyzerSettings, this);
            analyzer.Analyze();

            Transformer transformer = new Transformer(model, _analyzerSettings.Transformation, this);
            transformer.Transform();

            model.Save(_analyzerSettings.Output.Filename, _analyzerSettings.Output.Compress, this);
            Logger.LogUserMessage($"Found elements={model.GetElementCount()} relations={model.GetRelationCount()} resolvedRelations={model.ResolvedRelationPercentage:0.0}%");
        }

        protected override void LogOutputParameters()
        {
            Logger.LogUserMessage($"Output file: {_analyzerSettings.Output.Filename} compressed={_analyzerSettings.Output.Compress}");
        }
    }

    public static class Program
    {
        static void Main(string[] args)
        {
            Logger.Init(Assembly.GetExecutingAssembly(), true);

            if (args.Length < 1)
            {
                Logger.LogUserMessage("Usage: DsmSuite.Analyzer.DotNet <settingsfile>");
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
                    AnalyzerSettings analyzerSettings = AnalyzerSettings.ReadFromFile(settingsFileInfo.FullName);
                    Logger.LogLevel = analyzerSettings.LogLevel;

                    ConsoleAction action = new ConsoleAction(analyzerSettings);
                    action.Execute();
                }
            }
        }
    }
}
