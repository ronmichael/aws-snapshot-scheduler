
AWS Snapshot Scheduler
=============================================================
The AWS Snapshot Scheduler lets you schedule snapshots of your EC2 volumes just by adding tags
to each volume.  It will also automatically delete old snapshots based on expiration dates
that you set.

If you just want to install this and go,
grab the installer at https://github.com/ronmichael/aws-snapshot-scheduler/raw/master/installer/AwsSchedulerInstaller.msi.


News
-------------------------------------------------------------
March 16, 2013
- When reading access and secret keys from environment variables, snapshot_schedule_access and snapshot_schedule_secret are no longer used.
Instead we are now using the standard naming convention used by AWS command line tools: AWS_ACCESS_KEY and AWS_SECRET_KEY.

- When passing access and secret keys in the command line, they no longer are expected to be the 1st and 2nd parameters. Instead you
must now name the parameters with --aws-access-key and --aws-secret-key just as you would using the AWS command line tools. For example:

		awssnapshotscheduler --aws-access-key myaccesskey --aws-secret-key mysecretkey
- You can now define your region. This means you can now use awssnapshotscheduler outside of us-east-1. Just pass it in the command line as --region.
For example:	

		awssnapshotscheduler --region us-west-2
- Updated the AWS SDK included in the installer to version 1.5.16.1.
	

Requirements and dependencies (included with installer)
-------------------------------------------------------------
- AWS .NET SDK http://aws.amazon.com/sdkfornet/
- Command Line Parser Library https://github.com/gsscoder/commandline


Setup : Scheduler access keys
-------------------------------------------------------------
First you need to get an AWS access key and secret key for the app.
You can use your master account's access key but you are safer creating a dedicated account for the snapshot scheduler.
Create a new account in IAM and grab the access and secret key.
Then configure the user permissions, use the Policy Generator,  and allow access to the following Amazon EC2 actions:

- CreateSnapshot
- CreateTags
- DeleteSnapshot
- DescribeSnapshots
- DescribeTags
- DescribeVolumes


Here is a sample policy document:
		
	{
	  "Statement": [
		{
		  "Sid": "Stmt1331867841523",
		  "Action": [
			"ec2:CreateSnapshot",
			"ec2:DeleteSnapshot",
			"ec2:DescribeSnapshots",
			"ec2:DescribeTags",
			"ec2:DescribeVolumes",
			"ec2:CreateTags"
		  ],
		  "Effect": "Allow",
		  "Resource": [
			"*"
		  ]
		}
	  ]
	}

The simplest way to manage the keys is to store them in environment variables on your computer.
Store the access key in AWS_ACCESS_KEY and store the secret key in AWS_SECRET_KEY.
These are the same variables the the standard AWS command line tools use.

Otherwise, you can pass the access key and secret key in the command line. Pass the access key as --aws-access-key
and the secret key as --aws-secret-key. Again, this is the same syntax as the AWS command line tools.


Setup : Amazon volumes
-------------------------------------------------------------
The service looks through all the volumes in your account for the snapshotSchedule tag and then performs
a snapshot based on the value.  Acceptable values:

	hourly [interval] [minutesAfterTheHour] [expiration]

	daily [timeOfDay] [expirationPeriod]

	weekly [dayOfWeek] [timeOfDay] [expiration]

Interval is always expected to be simple integers.  Default is 1.

MinutesAfterTheHour is formated as a colon followed by a typical minute expression, e.g. :30, :15, :05
Default is :00.

Expiration period is formated as an "x" followed by the timeframe, e.g. : x30days, x5weeks. 
When not provided, no expiration date is set.  Valid timeframe formats are:
- days / day / d
- weeks / week / w
- months / month / m

TimeOfDay is expected in any format that .NET will recognize as a typical, e.g. 3:30pm, 15:30.  
It is expected to be in GMT/UTC.  Default is 0:00.

Some examples:
	
	daily 3pm x30 days
	hourly 3 :30 x15days
	hour :45
	weekly sunday 3:30 x2days
	weekly tuesday 



Operation
-------------------------------------------------------------
You'll want to schedule this to run regularly through something like Windows task Scheduler.

Whenever a snapshot is performed, the service also adds (or udpates) two tags in the volume.
The first is "lastSnapshot" which shows you the date and time that the last snapshot.
The next is "nextSnapshot" which shows you an estimated date and time of the next snapshot.


Thanks
-------------------------------------------------------------
Thanks to the following users for feedback & suggestions:
- Andrew Duggan
- Eric Pardee



The MIT License
-------------------------------------------------------------
Copyright (c) 2012 Ron Michael Zettlemoyer
				
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
