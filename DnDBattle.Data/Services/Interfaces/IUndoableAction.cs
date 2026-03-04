using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DnDBattle.Data.Services.Interfaces
{
    public interface IUndoableAction
    {
        void Do();
        void Undo();
        string Description { get; }
    }
}
