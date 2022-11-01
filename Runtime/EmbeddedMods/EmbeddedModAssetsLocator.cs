using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Katas.UniMod
{
    /// <summary>
    /// Resource locator implementation used by the EmbeddedModLoader to locate serialized EmbeddedModAssets. It tries to mimic how a real IResourceLocator
    /// would behave for a local mod, but it is not perfect (for example you won't find the mod startup object here).
    /// </summary>
    public sealed class EmbeddedModAssetsLocator : IResourceLocator
    {
        public string LocatorId { get; }
        public IEnumerable<object> Keys { get; }
        
        private readonly Dictionary<object, List<IResourceLocation>> _locationsByKey;
        private readonly List<object> _keys;
        private readonly Dictionary<(object Key, Type Type), IList<IResourceLocation>> _locateCache;
        
        public static async UniTask<EmbeddedModAssetsLocator> CreateAsync(string id, IEnumerable<EmbeddedModAsset> assets)
        {
            var locator = new EmbeddedModAssetsLocator(id);
            await locator.InitializeAsync(assets);
            return locator;
        }
        
        private EmbeddedModAssetsLocator(string id)
        {
            _locationsByKey = new Dictionary<object, List<IResourceLocation>>();
            _keys = new List<object>();
            _locateCache = new Dictionary<(object, Type), IList<IResourceLocation>>();
            
            LocatorId = id;
            Keys = _keys.AsReadOnly();
        }

        public bool Locate(object key, Type type, out IList<IResourceLocation> locations)
        {
            // try to get the locations from the cache
            var cacheKey = (key, type);
            if (_locateCache.TryGetValue(cacheKey, out locations))
                return true;
            
            // try to get the locations list for the given key
            if (!_locationsByKey.TryGetValue(key, out List<IResourceLocation> locationsList))
                return false;

            // filter the returned list by the resource type
            if (type is not null)
                locationsList = locationsList.Where(location => location.ResourceType == type).ToList();
            
            if (locationsList.Count == 0)
                return false;
            
            // cache and return the results
            locations = locationsList.AsReadOnly();
            _locateCache.Add(cacheKey, locations);
            
            return true;
        }
        
        private async UniTask InitializeAsync(IEnumerable<EmbeddedModAsset> assets)
        {
            // load all the asset locations from Addressables and map them to their keys. this will also load all the dependency locations
            await UniTask.WhenAll(assets.Select(LoadAssetAsync));
            
            // add all keys
            _keys.AddRange(_locationsByKey.Keys);
        }

        private async UniTask LoadAssetAsync(EmbeddedModAsset asset)
        {
            AsyncOperationHandle<IList<IResourceLocation>> handle;
            using var _ = ListPool<object>.Get(out var keys);
            keys.Add(asset.guid);
            
            try
            {
                handle = Addressables.LoadResourceLocationsAsync(keys as IEnumerable, Addressables.MergeMode.Union);
                await handle;
            }
            catch (Exception)
            {
                return;
            }
            
            if (!handle.IsValid())
                return;
            
            // fetch the loaded location
            IResourceLocation location = handle.Result?.FirstOrDefault();
            Addressables.Release(handle);
            if (location is null)
                return;
            
            // map to the guid, primary key and labels
            AddLocation(asset.guid, location);
            AddLocation(location.PrimaryKey, location);
            foreach (string label in asset.labels)
                AddLocation(label, location);
            
            // recursively map all the dependencies to their primary key
            AddDependencies(location);
        }

        private void AddLocation(object key, IResourceLocation location)
        {
            if (!_locationsByKey.TryGetValue(key, out List<IResourceLocation> locations))
                _locationsByKey.Add(key, locations = new List<IResourceLocation>());
            
            locations.Add(location);
        }

        private void AddDependencies(IResourceLocation location)
        {
            if (location.Dependencies is null)
                return;

            foreach (IResourceLocation dependency in location.Dependencies)
            {
                AddLocation(dependency.PrimaryKey, dependency);
                AddDependencies(dependency);
            }
        }
    }
}