using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace specAnalyzerTest.ViewModels
{
    public class BaseCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;
        private Action _action;
        private bool _enabled = true;
        public BaseCommand(Action action, bool enabled)
        {
            _enabled = enabled;
            _action = action;
        }

        public void Invert()
        {
            _enabled = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
        public void Enable()
        {
            _enabled = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return _enabled;
        }

        public virtual void Execute(object parameter)
        {
            _action.Invoke();
        }
    }
}
