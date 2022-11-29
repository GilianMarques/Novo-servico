using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace outros
{
    public static class Async
    {
        public static DispatcherAwaiter Thread => new DispatcherAwaiter();

        internal static void runAsync(int delay, Action callback)
        {

            ThreadStart startDelegate = new ThreadStart(() => { callback(); });

            Thread thread = new Thread(startDelegate) { Priority = ThreadPriority.Normal };
            thread.SetApartmentState(ApartmentState.STA);// necessario pra usar a api de notificaçoes


            DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler((object sender, EventArgs e) =>
            {
                timer.Stop();
                thread.Start();
            });
            timer.Interval = new TimeSpan(0, 0, 0, 0, delay);
            timer.Start();

        }

        internal static void runAsync(Action callback)
        {

            ThreadStart startDelegate = new ThreadStart(() => { callback(); });

            Thread thread = new Thread(startDelegate) { Priority = ThreadPriority.Normal };
            thread.SetApartmentState(ApartmentState.STA);// necessario pra usar a api de notificaçoes
            thread.Start();

        }

        internal static async void runOnUI(Action callback)
        {

            await Thread;
            callback();
        }

        internal static async void runOnUI(int delay, Action callback)
        {

            await Thread;

            DispatcherTimer timer = new System.Windows.Threading.DispatcherTimer();
            timer.Tick += new EventHandler((object sender, EventArgs e) =>
            {
                timer.Stop(); callback();
            });
            timer.Interval = new TimeSpan(0, 0, 0, 0, delay);
            timer.Start();

        }

        public struct DispatcherAwaiter : INotifyCompletion
        {
            public bool IsCompleted => Application.Current.Dispatcher.CheckAccess();

            public void OnCompleted(Action continuation) => Application.Current.Dispatcher.Invoke(continuation);

            public void GetResult() { }

            public DispatcherAwaiter GetAwaiter()
            {
                return this;
            }
        }
    }
}