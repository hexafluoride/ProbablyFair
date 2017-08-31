using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProbablyFair
{
    [Serializable]
    public class LogEntry
    {
        public DateTime Time { get; set; }
        public ulong Index { get; set; }
        public byte[] RawResult { get; set; }
        public object Result { get; set; }
        public ResultType Type { get; set; }
        public Array Params { get; set; }
        public string Tag { get; set; }

        public LogEntry()
        {
            Time = DateTime.UtcNow;
        }

        public override string ToString()
        {
            string params_str = "";

            if(Params != null && Params.Length > 0)
            {
                for(int i = 0; i < Params.Length - 1; i++)
                {
                    params_str += Params.GetValue(i).ToString() + ", ";
                }

                params_str += Params.GetValue(Params.Length - 1);
                params_str = string.Format("[{0}]", params_str);
            }

            return string.Format("[Time={0}, Tag={1}{6}, Index={2}, Type={3}, Result={4}, RawResult={5}]", Time, Tag, Index, Type, Result, RawResult.ToShortString(), params_str);
        }
    }

    public static class Extensions
    {
        public static string ToUsefulString(this byte[] arr)
        {
            return BitConverter.ToString(arr).Replace("-", "").ToLower();
        }

        public static string ToShortString(this byte[] arr)
        {
            if(arr.Length < 4)
            {
                return BitConverter.ToString(arr).Replace("-", "").ToLower();
            }

            return string.Format("{0:X2}{1:X2}..{2:X2}{3:X2}", arr[0], arr[1], arr[arr.Length - 2], arr[arr.Length - 1]).ToLower();
        }
    }

    public enum ResultType
    {
        Integer,
        Long,
        Double,
        Boolean,
        ByteArray
    }
}
