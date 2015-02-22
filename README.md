# RRSWarmer
Automatic service warm up for AzureML Web Service API 

Once you create an AzureML Web Service, to keep it always ready for low latency responses follow this instruction:

1) Download and compile the code in this repository

2) This will produce `AniStresser.exe` 

3) Create a file named `run.cmd` with this content, replace {placeholders} with your id's and keys

	AniStresser.exe ussouthcentral.services.azureml.net workspaces/{your workspace}/services/{your service id}/execute?api-version=2.0 {service key} output.txt input.json 40:40:5
	
4) Create input.json file with a sample payload for your service

5) Start run.cmd to test that the service works fine

6) Create a zip file with AniStresser.exe, run.cmd, and all other files needed to run the tool

7) Go to manage.windowsazure.com -> WEBSITES -> [creat a new web site]
* Go to that web site, and click WEBJOBS tab
* Create a new web job, in US South Central
* Use the zip file created above to supply the payload for the job
* Configure the job to run on *SCHEDULE*, every 6 hrs
	
  
8) You are done

*Note that the tool makes service calls which may incur AzureML charges. Every run makes 240 calls, ~1000 calls/day = $0.5, or $15/month
   
