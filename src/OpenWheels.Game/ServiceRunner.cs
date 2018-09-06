using System;
using System.Collections.Generic;

namespace OpenWheels.Game
{
    /// <summary>
    /// Manages different kinds of executable units that require updating in frequent time interval.
    /// ServiceRunner supports three kinds of services: <see cref="IUpdatable"/>, coroutines and timers.
    /// When <see cref="Update"/> is called the services are updated.
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
    /// </summary>
    public class ServiceRunner : IUpdatable
    {
        private List<IUpdatable> _updatables;

        private ObjectPool<Coroutine<TimeSpan>> _coroutinePool;
        private List<Coroutine<TimeSpan>> _coroutines;

        private ObjectPool<Timer> _timerPool;
        private List<Timer> _timers;

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
            _coroutinePool = new ObjectPool<Coroutine<TimeSpan>>(() => new Coroutine<TimeSpan>());
            _coroutines = new List<Coroutine<TimeSpan>>();
            _timerPool = new ObjectPool<Timer>(() => new Timer());
            _timers = new List<Timer>();
        }

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
                    c.Enumerator.Dispose();
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