using System;
using System.Threading;
using System.Threading.Tasks;
using Blazored.LocalStorage;

namespace ch1seL.Blazored.LocalStorage.Concurrent
{
    public abstract class ConcurrentEntityStorageBase<T> : IDisposable where T : class, new()
    {
        private readonly ILocalStorageService _localStorage;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        private readonly string _storageName;

        protected ConcurrentEntityStorageBase(ILocalStorageService localStorage, string storageName = null)
        {
            _localStorage = localStorage;
            _storageName = storageName ?? GetType().GetFriendlyName();
        }

        public void Dispose()
        {
            _semaphoreSlim?.Dispose();
        }

        protected async Task<TR> Request<TR>(Func<T, TR> func)
        {
            T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
            return func(storage);
        }

        protected async Task<TR> Request<TR>(Func<T, Task<TR>> func)
        {
            T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
            return await func(storage);
        }

        protected async Task<TR> Update<TR>(Func<T, TR> func)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
                TR result = func(storage);
                await _localStorage.SetItemAsync(_storageName, storage);
                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        protected async Task<TR> Update<TR>(Func<T, Task<TR>> func)
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
                TR result = await func(storage);
                await _localStorage.SetItemAsync(_storageName, storage);
                return result;
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        protected async Task Update(Action<T> action)
        {
            await Update(storage =>
            {
                action(storage);
                return (object) null;
            });
        }
    }
    
    public static class TypeNameExtensions
    {
        public static string GetFriendlyName(this Type type)
        {
            var friendlyName = type.Name;
            if (!type.IsGenericType) return friendlyName;
            
            var iBacktick = friendlyName.IndexOf('`');
            if (iBacktick > 0)
            {
                friendlyName = friendlyName.Remove(iBacktick);
            }
            friendlyName += "<";
            var typeParameters = type.GetGenericArguments();
            for (var i = 0; i < typeParameters.Length; ++i)
            {
                var typeParamName = GetFriendlyName(typeParameters[i]);
                friendlyName += (i == 0 ? typeParamName : "," + typeParamName);
            }
            friendlyName += ">";

            return friendlyName;
        }
    }
}