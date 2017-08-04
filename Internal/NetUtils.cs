#region Usings

using System;
using UnityEngine;
using UnityEngine.Networking;

#endregion

namespace WakeNet.Internal
{
    public static class NetUtils
    {
        /// <summary>
        ///     Returns true if the given code is a network error, false if not.
        /// </summary>
        public static bool IsNetworkError(object context, byte error)
        {
            if (Enum.IsDefined(typeof(NetworkError), (int)error))
            {
                var netError = (NetworkError) error;
                if (netError == NetworkError.Ok) return true;

                LogError("Network Error occured in ({0}). Error : {1}", context.GetType().FullName, netError);
            }
            else LogError("Error occured in ({0}). Error code : {1}", context.GetType().FullName, error);

            return false;
        }

        /// <summary>
        ///     Returns true if our socket id is valid, false if not.
        /// </summary>
        public static bool IsSocketValid(int sock)
        {
            if (sock < 0) return false;
            return true;
        }

        public static void Log(object message)
        {
            Debug.Log(message);
        }

        public static void Log(string format, params object[] args)
        {
            Debug.LogFormat(format, args);
        }

        public static void LogWarning(object message)
        {
            Debug.LogWarning(message);
        }

        public static void LogWarning(string format, params object[] args)
        {
            Debug.LogWarningFormat(format, args);
        }

        public static void LogError(object message)
        {
            Debug.LogError(message);
        }

        public static void LogError(string format, params object[] args)
        {
            Debug.LogErrorFormat(format, args);
        }

        public static void LogException(Exception e)
        {
            Debug.LogException(e);
        }
    }
}