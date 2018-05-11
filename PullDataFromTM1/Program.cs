using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using JabilTM1RestAPI;
using JabilTM1RestAPI.Parameters;

namespace PullDataFromTM1
{
    class Program
    {
        static Tm1 tm1One;
        static Tm1 tm1Two;
        static string CSVOutput = @"D:\DeveloperEnvironments\Matt\TXT Dump\CSVOutputs\";
        static bool IgnoreNoDelta = true;
        static int CellNumber = 0;
        static string MDXLocation = @"D:\DeveloperEnvironments\Matt\PullDataFromTM1\TxtConfigs\MDX.txt";
        static string PeriodlistLocation = @"D:\DeveloperEnvironments\Matt\PullDataFromTM1\TxtConfigs\Periods.txt";

        static string[] Versions = { "Virtual 8 Quarter", "8 Quarter" };

        static void Main(string[] args)
        {

            tm1One = LoginToTM1(ConfigurationManager.AppSettings["tm1ServerOne"].ToString(), ConfigurationManager.AppSettings["CAMURLOne"].ToString());
            tm1Two = LoginToTM1(ConfigurationManager.AppSettings["tm1ServerTwo"].ToString(), ConfigurationManager.AppSettings["CAMURLTwo"].ToString());

            //var Views = new JabilTM1RestAPI.Resources.Cellset[2];
            
            //var periods = System.IO.File.ReadAllLines(PeriodlistLocation);
           // var mdxString = System.IO.File.ReadAllText(MDXLocation);

            ///var mdx = new MdxParameter();

            

           

        }



        public static Tm1 LoginToTM1(string serverURL, string CAMURL)
        {
            var tm1 = new Tm1();
            tm1.Okta = bool.Parse(ConfigurationManager.AppSettings["OKTA"].ToString());
            tm1.Cam = bool.Parse(ConfigurationManager.AppSettings["CAM"].ToString());
            tm1.LoginUrl = CAMURL;
            tm1.ServerUrl = serverURL;
            tm1.UserName = ConfigurationManager.AppSettings["username"].ToString();
            tm1.Password = ConfigurationManager.AppSettings["password"].ToString();
            tm1.MaxTimeoutMinutes = int.Parse(ConfigurationManager.AppSettings["maxTimeout"].ToString());
            tm1.MaxConnections = 5;

            return tm1;
        }

        public static List<CellInfo> CombineViews(JabilTM1RestAPI.Resources.Cellset[] Views)
        {
            var CellCollections = CreateListOfCells(Views);
            var combinedLists = FindMatches(CellCollections);

            return combinedLists;
        }

        public static List<CellInfo> FindMatches(List<CellInfo>[] CellCollections)
        {
            List<CellInfo> combinedLists = new List<CellInfo>();
            List<CellInfo> bothLists = CellCollections[0].ToList();
            bothLists.AddRange(CellCollections[1]);

            foreach (var cell in CellCollections[0])
            {

                foreach (var compareCell in CellCollections[1])
                {

                    var match = CompareDims(cell, compareCell);

                    if (match)
                    {
                        cell.Values[1] = compareCell.Values[0];
                        cell.Delta = Math.Round(cell.Values[0], 2) - Math.Round(cell.Values[1], 2);

                        combinedLists.Add(cell);


                        bothLists.Remove(cell);
                        bothLists.Remove(compareCell);

                        break;
                    }

                }

            }



            foreach (var cell in bothLists)
            {

                cell.NoMatchFound = true;
                combinedLists.Add(cell);

            }

            return combinedLists;

        }

        public static List<CellInfo>[] CreateListOfCells(JabilTM1RestAPI.Resources.Cellset[] Views)
        {
            var CellCollections = new List<CellInfo>[Views.Length];

            for (var viewCnt = 0; viewCnt < Views.Length; viewCnt++)
            {
                var CellList = new List<CellInfo>();
                var cell = new CellInfo();
                var view = Views[viewCnt];

                GoThroughAxes(Views[viewCnt], 0, cell, CellList, 0);

                CellCollections[viewCnt] = CellList;
            }

            return CellCollections;
        }

        public static void GoThroughAxes(JabilTM1RestAPI.Resources.Cellset view, int AxesIndex, CellInfo cell, List<CellInfo> CellList, int priorAxisTupleCnt)
        {


            var oldTuple = cell.Dims.ToList();

            var a = view.Axes[AxesIndex];
            for (var cnt = 0; cnt < a.Tuples.Count; cnt++)
            {

                cell = new CellInfo();
                cell.Dims.AddRange(oldTuple);


                var t = a.Tuples[cnt];
                for (var index = 0; index < t.Members.Count; index++)
                {
                    cell.Dims.Add(t.Members[index].Name);
                }

                if (view.Axes.Count == (AxesIndex + 1))
                {
                    var cellIndex = (cnt * view.Axes[0].Tuples.Count) + priorAxisTupleCnt;
                    if (view.Cells[cellIndex].Value != null)
                    {
                        cell.Values[0] = double.Parse(view.Cells[cellIndex].Value.ToString());
                    }
                    
                    CellList.Add(cell);
                    CellNumber++;

                }
                else
                {
                    GoThroughAxes(view, (AxesIndex + 1), cell, CellList, cnt);
                }
            }

        }

        public static bool CompareDims(CellInfo firstCell, CellInfo secondCell)
        {
            bool isTheSame = false;
            var numCorrect = 0;

            foreach (var d1 in firstCell.Dims)
            {

                foreach (var d2 in secondCell.Dims)
                {
                    if (d1 == d2 || d1 == Versions[0] || d1 == Versions[1])
                    {

                        numCorrect++;
                        break;

                    }
                }
            }

            if (numCorrect == firstCell.Dims.Count) { isTheSame = true; }

            return isTheSame;

        }

        public static void MapToCSV(string FileLocation, List<CellInfo> combinedLists, bool IgnoreNoDelta)
        {

            List<string> csv = new List<string>();

            if (IgnoreNoDelta)
            {
                var list = new List<CellInfo>();

                foreach (var cell in combinedLists)
                {
                    if ((cell.Delta <= 0.01 && cell.Delta >= -0.01) && !cell.NoMatchFound)
                    {
                        list.Add(cell);
                    }
                }

                foreach (var cell in list)
                {
                    combinedLists.Remove(cell);
                }
            }

            foreach (var cell in combinedLists)
            {

                var s = new StringBuilder();

                if (cell.NoMatchFound)
                {
                    foreach (var dim in cell.Dims)
                    {
                        s.Append(dim + ",");
                    }

                    s.Append("No Match Found");
                }
                else
                {

                    foreach (var dim in cell.Dims)
                    {
                        s.Append(dim + ",");
                    }

                    s.Append(cell.Values[0].ToString());
                    s.Append(",");
                    s.Append(cell.Values[1].ToString());
                    s.Append(",");
                    s.Append(cell.Delta);

                }

                csv.Add(s.ToString());
            }

            foreach(var text in csv)
            {
                System.IO.File.AppendAllText(FileLocation, text + "\n");
            }
            

        }
    }
}
