using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using GHPHandShake.Models;


namespace GHPHandShake.Services
{
    public static class SelectMatPos
    {
        public static int GetMatPos(string input ,MaterialConfig config  )
        {
            string MatchValue = MaterialClassifier.GetTypeBySubTypeMatch(input, config);

            if (Common.Constants.AllTypeNames.Contains(MatchValue))
            {
                return Convert.ToInt32(MatchValue);
            }
            else
                MessageBox.Show("没有匹配得Mat_Pos");
                return 0;
        }
    }
}
