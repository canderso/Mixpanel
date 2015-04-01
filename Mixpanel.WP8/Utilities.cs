using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Text;

#if !NETFX
using Windows.Storage;
using System.Threading.Tasks;
#endif

namespace Mixpanel
{
    internal sealed class Utilities
    {
        public const int DefaultWrapSharingViolationsRetryCount = 10;
        public const int DefaultWrapSharingViolationsWaitTime = 100;

        /// <summary>
        /// Gets the default encoding used for byte conversions.
        /// </summary>
        /// <value>
        /// The default encoding.
        /// </value>
        public static Encoding DefaultEncoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }

        private Utilities()
        {
        }

        /// <summary>
        /// Converts a regular string to a base64 string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string ToBase64(string value)
        {
            if (value == null)
                return null;

            byte[] bytes = DefaultEncoding.GetBytes(value);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Converts a base64 string to a regular string.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string FromBase64(string value)
        {
            if (value == null)
                return null;

            byte[] bytes = Convert.FromBase64String(value);
            if (bytes == null)
                return null;

            return DefaultEncoding.GetString(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// Encodes a url string without a reference to the System.Web assembly. 
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>An encoded string.</returns>
        [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings")]
        public static string UrlEncode(string text)
        {
            if (text == null)
            {
                return null;
            }
            return Uri.EscapeDataString(text);
        }

        /// <summary>
        /// Decodes a url string without a reference to the System.Web assembly. 
        /// </summary>
        /// <param name="text">The input text.</param>
        /// <returns>A decoded string.</returns>
        [SuppressMessage("Microsoft.Design", "CA1055:UriReturnValuesShouldNotBeStrings")]
        public static string UrlDecode(string text)
        {
            if (text == null)
            {
                return null;
            }
            return Uri.UnescapeDataString(text.Replace("+", " "));
        }

        /// <summary>
        /// Converts a unix based number of seconds into a .NET DateTime value (UTC).
        /// </summary>
        /// <param name="unixDateTicks">The number of seconds since January 1st, 1970.</param>
        /// <returns></returns>
        public static DateTime ToDateTime(long unixDateTicks)
        {
            if (unixDateTicks <= 0)
                return DateTime.MinValue;

            DateTime unixStartDate = new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            return unixStartDate.AddSeconds(unixDateTicks);
        }

        /// <summary>
        /// Converts a .NET DateTime into a unix "Epoch Time" based number of seconds.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns></returns>
        public static long ToEpochTime(DateTime date)
        {
            DateTime unixStartDate = new DateTime(1970, 01, 01, 0, 0, 0, DateTimeKind.Utc);
            if (date <= unixStartDate)
                return 0;

            TimeSpan diff = date - unixStartDate;
            // Rounded to the nearest 64-bit signed integer. 
            // If value is halfway between two whole numbers, the even number is returned; that is, 4.5 is converted to 4, and 5.5 is converted to 6.
            return Convert.ToInt64(diff.TotalSeconds);
        }

        /// <summary>
        /// /// Converts an object to a boolean.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static bool ToBoolean(object value, bool defaultValue)
        {
            if (value == null)
                return defaultValue;

            if (value is bool)
                return (bool)value;

            if (value is bool?)
            {
                bool? nullable = (bool?)value;
                return nullable.GetValueOrDefault(defaultValue);
            }

            return ToBoolean(value.ToString(), defaultValue);
        }

        /// <summary>
        /// Converts a string to a boolean.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns></returns>
        public static bool ToBoolean(string text, bool defaultValue)
        {
            if (string.IsNullOrEmpty(text))
                return defaultValue;

            text = text.Trim();
            if (text == "0")
                return false;

            if (text == "1")
                return true;

            if (text == "-1")
                return false;

            switch (text.ToUpperInvariant())
            {
                case "TRUE":
                case "YES":
                case "Y":
                    return true;

                case "FALSE":
                case "NO":
                case "N":
                    return false;
            }

            bool flag;
            if (bool.TryParse(text, out flag))
                return flag;

            int num;
            if (int.TryParse(text, out num))
                return (num != 0);

            return defaultValue;
        }

#if NETFX
        public delegate void WrapSharingViolationsCallback();

        public delegate bool WrapSharingViolationsExceptionsCallback(IOException ioe, int retry, int retryCount, int waitTime);

        public static void WrapSharingViolations(WrapSharingViolationsCallback action)
        {
            WrapSharingViolations(action, DefaultWrapSharingViolationsRetryCount, DefaultWrapSharingViolationsWaitTime);
        }

        public static void WrapSharingViolations(WrapSharingViolationsCallback action, int retryCount, int waitTime)
        {
            WrapSharingViolations(action, null, retryCount, waitTime);
        }

        public static void WrapSharingViolations(WrapSharingViolationsCallback action, WrapSharingViolationsExceptionsCallback exceptionsCallback, int retryCount, int waitTime)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            for (int i = 0; i < retryCount; i++)
            {
                try
                {
                    action();
                    return;
                }
                catch (IOException ioe)
                {
                    if (IsSharingViolation(ioe) && i < (retryCount - 1))
                    {
                        bool wait = true;
                        if (exceptionsCallback != null)
                        {
                            wait = exceptionsCallback(ioe, i, retryCount, waitTime);
                        }
                        if (wait)
                        {
                            System.Threading.Thread.Sleep(waitTime);
                        }
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public static bool IsSharingViolation(IOException exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            int hr = GetHResult(exception, 0);
            return hr == -2147024864; // 0x80070020 ERROR_SHARING_VIOLATION
        }

        public static int GetHResult(IOException exception, int defaultValue)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            try
            {
                const string name = "HResult";
                PropertyInfo pi = exception.GetType().GetProperty(name, BindingFlags.NonPublic | BindingFlags.Instance); // CLR2
                if (pi == null)
                {
                    pi = exception.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance); // CL4
                }
                if (pi != null)
                    return (int)pi.GetValue(exception, null);
            }
            catch
            {
            }
            return defaultValue;
        }
#endif

#if !NETFX
        /// <summary>
        /// Gets a file if it exists, null otherwise.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public static async Task<StorageFile> SafeGetFile(string path)
        {
            StorageFile file = null;
            try
            {
                file = await StorageFile.GetFileFromPathAsync(path);
            }
            catch (FileNotFoundException)
            {
                // http://social.msdn.microsoft.com/Forums/en-US/winappswithcsharp/thread/1eb71a80-c59c-4146-aeb6-fefd69f4b4bb/
                // The only way to check if a file exists is to catch the FileNotFoundException.
                Debug.WriteLine("SafeGetFile - File not found: path=" + path);
            }
            return file;
        }

        /// <summary>
        /// Gets a file if it exists, null otherwise.
        /// </summary>
        /// <param name="folder">The folder.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <returns></returns>
        public static async Task<StorageFile> SafeGetFile(StorageFolder folder, string fileName)
        {
            StorageFile file = null;
            try
            {
                file = await folder.GetFileAsync(fileName);
            }
            catch (FileNotFoundException)
            {
                // http://social.msdn.microsoft.com/Forums/en-US/winappswithcsharp/thread/1eb71a80-c59c-4146-aeb6-fefd69f4b4bb/
                // The only way to check if a file exists is to catch the FileNotFoundException.
                Debug.WriteLine("SafeGetFile - File not found: parentFolder=" + folder.Name + ", fileName=" + fileName);
            }
            return file;
        }
#endif
    }
}
