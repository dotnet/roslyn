using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace MSBuildWorkspaceTester.Framework
{
    public class NotifyTaskCompletion<TResult> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Task<TResult> Task { get; }

        public TResult Result => Task.Status == TaskStatus.RanToCompletion ? Task.Result : default;
        public TaskStatus Status => Task.Status;
        public bool IsCompleted => Task.IsCompleted;
        public bool IsNotCompleted => !Task.IsCompleted;
        public bool IsSuccessfullyCompleted => Task.Status == TaskStatus.RanToCompletion;
        public bool IsCanceled => Task.IsCanceled;
        public bool IsFaulted => Task.IsFaulted;
        public AggregateException Exception => Task.Exception;
        public Exception InnerException => Exception?.InnerException;
        public string ErrorMessage => InnerException?.Message;

        public NotifyTaskCompletion(Task<TResult> task)
        {
            Task = task;
            if (!task.IsCompleted)
            {
                Task _ = WatchTaskAsync(task);
            }
        }

        private async Task WatchTaskAsync(Task task)
        {
            await task;

            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler == null)
            {
                return;
            }

            void Notify(string propertyName) => handler(this, PropertyChangedEventArgsCache.GetEventArgs(propertyName));

            Notify(nameof(Status));
            Notify(nameof(IsCompleted));
            Notify(nameof(IsNotCompleted));

            if (task.IsCanceled)
            {
                Notify(nameof(IsCanceled));
            }
            else if (task.IsFaulted)
            {
                Notify(nameof(IsFaulted));
                Notify(nameof(Exception));
                Notify(nameof(InnerException));
                Notify(nameof(ErrorMessage));
            }
            else
            {
                Notify(nameof(IsSuccessfullyCompleted));
                Notify(nameof(Result));
            }
        }
    }
}
