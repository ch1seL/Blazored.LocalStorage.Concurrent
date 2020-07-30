using System;
using System.Threading;
using System.Threading.Tasks;

namespace Blazored.LocalStorage {
    public abstract class ConcurrentEntityStorageBase<T> : IDisposable where T : class, new() {
        private readonly ILocalStorageService _localStorage;
        private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);
        private readonly string _storageName;

        protected ConcurrentEntityStorageBase(ILocalStorageService localStorage, string storageName = null) {
            _localStorage = localStorage;
            _storageName = storageName ?? typeof(T).Name;
        }

        public void Dispose() {
            _semaphoreSlim?.Dispose();
        }

        protected async Task<TR> Request<TR>(Func<T, TR> func) {
            T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
            return func(storage);
        }

        protected async Task<TR> Request<TR>(Func<T, Task<TR>> func) {
            T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
            return await func(storage);
        }

        protected async Task<TR> Update<TR>(Func<T, TR> func) {
            await _semaphoreSlim.WaitAsync();
            try {
                T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
                TR result = func(storage);
                await _localStorage.SetItemAsync(_storageName, storage);
                return result;
            } finally {
                _semaphoreSlim.Release();
            }
        }

        protected async Task<TR> Update<TR>(Func<T, Task<TR>> func) {
            await _semaphoreSlim.WaitAsync();
            try {
                T storage = await _localStorage.GetItemAsync<T>(_storageName) ?? new T();
                TR result = await func(storage);
                await _localStorage.SetItemAsync(_storageName, storage);
                return result;
            } finally {
                _semaphoreSlim.Release();
            }
        }

        protected async Task Update(Action<T> action) {
            await Update(storage => {
                action(storage);
                return (object) null;
            });
        }
    }
}