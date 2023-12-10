using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace DownloadImage.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public ViewModelBase()
        {
            this.Dispatcher = Dispatcher.CurrentDispatcher;
        }
        [IgnoreDataMember]
        [JsonIgnore]
        public Dispatcher Dispatcher { get; set; }

        protected void DoOnMain(Action a)
        {
            Dispatcher.Invoke(a);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(String propertyName)
        {
            if (PropertyChanged != null)
            {
                Action a = () => { PropertyChanged(this, new PropertyChangedEventArgs(propertyName)); };
                if (Dispatcher != null)
                {
                    Dispatcher.Invoke(a);
                }
                else
                {
                    a();
                }
            }
        }

        protected void OnPropertyChanged(Expression expression)
        {
            OnPropertyChanged(((MemberExpression)expression).Member.Name);
        }

        protected bool SetValueIfChanged<T>(Expression<Func<T>> propertyExpression, Expression<Func<T>> fieldExpression, object value)
        {
            var property = (PropertyInfo)((MemberExpression)propertyExpression.Body).Member;
            var field = (FieldInfo)((MemberExpression)fieldExpression.Body).Member;
            return SetValueIfChanged(property, field, value);
        }

        protected bool SetValueIfChanged(PropertyInfo pi, FieldInfo fi, object value)
        {
            var currentValue = pi.GetValue(this, new object[] { });
            if ((currentValue == null && value == null) || (currentValue != null && currentValue.Equals(value)))
                return false;
            fi.SetValue(this, value);
            OnPropertyChanged(pi.Name);
            return true;
        }
    }
}
