using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Katas.UniMod
{
    /// <summary>
    /// You can use this component for an automatic default initialization of a mod context.
    /// </summary>
    public sealed class UniModContextInitializer : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private string applicationId = "com.defaultcompany.application";
        [SerializeField] private string applicationVersion = "0.1.0";
        [SerializeField] private bool supportStandaloneMods = true;
        [SerializeField] private bool supportModsContainingAssemblies = true;
        [SerializeField] private bool supportModsCreatedForOtherApps = false;
        
        [Header("Loading")][Space(5)]
        [SerializeField] private bool refreshContextOnStart = false;
        [SerializeField] private bool loadAllModsOnStart = false;

        public IModContext ModContext
        {
            get
            {
                if (_context is null)
                    InitializeContext();
                
                return _context;
            }
        }
        
        private IModContext _context;
        
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
            
            // instantiate a moddable application with the parameters configured in the inspector 
            var application = new ModdableApplication(applicationId, applicationVersion);
            application.SupportStandaloneMods = supportStandaloneMods;
            application.SupportModsContainingAssemblies = supportModsContainingAssemblies;
            application.SupportModsCreatedForOtherApps = supportModsCreatedForOtherApps;
            
            // create a default mod context with the moddable application
            _context = UniModContext.CreateDefaultContext(application);
            
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