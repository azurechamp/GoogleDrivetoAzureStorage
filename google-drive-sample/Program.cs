using Google.Apis.Drive.v2.Data;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace google_drive_sample
{
    static class Program
    {

        
        static void Main()
        {

         GoogleDriveHelper gdrive_helper;
         string current_file_path;
         var CSV_PATH = "\\Users\\PATH-TO-CSV"; //Add Location for .csv file if you want to read data from CSV
         var reader = new System.IO.StreamReader(System.IO.File.OpenRead(CSV_PATH));

            while (!reader.EndOfStream)
            {
                //Read File 
                var line = reader.ReadLine();
                //Split CSV Data
                var values = line.Split(',');

                //Extract Values out of Split Data
                var id = values[0];
                var title = values[1];
                var videoid = values[2];
            
               
                //Call Google Drive helper 
                gdrive_helper = new GoogleDriveHelper();
                //Get file by Google Drive File ID
                var a = gdrive_helper.GetFileById(videoid);
                //Download the Stream of file using Google's API
                var stream = gdrive_helper.DownloadFile1(a, string.Format(@"C:\FilesFromGoogleDrive\{0}", a.Title));
                //Upload the Stream as Blob using Azure Storage API FROM DATA STREAM
                ProcessAsync(stream,newTitle).GetAwaiter().GetResult();
                Console.WriteLine(a.Title);


            }
            

        }




        private static async Task ProcessAsync(System.IO.MemoryStream stream, string title)
        {
            CloudStorageAccount storageAccount = null;
            CloudBlobContainer cloudBlobContainer = null;
            string sourceFile = null;
            string destinationFile = null;

            string storageConnectionString = ""; //Better idea to put it in config file

            // Check whether the connection string can be parsed.
            if (CloudStorageAccount.TryParse(storageConnectionString, out storageAccount))
            {
                try
                {
                    // Create the CloudBlobClient that represents the Blob storage endpoint for the storage account.
                    CloudBlobClient cloudBlobClient = storageAccount.CreateCloudBlobClient();

                    // Create a container called 'testcontainer'
                    cloudBlobContainer = cloudBlobClient.GetContainerReference("testcontainer");
                    await cloudBlobContainer.CreateIfNotExistsAsync();
                   
                    Console.WriteLine("Created container", cloudBlobContainer.Name);
                    Console.WriteLine();

                    // Set the permissions so the blobs are public. 
                    BlobContainerPermissions permissions = new BlobContainerPermissions
                    {
                        PublicAccess = BlobContainerPublicAccessType.Blob
                    };
                    await cloudBlobContainer.SetPermissionsAsync(permissions);

                    
                    CloudBlockBlob cloudBlockBlob = cloudBlobContainer.GetBlockBlobReference(title);

                    stream.Position = 0;
                    cloudBlockBlob.UploadFromStream(stream);
                    
                }
                catch (StorageException ex)
                {
                    Console.WriteLine("Error returned from the service: {0}", ex.Message);
                }
           
            }
            else
            {
                Console.WriteLine(
                    "A connection string has not been defined in the system environment variables. " +
                    "Add a environment variable named 'storageconnectionstring' with your storage " +
                    "connection string as a value.");
            }
        }
    }
    
}
