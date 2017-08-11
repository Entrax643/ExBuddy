using System;
using System.Threading;

namespace ExBuddy.Helpers
{
    /// <summary>
    /// Lazy loads an instance of type <see cref="T"/> similar to the <see cref="Lazy{T}"/> class. Provides
    /// an option to reset the initialization execution status if the initialization returns a null value.
    /// </summary>
    public class Lazier<T>
        where T : class
    {
        private readonly object syncLock = new object();

        protected readonly LazyThreadSafetyMode safetyMode;
        protected readonly Func<T> init;
        protected readonly bool resetOnNull;

        private T value;
        private Exception exception;
        
        /// <summary>
        /// Instantiates a new instance of the <see cref="Lazier{T}"/> class.
        /// </summary>
        /// <param name="initializationFunction"></param>
        /// <param name="safetyMode"></param>
        /// <param name="resetOnNull"></param>
        public Lazier(Func<T> initializationFunction, LazyThreadSafetyMode safetyMode = LazyThreadSafetyMode.ExecutionAndPublication, bool resetOnNull = false)
        {
            this.safetyMode = safetyMode;
            this.init = initializationFunction;
            this.resetOnNull = resetOnNull;
        }
        
        /// <summary>
        /// Gets the value returned by the initialization Func. If Func returns null, this instance
        /// is not considered initialized.
        /// </summary>
        public T Value
        {
            get
            {
                if (this.IsValueCreated) return this.value;
                if (this.exception != null) throw exception;
                
                if (this.safetyMode == LazyThreadSafetyMode.ExecutionAndPublication)
                {
                    lock (this.syncLock)
                    {
                        if (this.IsValueCreated) return this.value;
                        if (this.exception != null) throw this.exception;

                        try
                        {
                            var initRtn = this.init();

                            if (this.resetOnNull && initRtn == null) return null;

                            this.value = initRtn;
                            this.IsValueCreated = true;

                            return this.value;
                        }
                        catch (Exception e)
                        {
                            this.exception = e;
                            throw;
                        }
                    }
                }

                if (safetyMode == LazyThreadSafetyMode.PublicationOnly)
                {
                    T initRtn = null;
                    Exception initEx = null;
                    try
                    {
                        initRtn = init();
                    }
                    catch (Exception e)
                    {
                        initEx = e;
                    }

                    lock (syncLock)
                    {
                        if (this.IsValueCreated) return this.value;
                        if (this.exception != null) throw this.exception;

                        if (initEx != null)
                        {
                            this.exception = initEx;
                            throw initEx;
                        }

                        if (this.resetOnNull && initRtn == null) return null;

                        this.value = initRtn;
                        this.IsValueCreated = true;

                        return this.value;
                    }
                }

                // LazyThreadSafetyMode.None
                try
                {
                    this.value = init();

                    if (this.resetOnNull && this.value == null) return null;

                    this.IsValueCreated = true;

                    return this.value;
                }
                catch (Exception e)
                {
                    this.exception = e;
                    throw;
                }
            }
        }

        /// <summary>
        /// Resets the initialization status.
        /// </summary>
        public void ResetValue()
        {
            if (safetyMode != LazyThreadSafetyMode.None)
            {
                lock (syncLock)
                {
                    this.value = null;
                    this.exception = null;
                }
            }
            else
            {
                this.value = null;
                this.exception = null;
            }
        }

        /// <summary>
        /// Gets whether the object has been initialized.
        /// </summary>
        public bool IsValueCreated
        {
            get;
            protected set;
        }
    }
}
