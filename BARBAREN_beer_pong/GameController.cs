using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;

namespace BARBAREN_beer_pong_lib
{
    public class GameController
    {

        //
        // project path
        private string _projectpath;

        //
        // period path
        private string _period;
        
        public GameController()
        {
            SetupEnvironment();
        }

        private static GameController _instance;

        /**
         *  Use this to access the API
         *  
         */
        public static GameController GetInstance()
        {
            if (_instance == null)
            {
                _instance = new GameController();
            }
            return _instance;
        }

        //
        // ADD SCORE
        //

        public class ScoreProbe
        {
            public string Teamname;
            public int Won;
            public int Lost;
            public int Score;

            public ScoreProbe(string a, int b, int c, int d)
            {
                Teamname = a;
                Won = b;
                Lost = c;
                Score = d;
            }
        }

        /**
         * Returns the list of scores from the current period, sorted by socre
         */
        public ScoreProbe[] GetScores()
        {
            string alpha = GetWorkingTarget();
            Stack<ScoreProbe> probe = new Stack<ScoreProbe>();
            foreach (string line in File.ReadLines(alpha))
            {
                if (line.Contains("\t"))
                {
                    string[] selector = line.Split('\t');
                    probe.Push(new ScoreProbe(selector[0],int.Parse(selector[1]),int.Parse(selector[2]),int.Parse(selector[3])));
                }
            }

            ScoreProbe[] probes = probe.ToArray();
            while (true)
            {
                bool again = false;
                for (int i = 0; i < probes.Length; i++)
                {
                    ScoreProbe a = probes[i];
                    ScoreProbe b = null;
                    if ((i + 1) < probes.Length)
                    {
                        b = probes[i + 1];
                    }

                    if (b == null)
                    {
                        goto Gamma;
                    }

                    if (b.Score > a.Score)
                    {
                        again = true;
                        ScoreProbe c = probes[i];
                        probes[i] = probes[i + 1];
                        probes[i + 1] = c;
                    }
                }
                Gamma:

                if (!again)
                {
                    break;
                }
            }

            return probes;
        }

        /**
         * Increase win-count by 1
         */
        public void IncreseWins(string teamname)
        {
            SetWins(teamname,GetWins(teamname)+1);
        }

        /**
         * Increase lost-count by 1
         */
        public void IncreseLost(string teamname)
        {
            SetLost(teamname,GetLost(teamname)+1);
        }

        /*
         * Decrese win-cout by 1
         */
        public void DecreseWins(string teamname)
        {
            SetWins(teamname,GetWins(teamname)-1);
        }

        /*
         * Decrese lost-count by 1
         */
        public void DecreseLost(string teamname)
        {
            SetLost(teamname,GetLost(teamname)-1);
        }

        public string GetScoreTarget(string teamname)
        {
            if (GetWorkingPeriod() == null)
            {
                throw new Exception("SetWorkingPeriod not used");
            }
            
            string targetfile = GetWorkingTarget();
            string targetstring = null;
            foreach (string line in File.ReadLines(targetfile))
            {
                if (line.Contains("\t"))
                {
                    if (line.Split('\t')[0].Equals(teamname))
                    {
                        targetstring = line;
                    }
                }
            }

            return targetstring;
        }

        public string GetWorkingTarget()
        {
            return GetPeriodAttributePath("scores.txt");
        }

        public void ReplaceFileContext(string teamname, string context)
        {
            if (GetWorkingPeriod() == null)
            {
                throw new Exception("SetWorkingPeriod not used");
            }
            
            string targetfile = GetWorkingTarget();
            Stack<string> stack = new Stack<string>();
            Boolean isadded = true;
            foreach (string line in File.ReadLines(targetfile))
            {
                if (line.StartsWith(teamname + "\t"))
                {
                    stack.Push(context);
                    isadded = false;
                }
                else
                {
                    stack.Push(line);
                }
            }

            if (isadded)
            {
                stack.Push(context);
            }
            StreamWriter writer = File.CreateText(targetfile);
            foreach (string line in stack)
            {
                writer.WriteLine(line);
            }
            writer.Close();
        }

        public string[] GetScoreTagOf(string teamname)
        {
            string target = GetScoreTarget(teamname);
            if (target == null)
            {
                if (TeamExists(teamname))
                {
                    target = teamname + "\t0\t0\t0";
                }
                else
                {
                    throw new Exception("Invalid teamname");
                }
            }

            string[] tokens = target.Split('\t');
            return tokens;
        }
        

        /*
         * Set wins by team
         */
        public void SetWins(string teamname, int score)
        {
            string[] tokens = GetScoreTagOf(teamname);
            int rlx = score - int.Parse(tokens[2]);
            string rsl = teamname + "\t" + score + "\t" + tokens[2] + "\t" + rlx;
            ReplaceFileContext(teamname, rsl);
        }

        /*
         * Set losts by team
         */
        public void SetLost(string teamname, int score)
        {
            string[] tokens = GetScoreTagOf(teamname);
            int rlx = int.Parse(tokens[1]) - score ;
            string rsl = teamname + "\t" + tokens[1] + "\t" + score + "\t" + rlx;
            ReplaceFileContext(teamname, rsl);
        }

        /*
         * Get wins by team
         */
        public int GetWins(string teamname)
        {
            return int.Parse(GetScoreTagOf(teamname)[1]);
        }

        /*
         * Get losts by team
         */
        public int GetLost(string teamname)
        {
            return int.Parse(GetScoreTagOf(teamname)[2]);
        }

        /*
         * Get scoe by team
         */
        public int GetScore(string teamname)
        {
            return int.Parse(GetScoreTagOf(teamname)[3]);
        }
        
        //
        // TEAM
        //

        /*
         * Get teamnames in alfabetical order
         */
        public string[] GetTeamNames()
        {
            string[] keys =  Directory.GetDirectories(GetPeriodPath()).OrderBy(f=>f).ToArray();
            Stack<string> stack = new Stack<string>();
            foreach (string sleutel in keys)
            {
                stack.Push(sleutel.Replace(GetPeriodPath() +Path.DirectorySeparatorChar,""));
            }
            return stack.ToArray();
        }

        public bool TeamExists(string newname)
        {
            foreach (string name in GetTeamNames())
            {
                if (name.Equals(newname))
                {
                    return true;
                }
            }

            return false;
        }

        /*
         * Define a new team
         */
        public void AddTeam(string newname)
        {
            if (!TeamExists(newname))
            {
                Directory.CreateDirectory(GetTeamPath(newname));
                File.Create(GetTeamMembersPath(newname)).Close();

                
                
                Console.WriteLine("Team \"" + newname + "\" has been added to the game");
            }
            else
            {
                Console.WriteLine("Command AddTeam("+newname+") ignored because it already exists");
            }
        }

        public void SetTeamIcon(string teamname, string path)
        {
            if (path.EndsWith(".jpg"))
            {
                File.Copy(path,GetTeamIcon(teamname));
            }
            else
            {
                throw new Exception("Only jpg supported");
            }
        }

        public string GetTeamIcon(string teamname)
        {
            return GetTeamIconPath(teamname);
        }

        /*
         * Get a list of teammatesnames by team
         */
        public string[] GetTeamMembers(string teamname)
        {
            if (TeamExists(teamname))
            {
                if (File.Exists(GetTeamMembersPath(teamname)))
                {
                    return File.ReadAllLines(GetTeamMembersPath(teamname));
                }
            }
            return new string[]{};
        }

        public bool TeamMemberExists(string teamname, string teammembername)
        {
            foreach (string member in GetTeamMembers(teamname))
            {
                if (member.Equals(teammembername))
                {
                    return true;
                }
            }
            return false;
        }

        /*
         * Removes a teammate from a team
         */
        public void RemoveTeamMember(string teamname, string teammembername)
        {
            if (TeamMemberExists(teamname, teammembername))
            {
                Stack<string> stack = new Stack<string>();
                foreach (string context in File.ReadAllLines(GetTeamMembersPath(teamname)))
                {
                    if (!context.Equals(teammembername))
                    {
                        stack.Push(context);
                    }
                }

                StreamWriter stream = new StreamWriter(File.Open(GetTeamMembersPath(teamname),FileMode.Open));

                foreach (string item in stack)
                {
                    stream.WriteLine(item);
                }
                stream.Flush();
                stream.Close();
                Console.WriteLine("Removed "+teammembername+" of "+teamname);
            }
            else
            {
                Console.WriteLine("Cannot remove "+teammembername+" because he/she does not exist");
            }
        }

        /*
         * Adds a teammember to the team
         */
        public void AddTeamMember(string teamname, string teammembername)
        {
            if (TeamMemberExists(teamname, teammembername))
            {
                Console.WriteLine(teammembername+" already exists in "+teamname);
            }
            else
            {
                Stack<string> stack = new Stack<string>();
                foreach (string context in File.ReadAllLines(GetTeamMembersPath(teamname)))
                {
                    stack.Push(context);
                }
                stack.Push(teammembername);
                StreamWriter stream = new StreamWriter(File.Open(GetTeamMembersPath(teamname),FileMode.Open));

                foreach (string item in stack)
                {
                    stream.WriteLine(item);
                }
                stream.Flush();
                stream.Close();
                Console.WriteLine(teammembername+" is now a part of "+teamname);
            }
        }
        
        public string GetTeamIconPath(string teamname)
        {
            return GetTeamPath(teamname) + Path.DirectorySeparatorChar + "team.jpg";
        }

        public string GetTeamMembersPath(string teamname)
        {
            return GetTeamPath(teamname) + Path.DirectorySeparatorChar + "teammates.txt";
        }

        public string GetTeamPath(string teamname)
        {
            return GetPeriodAttributePath(teamname);
        }
        
        //
        // PERIOD
        //

        public string GetPeriodPath()
        {
            return GetPeriodPath(GetWorkingPeriod());
        }

        public string GetPeriodPath(string period)
        {
            return _projectpath + Path.DirectorySeparatorChar + period; 
        }

        public string GetPeriodAttributePath(string period, string attribute)
        {
            return GetPeriodPath(period) + Path.DirectorySeparatorChar + attribute;
        }

        public string GetPeriodAttributePath(string attribute)
        {
            return GetPeriodAttributePath(GetWorkingPeriod(), attribute);
        }
        


        public string[] GetPeriodsByDate()
        {
            string[] target = GetPeriodNames();
            while (true)
            {
                var veranderd = false;
                for (int i = 0; i < target.Length; i++)
                {
                    DateTime a = Directory.GetCreationTime(GetPeriodPath(target[i]));
                    DateTime b;
                    if ((i + 1) < target.Length)
                    {
                        b = Directory.GetCreationTime(GetPeriodPath(target[i]));
                        if (a.CompareTo(b) < 0)
                        {
                            String c = target[i];
                            String d = target[i + 1];
                            target[i] = d;
                            target[i + 1] = c;
                            veranderd = true;
                        }
                    }
                }

                if (!veranderd)
                {
                    break;
                }
            }

            return target;
        }
        
        //
        // Gets a list of available working periods
        public string[] GetPeriodNames()
        {
            string[] keys =  Directory.GetDirectories(GetBase()).OrderBy(f=>f).ToArray();
            Stack<string> stack = new Stack<string>();
            foreach (string sleutel in keys)
            {
                stack.Push(sleutel.Replace(GetBase() + Path.DirectorySeparatorChar,""));
            }
            return stack.ToArray();
        }

        //
        // Returns current working period
        public string GetWorkingPeriod()
        {
            return _period;
        }

        //
        // Sets working period
        public void SetWorkingPeriod(string per)
        {
            Console.WriteLine("Working period has been changed from "+_period + " to "+ per);
            _period = per;
        }

        //
        // Checks if period exists in the game
        public bool PeriodExists(string newname)
        {
            foreach (string name in GetPeriodNames())
            {
                if (name.Equals(newname))
                {
                    return true;
                }
            }

            return false;
        }

        //
        // Adds a new period
        public void AddPeriod(string newname)
        {
            if (!PeriodExists(newname))
            {
                Directory.CreateDirectory(GetPeriodPath(newname));
                File.Create(GetPeriodAttributePath(newname,"scores.txt")).Close();
                Console.WriteLine("Period \"" + newname + "\" has been added to the game");
            }
            else
            {
                Console.WriteLine("Command AddPeriod("+newname+") ignored because it already exists");
            }
        }
        
        //
        // DEFAULT
        //

        public string GetBase()
        {
            return _projectpath;
        }
        
        //
        // Checks if all directories and other dependencies are present 
        public void SetupEnvironment()
        {
            String pathToDesktop = AppDomain.CurrentDomain.BaseDirectory;//Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            if (Directory.Exists(pathToDesktop))
            {
                _projectpath = pathToDesktop + Path.DirectorySeparatorChar + "BarbarenBeerPong";
                if (!Directory.Exists(_projectpath))
                {
                    Directory.CreateDirectory(_projectpath);
                    Console.WriteLine("Game directory created: " + Path.DirectorySeparatorChar +_projectpath+Path.DirectorySeparatorChar+"");
                }
            }
        }
        
    }
    
}