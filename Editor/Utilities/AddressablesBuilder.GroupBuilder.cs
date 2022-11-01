using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using Object = UnityEngine.Object;

namespace Katas.UniMod.Editor
{
    public sealed partial class AddressablesBuilder
    {
        public interface IGroupBuilder
        {
            public void CreateEntry(Object asset, string address = null, IEnumerable<string> labels = null);
            public void CreateEntry(string guid, string address = null, IEnumerable<string> labels = null);
        }
        
        /// <summary>
        /// Group builder implementation created for the AddressablesBuilder. It is meant to be used with a temporary AddressableAssetSettings object.
        /// If created from an already existing group a new group will be created, acting as a copy, and all asset entries will be temporary moved
        /// to the new group until the instance is disposed.
        /// </summary>
        private sealed class GroupBuilder : IGroupBuilder
        {
            private readonly AddressableAssetSettings _settings;
            private readonly AddressableAssetGroup _group;
            private readonly AddressableAssetGroup _originalGroup;
            private readonly BundledAssetGroupSchema _bundledSchema;
            private readonly AddressableAssetEntry[] _originalEntries;
            private readonly bool[] _originalEntriesReadOnly;

            private bool _isDisposed = true;

            public GroupBuilder(AddressableAssetSettings settings, string groupName)
            {
                // try to get schemas from the default template, if not, try to get them from the default group
                var defaultTemplate = settings.GroupTemplateObjects?.FirstOrDefault() as AddressableAssetGroupTemplate;
                List<AddressableAssetGroupSchema> schemas = defaultTemplate ?
                    defaultTemplate.SchemaObjects
                    : settings.DefaultGroup ? settings.DefaultGroup.Schemas : new List<AddressableAssetGroupSchema>(0);

                _settings = settings;
                _group = settings.CreateGroup(groupName, false, true, false, schemas);
                _bundledSchema = _group.GetSchema<BundledAssetGroupSchema>() ?? throw new Exception($"The group must have a {nameof(BundledAssetGroupSchema)}");
                SetupBundledSchema();
                
                _isDisposed = false;
            }

            public GroupBuilder(
                AddressableAssetSettings settings,
                string groupName,
                List<AddressableAssetGroupSchema> schemasToCopy,
                params Type[] types)
            {
                _settings = settings;
                _group = settings.CreateGroup(groupName, false, true, false, schemasToCopy, types);
                _bundledSchema = _group.GetSchema<BundledAssetGroupSchema>() ?? throw new Exception($"The group must have a {nameof(BundledAssetGroupSchema)}");
                SetupBundledSchema();
                
                _isDisposed = false;
            }

            public GroupBuilder(AddressableAssetSettings settings, AddressableAssetGroup group)
            {
                _settings = settings;
                _group = settings.CreateGroup(group.Name, false, true, false, group.Schemas);
                _originalGroup = group;
                _bundledSchema = _group.GetSchema<BundledAssetGroupSchema>() ?? throw new Exception($"The group must have a {nameof(BundledAssetGroupSchema)}");
                SetupBundledSchema();
                
                // save all the original entries and move them to the new group
                _originalEntries = _originalGroup.entries.ToArray();
                _originalEntriesReadOnly = new bool[_originalEntries.Length];

                for (int i = 0; i < _originalEntries.Length; ++i)
                {
                    _originalEntriesReadOnly[i] = _originalEntries[i].ReadOnly;
                    _settings.MoveEntry(_originalEntries[i], _group, true, false);
                }
                
                _isDisposed = false;
            }
            
            public void CreateEntry(Object asset, string address = null, IEnumerable<string> labels = null)
            {
                string guid = GetAssetGuid(asset);
                CreateEntry(guid, address, labels);
            }

            public void CreateEntry(string guid, string address = null, IEnumerable<string> labels = null)
            {
                ThrowIfDisposed();
                AddressableAssetEntry entry = _settings.CreateOrMoveEntry(guid, _group, true, false);
                
                if (address is not null)
                    entry.address = address;
                if (labels is not null)
                    foreach(string label in labels)
                        entry.SetLabel(label, true, false, false);
            }
            
            public void SetPathIds(string buildPathId, string loadPathId)
            {
                ThrowIfDisposed();
                _bundledSchema.BuildPath.SetVariableById(_settings, buildPathId);
                _bundledSchema.LoadPath.SetVariableById(_settings, loadPathId);
            }

            public void Dispose()
            {
                if (_isDisposed || !_originalGroup || _originalEntries is null)
                    return;
                
                // move back original entries to their group
                for (int i = 0; i < _originalEntries.Length; ++i)
                    _settings.MoveEntry(_originalEntries[i], _originalGroup, _originalEntriesReadOnly[i], false);
                
                _isDisposed = true;
            }

            private void SetupBundledSchema()
            {
                _bundledSchema.IncludeInBuild = true;
                _bundledSchema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
            }

            private void ThrowIfDisposed()
            {
                if (_isDisposed)
                    throw new Exception("The group builder has been disposed and you are trying to access it");
            }
            
            private string GetAssetGuid(Object asset)
            {
                ThrowIfDisposed();

                if (!asset)
                    throw new Exception("The given asset is null or it has been destroyed");
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out string guid, out long _))
                    throw new Exception($"Could not get the asset GUID from the AssetDatabase: {asset}");
                
                return guid;
            }
        }
    }
}