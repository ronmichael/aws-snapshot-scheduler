using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
//using Amazon.S3;
//using Amazon.S3.Model;
using System.Runtime.InteropServices;
using Microsoft.Win32;


namespace AwsSnapshotScheduler
{
    class Program
    {


        public static Options options;

        public static void Main(string[] args)
        {

            options = new Options();

            CommandLine.Parser parser = new CommandLine.Parser(with => with.HelpWriter = Console.Error);

            parser.ParseArgumentsStrict(args, options, () => Environment.Exit(-2));

            if (options.AccessKey == null || options.SecretKey == null)
            {
                Console.WriteLine(options.GetUsage());
                Environment.Exit(-2);
            }

            try
            {

                // ListVolumes(); // got for testing connectivity

                CheckForScheduledSnapshots();

                CheckForExpiredSnapshots();

            }
            catch (Exception err)
            {
                Console.WriteLine("Error from " + err.Source + ": " + err.Message);
                Environment.Exit(-2);
            }

            Environment.Exit(0);

        }



        /// <summary>
        /// List all volumes found in region
        /// </summary>
        public static void ListVolumes()
        {
            AmazonEC2 ec2 = Ec2Helper.CreateClient();

            DescribeVolumesRequest rq = new DescribeVolumesRequest();
            DescribeVolumesResponse rs = ec2.DescribeVolumes(rq);

            foreach (Volume v in rs.DescribeVolumesResult.Volume) {
                Console.WriteLine(v.VolumeId);

            }

        }


        /// <summary>
        /// Check for any snapshots set to expire -- that have a tag key of "expires" with a value that is in the past.
        /// </summary>
        public static void CheckForExpiredSnapshots()
        {

            Console.WriteLine("Checking for expired snapshots...");

            AmazonEC2 ec2 = Ec2Helper.CreateClient();

            DescribeSnapshotsRequest rq = new DescribeSnapshotsRequest();
            rq.WithOwner("self");
            rq.WithFilter(new Filter() { Name = "tag-key", Value = new List<string>() { "expires" } });

            DescribeSnapshotsResponse rs = ec2.DescribeSnapshots(rq);

            foreach(Snapshot s in rs.DescribeSnapshotsResult.Snapshot)
            {
                string expireText = Ec2Helper.GetTagValue(s.Tag, "expires");

                DateTime expires;

                if (DateTime.TryParse(expireText, out expires))
                {
                    if(expires<DateTime.UtcNow)
                    {
                        Console.WriteLine("Deleting " + s.SnapshotId + " for " + s.VolumeId + "...");
                        Ec2Helper.DeleteSnapsot(s.SnapshotId);
                    }
                }

            }

        }


        /// <summary>
        /// Check for any volumes that have a snapshot scheduled based on the schedule in the snapshotSchedule tag key.
        /// </summary>
        public static void CheckForScheduledSnapshots()
        {

            Console.WriteLine("Checking for scheduled snapshots...");

            AmazonEC2 ec2 = Ec2Helper.CreateClient();

            DescribeVolumesRequest rq = new DescribeVolumesRequest();
            rq.WithFilter(new Filter() { Name = "tag-key", Value = new List<string>() { "snapshotSchedule" } });
            DescribeVolumesResponse rs = ec2.DescribeVolumes(rq);

            foreach (Volume v in rs.DescribeVolumesResult.Volume)
            {

                
                string[] sch2 = Ec2Helper.GetTagValue(v.Tag, "snapshotSchedule").Split(' ');

                string volumename = Ec2Helper.GetTagValue(v.Tag, "Name");


                DateTime lastSnap; // date of last snapshot
                DateTime nextSnap; // the next backup that should have occured based on last backup
                DateTime nextNextSnap; // the next backup that should occur assuming a backup runs now or ran at the last proper time

                DateTime now = DateTime.UtcNow;

                if (!DateTime.TryParse(Ec2Helper.GetTagValue(v.Tag, "lastSnapshot"), out lastSnap))
                    lastSnap = Convert.ToDateTime("1/1/2010");
                    

                Console.WriteLine("Checking " + v.VolumeId + " / " + volumename + "...");
//sch2 = ("hourly 4 :30 x30days").Split(' ');
//lastSnap = Convert.ToDateTime("2/29/2012 6:00:15pm");
//now = Convert.ToDateTime("2/29/2012 10:00:14pm");

                

                
                switch(sch2[0])
                {
                    case "hourly": // hourly, hourly 1 :15, hourly :30, hourly 4 (pass it hours between backups & when on the hour to do it, any order; defaults to every hour on the hour)
                            
                        int ah = GetAfterTheHour(sch2, 0);
                        int hi = GetInt(sch2, 1);

                        nextSnap = lastSnap.AddMinutes(-lastSnap.Minute).AddSeconds(-lastSnap.Second).AddMilliseconds(-lastSnap.Millisecond);
                        nextSnap = nextSnap.AddHours(hi).AddMinutes(ah);

                        // this is not right
                        nextNextSnap = now.AddMinutes(-now.Minute).AddSeconds(-now.Second).AddMilliseconds(-now.Millisecond);
                        nextNextSnap = nextNextSnap.AddMinutes(ah).AddHours(hi);

                        break;

                    case "daily": // daily, 3pm, daily 15:15, daily 3:30pm (times are UTC; defaults to midnight UTC)

                        DateTime hour = GetTime(sch2, Convert.ToDateTime("0:00"));

                        nextSnap = lastSnap.Date.AddDays(1).AddTicks(hour.TimeOfDay.Ticks);

                        nextNextSnap = now.Date.AddDays(1).AddTicks(hour.TimeOfDay.Ticks);
                            
                        break;

                    case "weekly": // weekly, weekly sunday, weekly thursday 3pm (times are UTC; defaults to sunday midnight UTC)

                        DateTime whour = GetTime(sch2, Convert.ToDateTime("0:00"));
                        DayOfWeek dow = GetDow(sch2, DayOfWeek.Sunday);

                        if(lastSnap.DayOfWeek>=dow)
                            nextSnap = lastSnap.Date.AddDays(-(int)lastSnap.DayOfWeek).AddDays(7 + (int)dow).AddTicks(whour.TimeOfDay.Ticks);
                        else
                            nextSnap = lastSnap.Date.AddDays(-(int)lastSnap.DayOfWeek).AddDays((int)dow).AddTicks(whour.TimeOfDay.Ticks);
                            
                        nextNextSnap = now.Date.AddDays(-(int)now.DayOfWeek).AddDays(7 + (int)dow).AddTicks(whour.TimeOfDay.Ticks);
                        if (nextSnap == nextNextSnap)
                            nextNextSnap = nextNextSnap.AddDays(7);
                            
                        break;
                    default:
                        lastSnap = now.AddYears(1);
                        nextSnap = lastSnap;
                        nextNextSnap = lastSnap;
                        break;
                }

                    
//Console.WriteLine("last=" + lastSnap.ToString());
//Console.WriteLine("now=" + now);
//Console.WriteLine("next=" + nextSnap.ToString());
//Console.WriteLine("nextNext=" + nextNextSnap.ToString());
//Console.ReadKey();
//return;
                if (nextSnap <= now)
                {

                    // create snapshot of volume

                    string expires = "";
                    int expireHours = GetExpireHours(sch2, 0);
                    if (expireHours > 0)
                    {
                        expires = now.AddHours(expireHours).ToString();
                    }


                    Backup(volumename, "automatic", v.VolumeId, volumename, Ec2Helper.GetInstanceName(v.Attachment[0].InstanceId), expires);


                    // update volume tags

                    CreateTagsRequest rqq = new CreateTagsRequest();

                    rqq.WithResourceId(v.VolumeId);

                    nextSnap = nextSnap.AddSeconds(-nextSnap.Second).AddMilliseconds(-nextSnap.Millisecond);

                    rqq.WithTag(new Tag[] {
                        new Tag { Key = "lastSnapshot", Value = now.ToString() },
                        new Tag { Key = "nextSnapshot", Value = nextNextSnap.ToString() }
                    });

                    var createTagResponse = ec2.CreateTags(rqq);
                }
                else
                {
                    Console.WriteLine("    Next scheduled " + nextSnap.ToString());
                }


            }



        }




        public static void Backup(string name, string description, string volumeid, string volumename, string instancename, string expires)
        {

            Console.Write("create snapshot of " + volumeid + " / " + volumename + " / " + instancename);

            AmazonEC2 ec2 = Ec2Helper.CreateClient();

            CreateSnapshotRequest rq = new CreateSnapshotRequest();
            rq.VolumeId = volumeid;
            rq.Description = description;
            
            CreateSnapshotResponse rs = ec2.CreateSnapshot(rq);

            string snapshotid = rs.CreateSnapshotResult.Snapshot.SnapshotId;

            // create tag with name and expiration date

            CreateTagsRequest rqq = new CreateTagsRequest();
            
            rqq.WithResourceId(snapshotid);
            
            rqq.WithTag(new Tag[] {
                new Tag { Key = "Name", Value = name },
                new Tag { Key = "source", Value = "scheduler" },
                new Tag { Key = "instance", Value = instancename },
                new Tag { Key = "volume", Value = volumename },
                new Tag { Key = "expires", Value = expires.ToString() }
            });


            var createTagResponse = ec2.CreateTags(rqq);



        }

 




        #region Parameter processing functions

        public static int GetAfterTheHour(string[] p, int def = 0)
        {

            int ah = def;

            for (int x = 0; x < p.Length; x++)
            {
                string r = p[x];
                if (r.StartsWith(":"))
                {
                    int z;
                    if (int.TryParse(r.Substring(1), out z))
                    {
                        if (z >= 0 || z < 60)
                            ah = z;
                    }
                    break;
                }
            }

            return ah;

        }

        public static int GetExpireHours(string[] p, int def = 0) // parameter prefixed with x
        {

            int ah = def;

            for (int x = 0; x < p.Length; x++)
            {
                string r = p[x];
                if (r.StartsWith("x"))
                {

                    MatchCollection mc = Regex.Matches(r.Substring(1), @"(\d*)(d|day|days|h|hour|hours|w|week|weeks)?", RegexOptions.IgnoreCase);
                    if (mc.Count > 0)
                    {
                        if (mc[0].Groups.Count >= 2)
                        {
                            ah = Convert.ToInt32(mc[0].Groups[1].Value);
                        }
                        if (mc[0].Groups.Count >= 3)
                        {
                            switch (mc[0].Groups[2].Value.ToLower())
                            {
                                case "d":
                                case "days":
                                case "day":
                                    ah = ah * 24;
                                    break;
                                case "w":
                                case "weeks":
                                case "week":
                                    ah = ah * 24 * 7;
                                    break;
                                case "m":
                                case "months":
                                case "month":
                                    ah = ah * 24 * 30;
                                    break;

                            }
                        }

                    }

                    break;
                }
            }

            return ah;

        }

        public static int GetInt(string[] p, int def = 0)
        {

            int ah = def;

            for (int x = 0; x < p.Length; x++)
            {
                string r = p[x];
                int z;
                if (int.TryParse(r, out z))
                {
                    ah = z;
                    break;
                }
            }

            return ah;

        }

        public static DayOfWeek GetDow(string[] p, DayOfWeek def = DayOfWeek.Sunday)
        {
            List<string> dows = new List<string> { "sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday" };
            int dow = (int)def;

            for (int x = 0; x < p.Length; x++)
            {
                string r = p[x].ToLower();
                for (int xx = 0; xx < dows.Count; xx++)
                {
                    string dd = dows[xx];
                    if (dd == r || dd.Substring(0, 3) == r)
                    {
                        dow = x + 1;
                        break;
                    }
                }
            }

            return (DayOfWeek)dow;

        }

        public static DateTime GetTime(string[] p, DateTime? def = null)
        {

            DateTime outt = Convert.ToDateTime("0:00");
            if (def != null) outt = (DateTime)def;

            for (int x = 0; x < p.Length; x++)
            {
                string r = p[x];
                if (Regex.IsMatch(r, @"^((0?[1-9]|1[012])(:[0-5]\d){0,2}((\ )?[AP]M))$|^([1-9]|[01]\d|2[0-3])(:[0-5]\d){0,2}$", RegexOptions.IgnoreCase))
                {
                    DateTime z;
                    if (DateTime.TryParse(r, out z))
                    {
                        outt = z;
                        break;
                    }


                }
            }

            return outt;

        }

        #endregion

        
        // use these functions later to help user register settings in environment

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool
            SendMessageTimeout(
            IntPtr hWnd,
            int Msg,
            int wParam,
            string lParam,
            int fuFlags,
            int uTimeout,
            out int lpdwResult
            );

        
        public const int HWND_BROADCAST = 0xffff;
        public const int WM_SETTINGCHANGE = 0x001A;
        public const int SMTO_NORMAL = 0x0000;
        public const int SMTO_BLOCK = 0x0001;
        public const int SMTO_ABORTIFHUNG = 0x0002;
        public const int SMTO_NOTIMEOUTIFNOTHUNG = 0x0008;

        public static void RegisterKeys(string access, string secret)
        {
            // with all this... it STILL doesn't seem to register in new command prompts or the system until you log out
            // OR until you go into System properties / Advanced / Environment variables and hit OK...

            //Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "snapshot_schedule_access", access);
            //Registry.SetValue("HKEY_LOCAL_MACHINE\\SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Environment", "snapshot_schedule_secret", secret);
            
            System.Environment.SetEnvironmentVariable("snapshot_schedule_access", access, EnvironmentVariableTarget.Machine);
            System.Environment.SetEnvironmentVariable("snapshot_schedule_secret", secret, EnvironmentVariableTarget.Machine);
            
            /*
            System.Environment.SetEnvironmentVariable("snapshot_schedule_access", access, EnvironmentVariableTarget.Process);
            System.Environment.SetEnvironmentVariable("snapshot_schedule_secret", secret, EnvironmentVariableTarget.Process);
            System.Environment.SetEnvironmentVariable("snapshot_schedule_access", access, EnvironmentVariableTarget.User);
            System.Environment.SetEnvironmentVariable("snapshot_schedule_secret", secret, EnvironmentVariableTarget.User);
            */
            int result;
            
            SendMessageTimeout((System.IntPtr)HWND_BROADCAST,
                WM_SETTINGCHANGE, 0, "Environment", SMTO_BLOCK | SMTO_ABORTIFHUNG |
                SMTO_NOTIMEOUTIFNOTHUNG, 5000, out result);
            

        }
 
    }
}