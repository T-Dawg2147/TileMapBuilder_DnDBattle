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
        private static readonly Stack<IUndoableAction> _undo = [];
        private static readonly Stack<IUndoableAction> _redo = [];

        public static event EventHandler StateChanged;

        public static readonly int Limit = 100;

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Record(IUndoableAction action, bool performNow = true)
        {
            if (action == null) return;
            if (performNow) action.Do();
            _undo.Push(action);

            while (_undo.Count > Limit) _undo.Pop();
            _redo.Clear();
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public void Undo()
        {
            if (_undo.Count == 0) return;
            var act = _undo.Pop();
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
            _undo.Push(act);
            StateChanged?.Invoke(null, EventArgs.Empty);
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
            StateChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
