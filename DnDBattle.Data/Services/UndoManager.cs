using DnDBattle.Data.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Data.Services
{
    public class UndoManager
    {
        private readonly LinkedList<IUndoableAction> _undoList = [];
        private readonly Stack<IUndoableAction> _redo = [];

        public event EventHandler? StateChanged;

        public readonly int Limit = 100;

        public bool CanUndo => _undoList.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Record(IUndoableAction action, bool performNow = true)
        {
            if (action == null) return;
            if (performNow) action.Do();
            _undoList.AddLast(action);

            while (_undoList.Count > Limit) 
                _undoList.RemoveFirst();

            _redo.Clear();
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public void Undo()
        {
            if (_undoList.Count == 0) return;
            var act = _undoList.Last!.Value;
            _undoList.RemoveLast();
            try { act.Undo(); }
            catch { }
            _redo.Push(act);
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public void Redo()
        {
            if (_redo.Count == 0) return;
            var act = _redo.Pop();
            try { act.Do(); }
            catch { }
            _undoList.AddLast(act);
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public void Clear()
        {
            _undoList.Clear();
            _redo.Clear();
            StateChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
