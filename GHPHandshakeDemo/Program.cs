using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace GHPHandshakeDemo
{
    internal class Program
    {

        static void Main(string[] args)
        {
            string input = "[)>@06@12S0002@P1514380207100@1PO336066@31PO336066@12VC11009321@10VCHN-NINGGUO@2P@20P@6D20241021@14D20261021@30PN@ZN@K5200009283@16K4524102101@V700027676@3SUCSC000130965@Q121NAR000@20T1@1TD202410214179@2T@1ZACU@@";

            var processor = new StringProcessor();

            string finalOutput = processor.GenerateLoadCommand(input);

            

            Console.WriteLine(finalOutput);
            Console.ReadLine();
        }
    }

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
            string commandName = "HGW101_AG030_LOAD",
            int matPos = 1,
            int tokens = 3)
        {
            string processed = Process(input);
            string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

            return $"LOAD_MATERIAL,{commandName},10,{timestamp}," +
                   $"<LoadMaterial mat_pos_1=\"{matPos}\" mat_uid_1=\"{processed}\" tokens=\"{tokens}\" />";
        }
    }
}
