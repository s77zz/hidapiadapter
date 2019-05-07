﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace HidApiAdapter
{
    public class HidDevice
    {
        private const int MARSHALED_STRING_MAX_LEN = 1024 / 2;

        private const int BUFFER_DEFAULT_SIZE = 64;

        private readonly hid_device_info m_DeviceInfo;
																 

        public IntPtr DevicePtr { get; private set; } = IntPtr.Zero;
        private HidDevice() { }

        internal HidDevice(hid_device_info deviceInfo, IntPtr devicePtr)
        {
            m_DeviceInfo = deviceInfo;
            DevicePtr = devicePtr;
        }

        /// <summary>
        /// Can instance interact with HID device 
        /// </summary>
        public bool IsValid => DevicePtr != IntPtr.Zero;

        private bool m_IsConnected;
        /// <summary>
        /// Device connected successful
        /// </summary>
        public bool IsConnected => m_IsConnected;

        public int VendorId => m_DeviceInfo.vendor_id;
        public int ProductId => m_DeviceInfo.product_id;

        /// <summary>
        /// Platform-specific device path
        /// </summary>
        /// <returns></returns>
        public string Path() => 
            Marshal.PtrToStringAnsi(m_DeviceInfo.path);

        /// <summary>
        /// Connect to HID device
        /// </summary>
        /// <returns></returns>
        public bool Connect()
        {
            if (DevicePtr == IntPtr.Zero)
                return false;

            DevicePtr = HidApi.hid_open_path(m_DeviceInfo.path);

            if(DevicePtr != IntPtr.Zero)
                m_IsConnected = true;

            return true;
        }

        /// <summary>
        /// Disconnect from HID device
        /// </summary>
        /// <returns></returns>
        public bool Disconnect()
        {
            if (DevicePtr == IntPtr.Zero)
                return false;

            HidApi.hid_close(DevicePtr);

            m_IsConnected = false;
            return true;
        }

        private byte[] m_WriteBuffer = new byte[BUFFER_DEFAULT_SIZE];

        public int Write(byte[] bytes)
        {
            if (DevicePtr == IntPtr.Zero)
                return 0;

            if (bytes == null || bytes.Length == 0)
                return 0;

            //if (m_WriteBuffer.Length <= bytes.Length)
            //    Array.Resize(ref m_WriteBuffer, bytes.Length + 2);

            //TODO fix this for other OSs
            //hidapi for windows has problem - first byte must be 0 also array length shuold be increased for 1
            //Array.Copy(bytes, 0, m_WriteBuffer, 1, bytes.Length);

            return HidApi.hid_write(DevicePtr, bytes, Convert.ToUInt32(bytes.Length ));
        }
        
        public int Read(byte[] buff, int len)
        {
            if (DevicePtr == IntPtr.Zero)
                return 0;

            return HidApi.hid_read(DevicePtr, buff, Convert.ToUInt32(len));
        }

        public int Read(byte[] buff, int len,int timeout)
        {
            if (DevicePtr == IntPtr.Zero)
                return 0;

            return HidApi.hid_read_timeout(DevicePtr, buff, Convert.ToUInt32(len),timeout);
        }
        /// <summary>
        /// Set the device handle to be non-blocking. 
        /// In non-blocking mode calls to hid_read() will return immediately with a value of 0 if there is no data to be read. 
        /// In blocking mode, hid_read() will wait(block) until there is data to read before returning.
        /// Nonblocking can be turned on and off at any time 
        /// </summary>
        /// <param name="nonblocking">Enable(1) or not(0) the nonblocking reads</param>
        /// <returns></returns>
        public int SetNonblocking(int nonblocking)
        {
            //if (DevicePtr == IntPtr.Zero)
            //    return 0;


            return HidApi.hid_set_nonblocking(DevicePtr,  nonblocking);
        }


        #region device info

        StringBuilder m_DeviceInfoBuffer = new StringBuilder(BUFFER_DEFAULT_SIZE);

        /// <summary>
        /// Device serial number
        /// </summary>
        /// <returns></returns>
        public string SerialNumber()
        {
            m_DeviceInfoBuffer.Clear();
            HidApi.hid_get_serial_number_string(DevicePtr, m_DeviceInfoBuffer, MARSHALED_STRING_MAX_LEN);

            return m_DeviceInfoBuffer.ToString();
        }


        /// <summary>
        /// Device manufacturer
        /// </summary>
        /// <returns></returns>
        public string Manufacturer()
        {
            m_DeviceInfoBuffer.Clear();
            HidApi.hid_get_manufacturer_string(DevicePtr, m_DeviceInfoBuffer, MARSHALED_STRING_MAX_LEN);

            return m_DeviceInfoBuffer.ToString();
        }

        /// <summary>
        /// Device product
        /// </summary>
        /// <returns></returns>
        public string Product()
        {
            m_DeviceInfoBuffer.Clear();
            HidApi.hid_get_product_string(DevicePtr, m_DeviceInfoBuffer, MARSHALED_STRING_MAX_LEN);

            return m_DeviceInfoBuffer.ToString();
        }

        /// <summary>
        /// Get all available strings of HID device 
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> DeviceStrings()
        {
            const int maxStringNum = 16;
            int counter = 0;

            m_DeviceInfoBuffer.Clear();
            while (HidApi.hid_get_indexed_string(DevicePtr, counter, m_DeviceInfoBuffer, MARSHALED_STRING_MAX_LEN) == 0 && counter++ < maxStringNum)
            {
                yield return m_DeviceInfoBuffer.ToString();
                m_DeviceInfoBuffer.Clear();
            }
        }

        /// <summary>
        /// Get a string from a HID device, based on its string index
        /// </summary>
        /// <param name="index">The index of the string to get</param>
        /// <returns></returns>
        public string DevicesString(int index)
        {
            m_DeviceInfoBuffer.Clear();

            var res = HidApi.hid_get_indexed_string(DevicePtr, index, m_DeviceInfoBuffer, MARSHALED_STRING_MAX_LEN);

            return res == 0 ? m_DeviceInfo.ToString() : null;
        }

        #endregion

        public override string ToString()
        {
            if (IsValid)
                return $"manufacturer: {Manufacturer()}, serial_number:{SerialNumber()}, product:{Product()}";
            else
                return "unknown device (not connected)";
        }

    }
}
