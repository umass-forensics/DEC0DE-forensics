/**
 * Copyright (C) 2012 University of Massachusetts, Amherst
 * Brian Lynn
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Security.Cryptography;

namespace Dec0de.UI
{
    static class DcUtils
    {
        /// <summary>
        /// Converts a byte array to a hex string.
        /// </summary>
        /// <param name="bytes">Bytes whose hexadecimal equivalent is required.</param>
        /// <returns>The hexadecimal string equivalent of input bytes.</returns>
        public static string BytesToHex(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) {
                hex.AppendFormat("{0:x2}", b); // lower case
            }
            return hex.ToString();
        }

        /// <summary>
        /// Disables the application wait cursor if currently set.
        /// </summary>
        public static void ResetWaitCursor()
        {
            if (Application.UseWaitCursor) Application.UseWaitCursor = false;
        }

        /// <summary>
        /// Enables the use of the application wait cursor. Sends a Windows
        /// message to force the cursor to update.
        /// </summary>
        /// <param name="handle"></param>
        public static void SetWaitCursor(IntPtr handle)
        {
            if (Application.UseWaitCursor) {
                return;
            }
            Application.UseWaitCursor = true;
            if (handle != null) {
                try {
                    SendMessage(handle, 0x20, handle, (IntPtr)1);
                } catch {
                }
            }
        }
		
        /// <summary>
        /// Calculates the SHA1 hash of the entire memory file.
        /// </summary>
        /// <param name="filePath">The path to the phone's memory file.</param>
        /// <returns>The SHA1 hash of the memory file.</returns>

		public static string CalculateFileSha1(string filePath)
		{
		    string fileSha1 = null;
            try
            {                
                FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fileSha1 = DcUtils.BytesToHex((new SHA1Managed()).ComputeHash(fs));
                fs.Close();
            }
            catch (ThreadAbortException)
            {
                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "File Hash", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return null;
            }
            return fileSha1;
		}

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);

    }
}
