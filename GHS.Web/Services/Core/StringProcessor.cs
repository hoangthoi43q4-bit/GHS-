using System.Text.RegularExpressions;

namespace GHS.Web.Services.Core;

/// <summary>
/// SECS/GEM protocol message parser and LOAD_MATERIAL command generator.
/// Migrated from GHPHandShake.Views.StringProcessor.
/// Bug fix: SelectMatPos logic integrated with proper scoping.
/// </summary>
public class StringProcessor
{
    /// <summary>
    /// Parse the raw scan input (P@V@3S format) into a structured material UID.
    /// P segment is padded to 18 chars, V segment to 10 chars.
    /// </summary>
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
                pContent = pContent.PadLeft(18, '0');
            }
            else if (segment.StartsWith("V") && string.IsNullOrEmpty(vContent))
            {
                vContent = segment.Length > 1 ? segment.Substring(1) : "";
                vContent = vContent.PadLeft(10, '0');
            }
            else if (segment.StartsWith("3S") && string.IsNullOrEmpty(s3Content))
            {
                s3Content = segment.Length > 2 ? segment.Substring(2) : "";
            }
        }

        return $"{pContent}@{vContent}@{s3Content}";
    }

    /// <summary>
    /// Generate the LOAD_MATERIAL command string for TCP transmission.
    /// </summary>
    public string GenerateLoadCommand(
        string input,
        string stationName,
        int matPos,
        int tokens = 3)
    {
        string processed = Process(input);
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        return $"LOAD_MATERIAL,{stationName},10,{timestamp}," +
               $"<LoadMaterial mat_pos_1=\"{matPos}\" mat_uid_1=\"{processed}\" tokens=\"{tokens}\" />";
    }

    /// <summary>
    /// 生成 UNLOAD_MATERIAL 下料指令。
    /// 格式：UNLOAD_MATERIAL,{stationName},10,{yyyyMMddHHmmssfff},
    ///       &lt;UnLoadMaterial mat_pos_1="{position}" mat_uid_1="{uid}" tokens="3" /&gt;
    /// - position：来自 XML 的 Position
    /// - uid：来自 XML 的完整 Id（原样，不去零、不截 @）
    /// - 10 与 tokens=3 按现场约定固定
    /// </summary>
    public string GenerateUnloadCommand(string stationName, string position, string uid, int tokens = 3)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmssfff");

        return $"UNLOAD_MATERIAL,{stationName},10,{timestamp}," +
               $"<UnLoadMaterial mat_pos_1=\"{position}\" mat_uid_1=\"{uid}\" tokens=\"{tokens}\" />";
    }

    /// <summary>
    /// Extract the material number (P segment) from raw scan input.
    /// </summary>
    public string FindMaterial(string input)
    {
        string[] segments = input.Split(new char[] { '@' }, StringSplitOptions.None);

        string material = "";

        foreach (string segment in segments)
        {
            if (!string.IsNullOrEmpty(material))
            {
                break;
            }

            if (segment.StartsWith("P") && string.IsNullOrEmpty(material))
            {
                material = segment.Length > 1 ? segment.Substring(1) : "";
                material = RemoveDashAndSpace(material);
            }
        }

        return material;
    }

    /// <summary>
    /// Remove dashes and spaces from material number for matching.
    /// </summary>
    private static string RemoveDashAndSpace(string input)
    {
        return input.Replace("-", "").Replace(" ", "");
    }
}
