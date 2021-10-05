using UnityEngine;

namespace USerialization.Unity
{
    public class UnityDebugLogger : ILogger
    {
        public void Error(string error)
        {
            Debug.LogError(error);
        }
    }
}
