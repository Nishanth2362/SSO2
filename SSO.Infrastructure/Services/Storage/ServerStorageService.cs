using Microsoft.Extensions.Logging;
using SSO.Application.Interfaces.Serialization.Serializers;
using SSO.Application.Interfaces.Services.Storage;
using SSO.Application.Interfaces.Services.Storage.Provider;
using System.Text.Json;

namespace SSO.Infrastructure.Services.Storage
{
    public class ServerStorageService : IServerStorageService, ISyncServerStorageService
    {
        private readonly IStorageProvider _storageProvider;
        private readonly IJsonSerializer _serializer;
        private readonly ILogger<ServerStorageService> _logger;
        public ServerStorageService(IStorageProvider storageProvider, IJsonSerializer serializer, ILogger<ServerStorageService> logger)
        {
            _storageProvider = storageProvider;
            _serializer = serializer;
            _logger = logger;
        }

        public async ValueTask SetItemAsync<T>(string key, T data)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            ChangingEventArgs e = await RaiseOnChangingAsync(key, data).ConfigureAwait(false);

            if (e.Cancel)
            {
                return;
            }

            string serializedData = _serializer.Serialize(data);
            await _storageProvider.SetItemAsync(key, serializedData).ConfigureAwait(false);

            RaiseOnChanged(key, e.OldValue, data);
        }

        public async ValueTask SetItemAsStringAsync(string key, string data)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ChangingEventArgs e = await RaiseOnChangingAsync(key, data).ConfigureAwait(false);

            if (e.Cancel)
            {
                return;
            }

            await _storageProvider.SetItemAsync(key, data).ConfigureAwait(false);

            RaiseOnChanged(key, e.OldValue, data);
        }

        public async ValueTask<T> GetItemAsync<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string serializedData = await _storageProvider.GetItemAsync(key).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(serializedData))
            {
                return default;
            }

            try
            {
                return _serializer.Deserialize<T>(serializedData);
            }
            catch (JsonException e) when (e.Path == "$" && typeof(T) == typeof(string))
            {
                // For backward compatibility return the plain string.
                // On the next save a correct value will be stored and this Exception will not happen again, for this 'key'
                return (T)(object)serializedData;
            }
        }

        public ValueTask<string> GetItemAsStringAsync(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? throw new ArgumentNullException(nameof(key)) : _storageProvider.GetItemAsync(key);
        }

        public ValueTask RemoveItemAsync(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? throw new ArgumentNullException(nameof(key)) : _storageProvider.RemoveItemAsync(key);
        }

        public ValueTask ClearAsync()
        {
            return _storageProvider.ClearAsync();
        }

        public ValueTask<int> LengthAsync()
        {
            return _storageProvider.LengthAsync();
        }

        public ValueTask<string> KeyAsync(int index)
        {
            return _storageProvider.KeyAsync(index);
        }

        public ValueTask<bool> ContainKeyAsync(string key)
        {
            return _storageProvider.ContainKeyAsync(key);
        }

        public void SetItem<T>(string key, T data)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            ChangingEventArgs e = RaiseOnChangingSync(key, data);

            if (e.Cancel)
            {
                return;
            }

            string serializedData = _serializer.Serialize(data);
            _storageProvider.SetItem(key, serializedData);

            RaiseOnChanged(key, e.OldValue, data);
        }

        public void SetItemAsString(string key, string data)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            ChangingEventArgs e = RaiseOnChangingSync(key, data);

            if (e.Cancel)
            {
                return;
            }

            _storageProvider.SetItem(key, data);

            RaiseOnChanged(key, e.OldValue, data);
        }

        public T GetItem<T>(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string serializedData = _storageProvider.GetItem(key);

            if (string.IsNullOrWhiteSpace(serializedData))
            {
                return default;
            }

            try
            {
                return _serializer.Deserialize<T>(serializedData);
            }
            catch (JsonException e) when (e.Path == "$" && typeof(T) == typeof(string))
            {
                // For backward compatibility return the plain string.
                // On the next save a correct value will be stored and this Exception will not happen again, for this 'key'
                return (T)(object)serializedData;
            }
        }

        public string GetItemAsString(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? throw new ArgumentNullException(nameof(key)) : _storageProvider.GetItem(key);
        }

        public void RemoveItem(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            _storageProvider.RemoveItem(key);
        }

        public void Clear()
        {
            _storageProvider.Clear();
        }

        public int Length()
        {
            return _storageProvider.Length();
        }

        public string Key(int index)
        {
            return _storageProvider.Key(index);
        }

        public bool ContainKey(string key)
        {
            return _storageProvider.ContainKey(key);
        }

        public event EventHandler<ChangingEventArgs> Changing;
        private async Task<ChangingEventArgs> RaiseOnChangingAsync(string key, object data)
        {
            ChangingEventArgs e = new()
            {
                Key = key,
                OldValue = await GetItemInternalAsync<object>(key).ConfigureAwait(false),
                NewValue = data
            };

            Changing?.Invoke(this, e);

            return e;
        }

        private ChangingEventArgs RaiseOnChangingSync(string key, object data)
        {
            ChangingEventArgs e = new()
            {
                Key = key,
                OldValue = GetItemInternal(key),
                NewValue = data
            };

            Changing?.Invoke(this, e);

            return e;
        }

        private async Task<T> GetItemInternalAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string serializedData = await _storageProvider.GetItemAsync(key).ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(serializedData))
            {
                return default;
            }

            try
            {
                return _serializer.Deserialize<T>(serializedData);
            }
            catch (JsonException)
            {
                return (T)(object)serializedData;
            }
        }

        private object GetItemInternal(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string serializedData = _storageProvider.GetItem(key);

            if (string.IsNullOrWhiteSpace(serializedData))
            {
                return default;
            }

            try
            {
                return _serializer.Deserialize<object>(serializedData);
            }
            catch (JsonException)
            {
                return serializedData;
            }
        }

        public event EventHandler<ChangedEventArgs> Changed;
        private void RaiseOnChanged(string key, object oldValue, object data)
        {
            ChangedEventArgs e = new()
            {
                Key = key,
                OldValue = oldValue,
                NewValue = data
            };

            Changed?.Invoke(this, e);
        }
    }
}
