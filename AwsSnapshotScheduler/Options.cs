using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine;
using CommandLine.Text;


namespace AwsSnapshotScheduler
{
    class Options
    {
    
        private string accesskey, secretkey, region;

        public Options()
        {
           
        }


        [Option('O', "aws-access-key", DefaultValue="environment variable AWS_ACCESS_KEY", HelpText = "Access key ID associated with your AWS account.")]
        public string AccessKey {
            get { return accesskey; }
            set { 
                if(value=="environment variable AWS_ACCESS_KEY")
                    accesskey = System.Environment.GetEnvironmentVariable("AWS_ACCESS_KEY"); 
                else
                    accesskey = value; 
            }
        }

        [Option('W', "aws-secret-key", DefaultValue = "environment variable AWS_SECRET_KEY", HelpText = "Secret access key associated with your AWS account.")]
        public string SecretKey
        {
            get { return secretkey; }
            set { 
                if(value=="environment variable AWS_SECRET_KEY")
                    secretkey = System.Environment.GetEnvironmentVariable("AWS_SECRET_KEY"); 
                else
                    secretkey = value; 
            }
        }

        [Option("region", DefaultValue = "us-east-1", HelpText = "Amazon region to target. Overrides the region specified by the EC2_URL environment variable.")]
        public string Region
        {
            get { return region; }
            set { region = value; }
        }

        [ParserState]
        public IParserState LastParserState { get; set; }

        [HelpOption]
        public string GetUsage()
        {
            return HelpText.AutoBuild(this,
              (HelpText current) => HelpText.DefaultParsingErrorsHandler(this, current));
        }
    }
}
