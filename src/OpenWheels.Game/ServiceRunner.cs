using System;
using System.Collections.Generic;

namespace OpenWheels.Game
{
    /// <summary>
    /// Manages different kinds of executable units that require updating in frequent time interval.
    /// ServiceRunner supports three kinds of services: <see cref="IUpdatable"/>, coroutines and timers.
    /// When <see cref="Update"/> is called the services are updated. ServiceRunner uses object pooling to minimize
    /// allocations. After stabilizing none of the methods exposed should generate any garbage.
    ///
    /// For registered <see cref="IUpdatable"/> implementations the <see cref="IUpdatable.Update"/> method is called.
    ///
    /// For coroutines, elapsed time is subtracted from their delay. If they don't have a delay aftwerwards the
    /// coroutine is invoked. The yielded <see cref="TimeSpan"/> is added to the delay. A coroutine should yield
    /// <see cref="TimeSpan.Zero"/> to be called again the next update.
    ///
    /// For timers if they're canceled the canceled method is called (if any) and the timer is removed. If not
    /// canceled the delta time is added to the timer's elapsed time. If the elapsed time exceeds the timer duration
    /// the timer's finish method is invoked and the timer is removed.
    ///
    /// ServiceRunner also supports running tweens. This functionality internally uses a timer.
    /// A tween is a smooth modification of a value over time from a starting value to an end value.
    /// There are overloads that do not take a starting value, instead using the value of the lerped field at that time.
    /// Some overloads also do not take a linear interpolation function, but instead generate one for the type of the
    /// tweened field. The generated implementation depends on an Add(T,T), Subtract(T,T) and Multiply(float,T) operator
    /// implementation. If any of the operators are missing the compilation of the generated Lerp function will fail.
    /// The compiled Lerp function is cached.
    ///
    /// All class instances needed by the service runner are retrieved from their respective shared <see cref="ObjectPool{T}"/>.false
    /// </summary>
    /// <seealso cref="ObjectPool{T}.Shared"/>
    public class ServiceRunner : IUpdatable
    {
        private List<IUpdatable> _updatables;
        private List<Coroutine<TimeSpan>> _coroutines;
        private List<Timer> _timers;

        private static ServiceRunner _default;

        /// <summary>
        /// Get a lazily initialized statically accessible instance of <see cref="ServiceRunner"/>.
        /// </summary>
        public static ServiceRunner Default => System.Threading.Volatile.Read(ref _default) ?? EnsureDefault();

        private static ServiceRunner EnsureDefault()
        {
            System.Threading.Interlocked.CompareExchange(ref _default, new ServiceRunner(), null);
            return _default;
        }

        private class Coroutine<T>
        {
            public TimeSpan Delay;
            public IEnumerator<T> Enumerator;
        }

        private abstract class Timer
        {
            protected TimerData Data;
            public bool IsCanceled => Data.Canceled;
            public bool IsDone => Data.Done;

            public abstract void Update(TimeSpan delta);
        }

        private class VoidTimer : Timer
        {
            private Action<TimerData> _update;
            private Action _finish;
            private Action<TimerData> _canceled;

            public void Set(TimeSpan duration, Action<TimerData> update, Action finish, Action<TimerData> canceled)
            {
                Data = ObjectPool<TimerData>.Shared.Get();
                Data.Canceled = false;
                Data.Duration = duration;
                Data.Elapsed = TimeSpan.Zero;
                _update = update;
                _finish = finish;
                _canceled = canceled;
            }

            public override void Update(TimeSpan delta)
            {
                Data.Delta = delta;

                if (IsCanceled)
                {
                    _canceled?.Invoke(Data);
                }
                else
                {
                    Data.Elapsed += delta;
                    _update?.Invoke(Data);

                    if (IsDone)
                        _finish?.Invoke();
                }

                if (IsCanceled || IsDone)
                {
                    ObjectPool<TimerData>.Shared.Return(Data);
                    ObjectPool<VoidTimer>.Shared.Return(this);
                }
            }
        }

        private class Timer<T> : Timer
        {
            private Action<TimerData, T> _update;
            private Action<T> _finish;
            private Action<TimerData, T> _canceled;
            private T _context;

            public void Set(TimeSpan duration, Action<TimerData, T> update, Action<T> finish, Action<TimerData, T> canceled, T context)
            {
                Data = ObjectPool<TimerData>.Shared.Get();
                Data.Canceled = false;
                Data.Duration = duration;
                Data.Elapsed = TimeSpan.Zero;
                _update = update;
                _finish = finish;
                _canceled = canceled;
                _context = context;
            }

            public override void Update(TimeSpan delta)
            {
                Data.Delta = delta;

                if (IsCanceled)
                {
                    _canceled?.Invoke(Data, _context);
                }
                else
                {
                    Data.Elapsed += delta;
                    _update?.Invoke(Data, _context);

                    if (IsDone)
                        _finish?.Invoke(_context);
                }

                if (IsCanceled || IsDone)
                {
                    ObjectPool<TimerData>.Shared.Return(Data);
                    ObjectPool<Timer<T>>.Shared.Return(this);
                }
            }
        }

        /// <summary>
        /// Create a new <see cref="ServiceRunner"/>.
        /// </summary>
        public ServiceRunner()
        {
            _updatables = new List<IUpdatable>();
            _coroutines = new List<Coroutine<TimeSpan>>();
            _timers = new List<Timer>();
        }

        #region IUpdatable

        /// <summary>
        /// Register an <see cref="IUpdatable"/> to get updated when <see cref="Update"/> is called.
        /// </summary>
        /// <param name="item">Item to register.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="item"/> is <c>null</c>.</exception>
        public void Register(IUpdatable item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            _updatables.Add(item);
        }

        /// <summary>
        /// Unregister an <see cref="IUpdatable"/> that was added with <see cref="Register"/> so it no longer gets updated.
        /// </summary>
        /// <param name="item">Item to unregister.</param>
        public void Unregister(IUpdatable item)
        {
            _updatables.Remove(item);
        }

        #endregion

        #region Run Timer

        /// <summary>
        /// Run a coroutine. The yielded <see cref="TimeSpan"/> indicates the delay to the next call.
        /// Yield <see cref="TimeSpan.Zero"/> to call the coroutine again the next frame.
        /// Note that this overload calls <see cref="IEnumerable{T}.GetEnumerator"/> and allocates memory with that call.
        /// If you run this coroutine often consider caching and resetting the enumerator and using the
        /// <see cref="Run(System.Collections.Generic.IEnumerator{System.TimeSpan})"/> overload.
        /// </summary>
        /// <param name="coroutine">Coroutine to run.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="coroutine"/> is <c>null</c>.</exception>
        public void Run(IEnumerable<TimeSpan> coroutine)
        {
            if (coroutine == null)
                throw new ArgumentNullException(nameof(coroutine));
            Run(coroutine.GetEnumerator());
        }

        /// <summary>
        /// Run a coroutine. The yielded <see cref="TimeSpan"/> indicates the delay to the next call.
        /// Yield <see cref="TimeSpan.Zero"/> to call the coroutine again the next frame.
        /// </summary>
        /// <param name="coroutine">Enumerator of the coroutine to run.</param>
        /// <exception cref="ArgumentNullException">If <paramref name="coroutine"/> is <c>null</c>.</exception>
        public void Run(IEnumerator<TimeSpan> coroutine)
        {
            if (coroutine == null)
                throw new ArgumentNullException(nameof(coroutine));

            var cr = ObjectPool<Coroutine<TimeSpan>>.Shared.Get();
            cr.Enumerator = coroutine;
            cr.Delay = TimeSpan.Zero;
            _coroutines.Add(cr);
        }

        /// <summary>
        /// Start a timer and run a method when it finishes.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run<T>(float seconds, T ctx, Action<T> finish, Action<TimerData, T> canceled = null)
        {
            Run(seconds, ctx, null, finish, canceled);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run<T>(float seconds, T ctx, Action<TimerData, T> update, Action<TimerData, T> canceled)
        {
            Run(seconds, ctx, update);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's finished or canceled.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run<T>(float seconds, T ctx, Action<TimerData, T> update, Action<T> finish = null, Action<TimerData, T> canceled = null)
        {
            Run(TimeSpan.FromSeconds(seconds), ctx, update, finish, canceled);
        }

        /// <summary>
        /// Start a timer and run a method when it finishes.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run(float seconds, Action finish, Action<TimerData> canceled = null)
        {
            Run(seconds, null, finish, canceled);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run(float seconds, Action<TimerData> update, Action<TimerData> canceled)
        {
            Run(seconds, update);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's finished or canceled.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run(float seconds, Action<TimerData> update, Action finish = null, Action<TimerData> canceled = null)
        {
            Run(TimeSpan.FromSeconds(seconds), update, finish, canceled);
        }

        /// <summary>
        /// Start a timer and run a method when it finishes.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run<T>(TimeSpan duration, T ctx, Action<T> finish, Action<TimerData, T> canceled = null)
        {
            Run(duration, ctx, null, finish, canceled);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run<T>(TimeSpan duration, T ctx, Action<TimerData, T> update, Action<TimerData, T> canceled)
        {
            Run(duration, ctx, update);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's finished or canceled.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run<T>(TimeSpan duration, T ctx, Action<TimerData, T> update, Action<T> finish = null, Action<TimerData, T> canceled = null)
        {
            var tm = ObjectPool<Timer<T>>.Shared.Get();
            tm.Set(duration, update, finish, canceled, ctx);
            _timers.Add(tm);
        }

        /// <summary>
        /// Start a timer and run a method when it finishes.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run(TimeSpan duration, Action finish, Action<TimerData> canceled = null)
        {
            Run(duration, null, finish, canceled);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run(TimeSpan duration, Action<TimerData> update, Action<TimerData> canceled)
        {
            Run(duration, update);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's finished or canceled.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        public void Run(TimeSpan duration, Action<TimerData> update, Action finish = null, Action<TimerData> canceled = null)
        {
            var tm = ObjectPool<VoidTimer>.Shared.Get();
            tm.Set(duration, update, finish, canceled);
            _timers.Add(tm);
        }

        #endregion

        #region Tween

        /// <summary>
        /// Tween a field of an object to a certain value. The current value is used as the start value.
        /// </summary>
        /// <remarks>
        /// This overload dynamically generates and compiles a Lerp method for the type T using Add(T, T),
        /// Subtract(T, T) and Mutliply(float, T) operators.
        /// If the type of the field does not implement these operators this function call will fail.
        /// You can add a custom Lerp function as an additional parameter to use another overload and bypass
        /// the dynamic Lerp generation.
        /// </remarks>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="seconds">Duration of the tween in seconds.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty to, float seconds, Ease easingFunction)
        {
            Tween(item, selector, selector(item) ,to, TimeSpan.FromSeconds(seconds), easingFunction, LerpGen<TProperty>.Lerp);
        }

        /// <summary>
        /// Tween a field of an object to a certain value.
        /// </summary>
        /// <remarks>
        /// This overload dynamically generates and compiles a Lerp method for the type T using Add(T, T),
        /// Subtract(T, T) and Mutliply(float, T) operators.
        /// If the type of the field does not implement these operators this function call will fail.
        /// You can add a custom Lerp function as an additional parameter to use another overload and bypass
        /// the dynamic Lerp generation.
        /// </remarks>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="from">Start value of the tween.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="seconds">Duration of the tween in seconds.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty from, TProperty to, float seconds, Ease easingFunction)
        {
            Tween(item, selector, from, to, TimeSpan.FromSeconds(seconds), easingFunction, LerpGen<TProperty>.Lerp);
        }

        /// <summary>
        /// Tween a field of an object to a certain value. The current value is used as the start value.
        /// </summary>
        /// <remarks>
        /// This overload dynamically generates and compiles a Lerp method for the type T using Add(T, T),
        /// Subtract(T, T) and Mutliply(float, T) operators.
        /// If the type of the field does not implement these operators this function call will fail.
        /// You can add a custom Lerp function as an additional parameter to use another overload and bypass
        /// the dynamic Lerp generation.
        /// </remarks>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="duration">Duration of the tween.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty to, TimeSpan duration, Ease easingFunction)
        {
            Tween(item, selector, selector(item), to, duration, easingFunction, LerpGen<TProperty>.Lerp);
        }

        /// <summary>
        /// Tween a field of an object to a certain value.
        /// </summary>
        /// <remarks>
        /// This overload dynamically generates and compiles a Lerp method for the type T using Add(T, T),
        /// Subtract(T, T) and Mutliply(float, T) operators.
        /// If the type of the field does not implement these operators this function call will fail.
        /// You can add a custom Lerp function as an additional parameter to use another overload and bypass
        /// the dynamic Lerp generation.
        /// </remarks>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="from">Start value of the tween.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="duration">Duration of the tween.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty from, TProperty to, TimeSpan duration, Ease easingFunction)
        {
            Tween(item, selector, from, to, duration, easingFunction, LerpGen<TProperty>.Lerp);
        }

        /// <summary>
        /// Tween a field of an object to a certain value. The current value is used as the start value.
        /// </summary>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="seconds">Duration of the tween in seconds.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <param name="lerp">Linear interpolation function to use.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty to, float seconds, Ease easingFunction, Lerp<TProperty> lerp)
        {
            Tween(item, selector, selector(item), to, TimeSpan.FromSeconds(seconds), easingFunction, lerp);
        }

        /// <summary>
        /// Tween a field of an object to a certain value.
        /// </summary>
        /// <remarks>
        /// This overload dynamically generates and compiles a Lerp method for the type T using Add(T, T),
        /// Subtract(T, T) and Mutliply(float, T) operators.
        /// If the type of the field does not implement these operators this function call will fail.
        /// You can add a custom Lerp function as an additional parameter to use another overload and bypass
        /// the dynamic Lerp generation.
        /// </remarks>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="from">Start value of the tween.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="seconds">Duration of the tween in seconds.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <param name="lerp">Linear interpolation function to use.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty from, TProperty to, float seconds, Ease easingFunction, Lerp<TProperty> lerp)
        {
            Tween(item, selector, from, to, TimeSpan.FromSeconds(seconds), easingFunction, lerp);
        }

        /// <summary>
        /// Tween a field of an object to a certain value. The current value is used as the start value.
        /// </summary>
        /// <remarks>
        /// This overload dynamically generates and compiles a Lerp method for the type T using Add(T, T),
        /// Subtract(T, T) and Mutliply(float, T) operators.
        /// If the type of the field does not implement these operators this function call will fail.
        /// You can add a custom Lerp function as an additional parameter to use another overload and bypass
        /// the dynamic Lerp generation.
        /// </remarks>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="duration">Duration of the tween.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <param name="lerp">Linear interpolation function to use.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty to, TimeSpan duration, Ease easingFunction, Lerp<TProperty> lerp)
        {
            Tween(item, selector, selector(item), to, duration, easingFunction, lerp);
        }

        /// <summary>
        /// Tween a field of an object to a certain value.
        /// </summary>
        /// <param name="item">Object to tween a field from.</param>
        /// <param name="selector">Selector that returns a ref to the field given the object.</param>
        /// <param name="from">Start value of the tween.</param>
        /// <param name="to">End value of the tween.</param>
        /// <param name="duration">Duration of the tween.</param>
        /// <param name="easingFunction">Easing function to apply for the tween.</param>
        /// <param name="lerp">Linear interpolation function to use.</param>
        /// <typeparam name="TItem">Type of the object.</typeparam>
        /// <typeparam name="TProperty">Type of the field.</typeparam>
        public void Tween<TItem, TProperty>(TItem item, GetRef<TItem, TProperty> selector, TProperty from, TProperty to, TimeSpan duration, Ease easingFunction, Lerp<TProperty> lerp)
        {
            var tween = ObjectPool<Tween<TItem, TProperty>>.Shared.Get();
            tween.Item = item;
            tween.Selector = selector;
            tween.Lerp = lerp;
            tween.From = from;
            tween.To = to;
            tween.EasingFunction = easingFunction;
            Tween(tween, duration);
        }

        /// <summary>
        /// Perform a <see cref="Tween"/> over the given time.
        /// </summary>
        /// <param name="tween">Tween to perform.</param>
        /// <param name="seconds">Duration of the tween in seconds.</param>
        public void Tween(Tween tween, float seconds)
        {
            Run(TimeSpan.FromSeconds(seconds), tween, (td, tw) => tw.Apply(td.Progress));
        }

        /// <summary>
        /// Perform a <see cref="Tween"/> over a given <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="tween">Tween to perform.</param>
        /// <param name="duration">Duration of the tween.</param>
        public void Tween(Tween tween, TimeSpan duration)
        {
            Run(duration, tween, (td, tw) => tw.Apply(td.Progress));
        }

        #endregion

        /// <summary>
        /// Update all registered <see cref="IUpdatable"/> implementations, all coroutines and all timers.
        /// </summary>
        /// <param name="delta">Time since the last update call.</param>
        public void Update(TimeSpan delta)
        {
            UpdateUpdatables(delta);
            UpdateCoroutines(delta);
            UpdateTimers(delta);
        }

        private void UpdateUpdatables(TimeSpan delta)
        {
            foreach (var u in _updatables)
                u.Update(delta);
        }

        private void UpdateCoroutines(TimeSpan delta)
        {
            for (var i = _coroutines.Count - 1; i >= 0; i--)
            {
                var c = _coroutines[i];

                c.Delay -= delta;
                if (c.Delay > TimeSpan.Zero)
                    continue;

                if (c.Enumerator.MoveNext())
                {
                    var ts = c.Enumerator.Current;
                    c.Delay = ts;
                }
                else
                {
                    _coroutines.RemoveAt(i);
                }
            }
        }

        private void UpdateTimers(TimeSpan delta)
        {
            for (var i = _timers.Count - 1; i >= 0; i--)
            {
                var t = _timers[i];
                t.Update(delta);

                if (t.IsCanceled || t.IsDone)
                    _timers.RemoveAt(i);
            }
        }
    }
}