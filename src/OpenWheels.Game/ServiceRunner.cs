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
    /// The compiled Lerp function is cached. The tween functions get a
    /// <see cref="OpenWheels.Game.Tween{TItem,TProperty}"/> instance from the shared <see cref="ObjectPool{T}"/> for
    /// the tween type.
    /// </summary>
    public class ServiceRunner : IUpdatable
    {
        private List<IUpdatable> _updatables;

        private ObjectPool<Coroutine<TimeSpan>> _coroutinePool;
        private List<Coroutine<TimeSpan>> _coroutines;

        private ObjectPool<Timer> _timerPool;
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

        private class Timer
        {
            public TimerData Data;
            public InAction<TimerData> Update;
            public Action<object> Finish;
            public InAction<TimerData> Canceled;

            public bool IsCanceled => Data.Canceled;
            public bool IsDone => Data.Done;

            public void Set(TimeSpan duration, InAction<TimerData> update, Action<object> finish, InAction<TimerData> canceled, object context)
            {
                Data = new TimerData(duration, context);
                Update = update;
                Finish = finish;
                Canceled = canceled;
            }
        }

        /// <summary>
        /// Create a new <see cref="ServiceRunner"/>.
        /// </summary>
        public ServiceRunner()
        {
            _updatables = new List<IUpdatable>();
            _coroutinePool = new ObjectPool<Coroutine<TimeSpan>>();
            _coroutines = new List<Coroutine<TimeSpan>>();
            _timerPool = new ObjectPool<Timer>();
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

            var cr = _coroutinePool.Get();
            cr.Enumerator = coroutine;
            cr.Delay = TimeSpan.Zero;
            _coroutines.Add(cr);
        }

        /// <summary>
        /// Start a timer and run a method when it finishes.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void RunAfter(float seconds, Action<object> finish, object ctx = null)
        {
            Run(seconds, null, finish, null, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(float seconds, InAction<TimerData> update, object ctx)
        {
            Run(seconds, update, null, null, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it finishes.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(float seconds, InAction<TimerData> update, Action<object> finish, object ctx)
        {
            Run(TimeSpan.FromSeconds(seconds), update, finish, null, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's canceled.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(float seconds, InAction<TimerData> update, InAction<TimerData> canceled, object ctx)
        {
            Run(TimeSpan.FromSeconds(seconds), update, null, canceled, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's finished or canceled.
        /// </summary>
        /// <param name="seconds">Duration of the timer in seconds.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(float seconds, InAction<TimerData> update = null, Action<object> finish = null, InAction<TimerData> canceled = null, object ctx = null)
        {
            Run(TimeSpan.FromSeconds(seconds), update, finish, canceled, ctx);
        }

        /// <summary>
        /// Start a timer and run a method when it finishes.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void RunAfter(TimeSpan duration, Action<object> finish, object ctx = null)
        {
            Run(duration, null, finish, null, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(TimeSpan duration, InAction<TimerData> update, object ctx)
        {
            Run(duration, update, null, null, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it finishes.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(TimeSpan duration, InAction<TimerData> update, Action<object> finish, object ctx)
        {
            Run(duration, update, finish, null, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's canceled.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(TimeSpan duration, InAction<TimerData> update, InAction<TimerData> canceled, object ctx)
        {
            Run(duration, update, null, canceled, ctx);
        }

        /// <summary>
        /// Start a timer and run a method every frame while it's running and another method when it's finished or canceled.
        /// </summary>
        /// <param name="duration">Duration of the timer.</param>
        /// <param name="update">Method to run every frame while the timer is running.</param>
        /// <param name="finish">Method to run when the timer finishes.</param>
        /// <param name="canceled">Method to run when the timer is canceled.</param>
        /// <param name="ctx">Context to set to avoid closures.</param>
        public void Run(TimeSpan duration, InAction<TimerData> update, Action<object> finish, InAction<TimerData> canceled, object ctx = null)
        {
            var tm = _timerPool.Get();
            tm.Set(duration, update, finish, canceled, ctx);
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
        /// Perform a <see cref="Tween"/> over a given <see cref="TimeSpan"/>.
        /// </summary>
        /// <param name="tween">Tween to perform.</param>
        /// <param name="duration">Duration of the tween.</param>
        public void Tween(Tween tween, TimeSpan duration)
        {
            Run(duration, (in TimerData td) => UpdateTween(td), tween);
        }

        private static void UpdateTween(in TimerData td)
        {
            var tween = (Tween) td.Context;
            tween.Apply(td.Progress);
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
                t.Data.Delta = delta;

                if (t.IsCanceled)
                {
                    t.Canceled?.Invoke(t.Data);
                    _timers.RemoveAt(i);
                }
                else
                {
                    ref TimerData ctx = ref t.Data;
                    ctx.Elapsed += delta;
                    t.Update?.Invoke(ctx);

                    if (t.IsDone)
                    {
                        t.Finish?.Invoke(t.Data.Context);
                        _timers.RemoveAt(i);
                        _timerPool.Return(t);
                    }
                }
            }
        }
    }
}