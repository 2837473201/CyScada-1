using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace CyScada.Common
{
    public class CommonUtil
    {
        public const string DefaultUserPassword="118DD9CC92B899DF";//123456加密
        public const string AuthSeperator = ",";


        private static Queue<string> logQueue = new Queue<string>();
        private static object locker = new object();
        private static bool runner = true;

        public enum MappingType
        {
            UserTheme
        }

        public static string Encrypt(string password)
        {
            return DESEncrypt.Encrypt(password);
        }

        public static string Decrypt(string password)
        {
            return DESEncrypt.Decrypt(password);
        }


        public static string AppendAuthorityCode(string currentAuth,string authCode)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                return currentAuth;
            }
            if (!authCode.Contains(AuthSeperator))
            {
                authCode = authCode + AuthSeperator;
            }

            if (!currentAuth.Contains(authCode))
            {
                currentAuth += authCode;
            }
            return currentAuth;
        }

        public static string RemoveAuthorityCode(string currentAuth, string authCode)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                return currentAuth;
            }
            if (!authCode.Contains(AuthSeperator))
            {
                authCode = authCode + AuthSeperator;
            }

            if (currentAuth.Contains(authCode))
            {
                currentAuth = currentAuth.Replace(authCode, string.Empty);
            }
            return currentAuth;
        }

        public static bool ExistAuthorityCode(string currentAuth, string authCode)
        {
            if (string.IsNullOrEmpty(authCode))
            {
                return false;
            }
            if (!authCode.Contains(AuthSeperator))
            {
                authCode = authCode + AuthSeperator;
            }

            return currentAuth.Contains(authCode);
        }



        public static void EnqueueLog(string strMessage)
        {
            lock (locker)
            {
                logQueue.Enqueue(strMessage);
            }
        }

        public CommonUtil()
        {
            var currentPath = Path.GetFullPath(Assembly.GetExecutingAssembly().Location);
            ThreadPool.QueueUserWorkItem(w =>
            {
                while (runner)
                {
                    if (logQueue.Any())
                    {
                        var message = string.Empty;
                        lock (locker)
                        {
                            message = logQueue.Dequeue();
                        }
                        File.AppendAllText(currentPath + "\\Log\\log.txt", "【" +
                                                                           DateTime.Now.ToString(
                                                                               "yyyy-MM-dd HH:mm:ss:fff") + "】" +
                                                                           message);
                    }
                    Thread.Sleep(100);
                }
            });
        }




    }
}
