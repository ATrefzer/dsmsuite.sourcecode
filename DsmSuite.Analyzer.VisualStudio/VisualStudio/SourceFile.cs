﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

namespace DsmSuite.Analyzer.VisualStudio.VisualStudio
{
    public class SourceFile
    {
        private readonly FileInfo _sourceFileInfo;
        private readonly HashSet<string> _includes;
        private readonly IEnumerable<string> _forcedIncludes;
        private readonly IncludeResolveStrategy _includeResolveStrategy;
        private string _checksum;

        public SourceFile(string filename)
        {
            ProjectFolder = "";
            _sourceFileInfo = new FileInfo(filename);
            _includes = new HashSet<string>();
        }

        public SourceFile(FileInfo sourceFileInfo, string projectProjectFolder, IEnumerable<string> forcedIncludes, IncludeResolveStrategy includeResolveStrategy)
        {
            ProjectFolder = projectProjectFolder;
            _forcedIncludes = forcedIncludes;
            _sourceFileInfo = sourceFileInfo;
            _includes = new HashSet<string>();
            _checksum = null;
            _includeResolveStrategy = includeResolveStrategy;
        }

        public FileInfo SourceFileInfo => _sourceFileInfo;

        public string Name => _sourceFileInfo.FullName;

        public string FileType => (_sourceFileInfo.Extension.Length > 0) ? _sourceFileInfo.Extension.Substring(1) : "";

        public string ProjectFolder { get; }

        public string Checksum => _checksum ?? (_checksum = DetermineId(_sourceFileInfo)); // Calculated once when needed

        public ICollection<string> Includes => _includes;
      
        public void Analyze()
        {
            _includes.Clear();

            if (_forcedIncludes != null)
            {
                foreach (string forcedInclude in _forcedIncludes)
                {
                    string absoluteIncludeFilename = _includeResolveStrategy.Resolve(_sourceFileInfo.FullName, forcedInclude);
                    if (absoluteIncludeFilename != null)
                    {
                        _includes.Add(absoluteIncludeFilename);
                    }
                }
            }

            if (_sourceFileInfo.Exists && (_includeResolveStrategy != null))
            {
                using (FileStream stream = _sourceFileInfo.OpenRead())
                {
                    StreamReader sr = new StreamReader(stream);
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        string relativeIncludeFilename = ExtractFileFromIncludeStatement(line);

                        if (relativeIncludeFilename != null)
                        {
                            string absoluteIncludeFilename = _includeResolveStrategy.Resolve(_sourceFileInfo.FullName, relativeIncludeFilename);
                            if (absoluteIncludeFilename != null)
                            {
                                _includes.Add(absoluteIncludeFilename);
                            }
                        }
                    }
                }
            }
        }

        private string ExtractFileFromIncludeStatement(string line)
        {
            string includedFilename = null;

            Regex regex = new Regex("#[ ]{0,}include");
            Match match = regex.Match(line);
            if (match.Success)
            {
                includedFilename = line.Substring(match.Value.Length+1).Replace("\"", "").Replace(">", "").Replace("<", "");
            }

            return includedFilename;
        }

        private string DetermineId(FileInfo fileInfo)
        {
            // Checksum is based on filename and content to be able to detect duplicate files
            return fileInfo.Name + "." + Calculate(fileInfo.FullName);
        }

        private static string Calculate(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    var hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }
    }
}
