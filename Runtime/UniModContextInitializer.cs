using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.UniMod
{
    /// <summary>
    /// You can use this component for an automatic default initialization of the UniMod context with
    /// some configuration parameters exposed in the inspector.
    /// <br/><br/>
    /// You can optionally add an <see cref="EmbeddedModSource"/> component to the same object to support embedded mods.
    /// </summary>
    [AddComponentMenu("UniMod/UniMod Context Initializer", 0)]
    [DisallowMultipleComponent]
    public sealed class UniModContextInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string hostId = "com.company.name";
        [SerializeField] private string hostVersion = "0.1.0";
        [SerializeField] private bool supportStandaloneMods = true;
        [SerializeField] private bool supportModsContainingAssemblies = true;
        [SerializeField] private bool supportModsCreatedForOtherHosts = false;
        
        [Header("Loading")][Space(5)]
        [SerializeField] private bool refreshContextOnStart = false;
        [SerializeField] private bool loadAllModsOnStart = false;

        public IUniModContext ModContext
        {
            get
            {
                if (_context is null)
                    InitializeContext();
                
                return _context;
            }
        }
        
        private IUniModContext _context;
        
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
            if (_context != null)
                return;
            
            // instantiate a mod host with the parameters configured in the inspector 
            var host = new ModHost(hostId, hostVersion);
            host.SupportStandaloneMods = supportStandaloneMods;
            host.SupportModsContainingAssemblies = supportModsContainingAssemblies;
            host.SupportModsCreatedForOtherHosts = supportModsCreatedForOtherHosts;
            
            // create a default mod context with the host
            _context = UniModContext.CreateDefaultContext(host);
            
            // check if we have an embedded mod source component
            var embeddedModSource = GetComponent<EmbeddedModSource>();
            if (embeddedModSource)
                _context.AddSource(embeddedModSource);
        }

        private async UniTaskVoid StartAsync()
        {
            if (!refreshContextOnStart)
                return;
            
            await ModContext.RefreshAsync();
            
            if (loadAllModsOnStart)
                await ModContext.TryLoadAllModsAsync();
        }
    }
}