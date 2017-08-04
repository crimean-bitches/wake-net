using System.Collections;
using Helper;
using UnityEngine;
using WakeNet.Internal;

namespace WakeNet
{
    public class NetManagerHandler : MonoBehaviour
    {
        [SerializeField] private float _executeTime;
        #region Singleton
        private static NetManagerHandler _instance;
        public static NetManagerHandler Instance
        {
            get
            {
                if (_instance != null) return _instance;
                _instance = new GameObject("NetManager").AddComponent<NetManagerHandler>();
                GameObject.DontDestroyOnLoad(_instance.gameObject);
                return _instance;
            }
        }
        #endregion

        private Coroutine _pollRoutine;

        public void Init()
        {
            if(_pollRoutine != null) return;
            _pollRoutine = StartCoroutine(PollRoutine());
        }

        private IEnumerator PollRoutine()
        {
            var ts = 0f;
            while (true)
            {
                ts = GameTime.Now;
                NetManager.PollEvents();
                _executeTime = GameTime.Now - ts;
                yield return new WaitForSeconds(1f / Config.RECEIVE_RATE - _executeTime);
            }
        }
    }
}