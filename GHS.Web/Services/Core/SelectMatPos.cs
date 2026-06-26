namespace GHS.Web.Services.Core;

/// <summary>
/// Determines the material position (mat_pos) by matching the input against configured types.
/// Migrated from GHPHandShake.Services.SelectMatPos.
/// Bug fixed: the original had a missing {} after MessageBox.Show, causing return 0 to always execute.
/// </summary>
public class SelectMatPos
{
    private static readonly HashSet<string> AllTypeNames = new()
    {
        "1","2","3","4","5","6","7","8","9","10","11","12","13","14","15","16","17","18","19"
    };

    private readonly MaterialClassifier _classifier;

    public SelectMatPos(MaterialClassifier classifier)
    {
        _classifier = classifier;
    }

    /// <summary>
    /// Get the material position (1-19) based on input matching.
    /// Returns 0 if no match found.
    /// </summary>
    public int GetMatPos(string input, List<MaterialTypeInfo> config)
    {
        // Clean dashes and spaces
        input = input.Replace("-", "").Replace(" ", "");

        string? matchValue = _classifier.GetTypeBySubTypeMatch(input, config);

        if (matchValue != null && AllTypeNames.Contains(matchValue))
        {
            return Convert.ToInt32(matchValue);
        }

        return 0;
    }
}
