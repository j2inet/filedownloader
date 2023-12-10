using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;

namespace DownloadImage.ViewModels
{
    public class DelegateCommand : ICommand
    {
        Dispatcher Dispatcher = Dispatcher.CurrentDispatcher;
        public DelegateCommand(Action execute)
            : this(execute, null)
        {
        }

        public DelegateCommand(Action execute, Func<object, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
                return _canExecute(parameter);
            return true;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
            {
                Dispatcher.Invoke(() => CanExecuteChanged(this, EventArgs.Empty));
            }
        }

        public event EventHandler CanExecuteChanged;

        private Action _execute;
        private Func<object, bool> _canExecute;
    }


    public class DelegateCommand<T> : ICommand
    {
        public DelegateCommand(Action<T> execute)
            : this(execute, null)
        {
        }

        public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute != null)
            {

                return _canExecute((T)parameter);
            }

            return true;
        }

        public void Execute(object parameter)
        {
            _execute((T)parameter);
        }

        public void RaiseCanExecuteChanged()
        {
            if (CanExecuteChanged != null)
                CanExecuteChanged(this, EventArgs.Empty);
        }

        public event EventHandler CanExecuteChanged;

        private Action<T> _execute;
        private Func<T, bool> _canExecute;
    }
}
