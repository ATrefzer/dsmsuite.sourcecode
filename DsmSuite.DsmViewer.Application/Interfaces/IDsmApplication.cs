﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DsmSuite.DsmViewer.Model.Interfaces;
using DsmSuite.Common.Util;

namespace DsmSuite.DsmViewer.Application.Interfaces
{
    public interface IDsmApplication
    {
        event EventHandler<bool> Modified;
        event EventHandler ActionPerformed;

        Task AsyncImportDsiModel(string dsiFilename, string dsmFilename, bool autoPartition, bool recordChanges, bool compressDsmFile, IProgress<ProgressInfo> progress);
        Task AsyncImportGraphVizModel(string dsiFilename, string dsmFilename, bool autoPartition, bool recordChanges, bool compressDsmFile, IProgress<ProgressInfo> progress);
        Task AsyncImportGraphMlModel(string dsiFilename, string dsmFilename, bool autoPartition, bool recordChanges, bool compressDsmFile, IProgress<ProgressInfo> progress);


        bool ShowCycles { get; set; }
        bool CaseSensitiveSearch { get; set; }
        void ImportDsiModel(string dsiFilename, string dsmFilename, bool applyPartitionAlgorithm, bool overwriteDsmFile, bool compressDsmFile, IProgress<ProgressInfo> progress);
        void ImportGraphVizModel(string dotFilename, string dsmFilename, bool applyPartitionAlgorithm, bool overwriteDsmFile, bool compressDsmFile, IProgress<ProgressInfo> progress);
        void ImportGraphMlModel(string graphMlFilename, string dsmFilename, bool applyPartitionAlgorithm, bool overwriteDsmFile, bool compressDsmFile, IProgress<ProgressInfo> progress);
        Task OpenModel(string dsmFilename, Progress<ProgressInfo> progress);
        Task SaveModel(string dsmFilename, Progress<ProgressInfo> progress);
        bool IsModified { get; }
        bool CanUndo();
        string GetUndoActionDescription();
        void Undo();
        bool CanRedo();
        string GetRedoActionDescription();
        void Redo();
        IDsmElement RootElement { get; }
        IEnumerable<IDsmElement> GetElementProvidedElements(IDsmElement element);
        IEnumerable<IDsmElement> GetElementProviders(IDsmElement element);
        IEnumerable<IDsmRelation> FindResolvedRelations(IDsmElement consumer, IDsmElement provider);
        IEnumerable<IDsmRelation> FindRelations(IDsmElement consumer, IDsmElement provider);
        IEnumerable<IDsmRelation> FindIngoingRelations(IDsmElement element);
        IEnumerable<IDsmRelation> FindOutgoingRelations(IDsmElement element);
        IEnumerable<IDsmRelation> FindInternalRelations(IDsmElement element);
        IEnumerable<IDsmRelation> FindExternalRelations(IDsmElement element);
        IEnumerable<IDsmElement> GetRelationProviders(IDsmElement consumer, IDsmElement provider);
        IEnumerable<IDsmElement> GetElementConsumers(IDsmElement element);
        IEnumerable<IDsmElement> GetRelationConsumers(IDsmElement consumer, IDsmElement provider);
        int GetHierarchicalCycleCount(IDsmElement element);
        int GetSystemCycleCount(IDsmElement element);
        bool IsFirstChild(IDsmElement element);
        bool IsLastChild(IDsmElement element);
        IDsmElement NextSibling(IDsmElement element);
        IDsmElement PreviousSibling(IDsmElement element);
        bool HasChildren(IDsmElement element);
        void Sort(IDsmElement element, string algorithm);
        IEnumerable<string> GetSupportedSortAlgorithms();
        void MoveUp(IDsmElement element);
        void MoveDown(IDsmElement element);


        int GetDependencyWeight(IDsmElement consumer, IDsmElement provider);
        int GetDirectDependencyWeight(IDsmElement consumer, IDsmElement provider);
        CycleType IsCyclicDependency(IDsmElement consumer, IDsmElement provider);
        int SearchElements(string searchText, SearchMode searchMode);
        IDsmElement GetElementByFullname(string fullname);
        void CreateElement(string name, string type, IDsmElement parent);
        void DeleteElement(IDsmElement element);
        void ChangeElementName(IDsmElement element, string name);
        void ChangeElementAnnotation(IDsmElement element, string annotation);
        void ChangeElementType(IDsmElement element, string type);
        void ChangeElementParent(IDsmElement element, IDsmElement newParent, int index);

        void CreateRelation(IDsmElement consumer, IDsmElement provider, string type, int weight);
        void DeleteRelation(IDsmRelation relation);
        void ChangeRelationType(IDsmRelation relation, string type);
        void ChangeRelationWeight(IDsmRelation relation, int weight);

        void MakeSnapshot(string name);

        IEnumerable<IAction> GetActions();
        void ClearActions();

        int GetElementSize(IDsmElement element);
        int GetElementCount();

        void AddElementAnnotation(IDsmElement element, string text);
        void AddRelationAnnotation(IDsmElement consumer, IDsmElement provider, string text);
        IDsmElementAnnotation FindElementAnnotation(IDsmElement element);
        IDsmRelationAnnotation FindRelationAnnotation(IDsmElement consumer, IDsmElement provider);
    }
}
