using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Katas.UniMod
{
    /// <summary>
    /// You can use this component for an automatic initialization of the UniMod context with
    /// some configuration parameters exposed in the inspector.
    /// <br/><br/>
    /// You can optionally add an <see cref="EmbeddedModSource"/> component to the same GameObject to support embedded mods.
    /// </summary>
    [AddComponentMenu("UniMod/UniMod Context Initializer", 0)]
    [DisallowMultipleComponent]
    public sealed class UniModContextInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string hostId = "com.company.name";
        [SerializeField] private string hostVersion = "0.1.0";
        [SerializeField] private bool supportStandaloneMods = true;
        [SerializeField] private bool supportScriptingInMods = false;
        [SerializeField] private bool supportModsCreatedForOtherHosts = false;
        
        [Header("Loading")][Space(5)]
        [SerializeField] private bool refreshContextOnStart = false;
        [SerializeField] private bool loadAllModsOnStart = false;
        
        [Header("Events")][Space(5)]
        public UnityEvent onContextInitialized = new();
        public UnityEvent onContextRefreshed = new();
        public UnityEvent onModsLoaded = new();

        private void Awake()
        {
            InitializeContext();
        }

        private void Start()
        {
            StartAsync().Forget();
        }

        private void InitializeContext()
        {
            if (UniModRuntime.IsContextInitialized)
            {
                Debug.LogWarning("[UniMod] tried to initialize a UniMod context but it has already been initialized");
                return;
            }
            
            // initialize a mod host with the user configuration
            var host = new ModHost(hostId, hostVersion);
            host.SupportStandaloneMods = supportStandaloneMods;
            host.SupportModsContainingAssemblies = supportScriptingInMods;
            host.SupportModsCreatedForOtherHosts = supportModsCreatedForOtherHosts;
            
            // initialize the UniMod context
            UniModRuntime.InitializeContext(host);
            
            // check if we have an embedded mod source component so we can add it to the context
            var embeddedModSource = GetComponent<EmbeddedModSource>();
            if (embeddedModSource)
                UniModRuntime.Context.AddSource(embeddedModSource);
            
            onContextInitialized.Invoke();
        }

        private async UniTaskVoid StartAsync()
        {
            if (refreshContextOnStart)
            {
                await UniModRuntime.Context.RefreshAsync();
                onContextRefreshed.Invoke();
            }

            if (loadAllModsOnStart)
            {
                await UniModRuntime.Context.TryLoadAllModsAsync();
                onModsLoaded.Invoke();
            }
        }
    }
}
