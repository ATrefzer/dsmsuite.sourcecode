﻿using System;
using System.Collections.Generic;
using System.IO;
using DsmSuite.Analyzer.Model.Interface;
using DsmSuite.Analyzer.VisualStudio.Settings;
using DsmSuite.Analyzer.VisualStudio.VisualStudio;
using DsmSuite.Common.Util;
using DsmSuite.Analyzer.DotNet.Lib;
using DsmSuite.Analyzer.VisualStudio.Utils;

namespace DsmSuite.Analyzer.VisualStudio.Analysis
{
    public class Analyzer
    {
        private readonly IDsiModel _model;
        private readonly AnalyzerSettings _analyzerSettings;
        private readonly SolutionFile _solutionFile;
        private readonly Dictionary<string, ProjectFileBase> _projectForSourceFilePath = new Dictionary<string, ProjectFileBase>();
        private readonly Dictionary<string, FileInfo> _sourcesFilesByChecksum = new Dictionary<string, FileInfo>();
        private readonly Dictionary<string, string> _interfaceFileChecksumsByFilePath = new Dictionary<string, string>();
        private readonly IProgress<ProgressInfo> _progress;

        public Analyzer(IDsiModel model, AnalyzerSettings analyzerSettings, IProgress<ProgressInfo> progress)
        {
            _model = model;
            _analyzerSettings = analyzerSettings;
            _progress = progress;
            _solutionFile = new SolutionFile(analyzerSettings.Input.Filename, _analyzerSettings, progress);
        }

        public void Analyze()
        {
            RegisterInterfaceFiles();
            AnalyzeSolution();
            RegisterDotNetTypes();
            RegisterDotNetRelations();
            RegisterSourceFiles();
            RegisterDirectIncludeRelations();
            RegisterGeneratedFileRelations();
            WriteFoundProjects();
            AnalyzerLogger.Flush();
        }

        private void RegisterInterfaceFiles()
        {
            foreach (string interfaceDirectory in _analyzerSettings.Input.InterfaceIncludeDirectories)
            {
                DirectoryInfo interfaceDirectoryInfo = new DirectoryInfo(interfaceDirectory);
                RegisterInterfaceFiles(interfaceDirectoryInfo);
            }
        }

        private void RegisterInterfaceFiles(DirectoryInfo interfaceDirectoryInfo)
        {
            if (interfaceDirectoryInfo.Exists)
            {
                foreach (FileInfo interfaceFile in interfaceDirectoryInfo.EnumerateFiles())
                {
                    if (interfaceFile.Exists)
                    {
                        SourceFile sourceFile = new SourceFile(interfaceFile.FullName);
                        _interfaceFileChecksumsByFilePath[interfaceFile.FullName.ToLower()] = sourceFile.Checksum;
                    }
                    else
                    {
                        Logger.LogError($"Interface file {interfaceFile.FullName} does not exist");
                    }
                }

                foreach (DirectoryInfo subDirectoryInfo in interfaceDirectoryInfo.EnumerateDirectories())
                {
                    RegisterInterfaceFiles(subDirectoryInfo);
                }
            }
            else
            {
                Logger.LogError($"Interface directory {interfaceDirectoryInfo.FullName} does not exist");
            }
        }

        private void AnalyzeSolution()
        {
            _solutionFile.Analyze();
        }

        private void RegisterDotNetTypes()
        {
            foreach (ProjectFileBase visualStudioProject in _solutionFile.Projects)
            {
                foreach (DotNetType type in visualStudioProject.DotNetTypes)
                {
                    RegisterDotNetType(visualStudioProject, type);
                }
            }
        }
        
        private void RegisterDotNetRelations()
        {
            foreach (ProjectFileBase visualStudioProject in _solutionFile.Projects)
            {
                foreach (DotNetRelation relation in visualStudioProject.DotNetRelations)
                {
                    RegisterDotNetRelation(visualStudioProject, relation);
                }
            }
        }

        private void RegisterDotNetType(ProjectFileBase visualStudioProject, DotNetType type)
        {
            string name = GetDotNetTypeName(visualStudioProject, type.Name);
            _model.AddElement(name, type.Type, "");
        }

        private void RegisterDotNetRelation(ProjectFileBase visualStudioProject, DotNetRelation relation)
        {
            string consumerName = GetDotNetTypeName(visualStudioProject, relation.ConsumerName);
            string providerName = GetDotNetTypeName(visualStudioProject, relation.ProviderName);
            _model.AddRelation(consumerName, providerName, relation.Type, 1, null);
        }

        private void RegisterSourceFiles()
        {
            int processedSourceFiles = 0;
            foreach (ProjectFileBase visualStudioProject in _solutionFile.Projects)
            {
                foreach (SourceFile sourceFile in visualStudioProject.SourceFiles)
                {
                    processedSourceFiles++;
                    UpdateSourceFileProgress("Registering source files", processedSourceFiles, _solutionFile.TotalSourceFiles);

                    RegisterSourceFile(visualStudioProject, sourceFile);
                    _projectForSourceFilePath[sourceFile.SourceFileInfo.FullName.ToLower()] = visualStudioProject;
                }
            }
        }

        private void RegisterSourceFile(ProjectFileBase visualStudioProject, SourceFile sourceFile)
        {
            Logger.LogInfo("Source file registered: " + sourceFile.Name);

            string type = sourceFile.FileType;

            if (_interfaceFileChecksumsByFilePath.Count > 0)
            {
                _sourcesFilesByChecksum[sourceFile.Checksum] = sourceFile.SourceFileInfo;
            }

            if (sourceFile.SourceFileInfo.Exists)
            {
                AnalyzerLogger.LogFileFoundInVisualStudioProject(sourceFile.Name, visualStudioProject.ProjectName);

                switch (_analyzerSettings.Analysis.ViewMode)
                {
                    case ViewMode.SolutionView:
                    {
                        string name = GetSolutionViewName(visualStudioProject, sourceFile);
                        _model.AddElement(name, type, sourceFile.SourceFileInfo.FullName);
                        break;
                    }
                    case ViewMode.DirectoryView:
                    {
                        string name = GetDirectoryViewName(sourceFile);
                        _model.AddElement(name, type, sourceFile.SourceFileInfo.FullName);
                        break;
                    }
                    default:
                        Logger.LogError("Unknown view mode");
                        break;
                }
            }
            else
            {
                AnalyzerLogger.LogErrorFileNotFound(sourceFile.Name, visualStudioProject.ProjectName);
            }
        }

        private void RegisterDirectIncludeRelations()
        {
            int processedSourceFiles = 0;
            foreach (ProjectFileBase visualStudioProject in _solutionFile.Projects)
            {
                foreach (SourceFile sourceFile in visualStudioProject.SourceFiles)
                {
                    processedSourceFiles++;
                    UpdateSourceFileProgress("Registering source file includes", processedSourceFiles, _solutionFile.TotalSourceFiles);

                    foreach (string includedFile in sourceFile.Includes)
                    {
                        RegisterDirectIncludeRelation(visualStudioProject, sourceFile, includedFile);
                    }
                }
            }
        }

        private void RegisterDirectIncludeRelation(ProjectFileBase visualStudioProject, SourceFile sourceFile, string includedFile)
        {
            Logger.LogInfo("Include relation registered: " + sourceFile.Name + " -> " + includedFile);

            switch (_analyzerSettings.Analysis.ViewMode)
            {
                case ViewMode.SolutionView:
                    RegisterDirectIncludeRelationSolutionView(visualStudioProject, sourceFile, includedFile);
                    break;
                case ViewMode.DirectoryView:
                    RegisterDirectIncludeRelationDirectoryView(sourceFile, includedFile);
                    break;
                default:
                    Logger.LogError("Unknown view mode");
                    break;
            }
        }

        private void RegisterDirectIncludeRelationSolutionView(ProjectFileBase visualStudioProject, SourceFile sourceFile, string includedFile)
        {
            string consumerName = GetSolutionViewName(visualStudioProject, sourceFile);
            if (consumerName != null)
            {
                if (IsProjectInclude(includedFile))
                {
                    // Register as normal visual studio project include
                    RegisterIncludeRelationSolutionView(consumerName, includedFile);
                }
                else if (IsInterfaceInclude(includedFile))
                {
                    // Interface includes must be clones of includes files in other visual studio projects
                    string resolvedIncludedFile = ResolveInterfaceFileSolutionView(includedFile, sourceFile);
                    if (resolvedIncludedFile != null)
                    {
                        // Register resolved interface as normal visual studio project include
                        // if source of clone in visual studio project found
                        RegisterIncludeRelationSolutionView(consumerName, resolvedIncludedFile);
                    }
                    else
                    {
                        // Skip not resolved interface
                        _model.SkipRelation(consumerName, includedFile, "include");
                    }
                }
                else if (IsExternalInclude(includedFile))
                {
                    // Register external include
                    RegisterExternalIncludeRelationSolutionView(includedFile, consumerName);
                }
                else if (IsSystemInclude(includedFile))
                {
                    // Ignore system include
                }
                else
                {
                    // Skip not resolved interface
                    AnalyzerLogger.LogIncludeFileNotFoundInVisualStudioProject(includedFile, visualStudioProject.ProjectName);
                    _model.SkipRelation(consumerName, includedFile, "include");
                }
            }
        }

        private string ResolveInterfaceFileSolutionView(string includedFile, SourceFile sourceFile)
        {
            // Interface files can be clones of source files found in visual studio projects 
            string resolvedIncludedFile = null;
            if (_interfaceFileChecksumsByFilePath.ContainsKey(includedFile.ToLower()))
            {
                string checksum = _interfaceFileChecksumsByFilePath[includedFile.ToLower()];
                if (_sourcesFilesByChecksum.ContainsKey(checksum))
                {
                    resolvedIncludedFile = _sourcesFilesByChecksum[checksum].FullName;
                    Logger.LogInfo("Included interface resolved: " + sourceFile.Name + " -> " + includedFile + " -> " + resolvedIncludedFile);
                }
            }
            return resolvedIncludedFile;
        }

        private void RegisterIncludeRelationSolutionView(string consumerName, string includedFile)
        {
            string caseInsensitiveFilename = includedFile.ToLower();
            if (_projectForSourceFilePath.ContainsKey(caseInsensitiveFilename))
            {
                ProjectFileBase projectFile = _projectForSourceFilePath[caseInsensitiveFilename];
                SourceFile includeFile = projectFile.GetSourceFile(caseInsensitiveFilename);
                string providerName = GetSolutionViewName(projectFile, includeFile);
                _model.AddRelation(consumerName, providerName, "include", 1, null);
            }
            else
            {
                _model.SkipRelation(consumerName, includedFile, "include");
            }
        }

        private void RegisterExternalIncludeRelationSolutionView(string includedFile, string consumerName)
        {
            // Add include element to model
            SourceFile includedSourceFile = new SourceFile(includedFile);
            string providerName = GetExternalName(includedSourceFile);
            string type = includedSourceFile.FileType;
            _model.AddElement(providerName, type, includedFile);

            // Add relation to model
            _model.AddRelation(consumerName, providerName, "include", 1, "include file is an external include");
        }

        private void RegisterDirectIncludeRelationDirectoryView(SourceFile sourceFile, string includedFile)
        {
            SourceFile includedSourceFile = new SourceFile(includedFile);
            string consumerName = GetDirectoryViewName(sourceFile);
            string providerName = GetDirectoryViewName(includedSourceFile);
            string type = includedSourceFile.FileType;
            _model.AddElement(providerName, type, includedFile);
            _model.AddRelation(consumerName, providerName, "include", 1, null);
        }
        
        private void RegisterGeneratedFileRelations()
        {
            foreach (ProjectFileBase visualStudioProject in _solutionFile.Projects)
            {
                foreach (GeneratedFileRelation relation in visualStudioProject.GeneratedFileRelations)
                {
                    Logger.LogInfo("Generated file relation registered: " + relation.Consumer.Name + " -> " + relation.Provider.Name);

                    switch (_analyzerSettings.Analysis.ViewMode)
                    {
                        case ViewMode.SolutionView:
                            {
                                string consumerName = GetSolutionViewName(visualStudioProject, relation.Consumer);
                                string providerName = GetSolutionViewName(visualStudioProject, relation.Provider);
                                _model.AddRelation(consumerName, providerName, "generated", 1, null);
                                break;
                            }
                        case ViewMode.DirectoryView:
                            {
                                string consumerName = GetDirectoryViewName(relation.Consumer);
                                string providerName = GetDirectoryViewName(relation.Provider);
                                _model.AddRelation(consumerName, providerName, "generated", 1, null);
                                break;
                            }
                        default:
                            Logger.LogError("Unknown view mode");
                            break;
                    }
                }
            }
        }
        
        private bool IsProjectInclude(string includedFile)
        {
            return _projectForSourceFilePath.ContainsKey(includedFile.ToLower());
        }

        private bool IsSystemInclude(string includedFile)
        {
            bool isSystemInclude = false;

            foreach (string systemIncludeDirectory in _analyzerSettings.Input.SystemIncludeDirectories)
            {
                if (includedFile.StartsWith(systemIncludeDirectory))
                {
                    isSystemInclude = true;
                }
            }
            return isSystemInclude;
        }

        private bool IsExternalInclude(string includedFile)
        {
            bool isExternalInclude = false;

            foreach (ExternalIncludeDirectory externalIncludeDirectory in _analyzerSettings.Input.ExternalIncludeDirectories)
            {
                if (includedFile.StartsWith(externalIncludeDirectory.Path))
                {
                    isExternalInclude = true;
                }
            }

            return isExternalInclude;
        }

        private bool IsInterfaceInclude(string includedFile)
        {
            bool isInterfaceInclude = false;

            foreach (string interfaceIncludeDirectory in _analyzerSettings.Input.InterfaceIncludeDirectories)
            {
                if (includedFile.StartsWith(interfaceIncludeDirectory))
                {
                    isInterfaceInclude = true;
                }
            }
            return isInterfaceInclude;
        }

        private string GetDotNetTypeName(ProjectFileBase visualStudioProject, string typeName)
        {
            string name = "";

            if (!string.IsNullOrEmpty(_solutionFile?.Name))
            {
                name += _solutionFile.Name;
                name += ".";
            }

            if (visualStudioProject != null)
            {
                if (!string.IsNullOrEmpty(visualStudioProject.SolutionFolder))
                {
                    name += visualStudioProject.SolutionFolder;
                    name += ".";
                }

                if (!string.IsNullOrEmpty(visualStudioProject.ProjectName))
                {
                    name += visualStudioProject.ProjectName;
                }

                if (!string.IsNullOrEmpty(visualStudioProject.TargetExtension))
                {
                    name += " (";
                    name += visualStudioProject.TargetExtension;
                    name += ")";
                }

                if (!string.IsNullOrEmpty(visualStudioProject.ProjectName))
                {
                    name += ".";
                }
            }

            name += typeName;

            return name.Replace("\\", ".");
        }

        private void WriteFoundProjects()
        {
            foreach (ProjectFileBase project in _solutionFile.Projects)
            {
                string projectName = GetSolutionViewName(project, null);
                string status = project.Success ? "ok" : "failed";
                AnalyzerLogger.LogProjectStatus(projectName, status);
            }
        }

        private string GetSolutionViewName(ProjectFileBase visualStudioProject, SourceFile sourceFile)
        {
            string name = "";

            if (!string.IsNullOrEmpty(_solutionFile?.Name))
            {
                name += _solutionFile.Name;
                name += ".";
            }

            if (visualStudioProject != null)
            {
                if (!string.IsNullOrEmpty(visualStudioProject.SolutionFolder))
                {
                    name += visualStudioProject.SolutionFolder;
                    name += ".";
                }

                if (!string.IsNullOrEmpty(visualStudioProject.ProjectName))
                {
                    name += visualStudioProject.ProjectName;
                }

                if (!string.IsNullOrEmpty(visualStudioProject.TargetExtension))
                {
                    name += " (";
                    name += visualStudioProject.TargetExtension;
                    name += ")";
                }

                if (!string.IsNullOrEmpty(visualStudioProject.ProjectName))
                {
                    name += ".";
                }
            }

            if (sourceFile != null)
            {
                if (!string.IsNullOrEmpty(sourceFile.ProjectFolder))
                {
                    name += sourceFile.ProjectFolder;
                    name += ".";
                }

                if (!string.IsNullOrEmpty(sourceFile.SourceFileInfo.Name))
                {
                    name += sourceFile.SourceFileInfo.Name;
                }
            }

            return name.Replace("\\", ".");
        }

        private string GetExternalName(SourceFile sourceFile)
        {
            string usedExternalIncludeDirectory = null;
            string resolveAs = null;
            foreach (ExternalIncludeDirectory externalIncludeDirectory in _analyzerSettings.Input.ExternalIncludeDirectories)
            {
                if (sourceFile.SourceFileInfo.FullName.StartsWith(externalIncludeDirectory.Path))
                {
                    usedExternalIncludeDirectory = externalIncludeDirectory.Path;
                    resolveAs = externalIncludeDirectory.ResolveAs;
                }
            }

            string name = null;

            if ((usedExternalIncludeDirectory != null) &&
                (resolveAs != null))
            {
                name = sourceFile.SourceFileInfo.FullName.Replace(usedExternalIncludeDirectory, resolveAs).Replace("\\", ".");
            }

            return name;
        }

        private string GetDirectoryViewName(SourceFile sourceFile)
        {
            string name = "";

            string rootDirectory = _analyzerSettings.Input.RootDirectory.Trim('\\'); // Ensure without trailing \
            if (sourceFile.SourceFileInfo.FullName.StartsWith(rootDirectory))
            {
                int start = rootDirectory.Length + 1;
                name = sourceFile.SourceFileInfo.FullName.Substring(start).Replace("\\", ".");
            }

            return name;
        }

        private void UpdateSourceFileProgress(string text, int currentItemCount, int totalItemCount)
        {
            ProgressInfo progressInfo = new ProgressInfo();
            progressInfo.ActionText = text;
            progressInfo.CurrentItemCount = currentItemCount;
            progressInfo.TotalItemCount = totalItemCount;
            progressInfo.ItemType = "files";
            progressInfo.Percentage = currentItemCount * 100 / totalItemCount;
            progressInfo.Done = currentItemCount == totalItemCount;
            _progress?.Report(progressInfo);
        }
    }
}
