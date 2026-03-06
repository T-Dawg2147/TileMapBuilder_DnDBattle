using DnDBattle.Data.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Data.Services.UndoRedo
{
    public class TilePropertyChangeAction : IUndoableAction
    {
        private readonly object _target;
        private readonly string _propertyName;
        private readonly object? _oldValue;
        private readonly object? _newValue;

        public string Description => $"Change {_propertyName}";

        public TilePropertyChangeAction(object target, string propertyName, object? oldValue, object? newValue)
        {
            _target = target;
            _propertyName = propertyName;
            _oldValue = oldValue;
            _newValue = newValue;
        }

        public void Do()
        {
            SetPropertyValue(_newValue);
        }

        public void Undo()
        {
            SetPropertyValue(_oldValue);
        }

        private void SetPropertyValue(object? value)
        {
            var prop = _target.GetType().GetProperty(_propertyName);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(_target, value);
            }
        }
    }
}
