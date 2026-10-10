using System;
using System.Security.Cryptography;
using System.Text;

namespace BE.LabelExtension.Core.Labels
{
    /// <summary>
    /// Creates the label part of new IDs: the letter <c>L</c> and eight cryptographically
    /// random bytes as 16 hexadecimal digits in upper case, for example <c>L3F2A9C15B8047DE1</c>.
    /// The label file stands in front as before, the user ID of the existing tool is dropped.
    /// </summary>
    public static class LabelKeyGenerator
    {
        private static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();

        /// <summary>Creates a new label part.</summary>
        /// <returns>The label part, for example <c>L3F2A9C15B8047DE1</c>.</returns>
        public static string NewKey()
        {
            var bytes = new byte[8];
            lock (Random)
            {
                Random.GetBytes(bytes);
            }

            var key = new StringBuilder("L", 17);
            foreach (byte value in bytes)
            {
                key.Append(value.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
            }

            return key.ToString();
        }

        /// <summary>Creates a new label part that does not exist yet.</summary>
        /// <param name="exists">Whether a label part is taken, for example in the label file.</param>
        /// <returns>A label part that is not taken.</returns>
        public static string NewKey(Func<string, bool> exists)
        {
            string key;
            do
            {
                key = NewKey();
            }
            while (exists(key));

            return key;
        }
    }
}
