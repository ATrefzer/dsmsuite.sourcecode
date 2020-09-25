﻿using DsmSuite.DsmViewer.Application.Actions.Base;
using DsmSuite.DsmViewer.Application.Interfaces;
using DsmSuite.DsmViewer.Model.Interfaces;
using System.Collections.Generic;
using System.Diagnostics;

namespace DsmSuite.DsmViewer.Application.Actions.Element
{
    public class ElementChangeParentAction : IAction
    {
        private readonly IDsmModel _model;
        private readonly IDsmElement _element;
        private readonly IDsmElement _old;
        private readonly int _oldIndex;
        private readonly IDsmElement _new;
        private readonly int _newIndex;

        public const ActionType RegisteredType = ActionType.ElementChangeParent;

        public ElementChangeParentAction(object[] args)
        {
            Debug.Assert(args.Length == 2);
            _model = args[0] as IDsmModel;
            Debug.Assert(_model != null);
            IReadOnlyDictionary<string, string> data = args[1] as IReadOnlyDictionary<string, string>;
            Debug.Assert(data != null);

            ActionReadOnlyAttributes attributes = new ActionReadOnlyAttributes(_model, data);
            _element = attributes.GetElement(nameof(_element));
            Debug.Assert(_element != null);

            _old = attributes.GetElement(nameof(_old));
            Debug.Assert(_old != null);

            _oldIndex = attributes.GetInt(nameof(_oldIndex));

            _new = attributes.GetElement(nameof(_new));
            Debug.Assert(_new != null);

            _newIndex = attributes.GetInt(nameof(_newIndex));
        }

        public ElementChangeParentAction(IDsmModel model, IDsmElement element, IDsmElement newParent, int index)
        {
            _model = model;
            Debug.Assert(_model != null);

            _element = element;
            Debug.Assert(_element != null);

            _old = element.Parent;
            Debug.Assert(_old != null);

            _oldIndex = _old.IndexOfChild(element);

            _new = newParent;
            Debug.Assert(_new != null);

            _newIndex = index;
        }

        public ActionType Type => RegisteredType;
        public string Title => "Change element parent";
        public string Description => $"element={_element.Fullname} parent={_old.Fullname}->{_new.Fullname}";

        public object Do()
        {
            _model.ChangeElementParent(_element, _new, _newIndex);
            _model.AssignElementOrder();
            return null;
        }

        public void Undo()
        {
            _model.ChangeElementParent(_element, _old, _oldIndex);
            _model.AssignElementOrder();
        }

        public IReadOnlyDictionary<string, string> Data
        {
            get
            {
                ActionAttributes attributes = new ActionAttributes();
                attributes.SetInt(nameof(_element), _element.Id);
                attributes.SetInt(nameof(_old), _old.Id);
                attributes.SetInt(nameof(_oldIndex), _oldIndex);
                attributes.SetInt(nameof(_new), _new.Id);
                attributes.SetInt(nameof(_newIndex), _newIndex);
                return attributes.Data;
            }
        }
    }
}
