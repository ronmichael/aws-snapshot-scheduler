﻿    using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Amazon;
using Amazon.EC2;
using Amazon.EC2.Model;
 


namespace AwsSnapshotScheduler
{
    class Ec2Helper
    {

       
  

        /// <summary>
        /// Return the EC2 client
        /// </summary>
        /// <returns></returns>
        public static AmazonEC2Client CreateClient()
        {
 
            AmazonEC2Config config = new AmazonEC2Config();
            config.ServiceURL = "https://ec2." + Program.options.Region + ".amazonaws.com";
            //config.RegionEndpoint = RegionEndpoint.USEast1;

            AmazonEC2Client ec2 = new Amazon.EC2.AmazonEC2Client(Program.options.AccessKey, Program.options.SecretKey, config);
            //AmazonEC2 ec2 = AWSClientFactory.CreateAmazonEC2Client(Program.options.AccessKey, Program.options.SecretKey, config);
            
            return ec2;

        }


        /// <summary>
        /// Delete the snapshop with the given ID from EBS
        /// </summary>
        /// <param name="p"></param>
        public static void DeleteSnapsot(string snapshotid)
        {

            AmazonEC2Client ec2 = CreateClient();

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

            AmazonEC2Client ec2 = CreateClient();

            DescribeTagsRequest rq = new DescribeTagsRequest();

            rq.Filters.Add(new Filter() { Name = "resource-id", Values = new List<string>() { instanceId } });

            DescribeTagsResponse rs = ec2.DescribeTags(rq);

            string name = "";
            
            TagDescription tag = rs.Tags.Find(item => item.Key == "Name");
            if (tag != null) name = tag.Value;

            return name;

        }


     

        public static string GetTagValue(List<Amazon.EC2.Model.Tag> Tags, string tagname)
        {
            Amazon.EC2.Model.Tag t = Tags.Find(item => item.Key == tagname);
            if (t != null)
                return t.Value;
            else
                return "";

        }


    }



}
