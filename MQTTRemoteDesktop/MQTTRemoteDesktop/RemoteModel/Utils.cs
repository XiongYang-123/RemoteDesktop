using Microsoft.Win32.SafeHandles;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace RemoteModel
{
    public static class Utils
    {
        public static DateTime TimeBaseline = new DateTime(1970, 1, 1);

        public static uint GetInt(this byte[] data)
        {
            return (uint)((data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3]);
        }
        public static byte[] GetBytes(this int i)
        {
            return new byte[] { (byte)((i >> 24) & 0xff), (byte)((i >> 16) & 0xff), (byte)((i >> 8) & 0xff), (byte)(i & 0xff) };
        }
        public static byte[] GetBytes(this uint i)
        {
            return new byte[] { (byte)((i >> 24) & 0xff), (byte)((i >> 16) & 0xff), (byte)((i >> 8) & 0xff), (byte)(i & 0xff) };
        }
        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        /// <summary>
        /// 图片压缩(降低质量以减小文件的大小)
        /// </summary>
        /// <param name="srcBitmap">传入的Bitmap对象</param>
        /// <param name="destStream">压缩后的Stream对象</param>
        /// <param name="level">压缩等级，0到100，0 最差质量，100 最佳</param>
        public static byte[] Compress(this Bitmap srcBitmap, long level)
        {
            MemoryStream destStream = new MemoryStream();
            ImageCodecInfo myImageCodecInfo;
            System.Drawing.Imaging.Encoder myEncoder;
            EncoderParameter myEncoderParameter;
            EncoderParameters myEncoderParameters;

            // Get an ImageCodecInfo object that represents the JPEG codec.
            myImageCodecInfo = GetEncoderInfo("image/jpeg");

            // Create an Encoder object based on the GUID

            // for the Quality parameter category.
            myEncoder = System.Drawing.Imaging.Encoder.Quality;

            // Create an EncoderParameters object.
            // An EncoderParameters object has an array of EncoderParameter
            // objects. In this case, there is only one

            // EncoderParameter object in the array.
            myEncoderParameters = new EncoderParameters(1);

            // Save the bitmap as a JPEG file with 给定的 quality level
            myEncoderParameter = new EncoderParameter(myEncoder, level);
            myEncoderParameters.Param[0] = myEncoderParameter;
            srcBitmap.Save(destStream, myImageCodecInfo, myEncoderParameters);
            return destStream.GetBuffer();
        }



        /// <summary>
        /// 位图转字节
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static byte[] Bitmap2Bytes(this Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] bytes = ms.GetBuffer();  //byte[]   bytes=   ms.ToArray(); 这两句都可以，至于区别么，下面有解释
            ms.Close();
            return bytes;
        }
        /// <summary>
        /// 字节转位图
        /// </summary>
        /// <param name="Data"></param>
        /// <returns></returns>
        public static Bitmap Bytes2BitMap(this byte[] Data)
        {
            MemoryStream ms1 = new MemoryStream(Data);
            Bitmap bm = (Bitmap)Image.FromStream(ms1);
            ms1.Close();
            return bm;
        }
        /// <summary>
        /// Ascii 编码转换字符串
        /// </summary>
        /// <param name="txt"></param>
        /// <returns></returns>
        public static byte[] ToAsciiArray(this string txt)
        {
            return Encoding.ASCII.GetBytes(txt);
        }
        /// <summary>
        /// 采用JSON序列化来深克隆对象
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        public static T DeepClone<T>(this T source)
        {
            // Don't serialize a null object, simply return the default for that object
            if (ReferenceEquals(source, null)) return default(T);
            return (T)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(source, Formatting.None), typeof(T));
        }
        public static string GetMD5String(string str)
        {
            return new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(str)).ToHexString();
        }
        /// <summary>   
        /// 获取数组的子串
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="index"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            try
            {
                Array.Copy(data, index, result, 0, length);
            }
            catch { }
            return result;
        }


        /// <summary>
        /// 右侧补齐指定的数组至指定长度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static T[] PadRight<T>(this T[] data, int length)
        {
            return data.Length >= length ? data : data.Concat(new T[length - data.Length]).ToArray();

        }
        /// <summary>
        /// 左侧补齐指定的数组至指定长度
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="data"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public static T[] PadLeft<T>(this T[] data, int length)
        {
            return data.Length >= length ? data : new T[length - data.Length].Concat(data).ToArray();

        }
        public static string ToASCIIString(this byte[] bytes)
        {
            if (bytes?.Length == 0)
                return null;
            return Encoding.ASCII.GetString(bytes);
        }
        /// <summary>
        /// 输出字节数组的十六进制字符串,默认是"t"参数
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="format">t=tight,每个数字之间没有空格分隔  T-输出大写, s=space,用空格分隔，S-输出大写。</param>
        /// <returns></returns>
        public static string ToHexString(this byte[] bytes, string format = "")
        {
            if (bytes == null) return "";
            switch (format)
            {
                case "s":
                    return string.Join(" ", bytes.Select(i => i.ToString("x2")).ToArray());
                case "S":
                    return string.Join(" ", bytes.Select(i => i.ToString("X2")).ToArray());
                case "T":
                    return string.Join("", bytes.Select(i => i.ToString("X2")).ToArray());
                case "t":
                default:
                    return string.Join("", bytes.Select(i => i.ToString("x2")).ToArray());
            }
        }
        /// <summary>
        /// 将十六进制字符串转换为字节数组，不可转换的字节会被置零，奇数长度字符串会左侧补0修整为最接近的偶数长度。
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] Hex2Bytes(string hexString)
        {
            byte[] array = null;
            if (!string.IsNullOrEmpty(hexString))
            {
                //左侧补0，使hexstring长度为偶数
                while (hexString.Length % 2 > 0) hexString = "0" + hexString;

                array = new byte[hexString.Length / 2]; //声明输出字节数组的长度
                for (int i = 0; i < hexString.Length; i += 2)
                {
                    try
                    {
                        array[i / 2] = Convert.ToByte(hexString.Substring(i, 2), 16);
                    }
                    catch
                    {
                        //如果遇到不能转换的字符，则相应字节保留为默认值：0x00
                    }
                }
            }
            return array;
        }
        public static string ToCompactJsonString<T>(this T obj)
        {
            return JsonConvert.SerializeObject(obj, Formatting.None, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        }
        public static byte[] ToCompactJsonByteArray<T>(this T obj)
        {
            return Encoding.ASCII.GetBytes(obj.ToCompactJsonString());
        }
        public static Random random = new Random();
        public static string Passwords = "0123456789qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM";
        public static string RandomPassword()
        {
            string str = "";
            for (int i = 0; i < 6; i++)
                str+=Passwords[random.Next(0, Passwords.Length)];
            return str;
        }

        public static byte[] RandomBytes(int length)
        {
            byte[] buffer = new byte[length];
            random.NextBytes(buffer);
            return buffer;
        }
        /// <summary>
        /// 将字节数组转换成Lora标准的Base64字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToLoraBase64Str(this byte[] bytes)
        {
            return bytes == null ? "" : Convert.ToBase64String(bytes).TrimEnd('=');
        }
        /// <summary>
        /// 将字节数组转换成Lora标准的Base64字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] FromLoraBase64Str(string str)
        {
            byte[] buf = new byte[0];
            if (!string.IsNullOrEmpty(str))
            {
                int mod = str.Length % 4;
                if (mod == 2) str += "==";
                else if (mod == 3) str += "=";

                try
                {
                    buf = Convert.FromBase64String(str);
                }
                catch { }
            }
            return buf;
        }
        public static bool IsNullOrEmpty(this byte[] bytes)
        {
            return bytes == null || bytes.Length == 0;
        }
        public static byte[] ToByteArray(this int num)
        {
            return new byte[] { (byte)((num >> 24) & 0xff), (byte)((num >> 16) & 0xff), (byte)((num >> 8) & 0xff), (byte)(num & 0xff) };
        }
        public static byte[] ToByteArray(this long num)
        {
            return new byte[] { (byte)((num >> 56) & 0xff), (byte)((num >> 48) & 0xff), (byte)((num >> 40) & 0xff), (byte)((num >> 32) & 0xff), (byte)(num & 0xff), (byte)((num >> 24) & 0xff), (byte)((num >> 16) & 0xff), (byte)((num >> 8) & 0xff), (byte)(num & 0xff) };
        }
        /// <summary>
        /// 判断字符串是否是合格的HexString，字符数不是偶数将被判断为不合格
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static bool IsLegalHexStr(string hexString)
        {
            //左侧补0，使hexstring长度为偶数
            while (hexString.Length % 2 > 0) hexString = "0" + hexString;

            bool isEligible = true;
            for (int i = 0; i < hexString.Length; i = i + 2)
            {
                try
                {
                    Convert.ToByte(hexString.Substring(i, 2), 16);
                }
                catch
                {
                    isEligible = false;
                    break;
                }
            }
            return isEligible;
        }
        public static IPEndPoint ToIpEndPoint(this DnsEndPoint ep)
        {
            IPEndPoint ipEp = null;
            try
            {
                IPAddress ip = ep.Host == "0.0.0.0" ? IPAddress.Any : Dns.GetHostAddresses(ep.Host).FirstOrDefault();
                ipEp = new IPEndPoint(ip, ep.Port);
            }
            catch { }
            return ipEp;
        }
        public static String ToSimpleUdpEpStr(this DnsEndPoint ep)
        {
            string epStr = ep.ToString();
            if (epStr.Contains('/')) epStr = epStr.Split('/').LastOrDefault();
            return epStr;
        }
        public static DnsEndPoint ToDnsEndPoint(this IPEndPoint ep) => new DnsEndPoint(ep.Address.ToString(), ep.Port, System.Net.Sockets.AddressFamily.InterNetwork);
        public static IPEndPoint ParseIpEpStr(string epStr)
        {
            if (!String.IsNullOrEmpty(epStr) && epStr.Contains(':'))
            {
                string[] ep = epStr.Split(':');

                if (ep.Length == 2)
                {
                    if (IPAddress.TryParse(ep[0], out IPAddress ip) && ushort.TryParse(ep[1], out ushort port))
                    { return new IPEndPoint(ip, port); }
                }
            }
            return null;
        }
        public static DnsEndPoint ParseDnsEpStr(string epStr)
        {
            if (epStr != null)
            {
                try
                {
                    if (epStr.Contains('/')) epStr = epStr.Split('/').LastOrDefault();
                    if (!String.IsNullOrEmpty(epStr) && epStr.Contains(':'))
                    {
                        string[] ep = epStr.Split(':');
                        if (ep.Length == 2)
                        {
                            //port 最大值小于 63000
                            if ((Uri.CheckHostName(ep[0]) == UriHostNameType.Dns || Uri.CheckHostName(ep[0]) == UriHostNameType.IPv4) && ushort.TryParse(ep[1], out ushort port))
                            { return new DnsEndPoint(ep[0], port, System.Net.Sockets.AddressFamily.InterNetwork); }
                        }
                    }
                }
                catch { }
            }
            return null;
        }
        public static bool TryParseDnsEpStr(string epStr, out DnsEndPoint ep)
        {
            ep = ParseDnsEpStr(epStr);
            if (ep == null) return false;
            else return true;
        }
        /// <summary>
        /// 将byte数组转为浮点数
        /// 注意：如果字节数组长度大于4个字节，则只处理前4个字节；如果字节数组长度小于4个字节，则右侧填0后再转换；
        /// </summary>
        /// <param name="bResponse">byte数组</param>
        /// <returns></returns>
        public static float ToFloat(this byte[] bytes)
        {
            if (bytes.Length < 4) bytes.PadRight(4);

            return BitConverter.ToSingle(bytes.Reverse().ToArray(), 0);

        }
    }
}




