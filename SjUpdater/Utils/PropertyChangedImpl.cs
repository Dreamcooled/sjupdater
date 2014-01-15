using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace SjUpdater.Utils
{
    public abstract class PropertyChangedImpl : INotifyPropertyChanged
    {
        #region Constructor

        readonly Dispatcher _dispatcher;
        protected PropertyChangedImpl()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
        }

        #endregion // Constructor

  
        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;
                Debug.Fail(msg);
            }
        }



        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler == null) return;
            PropertyChangedEventArgs e = new PropertyChangedEventArgs(propertyName);

           // if (_dispatcher == null || _dispatcher.CheckAccess())
                handler(this, e);
           // else
             //   _dispatcher.BeginInvoke(new Action(() => handler(this, e)));
        }

        #endregion // INotifyPropertyChanged Members

   
    }
}