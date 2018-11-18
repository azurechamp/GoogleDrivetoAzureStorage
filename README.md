# GoogleDrivetoAzureStorage
Script to Migrate Data from your Google Drive to Azure Storage 

## Installation

	git clone https://github.com/azurechamp/GoogleDrivetoAzureStorage.git

You have to install Google Drive API for dotnet using nuget package console.

	PM> Install-Package Google.Apis.Drive.v3

And then you have to create or select a project in the Google Developers Console and turn on the API. After turn on the API, you can get credential file. [reference Step1](https://developers.google.com/drive/v3/web/quickstart/dotnet)

After download JSON file
	
1. rename to `google_secret.json`
2. create `credentials` directory under the `google-drive-sample`
3. move `google_secret.json` to `google-drive-sample/credentials`

Connect with Azure Storage Account 

1. Place your Azure Storage Credentials into config file and reference it in the code to connect.
2. Rename the default contanier or to one if you have already created.

Once you have done all, make a CSV file for your data that you want to migrate, or you can use Google Drive helper class to list files and save them to Azure Storage as blobs. In case it's csv, tweak the logic already provided.

## Azure Logic App Limitation
You can also try to do this via Azure Logic app but it has limitation of 50 MB download from Google Drive which happens to be a big time limitation while we think about migration of our data via Azure Logic App. You can use this project to get things going without limitations, I've tested 500 + mb files and 1+ TB data migrated within a day.



Special thanks : https://github.com/se0kjun/google-drive-sample
