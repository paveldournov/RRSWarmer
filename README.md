# RRSWarmer
Automatic service warm up for AzureML Web Service API 

Once you create an AzureML Web Service, to keep it always ready for low latency responses follow this instruction:

1) Download and compile the code in this repository

2) This will produce `AniStresser.exe` 

3) Create a file named `run.cmd` with this content, replace {placeholders} with your id's and keys

	AniStresser.exe warm {your workspace} {workspace access token} {your service id} {endpoint name} input.json output.txt
	
4) Create input.json file with a sample payload for your service

5) Start run.cmd to test that the service works fine

6) Create a zip file with AniStresser.exe, run.cmd, and all other files needed to run the tool

7) Go to manage.windowsazure.com -> WEBSITES -> [create a new web site, *it is only needed for the web job, you don't actually need to create an actual site*]
* Go to that web site, and click WEBJOBS tab
* Create a new web job, in US South Central
* Use the zip file created above to supply the payload for the job
* Configure the job to run on SCHEDULE, every 6 hrs
	
  
----------------------
* *This is not an official tool from AzureML, no guarantees are given with it*
* *The tool makes service calls which may incur AzureML charges.* 
   
