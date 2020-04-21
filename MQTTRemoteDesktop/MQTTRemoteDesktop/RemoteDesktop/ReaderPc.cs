using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;

namespace RemoteDesktop
{
    public class ReaderPc
    {
        /// <summary>
        /// MD5 加密
        /// </summary>
        /// <param name="strPwd">加密字符串</param>
        /// <returns>加密后字符串</returns>
        public static byte[] Encrypt(string strPwd)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = System.Text.Encoding.Default.GetBytes(strPwd);//将字符编码为一个字节序列 
            byte[] md5data = md5.ComputeHash(data);//计算data字节数组的哈希值 
            md5.Clear();
            return md5data;
        }


        ///<summary> 
        ///获取cpu序列号     
        ///</summary> 
        ///<returns> string </returns> 
        public static string GetCpuInfo()
        {
            string cpuInfo = " ";
            using (ManagementClass cimobject = new ManagementClass("Win32_Processor"))
            {
                ManagementObjectCollection moc = cimobject.GetInstances();

                foreach (ManagementObject mo in moc)
                {
                    cpuInfo = mo.Properties["ProcessorId"].Value.ToString();
                    mo.Dispose();
                }
            }
            return cpuInfo;
        }


        /// <summary>
        /// 获取bios序列号
        /// </summary>
        /// <returns></returns>
        public static string GetBiosInfo()
        {
            ManagementClass mc = new ManagementClass("Win32_BIOS");
            ManagementObjectCollection moc = mc.GetInstances();
            string strID = null;
            foreach (ManagementObject mo in moc)
            {
                strID = mo.Properties["SerialNumber"].Value.ToString();
                break;
            }

            return strID;

        }
        /// <summary>
        /// 获取主板序列号
        /// </summary>
        /// <returns></returns>
        public static string GetMotherBoardSerialNumber()
        {
            ManagementClass mc = new ManagementClass("WIN32_BaseBoard");
            ManagementObjectCollection moc = mc.GetInstances();
            string SerialNumber = "";
            foreach (ManagementObject mo in moc)
            {
                SerialNumber = mo["SerialNumber"].ToString();
                break;
            }

            return SerialNumber;
        }

        ///<summary> 
        ///获取网卡硬件地址 
        ///</summary> 
        ///<returns> string </returns> 
        public static string GetMoAddress()
        {
            string address = " ";
            using (ManagementClass mc = new ManagementClass("Win32_NetworkAdapterConfiguration"))
            {
                ManagementObjectCollection moc2 = mc.GetInstances();
                foreach (ManagementObject mo in moc2)
                {
                    if ((bool)mo["IPEnabled"] == true)
                        address = mo["MacAddress"].ToString();
                    mo.Dispose();
                }
            }
            return address;
        }
        //生成机器码
        public static byte[] GetCpuID()
        {
            string msg = GetCpuInfo() + GetBiosInfo() + GetMotherBoardSerialNumber() + GetMoAddress().Trim();
            return Encrypt(msg);
        }
     
    }
}
