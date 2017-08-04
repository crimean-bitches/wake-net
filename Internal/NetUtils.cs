#region Usings

using UnityEngine;
using UnityEngine.Networking;

#endregion

namespace WakeNet.Internal
{
    public static class NetUtils
    {
        /// <summary>
        ///     Return string value of any network error if it is an error, otherwise return "";
        /// </summary>
        /// <param name="error">Error as string or "" if no error.</param>
        public static string GetNetworkError(byte error)
        {
            if (error != (byte) NetworkError.Ok)
            {
                var nerror = (NetworkError) error;
                return nerror.ToString();
            }

            return "";
        }

        /// <summary>
        ///     Returns true if the given code is a network error, false if not.
        /// </summary>
        public static bool IsNetworkError(byte error)
        {
            if (error != (byte) NetworkError.Ok) return true;
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
    }
}