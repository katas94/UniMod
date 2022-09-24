using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Katas.UniMod.Editor
{
    /// <summary>
    /// Helper class to perform validation of UnityEngine.Object lists in the inspector.
    /// </summary>
    internal sealed class AssetListValidator<T>
        where T : Object
    {
        public bool ListChanged { get; private set; }
        
        private readonly List<T> _list;
        private readonly Func<T, bool> _validator;
        private readonly List<T> _listCache = new();

        public AssetListValidator(List<T> list, Func<T, bool> validator = null)
        {
            _list = list;
            _validator = validator;
        }

        public void Validate(Func<T, bool> validatorOverride = null)
        {
            Func<T, bool> validator = validatorOverride ?? _validator;
            if (validator is null)
                return;
            
            ListChanged = _list.Count != _listCache.Count;

            for (int i = 0; i < _list.Count; ++i)
            {
                if (!ListChanged && _list[i] != _listCache[i])
                    ListChanged = true;
                
                if (_list[i] != null && !validator(_list[i]))
                    _list[i] = _listCache.Count > i ? _listCache[i] : null;
            }
            
            _listCache.Clear();
            _listCache.AddRange(_list);
        }
    }
}