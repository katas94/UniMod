using System.Threading;

namespace Katas.UniMod
{
    internal sealed class AsyncMethodController
    {
        public bool IsRunning => _cancellationSource != null;
        
        private CancellationTokenSource _cancellationSource;

        public CancellationToken Invoke()
        {
            Cancel();
            _cancellationSource = new CancellationTokenSource();
            return _cancellationSource.Token;
        }

        public void Cancel()
        {
            if (_cancellationSource is null)
                return;
            
            _cancellationSource.Cancel();
            _cancellationSource.Dispose();
            _cancellationSource = null;
        }

        public void Finish()
        {
            if (_cancellationSource is null)
                return;
            
            _cancellationSource.Dispose();
            _cancellationSource = null;
        }
    }
}