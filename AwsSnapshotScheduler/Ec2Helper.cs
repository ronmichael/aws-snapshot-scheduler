    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
using Amazon.SimpleDB;
using Amazon.SimpleDB.Model;
using Amazon.S3;
using Amazon.S3.Model;



namespace AwsSnapshotScheduler
{
    class Ec2Helper
    {

       
  

        /// <summary>
        /// Return the EC2 client
        /// </summary>
        /// <returns></returns>
        public static AmazonEC2 CreateClient()
        {

            string awsAccessKey = "";
            string awsSecretAccessKey = "";

            string[] args = System.Environment.GetCommandLineArgs();

            if (args.Length >= 2)
            {
                awsAccessKey = args[0];
                awsSecretAccessKey = args[1];
            }
            else
            {
                awsAccessKey = System.Environment.GetEnvironmentVariable("snapshot_schedule_access") ?? "";
                awsSecretAccessKey = System.Environment.GetEnvironmentVariable("snapshot_schedule_secret") ?? "";
              
            }

            if (awsSecretAccessKey.Length == 0 || awsSecretAccessKey.Length == 0)
            {
                Console.WriteLine("Missing access key and secret access key. Pass in command line or environment variables (snapshot-schedule-access and snapshot-schedule-secret)");
                System.Environment.Exit(500);
            }
            
            AmazonEC2 ec2 = AWSClientFactory.CreateAmazonEC2Client(awsAccessKey, awsSecretAccessKey);
            
            return ec2;

        }


        /// <summary>
        /// Delete the snapshop with the given ID from EBS
        /// </summary>
        /// <param name="p"></param>
        public static void DeleteSnapsot(string snapshotid)
        {

            AmazonEC2 ec2 = CreateClient();

            DeleteSnapshotRequest rq = new DeleteSnapshotRequest();
            rq.SnapshotId = snapshotid;

            DeleteSnapshotResponse rs = ec2.DeleteSnapshot(rq);

        }


        /// <summary>
        /// Get the name of the instances with the given instanceID from EC2
        /// </summary>
        /// <param name="instanceId"></param>
        /// <returns></returns>
        public static string GetInstanceName(string instanceId)
        {

            AmazonEC2 ec2 = CreateClient();

            DescribeTagsRequest rq = new DescribeTagsRequest();

            rq.WithFilter(new Filter() { Name = "resource-id", Value = new List<string>() { instanceId } });

            DescribeTagsResponse rs = ec2.DescribeTags(rq);

            string name = "";

            ResourceTag tag = rs.DescribeTagsResult.ResourceTag.Find(item => item.Key == "Name");
            if (tag != null) name = tag.Value;

            return name;

        }


     

        public static string GetTagValue(List<Tag> Tags, string tagname)
        {
            Tag t = Tags.Find(item => item.Key == tagname);
            if (t != null)
                return t.Value;
            else
                return "";

        }


    }



}
