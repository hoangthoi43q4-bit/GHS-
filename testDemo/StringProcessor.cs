using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace testDemo
{
    internal class StringProcessor
    {
        public string Process(string input)
        {
            string[] segments = input.Split(new char[] { '@' }, StringSplitOptions.None);

            string pContent = "";
            string vContent = "";
            string s3Content = "";

            foreach (string segment in segments)
            {
                if (!string.IsNullOrEmpty(pContent) &&
                    !string.IsNullOrEmpty(vContent) &&
                    !string.IsNullOrEmpty(s3Content))
                {
                    break;
                }

                if (segment.StartsWith("P") && string.IsNullOrEmpty(pContent))
                {
                    pContent = segment.Length > 1 ? segment.Substring(1) : "";
                }
                else if (segment.StartsWith("V") && string.IsNullOrEmpty(vContent))
                {
                    vContent = segment.Length > 1 ? segment.Substring(1) : "";
                }
                else if (segment.StartsWith("3S") && string.IsNullOrEmpty(s3Content))
                {
                    s3Content = segment.Length > 2 ? segment.Substring(2) : "";
                }
            }

            return $"00000{pContent}@0{vContent}@{s3Content}";
        }

        public string GenerateLoadCommand(
            string input,
            string StationName ,
            int matPos = 1,
            int tokens = 3)
        {
            string processed = Process(input);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

            return $"LOAD_MATERIAL,{StationName},10,{timestamp}," +
                   $"<LoadMaterial mat_pos_1=\"{matPos}\" mat_uid_1=\"{processed}\" tokens=\"{tokens}\" />";
        }
    }
}
