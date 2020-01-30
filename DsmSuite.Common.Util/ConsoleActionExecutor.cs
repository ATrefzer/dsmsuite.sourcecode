﻿using System;
using System.Diagnostics;
using System.Text;

namespace DsmSuite.Common.Util
{
    public class ConsoleActionExecutor<T>
    {
        private readonly string _description;
        private readonly T _settings;
        private int _progress;
        private string _currentText = string.Empty;
        private readonly Stopwatch _stopWatch;

        public delegate void ConsoleAction(T settings, IProgress<ProgressInfo> progres);

        public ConsoleActionExecutor(string description, T settings)
        {
            _description = description;
            _settings = settings;
            _progress = 0;
            _currentText = string.Empty;
            _stopWatch = new Stopwatch();
        }

        public void Execute(ConsoleAction action)
        {
            Logger.LogUserMessage(_description);
            Logger.LogUserMessage(new String('-', _description.Length));
            Progress<ProgressInfo> progress = new Progress<ProgressInfo>(UpdateProgress);

            StartTimer();
            action(_settings, progress);
            StopTimer();
            Logger.LogUserMessage($"Total elapsed time={ElapsedTime}");
            Logger.LogResourceUsage();
        }

        private void StartTimer()
        {
            _stopWatch.Start();
        }

        public void StopTimer()
        {
            _stopWatch.Stop();
        }

        public TimeSpan ElapsedTime
        {
            get { return _stopWatch.Elapsed; }
        }

        private void UpdateProgress(ProgressInfo progress)
        {
            if (progress.Percentage.HasValue)
            {
                UpdateProgressWithPercentage(progress);
            }
            else
            {
                UpdateProgressWithoutPercentage(progress);
            }
        }

        private void UpdateProgressWithPercentage(ProgressInfo progress)
        {
            Debug.Assert(progress.Percentage.HasValue);

            if (_progress != progress.Percentage)
            {
                _progress = progress.Percentage.Value;
                string text = $"{progress.ActionText} {progress.CurrentItemCount}/{progress.TotalItemCount} {progress.ItemType} progress={progress.Percentage}%";
                UpdateText(text, progress.Done);
            }
        }

        private void UpdateProgressWithoutPercentage(ProgressInfo progress)
        {
            string text = $"{progress.ActionText} {progress.CurrentItemCount} {progress.ItemType}";
            UpdateText(text, progress.Done);
        }

        private void UpdateText(string text, bool done)
        {
            if (!Console.IsOutputRedirected)
            {
                StringBuilder outputBuilder = new StringBuilder(text);
                int overlapCount = _currentText.Length - text.Length;
                if (overlapCount > 0)
                {

                    outputBuilder.Append(' ', overlapCount);
                    outputBuilder.Append('\b', overlapCount);
                    Console.Write(outputBuilder);
                }

                if (done)
                {
                    Console.Write("\r" + outputBuilder);
                    Console.WriteLine("\r" + text);
                }
                else
                {
                    Console.Write("\r" + outputBuilder);
                    Console.Write("\r" + text);
                }
                _currentText = text;
            }
        }
    }
}